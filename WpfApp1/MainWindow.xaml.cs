using System.ComponentModel;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;

namespace DeviceSimulator
{
    public partial class MainWindow : Window
    {
        private SerialPort _serialPort;

        public MainWindow()
        {
            InitializeComponent();
            PortList.ItemsSource = SerialPort.GetPortNames();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string portName = (string)PortList.SelectedItem;
            _serialPort = new SerialPort(portName, 19200, Parity.Even, 8, StopBits.One);
            _serialPort.Open();

            ConnectButton.IsEnabled = false;
            DisconnectButton.IsEnabled = true;
            SendResetButton.IsEnabled = true;
            CheckBox1.IsEnabled = true;
            CheckBox2.IsEnabled = true;
            CheckBox3.IsEnabled = true;
            CheckBox4.IsEnabled = true;
            CheckBox5.IsEnabled = true;
            CheckBox6.IsEnabled = true;
            CheckBox7.IsEnabled = true;
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            _serialPort.Close();

            ConnectButton.IsEnabled = true;
            DisconnectButton.IsEnabled = false;
            SendResetButton.IsEnabled = false;
            CheckBox1.IsEnabled = false;
            CheckBox2.IsEnabled = false;
            CheckBox3.IsEnabled = false;
            CheckBox4.IsEnabled = false;
            CheckBox5.IsEnabled = false;
            CheckBox6.IsEnabled = false;
            CheckBox7.IsEnabled = false;
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.IsChecked == true)
            {

                string buttonNumber = checkBox.Content.ToString().Split(' ')[1];
                _serialPort.Write(buttonNumber);
                checkBox.IsChecked = false;
            }
        }

        private void SendResetButton_Click(object sender, RoutedEventArgs e)
        {
            bool allButtonsReleased = CheckBox1.IsChecked == false &&
                                      CheckBox2.IsChecked == false &&
                                      CheckBox3.IsChecked == false &&
                                      CheckBox4.IsChecked == false &&
                                      CheckBox5.IsChecked == false &&
                                      CheckBox6.IsChecked == false &&
                                      CheckBox7.IsChecked == false;

            if (allButtonsReleased)
            {
                _serialPort.Write("R");
            }
            else
            {
                _serialPort.Write("E");
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
            }

            base.OnClosing(e);
        }
    }
}

