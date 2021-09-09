using System;
using System.Windows;
using MahApps.Metro.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using ControlzEx.Theming;

namespace NotEnoughAV1Encodes
{
    public partial class MainWindow : MetroWindow
    {
        private SettingsDB settingsDB = new();
        private readonly Video.VideoDB videoDB = new();
        private bool QueueParallel = true;
        private int ProgramState;
        private CancellationTokenSource cancellationTokenSource;

        public ObservableCollection<Queue.QueueElement> QueueList { get; set; } = new();

        public MainWindow()
        {
            InitializeComponent();
            Initialize();
        }

        #region Startup
        private void Initialize()
        {
            resources.MediaLanguages.FillDictionary();

            try
            {
                settingsDB = JsonConvert.DeserializeObject<SettingsDB>(File.ReadAllText(Path.Combine(Global.AppData, "NEAV1E", "settings.json")));
            }
            catch { }

            // Set Theme
            if (settingsDB.Theme != null)
            {
                try
                {
                    ThemeManager.Current.ChangeTheme(this, settingsDB.Theme);
                }
                catch { }
            }

            // Load Queue
            if (Directory.Exists(Path.Combine(Global.AppData, "NEAV1E", "Queue")))
            {
                string[] filePaths = Directory.GetFiles(Path.Combine(Global.AppData, "NEAV1E", "Queue"), "*.json", SearchOption.TopDirectoryOnly);

                foreach (string file in filePaths)
                {
                    ListBoxQueue.Items.Add(JsonConvert.DeserializeObject<Queue.QueueElement>(File.ReadAllText(file)));
                }
            }
        }
        #endregion

        #region Buttons
        private void ButtonCancelEncode_Click(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource.Cancel();
            ImageStartStop.Source = new BitmapImage(new Uri(@"/NotEnoughAV1Encodes;component/resources/img/start.png", UriKind.Relative));
        }
        private void ButtonProgramSettings_Click(object sender, RoutedEventArgs e)
        {
            Views.ProgramSettings programSettings = new(settingsDB);
            programSettings.ShowDialog();
            settingsDB.DeleteTempFiles = programSettings.DeleteTempFiles;
            settingsDB.ShutdownAfterEncode = programSettings.ShutdownAfterEncode;
            settingsDB.BaseTheme = programSettings.BaseTheme;
            settingsDB.AccentTheme = programSettings.AccentTheme;
            settingsDB.Theme = programSettings.Theme;
            try
            {
                ThemeManager.Current.ChangeTheme(this, settingsDB.Theme);
            }
            catch { }
            try
            {
                File.WriteAllText(Path.Combine(Global.AppData, "NEAV1E", "settings.json"), JsonConvert.SerializeObject(settingsDB, Formatting.Indented));
            }
            catch { }
        }

        private void ButtonRemoveSelectedQueueItem_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxQueue.SelectedItem != null)
            {
                Queue.QueueElement tmp = (Queue.QueueElement)ListBoxQueue.SelectedItem;
                ListBoxQueue.Items.Remove(ListBoxQueue.SelectedItem);
                try
                {
                    File.Delete(Path.Combine(Global.AppData, "NEAV1E", "Queue", tmp.InputFileName + "_" + tmp.UniqueIdentifier + ".json"));
                }
                catch { }
            }
        }

        private void ButtonOpenSource_Click(object sender, RoutedEventArgs e)
        {
            Views.OpenSource openSource = new(settingsDB.Theme);
            openSource.ShowDialog();
            if (openSource.Quit)
            {
                videoDB.InputPath = openSource.Path;
                videoDB.ParseMediaInfo();
                ListBoxAudioTracks.Items.Clear();
                ListBoxAudioTracks.ItemsSource = videoDB.AudioTracks;
                TextBoxVideoSource.Content = videoDB.InputPath;
                LabelVideoLength.Content = videoDB.MIDuration;
                LabelVideoResolution.Content = videoDB.MIWidth + "x" + videoDB.MIHeight;
                LabelVideoColorFomat.Content = videoDB.MIChromaSubsampling;
                string vfr = "";
                if (videoDB.MIIsVFR)
                {
                    vfr = " (VFR)";
                }
                LabelVideoFramerate.Content = videoDB.MIFramerate + vfr;
            }
        }

        private void ButtonSetDestination_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveVideoFileDialog = new()
            {
                Filter = "MKV Video|*.mkv|WebM Video|*.webm|MP4 Video|*.mp4"
            };

            if (saveVideoFileDialog.ShowDialog() == true)
            {
                videoDB.OutputPath = saveVideoFileDialog.FileName;
                LabelVideoDestination.Content = videoDB.OutputPath;
                videoDB.OutputFileName = Path.GetFileName(videoDB.OutputPath);
            }
        }

        private void ButtonStartStop_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxQueue.Items.Count != 0)
            {
                if (ProgramState is 0 or 2)
                {
                    ImageStartStop.Source = new BitmapImage(new Uri(@"/NotEnoughAV1Encodes;component/resources/img/pause.png", UriKind.Relative));

                    // Main Start
                    if (ProgramState is 0)
                    {
                        PreStart();
                    }

                    // Resume all PIDs
                    if (ProgramState is 2)
                    {
                        foreach (int pid in Global.LaunchedPIDs)
                        {
                            Resume.ResumeProcessTree(pid);
                        }
                    }

                    ProgramState = 1;
                }
                else if (ProgramState is 1)
                {
                    ProgramState = 2;
                    ImageStartStop.Source = new BitmapImage(new Uri(@"/NotEnoughAV1Encodes;component/resources/img/resume.png", UriKind.Relative));

                    // Pause all PIDs
                    foreach (int pid in Global.LaunchedPIDs)
                    {
                        Suspend.SuspendProcessTree(pid);
                    }
                }
            }
            else
            {
                // To-Do: Error Meldung
            }
        }

        private void ButtonAddToQueue_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(videoDB.InputPath))
            {
                // Throw Error
                return;
            }

            if (string.IsNullOrEmpty(videoDB.OutputPath))
            {
                // Throw Error
                return;
            }

            Queue.QueueElement queueElement = new();
            Audio.CommandGenerator commandgenerator = new();

            queueElement.Input = videoDB.InputPath;
            queueElement.Output = videoDB.OutputPath;
            queueElement.InputFileName = videoDB.InputFileName;
            queueElement.OutputFileName = videoDB.OutputFileName;
            queueElement.AudioCommand = commandgenerator.Generate(ListBoxAudioTracks.Items);
            queueElement.FrameCount = videoDB.MIFrameCount;

            // Generate a random identifier to avoid filesystem conflicts
            const string src = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            StringBuilder identifier = new();
            Random RNG = new();
            for (int i = 0; i < 15; i++)
            {
                identifier.Append(src[RNG.Next(0, src.Length)]);
            }

            queueElement.UniqueIdentifier = identifier.ToString();

            // Add to Queue
            ListBoxQueue.Items.Add(queueElement);

            if(!Directory.Exists(Path.Combine(Global.AppData, "NEAV1E", "Queue")))
            {
                Directory.CreateDirectory(Path.Combine(Global.AppData, "NEAV1E", "Queue"));
            }

            // Save as JSON
            File.WriteAllText(Path.Combine(Global.AppData, "NEAV1E", "Queue", videoDB.InputFileName + "_" + identifier + ".json"), JsonConvert.SerializeObject(queueElement, Formatting.Indented));

            Dispatcher.BeginInvoke((Action)(() => TabControl.SelectedIndex = 3));
        }
        #endregion

        #region UI Functions
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Validates that the TextBox Input are only numbers
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        #endregion

        #region Small Functions
        private void Shutdown()
        {
            if (settingsDB.ShutdownAfterEncode)
            {
                Process.Start("shutdown.exe", "/s /t 0");
            }
        }
        private void DeleteTempFiles(Queue.QueueElement queueElement)
        {
            if (settingsDB.DeleteTempFiles)
            {
                if (File.Exists(queueElement.Output))
                {
                    FileInfo _videoOutput = new(queueElement.Output);
                    if (_videoOutput.Length >= 50000)
                    {
                        try
                        {
                            DirectoryInfo tmp = new(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier));
                            tmp.Delete(true);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message, "Error Deleting Temp Files", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        queueElement.Status = "Potential Muxing Error";
                    }
                }
                else
                {
                    queueElement.Status = "Error: No Output detected";
                }
            }
        }
        #endregion

        #region Main Entry
        private async void PreStart()
        {
            // Creates new Cancellation Token
            cancellationTokenSource = new CancellationTokenSource();

            await MainStartAsync(cancellationTokenSource.Token);

            // Dispose Cancellation Source after Main Function finished
            cancellationTokenSource.Dispose();
        }

        private async Task MainStartAsync(CancellationToken _cancelToken)
        {
            // To-Do: Set WorkerCount either by QueueElement or Queue Parallel
            int WorkerCountQueue = 1;
            int WorkerCountElement = 1;

            using SemaphoreSlim concurrencySemaphore = new(WorkerCountQueue);
            // Creates a tasks list
            List<Task> tasks = new();

            foreach (Queue.QueueElement queueElement in ListBoxQueue.Items)
            {
                concurrencySemaphore.Wait();
                Task task = Task.Run(async () =>
                {
                    try
                    {
                        if (!Directory.Exists(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier)))
                        {
                            Directory.CreateDirectory(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier));
                        }

                        List<string> VideoChunks = new();

                        // Chunking
                        if (QueueParallel)
                        {
                            VideoChunks.Add(queueElement.Input);
                        }
                        else
                        {
                            string[] filePaths = Directory.GetFiles(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Chunks"), "*.mkv", SearchOption.TopDirectoryOnly);
                            foreach (string file in filePaths)
                            {
                                VideoChunks.Add(file);
                            }
                        }

                        // Audio Encoding
                        Audio.EncodeAudio encodeAudio = new();
                        await Task.Run(() => encodeAudio.Encode(queueElement, _cancelToken), _cancelToken);

                        // Starts "a timer" for eta / fps calculation
                        System.Timers.Timer aTimer = new System.Timers.Timer();
                        aTimer.Elapsed += (sender, e) => { UpdateProgressBar(sender, e, queueElement); } ;
                        aTimer.Interval = 1000;
                        aTimer.Start();

                        // Video Encoding
                        Video.VideoEncodePipe videoEncodePipe = new();
                        await Task.Run(() => Video.VideoEncodePipe.Encode(WorkerCountElement, VideoChunks, queueElement, _cancelToken), _cancelToken);

                        aTimer.Stop();

                        Video.VideoMuxer videoMuxer = new();
                        await Task.Run(() => videoMuxer.Concat(queueElement), _cancelToken);

                        await Task.Run(() => DeleteTempFiles(queueElement), _cancelToken);
                    }
                    catch (TaskCanceledException)
                    {
                        queueElement.Status = "Cancelled!";
                    }
                    finally
                    {
                        concurrencySemaphore.Release();
                    }
                });

                tasks.Add(task);
            }
            await Task.WhenAll(tasks.ToArray());

            ProgramState = 0;

            Shutdown();
        }

        private void UpdateProgressBar(object sender, EventArgs e, Queue.QueueElement queueElement)
        {
            // Gets all Progress Files of ffmpeg
            string[] filePaths = Directory.GetFiles(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Progress"), "*.log", SearchOption.TopDirectoryOnly);

            int encodedFrames = 0;

            foreach (string file in filePaths)
            {
                try
                {
                    // Reads the progress file of ffmpeg without locking it up
                    Stream stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    TextReader objstream = new StreamReader(stream);

                    // Reads the content of the stream
                    encodedFrames += int.Parse(objstream.ReadLine());

                    stream.Close();
                    objstream.Close();
                }
                catch { }

                queueElement.Progress = Convert.ToDouble(encodedFrames);
                queueElement.Status = "Encoded: " + ((decimal)encodedFrames / queueElement.FrameCount).ToString("0.00%");
            }
        }

        #endregion

    }
}
