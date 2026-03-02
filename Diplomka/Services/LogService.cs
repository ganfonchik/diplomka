using System;
using System.IO;
using System.Windows.Threading;
using Diplomka.Models;

namespace Diplomka.Services
{
    public class LogService
    {
        private static readonly Lazy<LogService> _lazy = new(() => new LogService());
        public static LogService Instance => _lazy.Value;

        private SensorDto? _lastDto;
        private DateTime? _lastTime;
        private readonly DispatcherTimer _timer;
        // Путь к файлу логов (можно использовать для отладки)
        public string LogFilePath => Path.Combine(Directory.GetCurrentDirectory(), "logs.jsonl");

        private LogService()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromHours(1) };
            _timer.Tick += (s, e) =>
            {
                try { OnTimerTick(); } catch { }
            };
            _timer.Start();
        }

        public void Notify(SensorDto dto)
        {
            _lastDto = dto;
            _lastTime = DateTime.Now;
            // Если файл ещё не создан — создаём первую запись сразу, чтобы было что открыть во время отладки
            try
            {
                if (!File.Exists(LogFilePath))
                {
                    SaveDtoToDatabase(dto, _lastTime.Value);
                }
            }
            catch { }
        }

        private void OnTimerTick()
        {
            try
            {
                if (_lastDto == null || _lastTime == null) return;
                var cutoff = DateTime.Now - TimeSpan.FromHours(1);
                if (_lastTime >= cutoff)
                {
                    SaveDtoToDatabase(_lastDto, _lastTime.Value);
                }
            }
            catch { }
        }

        // Fallback logger: append JSON line to "logs.jsonl". If you need MDF/LocalDB support,
        // add Microsoft.Data.SqlClient NuGet package and replace this implementation.
        private void SaveDtoToDatabase(SensorDto dto, DateTime timestamp)
        {
            try
            {
                var outPath = Path.Combine(Directory.GetCurrentDirectory(), "logs.jsonl");
                var record = new
                {
                    Timestamp = timestamp,
                    Temp = dto.TempDht ?? dto.TempLm35,
                    Humidity = dto.Humidity,
                    Light = dto.Light,
                    Co2 = dto.Co2,
                    Water = dto.Water,
                    Sound = dto.Sound
                };
                var line = System.Text.Json.JsonSerializer.Serialize(record);
                File.AppendAllText(outPath, line + "\n");
            }
            catch (Exception ex)
            {
                try { File.AppendAllText("logs_error.log", DateTime.Now + " SaveDtoToDatabase: " + ex + "\n"); } catch { }
            }
        }
    }
}
