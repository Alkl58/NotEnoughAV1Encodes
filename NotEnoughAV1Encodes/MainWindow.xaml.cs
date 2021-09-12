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
using System.Windows.Media;

namespace NotEnoughAV1Encodes
{
    public partial class MainWindow : MetroWindow
    {
        private bool QueueParallel;
        private SettingsDB settingsDB = new();
        private readonly Video.VideoDB videoDB = new();
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

            // Load Worker Count
            int coreCount = 0;
            foreach (System.Management.ManagementBaseObject item in new System.Management.ManagementObjectSearcher("Select * from Win32_Processor").Get())
            {
                coreCount += int.Parse(item["NumberOfCores"].ToString());
            }
            for (int i = 1; i <= coreCount; i++) { ComboBoxWorkerCount.Items.Add(i); }
            ComboBoxWorkerCount.SelectedItem = Convert.ToInt32(coreCount * 75 / 100);
            TextBoxWorkerCount.Text = coreCount.ToString();

            // Load Settings from JSON
            try { settingsDB = JsonConvert.DeserializeObject<SettingsDB>(File.ReadAllText(Path.Combine(Global.AppData, "NEAV1E", "settings.json"))); } catch { }

            LoadSettings();

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
            settingsDB.BGImage = programSettings.BGImage;
            settingsDB.OverrideWorkerCount = programSettings.OverrideWorkerCount;

            LoadSettings();

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
            queueElement.ChunkingMethod = ComboBoxChunkingMethod.SelectedIndex;
            queueElement.ReencodeMethod = ComboBoxReencodeMethod.SelectedIndex;
            queueElement.ChunkLength = int.Parse(TextBoxChunkLength.Text);

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
            Regex regex = new("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        #endregion

        #region Small Functions

        private void LoadSettings()
        {
            if (settingsDB.OverrideWorkerCount)
            {
                ComboBoxWorkerCount.Visibility = Visibility.Hidden;
                TextBoxWorkerCount.Visibility = Visibility.Visible;
            }
            else
            {
                ComboBoxWorkerCount.Visibility = Visibility.Visible;
                TextBoxWorkerCount.Visibility = Visibility.Hidden;
            }

            try
            {
                ThemeManager.Current.ChangeTheme(this, settingsDB.Theme);
            }
            catch { }
            try
            {
                if (settingsDB.BGImage != null)
                {
                    Uri fileUri = new(settingsDB.BGImage);
                    bgImage.Source = new BitmapImage(fileUri);

                    SolidColorBrush bg = new(Color.FromArgb(150, 100, 100, 100));
                    SolidColorBrush fg = new(Color.FromArgb(180, 100, 100, 100));
                    if (settingsDB.BaseTheme == 1)
                    {
                        // Dark
                        bg = new(Color.FromArgb(150, 20, 20, 20));
                        fg = new(Color.FromArgb(180, 20, 20, 20));
                    }

                    TabControl.Background = bg;
                    ListBoxAudioTracks.Background = fg;
                }
                else
                {
                    bgImage.Source = null;
                }
            }
            catch { }
        }

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
            QueueParallel = ToggleSwitchQueueParallel.IsOn;
            // Sets amount of Workers
            int WorkerCountQueue = 1;
            int WorkerCountElement = int.Parse(ComboBoxWorkerCount.Text);

            if (settingsDB.OverrideWorkerCount)
            {
                WorkerCountElement = int.Parse(TextBoxWorkerCount.Text);
            }

            // If user wants to encode the queue in parallel,
            // it will set the worker count to 1 and the "outer"
            // SemaphoreSlim will be set to the original worker count
            if (QueueParallel)
            {
                WorkerCountQueue = WorkerCountElement;
                WorkerCountElement = 1;
            }

            using SemaphoreSlim concurrencySemaphore = new(WorkerCountQueue);
            // Creates a tasks list
            List<Task> tasks = new();

            foreach (Queue.QueueElement queueElement in ListBoxQueue.Items)
            {
                concurrencySemaphore.Wait(_cancelToken);
                Task task = Task.Run(async () =>
                {
                    try
                    {
                        if (!Directory.Exists(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier)))
                        {
                            Directory.CreateDirectory(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier));
                        }

                        Audio.EncodeAudio encodeAudio = new();
                        Video.VideoSplitter videoSplitter = new();
                        Video.VideoEncodePipe videoEncodePipe = new();
                        Video.VideoMuxer videoMuxer = new();

                        List<string> VideoChunks = new();

                        // Chunking
                        if (QueueParallel)
                        {
                            VideoChunks.Add(queueElement.Input);
                        }
                        else
                        {
                            await Task.Run(() => videoSplitter.Split(queueElement, _cancelToken), _cancelToken);

                            string[] filePaths = Directory.GetFiles(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Chunks"), "*.mkv", SearchOption.TopDirectoryOnly);
                            foreach (string file in filePaths)
                            {
                                VideoChunks.Add(file);
                            }
                        }

                        // Audio Encoding
                        await Task.Run(() => Audio.EncodeAudio.Encode(queueElement, _cancelToken), _cancelToken);

                        // Starts "a timer" for eta / fps calculation
                        System.Timers.Timer aTimer = new();
                        aTimer.Elapsed += (sender, e) => { UpdateProgressBar(sender, e, queueElement); } ;
                        aTimer.Interval = 1000;
                        aTimer.Start();

                        // Video Encoding
                        await Task.Run(() => Video.VideoEncodePipe.Encode(WorkerCountElement, VideoChunks, queueElement, _cancelToken, QueueParallel), _cancelToken);

                        aTimer.Stop();

                        await Task.Run(() => Video.VideoMuxer.Concat(queueElement), _cancelToken);

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
                }, _cancelToken);

                tasks.Add(task);
            }
            await Task.WhenAll(tasks.ToArray());

            ProgramState = 0;

            Shutdown();
        }

        private static void UpdateProgressBar(object sender, EventArgs e, Queue.QueueElement queueElement)
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
