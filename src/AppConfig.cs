using System;
using System.IO;
using System.Text;

namespace Saveshot
{
    public enum ClipboardAction
    {
        KeepImage,
        CopyFilePath,
        CopyFileDrop
    }

    public class AppConfig
    {
        public string SaveDirectory { get; set; }
        public string ImageFormat { get; set; }
        public string FileNamePattern { get; set; }
        public bool StartWithWindows { get; set; }
        public bool ShowNotification { get; set; }
        public bool PlaySound { get; set; }
        public string SoundType { get; set; }
        public bool AutoSaveEnabled { get; set; }
        public ClipboardAction ClipboardAction { get; set; }

        private static string _configFilePath;

        public AppConfig()
        {
            // Default settings
            string defaultPictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            SaveDirectory = Path.Combine(defaultPictures, "Saveshot");
            ImageFormat = "PNG";
            FileNamePattern = "Screenshot_{date}_{time}";
            StartWithWindows = false;
            ShowNotification = true;
            PlaySound = true;
            SoundType = "Click"; // Subtle Windows Navigation Click
            AutoSaveEnabled = true;
            ClipboardAction = ClipboardAction.KeepImage;
        }

        public static string GetConfigFilePath()
        {
            if (!string.IsNullOrEmpty(_configFilePath))
                return _configFilePath;

            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string localIni = Path.Combine(exeDir, "settings.ini");

            if (File.Exists(localIni))
            {
                _configFilePath = localIni;
                return _configFilePath;
            }

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string saveshotDir = Path.Combine(appData, "Saveshot");
            if (!Directory.Exists(saveshotDir))
            {
                try { Directory.CreateDirectory(saveshotDir); } catch { }
            }

            _configFilePath = Path.Combine(saveshotDir, "settings.ini");
            return _configFilePath;
        }

        public static AppConfig Load()
        {
            var config = new AppConfig();
            string path = GetConfigFilePath();

            if (!File.Exists(path))
            {
                config.Save();
                return config;
            }

            try
            {
                string[] lines = File.ReadAllLines(path, Encoding.UTF8);
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#") || trimmed.StartsWith(";"))
                        continue;

                    int idx = trimmed.IndexOf('=');
                    if (idx <= 0) continue;

                    string key = trimmed.Substring(0, idx).Trim();
                    string val = trimmed.Substring(idx + 1).Trim();

                    switch (key.ToLowerInvariant())
                    {
                        case "savedirectory":
                            if (!string.IsNullOrEmpty(val)) config.SaveDirectory = val;
                            break;
                        case "imageformat":
                            if (!string.IsNullOrEmpty(val)) config.ImageFormat = val.ToUpperInvariant();
                            break;
                        case "filenamepattern":
                            if (!string.IsNullOrEmpty(val)) config.FileNamePattern = val;
                            break;
                        case "startwithwindows":
                            bool sww;
                            if (bool.TryParse(val, out sww)) config.StartWithWindows = sww;
                            break;
                        case "shownotification":
                            bool sn;
                            if (bool.TryParse(val, out sn)) config.ShowNotification = sn;
                            break;
                        case "playsound":
                            bool ps;
                            if (bool.TryParse(val, out ps)) config.PlaySound = ps;
                            break;
                        case "soundtype":
                            if (!string.IsNullOrEmpty(val)) config.SoundType = val;
                            break;
                        case "autosaveenabled":
                            bool asave;
                            if (bool.TryParse(val, out asave)) config.AutoSaveEnabled = asave;
                            break;
                        case "clipboardaction":
                            try
                            {
                                config.ClipboardAction = (ClipboardAction)Enum.Parse(typeof(ClipboardAction), val, true);
                            }
                            catch { }
                            break;
                    }
                }
            }
            catch { }

            return config;
        }

        public void Save()
        {
            try
            {
                string path = GetConfigFilePath();
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var sb = new StringBuilder();
                sb.AppendLine("; Saveshot configuration file");
                sb.AppendLine("SaveDirectory=" + SaveDirectory);
                sb.AppendLine("ImageFormat=" + ImageFormat);
                sb.AppendLine("FileNamePattern=" + FileNamePattern);
                sb.AppendLine("StartWithWindows=" + StartWithWindows);
                sb.AppendLine("ShowNotification=" + ShowNotification);
                sb.AppendLine("PlaySound=" + PlaySound);
                sb.AppendLine("SoundType=" + SoundType);
                sb.AppendLine("AutoSaveEnabled=" + AutoSaveEnabled);
                sb.AppendLine("ClipboardAction=" + ClipboardAction.ToString());

                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        public string GenerateFileName(DateTime dt, int counter = 0)
        {
            string pattern = string.IsNullOrEmpty(FileNamePattern) ? "Screenshot_{date}_{time}" : FileNamePattern;

            string res = pattern
                .Replace("{date}", dt.ToString("yyyy-MM-dd"))
                .Replace("{time}", dt.ToString("HH-mm-ss"))
                .Replace("{yyyy}", dt.ToString("yyyy"))
                .Replace("{MM}", dt.ToString("MM"))
                .Replace("{dd}", dt.ToString("dd"))
                .Replace("{HH}", dt.ToString("HH"))
                .Replace("{mm}", dt.ToString("mm"))
                .Replace("{ss}", dt.ToString("ss"));

            if (counter > 0)
            {
                res += "_" + counter;
            }

            string ext = ".png";
            if (ImageFormat.Equals("JPEG", StringComparison.OrdinalIgnoreCase) || ImageFormat.Equals("JPG", StringComparison.OrdinalIgnoreCase))
                ext = ".jpg";
            else if (ImageFormat.Equals("BMP", StringComparison.OrdinalIgnoreCase))
                ext = ".bmp";

            return res + ext;
        }
    }
}
