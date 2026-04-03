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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfRectangle = System.Windows.Shapes.Rectangle;
using RunCat365.Properties;

namespace RunCat365
{
    public partial class FloatingWindow : Window
    {
        private readonly WpfRectangle _rectangle;
        private readonly ImageBrush _brush;
        private readonly Canvas _canvas;
        private readonly DispatcherTimer _moveTimer;
        private BitmapSource? _spritesheet;
        private int _frameWidth = 48;
        private int _frameHeight = 48;
        private bool _userPositioned;

        private float _currentSpeed;

        public void SetSpeed(object? sender, float speed)
        {
            _currentSpeed = speed;
        }

        private void OnMoveTimer(object? sender, EventArgs e)
        {
            if (_currentSpeed <= 0) return;

            Left += _currentSpeed;

            double screenWidth = SystemParameters.PrimaryScreenWidth;
            if (Left > screenWidth)
            {
                Left = -Width;
            }
            else if (Left < -Width)
            {
                Left = screenWidth;
            }
        }

        public FloatingWindow()
        {
            _moveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _moveTimer.Tick += OnMoveTimer;

            InitializeComponent();

            _brush = new ImageBrush
            {
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top,
                TileMode = TileMode.None
            };

            _rectangle = new WpfRectangle
            {
                Fill = _brush,
                Width = 48,
                Height = 48
            };

            _canvas = new Canvas();
            _canvas.Children.Add(_rectangle);
            _canvas.Width = 48;
            _canvas.Height = 48;

            var grid = new Grid();
            grid.Children.Add(_canvas);
            Content = grid;
        }

        public void StartMoveTimer()
        {
            _moveTimer.Start();
        }

        public void StopMoveTimer()
        {
            _moveTimer.Stop();
        }

        private void SetDefaultPosition()
        {
            var workArea = SystemParameters.WorkArea;
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;

            bool taskbarOnLeft = workArea.Left > 0;
            bool taskbarOnRight = workArea.Right < screenWidth;
            bool taskbarOnBottom = workArea.Bottom < screenHeight;

            if (taskbarOnLeft || taskbarOnRight)
            {
                Top = workArea.Bottom - Height;
                Left = workArea.Left + (workArea.Width - Width) / 2;
            }
            else if (taskbarOnBottom)
            {
                Top = workArea.Bottom - Height;
                Left = workArea.Left + (workArea.Width - Width) / 2;
            }
            else
            {
                Top = workArea.Top;
                Left = workArea.Left + (workArea.Width - Width) / 2;
            }
        }

        public void LoadSpritesheet(BitmapSource spritesheet, int frameWidth, int frameHeight)
        {
            _spritesheet = spritesheet;
            _frameWidth = frameWidth;
            _frameHeight = frameHeight;

            _brush.ImageSource = spritesheet;
            _brush.Viewbox = new Rect(0, 0, frameWidth, frameHeight);
            _brush.ViewboxUnits = BrushMappingMode.Absolute;

            _rectangle.Width = frameWidth;
            _rectangle.Height = frameHeight;

            _canvas.Width = frameWidth;
            _canvas.Height = frameHeight;

            Width = frameWidth;
            Height = frameHeight;

            if (!_userPositioned)
            {
                SetDefaultPosition();
            }
        }

        public void SetFrame(int index)
        {
            if (_spritesheet is null) return;

            double x = index * _frameWidth;
            _brush.Viewbox = new Rect(x, 0, _frameWidth, _frameHeight);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            _userPositioned = true;
            DragMove();
            SavePosition();
        }

        private void SavePosition()
        {
            UserSettings.Default.WindowLeft = Left;
            UserSettings.Default.WindowTop = Top;
            UserSettings.Default.Save();
        }

        public void RestorePosition()
        {
            double savedLeft = UserSettings.Default.WindowLeft;
            double savedTop = UserSettings.Default.WindowTop;

            if (savedLeft != 0 || savedTop != 0)
            {
                Left = savedLeft;
                Top = savedTop;
                _userPositioned = true;
            }
            else
            {
                SetDefaultPosition();
                _userPositioned = false;
            }
        }
    }
}
