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
        private CancellationTokenSource cancellationTokenSource;
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
            DeleteCropPreviews();

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
        private void ButtonTestSettings_Click(object sender, RoutedEventArgs e)
        {
            Views.TestCustomSettings testCustomSettings = new(settingsDB.Theme, VideoTabVideoPartialControl.ComboBoxVideoEncoder.SelectedIndex, CheckBoxCustomVideoSettings.IsOn ? TextBoxCustomVideoSettings.Text : GenerateEncoderCommand());
            testCustomSettings.ShowDialog();
        }

        private void ToggleSwitchFilterCrop_Toggled(object sender, EventArgs e)
        {
            CreateCropPreviewsOnLoad();
        }

        private void ButtonCropAutoDetect_Click(object sender, EventArgs e)
        {
            AutoCropDetect();
        }

        private void ButtonCropPreviewForward_Click(object sender, EventArgs e)
        {
            if (videoDB.InputPath == null) return;
            int index = int.Parse(FiltersTabControl.LabelCropPreview.Content.ToString().Split("/")[0]) + 1;
            if (index > 4)
                index = 1;
            FiltersTabControl.LabelCropPreview.Content = index.ToString() + "/4";

            LoadCropPreview(index);
        }
        private void ButtonCropPreviewBackward_Click(object sender, EventArgs e)
        {
            if (videoDB.InputPath == null) return;
            int index = int.Parse(FiltersTabControl.LabelCropPreview.Content.ToString().Split("/")[0]) - 1;
            if (index < 1)
                index = 4;
            FiltersTabControl.LabelCropPreview.Content = index.ToString() + "/4";

            LoadCropPreview(index);
        }

        private void ButtonCancelEncode_Click(object sender, EventArgs e)
        {
            if (cancellationTokenSource == null) return;
            try
            {
                cancellationTokenSource.Cancel();
                TopButtonsControl.ImageStartStop.Source = new BitmapImage(new Uri(@"/NotEnoughAV1Encodes;component/resources/img/start.png", UriKind.Relative));
                TopButtonsControl.ButtonAddToQueue.IsEnabled = true;
                QueueTabControl.ButtonRemoveSelectedQueueItem.IsEnabled = true;
                QueueTabControl.ButtonEditSelectedItem.IsEnabled = true;
                QueueTabControl.ButtonClearQueue.IsEnabled = true;
                QueueTabControl.ComboBoxSortQueueBy.IsEnabled = true;

                // To Do: Save Queue States when Cancelling
                // Problem: Needs VideoChunks List
                // Possible Implementation:
                //        - Use VideoChunks Functions from MainStartAsync()
                //        - Save VideoChunks inside QueueElement
                //SaveQueueElementState();
            }
            catch { }
        }

        private void ButtonProgramSettings_Click(object sender, EventArgs e)
        {
            Views.ProgramSettings programSettings = new(settingsDB);
            programSettings.ShowDialog();
            settingsDB = programSettings.settingsDBTemp;

            LoadSettings();

            try
            {
                Directory.CreateDirectory(Path.Combine(Global.AppData, "NEAV1E"));
                File.WriteAllText(Path.Combine(Global.AppData, "NEAV1E", "settings.json"), JsonConvert.SerializeObject(settingsDB, Formatting.Indented));
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }


        private void ButtonOpenSource_Click(object sender, EventArgs e)
        {
            Views.OpenSource openSource = new(settingsDB.Theme);
            openSource.ShowDialog();
            if (openSource.Quit)
            {
                if (openSource.BatchFolder)
                {
                    // Check if Presets exist
                    if(SummaryTabControl.ComboBoxPresets.Items.Count == 0)
                    {
                        MessageBox.Show(LocalizedStrings.Instance["MessageCreatePresetBeforeBatch"]);
                        return;
                    }

                    // Batch Folder Input
                    Views.BatchFolderDialog batchFolderDialog = new(settingsDB.Theme, openSource.Path, settingsDB.SubfolderBatch);
                    batchFolderDialog.ShowDialog();
                    if (batchFolderDialog.Quit)
                    {
                        List<string> files =  batchFolderDialog.Files;
                        string inputPath = batchFolderDialog.Input;
                        string preset = batchFolderDialog.Preset;
                        string output = batchFolderDialog.Output;
                        int container = batchFolderDialog.Container;
                        bool presetBitdepth = batchFolderDialog.PresetBitdepth;
                        bool activatesubtitles = batchFolderDialog.ActivateSubtitles;
                        bool mirrorFolderStructure = batchFolderDialog.MirrorFolderStructure;

                        string outputContainer = "";
                        if (container == 0) outputContainer = ".mkv";
                        else if (container == 1) outputContainer = ".webm";
                        else if (container == 2) outputContainer = ".mp4";

                        const string src = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
                        try
                        {
                            foreach (string file in files)
                            {
                                // Generate a random identifier to avoid filesystem conflicts
                                StringBuilder identifier = new();
                                Random RNG = new();
                                for (int i = 0; i < 15; i++)
                                {
                                    identifier.Append(src[RNG.Next(0, src.Length)]);
                                }

                                // Load Preset
                                PresetSettings = JsonConvert.DeserializeObject<VideoSettings>(File.ReadAllText(Path.Combine(Global.AppData, "NEAV1E", "Presets", preset + ".json")));
                                DataContext = PresetSettings;

                                // Create video object
                                videoDB = new()
                                {
                                    InputPath = file
                                };

                                // Output Video
                                string outname = PresetSettings.PresetBatchName;
                                outname = outname.Replace("{filename}", Path.GetFileNameWithoutExtension(file));
                                outname = outname.Replace("{presetname}", preset);

                                videoDB.OutputPath = Path.Combine(output, outname + outputContainer);
                                if (mirrorFolderStructure)
                                {
                                    string relativePath = Path.GetRelativePath(inputPath, Path.GetDirectoryName(file));
                                    videoDB.OutputPath = Path.Combine(output, relativePath, outname + outputContainer);
                                }

                                videoDB.OutputFileName = Path.GetFileName(videoDB.OutputPath);
                                videoDB.ParseMediaInfo(PresetSettings);

                                try { AudioTabControl.ListBoxAudioTracks.Items.Clear(); } catch { }
                                try { AudioTabControl.ListBoxAudioTracks.ItemsSource = null; } catch { }
                                try { SubtitlesTabControl.ListBoxSubtitleTracks.Items.Clear(); } catch { }
                                try { SubtitlesTabControl.ListBoxSubtitleTracks.ItemsSource = null; } catch { }

                                AudioTabControl.ListBoxAudioTracks.ItemsSource = videoDB.AudioTracks;
                                SubtitlesTabControl.ListBoxSubtitleTracks.ItemsSource = videoDB.SubtitleTracks;

                                // Automatically toggle VFR Support, if source is MKV
                                if (videoDB.MIIsVFR && Path.GetExtension(videoDB.InputPath) is ".mkv" or ".MKV")
                                {
                                    VideoTabVideoPartialControl.CheckBoxVideoVFR.IsEnabled = true;
                                    VideoTabVideoPartialControl.CheckBoxVideoVFR.IsChecked = true;
                                }
                                else
                                {
                                    VideoTabVideoPartialControl.CheckBoxVideoVFR.IsChecked = false;
                                    VideoTabVideoPartialControl.CheckBoxVideoVFR.IsEnabled = false;
                                }

                                // Uses Bit-Depth of Video
                                if (!presetBitdepth)
                                {
                                    if (videoDB.MIBitDepth == "8") VideoTabVideoPartialControl.ComboBoxVideoBitDepth.SelectedIndex = 0;
                                    if (videoDB.MIBitDepth == "10") VideoTabVideoPartialControl.ComboBoxVideoBitDepth.SelectedIndex = 1;
                                    if (videoDB.MIBitDepth == "12") VideoTabVideoPartialControl.ComboBoxVideoBitDepth.SelectedIndex = 2;
                                }

                                // Skip Subtitles if Container is not MKV to avoid conflicts
                                bool skipSubs = container != 0;
                                if (!activatesubtitles) skipSubs = true;

                                AddToQueue(identifier.ToString(), skipSubs);
                            }
                        }
                        catch(Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }

                        Dispatcher.BeginInvoke((Action)(() => TabControl.SelectedIndex = 7));
                    }
                }
                else if(openSource.ProjectFile)
                {
                    // Project File Input
                    try
                    {
                        videoDB = new();
                        string file = openSource.Path;
                        Queue.QueueElement queueElement = JsonConvert.DeserializeObject<Queue.QueueElement>(File.ReadAllText(file));

                        PresetSettings = queueElement.Preset;
                        DataContext = PresetSettings;
                        videoDB = queueElement.VideoDB;

                        try { AudioTabControl.ListBoxAudioTracks.Items.Clear(); } catch { }
                        try { AudioTabControl.ListBoxAudioTracks.ItemsSource = null; } catch { }
                        try { SubtitlesTabControl.ListBoxSubtitleTracks.Items.Clear(); } catch { }
                        try { SubtitlesTabControl.ListBoxSubtitleTracks.ItemsSource = null; } catch { }

                        AudioTabControl.ListBoxAudioTracks.ItemsSource = videoDB.AudioTracks;
                        SubtitlesTabControl.ListBoxSubtitleTracks.ItemsSource = videoDB.SubtitleTracks;
                        SummaryTabControl.LabelVideoSource.Text = videoDB.InputPath;
                        SummaryTabControl.LabelVideoDestination.Text = videoDB.OutputPath;
                        SummaryTabControl.LabelVideoLength.Content = videoDB.MIDuration;
                        SummaryTabControl.LabelVideoResolution.Content = videoDB.MIWidth + "x" + videoDB.MIHeight;
                        SummaryTabControl.LabelVideoColorFomat.Content = videoDB.MIChromaSubsampling;

                        SummaryTabControl.ComboBoxChunkingMethod.SelectedIndex = queueElement.ChunkingMethod;
                        SummaryTabControl.ComboBoxReencodeMethod.SelectedIndex = queueElement.ReencodeMethod;
                        VideoTabVideoOptimizationControl.CheckBoxTwoPassEncoding.IsOn = queueElement.Passes == 2;
                        SummaryTabControl.TextBoxChunkLength.Text = queueElement.ChunkLength.ToString();
                        SummaryTabControl.TextBoxPySceneDetectThreshold.Text = queueElement.PySceneDetectThreshold.ToString();
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
                else
                {
                    SingleFileInput(openSource.Path);
                }
            }
        }

        private void SingleFileInput(string path)
        {
            // Single File Input
            videoDB = new()
            {
                InputPath = path
            };
            videoDB.ParseMediaInfo(PresetSettings);
            SummaryTabControl.LabelVideoDestination.Text = LocalizedStrings.Instance["LabelVideoDestination"];

            try { AudioTabControl.ListBoxAudioTracks.Items.Clear(); } catch { }
            try { AudioTabControl.ListBoxAudioTracks.ItemsSource = null; } catch { }
            try { SubtitlesTabControl.ListBoxSubtitleTracks.Items.Clear(); } catch { }
            try { SubtitlesTabControl.ListBoxSubtitleTracks.ItemsSource = null; } catch { }

            AudioTabControl.ListBoxAudioTracks.ItemsSource = videoDB.AudioTracks;
            SubtitlesTabControl.ListBoxSubtitleTracks.ItemsSource = videoDB.SubtitleTracks;
            SummaryTabControl.LabelVideoSource.Text = videoDB.InputPath;
            SummaryTabControl.LabelVideoLength.Content = videoDB.MIDuration;
            SummaryTabControl.LabelVideoResolution.Content = videoDB.MIWidth + "x" + videoDB.MIHeight;
            SummaryTabControl.LabelVideoColorFomat.Content = videoDB.MIChromaSubsampling;
            string vfr = "";
            if (videoDB.MIIsVFR)
            {
                vfr = " (VFR)";
                if (Path.GetExtension(videoDB.InputPath) is ".mkv" or ".MKV")
                {
                    VideoTabVideoPartialControl.CheckBoxVideoVFR.IsEnabled = true;
                    VideoTabVideoPartialControl.CheckBoxVideoVFR.IsChecked = true;
                }
                else
                {
                    // VFR Video only currently supported in .mkv container
                    // Reasoning is, that splitting a VFR MP4 Video to MKV Chunks will result in ffmpeg making it CFR
                    // Additionally Copying the MP4 Video to a MKV Video will result in the same behavior, leading to incorrect extracted timestamps
                    VideoTabVideoPartialControl.CheckBoxVideoVFR.IsChecked = false;
                    VideoTabVideoPartialControl.CheckBoxVideoVFR.IsEnabled = false;
                }
            }
            SummaryTabControl.LabelVideoFramerate.Content = videoDB.MIFramerate + vfr;

            // Output
            if (!string.IsNullOrEmpty(settingsDB.DefaultOutPath))
            {
                string outPath = Path.Combine(settingsDB.DefaultOutPath, Path.GetFileNameWithoutExtension(videoDB.InputPath) + settingsDB.DefaultOutContainer);

                if (videoDB.InputPath == outPath)
                {
                    outPath = Path.Combine(settingsDB.DefaultOutPath, Path.GetFileNameWithoutExtension(videoDB.InputPath) + "_av1" + settingsDB.DefaultOutContainer);
                }

                videoDB.OutputPath = outPath;
                SummaryTabControl.LabelVideoDestination.Text = videoDB.OutputPath;
                videoDB.OutputFileName = Path.GetFileName(videoDB.OutputPath);

                try
                {
                    if (Path.GetExtension(videoDB.OutputPath).ToLower() == ".mp4" ||
                        Path.GetExtension(videoDB.OutputPath).ToLower() == ".webm")
                    {
                        // Disable Subtitles if Output is MP4
                        foreach (Subtitle.SubtitleTracks subtitleTracks in SubtitlesTabControl.ListBoxSubtitleTracks.Items)
                        {
                            subtitleTracks.Active = false;
                            subtitleTracks.Enabled = false;
                        }
                    }
                }
                catch { }
            }

            DeleteCropPreviews();
            CreateCropPreviewsOnLoad();
        }

        private void ButtonSetDestination_Click(object sender, EventArgs e)
        {
            string fileName = "";

            if (!string.IsNullOrEmpty(videoDB.InputPath))
            {
                fileName = videoDB.InputFileName;
            }

            SaveFileDialog saveVideoFileDialog = new()
            {
                Filter = "MKV Video|*.mkv|WebM Video|*.webm|MP4 Video|*.mp4",
                FileName = fileName
            };

            if (saveVideoFileDialog.ShowDialog() == true)
            {
                videoDB.OutputPath = saveVideoFileDialog.FileName;
                SummaryTabControl.LabelVideoDestination.Text = videoDB.OutputPath;
                videoDB.OutputFileName = Path.GetFileName(videoDB.OutputPath);
                try
                {
                    if (Path.GetExtension(videoDB.OutputPath).ToLower() == ".mp4" || 
                        Path.GetExtension(videoDB.OutputPath).ToLower() == ".webm")
                    {
                        // Disable Subtitles if Output is MP4
                        foreach (Subtitle.SubtitleTracks subtitleTracks in SubtitlesTabControl.ListBoxSubtitleTracks.Items)
                        {
                            subtitleTracks.Active = false;
                            subtitleTracks.Enabled = false;
                        }
                    }
                    else
                    {
                        foreach (Subtitle.SubtitleTracks subtitleTracks in SubtitlesTabControl.ListBoxSubtitleTracks.Items)
                        {
                            subtitleTracks.Enabled = true;
                        }
                    }
                }
                catch { }
            }
        }

        private void ButtonStartStop_Click(object sender, EventArgs e)
        {
            if (QueueTabControl.ListBoxQueue.Items.Count == 0)
            {
                PreAddToQueue();
            }

            if (QueueTabControl.ListBoxQueue.Items.Count != 0)
            {
                if (ProgramState is 0 or 2)
                {
                    TopButtonsControl.ImageStartStop.Source = new BitmapImage(new Uri(@"/NotEnoughAV1Encodes;component/resources/img/pause.png", UriKind.Relative));
                    TopButtonsControl.LabelStartPauseButton.Content = LocalizedStrings.Instance["Pause"];

                    // Main Start
                    if (ProgramState is 0)
                    {
                        TopButtonsControl.ButtonAddToQueue.IsEnabled = false;
                        QueueTabControl.ButtonRemoveSelectedQueueItem.IsEnabled = false;
                        QueueTabControl.ButtonEditSelectedItem.IsEnabled = false;
                        QueueTabControl.ButtonClearQueue.IsEnabled = false;
                        QueueTabControl.ComboBoxSortQueueBy.IsEnabled = false;

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
                    TopButtonsControl.ImageStartStop.Source = new BitmapImage(new Uri(@"/NotEnoughAV1Encodes;component/resources/img/resume.png", UriKind.Relative));
                    TopButtonsControl.LabelStartPauseButton.Content = LocalizedStrings.Instance["Resume"];

                    // Pause all PIDs
                    foreach (int pid in Global.LaunchedPIDs)
                    {
                        Suspend.SuspendProcessTree(pid);
                    }
                }
            }
            else
            {
                MessageBox.Show(LocalizedStrings.Instance["MessageQueueEmpty"], LocalizedStrings.Instance["TabItemQueue"], MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ButtonAddToQueue_Click(object sender, EventArgs e)
        {
            PreAddToQueue();
        }

        private void PreAddToQueue()
        {
            // Prevents generating a new identifier, if queue item is being edited
            if (string.IsNullOrEmpty(uid))
            {
                // Generate a random identifier to avoid filesystem conflicts
                const string src = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
                StringBuilder identifier = new();
                Random RNG = new();
                for (int i = 0; i < 15; i++)
                {
                    identifier.Append(src[RNG.Next(0, src.Length)]);
                }
                uid = identifier.ToString();
            }

            // Add Job to Queue
            AddToQueue(uid, false);

            Dispatcher.BeginInvoke((Action)(() => TabControl.SelectedIndex = 7));

            // Reset Unique Identifier
            uid = null;
        }

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
                    SingleFileInput(item);
                }
            }
            if (counter > 1)
            {
                MessageBox.Show("Please use Batch Input (Drag & Drop multiple Files is not supported)");
            }
        }

        private void CheckBoxCustomVideoSettings_Toggled(object sender, RoutedEventArgs e)
        {
            if (CheckBoxCustomVideoSettings.IsOn && SummaryTabControl.presetLoadLock == false && IsLoaded)
            {
                TextBoxCustomVideoSettings.Text = GenerateEncoderCommand();
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Validates that the TextBox Input are only numbers
            Regex regex = new("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void TextBoxCustomVideoSettings_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Verifies the arguments the user inputs into the encoding settings textbox
            // If the users writes a "forbidden" argument, it will display the text red
            string[] forbiddenWords = { "help", "cfg", "debug", "output", "passes", "pass", "fpf", "limit",
            "skip", "webm", "ivf", "obu", "q-hist", "rate-hist", "fullhelp", "benchmark", "first-pass", "second-pass",
            "reconstruction", "enc-mode-2p", "input-stat-file", "output-stat-file" };

            foreach (string word in forbiddenWords)
            {
                if (settingsDB.BaseTheme == 0)
                {
                    // Lightmode
                    TextBoxCustomVideoSettings.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                }
                else
                {
                    // Darkmode
                    TextBoxCustomVideoSettings.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                }

                if (TextBoxCustomVideoSettings.Text.Contains(word))
                {
                    TextBoxCustomVideoSettings.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                    break;
                }
            }
        }
        #endregion

        #region Small Functions

        private static void DeleteCropPreviews()
        {
            for (int i = 1; i < 5; i++)
            {
                string image = Path.Combine(Global.Temp, "NEAV1E", "crop_preview_" + i.ToString() + ".bmp");
                if (File.Exists(image))
                {
                    try
                    {
                        File.Delete(image);
                    }
                    catch { }
                }
            }
        }

        private async void CreateCropPreviewsOnLoad()
        {
            if (!IsLoaded) return;

            if (videoDB.InputPath == null) return;

            if (!FiltersTabControl.ToggleSwitchFilterCrop.IsOn)
            {
                FiltersTabControl.ImageCropPreview.Source = new BitmapImage(new Uri("pack://application:,,,/NotEnoughAV1Encodes;component/resources/img/videoplaceholder.jpg")); ;
                return;
            }

            string crop = "-vf " + VideoFiltersCrop();

            await Task.Run(() => CreateCropPreviews(crop));

            try
            {
                int index = int.Parse(FiltersTabControl.LabelCropPreview.Content.ToString().Split("/")[0]);

                MemoryStream memStream = new(File.ReadAllBytes(Path.Combine(Global.Temp, "NEAV1E", "crop_preview_" + index.ToString() + ".bmp")));
                BitmapImage bmi = new();
                bmi.BeginInit();
                bmi.StreamSource = memStream;
                bmi.EndInit();
                FiltersTabControl.ImageCropPreview.Source = bmi;
            }
            catch { }
        }

        private async void AutoCropDetect()
        {
            if (videoDB.InputPath == null) return;

            List<string> cropList = new();

            string time = videoDB.MIDuration;
            
            int seconds = Convert.ToInt32(Math.Floor(TimeSpan.Parse(time).TotalSeconds / 4));

            // Use the current frame as start point of detection
            int index = int.Parse(FiltersTabControl.LabelCropPreview.Content.ToString().Split("/")[0]);

            string command = "/C ffmpeg.exe -ss " + (index * seconds).ToString() + " -i \"" + videoDB.InputPath + "\" -vf cropdetect=24:2:0 -t 5  -f null -";

            Process ffmpegProcess = new();
            ProcessStartInfo startInfo = new()
            {
                FileName = "cmd.exe",
                WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "FFmpeg"),
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = command,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true
            };

            ffmpegProcess.StartInfo = startInfo;
            ffmpegProcess.Start();

            string lastLine;
            while (! ffmpegProcess.StandardError.EndOfStream)
            {
                lastLine = ffmpegProcess.StandardError.ReadLine();
                if (lastLine.Contains("crop="))
                {
                    cropList.Add(lastLine.Split("crop=")[1]);
                }
            }

            ffmpegProcess.WaitForExit();

            // Get most occuring value
            string crop = cropList.Where(c => !string.IsNullOrEmpty(c)).GroupBy(a => a).OrderByDescending(b => b.Key[1].ToString()).First().Key;

            try
            {
                // Translate Output to crop values
                int cropTop = int.Parse(crop.Split(":")[3]);
                FiltersTabControl.TextBoxFiltersCropTop.Text = cropTop.ToString();
                PresetSettings.FilterCropTop = cropTop.ToString();

                int cropLeft = int.Parse(crop.Split(":")[2]);
                FiltersTabControl.TextBoxFiltersCropLeft.Text = cropLeft.ToString();
                PresetSettings.FilterCropLeft = cropLeft.ToString();

                int cropBottom = videoDB.MIHeight - cropTop - int.Parse(crop.Split(":")[1]);
                FiltersTabControl.TextBoxFiltersCropBottom.Text = cropBottom.ToString();
                PresetSettings.FilterCropBottom = cropBottom.ToString();

                int cropRight = videoDB.MIWidth - cropLeft - int.Parse(crop.Split(":")[0]);
                FiltersTabControl.TextBoxFiltersCropRight.Text = cropRight.ToString();
                PresetSettings.FilterCropRight = cropRight.ToString();


                string cropNew = "-vf " + VideoFiltersCrop();
                await Task.Run(() => CreateCropPreviews(cropNew));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void CreateCropPreviews(string crop)
        {
            Directory.CreateDirectory(Path.Combine(Global.Temp, "NEAV1E"));

            string time = videoDB.MIDuration;
            int seconds = Convert.ToInt32(Math.Floor(TimeSpan.Parse(time).TotalSeconds / 4));

            for (int i = 1; i < 5; i++)
            {
                // Extract Frames
                string command = "/C ffmpeg.exe -y -ss " + (i * seconds).ToString() + " -i \"" + videoDB.InputPath + "\" -vframes 1 " + crop + " \"" + Path.Combine(Global.Temp, "NEAV1E", "crop_preview_" + i.ToString() + ".bmp") + "\"";

                Process ffmpegProcess = new();
                ProcessStartInfo startInfo = new()
                {
                    UseShellExecute = true,
                    FileName = "cmd.exe",
                    WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "FFmpeg"),
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = command
                };

                ffmpegProcess.StartInfo = startInfo;
                ffmpegProcess.Start();
                ffmpegProcess.WaitForExit();
            }
        }

        private void LoadCropPreview(int index)
        {
            string input = Path.Combine(Global.Temp, "NEAV1E", "crop_preview_" + index.ToString() + ".bmp");
            if (! File.Exists(input)) return;

            try
            {
                MemoryStream memStream = new(File.ReadAllBytes(input));
                BitmapImage bmi = new();
                bmi.BeginInit();
                bmi.StreamSource = memStream;
                bmi.EndInit();
                FiltersTabControl.ImageCropPreview.Source = bmi;
            }
            catch { }
        }

        private void LoadSettings()
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

        private void AddToQueue(string identifier, bool skipSubs)
        {
            lockQueue = true;
            if (string.IsNullOrEmpty(videoDB.InputPath))
            {
                // Throw Error
                MessageBox.Show(LocalizedStrings.Instance["MessageNoInput"], LocalizedStrings.Instance["Error"], MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(videoDB.OutputPath))
            {
                // Throw Error
                MessageBox.Show(LocalizedStrings.Instance["MessageNoOutput"], LocalizedStrings.Instance["Error"], MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (videoDB.InputPath == videoDB.OutputPath)
            {
                // Throw Error
                MessageBox.Show(LocalizedStrings.Instance["MessageSameInputOutput"], LocalizedStrings.Instance["Error"], MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Queue.QueueElement queueElement = new();
            Audio.CommandGenerator audioCommandGenerator = new();
            Subtitle.CommandGenerator subCommandGenerator = new();

            queueElement.UniqueIdentifier = identifier;
            queueElement.Input = videoDB.InputPath;
            queueElement.Output = videoDB.OutputPath;
            queueElement.VideoCommand = CheckBoxCustomVideoSettings.IsOn ? TextBoxCustomVideoSettings.Text : GenerateEncoderCommand();
            queueElement.VideoHDRMuxCommand = HDRTabControl.GenerateMKVMergeHDRCommand();
            queueElement.AudioCommand = audioCommandGenerator.Generate(AudioTabControl.ListBoxAudioTracks.Items);
            queueElement.SubtitleCommand = skipSubs ? null : subCommandGenerator.GenerateSoftsub(SubtitlesTabControl.ListBoxSubtitleTracks.Items);
            queueElement.SubtitleBurnCommand = subCommandGenerator.GenerateHardsub(SubtitlesTabControl.ListBoxSubtitleTracks.Items, identifier);
            queueElement.FilterCommand = GenerateVideoFilters();
            queueElement.FrameCount = videoDB.MIFrameCount;
            queueElement.EncodingMethod = VideoTabVideoPartialControl.ComboBoxVideoEncoder.SelectedIndex;
            queueElement.ChunkingMethod = SummaryTabControl.ComboBoxChunkingMethod.SelectedIndex;
            queueElement.ReencodeMethod = SummaryTabControl.ComboBoxReencodeMethod.SelectedIndex;
            queueElement.Passes = VideoTabVideoOptimizationControl.CheckBoxTwoPassEncoding.IsOn ? 2 : 1;
            queueElement.ChunkLength = int.Parse(SummaryTabControl.TextBoxChunkLength.Text);
            queueElement.PySceneDetectThreshold = float.Parse(SummaryTabControl.TextBoxPySceneDetectThreshold.Text);
            queueElement.VFR = VideoTabVideoPartialControl.CheckBoxVideoVFR.IsChecked == true;
            queueElement.Preset = PresetSettings;
            queueElement.VideoDB = videoDB;

            if (FiltersTabControl.ToggleSwitchFilterDeinterlace.IsOn && FiltersTabControl.ComboBoxFiltersDeinterlace.SelectedIndex is 1 or 2)
            {
                queueElement.FrameCount += queueElement.FrameCount;
            }

            // Add to Queue
            QueueTabControl.ListBoxQueue.Items.Add(queueElement);

            Directory.CreateDirectory(Path.Combine(Global.AppData, "NEAV1E", "Queue"));

            // Save as JSON
            File.WriteAllText(Path.Combine(Global.AppData, "NEAV1E", "Queue", videoDB.InputFileName + "_" + identifier + ".json"), JsonConvert.SerializeObject(queueElement, Formatting.Indented));

            lockQueue = false;

            QueueTabControl.SortQueue();
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

        #region Video Filters
        private string GenerateVideoFilters()
        {
            bool crop = FiltersTabControl.ToggleSwitchFilterCrop.IsOn;
            bool rotate = FiltersTabControl.ToggleSwitchFilterRotate.IsOn;
            bool resize = FiltersTabControl.ToggleSwitchFilterResize.IsOn;
            bool deinterlace = FiltersTabControl.ToggleSwitchFilterDeinterlace.IsOn;
            bool fps = VideoTabVideoPartialControl.ComboBoxVideoFrameRate.SelectedIndex != 0;
            bool oneFilter = false;

            string FilterCommand = "";

            if (crop || rotate || resize || deinterlace || fps)
            {
                FilterCommand = " -vf ";
                if (crop)
                {
                    FilterCommand += VideoFiltersCrop();
                    oneFilter = true;
                }
                if (resize)
                {
                    if (oneFilter) { FilterCommand += ","; }
                    FilterCommand += VideoFiltersResize();
                    oneFilter = true;
                }
                if (rotate)
                {
                    if (oneFilter) { FilterCommand += ","; }
                    FilterCommand += VideoFiltersRotate();
                    oneFilter = true;
                }
                if (deinterlace)
                {
                    if (oneFilter) { FilterCommand += ","; }
                    FilterCommand += VideoFiltersDeinterlace();
                    oneFilter = true;
                }
                if (fps)
                {
                    if (oneFilter) { FilterCommand += ","; }
                    FilterCommand += VideoTabVideoPartialControl.GenerateFFmpegFramerate();
                }
            }


            return FilterCommand;
        }

        private string VideoFiltersCrop()
        {
            // Sets the values for cropping the video
            string widthNew;
            string heightNew;
            try
            {
                widthNew = (int.Parse(FiltersTabControl.TextBoxFiltersCropRight.Text) + int.Parse(FiltersTabControl.TextBoxFiltersCropLeft.Text)).ToString();
                heightNew = (int.Parse(FiltersTabControl.TextBoxFiltersCropTop.Text) + int.Parse(FiltersTabControl.TextBoxFiltersCropBottom.Text)).ToString();
            }
            catch
            {
                widthNew = "0";
                heightNew = "0";
            }

            return "crop=iw-" + widthNew + ":ih-" + heightNew + ":" + FiltersTabControl.TextBoxFiltersCropLeft.Text + ":" + FiltersTabControl.TextBoxFiltersCropTop.Text;
        }

        private string VideoFiltersRotate()
        {
            // Sets the values for rotating the video
            if (FiltersTabControl.ComboBoxFiltersRotate.SelectedIndex == 1) return "transpose=1";
            else if (FiltersTabControl.ComboBoxFiltersRotate.SelectedIndex == 2) return "transpose=2,transpose=2";
            else if (FiltersTabControl.ComboBoxFiltersRotate.SelectedIndex == 3) return "transpose=2";
            else return ""; // If user selected no ratation but still has it enabled
        }

        private string VideoFiltersDeinterlace()
        {
            int filterIndex = FiltersTabControl.ComboBoxFiltersDeinterlace.SelectedIndex;
            string filter = "";

            if (filterIndex == 0)
            {
                filter = "bwdif=mode=0";
            }
            else if (filterIndex == 1)
            {
                filter = "estdif=mode=0";
            }
            else if (filterIndex == 2)
            {
                string bin = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "nnedi", "nnedi3_weights.bin");
                bin = bin.Replace("\u005c", "\u005c\u005c").Replace(":", "\u005c:");
                filter = "nnedi=weights='" + bin + "'";
            }
            else if (filterIndex == 3)
            {
                filter = "yadif=mode=0";
            }

            return filter;
        }

        private string VideoFiltersResize()
        {
            // Auto Set Width
            if (FiltersTabControl.TextBoxFiltersResizeWidth.Text == "0")
            {
                return "scale=trunc(oh*a/2)*2:" + FiltersTabControl.TextBoxFiltersResizeHeight.Text + ":flags=" + FiltersTabControl.ComboBoxResizeAlgorithm.Text;
            }

            // Auto Set Height
            if (FiltersTabControl.TextBoxFiltersResizeHeight.Text == "0")
            {
                return "scale=" + FiltersTabControl.TextBoxFiltersResizeWidth.Text + ":trunc(ow/a/2)*2:flags=" + FiltersTabControl.ComboBoxResizeAlgorithm.Text;
            }

            return "scale=" + FiltersTabControl.TextBoxFiltersResizeWidth.Text + ":" + FiltersTabControl.TextBoxFiltersResizeHeight.Text + ":flags=" + FiltersTabControl.ComboBoxResizeAlgorithm.Text;

        }
        #endregion

        #region Encoder Settings
        private string GenerateEncoderCommand()
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
        private async void PreStart()
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
