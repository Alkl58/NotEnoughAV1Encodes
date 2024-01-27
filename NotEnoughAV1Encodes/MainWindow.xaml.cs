using System;
using System.Windows;
using MahApps.Metro.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using ControlzEx.Theming;
using System.Windows.Media;
using System.Linq;
using WPFLocalizeExtension.Engine;
using NotEnoughAV1Encodes.resources.lang;
using System.Windows.Shell;
using NotEnoughAV1Encodes.Encoders;

namespace NotEnoughAV1Encodes
{
    public partial class MainWindow : MetroWindow
    {
        /// <summary>Prevents Race Conditions on Startup</summary>
        public static bool startupLock = true;
        public static bool lockQueue = false;

        /// <summary>Encoding the Queue in Parallel or not</summary>
        private bool QueueParallel;

        /// <summary>State of the Program [0 = IDLE; 1 = Encoding; 2 = Paused]</summary>
        public static int ProgramState;

        public Settings settingsDB = new();
        public Video.VideoDB videoDB = new();

        public string uid;
        public CancellationTokenSource cancellationTokenSource;
        public VideoSettings PresetSettings = new();
        public static bool Logging { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            Initialize();
            DataContext = PresetSettings;

            if (!File.Exists(Path.Combine(Global.AppData, "NEAV1E", "settings.json")))
            {
                // First Launch
                Views.FirstStartup firstStartup = new(settingsDB);
                Hide();
                firstStartup.ShowDialog();
                Show();
            }

            LocalizeDictionary.Instance.Culture = settingsDB.CultureInfo;

            var exists = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)).Length > 1;

            if (exists)
            {
                MessageBox.Show(LocalizedStrings.Instance["MessageAlreadyRunning"], "", MessageBoxButton.OK, MessageBoxImage.Stop);
                Process.GetCurrentProcess().Kill();
                return;
            }
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Cleanup Crop Preview Images
            FiltersTabControl.DeleteCropPreviews();

            if (ProgramState == 0) return;

            // Ask User if ProgramState is not IDLE (0)
            MessageBoxResult result = MessageBox.Show(LocalizedStrings.Instance["CloseQuestion"], "", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
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
            for (int i = 1; i <= coreCount; i++) { SummaryTabControl.ComboBoxWorkerCount.Items.Add(i); }
            SummaryTabControl.ComboBoxWorkerCount.SelectedItem = Convert.ToInt32(coreCount * 75 / 100);
            SummaryTabControl.TextBoxWorkerCount.Text = coreCount.ToString();

            // Load Settings from JSON
            try 
            {
                settingsDB = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(Path.Combine(Global.AppData, "NEAV1E", "settings.json")));

                if (settingsDB == null)
                {
                    settingsDB = new();
                    MessageBox.Show("Program Settings File under %appdata%\\NEAV1E\\settings.json corrupted.\nProgram Settings has been reset.\nPresets are not affected.","That shouldn't have happened!");
                    try
                    {
                        File.WriteAllText(Path.Combine(Global.AppData, "NEAV1E", "settings.json"), JsonConvert.SerializeObject(settingsDB, Formatting.Indented));
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message); }
                }
            } catch { }

            LoadSettings();

            // Load Queue
            if (Directory.Exists(Path.Combine(Global.AppData, "NEAV1E", "Queue")))
            {
                string[] filePaths = Directory.GetFiles(Path.Combine(Global.AppData, "NEAV1E", "Queue"), "*.json", SearchOption.TopDirectoryOnly);

                foreach (string file in filePaths)
                {
                    try
                    {
                        var deserialized = JsonConvert.DeserializeObject<Queue.QueueElement>(File.ReadAllText(file));
                        QueueTabControl.ListBoxQueue.Items.Add(deserialized);
                    }
                    catch (Exception ex) 
                    {
                        MessageBox.Show("Queue File " + file + " is corrupted. \n\nMessage from JSON parser: \n" + ex.Message + "\n\nPlease report this at Github!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            SummaryTabControl.LoadPresets();

            try { SummaryTabControl.ComboBoxPresets.SelectedItem = settingsDB.DefaultPreset; } catch { }
            startupLock = false;

            try { QueueTabControl.ComboBoxSortQueueBy.SelectedIndex = settingsDB.SortQueueBy; } catch { }
        }
        #endregion

        #region Buttons
        private static void SaveQueueElementState(Queue.QueueElement queueElement, List<string> VideoChunks)
        {
            // Save / Override Queuefile to save Progress of Chunks

            foreach (string chunkT in VideoChunks)
            {
                // Get Index
                int index = VideoChunks.IndexOf(chunkT);

                // Already Encoded Status
                bool alreadyEncoded = File.Exists(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + "_finished.log"));

                // Remove Chunk if not finished
                if (!alreadyEncoded)
                {
                    queueElement.ChunkProgress.RemoveAll(chunk => chunk.ChunkName == chunkT);
                }
            }

            try
            {
                File.WriteAllText(Path.Combine(Global.AppData, "NEAV1E", "Queue", queueElement.VideoDB.InputFileName + "_" + queueElement.UniqueIdentifier + ".json"), JsonConvert.SerializeObject(queueElement, Formatting.Indented));
            }
            catch { }

        }
        #endregion

        #region UI Functions

        private void MetroWindow_Drop(object sender, DragEventArgs e)
        {
            // Drag & Drop Video Files into GUI
            List<string> filepaths = new();
            foreach (var s in (string[])e.Data.GetData(DataFormats.FileDrop, false)) { filepaths.Add(s); }
            int counter = 0;
            foreach (var item in filepaths) { counter += 1; }
            foreach (var item in filepaths)
            {
                if (counter == 1)
                {
                    // Single File Input
                    TopButtonsControl.SingleFileInput(item);
                }
            }
            if (counter > 1)
            {
                MessageBox.Show("Please use Batch Input (Drag & Drop multiple Files is not supported)");
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Validates that the TextBox Input are only numbers
            Regex regex = new("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        #endregion

        #region Small Functions
        public void LoadSettings()
        {
            if (settingsDB.OverrideWorkerCount)
            {
                SummaryTabControl.ComboBoxWorkerCount.Visibility = Visibility.Hidden;
                SummaryTabControl.TextBoxWorkerCount.Visibility = Visibility.Visible;
                if (settingsDB.WorkerCount != 99999999)
                    SummaryTabControl.TextBoxWorkerCount.Text = settingsDB.WorkerCount.ToString();
            }
            else
            {
                SummaryTabControl.ComboBoxWorkerCount.Visibility = Visibility.Visible;
                SummaryTabControl.TextBoxWorkerCount.Visibility = Visibility.Hidden;
                if (settingsDB.WorkerCount != 99999999)
                    SummaryTabControl.ComboBoxWorkerCount.SelectedIndex = settingsDB.WorkerCount;
            }

            SummaryTabControl.ComboBoxChunkingMethod.SelectedIndex = settingsDB.ChunkingMethod;
            SummaryTabControl.ComboBoxReencodeMethod.SelectedIndex = settingsDB.ReencodeMethod;
            SummaryTabControl.TextBoxChunkLength.Text = settingsDB.ChunkLength;
            SummaryTabControl.TextBoxPySceneDetectThreshold.Text = settingsDB.PySceneDetectThreshold;
            QueueTabControl.ToggleSwitchQueueParallel.IsOn = settingsDB.QueueParallel;

            // Sets Temp Path
            Global.Temp = settingsDB.TempPath;
            Logging = settingsDB.Logging;

            // Set Theme
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
                        bg = new(Color.FromArgb(100, 20, 20, 20));
                        fg = new(Color.FromArgb(180, 20, 20, 20));
                    }

                    TabControl.Background = bg;
                    AudioTabControl.ListBoxAudioTracks.Background = fg;
                    PresetSettings.BackgroundColor = fg;
                    SubtitlesTabControl.ListBoxSubtitleTracks.Background = fg;
                }
                else
                {
                    bgImage.Source = null;
                }
            }
            catch { }
        }

        private void AutoPauseResume()
        {
            TimeSpan idleTime = win32.IdleDetection.GetInputIdleTime();
            double time = idleTime.TotalSeconds;
            
            Debug.WriteLine("AutoPauseResume() => " + time.ToString() + " Seconds");
            if (ProgramState is 1)
            {
                // Pause
                if (time < 40.0)
                {
                    Dispatcher.Invoke(() => TopButtonsControl.ImageStartStop.Source = new BitmapImage(new Uri(@"/NotEnoughAV1Encodes;component/resources/img/resume.png", UriKind.Relative)));
                    Dispatcher.Invoke(() => TopButtonsControl.LabelStartPauseButton.Content = LocalizedStrings.Instance["Resume"]);
                    Dispatcher.Invoke(() => Title = "NEAV1E - " + LocalizedStrings.Instance["ToggleSwitchAutoPauseResume"] + " => Paused");

                    // Pause all PIDs
                    foreach (int pid in Global.LaunchedPIDs)
                    {
                        Suspend.SuspendProcessTree(pid);
                    }

                    ProgramState = 2;
                }
            }
            else if (ProgramState is 2)
            {
                Dispatcher.Invoke(() => Title = "NEAV1E - " + LocalizedStrings.Instance["ToggleSwitchAutoPauseResume"] + " => Paused - System IDLE since " + time.ToString() + " seconds");
                // Resume
                if (time > 60.0)
                {
                    Dispatcher.Invoke(() => TopButtonsControl.ImageStartStop.Source = new BitmapImage(new Uri(@"/NotEnoughAV1Encodes;component/resources/img/pause.png", UriKind.Relative)));
                    Dispatcher.Invoke(() => TopButtonsControl.LabelStartPauseButton.Content = LocalizedStrings.Instance["Pause"]);
                    Dispatcher.Invoke(() => Title = "NEAV1E - " + LocalizedStrings.Instance["ToggleSwitchAutoPauseResume"] + " => Encoding");

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
            }
        }

        private void Shutdown()
        {
            if (settingsDB.ShutdownAfterEncode)
            {
                Process.Start("shutdown.exe", "/s /t 0");
            }
        }

        private void DeleteTempFiles(Queue.QueueElement queueElement, DateTime startTime)
        {
            string errorText = "";
            if (queueElement.Error)
            {
                errorText = " - " + queueElement.ErrorCount.ToString() + " " + LocalizedStrings.Instance["ErrorsDetected"];
            }

            if (!File.Exists(queueElement.VideoDB.OutputPath)) {
                queueElement.Status = LocalizedStrings.Instance["OutputErrorDetected"] + errorText;
                return;
            }

            FileInfo videoOutput = new(queueElement.VideoDB.OutputPath);
            if (videoOutput.Length <= 50000) {
                queueElement.Status = LocalizedStrings.Instance["MuxingErrorDetected"] + errorText;
                return;
            }

            TimeSpan timespent = DateTime.Now - startTime;
            try {
                queueElement.Status = LocalizedStrings.Instance["FinishedEncoding"] + " " + timespent.ToString("hh\\:mm\\:ss") + " - avg " + Math.Round(queueElement.FrameCount / timespent.TotalSeconds, 2) + "fps" + errorText;
            }
            catch
            {
                queueElement.Status = LocalizedStrings.Instance["FinishedEncoding"] + " " + timespent.ToString("hh\\:mm\\:ss") + " - Error calculating average FPS" + errorText;
            }


            if (settingsDB.DeleteTempFiles && queueElement.Error == false) {
                try {
                    DirectoryInfo tmp = new(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier));
                    tmp.Delete(true);
                } catch {
                    queueElement.Status = LocalizedStrings.Instance["DeleteErrorDetected"] + errorText;
                }
            }
        }
        #endregion

        #region Encoder Settings
        public string GenerateEncoderCommand()
        {
            string settings = VideoTabVideoPartialControl.GenerateFFmpegColorSpace() + " ";

            string encoderSetting = VideoTabVideoPartialControl.ComboBoxVideoEncoder.SelectedIndex switch
            {
                0 => new AOMAV1FFmpeg().GetCommand(),
                1 => new Rav1eFFmpeg().GetCommand(),
                2 => new SvtAV1FFmpeg().GetCommand(),
                3 => new VpxVP9FFmpeg().GetCommand(),
                5 => new Aomenc().GetCommand(),
                6 => new Rav1eFFmpeg().GetCommand(),
                7 => new SvtAV1().GetCommand(),
                9 => new HEVCFFmpeg().GetCommand(),
                10 => new AVCFFmpeg().GetCommand(),
                12 => new QSVEnc().GetCommand(),
                13 => new NVEnc().GetCommand(),
                14 => new AMFAV1().GetCommand(),
                _ => ""
            };

            return settings + encoderSetting;
        }

        public string GenerateMPEGEncoderSpeed()
        {
            return VideoTabVideoOptimizationControl.SliderEncoderPreset.Value switch
            {
                0 => "placebo",
                1 => "veryslow",
                2 => "slower",
                3 => "slow",
                4 => "medium",
                5 => "fast",
                6 => "faster",
                7 => "veryfast",
                8 => "superfast",
                9 => "ultrafast",
                _ => "medium",
            };
        }

        public string GenerateQuickSyncEncoderSpeed()
        {
            return VideoTabVideoOptimizationControl.SliderEncoderPreset.Value switch
            {
                0 => "best",
                1 => "higher",
                2 => "high",
                3 => "balanced",
                4 => "fast",
                5 => "faster",
                6 => "fastest",
                _ => "balanced",
            };
        }

        public string GenerateNVENCEncoderSpeed()
        {
            return VideoTabVideoOptimizationControl.SliderEncoderPreset.Value switch
            {
                0 => "quality",
                1 => "default",
                2 => "performance",
                _ => "default"
            };
        }
        #endregion

        #region Main Entry
        public async void PreStart()
        {
            // Creates new Cancellation Token
            cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await MainStartAsync(cancellationTokenSource.Token);
            }
            catch (OperationCanceledException) { }

            // Dispose Cancellation Source after Main Function finished
            cancellationTokenSource.Dispose();
        }

        private async Task MainStartAsync(CancellationToken _cancelToken)
        {
            QueueParallel = QueueTabControl.ToggleSwitchQueueParallel.IsOn;
            // Sets amount of Workers
            int WorkerCountQueue = 1;
            int WorkerCountElement = int.Parse(SummaryTabControl.ComboBoxWorkerCount.Text);

            if (settingsDB.OverrideWorkerCount)
            {
                WorkerCountElement = int.Parse(SummaryTabControl.TextBoxWorkerCount.Text);
            }

            // If user wants to encode the queue in parallel,
            // it will set the worker count to 1 and the "outer"
            // SemaphoreSlim will be set to the original worker count
            if (QueueParallel)
            {
                WorkerCountQueue = WorkerCountElement;
                WorkerCountElement = 1;
            }

            // Starts Timer for Taskbar Progress Indicator
            System.Timers.Timer taskBarTimer = new();
            Dispatcher.Invoke(() => TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal);
            taskBarTimer.Elapsed += (sender, e) => { UpdateTaskbarProgress(); };
            taskBarTimer.Interval = 3000; // every 3s
            taskBarTimer.Start();

            // Starts Timer for Auto Pause Resume functionality
            System.Timers.Timer pauseResumeTimer = new();
            if (settingsDB.AutoResumePause)
            {
                pauseResumeTimer.Elapsed += (sender, e) => { AutoPauseResume(); };
                pauseResumeTimer.Interval = 20000; // check every 10s
                pauseResumeTimer.Start();
            }

            using SemaphoreSlim concurrencySemaphore = new(WorkerCountQueue);
            // Creates a tasks list
            List<Task> tasks = new();

            foreach (Queue.QueueElement queueElement in QueueTabControl.ListBoxQueue.Items)
            {
                await concurrencySemaphore.WaitAsync(_cancelToken);
                Task task = Task.Run(async () =>
                {
                    try
                    {
                        // Create Output Directory
                        try {  Directory.CreateDirectory(Path.GetDirectoryName(queueElement.VideoDB.OutputPath)); }  catch { }

                        // Create Temp Directory
                        Directory.CreateDirectory(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier));

                        Global.Logger("==========================================================", queueElement.Output + ".log");
                        Global.Logger("INFO  - Started Async Task - UID: " + queueElement.UniqueIdentifier, queueElement.Output + ".log");
                        Global.Logger("INFO  - Input: " + queueElement.Input, queueElement.Output + ".log");
                        Global.Logger("INFO  - Output: " + queueElement.Output, queueElement.Output + ".log");
                        Global.Logger("INFO  - Temp Folder: " + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier), queueElement.Output + ".log");
                        Global.Logger("==========================================================", queueElement.Output + ".log");

                        Audio.EncodeAudio encodeAudio = new();
                        Subtitle.ExtractSubtitles extractSubtitles = new();
                        Video.VideoSplitter videoSplitter = new();
                        Video.VideoEncode videoEncoder = new();
                        Video.VideoMuxer videoMuxer = new();

                        // Get Framecount
                        await Task.Run(() => queueElement.GetFrameCount());

                        // Subtitle Extraction
                        await Task.Run(() => extractSubtitles.Extract(queueElement, _cancelToken), _cancelToken);

                        List<string> VideoChunks = new();

                        // Chunking
                        if (QueueParallel || queueElement.ChunkingMethod == 2)
                        {
                            VideoChunks.Add(queueElement.VideoDB.InputPath);
                            Global.Logger("WARN  - Queue is being processed in Parallel", queueElement.Output + ".log");
                        }
                        else
                        {
                            await Task.Run(() => videoSplitter.Split(queueElement, _cancelToken), _cancelToken);

                            if (queueElement.ChunkingMethod == 0 || queueElement.Preset.TargetVMAF)
                            {
                                // Equal Chunking
                                IOrderedEnumerable<string> sortedChunks = Directory.GetFiles(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Chunks"), "*.mkv", SearchOption.TopDirectoryOnly).OrderBy(f => f);
                                foreach (string file in sortedChunks)
                                {
                                    VideoChunks.Add(file);
                                    Global.Logger("TRACE - Equal Chunking VideoChunks Add " + file, queueElement.Output + ".log");
                                }
                            }
                            else
                            {
                                // Scene Detect
                                if (File.Exists(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "splits.txt")))
                                {
                                    VideoChunks = File.ReadAllLines(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "splits.txt")).ToList();
                                    Global.Logger("TRACE - SceneDetect VideoChunks Add " + VideoChunks, queueElement.Output + ".log");
                                }
                            }
                        }

                        if (VideoChunks.Count == 0)
                        {
                            queueElement.Status = "Error: No Video Chunk found";
                            Global.Logger("FATAL - Error: No Video Chunk found", queueElement.Output + ".log");
                        }
                        else
                        {
                            // Audio Encoding
                            await Task.Run(() => encodeAudio.Encode(queueElement, _cancelToken), _cancelToken);

                            // Extract VFR Timestamps
                            await Task.Run(() => queueElement.GetVFRTimeStamps(), _cancelToken);

                            // Start timer for eta / fps calculation
                            DateTime startTime = DateTime.Now - queueElement.TimeEncoded;
                            System.Timers.Timer aTimer = new();
                            aTimer.Elapsed += (sender, e) => { UpdateProgressBar(queueElement, startTime); };
                            aTimer.Interval = 1000;
                            aTimer.Start();

                            // Video Encoding
                            await Task.Run(() => videoEncoder.Encode(WorkerCountElement, VideoChunks, queueElement, QueueParallel, settingsDB.PriorityNormal, settingsDB, _cancelToken), _cancelToken);

                            // Stop timer for eta / fps calculation
                            aTimer.Stop();

                            // Video Muxing
                            await Task.Run(() => videoMuxer.Concat(queueElement), _cancelToken);

                            // Temp File Deletion
                            await Task.Run(() => DeleteTempFiles(queueElement, startTime), _cancelToken);

                            // Save Queue States (e.g. Chunk Progress)
                            SaveQueueElementState(queueElement, VideoChunks);
                        }
                    }
                    catch (TaskCanceledException) { }
                    finally
                    {
                        concurrencySemaphore.Release();
                    }
                }, _cancelToken);

                tasks.Add(task);
            }
            try
            {
                await Task.WhenAll(tasks.ToArray());
            }
            catch (OperationCanceledException) { }

            ProgramState = 0;
            TopButtonsControl.ImageStartStop.Source = new BitmapImage(new Uri(@"/NotEnoughAV1Encodes;component/resources/img/start.png", UriKind.Relative));
            TopButtonsControl.LabelStartPauseButton.Content = LocalizedStrings.Instance["LabelStartPauseButton"];
            TopButtonsControl.ButtonAddToQueue.IsEnabled = true;
            QueueTabControl.ButtonRemoveSelectedQueueItem.IsEnabled = true;
            QueueTabControl.ButtonEditSelectedItem.IsEnabled = true;
            QueueTabControl.ButtonClearQueue.IsEnabled = true;
            QueueTabControl.ComboBoxSortQueueBy.IsEnabled = true;

            // Stop Timer for Auto Pause Resume functionality
            if (settingsDB.AutoResumePause)
            {
                pauseResumeTimer.Stop();
            }

            // Stop TaskbarItem Progressbar
            taskBarTimer.Stop();
            Dispatcher.Invoke(() => TaskbarItemInfo.ProgressValue = 1.0);
            Dispatcher.Invoke(() => TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Paused);

            // Remove Tasks from Queue if enabled in settings
            if (settingsDB.AutoClearQueue)
            {
                List<Queue.QueueElement> queueItems = new();
                foreach(Queue.QueueElement queueElement in QueueTabControl.ListBoxQueue.Items)
                {
                    if (queueElement == null) continue;
                    // Skip Item if there was some error during encoding / muxing
                    if (queueElement.Error == true) continue;
                    // Check if Outfile exists
                    if (!File.Exists(queueElement.VideoDB.OutputPath)) continue;
                    // Check Outfilesize
                    FileInfo videoOutput = new(queueElement.VideoDB.OutputPath);
                    if (videoOutput.Length <= 50000) continue;

                    queueItems.Add(queueElement);
                }
                foreach(Queue.QueueElement queueElement in queueItems)
                {
                    QueueTabControl.ListBoxQueue.Items.Remove(queueElement);
                    try
                    {
                        File.Delete(Path.Combine(Global.AppData, "NEAV1E", "Queue", queueElement.VideoDB.InputFileName + "_" + queueElement.UniqueIdentifier + ".json"));
                    }
                    catch { }
                }
            }

            Shutdown();
        }
        #endregion

        #region Progressbar
        private static void UpdateProgressBar(Queue.QueueElement queueElement, DateTime startTime)
        {
            queueElement.TimeEncoded = DateTime.Now - startTime;
            long encodedFrames = 0;
            long encodedFramesSecondPass = 0;

            foreach (Queue.ChunkProgress progress in queueElement.ChunkProgress)
            {
                try
                {
                    encodedFrames += progress.Progress;
                }
                catch { }
            }

            // Progress 1-Pass encoding or 1st Pass of 2-Pass encoding
            queueElement.Progress = Convert.ToDouble(encodedFrames);
            
            if (queueElement.Passes == 2)
            {
                // 2 Pass encoding
                foreach (Queue.ChunkProgress progress in queueElement.ChunkProgress)
                {
                    try
                    {
                        encodedFramesSecondPass += progress.ProgressSecondPass;
                    }
                    catch { }
                }

                // Progress 2nd-Pass of 2-Pass Encoding
                queueElement.ProgressSecondPass = Convert.ToDouble(encodedFramesSecondPass);

                string estimatedFPS1stPass = "";
                string estimatedFPS2ndPass = "";
                string estimatedTime1stPass = "";
                string estimatedTime2ndPass = "";

                if (encodedFrames != queueElement.FrameCount)
                {
                    estimatedFPS1stPass = "   -  ~" + Math.Round(encodedFrames / queueElement.TimeEncoded.TotalSeconds, 2).ToString("0.00") + "fps";
                    estimatedTime1stPass = "   -  ~" + Math.Round(((queueElement.TimeEncoded.TotalSeconds / encodedFrames) * (queueElement.FrameCount - encodedFrames)) / 60, MidpointRounding.ToEven) + LocalizedStrings.Instance["QueueMinLeft"];
                }

                if(encodedFramesSecondPass != queueElement.FrameCount)
                {
                    estimatedFPS2ndPass = "   -  ~" + Math.Round(encodedFramesSecondPass / queueElement.TimeEncoded.TotalSeconds, 2).ToString("0.00") + "fps";
                    estimatedTime2ndPass = "   -  ~" + Math.Round(((queueElement.TimeEncoded.TotalSeconds / encodedFramesSecondPass) * (queueElement.FrameCount - encodedFramesSecondPass)) / 60, MidpointRounding.ToEven) + LocalizedStrings.Instance["QueueMinLeft"];
                }
                
                queueElement.Status = LocalizedStrings.Instance["Queue1stPass"] + " " + ((decimal)encodedFrames / queueElement.FrameCount).ToString("00.00%") + estimatedFPS1stPass + estimatedTime1stPass + " - " + LocalizedStrings.Instance["Queue2ndPass"] + " " + ((decimal)encodedFramesSecondPass / queueElement.FrameCount).ToString("00.00%") + estimatedFPS2ndPass + estimatedTime2ndPass;
            }
            else
            {
                // 1 Pass encoding
                string estimatedFPS = "   -  ~" + Math.Round(encodedFrames / queueElement.TimeEncoded.TotalSeconds, 2).ToString("0.00") + "fps";
                string estimatedTime = "   -  ~" + Math.Round(((queueElement.TimeEncoded.TotalSeconds / encodedFrames) * (queueElement.FrameCount - encodedFrames)) / 60, MidpointRounding.ToEven) + LocalizedStrings.Instance["QueueMinLeft"];

                queueElement.Status = "Encoded: " + ((decimal)encodedFrames / queueElement.FrameCount).ToString("00.00%") + estimatedFPS + estimatedTime;
            }

            try
            {
                File.WriteAllText(Path.Combine(Global.AppData, "NEAV1E", "Queue", queueElement.VideoDB.InputFileName + "_" + queueElement.UniqueIdentifier + ".json"), JsonConvert.SerializeObject(queueElement, Formatting.Indented));
            }
            catch { }
        }


        private void UpdateTaskbarProgress()
        {
            double totalFrames = 0;
            double totalFramesEncoded = 0;
            System.Windows.Controls.ItemCollection queueList = QueueTabControl.ListBoxQueue.Items;

            // Calculte Total Framecount
            try
            {
                foreach (Queue.QueueElement queueElement in queueList)
                {
                    totalFrames += queueElement.FrameCount;
                    totalFramesEncoded += queueElement.Progress;
                    if (queueElement.Passes == 2)
                    {
                        // Double Framecount of that queue element for two pass encoding
                        totalFrames += queueElement.FrameCount;
                        totalFramesEncoded += queueElement.ProgressSecondPass;
                    }
                }
            }
            catch { }

            // Dividing by 0 is always great, so we are going to skip it
            if (totalFrames == 0 || totalFramesEncoded == 0) return;

            try
            {
                Dispatcher.Invoke(() => TaskbarItemInfo.ProgressValue = totalFramesEncoded / totalFrames);
            }
            catch { }
        }
        #endregion
    }
}
