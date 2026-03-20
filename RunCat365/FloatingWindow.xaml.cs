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
using WpfRectangle = System.Windows.Shapes.Rectangle;

namespace RunCat365
{
    public partial class FloatingWindow : Window
    {
        private readonly WpfRectangle _rectangle;
        private readonly ImageBrush _brush;
        private readonly Canvas _canvas;
        private readonly Func<double> _getProgress;
        private BitmapSource? _spritesheet;
        private int _frameWidth = 48;
        private int _frameHeight = 48;
        private bool _isUserPlaced;

        private double _baseSpeed = 4.0;
        private int _direction = 1;

        public double MovementSpeedBase
        {
            get => _baseSpeed;
            set => _baseSpeed = value;
        }

        private void UpdatePosition()
        {
            if (_spritesheet == null) return;

            double progress = _getProgress();
            double speed = _baseSpeed * progress;
            double deltaX = speed * _direction;

            double newLeft = Left + deltaX;
            var workArea = SystemParameters.WorkArea;

            if (newLeft > workArea.Right)
            {
                newLeft = workArea.Left - Width;
            }
            else if (newLeft < workArea.Left - Width)
            {
                newLeft = workArea.Right;
            }

            Left = newLeft;
        }

        public void Tick()
        {
            UpdatePosition();
        }

        public FloatingWindow(Func<double> getProgress)
        {
            _getProgress = getProgress;

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

        private void SetDefaultPosition()
        {
            var workArea = SystemParameters.WorkArea;
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;

            bool taskbarOnLeft = workArea.Left > 0;
            bool taskbarOnRight = workArea.Right < screenWidth;
            bool taskbarOnBottom = workArea.Bottom < screenHeight;

            if (taskbarOnBottom)
            {
                Top = workArea.Bottom - Height;
            }
            else
            {
                Top = workArea.Top;
            }

            if (taskbarOnLeft)
            {
                Top = workArea.Bottom - Height;
                Left = workArea.Left + (workArea.Width - Width) / 2;
            }
            else if (taskbarOnRight)
            {
                Top = workArea.Bottom - Height;
                Left = workArea.Left + (workArea.Width - Width) / 2;
            }
            else
            {
                Left = workArea.Left + (workArea.Width - Width) / 2;
            }

            _direction = 1;
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

            if (!_isUserPlaced)
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
            _isUserPlaced = true;
            DragMove();
        }
    }
}
