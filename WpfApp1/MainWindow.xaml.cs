using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;

namespace DeviceSimulator
{
    public partial class MainWindow : Window
    {
        private SerialPort _serialPort;
        private const string PortName = "COM1"; // Замените на имя виртуального COM-порта
        private const int BaudRate = 9600;
        private int _maxRegistrations = 3;
        private Dictionary<int, int> _buttonRegistrations = new Dictionary<int, int>();

        public MainWindow()
        {
            InitializeComponent();
            InitializeButtonRegistrations();
            RegisterCheckBoxEventHandlers();
            PortList.ItemsSource = SerialPort.GetPortNames();
        }

        private void InitializeButtonRegistrations()
        {
            for (int i = 1; i <= 7; i++)
            {
                _buttonRegistrations[i] = 0;
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string portName = (string)PortList.SelectedItem;
            _serialPort = new SerialPort(portName, BaudRate, Parity.None, 8, StopBits.One);
            _serialPort.DataReceived += SerialPort_DataReceived;
            _serialPort.Open();

            UpdateButtonStates(true);
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            _serialPort.Close();
            UpdateButtonStates(false);
        }

        private void UpdateButtonStates(bool isConnected)
        {
            ConnectButton.IsEnabled = !isConnected;
            DisconnectButton.IsEnabled = isConnected;
            SendResetButton.IsEnabled = isConnected;
            CheckBox1.IsEnabled = isConnected;
            CheckBox2.IsEnabled = isConnected;
            CheckBox3.IsEnabled = isConnected;
            CheckBox4.IsEnabled = isConnected;
            CheckBox5.IsEnabled = isConnected;
            CheckBox6.IsEnabled = isConnected;
            CheckBox7.IsEnabled = isConnected;
        }

        private void RegisterCheckBoxEventHandlers()
        {
            CheckBox1.Checked += CheckBox_Checked;
            CheckBox2.Checked += CheckBox_Checked;
            CheckBox3.Checked += CheckBox_Checked;
            CheckBox4.Checked += CheckBox_Checked;
            CheckBox5.Checked += CheckBox_Checked;
            CheckBox6.Checked += CheckBox_Checked;
            CheckBox7.Checked += CheckBox_Checked;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            if ((bool)checkBox.IsChecked)
            {
                checkBox.IsEnabled = false;
            }
            int buttonNumber = int.Parse(checkBox.Tag.ToString());

            if (_buttonRegistrations[buttonNumber] < _maxRegistrations)
            {
                _buttonRegistrations[buttonNumber]++;
                _serialPort.Write(buttonNumber.ToString());                
            }
            else
            {
                // Отключаем все CheckBox-и после отправки номера команды
                SetCheckBoxEnabledState(false);
            }
        }

        private void SetCheckBoxEnabledState(bool isEnabled)
        {
            CheckBox1.IsEnabled = isEnabled;
            CheckBox2.IsEnabled = isEnabled;
            CheckBox3.IsEnabled = isEnabled;
            CheckBox4.IsEnabled = isEnabled;
            CheckBox5.IsEnabled = isEnabled;
            CheckBox6.IsEnabled = isEnabled;
            CheckBox7.IsEnabled = isEnabled;
        }


        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string data = _serialPort.ReadExisting();
            char command = data[0];

            if (command == 'R')
            {
                // Включаем все CheckBox-и после выполнения сброса
                Dispatcher.BeginInvoke(() => SetCheckBoxEnabledState(true));
                Dispatcher.Invoke(() =>
                {
                    bool allButtonsReleased = CheckBox1.IsChecked == false &&
                                              CheckBox2.IsChecked == false &&
                                              CheckBox3.IsChecked == false &&
                                              CheckBox4.IsChecked == false &&
                                              CheckBox5.IsChecked == false &&
                                              CheckBox6.IsChecked == false &&
                                              CheckBox7.IsChecked == false;

                    if (!allButtonsReleased)
                    {
                        _serialPort.Write("E");
                    }
                    else
                    {
                        InitializeButtonRegistrations();

                        if (data.Length > 1)
                        {
                            int.TryParse(data[1].ToString(), out _maxRegistrations);
                        }
                    }
                });
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

