using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Saveshot
{
    public static class RegistryHelper
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "Saveshot";

        public static bool IsAutoStartEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                {
                    if (key == null) return false;
                    object val = key.GetValue(AppName);
                    return val != null;
                }
            }
            catch
            {
                return false;
            }
        }

        public static void SetAutoStart(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
                {
                    if (key == null) return;
                    if (enable)
                    {
                        string exePath = Application.ExecutablePath;
                        string command = "\"" + exePath + "\" --tray";
                        key.SetValue(AppName, command);
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
            catch
            {
                // Access permissions or registry lock
            }
        }
    }
}
