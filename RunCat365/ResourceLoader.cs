using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;

namespace RunCat365
{
    internal static class ResourceLoader
    {
        private static readonly Assembly assembly = typeof(ResourceLoader).Assembly;
        private static readonly Dictionary<string, BitmapImage> bitmapCache = new();
        private static readonly Dictionary<string, WriteableBitmap> spritesheetCache = new();
        private static readonly HashSet<string> availableResources = new();

        internal static Assembly Assembly => assembly;

        static ResourceLoader()
        {
            var resourceNames = assembly.GetManifestResourceNames();
            foreach (var name in resourceNames)
            {
                availableResources.Add(name);
            }
        }

        internal static byte[]? GetRunnerPixels(string runnerName, int frameIndex, out int width, out int height)
        {
            width = 0;
            height = 0;
            var key = $"{runnerName.ToLower()}_{frameIndex}";

            if (bitmapCache.TryGetValue(key, out var cached))
            {
                width = cached.PixelWidth;
                height = cached.PixelHeight;
                var stride = width * 4;
                var pixels = new byte[stride * height];
                cached.CopyPixels(pixels, stride, 0);
                return pixels;
            }

            var resourceName = FindResourceName(runnerName.ToLower(), frameIndex);
            if (resourceName is null) return null;

            try
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
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

                bitmapCache[key] = image;
                return pixels;
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load resource: {runnerName}_{frameIndex}");
                return null;
            }
        }

        internal static WriteableBitmap? GetCachedSpritesheet(string runnerName, int frameCount)
        {
            var cacheKey = $"spritesheet_{runnerName.ToLower()}";

            if (spritesheetCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            int maxWidth = 0;
            int maxHeight = 0;
            var frameData = new List<(byte[] pixels, int w, int h)>();

            for (int i = 0; i < frameCount; i++)
            {
                var pixels = GetRunnerPixels(runnerName, i, out var w, out var h);
                if (pixels is null) continue;
                frameData.Add((pixels, w, h));
                if (w > maxWidth) maxWidth = w;
                if (h > maxHeight) maxHeight = h;
            }

            if (frameData.Count == 0 || maxWidth == 0 || maxHeight == 0)
            {
                return null;
            }

            var spritesheet = new WriteableBitmap(
                frameCount * maxWidth, maxHeight, 96, 96,
                System.Windows.Media.PixelFormats.Bgra32, null);

            for (int i = 0; i < frameData.Count; i++)
            {
                var (pixels, w, h) = frameData[i];
                int offsetX = (maxWidth - w) / 2;
                int offsetY = (maxHeight - h) / 2;

                var rect = new Int32Rect(i * maxWidth + offsetX, offsetY, w, h);
                spritesheet.WritePixels(rect, pixels, w * 4, 0);
            }

            spritesheet.Freeze();
            spritesheetCache[cacheKey] = spritesheet;
            return spritesheet;
        }

        private static string? FindResourceName(string runnerName, int frameIndex)
        {
            var suffix = $".{runnerName}_{frameIndex}.png";
            foreach (var name in availableResources)
            {
                if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    return name;
                }
            }
            return null;
        }
    }
}
