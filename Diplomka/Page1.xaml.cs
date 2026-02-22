using System;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;

namespace Diplomka
{
    /// <summary>
    /// Логика взаимодействия для Page1.xaml
    /// </summary>
    public partial class Page1 : Page
    {
        private MainWindow _owner;

        public Page1()
        {
            InitializeComponent();
        }

        public Page1(MainWindow owner) : this()
        {
            _owner = owner;
            Loaded += Page1_Loaded;
        }

        private void Page1_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshPorts();
            try { SimulateCheck.IsChecked = _owner?.IsSimulating ?? true; } catch { }
            try { if (!string.IsNullOrWhiteSpace(_owner?.CurrentPort)) PortCombo.SelectedItem = _owner.CurrentPort; } catch { }
        }

        private void RefreshPorts()
        {
            try
            {
                PortCombo.ItemsSource = SerialPort.GetPortNames();
            }
            catch { }
        }

        private void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            var port = PortCombo.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(port)) { StatusText.Text = "Select port"; return; }
            if (!int.TryParse(BaudText.Text, out var baud)) { StatusText.Text = "Bad baud"; return; }

            var ok = _owner.ConnectToPort(port, baud);
            StatusText.Text = ok ? "Connected" : "Connect failed";
        }

        private void DisconnectBtn_Click(object sender, RoutedEventArgs e)
        {
            _owner.DisconnectPort();
            StatusText.Text = "Disconnected";
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            var port = PortCombo.SelectedItem as string ?? string.Empty;
            if (!int.TryParse(BaudText.Text, out var baud)) baud = 9600;
            var simulate = SimulateCheck.IsChecked == true;
            _owner.SaveSettings(port, baud, simulate);
            _owner.SetSimulation(simulate);
            StatusText.Text = "Saved";
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            _owner.ClearHistory();
            StatusText.Text = "Cleared";
        }
    }
}
