using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace MultiWallFX
{
    public static class StartupHelper
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "MultiWallFX";

        public static bool IsStartupEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKey, false))
                {
                    if (key != null)
                    {
                        var val = key.GetValue(AppName);
                        return val != null;
                    }
                }
            }
            catch { }
            return false;
        }

        public static void SetStartup(bool enable)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            string exePath = Assembly.GetExecutingAssembly().Location;
                            string cmd = "\"" + exePath + "\" -startup";
                            key.SetValue(AppName, cmd, RegistryValueKind.String);
                        }
                        else
                        {
                            if (key.GetValue(AppName) != null)
                            {
                                key.DeleteValue(AppName, false);
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }
}
