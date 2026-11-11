using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Saveshot
{
    public static class SoundHelper
    {
        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool PlaySound(string pszSound, IntPtr hmod, uint fdwSound);

        private const uint SND_ASYNC = 0x0001;
        private const uint SND_NODEFAULT = 0x0002;
        private const uint SND_FILENAME = 0x00020000;

        private static readonly string WindowsMediaDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Media");

        public static void Play(string soundType)
        {
            if (string.IsNullOrEmpty(soundType) || soundType.Equals("None", StringComparison.OrdinalIgnoreCase))
                return;

            string wavPath = GetWavPath(soundType);

            try
            {
                if (!string.IsNullOrEmpty(wavPath) && File.Exists(wavPath))
                {
                    PlaySound(wavPath, IntPtr.Zero, SND_ASYNC | SND_FILENAME | SND_NODEFAULT);
                }
            }
            catch
            {
                // Silently ignore audio playback errors
            }
        }

        public static string GetWavPath(string soundType)
        {
            if (string.IsNullOrEmpty(soundType))
                return null;

            switch (soundType.ToLowerInvariant())
            {
                case "click":
                    // Gentle, subtle Windows navigation click
                    return Path.Combine(WindowsMediaDir, "Windows Navigation Start.wav");

                case "notify":
                    // Soft pleasant chime
                    string notifyPath = Path.Combine(WindowsMediaDir, "Windows Notify Messaging.wav");
                    if (File.Exists(notifyPath)) return notifyPath;
                    return Path.Combine(WindowsMediaDir, "Windows Background.wav");

                case "ding":
                    // Classic crystal ding
                    return Path.Combine(WindowsMediaDir, "Windows Ding.wav");

                case "popup":
                    return Path.Combine(WindowsMediaDir, "Windows Pop-up Blocked.wav");

                default:
                    return null;
            }
        }
    }
}
