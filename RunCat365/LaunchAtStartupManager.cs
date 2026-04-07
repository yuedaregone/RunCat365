using System.IO;

namespace RunCat365
{
    internal class LaunchAtStartupManager
    {
        private readonly string shortcutPath;

        public LaunchAtStartupManager()
        {
            string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            shortcutPath = Path.Combine(startupFolder, "RunCat 365.lnk");
        }

        public bool GetStartup() => File.Exists(shortcutPath);

        public bool SetStartup(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    if (!File.Exists(shortcutPath))
                    {
                        return CreateShortcut();
                    }
                    return true;
                }
                else
                {
                    if (File.Exists(shortcutPath))
                    {
                        File.Delete(shortcutPath);
                    }
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private bool CreateShortcut()
        {
            string? exePath = Environment.ProcessPath;
            if (exePath is null) return false;

            try
            {
                dynamic wsh = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!)!;
                dynamic shortcut = wsh.CreateShortcut(shortcutPath)!;
                shortcut.TargetPath = exePath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
                shortcut.Description = "RunCat 365";
                shortcut.Save();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
