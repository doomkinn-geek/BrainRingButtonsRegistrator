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
        private CancellationTokenSource _cancellationTokenSource;

        public MainWindow()
        {
            InitializeComponent();
            stopButton.IsEnabled = false;
            continueButton.IsEnabled = false;
        }

        private async void StartButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            string portName = ConfigurationManager.AppSettings["PortName"];
            int baudRate = int.Parse(ConfigurationManager.AppSettings["BaudRate"]);
            _quizApp = new QuizApp(portName, baudRate, UpdateTeamLabels);
            _cancellationTokenSource = new CancellationTokenSource();
            _quizApp.Pause += QuizApp_Pause;

            /*if (!_quizApp.Start())
            {
                return;
            }*/
            await _quizApp.StartAsync(_cancellationTokenSource.Token);
            _quizApp.Reset();

            team1Label.Text = "";
            team2Label.Text = "";
            team3Label.Text = "";

            if (int.TryParse(timerTextBox.Text, out int timerDuration))
            {
                _timer = new DispatcherTimer();
                _timer.Interval = TimeSpan.FromSeconds(timerDuration);
                _timer.Tick += Timer_Tick;
                _timer.Start();
            }

            startButton.IsEnabled = false;
            stopButton.IsEnabled = true;
            StartCountdown(int.Parse(timerTextBox.Text));
            PlaySound("start_sound.wav");            
        }
        private void QuizApp_Pause(object sender, EventArgs e)
        {
            PauseTimerAndPlaySound();
        }
        private async Task PlaySound(string soundFilePath)
        {
            //string soundFilePath = "start_sound.wav";
            if(File.Exists(soundFilePath))
    {
                await Task.Run(() =>
                {
                    using (var soundPlayer = new SoundPlayer(soundFilePath))
                    {
                        soundPlayer.Load();
                        soundPlayer.PlaySync();
                    }
                });
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


        private void StopButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource.Cancel();            
            _countdownTimer?.Stop();
            PlaySound("stop_sound.wav");

            startButton.IsEnabled = true;
            stopButton.IsEnabled = false;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _quizApp.Stop();
            _timer.Stop();

            startButton.IsEnabled = true;
            stopButton.IsEnabled = false;
        }

        private void UpdateTeamLabels(List<int> candidates, bool error, string receivedData)
        {
            Dispatcher.Invoke(() =>
            {
                if (error)
                {
                    //MessageBox.Show("Error occurred.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    team1Label.Text = "О";
                    team2Label.Text = "Ш";
                    team3Label.Text = "Б";
                }
                else
                {
                    if (candidates != null)
                    {
                        if (candidates.Count > 0)
                        {
                            team1Label.Text = $"{candidates[0]}";
                            //PlaySound("stop_sound.wav");
                            //continueButton.IsEnabled = true;
                            //_countdownTimer?.Stop();    
                            //_quizApp.Stop();
                            //stopButton.IsEnabled = false;
                            //startButton.IsEnabled = true;
                        }

                        if (candidates.Count > 1)
                        {
                            team2Label.Text = $"{candidates[1]}";
                        }

                        if (candidates.Count > 2)
                        {
                            team3Label.Text = $"{candidates[2]}";
                        }
                    }
                }
                receivedDataTextBox.AppendText(receivedData);
                receivedDataTextBox.ScrollToEnd();
            });
        }
        private void PauseTimerAndPlaySound()
        {
            _countdownTimer.Stop();
            PlaySound("stop_sound.wav");
        }

        private void continueButton_Click(object sender, RoutedEventArgs e)
        {
            PauseTimerAndPlaySound();
        }
    }
}
