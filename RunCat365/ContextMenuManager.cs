// Copyright 2025 Takuto Nakamura
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
using System.ComponentModel;

namespace RunCat365
{
    internal class ContextMenuManager : IDisposable
    {
        private readonly CustomToolStripMenuItem systemInfoMenu = new();
        private readonly NotifyIcon notifyIcon = new();
        private readonly List<Icon> icons = [];
        private readonly Lock iconLock = new();
        private int current = 0;
        private EndlessGameForm? endlessGameForm;
        private bool tomatoClockCompleted = false;
        private float tomatoClockRedIntensity = 0f;
        private float lastRedIntensity = -1f; // Track last intensity to avoid unnecessary updates

        internal ContextMenuManager(
            Func<Runner> getRunner,
            Action<Runner> setRunner,
            Func<Theme> getSystemTheme,
            Func<Theme> getManualTheme,
            Action<Theme> setManualTheme,
            Func<SpeedSource> getSpeedSource,
            Action<SpeedSource> setSpeedSource,
            Func<SpeedSource, bool> isSpeedSourceAvailable,
            Func<FPSMaxLimit> getFPSMaxLimit,
            Action<FPSMaxLimit> setFPSMaxLimit,
            Func<bool> getLaunchAtStartup,
            Func<bool, bool> toggleLaunchAtStartup,
            Action openRepository,
            Action onExit,
            Func<int> getTomatoClockDuration,
            Action<int> setTomatoClockDuration,
            Func<bool> isTomatoClockRunning,
            Action startTomatoClock,
            Action pauseTomatoClock,
            Action resetTomatoClock
        )
        {
            systemInfoMenu.Text = "-\n-\n-\n-\n-";
            systemInfoMenu.Enabled = false;

            var runnersMenu = new CustomToolStripMenuItem(Strings.Menu_Runner);
            runnersMenu.SetupSubMenusFromEnum<Runner>(
                r => r.GetLocalizedString(),
                (parent, sender, e) =>
                {
                    HandleMenuItemSelection<Runner>(
                        parent,
                        sender,
                        (string? s, out Runner r) => Enum.TryParse(s, out r),
                        r => setRunner(r)
                    );
                    SetIcons(getSystemTheme(), getManualTheme(), getRunner());
                },
                r => getRunner() == r,
                r => GetRunnerThumbnailBitmap(getSystemTheme(), r)
            );

            var themeMenu = new CustomToolStripMenuItem(Strings.Menu_Theme);
            themeMenu.SetupSubMenusFromEnum<Theme>(
                t => t.GetLocalizedString(),
                (parent, sender, e) =>
                {
                    HandleMenuItemSelection<Theme>(
                        parent,
                        sender,
                        (string? s, out Theme t) => Enum.TryParse(s, out t),
                        t => setManualTheme(t)
                    );
                    SetIcons(getSystemTheme(), getManualTheme(), getRunner());
                },
                t => getManualTheme() == t,
                _ => null
            );

            var speedSourceMenu = new CustomToolStripMenuItem(Strings.Menu_SpeedSource);
            speedSourceMenu.SetupSubMenusFromEnum<SpeedSource>(
                s => s.GetLocalizedString(),
                (parent, sender, e) =>
                {
                    HandleMenuItemSelection<SpeedSource>(
                        parent,
                        sender,
                        (string? s, out SpeedSource ss) => Enum.TryParse(s, out ss),
                        s => setSpeedSource(s)
                    );
                },
                s => getSpeedSource() == s,
                _ => null,
                isSpeedSourceAvailable
            );

            var fpsMaxLimitMenu = new CustomToolStripMenuItem(Strings.Menu_FPSMaxLimit);
            fpsMaxLimitMenu.SetupSubMenusFromEnum<FPSMaxLimit>(
                f => f.GetString(),
                (parent, sender, e) =>
                {
                    HandleMenuItemSelection<FPSMaxLimit>(
                        parent,
                        sender,
                        (string? s, out FPSMaxLimit f) => FPSMaxLimitExtension.TryParse(s, out f),
                        f => setFPSMaxLimit(f)
                    );
                },
                f => getFPSMaxLimit() == f,
                _ => null
            );

            var launchAtStartupMenu = new CustomToolStripMenuItem(Strings.Menu_LaunchAtStartup)
            {
                Checked = getLaunchAtStartup()
            };
            launchAtStartupMenu.Click += (sender, e) => HandleStartupMenuClick(sender, toggleLaunchAtStartup);

            var settingsMenu = new CustomToolStripMenuItem(Strings.Menu_Settings);
            settingsMenu.DropDownItems.AddRange(
                themeMenu,
                speedSourceMenu,
                fpsMaxLimitMenu,
                launchAtStartupMenu
            );

            // Tomato Clock Menu
            var tomatoClockMenu = new CustomToolStripMenuItem("Tomato Clock");
            
            var tomatoClockStartMenu = new CustomToolStripMenuItem("Start");
            tomatoClockStartMenu.Click += (sender, e) => startTomatoClock();
            
            var tomatoClockPauseMenu = new CustomToolStripMenuItem("Pause");
            tomatoClockPauseMenu.Click += (sender, e) => pauseTomatoClock();
            
            var tomatoClockResetMenu = new CustomToolStripMenuItem("Reset");
            tomatoClockResetMenu.Click += (sender, e) => resetTomatoClock();
            
            // Duration submenu
            var tomatoClockDurationMenu = new CustomToolStripMenuItem("Duration (minutes)");
            var durations = new[] { 15, 20, 25, 30, 45, 60 };
            foreach (var duration in durations)
            {
                var durationItem = new CustomToolStripMenuItem($"{duration} min");
                durationItem.Click += (sender, e) =>
                {
                    setTomatoClockDuration(duration);
                };
                tomatoClockDurationMenu.DropDownItems.Add(durationItem);
            }

            tomatoClockMenu.DropDownItems.AddRange(
                tomatoClockStartMenu,
                tomatoClockPauseMenu,
                tomatoClockResetMenu,
                new ToolStripSeparator(),
                tomatoClockDurationMenu
            );

            var endlessGameMenu = new CustomToolStripMenuItem(Strings.Menu_EndlessGame);
            endlessGameMenu.Click += (sender, e) => ShowOrActivateGameWindow(getSystemTheme);

            var appVersionMenu = new CustomToolStripMenuItem(
                $"{Application.ProductName} v{Application.ProductVersion}"
            )
            {
                Enabled = false
            };

            var repositoryMenu = new CustomToolStripMenuItem(Strings.Menu_OpenRepository);
            repositoryMenu.Click += (sender, e) => openRepository();

            var informationMenu = new CustomToolStripMenuItem(Strings.Menu_Information);
            informationMenu.DropDownItems.AddRange(
                appVersionMenu,
                repositoryMenu
            );

            var exitMenu = new CustomToolStripMenuItem(Strings.Menu_Exit);
            exitMenu.Click += (sender, e) => onExit();

            var contextMenuStrip = new ContextMenuStrip(new Container());
            contextMenuStrip.Items.AddRange(
                systemInfoMenu,
                new ToolStripSeparator(),
                runnersMenu,
                new ToolStripSeparator(),
                settingsMenu,
                tomatoClockMenu,
                informationMenu,
                endlessGameMenu,
                new ToolStripSeparator(),
                exitMenu
            );
            contextMenuStrip.Renderer = new ContextMenuRenderer();

            SetIcons(getSystemTheme(), getManualTheme(), getRunner());

            notifyIcon.Visible = true;
            notifyIcon.ContextMenuStrip = contextMenuStrip;
        }

        private static void HandleMenuItemSelection<T>(
            ToolStripMenuItem parentMenu,
            object? sender,
            CustomTryParseDelegate<T> tryParseMethod,
            Action<T> assignValueAction
        )
        {
            if (sender is null) return;
            var item = (ToolStripMenuItem)sender;
            foreach (ToolStripMenuItem childItem in parentMenu.DropDownItems)
            {
                childItem.Checked = false;
            }
            item.Checked = true;

            if (item.Tag is T tagValue)
            {
                assignValueAction(tagValue);
            }
            else if (tryParseMethod(item.Text, out T parsedValue))
            {
                assignValueAction(parsedValue);
            }
        }

        private static Bitmap? GetRunnerThumbnailBitmap(Theme systemTheme, Runner runner)
        {
            var color = systemTheme.GetContrastColor();
            var iconName = $"{runner.GetString()}_0".ToLower();
            var obj = Resources.ResourceManager.GetObject(iconName);
            if (obj is not Bitmap bitmap) return null;
            return systemTheme == Theme.Light ? bitmap : bitmap.Recolor(color);
        }

        internal void SetIcons(Theme systemTheme, Theme manualTheme, Runner runner)
        {
            var theme = manualTheme == Theme.System ? systemTheme : manualTheme;
            var baseColor = theme.GetContrastColor();
            
            // Blend red color based on intensity
            Color finalColor;
            if (tomatoClockCompleted)
            {
                finalColor = Color.Red;
            }
            else if (tomatoClockRedIntensity > 0)
            {
                // Blend between base color and deep red
                // Use deep red (dark red) instead of pure red for better visibility
                Color deepRed = Color.FromArgb(180, 50, 50); // Deep red with some darkness
                finalColor = BlendColors(baseColor, deepRed, tomatoClockRedIntensity);
            }
            else
            {
                finalColor = baseColor;
            }
            
            var runnerName = runner.GetString();
            var rm = Resources.ResourceManager;
            var capacity = runner.GetFrameNumber();
            var list = new List<Icon>(capacity);
            for (int i = 0; i < capacity; i++)
            {
                var iconName = $"{runnerName}_{i}".ToLower();
                if (rm.GetObject(iconName) is not Bitmap bitmap) continue;
                if (theme == Theme.Light && tomatoClockRedIntensity == 0 && !tomatoClockCompleted)
                {
                    list.Add(bitmap.ToIcon());
                }
                else
                {
                    using var recolored = bitmap.Recolor(finalColor);
                    list.Add(recolored.ToIcon());
                }
            }

            lock (iconLock)
            {
                icons.Clear();
                icons.AddRange(list);
                current = 0;
            }
        }

        private static Color BlendColors(Color color1, Color color2, float ratio)
        {
            // Clamp ratio to 0-1
            ratio = Math.Clamp(ratio, 0f, 1f);
            
            // Linear interpolation between two colors
            int r = (int)(color1.R * (1 - ratio) + color2.R * ratio);
            int g = (int)(color1.G * (1 - ratio) + color2.G * ratio);
            int b = (int)(color1.B * (1 - ratio) + color2.B * ratio);
            
            return Color.FromArgb(r, g, b);
        }

        internal void SetTomatoClockCompleted(bool completed)
        {
            tomatoClockCompleted = completed;
            if (completed)
            {
                lastRedIntensity = 1.0f;
            }
        }

        internal void SetTomatoClockRedIntensity(float intensity)
        {
            tomatoClockRedIntensity = intensity;
        }

        internal void ResetTomatoClockRedIntensity()
        {
            lastRedIntensity = -1f;
            tomatoClockRedIntensity = 0f;
        }

        internal bool ShouldUpdateIcons()
        {
            // Check if we need to update icons based on red intensity change
            if (tomatoClockCompleted)
            {
                return lastRedIntensity != 1.0f;
            }
            
            float threshold = 0.05f; // Update if intensity changes by more than 5%
            bool shouldUpdate = Math.Abs(tomatoClockRedIntensity - lastRedIntensity) > threshold;
            
            if (shouldUpdate)
            {
                lastRedIntensity = tomatoClockRedIntensity;
            }
            
            return shouldUpdate;
        }

        private static void HandleStartupMenuClick(object? sender, Func<bool, bool> toggleLaunchAtStartup)
        {
            if (sender is null) return;
            var item = (ToolStripMenuItem)sender;
            try
            {
                if (toggleLaunchAtStartup(item.Checked))
                {
                    item.Checked = !item.Checked;
                }
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, Strings.Message_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

        }

        private void ShowOrActivateGameWindow(Func<Theme> getSystemTheme)
        {
            if (endlessGameForm is null)
            {
                endlessGameForm = new EndlessGameForm(getSystemTheme());
                endlessGameForm.FormClosed += (sender, e) =>
                {
                    endlessGameForm = null;
                };
                endlessGameForm.Show();
            }
            else
            {
                endlessGameForm.Activate();
            }
        }

        internal void ShowBalloonTip(BalloonTipType balloonTipType)
        {
            var info = balloonTipType.GetInfo();
            notifyIcon.ShowBalloonTip(5000, info.Title, info.Text, info.Icon);
        }

        internal void AdvanceFrame()
        {
            lock (iconLock)
            {
                if (icons.Count == 0) return;
                if (icons.Count <= current) current = 0;
                notifyIcon.Icon = icons[current];
                current = (current + 1) % icons.Count;
            }
        }

        internal void SetSystemInfoMenuText(string text)
        {
            systemInfoMenu.Text = text;
        }

        internal void SetNotifyIconText(string text)
        {
            notifyIcon.Text = text;
        }

        internal void HideNotifyIcon()
        {
            notifyIcon.Visible = false;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (iconLock)
                {
                    icons.Clear();
                }

                if (notifyIcon is not null)
                {
                    notifyIcon.ContextMenuStrip?.Dispose();
                    notifyIcon.Dispose();
                }

                endlessGameForm?.Dispose();
            }
        }

        private delegate bool CustomTryParseDelegate<T>(string? value, out T result);
    }
}
