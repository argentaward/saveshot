using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Saveshot
{
    public class TrayApplicationContext : ApplicationContext
    {
        private readonly AppConfig _config;
        private readonly NotifyIcon _notifyIcon;
        private readonly ClipboardMonitor _monitor;
        private MenuItem _menuItemAutoSave;
        private string _lastSavedFile;

        public TrayApplicationContext(AppConfig config)
        {
            _config = config;

            _notifyIcon = new NotifyIcon
            {
                Text = "Saveshot — Автосохранение скриншотов",
                Visible = true
            };

            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(iconPath))
                {
                    _notifyIcon.Icon = new Icon(iconPath);
                }
                else
                {
                    _notifyIcon.Icon = SystemIcons.Application;
                }
            }
            catch
            {
                _notifyIcon.Icon = SystemIcons.Application;
            }

            BuildContextMenu();

            _notifyIcon.DoubleClick += (s, e) => OpenScreenshotsFolder();
            _notifyIcon.BalloonTipClicked += (s, e) => OnBalloonClicked();

            _monitor = new ClipboardMonitor(_config);
            _monitor.ScreenshotSaved += OnScreenshotSaved;

            try
            {
                if (!Directory.Exists(_config.SaveDirectory))
                {
                    Directory.CreateDirectory(_config.SaveDirectory);
                }
            }
            catch { }
        }

        public void ShowWelcomeBalloon()
        {
            try
            {
                _notifyIcon.ShowBalloonTip(
                    2500,
                    "Saveshot запущен",
                    "Программа работает в фоновом режиме.\nНажмите Win + Shift + S, чтобы сделать снимок.",
                    ToolTipIcon.Info);
            }
            catch { }
        }

        private void BuildContextMenu()
        {
            var menu = new ContextMenu();

            var miOpen = new MenuItem("📁 Открыть папку скриншотов", (s, e) => OpenScreenshotsFolder());
            miOpen.DefaultItem = true;
            menu.MenuItems.Add(miOpen);

            menu.MenuItems.Add(new MenuItem("📸 Сделать снимок (Win+Shift+S)", (s, e) => LaunchSnipping()));
            menu.MenuItems.Add("-");

            _menuItemAutoSave = new MenuItem("✔ Автосохранение активно", (s, e) => ToggleAutoSave());
            _menuItemAutoSave.Checked = _config.AutoSaveEnabled;
            menu.MenuItems.Add(_menuItemAutoSave);

            menu.MenuItems.Add("-");
            menu.MenuItems.Add(new MenuItem("⚙ Настройки...", (s, e) => ShowSettings()));
            menu.MenuItems.Add(new MenuItem("ℹ О программе", (s, e) => ShowAbout()));
            menu.MenuItems.Add("-");
            menu.MenuItems.Add(new MenuItem("❌ Выход", (s, e) => ExitApplication()));

            _notifyIcon.ContextMenu = menu;
        }

        private void ToggleAutoSave()
        {
            _config.AutoSaveEnabled = !_config.AutoSaveEnabled;
            _menuItemAutoSave.Checked = _config.AutoSaveEnabled;
            _config.Save();

            string status = _config.AutoSaveEnabled ? "включено" : "приостановлено";
            _notifyIcon.ShowBalloonTip(1500, "Saveshot", "Автосохранение " + status, ToolTipIcon.Info);
        }

        private void OnScreenshotSaved(object sender, ScreenshotSavedEventArgs e)
        {
            try
            {
                _lastSavedFile = e.FilePath;

                // Play sound if enabled
                if (_config.PlaySound)
                {
                    SoundHelper.Play(_config.SoundType);
                }

                // Show balloon tip if enabled
                if (_config.ShowNotification)
                {
                    string name = Path.GetFileName(e.FilePath);
                    _notifyIcon.ShowBalloonTip(3000, "Скриншот сохранен", name + "\n(Нажмите, чтобы открыть в папке)", ToolTipIcon.Info);
                }
            }
            catch
            {
                // Never crash on notification/sound failure
            }
        }

        private void OnBalloonClicked()
        {
            try
            {
                if (!string.IsNullOrEmpty(_lastSavedFile) && File.Exists(_lastSavedFile))
                {
                    Process.Start("explorer.exe", "/select,\"" + _lastSavedFile + "\"");
                }
                else
                {
                    OpenScreenshotsFolder();
                }
            }
            catch
            {
                OpenScreenshotsFolder();
            }
        }

        private void OpenScreenshotsFolder()
        {
            try
            {
                if (!Directory.Exists(_config.SaveDirectory))
                {
                    Directory.CreateDirectory(_config.SaveDirectory);
                }
                Process.Start("explorer.exe", "\"" + _config.SaveDirectory + "\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось открыть папку: " + ex.Message, "Saveshot", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LaunchSnipping()
        {
            try
            {
                Process.Start("ms-screenclip:");
            }
            catch
            {
                try
                {
                    Process.Start("snippingtool.exe");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Не удалось запустить ножницы: " + ex.Message, "Saveshot", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        public void ShowSettings()
        {
            using (var dlg = new SettingsForm(_config))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    _menuItemAutoSave.Checked = _config.AutoSaveEnabled;
                }
            }
        }

        private void ShowAbout()
        {
            string msg = "Saveshot v1.0\n\n" +
                         "Легковесная нативная утилита автосохранения скриншотов Windows.\n" +
                         "Работает автоматически при нажатии Win + Shift + S.\n\n" +
                         "• 0% нагрузки на CPU\n" +
                         "• Мгновенное сохранение в выбранную папку\n" +
                         "• Запуск вместе с Windows";
            MessageBox.Show(msg, "О программе Saveshot", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExitApplication()
        {
            _monitor.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            Application.Exit();
        }
    }
}
