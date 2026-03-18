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

using Microsoft.Win32;
using RunCat365.Properties;
using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using FormsTimer = System.Windows.Forms.Timer;

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

            // Terminate RunCat365 if there's any existing instance.
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
        private const int ANIMATE_TIMER_DEFAULT_INTERVAL = 200;
        private readonly LaunchAtStartupManager launchAtStartupManager;
        private readonly ContextMenuManager contextMenuManager;
        private readonly FloatingWindow floatingWindow;
        private readonly FormsTimer animateTimer;
        private readonly TomatoClock tomatoClock;
        private Runner runner = Runner.Cat;
        private Theme manualTheme = Theme.System;
        private int tomatoClockDuration = 25;

        public RunCat365ApplicationContext()
        {
            UserSettings.Default.Reload();
            _ = Enum.TryParse(UserSettings.Default.Runner, out runner);
            _ = Enum.TryParse(UserSettings.Default.Theme, out manualTheme);
            
            if (UserSettings.Default.TomatoClockDuration > 0)
            {
                tomatoClockDuration = UserSettings.Default.TomatoClockDuration;
            }

            SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(UserPreferenceChanged);

            launchAtStartupManager = new LaunchAtStartupManager();
            
            tomatoClock = new TomatoClock();
            tomatoClock.SetDuration(tomatoClockDuration);
            tomatoClock.Tick += TomatoClock_Tick;
            tomatoClock.Completed += TomatoClock_Completed;

            contextMenuManager = new ContextMenuManager(
                () => runner,
                r => ChangeRunner(r),
                () => GetSystemTheme(),
                () => manualTheme,
                t => ChangeManualTheme(t),
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

            animateTimer = new FormsTimer
            {
                Interval = ANIMATE_TIMER_DEFAULT_INTERVAL
            };
            animateTimer.Tick += AnimationTick;
            animateTimer.Start();

            floatingWindow = new FloatingWindow();
            floatingWindow.Show();

            tomatoClock.Start();

            ShowBalloonTipIfNeeded();
        }

        private void ExitApplication()
        {
            Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        private static Theme GetSystemTheme()
        {
            var keyName = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            using var rKey = Registry.CurrentUser.OpenSubKey(keyName);
            if (rKey is null) return Theme.Light;
            var value = rKey.GetValue("SystemUsesLightTheme");
            if (value is null) return Theme.Light;
            return (int)value == 0 ? Theme.Dark : Theme.Light;
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

        private void UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                var systemTheme = GetSystemTheme();
                contextMenuManager.SetIcons(systemTheme, manualTheme, runner);
            }
        }

        private void ChangeRunner(Runner r)
        {
            runner = r;
            UserSettings.Default.Runner = runner.ToString();
            UserSettings.Default.Save();
        }

        private void ChangeManualTheme(Theme t)
        {
            manualTheme = t;
            UserSettings.Default.Theme = manualTheme.ToString();
            UserSettings.Default.Save();
        }

        private void ChangeTomatoClockDuration(int duration)
        {
            tomatoClockDuration = duration;
            tomatoClock.SetDuration(duration);
            UserSettings.Default.TomatoClockDuration = duration;
            UserSettings.Default.Save();
        }

        private void AnimationTick(object? sender, EventArgs e)
        {
            contextMenuManager.AdvanceFrame();
            var icon = contextMenuManager.GetCurrentIcon();
            if (icon is not null)
            {
                floatingWindow.UpdateIcon(icon);
            }
        }

        private void TomatoClock_Tick(object? sender, EventArgs e)
        {
            UpdateTomatoClockInfo();
        }

        private void StartTomatoClock()
        {
            contextMenuManager.SetTomatoClockCompleted(false);
            contextMenuManager.ResetTomatoClockRedIntensity();
            contextMenuManager.SetIcons(GetSystemTheme(), manualTheme, runner);
            tomatoClock.Start();
        }

        private void PauseTomatoClock()
        {
            tomatoClock.Pause();
        }

        private void ResetTomatoClock()
        {
            contextMenuManager.SetTomatoClockCompleted(false);
            contextMenuManager.ResetTomatoClockRedIntensity();
            contextMenuManager.SetIcons(GetSystemTheme(), manualTheme, runner);
            tomatoClock.Reset();
        }

        private void TomatoClock_Completed(object? sender, EventArgs e)
        {
            contextMenuManager.SetTomatoClockCompleted(true);
            contextMenuManager.SetIcons(GetSystemTheme(), manualTheme, runner);
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

        private int CalculateInterval()
        {
            float progress = tomatoClock.GetProgress();
            float minInterval = 25f;
            float maxInterval = 500f;
            float interval = maxInterval - (progress * (maxInterval - minInterval));
            return (int)interval;
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

            float redIntensity = tomatoClock.GetProgress();
            contextMenuManager.SetTomatoClockRedIntensity(redIntensity);

            int interval = CalculateInterval();
            animateTimer.Interval = interval;

            if (contextMenuManager.ShouldUpdateIcons())
            {
                contextMenuManager.SetIcons(GetSystemTheme(), manualTheme, runner);
            }

            contextMenuManager.SetSystemInfoMenuText(string.Join("\n", systemInfoValues));
        }

        public void Dispose()
        {
            SystemEvents.UserPreferenceChanged -= UserPreferenceChanged;

            animateTimer?.Stop();
            animateTimer?.Dispose();
            tomatoClock?.Dispose();

            floatingWindow?.Close();

            contextMenuManager?.HideNotifyIcon();
            contextMenuManager?.Dispose();
        }
    }
}
