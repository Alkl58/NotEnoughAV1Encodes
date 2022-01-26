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

namespace NotEnoughAV1Encodes
{
    public partial class MainWindow : MetroWindow
    {
        private bool startupLock = true;
        private bool QueueParallel;
        private SettingsDB settingsDB = new();
        private Video.VideoDB videoDB = new();
        private int ProgramState;
        private CancellationTokenSource cancellationTokenSource;
        public Settings PresetSettings = new();
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

            LoadPresets();

            try { ComboBoxPresets.SelectedItem = settingsDB.DefaultPreset; } catch { }
            startupLock = false;
        }

        private void LoadPresets()
        {
            // Load Presets
            if (Directory.Exists(Path.Combine(Global.AppData, "NEAV1E", "Presets")))
            {
                string[] filePaths = Directory.GetFiles(Path.Combine(Global.AppData, "NEAV1E", "Presets"), "*.json", SearchOption.TopDirectoryOnly);

                foreach (string file in filePaths)
                {
                    ComboBoxPresets.Items.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
        }
        #endregion

        #region Buttons
        private void ButtonTestSettings_Click(object sender, RoutedEventArgs e)
        {
            Views.TestCustomSettings testCustomSettings = new(settingsDB.Theme, ComboBoxVideoEncoder.SelectedIndex, CheckBoxCustomVideoSettings.IsOn ? TextBoxCustomVideoSettings.Text : GenerateEncoderCommand());
            testCustomSettings.ShowDialog();
        }

        private void ButtonCancelEncode_Click(object sender, RoutedEventArgs e)
        {
            if (cancellationTokenSource == null) return;
            try
            {
                cancellationTokenSource.Cancel();
                ImageStartStop.Source = new BitmapImage(new Uri(@"/NotEnoughAV1Encodes;component/resources/img/start.png", UriKind.Relative));
                ButtonAddToQueue.IsEnabled = true;
                ButtonRemoveSelectedQueueItem.IsEnabled = true;
                ButtonEditSelectedItem.IsEnabled = true;
            }
            catch { }
        }

        private void ButtonProgramSettings_Click(object sender, RoutedEventArgs e)
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

        private void ButtonRemoveSelectedQueueItem_Click(object sender, RoutedEventArgs e)
        {
            if (ProgramState != 0) return;
            if (ListBoxQueue.SelectedItem != null)
            {
                Queue.QueueElement tmp = (Queue.QueueElement)ListBoxQueue.SelectedItem;
                ListBoxQueue.Items.Remove(ListBoxQueue.SelectedItem);
                try
                {
                    File.Delete(Path.Combine(Global.AppData, "NEAV1E", "Queue", tmp.VideoDB.InputFileName + "_" + tmp.UniqueIdentifier + ".json"));
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
                if (openSource.BatchFolder)
                {
                    // Check if Presets exist
                    if(ComboBoxPresets.Items.Count == 0)
                    {
                        MessageBox.Show(LocalizedStrings.Instance["MessageCreatePresetBeforeBatch"]);
                        return;
                    }

                    // Batch Folder Input
                    Views.BatchFolderDialog batchFolderDialog = new(settingsDB.Theme, openSource.Path);
                    batchFolderDialog.ShowDialog();
                    if (batchFolderDialog.Quit)
                    {
                        List<string> files =  batchFolderDialog.Files;
                        string preset = batchFolderDialog.Preset;
                        string output = batchFolderDialog.Output;
                        int container = batchFolderDialog.Container;
                        bool presetBitdepth = batchFolderDialog.PresetBitdepth;

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
                                PresetSettings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(Path.Combine(Global.AppData, "NEAV1E", "Presets", preset + ".json")));
                                DataContext = PresetSettings;

                                // Create video object
                                videoDB = new();
                                videoDB.InputPath = file;

                                // Output Video
                                string outname = PresetSettings.PresetBatchName;
                                outname = outname.Replace("{filename}", Path.GetFileNameWithoutExtension(file));
                                outname = outname.Replace("{presetname}", preset);
                                videoDB.OutputPath = Path.Combine(output, outname + outputContainer);
                                videoDB.OutputFileName = Path.GetFileName(videoDB.OutputPath);
                                videoDB.ParseMediaInfo(PresetSettings);

                                try { ListBoxAudioTracks.Items.Clear(); } catch { }
                                try { ListBoxAudioTracks.ItemsSource = null; } catch { }
                                try { ListBoxSubtitleTracks.Items.Clear(); } catch { }
                                try { ListBoxSubtitleTracks.ItemsSource = null; } catch { }

                                ListBoxAudioTracks.ItemsSource = videoDB.AudioTracks;
                                ListBoxSubtitleTracks.ItemsSource = videoDB.SubtitleTracks;

                                // Automatically toggle VFR Support, if source is MKV
                                if (videoDB.MIIsVFR && Path.GetExtension(videoDB.InputPath) is ".mkv" or ".MKV")
                                {
                                    CheckBoxVideoVFR.IsEnabled = true;
                                    CheckBoxVideoVFR.IsChecked = true;
                                }
                                else
                                {
                                    CheckBoxVideoVFR.IsChecked = false;
                                    CheckBoxVideoVFR.IsEnabled = false;
                                }

                                // Uses Bit-Depth of Video
                                if (!presetBitdepth)
                                {
                                    if (videoDB.MIBitDepth == "8") ComboBoxVideoBitDepth.SelectedIndex = 0;
                                    if (videoDB.MIBitDepth == "10") ComboBoxVideoBitDepth.SelectedIndex = 1;
                                    if (videoDB.MIBitDepth == "12") ComboBoxVideoBitDepth.SelectedIndex = 2;
                                }

                                // Skip Subtitles if Container is not MKV to avoid conflicts
                                bool skipSubs = container != 0;

                                AddToQueue(identifier.ToString(), skipSubs);
                            }
                        }
                        catch(Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }

                        Dispatcher.BeginInvoke((Action)(() => TabControl.SelectedIndex = 6));
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

                        try { ListBoxAudioTracks.Items.Clear(); } catch { }
                        try { ListBoxAudioTracks.ItemsSource = null; } catch { }
                        try { ListBoxSubtitleTracks.Items.Clear(); } catch { }
                        try { ListBoxSubtitleTracks.ItemsSource = null; } catch { }

                        ListBoxAudioTracks.ItemsSource = videoDB.AudioTracks;
                        ListBoxSubtitleTracks.ItemsSource = videoDB.SubtitleTracks;
                        LabelVideoSource.Content = videoDB.InputPath;
                        LabelVideoDestination.Content = videoDB.OutputPath;
                        LabelVideoLength.Content = videoDB.MIDuration;
                        LabelVideoResolution.Content = videoDB.MIWidth + "x" + videoDB.MIHeight;
                        LabelVideoColorFomat.Content = videoDB.MIChromaSubsampling;

                        ComboBoxChunkingMethod.SelectedIndex = queueElement.ChunkingMethod;
                        ComboBoxReencodeMethod.SelectedIndex = queueElement.ReencodeMethod;
                        CheckBoxTwoPassEncoding.IsOn = queueElement.Passes == 2;
                        TextBoxChunkLength.Text = queueElement.ChunkLength.ToString();
                        TextBoxPySceneDetectThreshold.Text = queueElement.PySceneDetectThreshold.ToString();
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
            videoDB = new();
            videoDB.InputPath = path;
            videoDB.ParseMediaInfo(PresetSettings);
            LabelVideoDestination.Content = LocalizedStrings.Instance["LabelVideoDestination"];

            try { ListBoxAudioTracks.Items.Clear(); } catch { }
            try { ListBoxAudioTracks.ItemsSource = null; } catch { }
            try { ListBoxSubtitleTracks.Items.Clear(); } catch { }
            try { ListBoxSubtitleTracks.ItemsSource = null; } catch { }

            ListBoxAudioTracks.ItemsSource = videoDB.AudioTracks;
            ListBoxSubtitleTracks.ItemsSource = videoDB.SubtitleTracks;
            LabelVideoSource.Content = videoDB.InputPath;
            LabelVideoLength.Content = videoDB.MIDuration;
            LabelVideoResolution.Content = videoDB.MIWidth + "x" + videoDB.MIHeight;
            LabelVideoColorFomat.Content = videoDB.MIChromaSubsampling;
            string vfr = "";
            if (videoDB.MIIsVFR)
            {
                vfr = " (VFR)";
                if (Path.GetExtension(videoDB.InputPath) is ".mkv" or ".MKV")
                {
                    CheckBoxVideoVFR.IsEnabled = true;
                    CheckBoxVideoVFR.IsChecked = true;
                }
                else
                {
                    // VFR Video only currently supported in .mkv container
                    // Reasoning is, that splitting a VFR MP4 Video to MKV Chunks will result in ffmpeg making it CFR
                    // Additionally Copying the MP4 Video to a MKV Video will result in the same behavior, leading to incorrect extracted timestamps
                    CheckBoxVideoVFR.IsChecked = false;
                    CheckBoxVideoVFR.IsEnabled = false;
                }
            }
            LabelVideoFramerate.Content = videoDB.MIFramerate + vfr;
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
                try
                {
                    if (Path.GetExtension(videoDB.OutputPath).ToLower() == ".mp4")
                    {
                        // Disable Subtitles if Output is MP4
                        foreach (Subtitle.SubtitleTracks subtitleTracks in ListBoxSubtitleTracks.Items)
                        {
                            subtitleTracks.Active = false;
                            subtitleTracks.Enabled = false;
                        }
                    }
                    else
                    {
                        foreach (Subtitle.SubtitleTracks subtitleTracks in ListBoxSubtitleTracks.Items)
                        {
                            subtitleTracks.Enabled = true;
                        }
                    }
                }
                catch { }
            }
        }

        private void ButtonStartStop_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxQueue.Items.Count == 0)
            {
                PreAddToQueue();
            }

            if (ListBoxQueue.Items.Count != 0)
            {
                if (ProgramState is 0 or 2)
                {
                    ImageStartStop.Source = new BitmapImage(new Uri(@"/NotEnoughAV1Encodes;component/resources/img/pause.png", UriKind.Relative));
                    LabelStartPauseButton.Content = LocalizedStrings.Instance["Pause"];

                    // Main Start
                    if (ProgramState is 0)
                    {
                        ButtonAddToQueue.IsEnabled = false;
                        ButtonRemoveSelectedQueueItem.IsEnabled = false;
                        ButtonEditSelectedItem.IsEnabled = false;

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

        private void ButtonAddToQueue_Click(object sender, RoutedEventArgs e)
        {
            PreAddToQueue();
        }

        private void PreAddToQueue()
        {
            // Generate a random identifier to avoid filesystem conflicts
            const string src = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            StringBuilder identifier = new();
            Random RNG = new();
            for (int i = 0; i < 15; i++)
            {
                identifier.Append(src[RNG.Next(0, src.Length)]);
            }
            AddToQueue(identifier.ToString(), false);
            Dispatcher.BeginInvoke((Action)(() => TabControl.SelectedIndex = 6));
        }

        private void ButtonSavePreset_Click(object sender, RoutedEventArgs e)
        {
            Views.SavePresetDialog savePresetDialog = new(settingsDB.Theme);
            savePresetDialog.ShowDialog();
            if (savePresetDialog.Quit)
            {
                Directory.CreateDirectory(Path.Combine(Global.AppData, "NEAV1E", "Presets"));
                PresetSettings.PresetBatchName = savePresetDialog.PresetBatchName;
                PresetSettings.AudioCodecMono = savePresetDialog.AudioCodecMono;
                PresetSettings.AudioCodecStereo = savePresetDialog.AudioCodecStereo;
                PresetSettings.AudioCodecSixChannel = savePresetDialog.AudioCodecSixChannel;
                PresetSettings.AudioCodecEightChannel = savePresetDialog.AudioCodecEightChannel;
                PresetSettings.AudioBitrateMono = savePresetDialog.AudioBitrateMono;
                PresetSettings.AudioBitrateStereo = savePresetDialog.AudioBitrateStereo;
                PresetSettings.AudioBitrateSixChannel = savePresetDialog.AudioBitrateSixChannel;
                PresetSettings.AudioBitrateEightChannel = savePresetDialog.AudioBitrateEightChannel;
                File.WriteAllText(Path.Combine(Global.AppData, "NEAV1E", "Presets", savePresetDialog.PresetName + ".json"), JsonConvert.SerializeObject(PresetSettings, Formatting.Indented));
                ComboBoxPresets.Items.Clear();
                LoadPresets();
            }
        }

        private void ButtonDeletePreset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                File.Delete(Path.Combine(Global.AppData, "NEAV1E", "Presets", ComboBoxPresets.Text + ".json"));
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }

            try
            {
                ComboBoxPresets.Items.Clear();
                LoadPresets();
            }
            catch { }

        }

        private void ButtonSetPresetDefault_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                settingsDB.DefaultPreset = ComboBoxPresets.Text;
                Directory.CreateDirectory(Path.Combine(Global.AppData, "NEAV1E"));
                File.WriteAllText(Path.Combine(Global.AppData, "NEAV1E", "settings.json"), JsonConvert.SerializeObject(settingsDB, Formatting.Indented));
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void ButtonEditSelectedItem_Click(object sender, RoutedEventArgs e)
        {
            if (ProgramState != 0) return;
            if (ListBoxQueue.SelectedItem != null)
            {
                Queue.QueueElement tmp = (Queue.QueueElement)ListBoxQueue.SelectedItem;
                PresetSettings = tmp.Preset;
                DataContext = PresetSettings;
                videoDB = tmp.VideoDB;

                try { ListBoxAudioTracks.Items.Clear(); } catch { }
                try { ListBoxAudioTracks.ItemsSource = null; } catch { }
                try { ListBoxSubtitleTracks.Items.Clear(); } catch { }
                try { ListBoxSubtitleTracks.ItemsSource = null; } catch { }

                ListBoxAudioTracks.ItemsSource = videoDB.AudioTracks;
                ListBoxSubtitleTracks.ItemsSource = videoDB.SubtitleTracks;
                LabelVideoSource.Content = videoDB.InputPath;
                LabelVideoDestination.Content = videoDB.OutputPath;
                LabelVideoLength.Content = videoDB.MIDuration;
                LabelVideoResolution.Content = videoDB.MIWidth + "x" + videoDB.MIHeight;
                LabelVideoColorFomat.Content = videoDB.MIChromaSubsampling;

                ComboBoxChunkingMethod.SelectedIndex = tmp.ChunkingMethod;
                ComboBoxReencodeMethod.SelectedIndex = tmp.ReencodeMethod;
                CheckBoxTwoPassEncoding.IsOn = tmp.Passes == 2;
                TextBoxChunkLength.Text = tmp.ChunkLength.ToString();
                TextBoxPySceneDetectThreshold.Text = tmp.PySceneDetectThreshold.ToString();


                try
                {
                    File.Delete(Path.Combine(Global.AppData, "NEAV1E", "Queue", tmp.VideoDB.InputFileName + "_" + tmp.UniqueIdentifier + ".json"));
                }
                catch { }

                ListBoxQueue.Items.Remove(ListBoxQueue.SelectedItem);

                Dispatcher.BeginInvoke((Action)(() => TabControl.SelectedIndex = 0));
            }
        }

        private void QueueMenuItemSave_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxQueue.SelectedItem != null)
            {
                try
                {
                    Queue.QueueElement tmp = (Queue.QueueElement)ListBoxQueue.SelectedItem;
                    SaveFileDialog saveVideoFileDialog = new();
                    saveVideoFileDialog.AddExtension = true;
                    saveVideoFileDialog.Filter = "JSON File|*.json";
                    if (saveVideoFileDialog.ShowDialog() == true)
                    {
                        File.WriteAllText(saveVideoFileDialog.FileName, JsonConvert.SerializeObject(tmp, Formatting.Indented));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void ListBoxQueue_KeyDown(object sender, KeyEventArgs e)
        {
            if (ListBoxQueue.SelectedItem == null) return;
            if (e.Key == Key.Delete)
            {
                Queue.QueueElement tmp = (Queue.QueueElement)ListBoxQueue.SelectedItem;
                ListBoxQueue.Items.Remove(ListBoxQueue.SelectedItem);
                try
                {
                    File.Delete(Path.Combine(Global.AppData, "NEAV1E", "Queue", tmp.VideoDB.InputFileName + "_" + tmp.UniqueIdentifier + ".json"));
                }
                catch { }
            }
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
        private bool presetLoadLock = false;
        private void ComboBoxPresets_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ComboBoxPresets.SelectedItem == null) return;
            try
            {
                presetLoadLock = true;
                PresetSettings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(Path.Combine(Global.AppData, "NEAV1E", "Presets", ComboBoxPresets.SelectedItem.ToString() + ".json")));
                DataContext = PresetSettings;
                presetLoadLock = false;
            }
            catch { }
        }

        private void ComboBoxVideoEncoder_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (TextBoxMaxBitrate != null)
            {
                if (ComboBoxVideoEncoder.SelectedIndex is 0 or 5)
                {
                    //aom ffmpeg
                    TextBoxMaxBitrate.Visibility = Visibility.Visible;
                    TextBoxMinBitrate.Visibility = Visibility.Visible;
                    if (ComboBoxVideoEncoder.SelectedIndex == 0)
                    {
                        SliderEncoderPreset.Maximum = 8;
                    }
                    else
                    {
                        SliderEncoderPreset.Maximum = 9;
                    }
                    SliderEncoderPreset.Value = 4;
                    SliderQuality.Maximum = 63;
                    SliderQuality.Value = 25;
                    CheckBoxTwoPassEncoding.IsEnabled = true;
                }
                else if (ComboBoxVideoEncoder.SelectedIndex is 1 or 6)
                {
                    //rav1e ffmpeg
                    TextBoxMaxBitrate.Visibility = Visibility.Collapsed;
                    TextBoxMinBitrate.Visibility = Visibility.Collapsed;
                    ComboBoxQualityMode.SelectedIndex = 0;
                    SliderEncoderPreset.Maximum = 10;
                    SliderEncoderPreset.Value = 5;
                    SliderQuality.Maximum = 255;
                    SliderQuality.Value = 80;
                    CheckBoxTwoPassEncoding.IsOn = false;
                    CheckBoxTwoPassEncoding.IsEnabled = false;
                    CheckBoxRealTimeMode.IsOn = false;
                    CheckBoxRealTimeMode.Visibility = Visibility.Collapsed;
                }
                else if (ComboBoxVideoEncoder.SelectedIndex is 2 or 7)
                {
                    //svt-av1 ffmpeg
                    TextBoxMaxBitrate.Visibility = Visibility.Collapsed;
                    TextBoxMinBitrate.Visibility = Visibility.Collapsed;
                    ComboBoxQualityMode.SelectedIndex = 0;
                    SliderEncoderPreset.Maximum = 8;
                    SliderEncoderPreset.Value = 6;
                    SliderQuality.Maximum = 63;
                    SliderQuality.Value = 40;
                    CheckBoxTwoPassEncoding.IsEnabled = true;
                    CheckBoxTwoPassEncoding.IsOn = false;
                    CheckBoxRealTimeMode.IsOn = false;
                    CheckBoxRealTimeMode.Visibility = Visibility.Collapsed;
                }
                else if (ComboBoxVideoEncoder.SelectedIndex is 3)
                {
                    //vpx-vp9 ffmpeg
                    TextBoxMaxBitrate.Visibility = Visibility.Visible;
                    TextBoxMinBitrate.Visibility = Visibility.Visible;
                    SliderEncoderPreset.Maximum = 9;
                    SliderEncoderPreset.Value = 4;
                    SliderQuality.Maximum = 63;
                    SliderQuality.Value = 25;
                    CheckBoxTwoPassEncoding.IsEnabled = true;
                    CheckBoxRealTimeMode.IsOn = false;
                    CheckBoxRealTimeMode.Visibility = Visibility.Collapsed;
                }
                else if (ComboBoxVideoEncoder.SelectedIndex is 9 or 10)
                {
                    //libx265 libx264 ffmpeg
                    TextBoxMaxBitrate.Visibility = Visibility.Collapsed;
                    TextBoxMinBitrate.Visibility = Visibility.Collapsed;
                    SliderEncoderPreset.Maximum = 9;
                    SliderEncoderPreset.Value = 4;
                    SliderQuality.Maximum = 51;
                    SliderQuality.Value = 18;
                    CheckBoxTwoPassEncoding.IsEnabled = false;
                    CheckBoxTwoPassEncoding.IsOn = false;
                    CheckBoxRealTimeMode.IsOn = false;
                    CheckBoxRealTimeMode.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void CheckBoxTwoPassEncoding_Checked(object sender, RoutedEventArgs e)
        {
            if (ComboBoxVideoEncoder.SelectedIndex is 2 or 7 && ComboBoxQualityMode.SelectedIndex == 0 && CheckBoxTwoPassEncoding.IsOn)
            {
                CheckBoxTwoPassEncoding.IsOn = false;
            }

            if (CheckBoxRealTimeMode.IsOn && CheckBoxTwoPassEncoding.IsOn)
            {
                CheckBoxTwoPassEncoding.IsOn = false;
            }
        }

        private void CheckBoxRealTimeMode_Toggled(object sender, RoutedEventArgs e)
        {
            // Reverts to 1 Pass encoding if Real Time Mode is activated
            if (CheckBoxRealTimeMode.IsOn && CheckBoxTwoPassEncoding.IsOn)
            {
                CheckBoxTwoPassEncoding.IsOn = false;
            }
        }

        private void SliderEncoderPreset_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Shows / Hides Real Time Mode CheckBox
            if (CheckBoxRealTimeMode != null && ComboBoxVideoEncoder != null)
            {
                if (ComboBoxVideoEncoder.SelectedIndex == 0 || ComboBoxVideoEncoder.SelectedIndex == 5)
                {
                    if (SliderEncoderPreset.Value >= 5)
                    {
                        CheckBoxRealTimeMode.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        CheckBoxRealTimeMode.IsOn = false;
                        CheckBoxRealTimeMode.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    CheckBoxRealTimeMode.Visibility = Visibility.Collapsed;
                }
            }
            if (ComboBoxVideoEncoder.SelectedIndex is 9 or 10)
            {
                LabelSpeedValue.Content = GenerateMPEGEncoderSpeed();
            }
        }

        private void ComboBoxQualityMode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (TextBoxAVGBitrate != null)
            {
                if (ComboBoxVideoEncoder.SelectedIndex is 1 or 2 or 6 or 7 or 9 or 10)
                {
                    if (ComboBoxQualityMode.SelectedIndex is 1 or 3)
                    {
                        ComboBoxQualityMode.SelectedIndex = 0;
                        MessageBox.Show(LocalizedStrings.Instance["MessageQualityModeRav1eSVT"]);
                        return;
                    }
                    if(CheckBoxTwoPassEncoding.IsOn && ComboBoxVideoEncoder.SelectedIndex is 2 or 7 && ComboBoxQualityMode.SelectedIndex == 0)
                    {
                        CheckBoxTwoPassEncoding.IsOn = false;
                    }
                }
                if (ComboBoxVideoEncoder.SelectedIndex is 5)
                {
                    if (ComboBoxQualityMode.SelectedIndex is 3)
                    {
                        ComboBoxQualityMode.SelectedIndex = 0;
                        MessageBox.Show(LocalizedStrings.Instance["MessageConstrainedBitrateAomenc"]);
                        return;
                    }
                }
                if (ComboBoxQualityMode.SelectedIndex == 0)
                {
                    SliderQuality.IsEnabled = true;
                    TextBoxAVGBitrate.IsEnabled = false;
                    TextBoxMaxBitrate.IsEnabled = false;
                    TextBoxMinBitrate.IsEnabled = false;
                }
                else if (ComboBoxQualityMode.SelectedIndex == 1)
                {
                    SliderQuality.IsEnabled = true;
                    TextBoxAVGBitrate.IsEnabled = false;
                    TextBoxMaxBitrate.IsEnabled = true;
                    TextBoxMinBitrate.IsEnabled = false;
                }
                else if (ComboBoxQualityMode.SelectedIndex == 2)
                {
                    SliderQuality.IsEnabled = false;
                    TextBoxAVGBitrate.IsEnabled = true;
                    TextBoxMaxBitrate.IsEnabled = false;
                    TextBoxMinBitrate.IsEnabled = false;
                }
                else if (ComboBoxQualityMode.SelectedIndex == 3)
                {
                    SliderQuality.IsEnabled = false;
                    TextBoxAVGBitrate.IsEnabled = true;
                    TextBoxMaxBitrate.IsEnabled = true;
                    TextBoxMinBitrate.IsEnabled = true;
                }
            }
        }

        private void ComboBoxVideoBitDepth_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ComboBoxVideoEncoder.SelectedIndex == 10 && ComboBoxVideoBitDepth.SelectedIndex == 2)
            {
                ComboBoxVideoBitDepth.SelectedIndex = 1;
            }
        }

        private void CheckBoxCustomVideoSettings_Toggled(object sender, RoutedEventArgs e)
        {
            if (CheckBoxCustomVideoSettings.IsOn && presetLoadLock == false)
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
        private void ComboBoxChunkingMethod_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (startupLock) return;
            settingsDB.ChunkingMethod = ComboBoxChunkingMethod.SelectedIndex;
            settingsDB.ReencodeMethod = ComboBoxReencodeMethod.SelectedIndex;
            try
            {
                Directory.CreateDirectory(Path.Combine(Global.AppData, "NEAV1E"));
                File.WriteAllText(Path.Combine(Global.AppData, "NEAV1E", "settings.json"), JsonConvert.SerializeObject(settingsDB, Formatting.Indented));
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        private void TextBoxChunkLength_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (startupLock) return;
            settingsDB.ChunkLength = TextBoxChunkLength.Text;
            settingsDB.PySceneDetectThreshold = TextBoxPySceneDetectThreshold.Text;
            try
            {
                Directory.CreateDirectory(Path.Combine(Global.AppData, "NEAV1E"));
                File.WriteAllText(Path.Combine(Global.AppData, "NEAV1E", "settings.json"), JsonConvert.SerializeObject(settingsDB, Formatting.Indented));
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
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

            ComboBoxChunkingMethod.SelectedIndex = settingsDB.ChunkingMethod;
            ComboBoxReencodeMethod.SelectedIndex = settingsDB.ReencodeMethod;
            TextBoxChunkLength.Text = settingsDB.ChunkLength;
            TextBoxPySceneDetectThreshold.Text = settingsDB.PySceneDetectThreshold;

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
                        bg = new(Color.FromArgb(150, 20, 20, 20));
                        fg = new(Color.FromArgb(180, 20, 20, 20));
                    }

                    TabControl.Background = bg;
                    ListBoxAudioTracks.Background = fg;
                    ListBoxSubtitleTracks.Background = fg;
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

            Queue.QueueElement queueElement = new();
            Audio.CommandGenerator audioCommandGenerator = new();
            Subtitle.CommandGenerator subCommandGenerator = new();

            queueElement.UniqueIdentifier = identifier;
            queueElement.Input = videoDB.InputPath;
            queueElement.Output = videoDB.OutputPath;
            queueElement.VideoCommand = CheckBoxCustomVideoSettings.IsOn ? TextBoxCustomVideoSettings.Text : GenerateEncoderCommand();
            queueElement.AudioCommand = audioCommandGenerator.Generate(ListBoxAudioTracks.Items);
            queueElement.SubtitleCommand = skipSubs ? null : subCommandGenerator.GenerateSoftsub(ListBoxSubtitleTracks.Items);
            queueElement.SubtitleBurnCommand = subCommandGenerator.GenerateHardsub(ListBoxSubtitleTracks.Items, identifier);
            queueElement.FilterCommand = GenerateVideoFilters();
            queueElement.FrameCount = videoDB.MIFrameCount;
            queueElement.EncodingMethod = ComboBoxVideoEncoder.SelectedIndex;
            queueElement.ChunkingMethod = ComboBoxChunkingMethod.SelectedIndex;
            queueElement.ReencodeMethod = ComboBoxReencodeMethod.SelectedIndex;
            queueElement.Passes = CheckBoxTwoPassEncoding.IsOn ? 2 : 1;
            queueElement.ChunkLength = int.Parse(TextBoxChunkLength.Text);
            queueElement.PySceneDetectThreshold = float.Parse(TextBoxPySceneDetectThreshold.Text);
            queueElement.VFR = CheckBoxVideoVFR.IsChecked == true;
            queueElement.Preset = PresetSettings;
            queueElement.VideoDB = videoDB;

            if (ToggleSwitchFilterDeinterlace.IsOn && ComboBoxFiltersDeinterlace.SelectedIndex is 1 or 2)
            {
                queueElement.FrameCount += queueElement.FrameCount;
            }

            // Add to Queue
            ListBoxQueue.Items.Add(queueElement);

            Directory.CreateDirectory(Path.Combine(Global.AppData, "NEAV1E", "Queue"));

            // Save as JSON
            File.WriteAllText(Path.Combine(Global.AppData, "NEAV1E", "Queue", videoDB.InputFileName + "_" + identifier + ".json"), JsonConvert.SerializeObject(queueElement, Formatting.Indented));
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
            if (!File.Exists(queueElement.VideoDB.OutputPath)) {
                queueElement.Status = "Error: No Output detected";
                return;
            }

            FileInfo videoOutput = new(queueElement.VideoDB.OutputPath);
            if (videoOutput.Length <= 50000) {
                queueElement.Status = "Possible Muxing Error";
                return;
            }

            TimeSpan timespent = DateTime.Now - startTime;
            try {
                queueElement.Status = "Finished Encoding - Elapsed Time " + timespent.ToString("hh\\:mm\\:ss") + " - avg " + Math.Round(queueElement.FrameCount / timespent.TotalSeconds, 2) + "fps";
            }
            catch
            {
                queueElement.Status = "Finished Encoding - Elapsed Time " + timespent.ToString("hh\\:mm\\:ss") + " - Error calculating average FPS";
            }


            if (settingsDB.DeleteTempFiles) {
                try {
                    DirectoryInfo tmp = new(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier));
                    tmp.Delete(true);
                } catch {
                    queueElement.Status = "Error Deleting Temp Files";
                }
            }
        }
        #endregion

        #region Encoder Settings

        // ════════════════════════════════════ Video Filters ═════════════════════════════════════

        private string GenerateVideoFilters()
        {
            bool crop = ToggleSwitchFilterCrop.IsOn;
            bool rotate = ToggleSwitchFilterRotate.IsOn;
            bool resize = ToggleSwitchFilterResize.IsOn;
            bool deinterlace = ToggleSwitchFilterDeinterlace.IsOn;
            bool _oneFilter = false;

            string FilterCommand = "";

            if (crop || rotate || resize || deinterlace)
            {
                FilterCommand = " -vf ";
                if (resize)
                {
                    // Has to be last, due to scaling algorithm
                    FilterCommand += VideoFiltersResize();
                    _oneFilter = true;
                }
                if (crop)
                {
                    if (_oneFilter) { FilterCommand += ","; }
                    FilterCommand += VideoFiltersCrop();
                    _oneFilter = true;
                }
                if (rotate)
                {
                    if (_oneFilter) { FilterCommand += ","; }
                    FilterCommand += VideoFiltersRotate();
                    _oneFilter = true;
                }
                if (deinterlace)
                {
                    if (_oneFilter) { FilterCommand += ","; }
                    FilterCommand += VideoFiltersDeinterlace();
                }
            }

            return FilterCommand;
        }

        private string VideoFiltersCrop()
        {
            // Sets the values for cropping the video
            string widthNew = (int.Parse(TextBoxFiltersCropRight.Text) + int.Parse(TextBoxFiltersCropLeft.Text)).ToString();
            string heightNew = (int.Parse(TextBoxFiltersCropTop.Text) + int.Parse(TextBoxFiltersCropBottom.Text)).ToString();
            return "crop=iw-" + widthNew + ":ih-" + heightNew + ":" + TextBoxFiltersCropLeft.Text + ":" + TextBoxFiltersCropTop.Text;
        }

        private string VideoFiltersRotate()
        {
            // Sets the values for rotating the video
            if (ComboBoxFiltersRotate.SelectedIndex == 1) return "transpose=1";
            else if (ComboBoxFiltersRotate.SelectedIndex == 2) return "transpose=2,transpose=2";
            else if (ComboBoxFiltersRotate.SelectedIndex == 3) return "transpose=2";
            else return ""; // If user selected no ratation but still has it enabled
        }

        private string VideoFiltersDeinterlace()
        {
            int filterIndex = ComboBoxFiltersDeinterlace.SelectedIndex;
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
            // Sets the values for scaling the video
            if (TextBoxFiltersResizeWidth.Text != "0")
            {
                // Custom Scale
                return "scale=" + TextBoxFiltersResizeWidth.Text + ":" + TextBoxFiltersResizeHeight.Text + ":flags=" + ComboBoxResizeAlgorithm.Text;
            }
            // Auto Scale
            return "scale=trunc(oh*a/2)*2:" + TextBoxFiltersResizeHeight.Text + ":flags=" + ComboBoxResizeAlgorithm.Text;
        }

        private string GenerateEncoderCommand()
        {
            string _settings = GenerateFFmpegColorSpace() + " " + GenerateFFmpegFramerate() + " ";
            if (ComboBoxVideoEncoder.SelectedIndex == 0)
            {
                return _settings + GenerateAomFFmpegCommand();
            }
            else if (ComboBoxVideoEncoder.SelectedIndex == 1)
            {
                return _settings + GenerateRav1eFFmpegCommand();
            }
            else if (ComboBoxVideoEncoder.SelectedIndex == 2)
            {
                return _settings + GenerateSvtAV1FFmpegCommand();
            }
            else if (ComboBoxVideoEncoder.SelectedIndex == 3)
            {
                return _settings + GenerateVpxVP9Command();
            }
            else if (ComboBoxVideoEncoder.SelectedIndex == 5)
            {
                return _settings + GenerateAomencCommand();
            }
            else if (ComboBoxVideoEncoder.SelectedIndex == 6)
            {
                return _settings + GenerateRav1eCommand();
            }
            else if (ComboBoxVideoEncoder.SelectedIndex == 7)
            {
                return _settings + GenerateSvtAV1Command();
            }
            else if (ComboBoxVideoEncoder.SelectedIndex == 9)
            {
                return _settings + GenerateHEVCFFmpegCommand();
            }
            else if (ComboBoxVideoEncoder.SelectedIndex == 10)
            {
                return _settings + GenerateAVCFFmpegCommand();
            }

            return "";
        }

        private string GenerateAomFFmpegCommand()
        {
            string _settings = "-c:v libaom-av1";

            if (ComboBoxQualityMode.SelectedIndex == 0)
            {
                _settings += " -crf " + SliderQuality.Value + " -b:v 0";
            }
            else if (ComboBoxQualityMode.SelectedIndex == 1)
            {
                _settings += " -crf " + SliderQuality.Value + " -b:v " + TextBoxMaxBitrate.Text + "k";
            }
            else if (ComboBoxQualityMode.SelectedIndex == 2)
            {
                _settings += " -b:v " + TextBoxMinBitrate.Text + "k";
            }
            else if (ComboBoxQualityMode.SelectedIndex == 3)
            {
                _settings += " -minrate " + TextBoxMinBitrate.Text + "k -b:v " + TextBoxAVGBitrate.Text + "k -maxrate " + TextBoxMaxBitrate.Text + "k";
            }

            _settings += " -cpu-used " + SliderEncoderPreset.Value;

            if (ToggleSwitchAdvancedSettings.IsOn)
            {
                _settings += " -threads " + ComboBoxAomencThreads.Text;                                      // Threads
                _settings += " -tile-columns " + ComboBoxAomencTileColumns.Text;                             // Tile Columns
                _settings += " -tile-rows " + ComboBoxAomencTileRows.Text;                                   // Tile Rows
                _settings += " -lag-in-frames " + TextBoxAomencLagInFrames.Text;                             // Lag in Frames
                _settings += " -aq-mode " + ComboBoxAomencAQMode.SelectedIndex;                              // AQ-Mode
                _settings += " -tune " + ComboBoxAomencTune.Text;                                            // Tune

                if (TextBoxAomencMaxGOP.Text != "0")
                {
                    _settings += " -g " + TextBoxAomencMaxGOP.Text;                                           // Keyframe Interval
                }
                if (CheckBoxAomencRowMT.IsChecked == false)
                {
                    _settings += " -row-mt 0";                                                                // Row Based Multithreading
                }
                if (CheckBoxAomencCDEF.IsChecked == false)
                {
                    _settings += " -enable-cdef 0";                                                           // Constrained Directional Enhancement Filter
                }

                if (CheckBoxAomencARNRMax.IsChecked == true)
                {
                    _settings += " -arnr-max-frames " + ComboBoxAomencARNRMax.Text;                           // ARNR Maxframes
                    _settings += " -arnr-strength " + ComboBoxAomencARNRStrength.Text;                        // ARNR Strength
                }
                if (CheckBoxRealTimeMode.IsOn)
                {
                    _settings += " -usage realtime ";                                                         // Real Time Mode
                }

                _settings += " -aom-params ";
                _settings += " tune-content=" + ComboBoxAomencTuneContent.Text;                             // Tune-Content
                _settings += ":sharpness=" + ComboBoxAomencSharpness.Text;                                  // Sharpness (Filter)
                _settings += ":enable-keyframe-filtering=" + ComboBoxAomencKeyFiltering.SelectedIndex;      // Key Frame Filtering
                if (ComboBoxAomencColorPrimaries.SelectedIndex != 0)
                {
                    _settings += ":color-primaries=" + ComboBoxAomencColorPrimaries.Text;                   // Color Primaries
                }
                if (ComboBoxAomencColorTransfer.SelectedIndex != 0)
                {
                    _settings += ":transfer-characteristics=" + ComboBoxAomencColorTransfer.Text;           // Color Transfer
                }
                if (ComboBoxAomencColorMatrix.SelectedIndex != 0)
                {
                    _settings += ":matrix-coefficients=" + ComboBoxAomencColorMatrix.Text;                  // Color Matrix
                }
            }
            else
            {
                _settings += " -threads 4 -tile-columns 2 -tile-rows 1 -g " + GenerateKeyFrameInerval();
            }

            return _settings;
        }

        private string GenerateRav1eFFmpegCommand()
        {
            string _settings = "-c:v librav1e";

            if (ComboBoxQualityMode.SelectedIndex == 0)
            {
                _settings += " -qp " + SliderQuality.Value;
            }
            else if (ComboBoxQualityMode.SelectedIndex == 2)
            {
                _settings += " -b:v " + TextBoxAVGBitrate.Text + "k";
            }

            _settings += " -speed " + SliderEncoderPreset.Value;

            if (ToggleSwitchAdvancedSettings.IsOn)
            {
                _settings += " -tile-columns " + ComboBoxRav1eTileColumns.SelectedIndex;                     // Tile Columns
                _settings += " -tile-rows " + ComboBoxRav1eTileRows.SelectedIndex;                           // Tile Rows
                _settings += " -rav1e-params ";
                _settings += "threads=" + ComboBoxRav1eThreads.SelectedIndex;                                 // Threads
                _settings += ":rdo-lookahead-frames=" + TextBoxRav1eLookahead.Text;                           // RDO Lookahead
                _settings += ":tune=" + ComboBoxRav1eTune.Text;                                               // Tune
                if (TextBoxRav1eMaxGOP.Text != "0")
                {
                    _settings += ":keyint=" + TextBoxRav1eMaxGOP.Text;                                        // Keyframe Interval
                }
                if (ComboBoxRav1eColorPrimaries.SelectedIndex != 0)
                {
                    _settings += ":primaries=" + ComboBoxRav1eColorPrimaries.Text;                            // Color Primaries
                }
                if (ComboBoxRav1eColorTransfer.SelectedIndex != 0)
                {
                    _settings += ":transfer=" + ComboBoxRav1eColorTransfer.Text;                              // Color Transfer
                }
                if (ComboBoxRav1eColorMatrix.SelectedIndex != 0)
                {
                    _settings += ":matrix=" + ComboBoxRav1eColorMatrix.Text;                                  // Color Matrix
                }
                if (CheckBoxRav1eMasteringDisplay.IsChecked == true)
                {
                    _settings += ":mastering-display=G(" + TextBoxRav1eMasteringGx.Text + ",";                // Mastering Gx
                    _settings += TextBoxRav1eMasteringGy.Text + ")B(";                                        // Mastering Gy
                    _settings += TextBoxRav1eMasteringBx.Text + ",";                                          // Mastering Bx
                    _settings += TextBoxRav1eMasteringBy.Text + ")R(";                                        // Mastering By
                    _settings += TextBoxRav1eMasteringRx.Text + ",";                                          // Mastering Rx
                    _settings += TextBoxRav1eMasteringRy.Text + ")WP(";                                       // Mastering Ry
                    _settings += TextBoxRav1eMasteringWPx.Text + ",";                                         // Mastering WPx
                    _settings += TextBoxRav1eMasteringWPy.Text + ")L(";                                       // Mastering WPy
                    _settings += TextBoxRav1eMasteringLx.Text + ",";                                          // Mastering Lx
                    _settings += TextBoxRav1eMasteringLy.Text + ")";                                          // Mastering Ly
                }
                if (CheckBoxRav1eContentLight.IsChecked == true)
                {
                    _settings += ":content-light=" + TextBoxRav1eContentLightCll.Text;                        // Content Light CLL
                    _settings += "," + TextBoxRav1eContentLightFall.Text;                                     // Content Light FALL
                }
            }
            else
            {
                _settings += " -tile-columns 2 -tile-rows 1 -g " + GenerateKeyFrameInerval() + " -rav1e-params threads=4";
            }

            return _settings;
        }

        private string GenerateSvtAV1FFmpegCommand()
        {
            string _settings = "-c:v libsvtav1";

            if (ComboBoxQualityMode.SelectedIndex == 0)
            {
                _settings += " -rc 0 -qp " + SliderQuality.Value;
            }
            else if (ComboBoxQualityMode.SelectedIndex == 2)
            {
                _settings += " -rc 1 -b:v " + TextBoxAVGBitrate.Text + "k";
            }

            _settings += " -preset " + SliderEncoderPreset.Value;

            if (ToggleSwitchAdvancedSettings.IsOn)
            {
                _settings += " -tile_columns " + ComboBoxSVTAV1TileColumns.Text;                              // Tile Columns
                _settings += " -tile_rows " + ComboBoxSVTAV1TileRows.Text;                                    // Tile Rows
                _settings += " -g " + TextBoxSVTAV1MaxGOP.Text;                                               // Keyframe Interval
                _settings += " -la_depth " + TextBoxSVTAV1Lookahead.Text;                                     // Lookahead
            }
            else
            {
                _settings += " -g " + GenerateKeyFrameInerval();
            }

            return _settings;
        }

        private string GenerateVpxVP9Command()
        {
            string _settings = "-c:v libvpx-vp9";

            if (ComboBoxQualityMode.SelectedIndex == 0)
            {
                _settings += " -crf " + SliderQuality.Value + " -b:v 0";
            }
            else if (ComboBoxQualityMode.SelectedIndex == 1)
            {
                _settings += " -crf " + SliderQuality.Value + " -b:v " + TextBoxMaxBitrate.Text + "k";
            }
            else if (ComboBoxQualityMode.SelectedIndex == 2)
            {
                _settings += " -b:v " + TextBoxMinBitrate.Text + "k";
            }
            else if (ComboBoxQualityMode.SelectedIndex == 3)
            {
                _settings += " -minrate " + TextBoxMinBitrate.Text + "k -b:v " + TextBoxAVGBitrate.Text + "k -maxrate " + TextBoxMaxBitrate.Text + "k";
            }

            _settings += " -cpu-used " + SliderEncoderPreset.Value;

            if (ToggleSwitchAdvancedSettings.IsOn)
            {
                _settings += " -threads " + ComboBoxVP9Threads.Text;                        // Max Threads
                _settings += " -tile-columns " + ComboBoxVP9TileColumns.SelectedIndex;      // Tile Columns
                _settings += " -tile-rows " + ComboBoxVP9TileRows.SelectedIndex;            // Tile Rows
                _settings += " -lag-in-frames " + TextBoxVP9LagInFrames.Text;               // Lag in Frames
                _settings += " -g " + TextBoxVP9MaxKF.Text;                                 // Max GOP
                _settings += " -aq-mode " + ComboBoxVP9AQMode.SelectedIndex;                // AQ-Mode
                _settings += " -tune " + ComboBoxVP9ATune.SelectedIndex;                    // Tune
                _settings += " -tune-content " + ComboBoxVP9ATuneContent.SelectedIndex;     // Tune-Content
                if (CheckBoxVP9ARNR.IsChecked == true)
                {
                    _settings += " -arnr-maxframes " + ComboBoxAomencVP9Max.Text;           // ARNR Max Frames
                    _settings += " -arnr-strength " + ComboBoxAomencVP9Strength.Text;       // ARNR Strength
                    _settings += " -arnr-type " + ComboBoxAomencVP9ARNRType.Text;           // ARNR Type
                }
            }
            else
            {
                _settings += " -threads 4 -tile-columns 2 -tile-rows 1 -g " + GenerateKeyFrameInerval();
            }

            return _settings;
        }

        private string GenerateAomencCommand()
        {
            string _settings = "-f yuv4mpegpipe - | ";

            _settings += "\"" + Path.Combine(Directory.GetCurrentDirectory(), "Apps", "aomenc", "aomenc.exe") + "\" -";

            if (ComboBoxQualityMode.SelectedIndex == 0)
            {
                _settings += " --cq-level=" + SliderQuality.Value + " --end-usage=q";
            }
            else if (ComboBoxQualityMode.SelectedIndex == 1)
            {
                _settings += " --cq-level=" + SliderQuality.Value + " --target-bitrate=" + TextBoxMaxBitrate.Text + " --end-usage=cq";
            }
            else if (ComboBoxQualityMode.SelectedIndex == 2)
            {
                _settings += " --target-bitrate=" + TextBoxMinBitrate.Text + " --end-usage=vbr";
            }

            _settings += " --cpu-used=" + SliderEncoderPreset.Value;

            if (ToggleSwitchAdvancedSettings.IsOn)
            {
                _settings += " --threads=" + ComboBoxAomencThreads.Text;                                      // Threads
                _settings += " --tile-columns=" + ComboBoxAomencTileColumns.Text;                             // Tile Columns
                _settings += " --tile-rows=" + ComboBoxAomencTileRows.Text;                                   // Tile Rows
                _settings += " --lag-in-frames=" + TextBoxAomencLagInFrames.Text;                             // Lag in Frames
                _settings += " --sharpness=" + ComboBoxAomencSharpness.Text;                                  // Sharpness (Filter)
                _settings += " --aq-mode=" + ComboBoxAomencAQMode.SelectedIndex;                              // AQ-Mode
                _settings += " --enable-keyframe-filtering=" + ComboBoxAomencKeyFiltering.SelectedIndex;      // Key Frame Filtering
                _settings += " --tune=" + ComboBoxAomencTune.Text;                                            // Tune
                _settings += " --tune-content=" + ComboBoxAomencTuneContent.Text;                             // Tune-Content
                if (TextBoxAomencMaxGOP.Text != "0")
                {
                    _settings += " --kf-max-dist=" + TextBoxAomencMaxGOP.Text;                                // Keyframe Interval
                }
                if (CheckBoxAomencRowMT.IsChecked == false)
                {
                    _settings += " --row-mt=0";                                                               // Row Based Multithreading
                }
                if (ComboBoxAomencColorPrimaries.SelectedIndex != 0)
                {
                    _settings += " --color-primaries=" + ComboBoxAomencColorPrimaries.Text;                   // Color Primaries
                }
                if (ComboBoxAomencColorTransfer.SelectedIndex != 0)
                {
                    _settings += " --transfer-characteristics=" + ComboBoxAomencColorTransfer.Text;           // Color Transfer
                }
                if (ComboBoxAomencColorMatrix.SelectedIndex != 0)
                {
                    _settings += " --matrix-coefficients=" + ComboBoxAomencColorMatrix.Text;                  // Color Matrix
                }
                if (CheckBoxAomencCDEF.IsChecked == false)
                {
                    _settings += " --enable-cdef=0";                                                          // Constrained Directional Enhancement Filter
                }
                if (CheckBoxAomencARNRMax.IsChecked == true)
                {
                    _settings += " --arnr-maxframes=" + ComboBoxAomencARNRMax.Text;                           // ARNR Maxframes
                    _settings += " --arnr-strength=" + ComboBoxAomencARNRStrength.Text;                       // ARNR Strength
                }
                if (CheckBoxRealTimeMode.IsOn)
                {
                    _settings += " --rt";                                                                     // Real Time Mode
                }
            }
            else
            {
                _settings += " --threads=4 --tile-columns=2 --tile-rows=1 --kf-max-dist=" + GenerateKeyFrameInerval();
            }

            return _settings;
        }

        private string GenerateRav1eCommand()
        {
            string _settings = "-f yuv4mpegpipe - | ";

            _settings += "\"" + Path.Combine(Directory.GetCurrentDirectory(), "Apps", "rav1e", "rav1e.exe") + "\" - -y";

            if (ComboBoxQualityMode.SelectedIndex == 0)
            {
                _settings += " --quantizer " + SliderQuality.Value;
            }
            else if (ComboBoxQualityMode.SelectedIndex == 2)
            {
                _settings += " --bitrate " + TextBoxAVGBitrate.Text;
            }

            _settings += " --speed " + SliderEncoderPreset.Value;

            if (ToggleSwitchAdvancedSettings.IsOn)
            {
                _settings += " --threads " + ComboBoxRav1eThreads.SelectedIndex;                              // Threads
                _settings += " --tile-cols " + ComboBoxRav1eTileColumns.SelectedIndex;                        // Tile Columns
                _settings += " --tile-rows " + ComboBoxRav1eTileRows.SelectedIndex;                           // Tile Rows
                _settings += " --rdo-lookahead-frames " + TextBoxRav1eLookahead.Text;                         // RDO Lookahead
                _settings += " --tune " + ComboBoxRav1eTune.Text;                                             // Tune

                if (TextBoxRav1eMaxGOP.Text != "0")
                {
                    _settings += " --keyint " + TextBoxRav1eMaxGOP.Text;                                      // Keyframe Interval
                }
                if (ComboBoxRav1eColorPrimaries.SelectedIndex != 0)
                {
                    _settings += " --primaries " + ComboBoxRav1eColorPrimaries.Text;                          // Color Primaries
                }
                if (ComboBoxRav1eColorTransfer.SelectedIndex != 0)
                {
                    _settings += " --transfer " + ComboBoxRav1eColorTransfer.Text;                            // Color Transfer
                }
                if (ComboBoxRav1eColorMatrix.SelectedIndex != 0)
                {
                    _settings += " --matrix " + ComboBoxRav1eColorMatrix.Text;                                // Color Matrix
                }
                if (CheckBoxRav1eMasteringDisplay.IsChecked == true)
                {
                    _settings += " --mastering-display G(" + TextBoxRav1eMasteringGx.Text + ",";              // Mastering Gx
                    _settings += TextBoxRav1eMasteringGy.Text + ")B(";                                        // Mastering Gy
                    _settings += TextBoxRav1eMasteringBx.Text + ",";                                          // Mastering Bx
                    _settings += TextBoxRav1eMasteringBy.Text + ")R(";                                        // Mastering By
                    _settings += TextBoxRav1eMasteringRx.Text + ",";                                          // Mastering Rx
                    _settings += TextBoxRav1eMasteringRy.Text + ")WP(";                                       // Mastering Ry
                    _settings += TextBoxRav1eMasteringWPx.Text + ",";                                         // Mastering WPx
                    _settings += TextBoxRav1eMasteringWPy.Text + ")L(";                                       // Mastering WPy
                    _settings += TextBoxRav1eMasteringLx.Text + ",";                                          // Mastering Lx
                    _settings += TextBoxRav1eMasteringLy.Text + ")";                                          // Mastering Ly
                }
                if (CheckBoxRav1eContentLight.IsChecked == true)
                {
                    _settings += " --content-light " + TextBoxRav1eContentLightCll.Text;                      // Content Light CLL
                    _settings += "," + TextBoxRav1eContentLightFall.Text;                                     // Content Light FALL
                }
            }
            else
            {
                _settings += " --threads 4 --tile-cols 2 --tile-rows 1 --keyint " + GenerateKeyFrameInerval();
            }

            return _settings;
        }

        private string GenerateSvtAV1Command()
        {
            string _settings = "-nostdin -f yuv4mpegpipe - | ";

            _settings += "\"" + Path.Combine(Directory.GetCurrentDirectory(), "Apps", "svt-av1", "SvtAv1EncApp.exe") + "\" -i stdin";

            if (ComboBoxQualityMode.SelectedIndex == 0)
            {
                _settings += " --rc 0 --crf " + SliderQuality.Value;
            }
            else if (ComboBoxQualityMode.SelectedIndex == 2)
            {
                _settings += " --rc 1 --tbr " + TextBoxAVGBitrate.Text;
            }

            _settings += " --preset " + SliderEncoderPreset.Value;

            if (ToggleSwitchAdvancedSettings.IsOn)
            {
                _settings += " --tile-columns " + ComboBoxSVTAV1TileColumns.Text;                             // Tile Columns
                _settings += " --tile-rows " + ComboBoxSVTAV1TileRows.Text;                                   // Tile Rows
                _settings += " --keyint " + TextBoxSVTAV1MaxGOP.Text;                                         // Keyframe Interval
                _settings += " --lookahead " + TextBoxSVTAV1Lookahead.Text;                                   // Lookahead
            }
            else
            {
                _settings += " --keyint " + GenerateKeyFrameInerval();
            }

            return _settings;
        }

        private string GenerateHEVCFFmpegCommand()
        {
            string _settings = "-c:v libx265";

            if (ComboBoxQualityMode.SelectedIndex == 0)
            {
                _settings += " -crf " + SliderQuality.Value;
            }
            else if (ComboBoxQualityMode.SelectedIndex == 2)
            {
                _settings += " -b:v " + TextBoxAVGBitrate.Text + "k";
            }

            _settings += " -preset ";
            _settings += GenerateMPEGEncoderSpeed();

            return _settings;
        }

        private string GenerateAVCFFmpegCommand()
        {
            string _settings = "-c:v libx264";

            if (ComboBoxQualityMode.SelectedIndex == 0)
            {
                _settings += " -crf " + SliderQuality.Value;
            }
            else if (ComboBoxQualityMode.SelectedIndex == 2)
            {
                _settings += " -b:v " + TextBoxAVGBitrate.Text + "k";
            }

            _settings += " -preset ";
            _settings += GenerateMPEGEncoderSpeed();

            return _settings;
        }

        private string GenerateMPEGEncoderSpeed()
        {
            return SliderEncoderPreset.Value switch
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

        private string GenerateKeyFrameInerval()
        {
            int seconds = 10;

            // Custom Framerate
            if (ComboBoxVideoFrameRate.SelectedIndex != 0)
            {
                try
                {
                    string selectedFramerate = ComboBoxVideoFrameRate.Text;
                    if (ComboBoxVideoFrameRate.SelectedIndex == 6) { selectedFramerate = "24"; }
                    if (ComboBoxVideoFrameRate.SelectedIndex == 9) { selectedFramerate = "30"; }
                    if (ComboBoxVideoFrameRate.SelectedIndex == 13) { selectedFramerate = "60"; }
                    int frames = int.Parse(selectedFramerate) * seconds;
                    return frames.ToString();
                } catch { }
            }

            // Framerate of Video if it's not VFR and MediaInfo Detected it
            if (!videoDB.MIIsVFR && !string.IsNullOrEmpty(videoDB.MIFramerate))
            {  
                try
                {
                    int framerate = int.Parse(videoDB.MIFramerate);
                    int frames = framerate * seconds;
                    return frames.ToString();
                } catch { }
            }

            return "240";
        }

        private string GenerateFFmpegColorSpace()
        {
            string _settings = "-pix_fmt yuv4";
            if (ComboBoxColorFormat.SelectedIndex == 0)
            {
                _settings += "20p";
            }
            else if (ComboBoxColorFormat.SelectedIndex == 1)
            {
                _settings += "22p";
            }
            else if (ComboBoxColorFormat.SelectedIndex == 2)
            {
                _settings += "44p";
            }
            if (ComboBoxVideoBitDepth.SelectedIndex == 1)
            {
                _settings += "10le -strict -1";
            }
            else if (ComboBoxVideoBitDepth.SelectedIndex == 2)
            {
                _settings += "12le -strict -1";
            }
            return _settings;
        }

        private string GenerateFFmpegFramerate()
        {
            string _settings = "";

            if (ComboBoxVideoFrameRate.SelectedIndex != 0)
            {
                _settings = "-vf fps=" + ComboBoxVideoFrameRate.Text;
                if (ComboBoxVideoFrameRate.SelectedIndex == 6) { _settings = "-vf fps=24000/1001"; }
                if (ComboBoxVideoFrameRate.SelectedIndex == 9) { _settings = "-vf fps=30000/1001"; }
                if (ComboBoxVideoFrameRate.SelectedIndex == 13) { _settings = "-vf fps=60000/1001"; }
            }

            return _settings;
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
                await concurrencySemaphore.WaitAsync(_cancelToken);
                Task task = Task.Run(async () =>
                {
                    try
                    {
                        // Create Output Directory
                        try
                        {
                            if (!Directory.Exists(Path.GetDirectoryName(queueElement.VideoDB.OutputPath)))
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(queueElement.VideoDB.OutputPath));
                            }
                        }
                        catch { }
                        Directory.CreateDirectory(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier));
                        Global.Logger("==========================================================", queueElement.Output + ".log");
                        Global.Logger("INFO  - Started Async Task - UID: " + queueElement.UniqueIdentifier, queueElement.Output + ".log");
                        Global.Logger("INFO  - Input: " + queueElement.Input, queueElement.Output + ".log");
                        Global.Logger("INFO  - Output: " + queueElement.Input, queueElement.Output + ".log");
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
                        if (QueueParallel)
                        {
                            VideoChunks.Add(queueElement.VideoDB.InputPath);
                            Global.Logger("WARN  - Queue is being processed in Parallel", queueElement.Output + ".log");
                        }
                        else
                        {
                            await Task.Run(() => videoSplitter.Split(queueElement, _cancelToken), _cancelToken);

                            if (queueElement.ChunkingMethod == 0)
                            {
                                // Equal Chunking
                                string[] filePaths = Directory.GetFiles(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Chunks"), "*.mkv", SearchOption.TopDirectoryOnly);
                                foreach (string file in filePaths)
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

                            // Starts "a timer" for eta / fps calculation
                            DateTime startTime = DateTime.Now;
                            System.Timers.Timer aTimer = new();
                            aTimer.Elapsed += (sender, e) => { UpdateProgressBar(queueElement, startTime); };
                            aTimer.Interval = 1000;
                            aTimer.Start();

                            // Video Encoding
                            await Task.Run(() => videoEncoder.Encode(WorkerCountElement, VideoChunks, queueElement, QueueParallel, settingsDB.PriorityNormal, _cancelToken), _cancelToken);

                            aTimer.Stop();

                            await Task.Run(() => videoMuxer.Concat(queueElement), _cancelToken);

                            await Task.Run(() => DeleteTempFiles(queueElement, startTime), _cancelToken);
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
            ImageStartStop.Source = new BitmapImage(new Uri(@"/NotEnoughAV1Encodes;component/resources/img/start.png", UriKind.Relative));
            LabelStartPauseButton.Content = LocalizedStrings.Instance["LabelStartPauseButton"];
            ButtonAddToQueue.IsEnabled = true;
            ButtonRemoveSelectedQueueItem.IsEnabled = true;
            ButtonEditSelectedItem.IsEnabled = true;

            Shutdown();
        }
        #endregion

        #region Progressbar
        private static void UpdateProgressBar(Queue.QueueElement queueElement, DateTime startTime)
        {
            TimeSpan timeSpent = DateTime.Now - startTime;
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
                    estimatedFPS1stPass = "   -  ~" + Math.Round(encodedFrames / timeSpent.TotalSeconds, 2).ToString("0.00") + "fps";
                    estimatedTime1stPass = "   -  ~" + Math.Round(((timeSpent.TotalSeconds / encodedFrames) * (queueElement.FrameCount - encodedFrames)) / 60, MidpointRounding.ToEven) + LocalizedStrings.Instance["QueueMinLeft"];
                }

                if(encodedFramesSecondPass != queueElement.FrameCount)
                {
                    estimatedFPS2ndPass = "   -  ~" + Math.Round(encodedFramesSecondPass / timeSpent.TotalSeconds, 2).ToString("0.00") + "fps";
                    estimatedTime2ndPass = "   -  ~" + Math.Round(((timeSpent.TotalSeconds / encodedFramesSecondPass) * (queueElement.FrameCount - encodedFramesSecondPass)) / 60, MidpointRounding.ToEven) + LocalizedStrings.Instance["QueueMinLeft"];
                }
                
                queueElement.Status = LocalizedStrings.Instance["Queue1stPass"] + " " + ((decimal)encodedFrames / queueElement.FrameCount).ToString("00.00%") + estimatedFPS1stPass + estimatedTime1stPass + " - " + LocalizedStrings.Instance["Queue2ndPass"] + " " + ((decimal)encodedFramesSecondPass / queueElement.FrameCount).ToString("00.00%") + estimatedFPS2ndPass + estimatedTime2ndPass;
            }
            else
            {
                // 1 Pass encoding
                string estimatedFPS = "   -  ~" + Math.Round(encodedFrames / timeSpent.TotalSeconds, 2).ToString("0.00") + "fps";
                string estimatedTime = "   -  ~" + Math.Round(((timeSpent.TotalSeconds / encodedFrames) * (queueElement.FrameCount - encodedFrames)) / 60, MidpointRounding.ToEven) + LocalizedStrings.Instance["QueueMinLeft"];

                queueElement.Status = "Encoded: " + ((decimal)encodedFrames / queueElement.FrameCount).ToString("00.00%") + estimatedFPS + estimatedTime;
            }
        }
        #endregion
    }
}
