using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace BrainRingButtonsRegistrator
{
    class QuizApp
    {
        private SerialPort _serialPort;
        private const int MaxCandidates = 3;
        private List<int> _candidates;
        private Action<List<int>, bool, string> _updateLabels;
        private bool _paused;
        
        public bool FalseStartRegistration { get; set; }
        public event EventHandler Pause;
        public event EventHandler<int> FalseStartRegistered;

        public QuizApp(string portName, int baudRate, Action<List<int>, bool, string> updateLabels)
        {
            _serialPort = new SerialPort(portName, baudRate, Parity.Even);
            _serialPort.DataReceived += SerialPort_DataReceived;
            _candidates = new List<int>(MaxCandidates);
            _updateLabels = updateLabels;
        }
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_paused)
            {
                return;
            }

            int bytesToRead = _serialPort.BytesToRead;
            byte[] buffer = new byte[bytesToRead];
            _serialPort.Read(buffer, 0, bytesToRead);

            string receivedData = Encoding.ASCII.GetString(buffer);

            foreach (char c in receivedData)
            {
                if (c == 'E')
                {
                    Console.WriteLine("Error occurred.");
                    _updateLabels(null, true, receivedData);
                }
                else
                {
                    int teamNumber;
                    if (int.TryParse(c.ToString(), out teamNumber) && teamNumber >= 1 && teamNumber <= 8)
                    {
                        if (FalseStartRegistration)
                        {
                            FalseStartRegistered?.Invoke(this, teamNumber);
                        }
                        else
                        {
                            if (_candidates.Count < MaxCandidates && !_candidates.Contains(teamNumber))
                            {
                                _candidates.Add(teamNumber);
                                Console.WriteLine($"Team {teamNumber} is ready to answer! (Rank: {_candidates.Count})");

                                if (_candidates.Count == MaxCandidates)
                                {
                                    Console.WriteLine("All candidates registered. Please proceed.");
                                }
                                _updateLabels(_candidates, false, c.ToString());
                            }
                        }
                    }
                }
            }
            if (_candidates.Count < MaxCandidates && !FalseStartRegistration)
            {
                if (!_paused)
                {
                    Pause?.Invoke(this, EventArgs.Empty);
                    //_paused = true;
                }
            }
        }

        // ...


        public void ContinueReading()
        {
            _paused = false;
        }

        public void CheckFalseStart()
        {
            if (_serialPort.IsOpen)
            {
                FalseStartRegistration = true;
                char resetSignal = 'R';
                _serialPort.Write(new[] { resetSignal }, 0, 1);
                byte[] buffer = Encoding.ASCII.GetBytes("7");
                _serialPort.Write(buffer, 0, buffer.Length);
            }
        }

        public void Reset()
        {
            if (_serialPort.IsOpen)
            {
                FalseStartRegistration = false;
                char resetSignal = 'R';
                _serialPort.Write(new[] { resetSignal }, 0, 1);
                byte[] buffer = Encoding.ASCII.GetBytes("3");
                _serialPort.Write(buffer, 0, buffer.Length);
            }
        }

        public bool Start()
        {
            try
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();                    
                }
            }
            catch(Exception ex)
            {
                _updateLabels(null, true, ex.Message);
                return false;
            }
            return true;
        }       

        public void Stop()
        {
            try
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
            }
            catch (Exception ex)
            {
                _updateLabels(null, true, ex.Message);
                return;
            }            
        }
    }
}
