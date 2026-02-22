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
using System.IO;
using System.Linq;

namespace Diplomka
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<double> TempValues { get; set; } = new();
        public ObservableCollection<double> HumidityValues { get; set; } = new();
        public ObservableCollection<double> LightValues { get; set; } = new();

        private SerialPort? _serialPort;
        private bool _isSimulating = true;
        private CartesianChart? _chart;
        // История DTO для таблицы
        private ObservableCollection<SensorDto> _sensorHistory = new();

        public MainWindow()
        {
            InitializeComponent();

            // Покажем доступные COM-порты для отладки
            try
            {
                TempCard.Text = "Ports: " + string.Join(",", SerialPort.GetPortNames());
            }
            catch { }

            // Начальные тестовые данные
            TempValues.Add(22);
            HumidityValues.Add(50);
            LightValues.Add(400);

            _chart = new CartesianChart
            {
                Series = new ISeries[]
                {
                    new LineSeries<double> { Values = TempValues, Name = "Температура" },
                    new LineSeries<double> { Values = HumidityValues, Name = "Влажность" },
                    new LineSeries<double> { Values = LightValues, Name = "Свет" }
                },
                LegendPosition = LiveChartsCore.Measure.LegendPosition.Top
            };

            ContentFrame.Content = _chart;
            // Убедиться, что таблица поверх фрейма
            try { Panel.SetZIndex(SensorTable, 1); Panel.SetZIndex(ContentFrame, 0); } catch { }

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            timer.Tick += (s, e) =>
            {
                if (!_isSimulating) return;

                var newTemp = TempValues[^1] + 0.2;
                var newHum = HumidityValues[^1] + 0.3;
                var newLight = LightValues[^1] + 5;

                TempValues.Add(newTemp);
                HumidityValues.Add(newHum);
                LightValues.Add(newLight);

                if (TempValues.Count > 10) TempValues.RemoveAt(0);
                if (HumidityValues.Count > 10) HumidityValues.RemoveAt(0);
                if (LightValues.Count > 10) LightValues.RemoveAt(0);
            };

            timer.Start();
            // Попробуем загрузить сохранённые настройки и подключиться
            LoadSettingsAndStart();
        }

        // --- Настройки API для Page1 ---
        public void SetSimulation(bool simulate)
        {
            _isSimulating = simulate;
            try { TempCard.Text = simulate ? "Simulation: on" : "Simulation: off"; } catch { }
        }

        public void ClearHistory()
        {
            _sensorHistory.Clear();
            TempValues.Clear();
            HumidityValues.Clear();
            LightValues.Clear();
            // вернуть начальные тестовые значения
            TempValues.Add(22);
            HumidityValues.Add(50);
            LightValues.Add(400);
        }

        public void DisconnectPort()
        {
            try
            {
                if (_serialPort != null)
                {
                    try { _serialPort.DataReceived -= SerialPort_DataReceived; } catch { }
                    try { _serialPort.Close(); } catch { }
                    _serialPort = null;
                }
            }
            catch { }
            _isSimulating = true;
            try { TempCard.Text = "Disconnected"; } catch { }
        }

        public bool ConnectToPort(string portName, int baud)
        {
            try
            {
                DisconnectPort();
                var sp = new SerialPort(portName, baud)
                {
                    NewLine = "\n",
                    ReadTimeout = 500
                };
                sp.DataReceived += SerialPort_DataReceived;
                sp.Open();
                _serialPort = sp;
                _isSimulating = false;
                try { TempCard.Text = $"Connected: {portName}@{baud}"; } catch { }
                return true;
            }
            catch (Exception ex)
            {
                try { File.AppendAllText("arduino_errors.log", DateTime.Now + $" ConnectToPort {portName}: " + ex + "\n"); } catch { }
                try { TempCard.Text = $"Connect failed: {portName}"; } catch { }
                return false;
            }
        }

        private void LoadSettingsAndStart()
        {
            try
            {
                if (File.Exists("settings.json"))
                {
                    var txt = File.ReadAllText("settings.json");
                    var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var settings = JsonSerializer.Deserialize<AppSettings>(txt, opts);
                    if (settings != null)
                    {
                        _isSimulating = settings.Simulate;
                        if (!string.IsNullOrWhiteSpace(settings.Port))
                        {
                            // попробуем подключиться к сохранённому порту
                            ConnectToPort(settings.Port, settings.BaudRate);
                        }
                    }
                }
                else
                {
                    // если нет настроек, запускаем автоматический поиск
                    StartSerial();
                }
            }
            catch { StartSerial(); }
        }

        public void SaveSettings(string port, int baud, bool simulate)
        {
            try
            {
                var settings = new AppSettings { Port = port, BaudRate = baud, Simulate = simulate };
                var txt = JsonSerializer.Serialize(settings);
                File.WriteAllText("settings.json", txt);
            }
            catch { }
        }

        private class AppSettings
        {
            public string? Port { get; set; }
            public int BaudRate { get; set; }
            public bool Simulate { get; set; }
        }

        private void StartSerial()
        {
            // Получаем список портов и даём приоритет COM3, если он присутствует
            var ports = SerialPort.GetPortNames().ToList();
            if (ports.Count == 0)
            {
                try { TempCard.Text = "Ports: (none)"; } catch { }
                return;
            }

            try { TempCard.Text = "Ports: " + string.Join(",", ports); } catch { }

            if (ports.Contains("COM3"))
            {
                ports.Remove("COM3");
                ports.Insert(0, "COM3");
            }

            foreach (var portName in ports)
            {
                try
                {
                    var sp = new SerialPort(portName, 9600)
                    {
                        NewLine = "\n",
                        ReadTimeout = 500
                    };

                    sp.DataReceived += SerialPort_DataReceived;
                    sp.Open();

                    _serialPort = sp;
                    // Отладочная индикация подключения
                    try { TempCard.Text = $"Connected: {portName}"; } catch { }
                    _isSimulating = false;
                    return;
                }
                catch (Exception ex)
                {
                    // Логируем попытки открытия портов
                    try { File.AppendAllText("arduino_errors.log", DateTime.Now + $" StartSerial try {portName}: " + ex + "\n"); } catch { }
                }
            }

            try { TempCard.Text = "No port opened"; } catch { }
        }

        private void SerialPort_DataReceived(object? sender, SerialDataReceivedEventArgs e)
        {
            var sp = (SerialPort?)sender;
            if (sp == null) return;

            try
            {
                var line = sp.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) return;

                // Покажем сырой входящий JSON для отладки
                try { Dispatcher.Invoke(() => TempCard.Text = line); } catch { }

                var dto = JsonSerializer.Deserialize<SensorDto>(line,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (dto == null) return;

                Dispatcher.Invoke(() =>
                {
                    int sensorCount = CountActiveSensors(dto);

                    if (sensorCount >= 5)
                    {
                        // Скрываем Frame целиком, чтобы он не перекрывал таблицу
                        ContentFrame.Visibility = Visibility.Collapsed;

                        // Показать таблицу и добавить запись в историю
                        SensorTable.Visibility = Visibility.Visible;
                        _sensorHistory.Insert(0, dto);
                        SensorTable.ItemsSource = _sensorHistory;
                        try { Panel.SetZIndex(SensorTable, 10); Panel.SetZIndex(ContentFrame, 0); }
                        catch { }
                        // Для отладки показываем число активных сенсоров
                        TempCard.Text = $"Sensors: {sensorCount}";
                    }
                    else
                    {
                        // Показываем Frame (график) и скрываем таблицу
                        ContentFrame.Visibility = Visibility.Visible;

                        SensorTable.Visibility = Visibility.Collapsed;

                        var temperature = dto.TempDht ?? dto.TempLm35 ?? 0;
                        var humidity = dto.Humidity ?? 0;
                        var light = dto.Light ?? 0;

                        TempValues.Add(temperature);
                        HumidityValues.Add(humidity);
                        LightValues.Add(light);

                        if (TempValues.Count > 10) TempValues.RemoveAt(0);
                        if (HumidityValues.Count > 10) HumidityValues.RemoveAt(0);
                        if (LightValues.Count > 10) LightValues.RemoveAt(0);
                    }
                });
            }
            catch (Exception ex)
            {
                try { File.AppendAllText("arduino_errors.log", DateTime.Now + " DataReceived: " + ex + "\n"); } catch { }
            }
        }

        private int CountActiveSensors(SensorDto dto)
        {
            int count = 0;

            if (dto.TempLm35.HasValue) count++;
            if (dto.TempDht.HasValue) count++;
            if (dto.Humidity.HasValue) count++;
            if (dto.Light.HasValue) count++;
            if (dto.Co2.HasValue) count++;
            if (dto.Water.HasValue) count++;
            if (dto.Sound.HasValue) count++;

            return count;
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

            [JsonPropertyName("water")]
            public double? Water { get; set; }

            [JsonPropertyName("sound")]
            public double? Sound { get; set; }
        }
        public bool IsSimulating => _isSimulating;
        public string? CurrentPort => _serialPort?.PortName;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Открыть страницу настроек в том же контейнере, передав ссылку на MainWindow
            try
            {
                ContentFrame?.Navigate(new Page1(this));
            }
            catch { }
        }
        //hfgsdhgfd
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // Показать основное содержимое (график)
            try
            {
                SensorTable.Visibility = Visibility.Collapsed;
                ContentFrame.Visibility = Visibility.Visible;
                if (_chart != null)
                    ContentFrame.Content = _chart;
            }
            catch { }
        }
    }
}