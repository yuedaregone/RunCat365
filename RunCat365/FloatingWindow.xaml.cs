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
        private readonly TranslateTransform _transform;
        private readonly Canvas _canvas;
        private BitmapSource? _spritesheet;
        private int _frameWidth;
        private int _frameHeight;
        private bool _isUserPlaced;

        public FloatingWindow()
        {
            InitializeComponent();

            _transform = new TranslateTransform();
            _brush = new ImageBrush
            {
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top,
                TileMode = TileMode.None
            };

            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(_transform);
            _brush.Transform = transformGroup;

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

            if (!_isUserPlaced)
            {
                var screen = SystemParameters.WorkArea;
                Left = screen.Right - Width - 50;
                Top = screen.Bottom - Height - 50;
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

            if (!_isUserPlaced)
            {
                var screen = SystemParameters.WorkArea;
                Left = screen.Right - Width - 50;
                Top = screen.Bottom - Height - 50;
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
