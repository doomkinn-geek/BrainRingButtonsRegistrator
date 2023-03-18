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
using System.Timers;

namespace BrainRingButtonsRegistrator
{

    public partial class MainWindow : Window
    {
        private QuizApp _quizApp;
        private DispatcherTimer _countdownTimer;

        public MainWindow()
        {
            InitializeComponent();
            startButton.IsEnabled = false;
            stopButton.IsEnabled = false;
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1)
            {
                if(checkButton.IsEnabled)
                {
                    CheckButton_Click(this, new RoutedEventArgs());
                }                
            }
            else if (e.Key == Key.Space)
            {
                if(startButton.IsEnabled) 
                {
                    StartButton_ClickAsync(this, new RoutedEventArgs());
                }                
            }
            else if (e.Key == Key.F12)
            {
                if(stopButton.IsEnabled)
                {
                    StopButton_ClickAsync(this, new RoutedEventArgs());
                }                
            }
        }

        private async void StartButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => { _quizApp.Stop(); });
            string portName = ConfigurationManager.AppSettings["PortName"];
            int baudRate = int.Parse(ConfigurationManager.AppSettings["BaudRate"]);
            _quizApp = new QuizApp(portName, baudRate, UpdateTeamLabels);            
            _quizApp.CandidateReceived += QuizApp_CandidateReceived;
            _quizApp.FalseStartRegistered += OnFalseStartRegistration;

            if (!_quizApp.Start())
            {
                return;
            }
            //_quizApp.Reset();

            team1Label.Text = "";
            team2Label.Text = "";
            team3Label.Text = "";

            
            
            OnStart();
            PlaySound("start_sound.wav");
        }
        private void QuizApp_CandidateReceived(object sender, EventArgs e)
        {
            OnCandidateReceived();
        }
        private void OnFalseStartRegistration(object sender, int teamNumber)
        {
            Console.WriteLine($"False start by team {teamNumber}!");
            Dispatcher.Invoke(() =>
            {
                errorBorder.Visibility = Visibility.Collapsed;
                switch (teamNumber)
                {
                    case 1:
                        squareBlock1.Visibility = Visibility.Visible; break;
                    case 2:
                        squareBlock2.Visibility = Visibility.Visible; break;
                    case 3:
                        squareBlock3.Visibility = Visibility.Visible; break;
                    case 4:
                        squareBlock4.Visibility = Visibility.Visible; break;
                    case 5:
                        squareBlock5.Visibility = Visibility.Visible; break;
                    case 6:
                        squareBlock6.Visibility = Visibility.Visible; break;
                    case 7:
                        squareBlock7.Visibility = Visibility.Visible; break;
                    default:
                        break;

                }                
            });            
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

        private async void Countdown_Tick(object sender, EventArgs e)
        {
            int currentSeconds = int.Parse(countdownTextBlock.Text);
            currentSeconds--;

            if (currentSeconds <= 0)
            {
                _countdownTimer.Stop();
                countdownTextBlock.Text = "00";
                await Task.Run(() => { _quizApp.Stop(); });                

                OnAllCandidatesReceived();
            }
            else
            {
                countdownTextBlock.Text = currentSeconds.ToString("D2");
            }
        }


        private async void StopButton_ClickAsync(object sender, RoutedEventArgs e)
        {   
            _countdownTimer?.Stop();                        
            await Task.Run(() => { _quizApp.Stop(); });

            OnStop();
        }

       
        private void UpdateTeamLabels(List<int> candidates, bool error, string receivedData)
        {
            Dispatcher.Invoke(async () =>
            {
                if (error)
                {
                    //countdownBorder.Visibility = Visibility.Collapsed;
                    errorBorder.Visibility = Visibility.Visible;

                    _countdownTimer?.Stop();
                    checkButton.IsEnabled = true;
                    startButton.IsEnabled = false;
                    stopButton.IsEnabled = false;                    
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


        private void continueButton_Click(object sender, RoutedEventArgs e)
        {
            _quizApp.ContinueReading();
            OnContinue();
            PlaySound("start_sound.wav");
        }

        private void OnCheckStart()
        {
            errorBorder.Visibility = Visibility.Collapsed;
            checkButton.IsEnabled = false;
            startButton.IsEnabled = true;
            stopButton.IsEnabled = true;
            team1Label.Text = string.Empty;
            team2Label.Text = string.Empty;
            team3Label.Text = string.Empty;
            squareBlock1.Visibility = Visibility.Collapsed;
            squareBlock2.Visibility = Visibility.Collapsed;
            squareBlock3.Visibility = Visibility.Collapsed;
            squareBlock4.Visibility = Visibility.Collapsed;
            squareBlock5.Visibility = Visibility.Collapsed;
            squareBlock6.Visibility = Visibility.Collapsed;
            squareBlock7.Visibility = Visibility.Collapsed;
            countdownTextBlock.Text = timerTextBox.Value.Value.ToString("D2");
            timerTextBox.IsEnabled = false;
        }

        private void OnStart()
        {            
            errorBorder.Visibility = Visibility.Collapsed;                     
            checkButton.IsEnabled = false;
            startButton.IsEnabled = false;
            stopButton.IsEnabled = true;
            StartCountdown(int.Parse(timerTextBox.Text));
        }

        private void OnCandidateReceived()
        {
            Dispatcher.Invoke(async () =>
            {
                if (_countdownTimer.IsEnabled)
                {                    
                    errorBorder.Visibility = Visibility.Collapsed;                    
                    PlaySound("stop_sound.wav");
                }
            });
        }

        private void OnStop()
        {
            Dispatcher.Invoke(async () =>
            {                
                errorBorder.Visibility = Visibility.Collapsed;                
                _countdownTimer?.Stop();             
                await Task.Run(() => { _quizApp.Stop(); });

                checkButton.IsEnabled = true;
                startButton.IsEnabled = false;                
                stopButton.IsEnabled = false;
                team1Label.Text = "";
                team2Label.Text = "";
                team3Label.Text = "";
                squareBlock1.Visibility = Visibility.Collapsed;
                squareBlock2.Visibility = Visibility.Collapsed;
                squareBlock3.Visibility = Visibility.Collapsed;
                squareBlock4.Visibility = Visibility.Collapsed;
                squareBlock5.Visibility = Visibility.Collapsed;
                squareBlock6.Visibility = Visibility.Collapsed;
                squareBlock7.Visibility = Visibility.Collapsed;
                timerTextBox.IsEnabled = true;
            });
        }

        private void OnContinue()
        {
            _quizApp.ContinueReading();
            _countdownTimer.Start();            
            errorBorder.Visibility = Visibility.Collapsed;
        }

        private async void OnAllCandidatesReceived()
        {   
            checkButton.IsEnabled = true;
            startButton.IsEnabled = false;            
            stopButton.IsEnabled = false;
            timerTextBox.IsEnabled = true;            
            _countdownTimer.Stop();
            await Task.Run(() => { _quizApp.Stop(); });
            PlaySound("stop_sound.wav");
        }

        private void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            string portName = ConfigurationManager.AppSettings["PortName"];
            int baudRate = int.Parse(ConfigurationManager.AppSettings["BaudRate"]);
            _quizApp = new QuizApp(portName, baudRate, UpdateTeamLabels);
            _quizApp.CandidateReceived += QuizApp_CandidateReceived;
            _quizApp.FalseStartRegistered += OnFalseStartRegistration;

            errorBorder.Visibility = Visibility.Collapsed;
            if (!_quizApp.Start())
            {
                return;
            }
            _quizApp.CheckFalseStart();
            OnCheckStart();
            PlaySound("sound_gong.wav");
        }
        private void timerTextBox_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            countdownTextBlock.Text = timerTextBox.Value.Value.ToString("D2");
        }

    }
}
