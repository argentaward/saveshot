using System;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;

namespace Saveshot
{
    public class ScreenshotSavedEventArgs : EventArgs
    {
        public string FilePath { get; private set; }
        public ScreenshotSavedEventArgs(string filePath)
        {
            FilePath = filePath;
        }
    }

    public class ClipboardMonitor : NativeWindow, IDisposable
    {
        private const int WM_CLIPBOARDUPDATE = 0x031D;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        private readonly AppConfig _config;
        private bool _isListening;
        private bool _isInternalClipboardWrite;
        private string _lastSavedHash;
        private DateTime _lastSavedTime = DateTime.MinValue;

        public event EventHandler<ScreenshotSavedEventArgs> ScreenshotSaved;

        public ClipboardMonitor(AppConfig config)
        {
            _config = config;
            Start();
        }

        public void Start()
        {
            if (_isListening) return;

            CreateHandle(new CreateParams());
            if (AddClipboardFormatListener(this.Handle))
            {
                _isListening = true;
            }
        }

        public void Stop()
        {
            if (!_isListening) return;

            RemoveClipboardFormatListener(this.Handle);
            DestroyHandle();
            _isListening = false;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_CLIPBOARDUPDATE)
            {
                OnClipboardUpdate();
            }

            base.WndProc(ref m);
        }

        private void OnClipboardUpdate()
        {
            // If autosave is paused, skip
            if (!_config.AutoSaveEnabled)
                return;

            // If we wrote to the clipboard ourselves (path or file drop), skip our own notification
            if (_isInternalClipboardWrite)
            {
                _isInternalClipboardWrite = false;
                return;
            }

            // Snipping tool might be writing data; retry briefly if clipboard is locked
            Image img = null;
            for (int attempt = 0; attempt < 6; attempt++)
            {
                try
                {
                    if (Clipboard.ContainsImage())
                    {
                        img = Clipboard.GetImage();
                        if (img != null)
                            break;
                    }
                    else
                    {
                        // No image in clipboard (text, files, etc.)
                        return;
                    }
                }
                catch (ExternalException)
                {
                    Thread.Sleep(35);
                }
                catch
                {
                    break;
                }
            }

            if (img == null)
                return;

            try
            {
                string hash = ComputeImageHash(img);

                // Deduplicate: Snipping Tool sometimes triggers multiple updates for the same capture
                if (hash == _lastSavedHash && (DateTime.UtcNow - _lastSavedTime).TotalSeconds < 2.5)
                {
                    return;
                }

                string targetDir = _config.SaveDirectory;
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                DateTime now = DateTime.Now;
                int counter = 0;
                string fileName = _config.GenerateFileName(now, counter);
                string fullPath = Path.Combine(targetDir, fileName);

                while (File.Exists(fullPath))
                {
                    counter++;
                    fileName = _config.GenerateFileName(now, counter);
                    fullPath = Path.Combine(targetDir, fileName);
                }

                // Save image with appropriate format
                SaveImageWithQuality(img, fullPath, _config.ImageFormat);

                _lastSavedHash = hash;
                _lastSavedTime = DateTime.UtcNow;

                // Handle clipboard substitution if requested
                if (_config.ClipboardAction == ClipboardAction.CopyFilePath)
                {
                    _isInternalClipboardWrite = true;
                    SafeSetClipboardText(fullPath);
                }
                else if (_config.ClipboardAction == ClipboardAction.CopyFileDrop)
                {
                    _isInternalClipboardWrite = true;
                    SafeSetClipboardFile(fullPath);
                }

                // Raise event for notification/sound
                if (ScreenshotSaved != null)
                {
                    ScreenshotSaved(this, new ScreenshotSavedEventArgs(fullPath));
                }
            }
            catch
            {
                // Silently handle any unexpected I/O errors
            }
            finally
            {
                img.Dispose();
            }
        }

        private static void SaveImageWithQuality(Image img, string path, string format)
        {
            if (string.Equals(format, "JPEG", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(format, "JPG", StringComparison.OrdinalIgnoreCase))
            {
                ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
                if (jpgEncoder != null)
                {
                    using (EncoderParameters encParams = new EncoderParameters(1))
                    {
                        encParams.Param[0] = new EncoderParameter(Encoder.Quality, 92L);
                        img.Save(path, jpgEncoder, encParams);
                        return;
                    }
                }
                img.Save(path, ImageFormat.Jpeg);
            }
            else if (string.Equals(format, "BMP", StringComparison.OrdinalIgnoreCase))
            {
                img.Save(path, ImageFormat.Bmp);
            }
            else
            {
                // Default to lossless PNG
                img.Save(path, ImageFormat.Png);
            }
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                    return codec;
            }
            return null;
        }

        private static string ComputeImageHash(Image img)
        {
            using (var ms = new MemoryStream())
            {
                img.Save(ms, ImageFormat.Bmp);
                ms.Position = 0;
                using (var md5 = MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(ms);
                    return BitConverter.ToString(hash).Replace("-", "");
                }
            }
        }

        private static void SafeSetClipboardText(string text)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    Clipboard.SetText(text);
                    return;
                }
                catch (ExternalException)
                {
                    Thread.Sleep(30);
                }
            }
        }

        private static void SafeSetClipboardFile(string path)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    var files = new StringCollection { path };
                    Clipboard.SetFileDropList(files);
                    return;
                }
                catch (ExternalException)
                {
                    Thread.Sleep(30);
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
