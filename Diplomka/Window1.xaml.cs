using Diplomka.Services;
using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace Diplomka
{
    public partial class Window1 : Window
    {
        private DispatcherTimer _timer = new DispatcherTimer();

        public Window1()
        {
            InitializeComponent();
            LoadLogs();

            _timer.Interval = TimeSpan.FromSeconds(2);
            _timer.Tick += (s, e) => LoadLogs();
            _timer.Start();
        }

        private void LoadLogs()
        {
            try
            {
                string path = LogService.Instance.LogFilePath;

                if (File.Exists(path))
                {
                    LogsTextBox.Text = File.ReadAllText(path);
                    LogsTextBox.ScrollToEnd();
                }
                else
                {
                    LogsTextBox.Text = "Файл логов пока не создан.";
                }
            }
            catch (Exception ex)
            {
                LogsTextBox.Text = "Ошибка чтения логов: " + ex.Message;
            }
        }
    }
}