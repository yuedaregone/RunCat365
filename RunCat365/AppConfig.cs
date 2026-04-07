using System.IO;
using System.Text.Json;

namespace RunCat365
{
    public class AppConfig
    {
        private static readonly string configFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RunCat365",
            "config.json");

        private static AppConfig? instance;
        private static readonly object lockObject = new object();

        public double MovementSpeedBase { get; set; } = 3;
        public string Runner { get; set; } = "Cat";
        public bool FirstLaunch { get; set; } = true;
        public int TomatoClockDuration { get; set; } = 25;
        public double WindowLeft { get; set; } = 0;
        public double WindowTop { get; set; } = 0;

        public static AppConfig Instance
        {
            get
            {
                if (instance is null)
                {
                    lock (lockObject)
                    {
                        if (instance is null)
                        {
                            instance = LoadConfig();
                        }
                    }
                }
                return instance;
            }
        }

        private static AppConfig LoadConfig()
        {
            try
            {
                if (File.Exists(configFilePath))
                {
                    string json = File.ReadAllText(configFilePath);
                    AppConfig? config = JsonSerializer.Deserialize<AppConfig>(json);
                    return config ?? new AppConfig();
                }
            }
            catch
            {
            }
            return new AppConfig();
        }

        public void Save()
        {
            try
            {
                string? directory = Path.GetDirectoryName(configFilePath);
                if (directory is not null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(configFilePath, json);
            }
            catch
            {
            }
        }

        public void Reload()
        {
            AppConfig loadedConfig = LoadConfig();
            MovementSpeedBase = loadedConfig.MovementSpeedBase;
            Runner = loadedConfig.Runner;
            FirstLaunch = loadedConfig.FirstLaunch;
            TomatoClockDuration = loadedConfig.TomatoClockDuration;
            WindowLeft = loadedConfig.WindowLeft;
            WindowTop = loadedConfig.WindowTop;
        }
    }
}
