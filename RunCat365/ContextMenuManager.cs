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

using Hardcodet.Wpf.TaskbarNotification;
using RunCat365.Properties;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RunCat365
{
    internal class ContextMenuManager : IDisposable
    {
        private readonly TaskbarIcon taskbarIcon;
        private readonly TextBlock systemInfoText;
        private readonly MenuItem runnersMenu;
        private readonly Func<Runner> getRunner;
        private readonly Action<Runner> setRunner;
        private readonly System.Windows.Media.FontFamily menuFont;

        private const double MenuFontSize = 13;

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
            this.getRunner = getRunner;
            this.setRunner = setRunner;
            menuFont = new System.Windows.Media.FontFamily(SupportedLanguageExtension.GetCurrentLanguage().GetFontName());

            systemInfoText = new TextBlock
            {
                Text = "-\n-\n-\n-\n-",
                IsEnabled = false,
                Foreground = System.Windows.Media.Brushes.Gray,
                FontFamily = menuFont,
                FontSize = MenuFontSize
            };

            runnersMenu = CreateRunnersMenu();

            var launchAtStartupMenu = CreateMenuItem(Strings.Menu_LaunchAtStartup, getLaunchAtStartup());
            launchAtStartupMenu.IsCheckable = true;
            launchAtStartupMenu.Click += (sender, e) =>
            {
                try
                {
                    if (toggleLaunchAtStartup(launchAtStartupMenu.IsChecked == true))
                    {
                        launchAtStartupMenu.IsChecked = !launchAtStartupMenu.IsChecked;
                    }
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(ex.Message, Strings.Message_Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };

            var tomatoClockStartMenu = CreateMenuItem("Start");
            tomatoClockStartMenu.Click += (sender, e) => startTomatoClock();

            var tomatoClockPauseMenu = CreateMenuItem("Pause");
            tomatoClockPauseMenu.Click += (sender, e) => pauseTomatoClock();

            var tomatoClockResetMenu = CreateMenuItem("Reset");
            tomatoClockResetMenu.Click += (sender, e) => resetTomatoClock();

            var tomatoClockDurationMenu = CreateMenuItem("Duration (minutes)");
            var durations = new[] { 15, 20, 25, 30, 45, 60 };
            foreach (var duration in durations)
            {
                var durationItem = CreateMenuItem($"{duration} min");
                durationItem.Click += (sender, e) => setTomatoClockDuration(duration);
                tomatoClockDurationMenu.Items.Add(durationItem);
            }

            var settingsMenu = CreateMenuItem(Strings.Menu_Settings);
            settingsMenu.Items.Add(launchAtStartupMenu);
            settingsMenu.Items.Add(new Separator());
            settingsMenu.Items.Add(tomatoClockStartMenu);
            settingsMenu.Items.Add(tomatoClockPauseMenu);
            settingsMenu.Items.Add(tomatoClockResetMenu);
            settingsMenu.Items.Add(tomatoClockDurationMenu);

            var exitMenu = CreateMenuItem(Strings.Menu_Exit);
            exitMenu.Click += (sender, e) => onExit();

            var contextMenu = new ContextMenu();
            TextOptions.SetTextFormattingMode(contextMenu, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(contextMenu, TextRenderingMode.ClearType);
            contextMenu.Items.Add(systemInfoText);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(runnersMenu);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(settingsMenu);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(exitMenu);

            System.Drawing.Icon? icon = null;
            try
            {
                using var stream = ResourceLoader.Assembly.GetManifestResourceStream("RunCat365.resources.app_icon.ico");
                if (stream is not null)
                {
                    icon = new System.Drawing.Icon(stream);
                }
            }
            catch { }

            if (icon is null)
            {
                icon = new System.Drawing.Icon(SystemIcons.Application, 16, 16);
            }

            taskbarIcon = new TaskbarIcon
            {
                Icon = icon,
                ContextMenu = contextMenu,
                Visibility = Visibility.Visible
            };
        }

        private MenuItem CreateMenuItem(string header, bool isChecked = false)
        {
            return new MenuItem
            {
                Header = header,
                IsChecked = isChecked,
                FontFamily = menuFont,
                FontSize = MenuFontSize
            };
        }

        private MenuItem CreateRunnersMenu()
        {
            var menu = CreateMenuItem(Strings.Menu_Runner);

            foreach (Runner runner in Enum.GetValues(typeof(Runner)))
            {
                var item = new MenuItem
                {
                    Header = runner.GetLocalizedString(),
                    IsCheckable = true,
                    IsChecked = getRunner() == runner,
                    FontFamily = menuFont,
                    FontSize = MenuFontSize,
                    Tag = runner
                };

                var thumbnail = GetRunnerThumbnailBitmap(runner);
                if (thumbnail != null)
                {
                    item.Icon = new System.Windows.Controls.Image { Source = thumbnail, Width = 16, Height = 16 };
                }

                item.Click += (sender, e) =>
                {
                    foreach (MenuItem childItem in menu.Items)
                    {
                        childItem.IsChecked = false;
                    }
                    item.IsChecked = true;

                    if (item.Tag is Runner selectedRunner)
                    {
                        setRunner(selectedRunner);
                    }
                };

                menu.Items.Add(item);
            }

            return menu;
        }

        private static BitmapImage? GetRunnerThumbnailBitmap(Runner runner)
        {
            var resourceName = $"RunCat365.resources.runners.{runner.GetString().ToLower()}.{runner.GetString().ToLower()}_0.png";

            try
            {
                using var stream = ResourceLoader.Assembly.GetManifestResourceStream(resourceName);
                if (stream is null) return null;

                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
        }

        internal void ShowBalloonTip(BalloonTipType balloonTipType)
        {
            var info = balloonTipType.GetInfo();
            taskbarIcon.ShowBalloonTip(info.Title, info.Text, BalloonIcon.Info);
        }

        internal void SetSystemInfoMenuText(string text)
        {
            systemInfoText.Text = text;
        }

        internal void SetNotifyIconText(string text)
        {
            taskbarIcon.ToolTipText = text;
        }

        public void Dispose()
        {
            taskbarIcon.Dispose();
        }
    }
}
