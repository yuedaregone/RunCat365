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
using System.Windows;
using System.Windows.Media.Imaging;

namespace RunCat365
{
    internal class ContextMenuManager : IDisposable
    {
        private readonly CustomToolStripMenuItem systemInfoMenu = new();
        private readonly NotifyIcon notifyIcon = new();
        private readonly List<Icon> trayIcons = [];
        private readonly Lock trayIconLock = new();
        private int currentTrayIcon = 0;
        private bool tomatoClockCompleted = false;
        private float tomatoClockProgress = 0f;
        private float lastProgress = -1f;
        private Runner currentRunner = Runner.Cat;
        private Theme currentTheme = Theme.Light;

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
                    SetTrayIcons(getSystemTheme(), getManualTheme(), getRunner());
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
                    SetTrayIcons(getSystemTheme(), getManualTheme(), getRunner());
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

            var tomatoClockMenu = new CustomToolStripMenuItem("Tomato Clock");
            
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

            SetTrayIcons(getSystemTheme(), getManualTheme(), getRunner());

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
            return systemTheme == Theme.Light ? RecolorBitmap(bitmap, Color.Black) : bitmap;
        }

        internal void SetTrayIcons(Theme systemTheme, Theme manualTheme, Runner runner)
        {
            var theme = manualTheme == Theme.System ? systemTheme : manualTheme;
            Color finalColor = CalculateProgressColor(tomatoClockProgress);
            
            var runnerName = runner.GetString();
            var capacity = runner.GetFrameNumber();
            var list = new List<Icon>(capacity);
            for (int i = 0; i < capacity; i++)
            {
                var iconName = $"{runnerName}_{i}".ToLower();
                if (Resources.ResourceManager.GetObject(iconName) is not Bitmap bitmap) continue;
                if (theme == Theme.Light && tomatoClockProgress == 0f && !tomatoClockCompleted)
                {
                    list.Add(BitmapToIcon(bitmap));
                }
                else
                {
                    var recolored = RecolorBitmap(bitmap, finalColor);
                    list.Add(BitmapToIcon(recolored));
                }
            }

            lock (trayIconLock)
            {
                trayIcons.Clear();
                trayIcons.AddRange(list);
                currentTrayIcon = 0;
            }

            currentRunner = runner;
            currentTheme = theme;
        }

        internal BitmapSource GenerateSpritesheet(Theme systemTheme, Theme manualTheme, Runner runner, float progress)
        {
            var theme = manualTheme == Theme.System ? systemTheme : manualTheme;
            Color finalColor = CalculateProgressColor(progress);
            
            var runnerName = runner.GetString();
            var frameCount = runner.GetFrameNumber();
            
            var firstIconName = $"{runnerName}_0".ToLower();
            var firstFrame = Resources.ResourceManager.GetObject(firstIconName);
            if (firstFrame is not Bitmap firstBitmap)
            {
                throw new InvalidOperationException($"Failed to load first frame: {firstIconName}");
            }

            int frameWidth = firstBitmap.Width;
            int frameHeight = firstBitmap.Height;

            var spritesheet = new WriteableBitmap(frameCount * frameWidth, frameHeight, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);

            for (int i = 0; i < frameCount; i++)
            {
                var iconName = $"{runnerName}_{i}".ToLower();
                var frame = Resources.ResourceManager.GetObject(iconName);
                if (frame is not Bitmap bitmap) continue;

                Bitmap processedFrame;
                if (theme == Theme.Light && progress == 0f && !tomatoClockCompleted)
                {
                    processedFrame = bitmap;
                }
                else
                {
                    processedFrame = RecolorBitmap(bitmap, finalColor);
                }

                var rect = new Int32Rect(i * frameWidth, 0, frameWidth, frameHeight);
                var bitmapData = processedFrame.LockBits(
                    new Rectangle(0, 0, frameWidth, frameHeight),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format32bppArgb);

                try
                {
                    var stride = bitmapData.Stride;
                    var pixels = new byte[stride * frameHeight];
                    System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, pixels, 0, pixels.Length);
                    spritesheet.WritePixels(rect, pixels, stride, 0);
                }
                finally
                {
                    processedFrame.UnlockBits(bitmapData);
                    if (processedFrame != bitmap)
                    {
                        processedFrame.Dispose();
                    }
                }
            }

            spritesheet.Freeze();
            return spritesheet;
        }

        internal (int frameWidth, int frameHeight) GetFrameDimensions(Runner runner)
        {
            var runnerName = runner.GetString();
            var iconName = $"{runnerName}_0".ToLower();
            var obj = Resources.ResourceManager.GetObject(iconName);
            if (obj is not Bitmap bitmap) return (48, 48);
            return (bitmap.Width, bitmap.Height);
        }

        private static Color CalculateProgressColor(float progress)
        {
            float hue = 240f * (1f - progress);
            if (hue < 0f) hue += 360f;

            float saturation = 0.3f;
            float value = 0.9f;

            float c = value * saturation;
            float hPrime = hue / 60f;
            float x = c * (1f - Math.Abs((hPrime % 2f) - 1f));
            float m = value - c;

            float rPrime, gPrime, bPrime;
            if (hPrime >= 0 && hPrime < 1)
            {
                rPrime = c; gPrime = x; bPrime = 0f;
            }
            else if (hPrime >= 1 && hPrime < 2)
            {
                rPrime = x; gPrime = c; bPrime = 0f;
            }
            else if (hPrime >= 2 && hPrime < 3)
            {
                rPrime = 0f; gPrime = c; bPrime = x;
            }
            else if (hPrime >= 3 && hPrime < 4)
            {
                rPrime = 0f; gPrime = x; bPrime = c;
            }
            else if (hPrime >= 4 && hPrime < 5)
            {
                rPrime = x; gPrime = 0f; bPrime = c;
            }
            else
            {
                rPrime = c; gPrime = 0f; bPrime = x;
            }

            return Color.FromArgb(
                (int)((rPrime + m) * 255f),
                (int)((gPrime + m) * 255f),
                (int)((bPrime + m) * 255f));
        }

        private static Bitmap RecolorBitmap(Bitmap bitmap, Color color)
        {
            var newBitmap = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);

            var srcData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            var dstData = newBitmap.LockBits(
                new Rectangle(0, 0, newBitmap.Width, newBitmap.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                unsafe
                {
                    byte* srcPtr = (byte*)srcData.Scan0;
                    byte* dstPtr = (byte*)dstData.Scan0;

                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        byte* srcRow = srcPtr + (y * srcData.Stride);
                        byte* dstRow = dstPtr + (y * dstData.Stride);

                        for (int x = 0; x < bitmap.Width; x++)
                        {
                            byte* srcPixel = srcRow + (x * 4);
                            byte* dstPixel = dstRow + (x * 4);

                            dstPixel[0] = color.B;
                            dstPixel[1] = color.G;
                            dstPixel[2] = color.R;
                            dstPixel[3] = srcPixel[3];
                        }
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(srcData);
                newBitmap.UnlockBits(dstData);
            }

            return newBitmap;
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

        internal void SetTomatoClockCompleted(bool completed)
        {
            tomatoClockCompleted = completed;
            if (completed)
            {
                lastProgress = 1.0f;
            }
        }

        internal void SetTomatoClockProgress(float progress)
        {
            tomatoClockProgress = progress;
        }

        internal void ResetTomatoClockProgress()
        {
            lastProgress = -1f;
            tomatoClockProgress = 0f;
        }

        internal bool ShouldRegenerateSpritesheet()
        {
            if (tomatoClockCompleted)
            {
                return lastProgress != 1.0f;
            }

            float threshold = 0.05f;
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
