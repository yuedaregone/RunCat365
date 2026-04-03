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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RunCat365.Properties;

namespace RunCat365
{
    internal class FrameAnimationEngine
    {
        private BitmapSource? _spritesheet;
        private int _frameWidth;
        private int _frameHeight;
        private int _frameCount;
        private int _currentFrame;
        private double _intervalMs = 200;
        private long _lastFrameTime;
        private bool _isRunning;
        private Func<double>? _getTomatoProgress;
        private float _maxSpeed;

        public event EventHandler<int>? FrameChanged;
        public event EventHandler<float>? SpeedChanged;

        public FrameAnimationEngine()
        {
            _maxSpeed = (float)UserSettings.Default.MovementSpeedBase;
        }

        public void SetTomatoClock(Func<double> getTomatoProgress)
        {
            _getTomatoProgress = getTomatoProgress;
        }

        public void LoadSpritesheet(BitmapSource spritesheet, int frameWidth, int frameHeight)
        {
            _spritesheet = spritesheet;
            _frameWidth = frameWidth;
            _frameHeight = frameHeight;
            _frameCount = spritesheet.PixelWidth / frameWidth;
            _currentFrame = 0;
        }

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;
            _lastFrameTime = Environment.TickCount64;
            CompositionTarget.Rendering += OnRendering;
        }

        public void Stop()
        {
            _isRunning = false;
            CompositionTarget.Rendering -= OnRendering;
        }

        public void Reset()
        {
            _currentFrame = 0;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            if (!_isRunning || _spritesheet is null) return;

            double progress = _getTomatoProgress?.Invoke() ?? 0;

            _intervalMs = 500 - (progress * 475);
            float speed = (float)(_maxSpeed * progress);

            var now = Environment.TickCount64;
            if ((now - _lastFrameTime) < (long)_intervalMs) return;

            _lastFrameTime = now;
            _currentFrame = (_currentFrame + 1) % _frameCount;
            FrameChanged?.Invoke(this, _currentFrame);
            SpeedChanged?.Invoke(this, speed);
        }
    }
}
