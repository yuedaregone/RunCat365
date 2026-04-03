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

using System.Reflection;
using System.Windows.Media.Imaging;

namespace RunCat365
{
    internal static class ResourceLoader
    {
        private static readonly Dictionary<string, BitmapImage> cache = [];
        internal static readonly Assembly Assembly = typeof(ResourceLoader).Assembly;

        internal static byte[]? GetRunnerPixels(string runnerName, int frameIndex, out int width, out int height)
        {
            width = 0;
            height = 0;
            var key = $"{runnerName.ToLower()}_{frameIndex}";

            if (cache.TryGetValue(key, out var cached))
            {
                width = cached.PixelWidth;
                height = cached.PixelHeight;
                var stride = width * 4;
                var pixels = new byte[stride * height];
                cached.CopyPixels(pixels, stride, 0);
                return pixels;
            }

            var resourceNames = Assembly.GetManifestResourceNames();
            var resourceName = resourceNames.FirstOrDefault(n => n.EndsWith($".{runnerName.ToLower()}_{frameIndex}.png", StringComparison.OrdinalIgnoreCase));

            if (resourceName is null) return null;

            try
            {
                using var stream = Assembly.GetManifestResourceStream(resourceName);
                if (stream is null) return null;

                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();

                width = image.PixelWidth;
                height = image.PixelHeight;

                var stride = width * 4;
                var pixels = new byte[stride * height];
                image.CopyPixels(pixels, stride, 0);

                cache[key] = image;
                return pixels;
            }
            catch
            {
                return null;
            }
        }
    }
}
