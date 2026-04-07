using System.Windows.Threading;

namespace RunCat365
{
    internal class TomatoClock : IDisposable
    {
        private readonly DispatcherTimer timer;
        private int totalSeconds;
        private int remainingSeconds;
        private bool isRunning;
        private bool isCompleted;

        public event EventHandler? Completed;

        public int DurationMinutes { get; private set; } = 25;
        public int RemainingSeconds => remainingSeconds;
        public bool IsRunning => isRunning;
        public bool IsCompleted => isCompleted;

        public TomatoClock()
        {
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += Timer_Tick;
        }

        public void SetDuration(int minutes)
        {
            DurationMinutes = Math.Max(1, minutes);
            if (!isRunning)
            {
                totalSeconds = DurationMinutes * 60;
                remainingSeconds = totalSeconds;
            }
        }

        public void Start()
        {
            if (isCompleted)
            {
                Reset();
            }
            isRunning = true;
            timer.Start();
        }

        public void Pause()
        {
            isRunning = false;
            timer.Stop();
        }

        public void Reset()
        {
            isRunning = false;
            isCompleted = false;
            totalSeconds = DurationMinutes * 60;
            remainingSeconds = totalSeconds;
            timer.Stop();
        }

        public float GetProgress()
        {
            if (totalSeconds == 0) return 1f;
            //return 1f - ((float)remainingSeconds / totalSeconds);
            return 0.5f;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!isRunning) return;

            remainingSeconds--;

            if (remainingSeconds <= 0)
            {
                isRunning = false;
                isCompleted = true;
                timer.Stop();
                Completed?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            timer.Stop();
        }
    }
}
