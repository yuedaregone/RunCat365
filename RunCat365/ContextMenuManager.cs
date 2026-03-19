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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace RunCat365
{
    internal class ContextMenuManager : IDisposable
    {
        private readonly CustomToolStripMenuItem systemInfoMenu = new();
        private readonly NotifyIcon notifyIcon = new();
        private readonly List<Icon> trayIcons = [];
        private readonly Lock trayIconLock = new();
        private int currentTrayIcon = 0;
        private Runner currentRunner = Runner.Cat;

        internal ContextMenuManager(
            Func<Runner> getRunner,
            Action<Runner> setRunner,
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
                    SetTrayIcons(getRunner());
                },
                r => getRunner() == r,
                r => GetRunnerThumbnailBitmap(r)
            );

            var launchAtStartupMenu = new CustomToolStripMenuItem(Strings.Menu_LaunchAtStartup)
            {
                Checked = getLaunchAtStartup()
            };
            launchAtStartupMenu.Click += (sender, e) => HandleStartupMenuClick(sender, toggleLaunchAtStartup);

            var tomatoClockStartMenu = new CustomToolStripMenuItem("Start");
            tomatoClockStartMenu.Click += (sender, e) => startTomatoClock();
            
            var tomatoClockPauseMenu = new CustomToolStripMenuItem("Pause");
            tomatoClockPauseMenu.Click += (sender, e) => pauseTomatoClock();
            
            var tomatoClockResetMenu = new CustomToolStripMenuItem("Reset");
            tomatoClockResetMenu.Click += (sender, e) => resetTomatoClock();
            
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

            var settingsMenu = new CustomToolStripMenuItem(Strings.Menu_Settings);
            settingsMenu.DropDownItems.AddRange(
                launchAtStartupMenu,
                new ToolStripSeparator(),
                tomatoClockStartMenu,
                tomatoClockPauseMenu,
                tomatoClockResetMenu,
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
                new ToolStripSeparator(),
                exitMenu
            );
            contextMenuStrip.Renderer = new ContextMenuRenderer();

            SetTrayIcons(getRunner());

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

        private static Bitmap? GetRunnerThumbnailBitmap(Runner runner)
        {
            var iconName = $"{runner.GetString()}_0".ToLower();
            var obj = Resources.ResourceManager.GetObject(iconName);
            if (obj is not Bitmap bitmap) return null;
            return bitmap;
        }

        internal void SetTrayIcons(Runner runner)
        {
            var runnerName = runner.GetString();
            var capacity = runner.GetFrameNumber();
            var list = new List<Icon>(capacity);
            for (int i = 0; i < capacity; i++)
            {
                var iconName = $"{runnerName}_{i}".ToLower();
                if (Resources.ResourceManager.GetObject(iconName) is not Bitmap bitmap) continue;
                list.Add(BitmapToIcon(bitmap));
            }

            lock (trayIconLock)
            {
                trayIcons.Clear();
                trayIcons.AddRange(list);
                currentTrayIcon = 0;
            }

            currentRunner = runner;
        }

        internal (int frameWidth, int frameHeight) GetFrameDimensions(Runner runner)
        {
            var runnerName = runner.GetString();
            var iconName = $"{runnerName}_0".ToLower();
            var obj = Resources.ResourceManager.GetObject(iconName);
            if (obj is not Bitmap bitmap) return (48, 48);
            return (bitmap.Width, bitmap.Height);
        }

        private static Icon BitmapToIcon(Bitmap bitmap)
        {
            using var pngStream = new MemoryStream();
            bitmap.Save(pngStream, ImageFormat.Png);
            var pngData = pngStream.ToArray();

            using var icoStream = new MemoryStream();
            using var bw = new BinaryWriter(icoStream);

            bw.Write((short)0);
            bw.Write((short)1);
            bw.Write((short)1);

            bw.Write((byte)(bitmap.Width >= 256 ? 0 : bitmap.Width));
            bw.Write((byte)(bitmap.Height >= 256 ? 0 : bitmap.Height));
            bw.Write((byte)0);
            bw.Write((byte)0);
            bw.Write((short)1);
            bw.Write((short)32);
            bw.Write(pngData.Length);
            bw.Write(22);

            bw.Write(pngData);

            icoStream.Position = 0;
            return new Icon(icoStream);
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
                System.Windows.Forms.MessageBox.Show(ex.Message, Strings.Message_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        internal void ShowBalloonTip(BalloonTipType balloonTipType)
        {
            var info = balloonTipType.GetInfo();
            notifyIcon.ShowBalloonTip(5000, info.Title, info.Text, info.Icon);
        }

        internal Icon? GetCurrentTrayIcon()
        {
            lock (trayIconLock)
            {
                if (trayIcons.Count == 0) return null;
                return trayIcons[currentTrayIcon];
            }
        }

        internal void AdvanceTrayIcon()
        {
            lock (trayIconLock)
            {
                if (trayIcons.Count == 0) return;
                if (trayIcons.Count <= currentTrayIcon) currentTrayIcon = 0;
                currentTrayIcon = (currentTrayIcon + 1) % trayIcons.Count;
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
                lock (trayIconLock)
                {
                    trayIcons.Clear();
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
