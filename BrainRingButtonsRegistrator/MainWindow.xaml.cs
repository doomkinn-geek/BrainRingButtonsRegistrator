using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Media;
using System.IO;
using System.Configuration;
using System.Threading;
using System.Globalization;

namespace BrainRingButtonsRegistrator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private QuizApp _quizApp;
        private DispatcherTimer _timer;
        private DispatcherTimer _countdownTimer;

        public MainWindow()
        {
            InitializeComponent();
            string portName = ConfigurationManager.AppSettings["PortName"];
            int baudRate = int.Parse(ConfigurationManager.AppSettings["BaudRate"]);
            // Создайте экземпляр класса QuizApp
            _quizApp = new QuizApp(portName, baudRate, UpdateTeamLabels);

            // Подписаться на события
            _quizApp.ErrorReceived += QuizApp_ErrorReceived;
            _quizApp.FalseStartRegistered += OnFalseStartRegistration;
            _quizApp.Pause += QuizApp_Pause;
            _quizApp.AnswerCandidateRegistered += QuizApp_AnswerCandidateRegistered;
            
        }
        private void OnFalseStartRegistration(object sender, int teamNumber)
        {
            // Действия, которые нужно выполнить при регистрации фальстарта командой teamNumber
            // Например, вывести сообщение на экран или обновить список кандидатов
            Console.WriteLine($"False start by team {teamNumber}!");            
            Dispatcher.Invoke(async () =>
            {
                errorBorder.Visibility = Visibility.Collapsed;
                falseStartCandidatesTextBlock.Text += $"{teamNumber};";
            });
            _quizApp.ContinueReading();
        }

        private async void StartButton_ClickAsync(object sender, RoutedEventArgs e)
        {            
            // Проверьте, открыт ли порт
            if (!_quizApp.Start())
            {
                MessageBox.Show("Не удалось открыть порт. Проверьте настройки подключения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // Очистите список кандидатов
            _quizApp.Candidates.Clear();
            falseStartBorder.Visibility = Visibility.Visible;
            errorBorder.Visibility = Visibility.Collapsed;
            _quizApp.FalseStartRegistration = true;
            // Запустите чтение вопроса
            await _quizApp.StartReadingQuestion();            
        }

        private void QuizApp_AnswerCandidateRegistered(object sender, int e)
        {
            if (_quizApp.Candidates.Count == 1)
            {
                team1Label.Text = $"Team {e}";
            }
            else if (_quizApp.Candidates.Count == 2)
            {
                team2Label.Text = $"Team {e}";
            }
            else if (_quizApp.Candidates.Count == 3)
            {
                team3Label.Text = $"Team {e}";

                // Останавливаем таймер обратного отсчета и сбрасываем интерфейс в первоначальное состояние
                _countdownTimer.Stop();
                ResetInterface();
            }
        }
        private async Task ResetInterface()
        {            
            startButton.IsEnabled = true;
            acceptAnswersButton.IsEnabled = false;
            continueButton.IsEnabled = false;
            stopButton.IsEnabled = false;
            team1Label.Text = "";
            team2Label.Text = "";
            team3Label.Text = "";            
            _quizApp.Candidates.Clear();            
        }


        private void UpdateTeamLabels()
        {
            team1Label.Text = _quizApp.Candidates.Count >= 1 ? $"{_quizApp.Candidates[0]}" : "-";
            team2Label.Text = _quizApp.Candidates.Count >= 2 ? $"{_quizApp.Candidates[1]}" : "-";
            team3Label.Text = _quizApp.Candidates.Count >= 3 ? $"{_quizApp.Candidates[2]}" : "-";
        }



        private void QuizApp_ErrorReceived(object sender, EventArgs e)
        {
            Dispatcher.Invoke(async () =>
            {
                // Здесь вы можете добавить логику для обработки ошибки
                errorBorder.Visibility = Visibility.Visible;
                //receivedDataTextBox.Text += e.ToString();
            });
        }


        private void QuizApp_Pause(object sender, EventArgs e)
        {
            OnPause();
        }      

        private void PlaySound(string soundFilePath)
        {
            if (File.Exists(soundFilePath))
            {
                Task.Factory.StartNew(() =>
                {
                    using (var soundPlayer = new SoundPlayer(soundFilePath))
                    {
                        soundPlayer.Load();
                        soundPlayer.Play();
                    }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            else
            {
                receivedDataTextBox.Text = $"Звуковой файл не найден: {soundFilePath}";
            }
        }

        private void StartCountdown(int seconds)
        {
            countdownTextBlock.Text = seconds.ToString();
            _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _countdownTimer.Tick += Countdown_Tick;
            _countdownTimer.Start();
        }

        private void Countdown_Tick(object sender, EventArgs e)
        {
            int currentSeconds = int.Parse(countdownTextBlock.Text);
            currentSeconds--;

            if (currentSeconds <= 0)
            {
                _countdownTimer.Stop();
                countdownTextBlock.Text = "0";
            }
            else
            {
                countdownTextBlock.Text = currentSeconds.ToString();
            }
        }
        private async void StopButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            _countdownTimer?.Stop();
            _timer.Stop();
            await Task.Run(() => { _quizApp.Stop(); });

            OnStop();
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            await Task.Run(() => { _quizApp.Stop(); });
            _timer.Stop();

            OnStop();
        }

        private void UpdateTeamLabels(List<int> candidates, bool error, string receivedData)
        {
            Dispatcher.Invoke(async () =>
            {
                if (error)
                {
                    countdownBorder.Visibility = Visibility.Collapsed;
                    errorBorder.Visibility = Visibility.Visible;

                    _countdownTimer?.Stop();
                    startButton.IsEnabled = true;
                    stopButton.IsEnabled = false;
                    _timer?.Stop();
                    await Task.Run(() => { _quizApp.Stop(); });
                }
                else
                {
                    errorBorder.Visibility = Visibility.Collapsed;

                    if (candidates != null)
                    {
                        if (candidates.Count > 0)
                        {
                            team1Label.Text = $"{candidates[0]}";
                        }

                        if (candidates.Count > 1)
                        {
                            team2Label.Text = $"{candidates[1]}";
                        }

                        if (candidates.Count > 2)
                        {
                            team3Label.Text = $"{candidates[2]}";
                            OnAllCandidatesReceived();
                        }
                    }
                }
                // Определите максимальную длину строки для receivedDataTextBox
                int maxLineLength = CalculateMaxLineLength(receivedDataTextBox);

                receivedDataTextBox.AppendText(receivedData);
                receivedDataTextBox.AppendText(";");

                // Если текущая длина текста превышает максимальную длину строки, очистите receivedDataTextBox
                if (receivedDataTextBox.Text.Length > maxLineLength)
                {
                    receivedDataTextBox.Clear();
                    receivedDataTextBox.AppendText(receivedData);
                    receivedDataTextBox.AppendText(";");
                }
                receivedDataTextBox.ScrollToEnd();
            });
        }

        private int CalculateMaxLineLength(TextBox textBox)
        {
            var typeface = new Typeface(textBox.FontFamily, textBox.FontStyle, textBox.FontWeight, textBox.FontStretch);
            var formattedText = new FormattedText(
                "A",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                textBox.FontSize,
                Brushes.Black,
                new NumberSubstitution(),
                1);

            double characterWidth = formattedText.Width;
            double textBoxWidth = textBox.ActualWidth;

            return (int)Math.Floor(textBoxWidth / characterWidth);
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            // Возобновить прием ответов
            _quizApp.ContinueReading();
            _countdownTimer.Start();
        }

        private async void OnStart()
        {
            // Отображаем бордер для фальш-старта
            falseStartBorder.Visibility = Visibility.Visible;

            // Отправляем команду R и число 7
            await _quizApp.SendCommandAsync('R', 3);

            // Ждем 1 секунду
            await Task.Delay(1000);

            // Убираем проверку условия ошибки из OnStart()

            _quizApp.StartReadingQuestion();

            // Регистрируем фальш-старт
            _quizApp.RegisterFalseStartCandidate();

            // Включаем режим регистрации ответов
            countdownBorder.Visibility = Visibility.Visible;
            errorBorder.Visibility = Visibility.Collapsed;
            startButton.IsEnabled = false;
            stopButton.IsEnabled = true;
            continueButton.IsEnabled = false;
            StartCountdown(int.Parse(timerTextBox.Text));
        }

        private async void CheckButtonPressed()
        {
            if (_quizApp.ReadingQuestion)
            {
                // Регистрируем фальш-старт
                int candidate = await _quizApp.RegisterFalseStartCandidate();
                if (candidate > 0)
                {
                    // Обновляем список фальш-стартов
                    falseStartCandidatesTextBlock.Text += $"{candidate}, ";
                }
            }
            else
            {
                // Обработка нажатия кнопки, когда вопрос прочитан
                OnButtonPressed();
            }
        }
        private void OnButtonPressed()
        {
            // Здесь вы можете добавить логику для обработки нажатий кнопок командами
            // после того, как вопрос был прочитан. Например, вы можете зарегистрировать
            // команду, которая нажала кнопку, или выполнить другие действия, связанные
            // с процессом ответа на вопрос.
        }


        private void OnPause()
        {
            Dispatcher.Invoke(async () =>
            {
                if (_countdownTimer.IsEnabled)
                {
                    _countdownTimer.Stop();
                    countdownBorder.Visibility = Visibility.Collapsed;
                    errorBorder.Visibility = Visibility.Collapsed;
                    continueButton.IsEnabled = true;
                    PlaySound("stop_sound.wav");
                }
            });
        }

        private void OnStop()
        {
            Dispatcher.Invoke(async () =>
            {
                countdownBorder.Visibility = Visibility.Collapsed;
                errorBorder.Visibility = Visibility.Collapsed;
                _countdownTimer?.Stop();
                _timer.Stop();
                await Task.Run(() => { _quizApp.Stop(); });

                startButton.IsEnabled = true;
                continueButton.IsEnabled = false;
                stopButton.IsEnabled = false;
                team1Label.Text = "";
                team2Label.Text = "";
                team3Label.Text = "";
            });
        }

        private void OnContinue()
        {
            _quizApp.ContinueReading();
            continueButton.IsEnabled = false;
            _countdownTimer.Start();
            countdownBorder.Visibility = Visibility.Visible;
            errorBorder.Visibility = Visibility.Collapsed;
        }

        private async void OnAllCandidatesReceived()
        {
            countdownBorder.Visibility = Visibility.Collapsed;
            startButton.IsEnabled = true;
            continueButton.IsEnabled = false;
            stopButton.IsEnabled = false;
            _timer.Stop();
            _countdownTimer.Stop();
            await Task.Run(() => { _quizApp.Stop(); });
            PlaySound("stop_sound.wav");
            //PlaySound("sound_gong.wav");
        }

        private async void AcceptAnswersButton_Click(object sender, RoutedEventArgs e)
        {
            // Остановите таймер обратного отсчета
            _countdownTimer.Stop();

            // Установите ReadingQuestion в false
            //_quizApp.ReadingQuestion = false;

            // Запустите регистрацию кандидатов на ответ
            await _quizApp.StartRegistrationAsync();
        }

        private async Task RegisterAnswerCandidates()
        {
            // Очистите список кандидатов на ответ
            var answerCandidates = new List<int>();

            // Регистрируйте 3 кандидатов на ответ в порядке очереди
            for (int i = 0; i < 3; i++)
            {
                var candidate = await _quizApp.RegisterAnswerCandidate();
                answerCandidates.Add(candidate);
            }

            // Обновите метки команд с учетом новых кандидатов на ответ
            UpdateTeamLabels(answerCandidates, false, "");
        }


    }
}
