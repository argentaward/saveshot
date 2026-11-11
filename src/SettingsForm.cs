using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Saveshot
{
    public class SettingsForm : Form
    {
        private readonly AppConfig _config;

        private TextBox _txtFolder;
        private Button _btnBrowse;
        private Button _btnOpenFolder;

        private ComboBox _cmbFormat;
        private TextBox _txtPattern;
        private Label _lblPatternPreview;

        private RadioButton _rbKeepImage;
        private RadioButton _rbCopyPath;
        private RadioButton _rbCopyFile;

        private CheckBox _chkAutoStart;
        private CheckBox _chkNotification;
        private CheckBox _chkAutoSave;

        private CheckBox _chkSound;
        private ComboBox _cmbSound;
        private Button _btnTestSound;

        private Button _btnSnipping;
        private Button _btnSave;
        private Button _btnCancel;

        public SettingsForm(AppConfig config)
        {
            _config = config;
            InitializeComponent();
            LoadConfigToUi();
        }

        private void InitializeComponent()
        {
            this.Text = "Saveshot — Настройки";
            this.Size = new Size(540, 580);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.BackColor = Color.FromArgb(248, 249, 250);

            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
                }
            }
            catch { }

            int pad = 16;
            int curY = 14;
            int contentWidth = 490;

            // --- Group 1: Storage ---
            GroupBox grpStorage = new GroupBox
            {
                Text = "Куда сохранять скриншоты",
                Location = new Point(pad, curY),
                Size = new Size(contentWidth, 95),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            Label lblFolder = new Label
            {
                Text = "Папка назначения:",
                Location = new Point(14, 24),
                AutoSize = true
            };
            grpStorage.Controls.Add(lblFolder);

            _txtFolder = new TextBox
            {
                Location = new Point(14, 48),
                Size = new Size(300, 25)
            };
            grpStorage.Controls.Add(_txtFolder);

            _btnBrowse = new Button
            {
                Text = "Обзор...",
                Location = new Point(322, 47),
                Size = new Size(78, 27),
                UseVisualStyleBackColor = true
            };
            _btnBrowse.Click += (s, e) => BrowseFolder();
            grpStorage.Controls.Add(_btnBrowse);

            _btnOpenFolder = new Button
            {
                Text = "Открыть",
                Location = new Point(405, 47),
                Size = new Size(72, 27),
                UseVisualStyleBackColor = true
            };
            _btnOpenFolder.Click += (s, e) => OpenCurrentFolder();
            grpStorage.Controls.Add(_btnOpenFolder);

            this.Controls.Add(grpStorage);
            curY += grpStorage.Height + 10;

            // --- Group 2: Format & Naming ---
            GroupBox grpFormat = new GroupBox
            {
                Text = "Формат и имя файла",
                Location = new Point(pad, curY),
                Size = new Size(contentWidth, 110),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            Label lblFormat = new Label
            {
                Text = "Формат:",
                Location = new Point(14, 24),
                AutoSize = true
            };
            grpFormat.Controls.Add(lblFormat);

            _cmbFormat = new ComboBox
            {
                Location = new Point(80, 21),
                Size = new Size(160, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbFormat.Items.AddRange(new object[] { "PNG (Без потерь)", "JPEG (Сжатый)", "BMP" });
            _cmbFormat.SelectedIndexChanged += (s, e) => UpdatePreview();
            grpFormat.Controls.Add(_cmbFormat);

            Label lblPattern = new Label
            {
                Text = "Шаблон имени:",
                Location = new Point(14, 53),
                AutoSize = true
            };
            grpFormat.Controls.Add(lblPattern);

            _txtPattern = new TextBox
            {
                Location = new Point(120, 50),
                Size = new Size(220, 25)
            };
            _txtPattern.TextChanged += (s, e) => UpdatePreview();
            grpFormat.Controls.Add(_txtPattern);

            _lblPatternPreview = new Label
            {
                Text = "Пример: Screenshot_2026-11-11_17-30-00.png",
                Location = new Point(14, 80),
                Size = new Size(460, 20),
                ForeColor = Color.DimGray
            };
            grpFormat.Controls.Add(_lblPatternPreview);

            this.Controls.Add(grpFormat);
            curY += grpFormat.Height + 10;

            // --- Group 3: Clipboard Action ---
            GroupBox grpClipboard = new GroupBox
            {
                Text = "После сохранения в буфере обмена:",
                Location = new Point(pad, curY),
                Size = new Size(contentWidth, 88),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _rbKeepImage = new RadioButton
            {
                Text = "Оставлять изображение (стандартно)",
                Location = new Point(14, 20),
                AutoSize = true
            };
            grpClipboard.Controls.Add(_rbKeepImage);

            _rbCopyPath = new RadioButton
            {
                Text = "Копировать путь к сохраненному файлу",
                Location = new Point(14, 41),
                AutoSize = true
            };
            grpClipboard.Controls.Add(_rbCopyPath);

            _rbCopyFile = new RadioButton
            {
                Text = "Копировать сам файл (для вставки в Проводник, Telegram, Discord)",
                Location = new Point(14, 62),
                AutoSize = true
            };
            grpClipboard.Controls.Add(_rbCopyFile);

            this.Controls.Add(grpClipboard);
            curY += grpClipboard.Height + 10;

            // --- Group 4: Options & Sound ---
            GroupBox grpOptions = new GroupBox
            {
                Text = "Параметры, звук и уведомления",
                Location = new Point(pad, curY),
                Size = new Size(contentWidth, 130),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _chkAutoSave = new CheckBox
            {
                Text = "Автосохранение активно (ловить снимки Win+Shift+S)",
                Location = new Point(14, 20),
                AutoSize = true
            };
            grpOptions.Controls.Add(_chkAutoSave);

            _chkAutoStart = new CheckBox
            {
                Text = "Запускать Saveshot при входе в Windows",
                Location = new Point(14, 42),
                AutoSize = true
            };
            grpOptions.Controls.Add(_chkAutoStart);

            _chkNotification = new CheckBox
            {
                Text = "Всплывающее уведомление в трее",
                Location = new Point(14, 64),
                AutoSize = true
            };
            grpOptions.Controls.Add(_chkNotification);

            _chkSound = new CheckBox
            {
                Text = "Звук при сохранении:",
                Location = new Point(14, 91),
                AutoSize = true
            };
            _chkSound.CheckedChanged += (s, e) => ToggleSoundControls();
            grpOptions.Controls.Add(_chkSound);

            _cmbSound = new ComboBox
            {
                Location = new Point(170, 88),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbSound.Items.AddRange(new object[] {
                "Мягкий клик (тихий)",
                "Мелодичный сигнал (Notify)",
                "Колокольчик (Ding)",
                "Всплывающий щелчок (Pop-up)",
                "Без звука"
            });
            grpOptions.Controls.Add(_cmbSound);

            _btnTestSound = new Button
            {
                Text = "♪ Тест",
                Location = new Point(380, 87),
                Size = new Size(80, 27),
                UseVisualStyleBackColor = true
            };
            _btnTestSound.Click += (s, e) => PlaySelectedTestSound();
            grpOptions.Controls.Add(_btnTestSound);

            this.Controls.Add(grpOptions);
            curY += grpOptions.Height + 14;

            // --- Bottom Buttons ---
            _btnSnipping = new Button
            {
                Text = "✂ Сделать снимок",
                Location = new Point(pad, curY),
                Size = new Size(140, 32),
                UseVisualStyleBackColor = true
            };
            _btnSnipping.Click += (s, e) => LaunchSnipping();
            this.Controls.Add(_btnSnipping);

            _btnSave = new Button
            {
                Text = "Сохранить",
                Location = new Point(contentWidth - 165, curY),
                Size = new Size(95, 32),
                UseVisualStyleBackColor = true
            };
            _btnSave.Click += (s, e) => SaveAndClose();
            this.Controls.Add(_btnSave);

            _btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(contentWidth - 62, curY),
                Size = new Size(80, 32),
                UseVisualStyleBackColor = true
            };
            _btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(_btnCancel);

            this.AcceptButton = _btnSave;
            this.CancelButton = _btnCancel;
        }

        private void ToggleSoundControls()
        {
            _cmbSound.Enabled = _chkSound.Checked;
            _btnTestSound.Enabled = _chkSound.Checked;
        }

        private void PlaySelectedTestSound()
        {
            string soundType = GetSelectedSoundType();
            SoundHelper.Play(soundType);
        }

        private string GetSelectedSoundType()
        {
            switch (_cmbSound.SelectedIndex)
            {
                case 0: return "Click";
                case 1: return "Notify";
                case 2: return "Ding";
                case 3: return "Popup";
                default: return "None";
            }
        }

        private void LoadConfigToUi()
        {
            _txtFolder.Text = _config.SaveDirectory;

            if (_config.ImageFormat.Equals("JPEG", StringComparison.OrdinalIgnoreCase) ||
                _config.ImageFormat.Equals("JPG", StringComparison.OrdinalIgnoreCase))
                _cmbFormat.SelectedIndex = 1;
            else if (_config.ImageFormat.Equals("BMP", StringComparison.OrdinalIgnoreCase))
                _cmbFormat.SelectedIndex = 2;
            else
                _cmbFormat.SelectedIndex = 0; // PNG

            _txtPattern.Text = _config.FileNamePattern;

            switch (_config.ClipboardAction)
            {
                case ClipboardAction.CopyFilePath:
                    _rbCopyPath.Checked = true;
                    break;
                case ClipboardAction.CopyFileDrop:
                    _rbCopyFile.Checked = true;
                    break;
                default:
                    _rbKeepImage.Checked = true;
                    break;
            }

            _chkAutoSave.Checked = _config.AutoSaveEnabled;
            _chkAutoStart.Checked = RegistryHelper.IsAutoStartEnabled();
            _chkNotification.Checked = _config.ShowNotification;
            _chkSound.Checked = _config.PlaySound;

            string st = _config.SoundType ?? "Click";
            switch (st.ToLowerInvariant())
            {
                case "notify":
                    _cmbSound.SelectedIndex = 1;
                    break;
                case "ding":
                    _cmbSound.SelectedIndex = 2;
                    break;
                case "popup":
                    _cmbSound.SelectedIndex = 3;
                    break;
                case "none":
                    _cmbSound.SelectedIndex = 4;
                    break;
                default:
                    _cmbSound.SelectedIndex = 0; // Soft Click
                    break;
            }

            ToggleSoundControls();
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            try
            {
                string fmt = "PNG";
                if (_cmbFormat.SelectedIndex == 1) fmt = "JPEG";
                if (_cmbFormat.SelectedIndex == 2) fmt = "BMP";

                var tempConfig = new AppConfig
                {
                    FileNamePattern = _txtPattern.Text,
                    ImageFormat = fmt
                };
                string sample = tempConfig.GenerateFileName(DateTime.Now);
                _lblPatternPreview.Text = "Пример: " + sample;
            }
            catch
            {
                _lblPatternPreview.Text = "Пример: Screenshot_error.png";
            }
        }

        private void BrowseFolder()
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Выберите папку для сохранения скриншотов";
                dlg.SelectedPath = _txtFolder.Text;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _txtFolder.Text = dlg.SelectedPath;
                }
            }
        }

        private void OpenCurrentFolder()
        {
            try
            {
                string dir = _txtFolder.Text;
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                Process.Start("explorer.exe", "\"" + dir + "\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Не удалось открыть папку: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    MessageBox.Show(this, "Не удалось запустить ножницы: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void SaveAndClose()
        {
            string folder = _txtFolder.Text.Trim();
            if (string.IsNullOrEmpty(folder))
            {
                MessageBox.Show(this, "Укажите папку для сохранения скриншотов!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _config.SaveDirectory = folder;

            if (_cmbFormat.SelectedIndex == 1) _config.ImageFormat = "JPEG";
            else if (_cmbFormat.SelectedIndex == 2) _config.ImageFormat = "BMP";
            else _config.ImageFormat = "PNG";

            _config.FileNamePattern = string.IsNullOrEmpty(_txtPattern.Text.Trim()) ? "Screenshot_{date}_{time}" : _txtPattern.Text.Trim();

            if (_rbCopyPath.Checked) _config.ClipboardAction = ClipboardAction.CopyFilePath;
            else if (_rbCopyFile.Checked) _config.ClipboardAction = ClipboardAction.CopyFileDrop;
            else _config.ClipboardAction = ClipboardAction.KeepImage;

            _config.AutoSaveEnabled = _chkAutoSave.Checked;
            _config.StartWithWindows = _chkAutoStart.Checked;
            _config.ShowNotification = _chkNotification.Checked;
            _config.PlaySound = _chkSound.Checked;
            _config.SoundType = GetSelectedSoundType();

            // Apply autostart
            RegistryHelper.SetAutoStart(_config.StartWithWindows);

            // Save config to disk
            _config.Save();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
