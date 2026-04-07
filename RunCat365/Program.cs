using RunCat365.Properties;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace RunCat365
{
    internal static class Program
    {
        private const string MutexName = "_RUNCAT_MUTEX";
        private const string ActivateEventName = "_RUNCAT_ACTIVATE";

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        [STAThread]
        static void Main()
        {
#if DEBUG
            CultureInfo defaultCultureInfo = SupportedLanguage.English.GetDefaultCultureInfo();
#else
            CultureInfo defaultCultureInfo = SupportedLanguageExtension.GetCurrentLanguage().GetDefaultCultureInfo();
#endif
            CultureInfo.CurrentUICulture = defaultCultureInfo;
            CultureInfo.CurrentCulture = defaultCultureInfo;

            Mutex procMutex = new Mutex(true, MutexName, out bool isFirstInstance);
            if (!isFirstInstance)
            {
                using EventWaitHandle existingEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivateEventName);
                existingEvent.Set();
                return;
            }

            Application app = new Application();
            app.DispatcherUnhandledException += App_DispatcherUnhandledException;
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            using EventWaitHandle activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivateEventName);

            RunCat365ApplicationContext? context = null;
            app.Startup += (sender, e) =>
            {
                context = new RunCat365ApplicationContext();
            };

            ThreadPool.RegisterWaitForSingleObject(activateEvent, (state, timedOut) =>
            {
                if (context is not null)
                {
                    app.Dispatcher.Invoke(() => ActivateWindow(context.floatingWindow));
                }
            }, null, Timeout.Infinite, false);

            try
            {
                app.Run();
            }
            finally
            {
                procMutex.ReleaseMutex();
            }
        }

        private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
        }

        private static void ActivateWindow(FloatingWindow window)
        {
            if (window is not null)
            {
                IntPtr hWnd = new WindowInteropHelper(window).Handle;
                if (hWnd != IntPtr.Zero)
                {
                    ShowWindowAsync(hWnd, SW_RESTORE);
                    SetForegroundWindow(hWnd);
                }
            }
        }
    }

    internal class RunCat365ApplicationContext
    {
        internal readonly FloatingWindow floatingWindow;
        private readonly LaunchAtStartupManager launchAtStartupManager;
        private readonly ContextMenuManager contextMenuManager;
        private readonly FrameAnimationEngine animationEngine;
        private readonly TomatoClock tomatoClock;
        private readonly DispatcherTimer tomatoTimer;
        private readonly AppConfig config;
        private Runner runner = Runner.Cat;
        private int tomatoClockDuration = 25;

        public RunCat365ApplicationContext()
        {
            try
            {
                config = AppConfig.Instance;
                config.Reload();
                _ = Enum.TryParse(config.Runner, out runner);

                if (config.TomatoClockDuration > 0)
                {
                    tomatoClockDuration = config.TomatoClockDuration;
                }
            }
            catch
            {
                config = new AppConfig();
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

            try
            {
                animationEngine = new FrameAnimationEngine(config);
                animationEngine.FrameChanged += AnimationEngine_FrameChanged;
                animationEngine.SetTomatoClock(() => tomatoClock.GetProgress());
            }
            catch
            {
                AppConfig fallbackConfig = new AppConfig();
                animationEngine = new FrameAnimationEngine(fallbackConfig);
                animationEngine.FrameChanged += AnimationEngine_FrameChanged;
                animationEngine.SetTomatoClock(() => tomatoClock.GetProgress());
            }

            floatingWindow = new FloatingWindow(config);

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

            InitializeAnimation();

            floatingWindow.Show();
            animationEngine.SpeedChanged += floatingWindow.SetSpeed;
            floatingWindow.StartMoveTimer();

            tomatoTimer.Start();
            tomatoClock.Start();

            ShowBalloonTipIfNeeded();
        }

        private void InitializeAnimation()
        {
            try
            {
                Tuple<BitmapSource, int, int> spritesheetData = GenerateSpritesheet(runner);

                floatingWindow.RestorePosition();

                animationEngine.LoadSpritesheet(spritesheetData.Item1, spritesheetData.Item2, spritesheetData.Item3);
                animationEngine.Start();

                floatingWindow.LoadSpritesheet(spritesheetData.Item1, spritesheetData.Item2, spritesheetData.Item3);
            }
            catch
            {
                WriteableBitmap fallbackSpritesheet = new WriteableBitmap(
                    48, 48, 96, 96,
                    System.Windows.Media.PixelFormats.Bgra32, null);

                animationEngine.LoadSpritesheet(fallbackSpritesheet, 48, 48);
                animationEngine.Start();

                floatingWindow.LoadSpritesheet(fallbackSpritesheet, 48, 48);
            }
        }

        private Tuple<BitmapSource, int, int> GenerateSpritesheet(Runner runner)
        {
            string runnerName = runner.GetString();
            int frameCount = runner.GetFrameNumber();

            WriteableBitmap? cachedSpritesheet = ResourceLoader.GetCachedSpritesheet(runnerName, frameCount);
            if (cachedSpritesheet is not null)
            {
                int frameWidth = cachedSpritesheet.PixelWidth / frameCount;
                int frameHeight = cachedSpritesheet.PixelHeight;
                return Tuple.Create<BitmapSource, int, int>(cachedSpritesheet, frameWidth, frameHeight);
            }

            int maxWidth = 0;
            int maxHeight = 0;
            for (int i = 0; i < frameCount; i++)
            {
                byte[]? pixels = ResourceLoader.GetRunnerPixels(runnerName, i, out int w, out int h);
                if (pixels is null) continue;
                if (w > maxWidth) maxWidth = w;
                if (h > maxHeight) maxHeight = h;
            }

            if (maxWidth == 0 || maxHeight == 0)
            {
                maxWidth = 48;
                maxHeight = 48;
            }

            WriteableBitmap spritesheet = new WriteableBitmap(
                frameCount * maxWidth, maxHeight, 96, 96,
                System.Windows.Media.PixelFormats.Bgra32, null);

            for (int i = 0; i < frameCount; i++)
            {
                byte[]? pixels = ResourceLoader.GetRunnerPixels(runnerName, i, out int w, out int h);
                if (pixels is null) continue;

                int offsetX = (maxWidth - w) / 2;
                int offsetY = (maxHeight - h) / 2;

                Int32Rect rect = new Int32Rect(i * maxWidth + offsetX, offsetY, w, h);
                spritesheet.WritePixels(rect, pixels, w * 4, 0);
            }

            spritesheet.Freeze();
            return Tuple.Create<BitmapSource, int, int>(spritesheet, maxWidth, maxHeight);
        }

        private void AnimationEngine_FrameChanged(object? sender, int frameIndex)
        {
            floatingWindow.SetFrame(frameIndex);
        }

        private void ExitApplication()
        {
            Dispose();
            Application.Current.Shutdown();
        }

        private void ShowBalloonTipIfNeeded()
        {
            if (config.FirstLaunch)
            {
                contextMenuManager.ShowBalloonTip(BalloonTipType.AppLaunched);
                config.FirstLaunch = false;
                config.Save();
            }
        }

        private void ChangeRunner(Runner r)
        {
            runner = r;
            config.Runner = runner.ToString();
            config.Save();
            RegenerateSpritesheet();
            floatingWindow.SetFrameMoveRatios(r);
        }

        private void ChangeTomatoClockDuration(int duration)
        {
            tomatoClockDuration = duration;
            tomatoClock.SetDuration(duration);
            config.TomatoClockDuration = duration;
            config.Save();
        }

        private void StartTomatoClock()
        {
            animationEngine.Reset();
            tomatoClock.Start();
        }

        private void PauseTomatoClock()
        {
            tomatoClock.Pause();
        }

        private void ResetTomatoClock()
        {
            animationEngine.Reset();
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

            int remainingMinutes = tomatoClock.RemainingSeconds / 60;
            int remainingSeconds = tomatoClock.RemainingSeconds % 60;
            return $"{Strings.SystemInfo_TomatoClock}: {remainingMinutes:D2}:{remainingSeconds:D2}";
        }

        private void UpdateTomatoClockInfo()
        {
            contextMenuManager.SetNotifyIconText(GetTomatoClockDescription());

            List<string> systemInfoValues = new List<string>();

            systemInfoValues.Add(GetTomatoClockDescription());
            if (tomatoClock.IsRunning)
            {
                systemInfoValues.Add($"Progress: {tomatoClock.GetProgress():P0}");
            }

            contextMenuManager.SetSystemInfoMenuText(string.Join("\n", systemInfoValues));
        }

        private void RegenerateSpritesheet()
        {
            try
            {
                Tuple<BitmapSource, int, int> spritesheetData = GenerateSpritesheet(runner);

                animationEngine.LoadSpritesheet(spritesheetData.Item1, spritesheetData.Item2, spritesheetData.Item3);
                floatingWindow.LoadSpritesheet(spritesheetData.Item1, spritesheetData.Item2, spritesheetData.Item3);
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            tomatoTimer.Stop();
            animationEngine.Dispose();
            floatingWindow.StopMoveTimer();
            animationEngine.FrameChanged -= AnimationEngine_FrameChanged;
            animationEngine.SpeedChanged -= floatingWindow.SetSpeed;
            tomatoClock.Dispose();
            contextMenuManager.Dispose();
        }
    }
}
