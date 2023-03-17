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
            stopButton.IsEnabled = false;
            continueButton.IsEnabled = false;
        }

        private async void StartButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            string portName = ConfigurationManager.AppSettings["PortName"];
            int baudRate = int.Parse(ConfigurationManager.AppSettings["BaudRate"]);
            _quizApp = new QuizApp(portName, baudRate, UpdateTeamLabels);            
            _quizApp.Pause += QuizApp_Pause;

            if (!_quizApp.Start())
            {
                return;
            }
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
            
            OnStart();
            PlaySound("start_sound.wav");
        }
        private void QuizApp_Pause(object sender, EventArgs e)
        {
            OnPause();
        }
        /*private async Task PlaySound(string soundFilePath)
        {            
            if(File.Exists(soundFilePath))
            {
                await Task.Run(() =>
                {
                    using (var soundPlayer = new SoundPlayer(soundFilePath))
                    {
                        soundPlayer.LoadAsync();
                        soundPlayer.PlaySync();
                    }
                });
            }
            else
            {
                receivedDataTextBox.Text = $"Звуковой файл не найден: {soundFilePath}";
            }
        }*/
        private void PlaySound(string soundFilePath)
        {
            if (File.Exists(soundFilePath))
            {
                Task.Factory.StartNew(() =>
                {
                    using (var soundPlayer = new SoundPlayer(soundFilePath))
                    {
                        soundPlayer.Load();
                        soundPlayer.PlaySync();
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


        private void continueButton_Click(object sender, RoutedEventArgs e)
        {
            _quizApp.ContinueReading();
            OnContinue();
            PlaySound("start_sound.wav");
        }

        private void OnStart()
        {
            countdownBorder.Visibility = Visibility.Visible;
            errorBorder.Visibility = Visibility.Collapsed;
            startButton.IsEnabled = false;
            stopButton.IsEnabled = true;
            continueButton.IsEnabled = false;
            StartCountdown(int.Parse(timerTextBox.Text));
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
    }
}
