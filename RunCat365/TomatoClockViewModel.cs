using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RunCat365
{
    internal class TomatoClockViewModel : INotifyPropertyChanged
    {
        private readonly TomatoClock tomatoClock;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int RemainingSeconds => tomatoClock.RemainingSeconds;
        public float Progress => tomatoClock.GetProgress();
        public bool IsRunning => tomatoClock.IsRunning;
        public bool IsCompleted => tomatoClock.IsCompleted;
        public int DurationMinutes => tomatoClock.DurationMinutes;

        public string RemainingTimeText
        {
            get
            {
                if (IsCompleted)
                {
                    return "Complete!";
                }

                int minutes = RemainingSeconds / 60;
                int seconds = RemainingSeconds % 60;
                return $"{minutes:D2}:{seconds:D2}";
            }
        }

        public string ProgressText => IsRunning ? $"{Progress:P0}" : string.Empty;

        public TomatoClockViewModel(TomatoClock tomatoClock)
        {
            this.tomatoClock = tomatoClock;
            this.tomatoClock.Completed += TomatoClock_Completed;
        }

        public void SetDuration(int minutes)
        {
            tomatoClock.SetDuration(minutes);
            OnPropertyChanged(nameof(RemainingSeconds));
            OnPropertyChanged(nameof(RemainingTimeText));
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(ProgressText));
        }

        public void Start()
        {
            tomatoClock.Start();
            OnPropertyChanged(nameof(IsRunning));
            OnPropertyChanged(nameof(IsCompleted));
        }

        public void Pause()
        {
            tomatoClock.Pause();
            OnPropertyChanged(nameof(IsRunning));
        }

        public void Reset()
        {
            tomatoClock.Reset();
            OnPropertyChanged(nameof(IsRunning));
            OnPropertyChanged(nameof(IsCompleted));
            OnPropertyChanged(nameof(RemainingSeconds));
            OnPropertyChanged(nameof(RemainingTimeText));
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(ProgressText));
        }

        public void Update()
        {
            OnPropertyChanged(nameof(RemainingSeconds));
            OnPropertyChanged(nameof(RemainingTimeText));
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(IsRunning));
            OnPropertyChanged(nameof(IsCompleted));
        }

        private void TomatoClock_Completed(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(IsCompleted));
            OnPropertyChanged(nameof(IsRunning));
            OnPropertyChanged(nameof(RemainingTimeText));
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            tomatoClock.Completed -= TomatoClock_Completed;
        }
    }
}
