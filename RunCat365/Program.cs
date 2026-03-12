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
using System.Diagnostics;
using System.Globalization;
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
                ApplicationConfiguration.Initialize();
                Application.SetColorMode(SystemColorMode.System);
                Application.Run(new RunCat365ApplicationContext());
            }
            finally
            {
                procMutex?.ReleaseMutex();
            }
        }
    }

    internal class RunCat365ApplicationContext : ApplicationContext
    {
        private const int FETCH_TIMER_DEFAULT_INTERVAL = 1000;
        private const int FETCH_COUNTER_SIZE = 5;
        private const int ANIMATE_TIMER_DEFAULT_INTERVAL = 200;
        private readonly CPURepository cpuRepository;
        private readonly GPURepository gpuRepository;
        private readonly MemoryRepository memoryRepository;
        private readonly StorageRepository storageRepository;
        private readonly NetworkRepository networkRepository;
        private readonly LaunchAtStartupManager launchAtStartupManager;
        private readonly ContextMenuManager contextMenuManager;
        private readonly FormsTimer fetchTimer;
        private readonly FormsTimer animateTimer;
        private Runner runner = Runner.Cat;
        private Theme manualTheme = Theme.System;
        private FPSMaxLimit fpsMaxLimit = FPSMaxLimit.FPS40;
        private SpeedSource speedSource = SpeedSource.CPU;
        private int fetchCounter = 5;
        private readonly TomatoClock tomatoClock;
        private int tomatoClockDuration = 25;

        public RunCat365ApplicationContext()
        {
            UserSettings.Default.Reload();
            _ = Enum.TryParse(UserSettings.Default.Runner, out runner);
            _ = Enum.TryParse(UserSettings.Default.Theme, out manualTheme);
            _ = Enum.TryParse(UserSettings.Default.FPSMaxLimit, out fpsMaxLimit);
            _ = Enum.TryParse(UserSettings.Default.SpeedSource, out speedSource);
            
            // Load tomato clock duration
            if (UserSettings.Default.TomatoClockDuration > 0)
            {
                tomatoClockDuration = UserSettings.Default.TomatoClockDuration;
            }

            SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(UserPreferenceChanged);

            cpuRepository = new CPURepository();
            gpuRepository = new GPURepository();
            memoryRepository = new MemoryRepository();
            storageRepository = new StorageRepository();
            networkRepository = new NetworkRepository();
            launchAtStartupManager = new LaunchAtStartupManager();
            
            tomatoClock = new TomatoClock();
            tomatoClock.SetDuration(tomatoClockDuration);
            tomatoClock.Tick += TomatoClock_Tick;
            tomatoClock.Completed += TomatoClock_Completed;

            ResolveSpeedSource();

            contextMenuManager = new ContextMenuManager(
                () => runner,
                r => ChangeRunner(r),
                () => GetSystemTheme(),
                () => manualTheme,
                t => ChangeManualTheme(t),
                () => speedSource,
                s => ChangeSpeedSource(s),
                s => IsSpeedSourceAvailable(s),
                () => fpsMaxLimit,
                f => ChangeFPSMaxLimit(f),
                () => launchAtStartupManager.GetStartup(),
                s => launchAtStartupManager.SetStartup(s),
                () => OpenRepository(),
                () => Application.Exit(),
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
            animateTimer.Tick += new EventHandler(AnimationTick);
            animateTimer.Start();

            fetchTimer = new FormsTimer
            {
                Interval = FETCH_TIMER_DEFAULT_INTERVAL
            };
            fetchTimer.Tick += new EventHandler(FetchTick);
            fetchTimer.Start();

            ShowBalloonTipIfNeeded();
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

        private bool IsSpeedSourceAvailable(SpeedSource speedSource)
        {
            return speedSource switch
            {
                SpeedSource.CPU => true,
                SpeedSource.GPU => gpuRepository.IsAvailable,
                SpeedSource.Memory => true,
                SpeedSource.TomatoClock => true,
                _ => false,
            };
        }

        private void ResolveSpeedSource()
        {
            if (!IsSpeedSourceAvailable(speedSource))
            {
                ChangeSpeedSource(SpeedSource.CPU);
            }
        }

        private void ShowBalloonTipIfNeeded()
        {
            if (!cpuRepository.IsAvailable)
            {
                contextMenuManager.ShowBalloonTip(BalloonTipType.CPUInfoUnavailable);
            }
            else if (UserSettings.Default.FirstLaunch)
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

        private static void OpenRepository()
        {
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "https://github.com/Kyome22/RunCat365.git",
                    UseShellExecute = true
                });
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
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

        private void ChangeSpeedSource(SpeedSource s)
        {
            speedSource = s;
            UserSettings.Default.SpeedSource = speedSource.ToString();
            UserSettings.Default.Save();
        }

        private void ChangeFPSMaxLimit(FPSMaxLimit f)
        {
            fpsMaxLimit = f;
            UserSettings.Default.FPSMaxLimit = fpsMaxLimit.ToString();
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
        }

        private void TomatoClock_Tick(object? sender, EventArgs e)
        {
            if (speedSource == SpeedSource.TomatoClock)
            {
                FetchSystemInfo();
            }
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
            // Change icon color to red when tomato clock completes
            contextMenuManager.SetTomatoClockCompleted(true);
            
            // Refresh icons with red color
            contextMenuManager.SetIcons(GetSystemTheme(), manualTheme, runner);
            
            // Show notification
            contextMenuManager.ShowBalloonTip(BalloonTipType.AppLaunched); // Reuse existing balloon tip
        }

        private string GetInfoDescription(CPUInfo cpuInfo, GPUInfo? gpuInfo, MemoryInfo memoryInfo)
        {
            return speedSource switch
            {
                SpeedSource.CPU => cpuInfo.GetDescription(),
                SpeedSource.GPU => gpuInfo?.GetDescription() ?? "",
                SpeedSource.Memory => memoryInfo.GetDescription(),
                SpeedSource.TomatoClock => GetTomatoClockDescription(),
                _ => "",
            };
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

        private int CalculateInterval(CPUInfo cpuInfo, GPUInfo? gpuInfo, MemoryInfo memoryInfo)
        {
            var load = speedSource switch
            {
                SpeedSource.CPU => cpuInfo.Total,
                SpeedSource.GPU => gpuInfo?.Maximum ?? 0f,
                SpeedSource.Memory => memoryInfo.MemoryLoad,
                SpeedSource.TomatoClock => tomatoClock.GetProgress() * 100f,
                _ => 0f,
            };
            
            if (speedSource == SpeedSource.TomatoClock)
            {
                // Uniform speed increase: linear interpolation from 500ms to 25ms
                // progress = 0 -> interval = 500ms (slowest)
                // progress = 1 -> interval = 25ms (fastest)
                float progress = tomatoClock.GetProgress();
                float minInterval = 25f;
                float maxInterval = 500f;
                float interval = maxInterval - (progress * (maxInterval - minInterval));
                return (int)interval;
            }
            else
            {
                var speed = (float)Math.Max(1.0f, (load / 5.0f) * fpsMaxLimit.GetRate());
                return (int)(500.0f / speed);
            }
        }

        private int FetchSystemInfo()
        {
            var cpuInfo = cpuRepository.Get();
            var gpuInfo = gpuRepository.Get();
            var memoryInfo = memoryRepository.Get();
            var storageInfo = storageRepository.Get();
            var networkInfo = networkRepository.Get();

            contextMenuManager.SetNotifyIconText(GetInfoDescription(cpuInfo, gpuInfo, memoryInfo));

            var systemInfoValues = new List<string>();
            
            if (speedSource == SpeedSource.TomatoClock)
            {
                // Show tomato clock info instead of system info
                systemInfoValues.Add(GetTomatoClockDescription());
                if (tomatoClock.IsRunning)
                {
                    systemInfoValues.Add($"Progress: {tomatoClock.GetProgress():P0}");
                }
                
                // Update red intensity based on tomato clock progress
                float redIntensity = tomatoClock.GetRedIntensity();
                contextMenuManager.SetTomatoClockRedIntensity(redIntensity);
                
                // Refresh icons only when intensity changes significantly
                // This ensures the color transition is smooth but efficient
                if (contextMenuManager.ShouldUpdateIcons())
                {
                    contextMenuManager.SetIcons(GetSystemTheme(), manualTheme, runner);
                }
            }
            else
            {
                systemInfoValues.AddRange(cpuInfo.GenerateIndicator());
                if (gpuInfo.HasValue)
                {
                    systemInfoValues.AddRange(gpuInfo.Value.GenerateIndicator());
                }
                systemInfoValues.AddRange(memoryInfo.GenerateIndicator());
                systemInfoValues.AddRange(storageInfo.GenerateIndicator());
                systemInfoValues.AddRange(networkInfo.GenerateIndicator());
            }
            
            contextMenuManager.SetSystemInfoMenuText(string.Join("\n", [.. systemInfoValues]));

            return CalculateInterval(cpuInfo, gpuInfo, memoryInfo);
        }

        private void FetchTick(object? state, EventArgs e)
        {
            cpuRepository.Update();
            gpuRepository.Update();
            fetchCounter += 1;
            if (fetchCounter < FETCH_COUNTER_SIZE) return;
            fetchCounter = 0;
            var interval = FetchSystemInfo();
            animateTimer.Stop();
            animateTimer.Interval = interval;
            animateTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SystemEvents.UserPreferenceChanged -= UserPreferenceChanged;

                animateTimer?.Stop();
                animateTimer?.Dispose();
                fetchTimer?.Stop();
                fetchTimer?.Dispose();

                cpuRepository?.Close();

                contextMenuManager?.HideNotifyIcon();
                contextMenuManager?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
