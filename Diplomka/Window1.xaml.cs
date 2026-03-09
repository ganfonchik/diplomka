using Diplomka.Models;
using Diplomka.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace Diplomka
{
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
            LoadLogs();
        }

        private void LoadLogs()
        {
            try
            {
                string path = LogService.Instance.LogFilePath;

                if (!File.Exists(path))
                    return;

                var lines = File.ReadAllLines(path);

                List<LogRecord> logs = new List<LogRecord>();

                foreach (var line in lines)
                {
                    try
                    {
                        var record = JsonSerializer.Deserialize<LogRecord>(line);

                        if (record != null)
                            logs.Add(record);
                    }
                    catch { }
                }

                logs = logs.OrderByDescending(x => x.Timestamp).ToList();

                LogsGrid.ItemsSource = logs;
            }
            catch { }
        }
    }
}