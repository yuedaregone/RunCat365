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
using System.IO;

namespace RunCat365
{
    internal class ContextMenuManager : IDisposable
    {
        private readonly CustomToolStripMenuItem systemInfoMenu = new();
        private readonly NotifyIcon notifyIcon = new();
        private readonly List<Icon> icons = [];
        private readonly Lock iconLock = new();
        private int current = 0;
        private bool tomatoClockCompleted = false;
        private float tomatoClockProgress = 0f;
        private float lastProgress = -1f; // Track last progress to avoid unnecessary updates

        internal ContextMenuManager(
            Func<Runner> getRunner,
            Action<Runner> setRunner,
            Func<Theme> getSystemTheme,
            Func<Theme> getManualTheme,
            Action<Theme> setManualTheme,
            Func<bool> getLaunchAtStartup,
            Func<bool, bool> toggleLaunchAtStartup,
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

            var launchAtStartupMenu = new CustomToolStripMenuItem(Strings.Menu_LaunchAtStartup)
            {
                Checked = getLaunchAtStartup()
            };
            launchAtStartupMenu.Click += (sender, e) => HandleStartupMenuClick(sender, toggleLaunchAtStartup);

            var settingsMenu = new CustomToolStripMenuItem(Strings.Menu_Settings);
            settingsMenu.DropDownItems.AddRange(
                themeMenu,
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
                new ToolStripSeparator(),
                exitMenu
            );
            contextMenuStrip.Renderer = new ContextMenuRenderer();

            SetIcons(getSystemTheme(), getManualTheme(), getRunner());

            // Set static tray icon from application icon
            var exePath = Environment.ProcessPath ?? AppDomain.CurrentDomain.BaseDirectory;
            notifyIcon.Icon = Icon.ExtractAssociatedIcon(exePath);

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
            var iconName = $"{runner.GetString()}_0".ToLower();
            var obj = Resources.ResourceManager.GetObject(iconName);
            if (obj is not Bitmap bitmap) return null;
            return systemTheme == Theme.Light ? bitmap.Recolor(Color.Black) : bitmap;
        }

        internal void SetIcons(Theme systemTheme, Theme manualTheme, Runner runner)
        {
            var theme = manualTheme == Theme.System ? systemTheme : manualTheme;
             
            // Calculate color based on tomato clock progress using HSV
            // Hue: blue (240°) at start -> red (0°/360°) at end
            // Saturation and Value: use lighter values for subtle overlay
            float hue = 240f * (1f - tomatoClockProgress); // 240° at progress=0, 0° at progress=1
            // Clamp hue to [0, 360)
            if (hue < 0f) hue += 360f;
            
            // Use lighter saturation and value for subtle color overlay
            float saturation = 0.3f;
            float value = 0.9f;
            Color finalColor = HsvToRgb(hue, saturation, value);
             
            var runnerName = runner.GetString();
            var rm = Resources.ResourceManager;
            var capacity = runner.GetFrameNumber();
            var list = new List<Icon>(capacity);
            for (int i = 0; i < capacity; i++)
            {
                var iconName = $"{runnerName}_{i}".ToLower();
                if (rm.GetObject(iconName) is not Bitmap bitmap) continue;
                if (theme == Theme.Light && tomatoClockProgress == 0f && !tomatoClockCompleted)
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
        
        /// <summary>
        /// Converts HSV color to RGB color.
        /// H: hue [0, 360)
        /// S: saturation [0, 1]
        /// V: value [0, 1]
        /// Returns Color with alpha=255
        /// </summary>
        private static Color HsvToRgb(float h, float s, float v)
        {
            // Clamp values
            h = Math.Clamp(h, 0f, 360f);
            s = Math.Clamp(s, 0f, 1f);
            v = Math.Clamp(v, 0f, 1f);
            
            float c = v * s; // Chroma
            float hPrime = h / 60f; // Hue sector
            float x = c * (1f - Math.Abs((hPrime % 2f) - 1f));
            float m = v - c;
            
            float rPrime, gPrime, bPrime;
            if (hPrime >= 0 && hPrime < 1)
            {
                rPrime = c;
                gPrime = x;
                bPrime = 0f;
            }
            else if (hPrime >= 1 && hPrime < 2)
            {
                rPrime = x;
                gPrime = c;
                bPrime = 0f;
            }
            else if (hPrime >= 2 && hPrime < 3)
            {
                rPrime = 0f;
                gPrime = c;
                bPrime = x;
            }
            else if (hPrime >= 3 && hPrime < 4)
            {
                rPrime = 0f;
                gPrime = x;
                bPrime = c;
            }
            else if (hPrime >= 4 && hPrime < 5)
            {
                rPrime = x;
                gPrime = 0f;
                bPrime = c;
            }
            else // hPrime >= 5 && hPrime < 6
            {
                rPrime = c;
                gPrime = 0f;
                bPrime = x;
            }
            
            int r = (int)((rPrime + m) * 255f);
            int g = (int)((gPrime + m) * 255f);
            int b = (int)((bPrime + m) * 255f);
            
            return Color.FromArgb(r, g, b);
        }

        internal void SetTomatoClockCompleted(bool completed)
        {
            tomatoClockCompleted = completed;
            if (completed)
            {
                lastProgress = 1.0f;
            }
        }

        internal void SetTomatoClockRedIntensity(float intensity)
        {
            tomatoClockProgress = intensity;
        }

        internal void ResetTomatoClockRedIntensity()
        {
            lastProgress = -1f;
            tomatoClockProgress = 0f;
        }

        internal bool ShouldUpdateIcons()
        {
            // Check if we need to update icons based on progress change
            if (tomatoClockCompleted)
            {
                return lastProgress != 1.0f;
            }
             
            float threshold = 0.05f; // Update if progress changes by more than 5%
            bool shouldUpdate = Math.Abs(tomatoClockProgress - lastProgress) > threshold;
             
            if (shouldUpdate)
            {
                lastProgress = tomatoClockProgress;
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

        internal void ShowBalloonTip(BalloonTipType balloonTipType)
        {
            var info = balloonTipType.GetInfo();
            notifyIcon.ShowBalloonTip(5000, info.Title, info.Text, info.Icon);
        }

        internal Icon? GetCurrentIcon()
        {
            lock (iconLock)
            {
                if (icons.Count == 0) return null;
                return icons[current];
            }
        }

        internal void AdvanceFrame()
        {
            lock (iconLock)
            {
                if (icons.Count == 0) return;
                if (icons.Count <= current) current = 0;
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
            }
        }

        private delegate bool CustomTryParseDelegate<T>(string? value, out T result);
    }
}
