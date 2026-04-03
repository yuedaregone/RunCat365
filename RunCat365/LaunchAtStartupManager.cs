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

using System.IO;

namespace RunCat365
{
    internal class LaunchAtStartupManager
    {
        private readonly string _shortcutPath;

        public LaunchAtStartupManager()
        {
            var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            _shortcutPath = Path.Combine(startupFolder, "RunCat 365.lnk");
        }

        public bool GetStartup() => File.Exists(_shortcutPath);

        public bool SetStartup(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    if (!File.Exists(_shortcutPath))
                    {
                        return CreateShortcut();
                    }
                    return true;
                }
                else
                {
                    if (File.Exists(_shortcutPath))
                    {
                        File.Delete(_shortcutPath);
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
            var exePath = Environment.ProcessPath;
            if (exePath is null) return false;

            try
            {
                dynamic wsh = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!)!;
                dynamic shortcut = wsh.CreateShortcut(_shortcutPath)!;
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
