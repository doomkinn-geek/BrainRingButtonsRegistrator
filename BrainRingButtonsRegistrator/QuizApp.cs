using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BrainRingButtonsRegistrator
{
    class QuizApp
    {
        private SerialPort _serialPort;
        private const int MaxCandidates = 3;
        private List<int> _candidates;
        private Action<List<int>, bool, string> _updateLabels;
        private bool _paused;        
        private CancellationTokenSource _cancellationTokenSource;


        public bool ReadingQuestion { get; private set; }
        public bool FalseStartRegistration { get; set; }
        public event EventHandler ErrorReceived;
        public event EventHandler Pause;
        public event EventHandler<int> AnswerCandidateRegistered;
        public event EventHandler<int> FalseStartRegistered;

        public List<int> Candidates => _candidates;


        public QuizApp(string portName, int baudRate, Action<List<int>, bool, string> updateLabels)
        {
            _serialPort = new SerialPort(portName, baudRate, Parity.Even);
            _serialPort.DataReceived += SerialPort_DataReceived;
            _candidates = new List<int>(MaxCandidates);
            _updateLabels = updateLabels;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartReadingQuestion()
        {
            ReadingQuestion = true;

            // Очистите список кандидатов перед началом чтения вопроса
            _candidates.Clear();

            // Отправьте команду 'R' и число 7 для активации режима фальш-старта
            await SendCommandAsync('R', 3);
        }

        private async void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_paused)
            {
                return;
            }

            int bytesToRead = _serialPort.BytesToRead;
            byte[] buffer = new byte[bytesToRead];
            await _serialPort.BaseStream.ReadAsync(buffer, 0, bytesToRead);

            string receivedData = Encoding.ASCII.GetString(buffer);
            _updateLabels(null, true, receivedData);

            foreach (char c in receivedData)
            {
                if (ReadingQuestion)
                {
                    if (c == 'E')
                    {
                        Console.WriteLine("Error occurred.");
                        ErrorReceived?.Invoke(this, EventArgs.Empty);                        
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

                                    _updateLabels(_candidates, false, c.ToString());

                                    // Вызываем событие AnswerCandidateRegistered
                                    AnswerCandidateRegistered?.Invoke(this, teamNumber);

                                    if (_candidates.Count == MaxCandidates)
                                    {
                                        Console.WriteLine("All candidates registered. Please proceed.");
                                        Pause?.Invoke(this, EventArgs.Empty);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }



        public async Task<int> RegisterAnswerCandidate()
        {
            // Реализуйте логику для регистрации команды-кандидата на ответ.
            // Например, вы можете отправить команду и получить номер команды, которая нажала кнопку.
            // В этом примере мы просто возвращаем случайный номер команды.
            await Task.Delay(100); // Имитация задержки
            return new Random().Next(1, 8);
        }


        public async Task<int> RegisterFalseStartCandidate()
        {
            try
            {
                // Отправьте команду 'R' и число 7 для активации режима фальш-старта
                await SendCommandAsync('R', 7);

                // Ожидайте ответа от устройства в течение 1 секунды
                var timeout = TimeSpan.FromSeconds(1);
                var stopwatch = Stopwatch.StartNew();
                while (stopwatch.Elapsed < timeout)
                {
                    var response = await ReadResponseAsync();

                    // Если получен символ 'E', вернуть номер команды, которая нажала кнопку
                    if (response.StartsWith("E"))
                    {
                        int.TryParse(response.Substring(1), out int candidate);
                        return candidate;
                    }
                }

                // Если устройство не вернуло символ 'E', вернуть -1, чтобы указать, что фальш-старт не зарегистрирован
                return -1;
            }
            catch (Exception ex)
            {
                _updateLabels(_candidates, false, ex.ToString());
                return -1;
            }
        }

        public async Task SendCommandAsync(char command, int data)
        {
            /*// Отправьте команду на аппаратное обеспечение
            string commandToSend = $"{command}";
            byte[] buffer = Encoding.ASCII.GetBytes(commandToSend);
            //await _serialPort.BaseStream.WriteAsync(buffer, 0, buffer.Length, _cancellationTokenSource.Token);            
            _serialPort.BaseStream.Write(buffer, 0, buffer.Length);
            string commandToSend2 = $"{data}";
            byte[] buffer2 = Encoding.ASCII.GetBytes(commandToSend2);
            //await _serialPort.BaseStream.WriteAsync(buffer2, 0, buffer2.Length, _cancellationTokenSource.Token);            
            _serialPort.BaseStream.Write(buffer2, 0, buffer2.Length);*/            
            _serialPort.Write(new[] { command }, 0, 1);
            //byte[] buffer = BitConverter.GetBytes(data);
            byte[] buffer = Encoding.ASCII.GetBytes(data.ToString());
            _serialPort.Write(buffer, 0, buffer.Length);
        }

        private async Task<string> ReadResponseAsync()
        {
            byte[] buffer = new byte[1024];
            int bytesRead = await _serialPort.BaseStream.ReadAsync(buffer, 0, buffer.Length, _cancellationTokenSource.Token);
            string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            if(response.StartsWith("E"))
            {
                ErrorReceived?.Invoke(this, EventArgs.Empty);
            }
            return response;
        }


        public async Task StartRegistrationAsync()
        {
            // Очистите список кандидатов перед началом регистрации
            _candidates.Clear();

            // Установите ReadingQuestion в false, чтобы разрешить регистрацию
            ReadingQuestion = false;
            // Отправьте команду 'R' и число 0 для активации режима регистрации
            await SendCommandAsync('R', 0);
            // Чтение ответов с устройства
            await ReadResponseAsync();
        }

        public void ContinueReading()
        {
            _paused = false;
        }

        public void Reset()
        {
            if (_serialPort.IsOpen)
            {
                char resetSignal = 'R';
                _serialPort.Write(new[] { resetSignal }, 0, 1);
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
            catch (Exception ex)
            {
                _updateLabels(null, false, ex.Message);
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
                _updateLabels(null, false, ex.Message);
                return;
            }
        }
    }
}
