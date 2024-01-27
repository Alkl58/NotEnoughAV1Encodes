using Microsoft.Win32;
using Newtonsoft.Json;
using NotEnoughAV1Encodes.resources.lang;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace NotEnoughAV1Encodes.Controls
{
    public partial class MainWindowTopButtons : UserControl
    {
        public MainWindowTopButtons()
        {
            InitializeComponent();
        }

        private void ButtonOpenSource_Click(object sender, EventArgs e)
        {
            if (MainWindow.startupLock) return;

            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            Views.OpenSource openSource = new(mainWindow.settingsDB.Theme);
            openSource.ShowDialog();
            if (openSource.Quit)
            {
                if (openSource.BatchFolder)
                {
                    // Check if Presets exist
                    if (mainWindow.SummaryTabControl.ComboBoxPresets.Items.Count == 0)
                    {
                        MessageBox.Show(LocalizedStrings.Instance["MessageCreatePresetBeforeBatch"]);
                        return;
                    }

                    // Batch Folder Input
                    Views.BatchFolderDialog batchFolderDialog = new(mainWindow.settingsDB.Theme, openSource.Path, mainWindow.settingsDB.SubfolderBatch);
                    batchFolderDialog.ShowDialog();
                    if (batchFolderDialog.Quit)
                    {
                        List<string> files = batchFolderDialog.Files;
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
                                mainWindow.PresetSettings = JsonConvert.DeserializeObject<VideoSettings>(File.ReadAllText(Path.Combine(Global.AppData, "NEAV1E", "Presets", preset + ".json")));
                                mainWindow.DataContext = mainWindow.PresetSettings;

                                // Create video object
                                mainWindow.videoDB = new()
                                {
                                    InputPath = file
                                };

                                // Output Video
                                string outname = mainWindow.PresetSettings.PresetBatchName;
                                outname = outname.Replace("{filename}", Path.GetFileNameWithoutExtension(file));
                                outname = outname.Replace("{presetname}", preset);

                                mainWindow.videoDB.OutputPath = Path.Combine(output, outname + outputContainer);
                                if (mirrorFolderStructure)
                                {
                                    string relativePath = Path.GetRelativePath(inputPath, Path.GetDirectoryName(file));
                                    mainWindow.videoDB.OutputPath = Path.Combine(output, relativePath, outname + outputContainer);
                                }

                                mainWindow.videoDB.OutputFileName = Path.GetFileName(mainWindow.videoDB.OutputPath);
                                mainWindow.videoDB.ParseMediaInfo(mainWindow.PresetSettings);

                                try { mainWindow.AudioTabControl.ListBoxAudioTracks.Items.Clear(); } catch { }
                                try { mainWindow.AudioTabControl.ListBoxAudioTracks.ItemsSource = null; } catch { }
                                try { mainWindow.SubtitlesTabControl.ListBoxSubtitleTracks.Items.Clear(); } catch { }
                                try { mainWindow.SubtitlesTabControl.ListBoxSubtitleTracks.ItemsSource = null; } catch { }

                                mainWindow.AudioTabControl.ListBoxAudioTracks.ItemsSource = mainWindow.videoDB.AudioTracks;
                                mainWindow.SubtitlesTabControl.ListBoxSubtitleTracks.ItemsSource = mainWindow.videoDB.SubtitleTracks;

                                // Automatically toggle VFR Support, if source is MKV
                                if (mainWindow.videoDB.MIIsVFR && Path.GetExtension(mainWindow.videoDB.InputPath) is ".mkv" or ".MKV")
                                {
                                    mainWindow.VideoTabVideoPartialControl.CheckBoxVideoVFR.IsEnabled = true;
                                    mainWindow.VideoTabVideoPartialControl.CheckBoxVideoVFR.IsChecked = true;
                                }
                                else
                                {
                                    mainWindow.VideoTabVideoPartialControl.CheckBoxVideoVFR.IsChecked = false;
                                    mainWindow.VideoTabVideoPartialControl.CheckBoxVideoVFR.IsEnabled = false;
                                }

                                // Uses Bit-Depth of Video
                                if (!presetBitdepth)
                                {
                                    if (mainWindow.videoDB.MIBitDepth == "8") mainWindow.VideoTabVideoPartialControl.ComboBoxVideoBitDepth.SelectedIndex = 0;
                                    if (mainWindow.videoDB.MIBitDepth == "10") mainWindow.VideoTabVideoPartialControl.ComboBoxVideoBitDepth.SelectedIndex = 1;
                                    if (mainWindow.videoDB.MIBitDepth == "12") mainWindow.VideoTabVideoPartialControl.ComboBoxVideoBitDepth.SelectedIndex = 2;
                                }

                                // Skip Subtitles if Container is not MKV to avoid conflicts
                                bool skipSubs = container != 0;
                                if (!activatesubtitles) skipSubs = true;

                                AddToQueue(identifier.ToString(), skipSubs);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }

                        Dispatcher.BeginInvoke((Action)(() => mainWindow.TabControl.SelectedIndex = 7));
                    }
                }
                else if (openSource.ProjectFile)
                {
                    // Project File Input
                    try
                    {
                        mainWindow.videoDB = new();
                        string file = openSource.Path;
                        Queue.QueueElement queueElement = JsonConvert.DeserializeObject<Queue.QueueElement>(File.ReadAllText(file));

                        mainWindow.PresetSettings = queueElement.Preset;
                        mainWindow.DataContext = mainWindow.PresetSettings;
                        mainWindow.videoDB = queueElement.VideoDB;

                        try { mainWindow.AudioTabControl.ListBoxAudioTracks.Items.Clear(); } catch { }
                        try { mainWindow.AudioTabControl.ListBoxAudioTracks.ItemsSource = null; } catch { }
                        try { mainWindow.SubtitlesTabControl.ListBoxSubtitleTracks.Items.Clear(); } catch { }
                        try { mainWindow.SubtitlesTabControl.ListBoxSubtitleTracks.ItemsSource = null; } catch { }

                        mainWindow.AudioTabControl.ListBoxAudioTracks.ItemsSource = mainWindow.videoDB.AudioTracks;
                        mainWindow.SubtitlesTabControl.ListBoxSubtitleTracks.ItemsSource = mainWindow.videoDB.SubtitleTracks;
                        mainWindow.SummaryTabControl.LabelVideoSource.Text = mainWindow.videoDB.InputPath;
                        mainWindow.SummaryTabControl.LabelVideoDestination.Text = mainWindow.videoDB.OutputPath;
                        mainWindow.SummaryTabControl.LabelVideoLength.Content = mainWindow.videoDB.MIDuration;
                        mainWindow.SummaryTabControl.LabelVideoResolution.Content = mainWindow.videoDB.MIWidth + "x" + mainWindow.videoDB.MIHeight;
                        mainWindow.SummaryTabControl.LabelVideoColorFomat.Content = mainWindow.videoDB.MIChromaSubsampling;

                        mainWindow.SummaryTabControl.ComboBoxChunkingMethod.SelectedIndex = queueElement.ChunkingMethod;
                        mainWindow.SummaryTabControl.ComboBoxReencodeMethod.SelectedIndex = queueElement.ReencodeMethod;
                        mainWindow.VideoTabVideoOptimizationControl.CheckBoxTwoPassEncoding.IsOn = queueElement.Passes == 2;
                        mainWindow.SummaryTabControl.TextBoxChunkLength.Text = queueElement.ChunkLength.ToString();
                        mainWindow.SummaryTabControl.TextBoxPySceneDetectThreshold.Text = queueElement.PySceneDetectThreshold.ToString();
                    }
                    catch (Exception ex)
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

        public void SingleFileInput(string path)
        {
            if (MainWindow.startupLock) return;

            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            // Single File Input
            mainWindow.videoDB = new()
            {
                InputPath = path
            };
            mainWindow.videoDB.ParseMediaInfo(mainWindow.PresetSettings);
            mainWindow.SummaryTabControl.LabelVideoDestination.Text = LocalizedStrings.Instance["LabelVideoDestination"];

            try { mainWindow.AudioTabControl.ListBoxAudioTracks.Items.Clear(); } catch { }
            try { mainWindow.AudioTabControl.ListBoxAudioTracks.ItemsSource = null; } catch { }
            try { mainWindow.SubtitlesTabControl.ListBoxSubtitleTracks.Items.Clear(); } catch { }
            try { mainWindow.SubtitlesTabControl.ListBoxSubtitleTracks.ItemsSource = null; } catch { }

            mainWindow.AudioTabControl.ListBoxAudioTracks.ItemsSource = mainWindow.videoDB.AudioTracks;
            mainWindow.SubtitlesTabControl.ListBoxSubtitleTracks.ItemsSource = mainWindow.videoDB.SubtitleTracks;
            mainWindow.SummaryTabControl.LabelVideoSource.Text = mainWindow.videoDB.InputPath;
            mainWindow.SummaryTabControl.LabelVideoLength.Content = mainWindow.videoDB.MIDuration;
            mainWindow.SummaryTabControl.LabelVideoResolution.Content = mainWindow.videoDB.MIWidth + "x" + mainWindow.videoDB.MIHeight;
            mainWindow.SummaryTabControl.LabelVideoColorFomat.Content = mainWindow.videoDB.MIChromaSubsampling;

            string vfr = "";
            if (mainWindow.videoDB.MIIsVFR)
            {
                vfr = " (VFR)";
                if (Path.GetExtension(mainWindow.videoDB.InputPath) is ".mkv" or ".MKV")
                {
                    mainWindow.VideoTabVideoPartialControl.CheckBoxVideoVFR.IsEnabled = true;
                    mainWindow.VideoTabVideoPartialControl.CheckBoxVideoVFR.IsChecked = true;
                }
                else
                {
                    // VFR Video only currently supported in .mkv container
                    // Reasoning is, that splitting a VFR MP4 Video to MKV Chunks will result in ffmpeg making it CFR
                    // Additionally Copying the MP4 Video to a MKV Video will result in the same behavior, leading to incorrect extracted timestamps
                    mainWindow.VideoTabVideoPartialControl.CheckBoxVideoVFR.IsChecked = false;
                    mainWindow.VideoTabVideoPartialControl.CheckBoxVideoVFR.IsEnabled = false;
                }
            }

            mainWindow.SummaryTabControl.LabelVideoFramerate.Content = mainWindow.videoDB.MIFramerate + vfr;

            // Output
            if (!string.IsNullOrEmpty(mainWindow.settingsDB.DefaultOutPath))
            {
                string outPath = Path.Combine(mainWindow.settingsDB.DefaultOutPath, Path.GetFileNameWithoutExtension(mainWindow.videoDB.InputPath) + mainWindow.settingsDB.DefaultOutContainer);

                if (mainWindow.videoDB.InputPath == outPath)
                {
                    outPath = Path.Combine(mainWindow.settingsDB.DefaultOutPath, Path.GetFileNameWithoutExtension(mainWindow.videoDB.InputPath) + "_av1" + mainWindow.settingsDB.DefaultOutContainer);
                }

                mainWindow.videoDB.OutputPath = outPath;
                mainWindow.SummaryTabControl.LabelVideoDestination.Text = mainWindow.videoDB.OutputPath;
                mainWindow.videoDB.OutputFileName = Path.GetFileName(mainWindow.videoDB.OutputPath);

                try
                {
                    if (Path.GetExtension(mainWindow.videoDB.OutputPath).ToLower() == ".mp4" ||
                        Path.GetExtension(mainWindow.videoDB.OutputPath).ToLower() == ".webm")
                    {
                        // Disable Subtitles if Output is MP4
                        foreach (Subtitle.SubtitleTracks subtitleTracks in mainWindow.SubtitlesTabControl.ListBoxSubtitleTracks.Items)
                        {
                            subtitleTracks.Active = false;
                            subtitleTracks.Enabled = false;
                        }
                    }
                }
                catch { }
            }

            mainWindow.FiltersTabControl.DeleteCropPreviews();
            mainWindow.FiltersTabControl.CreateCropPreviewsOnLoad(mainWindow); // cursed
        }

        private void ButtonSetDestination_Click(object sender, EventArgs e)
        {
            if (MainWindow.startupLock) return;

            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            string fileName = "";

            if (!string.IsNullOrEmpty(mainWindow.videoDB.InputPath))
            {
                fileName = mainWindow.videoDB.InputFileName;
            }

            SaveFileDialog saveVideoFileDialog = new()
            {
                Filter = "MKV Video|*.mkv|WebM Video|*.webm|MP4 Video|*.mp4",
                FileName = fileName
            };

            if (saveVideoFileDialog.ShowDialog() == true)
            {
                mainWindow.videoDB.OutputPath = saveVideoFileDialog.FileName;
                mainWindow.SummaryTabControl.LabelVideoDestination.Text = mainWindow.videoDB.OutputPath;
                mainWindow.videoDB.OutputFileName = Path.GetFileName(mainWindow.videoDB.OutputPath);
                try
                {
                    if (Path.GetExtension(mainWindow.videoDB.OutputPath).ToLower() == ".mp4" ||
                        Path.GetExtension(mainWindow.videoDB.OutputPath).ToLower() == ".webm")
                    {
                        // Disable Subtitles if Output is MP4
                        foreach (Subtitle.SubtitleTracks subtitleTracks in mainWindow.SubtitlesTabControl.ListBoxSubtitleTracks.Items)
                        {
                            subtitleTracks.Active = false;
                            subtitleTracks.Enabled = false;
                        }
                    }
                    else
                    {
                        foreach (Subtitle.SubtitleTracks subtitleTracks in mainWindow.SubtitlesTabControl.ListBoxSubtitleTracks.Items)
                        {
                            subtitleTracks.Enabled = true;
                        }
                    }
                }
                catch { }
            }
        }

        private void AddToQueue(string identifier, bool skipSubs)
        {
            if (MainWindow.startupLock) return;

            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            MainWindow.lockQueue = true;
            if (string.IsNullOrEmpty(mainWindow.videoDB.InputPath))
            {
                // Throw Error
                MessageBox.Show(LocalizedStrings.Instance["MessageNoInput"], LocalizedStrings.Instance["Error"], MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(mainWindow.videoDB.OutputPath))
            {
                // Throw Error
                MessageBox.Show(LocalizedStrings.Instance["MessageNoOutput"], LocalizedStrings.Instance["Error"], MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (mainWindow.videoDB.InputPath == mainWindow.videoDB.OutputPath)
            {
                // Throw Error
                MessageBox.Show(LocalizedStrings.Instance["MessageSameInputOutput"], LocalizedStrings.Instance["Error"], MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Queue.QueueElement queueElement = new();
            Audio.CommandGenerator audioCommandGenerator = new();
            Subtitle.CommandGenerator subCommandGenerator = new();

            queueElement.UniqueIdentifier = identifier;
            queueElement.Input = mainWindow.videoDB.InputPath;
            queueElement.Output = mainWindow.videoDB.OutputPath;
            queueElement.VideoCommand = mainWindow.AdvancedTabControl.CheckBoxCustomVideoSettings.IsOn ? mainWindow.AdvancedTabControl.TextBoxCustomVideoSettings.Text : mainWindow.GenerateEncoderCommand();
            queueElement.VideoHDRMuxCommand = mainWindow.HDRTabControl.GenerateMKVMergeHDRCommand();
            queueElement.AudioCommand = audioCommandGenerator.Generate(mainWindow.AudioTabControl.ListBoxAudioTracks.Items);
            queueElement.SubtitleCommand = skipSubs ? null : subCommandGenerator.GenerateSoftsub(mainWindow.SubtitlesTabControl.ListBoxSubtitleTracks.Items);
            queueElement.SubtitleBurnCommand = subCommandGenerator.GenerateHardsub(mainWindow.SubtitlesTabControl.ListBoxSubtitleTracks.Items, identifier);
            queueElement.FilterCommand = mainWindow.FiltersTabControl.GenerateVideoFilters();
            queueElement.FrameCount = mainWindow.videoDB.MIFrameCount;
            queueElement.EncodingMethod = mainWindow.VideoTabVideoPartialControl.ComboBoxVideoEncoder.SelectedIndex;
            queueElement.ChunkingMethod = mainWindow.SummaryTabControl.ComboBoxChunkingMethod.SelectedIndex;
            queueElement.ReencodeMethod = mainWindow.SummaryTabControl.ComboBoxReencodeMethod.SelectedIndex;
            queueElement.Passes = mainWindow.VideoTabVideoOptimizationControl.CheckBoxTwoPassEncoding.IsOn ? 2 : 1;
            queueElement.ChunkLength = int.Parse(mainWindow.SummaryTabControl.TextBoxChunkLength.Text);
            queueElement.PySceneDetectThreshold = float.Parse(mainWindow.SummaryTabControl.TextBoxPySceneDetectThreshold.Text);
            queueElement.VFR = mainWindow.VideoTabVideoPartialControl.CheckBoxVideoVFR.IsChecked == true;
            queueElement.Preset = mainWindow.PresetSettings;
            queueElement.VideoDB = mainWindow.videoDB;

            if (mainWindow.FiltersTabControl.ToggleSwitchFilterDeinterlace.IsOn && mainWindow.FiltersTabControl.ComboBoxFiltersDeinterlace.SelectedIndex is 1 or 2)
            {
                queueElement.FrameCount += queueElement.FrameCount;
            }

            // Add to Queue
            mainWindow.QueueTabControl.ListBoxQueue.Items.Add(queueElement);

            Directory.CreateDirectory(Path.Combine(Global.AppData, "NEAV1E", "Queue"));

            // Save as JSON
            File.WriteAllText(Path.Combine(Global.AppData, "NEAV1E", "Queue", mainWindow.videoDB.InputFileName + "_" + identifier + ".json"), JsonConvert.SerializeObject(queueElement, Formatting.Indented));

            MainWindow.lockQueue = false;

            mainWindow.QueueTabControl.SortQueue();
        }

        private void ButtonProgramSettings_Click(object sender, EventArgs e)
        {
            if (MainWindow.startupLock) return;

            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            Views.ProgramSettings programSettings = new(mainWindow.settingsDB);
            programSettings.ShowDialog();
            mainWindow.settingsDB = programSettings.settingsDBTemp;

            mainWindow.LoadSettings();

            try
            {
                Directory.CreateDirectory(Path.Combine(Global.AppData, "NEAV1E"));
                File.WriteAllText(Path.Combine(Global.AppData, "NEAV1E", "settings.json"), JsonConvert.SerializeObject(mainWindow.settingsDB, Formatting.Indented));
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void ButtonAddToQueue_Click(object sender, EventArgs e)
        {
            PreAddToQueue();
        }

        private void PreAddToQueue()
        {
            if (MainWindow.startupLock) return;

            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            // Prevents generating a new identifier, if queue item is being edited
            if (string.IsNullOrEmpty(mainWindow.uid))
            {
                // Generate a random identifier to avoid filesystem conflicts
                const string src = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
                StringBuilder identifier = new();
                Random RNG = new();
                for (int i = 0; i < 15; i++)
                {
                    identifier.Append(src[RNG.Next(0, src.Length)]);
                }
                mainWindow.uid = identifier.ToString();
            }

            // Add Job to Queue
            AddToQueue(mainWindow.uid, false);

            Dispatcher.BeginInvoke((Action)(() => mainWindow.TabControl.SelectedIndex = 7));

            // Reset Unique Identifier
            mainWindow.uid = null;
        }

        private void ButtonStartStop_Click(object sender, EventArgs e)
        {
            if (MainWindow.startupLock) return;

            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (mainWindow.QueueTabControl.ListBoxQueue.Items.Count == 0)
            {
                PreAddToQueue();
            }

            if (mainWindow.QueueTabControl.ListBoxQueue.Items.Count != 0)
            {
                if (MainWindow.ProgramState is 0 or 2)
                {
                    ImageStartStop.Source = new BitmapImage(new Uri(@"/NotEnoughAV1Encodes;component/resources/img/pause.png", UriKind.Relative));
                    LabelStartPauseButton.Content = LocalizedStrings.Instance["Pause"];

                    // Main Start
                    if (MainWindow.ProgramState is 0)
                    {
                        ButtonAddToQueue.IsEnabled = false;
                        mainWindow.QueueTabControl.ButtonRemoveSelectedQueueItem.IsEnabled = false;
                        mainWindow.QueueTabControl.ButtonEditSelectedItem.IsEnabled = false;
                        mainWindow.QueueTabControl.ButtonClearQueue.IsEnabled = false;
                        mainWindow.QueueTabControl.ComboBoxSortQueueBy.IsEnabled = false;

                        mainWindow.PreStart();
                    }

                    // Resume all PIDs
                    if (MainWindow.ProgramState is 2)
                    {
                        foreach (int pid in Global.LaunchedPIDs)
                        {
                            Resume.ResumeProcessTree(pid);
                        }
                    }

                    MainWindow.ProgramState = 1;
                }
                else if (MainWindow.ProgramState is 1)
                {
                    MainWindow.ProgramState = 2;
                    ImageStartStop.Source = new BitmapImage(new Uri(@"/NotEnoughAV1Encodes;component/resources/img/resume.png", UriKind.Relative));
                    LabelStartPauseButton.Content = LocalizedStrings.Instance["Resume"];

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

        private void ButtonCancelEncode_Click(object sender, EventArgs e)
        {
            if (MainWindow.startupLock) return;

            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (mainWindow.cancellationTokenSource == null) return;
            try
            {
                mainWindow.cancellationTokenSource.Cancel();
                ImageStartStop.Source = new BitmapImage(new Uri(@"/NotEnoughAV1Encodes;component/resources/img/start.png", UriKind.Relative));
                ButtonAddToQueue.IsEnabled = true;
                mainWindow.QueueTabControl.ButtonRemoveSelectedQueueItem.IsEnabled = true;
                mainWindow.QueueTabControl.ButtonEditSelectedItem.IsEnabled = true;
                mainWindow.QueueTabControl.ButtonClearQueue.IsEnabled = true;
                mainWindow.QueueTabControl.ComboBoxSortQueueBy.IsEnabled = true;

                // To Do: Save Queue States when Cancelling
                // Problem: Needs VideoChunks List
                // Possible Implementation:
                //        - Use VideoChunks Functions from MainStartAsync()
                //        - Save VideoChunks inside QueueElement
                //SaveQueueElementState();
            }
            catch { }
        }
    }
}