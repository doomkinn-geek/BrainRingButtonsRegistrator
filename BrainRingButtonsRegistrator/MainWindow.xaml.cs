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
            stopButton.IsEnabled = false;
        }

        private void StartButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            string portName = "COM5"; 
            int baudRate = 19200;
            _quizApp = new QuizApp(portName, baudRate, UpdateTeamLabels);
            _quizApp.Start();
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
            PlaySound();            
        }
        private async Task PlaySound()
        {
            string soundFilePath = "start_sound.wav";
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
                receivedDataTextBox.Text = $"Sound file not found: {soundFilePath}";
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
            _quizApp.Reset();            
            _quizApp.Stop();
            _timer?.Stop();
            _countdownTimer?.Stop();
            PlaySound();

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
    }
}
