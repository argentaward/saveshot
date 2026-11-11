using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Saveshot
{
    static class Program
    {
        private const string AppMutexName = "Saveshot_SingleInstance_Mutex_991827364";

        [STAThread]
        static void Main(string[] args)
        {
            bool isNewInstance = false;
            Mutex mutex = null;

            try
            {
                mutex = new Mutex(true, AppMutexName, out isNewInstance);
            }
            catch (AbandonedMutexException)
            {
                isNewInstance = true;
            }

            if (!isNewInstance)
            {
                MessageBox.Show(
                    "Saveshot уже запущен!\nИщите значок камеры в системном трее (возле часов).",
                    "Saveshot",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                var config = AppConfig.Load();

                bool startInTray = false;
                bool showSettings = false;

                foreach (string arg in args)
                {
                    if (string.Equals(arg, "--tray", StringComparison.OrdinalIgnoreCase))
                        startInTray = true;
                    if (string.Equals(arg, "--settings", StringComparison.OrdinalIgnoreCase))
                        showSettings = true;
                }

                var context = new TrayApplicationContext(config);

                if (showSettings)
                {
                    context.ShowSettings();
                }
                else if (!startInTray)
                {
                    context.ShowWelcomeBalloon();
                }

                Application.Run(context);
            }
            finally
            {
                if (mutex != null)
                {
                    try { mutex.ReleaseMutex(); } catch { }
                    mutex.Dispose();
                }
            }
        }
    }
}
