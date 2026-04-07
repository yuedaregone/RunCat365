using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace RunCat365
{
    internal class FrameAnimationEngine : IDisposable
    {
        private BitmapSource? spritesheet;
        private int frameWidth;
        private int frameHeight;
        private int frameCount;
        private int currentFrame;
        private double intervalMs = 200;
        private long lastFrameTime;
        private bool isRunning;
        private Func<double>? getTomatoProgress;
        private float maxSpeed;
        private readonly DispatcherTimer animationTimer;

        public event EventHandler<int>? FrameChanged;
        public event EventHandler<float>? SpeedChanged;

        public FrameAnimationEngine(AppConfig config)
        {
            maxSpeed = (float)config.MovementSpeedBase;

            animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            animationTimer.Tick += OnTimerTick;
        }

        public void SetTomatoClock(Func<double> getTomatoProgress)
        {
            this.getTomatoProgress = getTomatoProgress;
        }

        public void LoadSpritesheet(BitmapSource spritesheet, int frameWidth, int frameHeight)
        {
            this.spritesheet = spritesheet;
            this.frameWidth = frameWidth;
            this.frameHeight = frameHeight;
            frameCount = spritesheet.PixelWidth / frameWidth;
            currentFrame = 0;
        }

        public void Start()
        {
            if (isRunning) return;
            isRunning = true;
            lastFrameTime = Environment.TickCount64;
            animationTimer.Start();
        }

        public void Stop()
        {
            isRunning = false;
            animationTimer.Stop();
        }

        public void Reset()
        {
            currentFrame = 0;
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (!isRunning || spritesheet is null) return;

            double progress = getTomatoProgress?.Invoke() ?? 0;

            var easedProgress = ApplyEasing(progress);
            intervalMs = 500 - (easedProgress * 475);
            float speed = (float)(maxSpeed * easedProgress);

            animationTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(intervalMs, 25));

            var now = Environment.TickCount64;
            if ((now - lastFrameTime) < (long)intervalMs) return;

            lastFrameTime = now;
            currentFrame = (currentFrame + 1) % frameCount;
            FrameChanged?.Invoke(this, currentFrame);
            SpeedChanged?.Invoke(this, speed);
        }

        private static double ApplyEasing(double progress)
        {
            return progress * progress * (3 - 2 * progress);
        }

        public void Dispose()
        {
            animationTimer.Stop();
        }
    }
}
