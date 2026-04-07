using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RunCat365.Runners;
using WpfRectangle = System.Windows.Shapes.Rectangle;

namespace RunCat365
{
    public partial class FloatingWindow : Window
    {
        private readonly WpfRectangle rectangle;
        private readonly ImageBrush brush;
        private readonly Canvas canvas;
        private readonly DispatcherTimer moveTimer;
        private readonly AppConfig config;
        private BitmapSource? spritesheet;
        private int frameWidth = 48;
        private int frameHeight = 48;
        private bool userPositioned;
        private float currentSpeed;
        private float[]? frameMoveRatios;
        private int currentFrame;

        public void SetSpeed(object? sender, float speed)
        {
            currentSpeed = speed;
        }

        public void SetFrameMoveRatios(Runner runner)
        {
            frameMoveRatios = runner switch
            {
                Runner.Cat => FrameMoveRatios.Cat,
                Runner.Horse => FrameMoveRatios.Horse,
                Runner.Parrot => FrameMoveRatios.Parrot,
                _ => null
            };
        }

        private void OnMoveTimer(object? sender, EventArgs e)
        {
            if (currentSpeed <= 0) return;

            float ratio = 1.0f;
            if (frameMoveRatios != null && currentFrame < frameMoveRatios.Length)
            {
                ratio = frameMoveRatios[currentFrame];
            }

            Left += currentSpeed * ratio;

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

        public FloatingWindow(AppConfig config)
        {
            this.config = config;

            moveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            moveTimer.Tick += OnMoveTimer;

            InitializeComponent();

            brush = new ImageBrush
            {
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top,
                TileMode = TileMode.None
            };

            rectangle = new WpfRectangle
            {
                Fill = brush,
                Width = 48,
                Height = 48
            };

            canvas = new Canvas();
            canvas.Children.Add(rectangle);
            canvas.Width = 48;
            canvas.Height = 48;

            Grid grid = new Grid();
            grid.Children.Add(canvas);
            Content = grid;

            string runnerName = config.Runner;
            Runner runner = runnerName switch
            {
                "Cat" => Runner.Cat,
                "Horse" => Runner.Horse,
                "Parrot" => Runner.Parrot,
                _ => Runner.Cat
            };
            SetFrameMoveRatios(runner);
        }

        public void StartMoveTimer()
        {
            moveTimer.Start();
        }

        public void StopMoveTimer()
        {
            moveTimer.Stop();
        }

        private void SetDefaultPosition()
        {
            Rect workArea = SystemParameters.WorkArea;
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
            this.spritesheet = spritesheet;
            this.frameWidth = frameWidth;
            this.frameHeight = frameHeight;

            brush.ImageSource = spritesheet;
            brush.Viewbox = new Rect(0, 0, frameWidth, frameHeight);
            brush.ViewboxUnits = BrushMappingMode.Absolute;

            rectangle.Width = frameWidth;
            rectangle.Height = frameHeight;

            canvas.Width = frameWidth;
            canvas.Height = frameHeight;

            Width = frameWidth;
            Height = frameHeight;

            if (!userPositioned)
            {
                SetDefaultPosition();
            }
        }

        public void SetFrame(int index)
        {
            if (spritesheet is null) return;

            currentFrame = index;
            double x = index * frameWidth;
            brush.Viewbox = new Rect(x, 0, frameWidth, frameHeight);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            userPositioned = true;
            DragMove();
            SavePosition();
        }

        private void SavePosition()
        {
            config.WindowLeft = Left;
            config.WindowTop = Top;
            config.Save();
        }

        public void RestorePosition()
        {
            double savedLeft = config.WindowLeft;
            double savedTop = config.WindowTop;

            if (savedLeft != 0 || savedTop != 0)
            {
                Left = savedLeft;
                Top = savedTop;
                userPositioned = true;
            }
            else
            {
                SetDefaultPosition();
                userPositioned = false;
            }
        }
    }
}
