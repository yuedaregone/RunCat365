// Copyright 2020 Takuto Nakamura
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using RunCat365.Properties;
using System.Globalization;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace RunCat365
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
#if DEBUG
            var defaultCultureInfo = SupportedLanguage.English.GetDefaultCultureInfo();
#else
            var defaultCultureInfo = SupportedLanguageExtension.GetCurrentLanguage().GetDefaultCultureInfo();
#endif
            CultureInfo.CurrentUICulture = defaultCultureInfo;
            CultureInfo.CurrentCulture = defaultCultureInfo;

            using var procMutex = new Mutex(true, "_RUNCAT_MUTEX", out var result);
            if (!result) return;

            try
            {
                var app = new System.Windows.Application();
                var context = new RunCat365ApplicationContext();
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                app.Run();
            }
            finally
            {
                procMutex?.ReleaseMutex();
            }
        }
    }

    internal class RunCat365ApplicationContext
    {
        private readonly LaunchAtStartupManager launchAtStartupManager;
        private readonly ContextMenuManager contextMenuManager;
        private readonly FloatingWindow floatingWindow;
        private readonly FrameAnimationEngine animationEngine;
        private readonly TomatoClock tomatoClock;
        private readonly DispatcherTimer tomatoTimer;
        private Runner runner = Runner.Cat;
        private int tomatoClockDuration = 25;

        public RunCat365ApplicationContext()
        {
            UserSettings.Default.Reload();
            _ = Enum.TryParse(UserSettings.Default.Runner, out runner);
            
            if (UserSettings.Default.TomatoClockDuration > 0)
            {
                tomatoClockDuration = UserSettings.Default.TomatoClockDuration;
            }

            launchAtStartupManager = new LaunchAtStartupManager();
            
            tomatoClock = new TomatoClock();
            tomatoClock.SetDuration(tomatoClockDuration);
            tomatoClock.Completed += TomatoClock_Completed;

            tomatoTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            tomatoTimer.Tick += TomatoTimer_Tick;

            animationEngine = new FrameAnimationEngine();
            animationEngine.FrameChanged += AnimationEngine_FrameChanged;

            contextMenuManager = new ContextMenuManager(
                () => runner,
                r => ChangeRunner(r),
                () => launchAtStartupManager.GetStartup(),
                s => launchAtStartupManager.SetStartup(s),
                () => ExitApplication(),
                () => tomatoClockDuration,
                d => ChangeTomatoClockDuration(d),
                () => tomatoClock.IsRunning,
                StartTomatoClock,
                PauseTomatoClock,
                ResetTomatoClock
            );

            floatingWindow = new FloatingWindow();

            InitializeAnimation();

            floatingWindow.Show();

            tomatoTimer.Start();
            tomatoClock.Start();
            animationEngine.SetTomatoClockState(DateTime.Now, TimeSpan.FromMinutes(tomatoClockDuration), true);

            ShowBalloonTipIfNeeded();
        }

        private void InitializeAnimation()
        {
            var (frameWidth, frameHeight) = contextMenuManager.GetFrameDimensions(runner);
            var spritesheet = GenerateSpritesheet(runner);

            animationEngine.LoadSpritesheet(spritesheet, frameWidth, frameHeight);
            animationEngine.Start();

            floatingWindow.LoadSpritesheet(spritesheet, frameWidth, frameHeight);
        }

        private BitmapSource GenerateSpritesheet(Runner runner)
        {
            var runnerName = runner.GetString();
            var frameCount = runner.GetFrameNumber();
            
            var firstIconName = $"{runnerName}_0".ToLower();
            var firstFrame = Resources.ResourceManager.GetObject(firstIconName);
            if (firstFrame is not System.Drawing.Bitmap firstBitmap)
            {
                throw new InvalidOperationException($"Failed to load first frame: {firstIconName}");
            }

            int frameWidth = firstBitmap.Width;
            int frameHeight = firstBitmap.Height;

            var spritesheet = new System.Windows.Media.Imaging.WriteableBitmap(
                frameCount * frameWidth, frameHeight, 96, 96,
                System.Windows.Media.PixelFormats.Bgra32, null);

            for (int i = 0; i < frameCount; i++)
            {
                var iconName = $"{runnerName}_{i}".ToLower();
                var frame = Resources.ResourceManager.GetObject(iconName);
                if (frame is not System.Drawing.Bitmap bitmap) continue;

                var rect = new System.Windows.Int32Rect(i * frameWidth, 0, frameWidth, frameHeight);
                var bitmapData = bitmap.LockBits(
                    new System.Drawing.Rectangle(0, 0, frameWidth, frameHeight),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                try
                {
                    var stride = bitmapData.Stride;
                    var pixels = new byte[stride * frameHeight];
                    System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, pixels, 0, pixels.Length);
                    spritesheet.WritePixels(rect, pixels, stride, 0);
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }
            }

            spritesheet.Freeze();
            return spritesheet;
        }

        private void AnimationEngine_FrameChanged(object? sender, int frameIndex)
        {
            floatingWindow.SetFrame(frameIndex);
            contextMenuManager.AdvanceTrayIcon();
            var trayIcon = contextMenuManager.GetCurrentTrayIcon();
            if (trayIcon is not null)
            {
                // Tray icon updates can be throttled if needed
            }
        }

        private void ExitApplication()
        {
            Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        private void ShowBalloonTipIfNeeded()
        {
            if (UserSettings.Default.FirstLaunch)
            {
                contextMenuManager.ShowBalloonTip(BalloonTipType.AppLaunched);
                UserSettings.Default.FirstLaunch = false;
                UserSettings.Default.Save();
            }
        }

        private void ChangeRunner(Runner r)
        {
            runner = r;
            UserSettings.Default.Runner = runner.ToString();
            UserSettings.Default.Save();
            RegenerateSpritesheet();
        }

        private void ChangeTomatoClockDuration(int duration)
        {
            tomatoClockDuration = duration;
            tomatoClock.SetDuration(duration);
            UserSettings.Default.TomatoClockDuration = duration;
            UserSettings.Default.Save();

            if (tomatoClock.IsRunning)
            {
                animationEngine.SetTomatoClockState(DateTime.Now, TimeSpan.FromMinutes(tomatoClockDuration), true);
            }
        }

        private void StartTomatoClock()
        {
            animationEngine.Reset();
            animationEngine.SetTomatoClockState(DateTime.Now, TimeSpan.FromMinutes(tomatoClockDuration), true);
            tomatoClock.Start();
        }

        private void PauseTomatoClock()
        {
            tomatoClock.Pause();
            animationEngine.SetTomatoClockState(DateTime.Now, TimeSpan.FromMinutes(tomatoClockDuration), false);
        }

        private void ResetTomatoClock()
        {
            animationEngine.Reset();
            animationEngine.SetTomatoClockState(DateTime.Now, TimeSpan.FromMinutes(tomatoClockDuration), false);
            tomatoClock.Reset();
        }

        private void TomatoClock_Completed(object? sender, EventArgs e)
        {
        }

        private void TomatoTimer_Tick(object? sender, EventArgs e)
        {
            UpdateTomatoClockInfo();
        }

        private string GetTomatoClockDescription()
        {
            if (tomatoClock.IsCompleted)
            {
                return $"{Strings.SystemInfo_TomatoClock}: Complete!";
            }
            
            var remainingMinutes = tomatoClock.RemainingSeconds / 60;
            var remainingSeconds = tomatoClock.RemainingSeconds % 60;
            return $"{Strings.SystemInfo_TomatoClock}: {remainingMinutes:D2}:{remainingSeconds:D2}";
        }

        private void UpdateTomatoClockInfo()
        {
            contextMenuManager.SetNotifyIconText(GetTomatoClockDescription());

            var systemInfoValues = new List<string>();

            systemInfoValues.Add(GetTomatoClockDescription());
            if (tomatoClock.IsRunning)
            {
                systemInfoValues.Add($"Progress: {tomatoClock.GetProgress():P0}");
            }

            contextMenuManager.SetSystemInfoMenuText(string.Join("\n", systemInfoValues));
        }

        private void RegenerateSpritesheet()
        {
            var (frameWidth, frameHeight) = contextMenuManager.GetFrameDimensions(runner);
            var spritesheet = GenerateSpritesheet(runner);

            animationEngine.LoadSpritesheet(spritesheet, frameWidth, frameHeight);
            floatingWindow.LoadSpritesheet(spritesheet, frameWidth, frameHeight);
        }

        public void Dispose()
        {
            tomatoTimer.Stop();
            tomatoClock.Dispose();

            animationEngine.Stop();

            floatingWindow?.Close();

            contextMenuManager?.HideNotifyIcon();
            contextMenuManager?.Dispose();
        }
    }
}
