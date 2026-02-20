using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WPF;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Diplomka
{
    public partial class MainWindow : Window
    {
        // Данные для графика
        public ObservableCollection<double> TempValues { get; set; } = new();
        public ObservableCollection<double> HumidityValues { get; set; } = new();
        public ObservableCollection<double> Co2Values { get; set; } = new();
        private SerialPort? _serialPort;
        private bool _isSimulating = true;

        public MainWindow()
        {
            InitializeComponent();

            // Начальные данные
            TempValues.Add(22); TempValues.Add(23); TempValues.Add(22.5);
            HumidityValues.Add(50); HumidityValues.Add(52); HumidityValues.Add(49);
            Co2Values.Add(400); Co2Values.Add(410); Co2Values.Add(405);

            // Создаем серии
            var series = new ISeries[]
            {
                new LineSeries<double> { Values = TempValues, Name = "Температура" },
                new LineSeries<double> { Values = HumidityValues, Name = "Влажность" },
                new LineSeries<double> { Values = Co2Values, Name = "CO₂" }
            };

            // Создаем график программно
            var chart = new CartesianChart
            {
                Series = series,
                LegendPosition = LiveChartsCore.Measure.LegendPosition.Top
            };

            // Помещаем график во второй ряд Grid
            Grid.SetRow(chart, 1);
            MainContentGrid.Children.Add(chart);

            // Таймер для обновления данных
            var timer = new DispatcherTimer
            {
                Interval = System.TimeSpan.FromSeconds(1)
            };
            timer.Tick += (s, e) =>
            {
                if (!_isSimulating) return; // если подключен Arduino, не симулируем

                // Генерация новых данных (пример)
                var newTemp = TempValues[^1] + 0.5;
                var newHum = HumidityValues[^1] + 0.3;
                var newCo2 = Co2Values[^1] + 1;

                TempValues.Add(newTemp);
                HumidityValues.Add(newHum);
                Co2Values.Add(newCo2);

                // Ограничиваем до 10 последних точек
                if (TempValues.Count > 10) TempValues.RemoveAt(0);
                if (HumidityValues.Count > 10) HumidityValues.RemoveAt(0);
                if (Co2Values.Count > 10) Co2Values.RemoveAt(0);

                // Обновляем карточки
                TempCard.Text = $"{newTemp:F1} °C";
                HumidityCard.Text = $"{newHum:F1} %";
                Co2Card.Text = $"{newCo2} ppm";
            };
            timer.Start();

            // Попытаться подключиться к Arduino (асинхронно через попытки открытия доступных COM-портов)
            StartSerial();
        }

        private void StartSerial()
        {
            // Попробуем сначала явно COM3 (если он есть), затем остальные
            var ports = SerialPort.GetPortNames().ToList();
            if (ports.Contains("COM3"))
            {
                ports.Remove("COM3");
                ports.Insert(0, "COM3");
            }

            foreach (var portName in ports)
            {
                try
                {
                    // Arduino code использует 9600 в приведённом скетче
                    var sp = new SerialPort(portName, 9600) { NewLine = "\n", ReadTimeout = 500 };
                    sp.DataReceived += SerialPort_DataReceived;
                    sp.Open();
                    // успешно открыт - сохраняем и прекращаем симуляцию
                    _serialPort = sp;
                    _isSimulating = false;
                    TempCard.Text = $"Connected: {portName} @9600";
                    return;
                }
                catch
                {
                    // попытка не удалась, пробуем следующий порт
                }
            }
        }

        private void SerialPort_DataReceived(object? sender, SerialDataReceivedEventArgs e)
        {
            var sp = (SerialPort?)sender;
            if (sp == null) return;

            try
            {
                var line = sp.ReadLine(); // ожидаем JSON на строке
                if (string.IsNullOrWhiteSpace(line)) return;

                var dto = JsonSerializer.Deserialize<SensorDto>(line, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (dto == null) return;

                // Пришли реальные данные — отключаем симуляцию
                _isSimulating = false;

                Dispatcher.Invoke(() =>
                {
                    // Обновляем коллекции и UI
                    var temperature = dto.TempDht ?? dto.TempLm35 ?? 0.0;
                    var humidity = dto.Humidity ?? 0.0;
                    TempValues.Add(temperature);
                    HumidityValues.Add(humidity);
                    // если есть свет, используем его как третью серию
                    if (dto.Light.HasValue && dto.Light.Value != 0)
                        Co2Values.Add(dto.Light.Value);
                    else
                        Co2Values.Add(0);

                    if (TempValues.Count > 10) TempValues.RemoveAt(0);
                    if (HumidityValues.Count > 10) HumidityValues.RemoveAt(0);
                    if (Co2Values.Count > 10) Co2Values.RemoveAt(0);

                    TempCard.Text = $"{temperature:F1} °C";
                    HumidityCard.Text = $"{humidity:F1} %";
                    if (dto.Light.HasValue && dto.Light.Value != 0)
                        Co2Card.Text = $"{dto.Light.Value:F0} lx";
                    else
                        Co2Card.Text = "--";
                });
            }
            catch
            {
                // игнорируем ошибки чтения/парсинга
            }
        }

        private class SensorDto
        {
            [JsonPropertyName("temp_lm35")]
            public double? TempLm35 { get; set; }

            [JsonPropertyName("temp_dht")]
            public double? TempDht { get; set; }

            [JsonPropertyName("humidity")]
            public double? Humidity { get; set; }

            [JsonPropertyName("light")]
            public double? Light { get; set; }

            [JsonPropertyName("co2")]
            public double? Co2 { get; set; }
        }
    }
}
