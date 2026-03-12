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

namespace RunCat365
{
    internal class TomatoClock
    {
        private readonly System.Windows.Forms.Timer timer;
        private int totalSeconds;
        private int remainingSeconds;
        private bool isRunning;
        private bool isCompleted;

        public event EventHandler? Tick;
        public event EventHandler? Completed;

        public int DurationMinutes { get; private set; } = 25;
        public int RemainingSeconds => remainingSeconds;
        public bool IsRunning => isRunning;
        public bool IsCompleted => isCompleted;

        public TomatoClock()
        {
            timer = new System.Windows.Forms.Timer
            {
                Interval = 1000 // 1 second
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
            return 1f - ((float)remainingSeconds / totalSeconds);
        }

        public float GetRemainingMinutes()
        {
            return (float)remainingSeconds / 60f;
        }

        public float GetRedIntensity()
        {
            // Red intensity increases in the last 5 minutes
            // 0 = no red, 1 = full red
            float remainingMinutes = GetRemainingMinutes();
            if (remainingMinutes > 5f) return 0f;
            return Math.Clamp(1f - (remainingMinutes / 5f), 0f, 1f);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!isRunning) return;

            remainingSeconds--;
            Tick?.Invoke(this, EventArgs.Empty);

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
            timer?.Stop();
            timer?.Dispose();
        }
    }
}
