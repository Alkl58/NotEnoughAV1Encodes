using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using MahApps.Metro.Controls;
using System.Xml;
using ControlzEx.Theming;

namespace NotEnoughAV1Encodes
{
    public partial class MainWindow : MetroWindow
    {

        // Final Commands
        public static string FilterCommand = null;
        public static string PipeBitDepthCommand = null;
        public static string EncoderAomencCommand = null;
        public static string EncoderRav1eCommand = null;
        public static string EncoderSvtAV1Command = null;
        // Temp Settings
        public static int WorkerCount = 0;          // amount of workers
        public static int EncodeMethod = 0;         // 0 = aomenc, 1 = rav1e, 2 = svt-av1...
        public static int SplitMethod = 0;          // 0 = ffmpeg; 1 = pyscenedetect; 2 = chunking
        public static bool OnePass = true;          // true = Onepass, false = Twopass
        public static bool Priority = true;         // true = normal, false = below normal (process priority)
        public static bool Logging = true;          // Program Logging
        public static string[] VideoChunks;         // Array of command/videochunks
        // Temp Settings Audio
        public static bool trackOne;                // Audio Track One active
        public static bool trackTwo;                // Audio Track Two active
        public static bool trackThree;              // Audio Track Three active
        public static bool trackFour;               // Audio Track Four active
        public static bool pcmBluray;               // Audio PCM Copy
        public static string trackOneLanguage;      // Audio Track One Language
        public static string trackTwoLanguage;      // Audio Track Two Language
        public static string trackThreeLanguage;    // Audio Track Three Language
        public static string trackFourLanguage;     // Audio Track Four Language
        public static string audioCodecTrackOne;    // Audio Track One Codec
        public static string audioCodecTrackTwo;    // Audio Track Two Codec
        public static string audioCodecTrackThree;  // Audio Track Three Codec
        public static string audioCodecTrackFour;   // Audio Track Four Codec
        public static int audioBitrateTrackOne;     // Audio Track One Bitrate
        public static int audioBitrateTrackTwo;     // Audio Track Two Bitrate
        public static int audioBitrateTrackThree;   // Audio Track Three Bitrate
        public static int audioBitrateTrackFour;    // Audio Track Four Bitrate
        public static int audioChannelsTrackOne;    // Audio Track One Channels
        public static int audioChannelsTrackTwo;    // Audio Track Two Channels
        public static int audioChannelsTrackThree;  // Audio Track Three Channels
        public static int audioChannelsTrackFour;   // Audio Track Four Channels
        // Temp Settings Subtitles
        public static string subCommand;            // Subtitle Muxing Command
        public static string subHardCommand;        // Subtitle Hardcoding Command
        public static bool subSoftSubEnabled;       // Subtitle Toggle for later Muxing
        public static bool subHardSubEnabled;       // Subtitle Toggle for hardsub
        // IO Paths
        public static string TempPath = Path.Combine(Path.GetTempPath(), "NEAV1E");
        public static string TempPathFileName = null;
        public static string VideoInput = null;     // Video Input Path
        public static string VideoOutput = null;    // Video Output Path
        public static bool VideoInputSet = false;   // Video Input Set Boolean
        public static bool VideoOutputSet = false;  // Video Output Set Boolean
        // Dependencies Paths
        public static string FFmpegPath = null;     // Path to ffmpeg
        public static string AomencPath = null;     // Path to aomenc
        public static string Rav1ePath = null;      // Path to rav1e
        public static string SvtAV1Path = null;     // Path to svt-av1
        public static string MKVToolNixPath = null; // Path to mkvtoolnix
        // Temp Variables
        public static bool EncodeStarted = false;   // Encode Started Boolean
        public static bool DeleteTempFiles = false; // Temp File Deletion
        public static bool PlayUISounds = false;    // UI Sounds (Finished Encoding / Error)
        public static bool ShowTerminal = false;    // Show / Hide Encoding Terminal
        public static int TotalFrames = 0;          // used for progressbar and frame check
        public DateTime StartTime;                  // used for eta calculation
        // Progress Cancellation
        private CancellationTokenSource cancellationTokenSource;

        public MainWindow()
        {
            InitializeComponent();
            Startup();
        }

        // ═══════════════════════════════════════ UI Logic ═══════════════════════════════════════

        // Subtitle UI

        private string SubtitleFiledialog()
        {
            // Opens OpenFileDialog for subtitles
            OpenFileDialog openSubtitleFileDialog = new OpenFileDialog();
            openSubtitleFileDialog.Filter = "Subtitle Files|*.pgs;*.srt;*.sup;*.ass;*.ssa;|All Files|*.*";
            Nullable<bool> result = openSubtitleFileDialog.ShowDialog();
            if (result == true)
            {
                return openSubtitleFileDialog.FileName;
            }
            return null;
        }

        private void ButtonSubtitleTrackOne_Click(object sender, RoutedEventArgs e)
        {
            TextBoxSubtitleTrackOne.Text = SubtitleFiledialog();
        }

        private void ButtonSubtitleTrackTwo_Click(object sender, RoutedEventArgs e)
        {
            TextBoxSubtitleTrackTwo.Text = SubtitleFiledialog();
        }

        private void ButtonSubtitleTrackThree_Click(object sender, RoutedEventArgs e)
        {
            TextBoxSubtitleTrackThree.Text = SubtitleFiledialog();
        }

        private void ButtonSubtitleTrackFour_Click(object sender, RoutedEventArgs e)
        {
            TextBoxSubtitleTrackFour.Text = SubtitleFiledialog();
        }

        private void ButtonSubtitleTrackFive_Click(object sender, RoutedEventArgs e)
        {
            TextBoxSubtitleTrackFive.Text = SubtitleFiledialog();
        }

        private void CheckBoxSubOneBurn_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxSubTwoBurn.IsChecked = false;
            CheckBoxSubThreeBurn.IsChecked = false;
            CheckBoxSubFourBurn.IsChecked = false;
            CheckBoxSubFiveBurn.IsChecked = false;
            CheckBoxSubOneDefault.IsChecked = false;
        }

        private void CheckBoxSubTwoBurn_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxSubOneBurn.IsChecked = false;
            CheckBoxSubThreeBurn.IsChecked = false;
            CheckBoxSubFourBurn.IsChecked = false;
            CheckBoxSubFiveBurn.IsChecked = false;
            CheckBoxSubTwoDefault.IsChecked = false;
        }

        private void CheckBoxSubThreeBurn_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxSubOneBurn.IsChecked = false;
            CheckBoxSubTwoBurn.IsChecked = false;
            CheckBoxSubFourBurn.IsChecked = false;
            CheckBoxSubFiveBurn.IsChecked = false;
            CheckBoxSubThreeDefault.IsChecked = false;
        }

        private void CheckBoxSubFourBurn_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxSubOneBurn.IsChecked = false;
            CheckBoxSubTwoBurn.IsChecked = false;
            CheckBoxSubThreeBurn.IsChecked = false;
            CheckBoxSubFiveBurn.IsChecked = false;
            CheckBoxSubFourDefault.IsChecked = false;
        }

        private void CheckBoxSubFiveBurn_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxSubOneBurn.IsChecked = false;
            CheckBoxSubTwoBurn.IsChecked = false;
            CheckBoxSubThreeBurn.IsChecked = false;
            CheckBoxSubFourBurn.IsChecked = false;
            CheckBoxSubFiveDefault.IsChecked = false;
        }

        private void CheckBoxSubOneDefault_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxSubTwoDefault.IsChecked = false;
            CheckBoxSubThreeDefault.IsChecked = false;
            CheckBoxSubFourDefault.IsChecked = false;
            CheckBoxSubFiveDefault.IsChecked = false;
            CheckBoxSubOneBurn.IsChecked = false;
        }

        private void CheckBoxSubTwoDefault_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxSubOneDefault.IsChecked = false;
            CheckBoxSubThreeDefault.IsChecked = false;
            CheckBoxSubFourDefault.IsChecked = false;
            CheckBoxSubFiveDefault.IsChecked = false;
            CheckBoxSubTwoBurn.IsChecked = false;
        }

        private void CheckBoxSubThreeDefault_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxSubOneDefault.IsChecked = false;
            CheckBoxSubTwoDefault.IsChecked = false;
            CheckBoxSubFourDefault.IsChecked = false;
            CheckBoxSubFiveDefault.IsChecked = false;
            CheckBoxSubThreeBurn.IsChecked = false;
        }

        private void CheckBoxSubFourDefault_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxSubOneDefault.IsChecked = false;
            CheckBoxSubTwoDefault.IsChecked = false;
            CheckBoxSubThreeDefault.IsChecked = false;
            CheckBoxSubFiveDefault.IsChecked = false;
            CheckBoxSubFourBurn.IsChecked = false;
        }

        private void CheckBoxSubFiveDefault_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxSubOneDefault.IsChecked = false;
            CheckBoxSubTwoDefault.IsChecked = false;
            CheckBoxSubThreeDefault.IsChecked = false;
            CheckBoxSubFourDefault.IsChecked = false;
            CheckBoxSubFiveBurn.IsChecked = false;
        }

        // ----------
        private void ComboBoxVideoPasses_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Reverts to 1 Pass encoding if Real Time Mode is activated 
            if (CheckBoxVideoAomencRealTime != null)
            {
                if (CheckBoxVideoAomencRealTime.IsChecked == true && ComboBoxVideoPasses.SelectedIndex == 1)
                {
                    ComboBoxVideoPasses.SelectedIndex = 0;
                }
            } 
        }

        private void CheckBoxVideoAomencRealTime_Checked(object sender, RoutedEventArgs e)
        {
            // Reverts to 1 Pass encoding if Real Time Mode is activated 
            if (CheckBoxVideoAomencRealTime.IsChecked == true && ComboBoxVideoPasses.SelectedIndex == 1)
            {
                ComboBoxVideoPasses.SelectedIndex = 0;
            }
        }

        private void SliderVideoSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Shows / Hides Real Time Mode CheckBox
            if (CheckBoxVideoAomencRealTime != null)
            {
                if (ComboBoxVideoEncoder.SelectedIndex == 0)
                {
                    if (SliderVideoSpeed.Value >= 5)
                    {
                        CheckBoxVideoAomencRealTime.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        CheckBoxVideoAomencRealTime.IsChecked = false;
                        CheckBoxVideoAomencRealTime.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    CheckBoxVideoAomencRealTime.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Validates that the TextBox Input are only numbers
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Sets the TaskBar Progressbar
            double taskMax = ProgressBar.Maximum, taskVal = ProgressBar.Value;
            TaskbarItemInfo.ProgressValue = (1.0 / taskMax) * taskVal;
        }

        private void Startup()
        {
            // Sets the GUI Version
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            LabelVersion.Content = version.Remove(version.Length - 2);

            CheckDependencies.Check();

            // Sets the workercount combobox
            int corecount = SmallFunctions.getCoreCount();
            for (int i = 1; i <= corecount; i++) { ComboBoxWorkerCount.Items.Add(i); }
            ComboBoxWorkerCount.SelectedItem = Convert.ToInt32(corecount * 75 / 100);

            LoadPresetsIntoComboBox();
            LoadDefaultProfile();
            LoadSettingsTab();
        }

        private void LoadPresetsIntoComboBox()
        {
            // Loads all Presets into ComboBox
            try
            {
                if (Directory.Exists("Profiles"))
                {
                    // DirectoryInfo of Profiles Folder
                    DirectoryInfo profiles = new DirectoryInfo("Profiles");
                    // Gets all .xml file -> add to FileInfo Array
                    FileInfo[] Files = profiles.GetFiles("*.xml");
                    // Sets the ComboBox with the FileInfo Array
                    ComboBoxPresets.ItemsSource = Files;
                }
            }
            catch { }
        }

        private void LoadDefaultProfile()
        {
            // Loads the default profile
            try
            {
                // If the default.xml file exist, it will try to load it
                bool fileExist = File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Profiles", "Default", "default.xml"));
                if (fileExist)
                {
                    XmlDocument doc = new XmlDocument();
                    string directory = Path.Combine(Directory.GetCurrentDirectory(), "Profiles", "Default", "default.xml");
                    doc.Load(directory);
                    XmlNodeList node = doc.GetElementsByTagName("Settings");
                    foreach (XmlNode n in node[0].ChildNodes) { if (n.Name == "DefaultProfile") { ComboBoxPresets.Text = n.InnerText; } }
                }
            }
            catch { }
        }

        private void ButtonSetDefaultPreset_Click(object sender, RoutedEventArgs e)
        {
            // Sets the default Profile
            try
            {
                // Checks / Creates Folder for default.xml file
                if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Profiles", "Default")))
                    Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Profiles", "Default"));
                // Path to default.xml file
                string directory = Path.Combine(Directory.GetCurrentDirectory(), "Profiles", "Default", "default.xml");
                // Init XMLWriter
                XmlWriter writer = XmlWriter.Create(directory);
                // Write XML Start Element
                writer.WriteStartElement("Settings");
                // Write Default Profile
                writer.WriteElementString("DefaultProfile", ComboBoxPresets.Text);
                // Write XML End Element
                writer.WriteEndElement();
                // Close XML Writer
                writer.Close();
            }
            catch { }
        }

        private void ButtonDeletePreset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Deletes the Preset File
                File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "Profiles", ComboBoxPresets.SelectedItem.ToString()));
                // Reloads ComboBox
                LoadPresetsIntoComboBox(); 
            }
            catch { }
        }

        private void ButtonExpandCollapseWindow_Click(object sender, RoutedEventArgs e)
        {
            // Resizes the Window to free screen space
            if (this.Width > 680)
            {
                this.Width = 607;
                this.Height = 210;
            }
            else
            {
                this.Width = 1085;
                this.Height = 650;
            }
        }

        private void ComboBoxPresets_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (ComboBoxPresets.SelectedItem != null)
                {
                    // Loads the selected preset file
                    LoadSettings(true, ComboBoxPresets.SelectedItem.ToString());
                }
                else { }
            }
            catch { }
        }

        private void ComboBoxVideoEncoder_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (SliderVideoSpeed != null)
            {
                if (ComboBoxVideoEncoder.SelectedIndex == 0)
                {
                    // aomenc
                    SliderVideoSpeed.Maximum = 9;
                    SliderVideoSpeed.Value = 4;
                    SliderVideoQuality.Value = 28;
                    SliderVideoQuality.Maximum = 63;
                }
                else if (ComboBoxVideoEncoder.SelectedIndex == 1)
                {
                    // rav1e
                    SliderVideoSpeed.Maximum = 10;
                    SliderVideoSpeed.Value = 6;
                    SliderVideoQuality.Maximum = 255;
                    SliderVideoQuality.Value = 100;
                }
                else if (ComboBoxVideoEncoder.SelectedIndex == 2)
                {
                    // svt-av1
                    SliderVideoSpeed.Maximum = 8;
                    SliderVideoSpeed.Value = 8;
                    SliderVideoQuality.Value = 50;
                    SliderVideoQuality.Maximum = 63;
                    ComboBoxWorkerCount.SelectedIndex = 0;
                }
            }

        }

        private void CheckBoxCustomVideoSettings_Checked(object sender, RoutedEventArgs e)
        {
            // When Checking the custom encoding settings checkbox it will write the current settings to it
            if (CheckBoxCustomVideoSettings.IsChecked == true)
            {
                // Sets the Encoder Settings
                if (ComboBoxVideoEncoder.SelectedIndex == 0) { TextBoxCustomVideoSettings.Text = SetAomencCommand(); }
                if (ComboBoxVideoEncoder.SelectedIndex == 1) { TextBoxCustomVideoSettings.Text = SetRav1eCommand(); }
                if (ComboBoxVideoEncoder.SelectedIndex == 2) { TextBoxCustomVideoSettings.Text = SetSvtAV1Command(); }
            }
        }

        private void CheckBoxSettingsUISounds_Checked(object sender, RoutedEventArgs e)
        {
            PlayUISounds = CheckBoxSettingsUISounds.IsChecked == true;
        }

        private void TextBoxCustomVideoSettings_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Verifies the arguments the user inputs into the encoding settings textbox
            // If the users writes a "forbidden" argument, it will display the text red
            string[] forbiddenWords = { "help", "cfg", "debug", "output", "passes", "pass", "fpf", "limit",
            "skip", "webm", "ivf", "obu", "q-hist", "rate-hist", "fullhelp", "benchmark", "first-pass", "second-pass",
            "reconstruction", "enc-mode-2p", "input-stat-file", "output-stat-file" };

            foreach (var word in forbiddenWords)
            {
                if (ComboBoxBaseTheme.SelectedIndex == 0)
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

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // Opens a Hyperlink in the browser
            Process.Start(e.Uri.ToString());
        }

        private void CancelRoutine()
        {
            ButtonStopEncode.BorderBrush = Brushes.Red;
            ButtonStartEncode.BorderBrush = new SolidColorBrush(Color.FromRgb(228, 228, 228));
            ProgressBar.Foreground = Brushes.Red;
            ProgressBar.Maximum = 100;
            ProgressBar.Value = 100;
            LabelProgressBar.Content = "Cancelled";
        }

        // ══════════════════════════════════════ Main Logic ══════════════════════════════════════

        private async void PreStart()
        {
            // This Function is needed to be able to cancel everything later
            // Button Click is not async and thus can't await MainEntry
            // Thats why we have this function "inbetween"

            // Sets the Temp Path
            if (CheckBoxCustomTempPath.IsChecked == true)
                TempPath = TextBoxCustomTempPath.Text;
            SmallFunctions.Logging("Temp Path: " + TempPath);
            // Resets the global Cancellation Boolean
            SmallFunctions.Cancel.CancelAll = false;
            // Reset Progressbar
            ProgressBar.Value = 0;
            // UI Color Setting
            ProgressBar.Foreground = new SolidColorBrush(Color.FromRgb(6, 176, 37));
            ButtonStartEncode.BorderBrush = Brushes.Green;
            ButtonStopEncode.BorderBrush = new SolidColorBrush(Color.FromRgb(228, 228, 228));
            // Creates new Cancellation Token
            cancellationTokenSource = new CancellationTokenSource();
            // Sets that the encode has started
            EncodeStarted = true;
            // Starts the Main Function
            await MainEntry(cancellationTokenSource.Token);
        }

        public async Task MainEntry(CancellationToken token)
        {
            try
            {
                // Temp Folder Creation
                if (!Directory.Exists(Path.Combine(TempPath, TempPathFileName, "Chunks")))
                    Directory.CreateDirectory(Path.Combine(TempPath, TempPathFileName, "Chunks"));
                Directory.CreateDirectory(Path.Combine(TempPath, TempPathFileName, "Progress"));
                // Sets Temp Settings
                SetEncoderSettings();
                // Set Video Filters
                SetVideoFilters();
                // Set Audio Parameters
                SetAudioSettings();
                // Set Subtitle Parameters
                SetSubtitleParameters();
                // Saves the Project as file
                SaveSettings(false, TempPathFileName);
                // Split Video
                SplitVideo();
                SetTempSettings();
                await Task.Run(() => { token.ThrowIfCancellationRequested(); SmallFunctions.GetSourceFrameCount(); }, token);
                if (trackOne || trackTwo || trackThree || trackFour)
                {
                    LabelProgressBar.Content = "Encoding Audio...";
                    await Task.Run(() => { token.ThrowIfCancellationRequested(); EncodeAudio.Encode(); }, token);
                }
                if (subHardSubEnabled)
                {                    
                    // Sets the new video input
                    VideoInput = Path.Combine(TempPath, TempPathFileName, "tmpsub.mkv");
                }

                ProgressBar.Foreground = new SolidColorBrush(Color.FromRgb(3, 112, 200));
                ProgressBar.Dispatcher.Invoke(() => ProgressBar.Maximum = TotalFrames);
                await Task.Run(() => { token.ThrowIfCancellationRequested(); EncodeVideo(); }, token);
                await Task.Run(async () => { token.ThrowIfCancellationRequested(); await VideoMuxing.Concat(); }, token);
                SmallFunctions.CheckVideoOutput();

                // Progressbar Label when encoding finished
                TimeSpan timespent = DateTime.Now - StartTime;
                LabelProgressBar.Content = "Finished Encoding - Elapsed Time " + timespent.ToString("hh\\:mm\\:ss") + " - avg " + Math.Round(TotalFrames / timespent.TotalSeconds, 2) + "fps";
                ProgressBar.Foreground = new SolidColorBrush(Color.FromRgb(6, 176, 37));
                ProgressBar.Value = 0;
                ProgressBar.Maximum = 10;
                ProgressBar.Value = 10;
                // Plays a sound if encoding has finished
                SmallFunctions.PlayFinishedSound();
                if (CheckBoxSettingsShutdownAfterEncode.IsChecked == true) { Process.Start("shutdown.exe", "/s /t 0"); }
            }
            catch { SmallFunctions.PlayStopSound(); }
            EncodeStarted = false;
        }

        private void SplitVideo()
        {
            // Temp Arguments for Splitting / Scenedetection
            bool reencodesplit = CheckBoxSplittingReencode.IsChecked == true;
            int splitmethod = ComboBoxSplittingMethod.SelectedIndex;
            int reencodeMethod = ComboBoxSplittingReencodeMethod.SelectedIndex;
            string ffmpegThreshold = TextBoxSplittingThreshold.Text;
            string chunkLength = TextBoxSplittingChunkLength.Text;
            VideoSplittingWindow videoSplittingWindow = new VideoSplittingWindow(splitmethod, reencodesplit, reencodeMethod, ffmpegThreshold, chunkLength, subHardSubEnabled, subHardCommand);
            videoSplittingWindow.ShowDialog();
        }

        private void FFmpegHardsub()
        {
            // This function reencodes the input video with subtitles
            // Skip Reencode if Chunking Method has been used, as it already hardcoded the subs
            if (ComboBoxSplittingReencodeMethod.SelectedIndex != 2)
            {
                ProgressBar.Dispatcher.Invoke(() => LabelProgressBar.Content = "Reencoding Video for Hardsubbing...");
                string ffmpegCommand = "/C ffmpeg.exe -i " + '\u0022' + VideoInput + '\u0022' + " " + subHardCommand + " -map_metadata -1 -c:v libx264 -crf 0 -preset veryfast -an " + '\u0022' + Path.Combine(TempPath, TempPathFileName, "tmpsub.mkv") + '\u0022';
                SmallFunctions.Logging("Subtitle Hardcoding Command: " + ffmpegCommand);
                // Reencodes the Video
                SmallFunctions.ExecuteFfmpegTask(ffmpegCommand);
                subHardSubEnabled = true;
            }
        }

        // ════════════════════════════════════ Temp Settings ═════════════════════════════════════

        private void SetTempSettings()
        {
            WorkerCount = int.Parse(ComboBoxWorkerCount.Text);                      // Sets the worker count
            OnePass = ComboBoxVideoPasses.SelectedIndex == 0;                       // Sets the amount of passes (true = 1, false = 2)
            Priority = ComboBoxProcessPriority.SelectedIndex == 0;                  // Sets the Process Priority
            SplitMethod = ComboBoxSplittingMethod.SelectedIndex;                    // Sets the Splitmethod, used for VideoEncode() function
            EncodeMethod = ComboBoxVideoEncoder.SelectedIndex;                      // Sets the encoder (0 aomenc; 1 rav1e; 2 svt-av1)
            DeleteTempFiles = CheckBoxSettingsDeleteTempFiles.IsChecked == true;    // Sets if Temp Files should be deleted
            ShowTerminal = CheckBoxSettingsTerminal.IsChecked == false;             // Sets if Terminal shall be shown during encode
            SmallFunctions.setVideoChunks(SplitMethod);                             // Sets the array of videochunks/commands
            SetPipeCommand();
        }

        private void SetPipeCommand()
        {
            // Potential breaking point: 422p 8bit / 444p 8bit not being "-strict -1"

            PipeBitDepthCommand = " -pix_fmt yuv";
            if (ComboBoxVideoEncoder.SelectedIndex == 0)
            {
                // aomenc
                if (ComboBoxAomencColorFormat.SelectedIndex == 0)
                {
                    PipeBitDepthCommand += "420p";
                }
                else if (ComboBoxAomencColorFormat.SelectedIndex == 1)
                {
                    PipeBitDepthCommand += "422p";
                }
                else if (ComboBoxAomencColorFormat.SelectedIndex == 2)
                {
                    PipeBitDepthCommand += "444p";
                }
            }

            if (ComboBoxVideoEncoder.SelectedIndex == 1)
            {
                // rav1e
                if (ComboBoxRav1eColorFormat.SelectedIndex == 0)
                {
                    PipeBitDepthCommand += "420p";
                }
                else if (ComboBoxRav1eColorFormat.SelectedIndex == 1)
                {
                    PipeBitDepthCommand += "422p";
                }
                else if (ComboBoxRav1eColorFormat.SelectedIndex == 2)
                {
                    PipeBitDepthCommand += "444p";
                }
            }

            if (ComboBoxVideoEncoder.SelectedIndex == 1)
            {
                // svt-av1
                if (ComboBoxSVTAV1ColorFormat.SelectedIndex == 0)
                {
                    PipeBitDepthCommand += "420p";
                }
                else if (ComboBoxSVTAV1ColorFormat.SelectedIndex == 1)
                {
                    PipeBitDepthCommand += "422p";
                }
                else if (ComboBoxSVTAV1ColorFormat.SelectedIndex == 2)
                {
                    PipeBitDepthCommand += "444p";
                }
            }

            if (ComboBoxVideoBitDepth.SelectedIndex == 1)
            {
                // 10bit
                PipeBitDepthCommand += "10le -strict -1";
            }
            else if (ComboBoxVideoBitDepth.SelectedIndex == 2)
            {
                // 12bit
                PipeBitDepthCommand += "12le -strict -1";
            }
        }

        private void SetAudioSettings()
        {
            // Sets Active Audio Tracks
            trackOne = CheckBoxAudioTrackOne.IsChecked == true;
            trackTwo = CheckBoxAudioTrackTwo.IsChecked == true;
            trackThree = CheckBoxAudioTrackThree.IsChecked == true;
            trackFour = CheckBoxAudioTrackFour.IsChecked == true;
            // Sets Audio Language
            trackOneLanguage = ComboBoxTrackOneLanguage.Text;
            trackTwoLanguage = ComboBoxTrackTwoLanguage.Text;
            trackThreeLanguage = ComboBoxTrackThreeLanguage.Text;
            trackFourLanguage = ComboBoxTrackFourLanguage.Text;
            // Sets Audio Bitrate
            audioBitrateTrackOne = int.Parse(TextBoxAudioBitrate.Text);
            audioBitrateTrackTwo = int.Parse(TextBoxAudioBitrateTrackTwo.Text);
            audioBitrateTrackThree = int.Parse(TextBoxAudioBitrateTrackThree.Text);
            audioBitrateTrackFour = int.Parse(TextBoxAudioBitrateTrackFour.Text);
            // Sets Audio Codec
            audioCodecTrackOne = ComboBoxAudioCodec.Text;
            audioCodecTrackTwo = ComboBoxAudioCodecTrackTwo.Text;
            audioCodecTrackThree = ComboBoxAudioCodecTrackThree.Text;
            audioCodecTrackFour = ComboBoxAudioCodecTrackFour.Text;
            // Sets Audio Channels
            switch (ComboBoxTrackOneChannels.SelectedIndex)
            {
                case 0: audioChannelsTrackOne = 1; break;
                case 1: audioChannelsTrackOne = 2; break;
                case 2: audioChannelsTrackOne = 6; break;
                case 3: audioChannelsTrackOne = 8; break;
                default: break;
            }
            switch (ComboBoxTrackTwoChannels.SelectedIndex)
            {
                case 0: audioChannelsTrackTwo = 1; break;
                case 1: audioChannelsTrackTwo = 2; break;
                case 2: audioChannelsTrackTwo = 6; break;
                case 3: audioChannelsTrackTwo = 8; break;
                default: break;
            }
            switch (ComboBoxTrackThreeChannels.SelectedIndex)
            {
                case 0: audioChannelsTrackThree = 1; break;
                case 1: audioChannelsTrackThree = 2; break;
                case 2: audioChannelsTrackThree = 6; break;
                case 3: audioChannelsTrackThree = 8; break;
                default: break;
            }
            switch (ComboBoxTrackFourChannels.SelectedIndex)
            {
                case 0: audioChannelsTrackFour = 1; break;
                case 1: audioChannelsTrackFour = 2; break;
                case 2: audioChannelsTrackFour = 6; break;
                case 3: audioChannelsTrackFour = 8; break;
                default: break;
            }
        }

        // ════════════════════════════════════ Video Filters ═════════════════════════════════════

        private void SetVideoFilters()
        {
            bool crop = CheckBoxFiltersCrop.IsChecked == true;
            bool rotate = CheckBoxFiltersRotate.IsChecked == true;
            bool resize = CheckBoxFiltersResize.IsChecked == true;
            bool deinterlace = CheckBoxFiltersDeinterlace.IsChecked == true;
            int tempCounter = 0;

            if (crop == true || rotate == true || resize == true || deinterlace == true)
            {
                FilterCommand = " -vf ";
                if (crop == true)
                {
                    FilterCommand += VideoFiltersCrop();
                    tempCounter += 1;
                }
                if (rotate == true)
                {
                    if (tempCounter != 0) { FilterCommand += ","; }
                    FilterCommand += VideoFiltersRotate();
                    tempCounter += 1;
                }
                if (deinterlace == true)
                {
                    if (tempCounter != 0) { FilterCommand += ","; }
                    FilterCommand = VideoFiltersDeinterlace();
                    tempCounter += 1;
                }
                if (resize == true)
                {
                    // Has to be last, due to scaling algorithm
                    if (tempCounter != 0) { FilterCommand += ","; }
                    FilterCommand += VideoFiltersResize();
                }
            }
            else
            {
                // If not set it would give issues when encoding another video in same ui instance
                FilterCommand = "";
            }
            SmallFunctions.Logging("Filter Command: " + FilterCommand);
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
            // Sets the values for deinterlacing the video
            return "yadif=" + ComboBoxFiltersDeinterlace.Text;
        }

        private string VideoFiltersResize()
        {
            // Sets the values for scaling the video
            return "scale=" + TextBoxFiltersResizeWidth.Text + ":" + TextBoxFiltersResizeHeight.Text + " -sws_flags " + ComboBoxFiltersScaling.Text;
        }

        // ═════════════════════════════════════ Audio Logic ══════════════════════════════════════

        private void GetAudioInformation()
        {
            Process getAudioIndexes = new Process();
            getAudioIndexes.StartInfo = new ProcessStartInfo()
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                WorkingDirectory = FFmpegPath,
                Arguments = "/C ffprobe.exe -i " + '\u0022' + VideoInput + '\u0022' + " -loglevel error -select_streams a -show_streams -show_entries stream=index:tags=:disposition= -of csv",
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            getAudioIndexes.Start();
            //Reads the Console Output
            string audioIndexes = getAudioIndexes.StandardOutput.ReadToEnd();
            getAudioIndexes.WaitForExit();
            //Splits the Console Output
            string[] audioIndexesFixed = audioIndexes.Split(new string[] { " ", "stream," }, StringSplitOptions.RemoveEmptyEntries);
            int detectedTracks = 0;
            bool trackone = false, tracktwo = false, trackthree = false, trackfour = false;
            // Iterates over the audioIndexesFixed Array
            foreach (var item in audioIndexesFixed)
            {
                switch (detectedTracks)
                {
                    case 0: trackone = true; break;
                    case 1: tracktwo = true; break;
                    case 2: trackthree = true; break;
                    case 3: trackfour = true; break;
                    default: break;
                }
                detectedTracks += 1;
            }
            // Enable / Disable CheckBoxes
            if (trackone) { CheckBoxAudioTrackOne.IsEnabled = true; CheckBoxAudioTrackOne.IsChecked = true; }
            else { CheckBoxAudioTrackOne.IsChecked = false; CheckBoxAudioTrackOne.IsEnabled = false; }
            if (tracktwo) { CheckBoxAudioTrackTwo.IsEnabled = true; CheckBoxAudioTrackTwo.IsChecked = true; }
            else { CheckBoxAudioTrackTwo.IsChecked = false; CheckBoxAudioTrackTwo.IsEnabled = false; }
            if (trackthree) { CheckBoxAudioTrackThree.IsEnabled = true; CheckBoxAudioTrackThree.IsChecked = true; }
            else { CheckBoxAudioTrackThree.IsChecked = false; CheckBoxAudioTrackThree.IsEnabled = false; }
            if (trackfour) { CheckBoxAudioTrackFour.IsEnabled = true; CheckBoxAudioTrackFour.IsChecked = true; }
            else { CheckBoxAudioTrackFour.IsChecked = false; CheckBoxAudioTrackFour.IsEnabled = false; }
            // This is needed if user encodes a bluray with pcm audio stream and wants to copy audio
            if (GetAudioInfo() == "pcm_bluray") { pcmBluray = true; } else { pcmBluray = false; }
            GetAudioLanguage();
        }

        public static string GetAudioInfo()
        {
            Process getAudioInfo = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    WorkingDirectory = FFmpegPath,
                    Arguments = "/C ffprobe.exe -i " + '\u0022' + VideoInput + '\u0022' + " -v error -select_streams a:0 -show_entries stream=codec_name -of default=noprint_wrappers=1:nokey=1",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            getAudioInfo.Start();
            string audio = getAudioInfo.StandardOutput.ReadLine();
            getAudioInfo.WaitForExit();
            return audio;
        }

        private void GetAudioLanguage()
        {
            //This function gets the Audio Languages from ffprobe and sets the ComboBoxes in the Audio Tab
            Process getAudioLang = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    WorkingDirectory = FFmpegPath,
                    Arguments = "/C ffprobe.exe -i " + '\u0022' + VideoInput + '\u0022' + " -v error -select_streams a -show_entries stream=index:stream_tags=language -of csv=p=0",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            getAudioLang.Start();
            string audio = getAudioLang.StandardOutput.ReadToEnd();
            string[] audioLanguages = audio.Split(new string[] { "1", "2", "3", "4", "," }, StringSplitOptions.RemoveEmptyEntries);
            audioLanguages = audioLanguages.Where(x => !string.IsNullOrEmpty(x)).ToArray();
            getAudioLang.WaitForExit();
            int index = 0;
            foreach (string line in audioLanguages)
            {
                string resultcropped;
                try { resultcropped = line.Replace(" ", "").Substring(0, 3); }
                catch { resultcropped = "und"; }

                int indexLang;
                switch (resultcropped)
                {
                    case "eng": indexLang = 1; break;
                    case "deu": indexLang = 2; break;
                    case "fre": indexLang = 3; break;
                    case "ita": indexLang = 4; break;
                    case "spa": indexLang = 5; break;
                    case "jpn": indexLang = 6; break;
                    case "chi": indexLang = 7; break;
                    case "kor": indexLang = 8; break;
                    default: indexLang = 0; break;
                }
                if (index == 0) { ComboBoxTrackOneLanguage.SelectedIndex = indexLang; }
                if (index == 1) { ComboBoxTrackTwoLanguage.SelectedIndex = indexLang; }
                if (index == 2) { ComboBoxTrackThreeLanguage.SelectedIndex = indexLang; }
                if (index == 3) { ComboBoxTrackFourLanguage.SelectedIndex = indexLang; }
                index += 1;
            }

        }

        // ════════════════════════════════════ Subtitle Logic ════════════════════════════════════

        private void SetSubtitleParameters()
        {
            // Has to be set, else it could create problems when running another encode in the same instance
            subSoftSubEnabled = false;
            subHardSubEnabled = false;
            subCommand = "";

            if (CheckBoxSubtitleActivatedOne.IsChecked == true)
            {
                // 1st Subtitle
                if (CheckBoxSubOneBurn.IsChecked == false)
                {
                    // Softsub
                    subSoftSubEnabled = true;
                    subCommand += SoftSubCMDGenerator(ComboBoxSubTrackOneLanguage.Text, TextBoxSubOneName.Text, TextBoxSubtitleTrackOne.Text, CheckBoxSubOneDefault.IsChecked == true);
                }
                else
                {
                    // Hardsub
                    HardSubCMDGenerator(TextBoxSubtitleTrackOne.Text);
                }
               
            }

            if (CheckBoxSubtitleActivatedTwo.IsChecked == true && CheckBoxSubTwoBurn.IsChecked != true)
            {
                // 2nd Subtitle
                if (CheckBoxSubTwoBurn.IsChecked == false)
                {
                    // Softsub
                    subSoftSubEnabled = true;
                    subCommand += SoftSubCMDGenerator(ComboBoxSubTrackTwoLanguage.Text, TextBoxSubTwoName.Text, TextBoxSubtitleTrackTwo.Text, CheckBoxSubTwoDefault.IsChecked == true);
                }
                else
                {
                    // Hardsub
                    HardSubCMDGenerator(TextBoxSubtitleTrackTwo.Text);
                }
            }

            if (CheckBoxSubtitleActivatedThree.IsChecked == true && CheckBoxSubThreeBurn.IsChecked != true)
            {
                // 3rd Subtitle
                if (CheckBoxSubThreeBurn.IsChecked == false)
                {
                    // Softsub
                    subSoftSubEnabled = true;
                    subCommand += SoftSubCMDGenerator(ComboBoxSubTrackThreeLanguage.Text, TextBoxSubThreeName.Text, TextBoxSubtitleTrackThree.Text, CheckBoxSubThreeDefault.IsChecked == true);
                }
                else
                {
                    // Hardsub
                    HardSubCMDGenerator(TextBoxSubtitleTrackThree.Text);
                }

            }

            if (CheckBoxSubtitleActivatedFour.IsChecked == true && CheckBoxSubFourBurn.IsChecked != true)
            {
                // 4th Subtitle
                if (CheckBoxSubFourBurn.IsChecked == false)
                {
                    // Softsub
                    subSoftSubEnabled = true;
                    subCommand += SoftSubCMDGenerator(ComboBoxSubTrackFourLanguage.Text, TextBoxSubFourName.Text, TextBoxSubtitleTrackFour.Text, CheckBoxSubFourDefault.IsChecked == true);
                }
                else
                {
                    // Hardsub
                    HardSubCMDGenerator(TextBoxSubtitleTrackFour.Text);
                }
                
            }

            if (CheckBoxSubtitleActivatedFive.IsChecked == true && CheckBoxSubFiveBurn.IsChecked != true)
            {
                // 5th Subtitle
                if (CheckBoxSubFiveBurn.IsChecked == false)
                {
                    // Softsub
                    subSoftSubEnabled = true;
                    subCommand += SoftSubCMDGenerator(ComboBoxSubTrackFiveLanguage.Text, TextBoxSubFiveName.Text, TextBoxSubtitleTrackFive.Text, CheckBoxSubFiveDefault.IsChecked == true);
                }
                else
                {
                    // Hardsub
                    HardSubCMDGenerator(TextBoxSubtitleTrackFive.Text);
                }
                
            }
            SmallFunctions.Logging("Subtitle Command: " + subCommand);
        }

        private string SoftSubCMDGenerator(string lang, string name, string input, bool defaultSub)
        {
            string subDefault = "no";
            if (defaultSub) { subDefault = "yes"; }
            return " --language 0:" + lang + " --track-name 0:" + '\u0022' + name + '\u0022' + " --default-track 0:" + subDefault + " " + '\u0022' + input + '\u0022';
        }

        private void HardSubCMDGenerator(string subInput)
        {
            // Hardsub
            string ext = Path.GetExtension(subInput);
            if (ext == ".ass" || ext == ".ssa")
            {
                subHardCommand = "-vf ass=" + '\u0022' + subInput + '\u0022';
                subHardCommand = subHardCommand.Replace("\u005c", "\u005c\u005c\u005c\u005c");
                subHardCommand = subHardCommand.Replace(":", "\u005c\u005c\u005c:");
            }
            else if (ext == ".srt")
            {
                subHardCommand = "-vf subtitles=" + '\u0022' + subInput + '\u0022';
                subHardCommand = subHardCommand.Replace("\u005c", "\u005c\u005c\u005c\u005c");
                subHardCommand = subHardCommand.Replace(":", "\u005c\u005c\u005c:");
            }
            // Theoretically PGS Subtitles can be hardcoded
            // The Problem is that ffmpeg disregards the potential offset
            // thus resulting in too early appearing subtitles
            // Softsub works, as it is handled by mkvmerge and not ffmpeg

            // Reencodes the video
            FFmpegHardsub();
        }

        public void GetSubtitleTracks()
        {
            //Creates Audio Directory in the temp dir
            if (!Directory.Exists(Path.Combine(TempPath, TempPathFileName, "Subtitles")))
                Directory.CreateDirectory(Path.Combine(TempPath, TempPathFileName, "Subtitles"));

            //This function gets subtitle information
            Process getSubtitles = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    WorkingDirectory = FFmpegPath,
                    Arguments = "/C ffprobe.exe -i " + '\u0022' + VideoInput + '\u0022' + " -v error -select_streams s -show_entries stream=codec_name:stream_tags=language -of csv=p=0",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            getSubtitles.Start();
            string subs = getSubtitles.StandardOutput.ReadToEnd();
            getSubtitles.WaitForExit();

            //Splits the output from ffprobe
            var result = subs.Split('\n').Where(x => !string.IsNullOrEmpty(x)).ToArray();

            int a = 0;
            int b = 0;
            //Iterates over the lines from the splitted output
            foreach (var line in result)
            {
                if (line.Contains("hdmv_pgs_subtitle") || line.Contains("ass") || line.Contains("ssa") || line.Contains("subrip") || line.Contains("dvd_subtitle"))
                {

                    string tempName = "";
                    Process process = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo.UseShellExecute = true;
                    startInfo.FileName = "cmd.exe";
                    startInfo.WorkingDirectory = FFmpegPath;

                    if (line.Contains("hdmv_pgs_subtitle"))
                    {
                        startInfo.Arguments = "/C ffmpeg.exe -y -i " + '\u0022' + VideoInput + '\u0022' + " -map 0:s:" + a + " -c:s copy " + '\u0022' + Path.Combine(TempPath, TempPathFileName, "Subtitles", "pgs_" + b + ".sup") + '\u0022';
                        tempName = Path.Combine(TempPath, TempPathFileName, "Subtitles", "pgs_" + b + ".sup");
                    }
                    else if (line.Contains("ass"))
                    {
                        startInfo.Arguments = "/C ffmpeg.exe -y -i " + '\u0022' + VideoInput + '\u0022' + " -map 0:s:" + a + " -c:s copy " + '\u0022' + Path.Combine(TempPath, TempPathFileName, "Subtitles", "ass_" + b + ".ass") + '\u0022';
                        tempName = Path.Combine(TempPath, TempPathFileName, "Subtitles", "ass_" + b + ".ass");
                    }
                    else if (line.Contains("subrip"))
                    {
                        startInfo.Arguments = "/C ffmpeg.exe -y -i " + '\u0022' + VideoInput + '\u0022' + " -map 0:s:" + a + " -c:s copy " + '\u0022' + Path.Combine(TempPath, TempPathFileName, "Subtitles", "subrip_" + b + ".srt") + '\u0022';
                        tempName = Path.Combine(TempPath, TempPathFileName, "Subtitles", "subrip_" + b + ".srt");
                    }
                    else if (line.Contains("ssa"))
                    {
                        startInfo.Arguments = "/C ffmpeg.exe -y -i " + '\u0022' + VideoInput + '\u0022' + " -map 0:s:" + a + " -c:s copy " + '\u0022' + Path.Combine(TempPath, TempPathFileName, "Subtitles", "ssa_" + b + ".ssa") + '\u0022';
                        tempName = Path.Combine(TempPath, TempPathFileName, "Subtitles", "ssa_" + b + ".ssa");
                    }
                    else if (line.Contains("dvd_subtitle"))
                    {
                        startInfo.Arguments = "/C ffmpeg.exe -y -i " + '\u0022' + VideoInput + '\u0022' + " -map 0:s:" + a + " -c:s copy " + '\u0022' + Path.Combine(TempPath, TempPathFileName, "Subtitles", "dvdsub_" + b + ".mkv") + '\u0022';
                    }

                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();

                    if (line.Contains("dvd_subtitle") == true)
                    {
                        // Extract dvdsub from mkv with mkvextract
                        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        startInfo.UseShellExecute = true;
                        startInfo.FileName = "cmd.exe";
                        startInfo.WorkingDirectory = MKVToolNixPath;

                        startInfo.Arguments = "/C mkvextract.exe " + '\u0022' + Path.Combine(TempPath, TempPathFileName, "Subtitles", "dvdsub_" + b + ".mkv") + '\u0022' + " tracks 0:" + '\u0022' + Path.Combine(TempPath, TempPathFileName, "Subtitles", "dvdsub_" + b + ".sub") + '\u0022';
                        
                        process.StartInfo = startInfo;
                        process.Start();
                        process.WaitForExit();

                        // Convert dvdsub to bluraysub
                        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        startInfo.UseShellExecute = true;
                        startInfo.FileName = "cmd.exe";
                        startInfo.WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "bdsup2sub");

                        startInfo.Arguments = "/C bdsup2sub.exe -o " + '\u0022' + Path.Combine(TempPath, TempPathFileName, "Subtitles", "pgs_dvd_" + b + ".sup") + '\u0022' + " " + '\u0022' + Path.Combine(TempPath, TempPathFileName, "Subtitles", "dvdsub_" + b + ".sub") + '\u0022';
                        process.StartInfo = startInfo;
                        process.Start();
                        process.WaitForExit();

                        // Cleanup
                        if (File.Exists(Path.Combine(TempPath, TempPathFileName, "Subtitles", "pgs_dvd_" + b + ".sup")))
                        {
                            try
                            {
                                File.Delete(Path.Combine(TempPath, TempPathFileName, "Subtitles", "dvdsub_" + b + ".sub"));
                                File.Delete(Path.Combine(TempPath, TempPathFileName, "Subtitles", "dvdsub_" + b + ".idx"));
                                File.Delete(Path.Combine(TempPath, TempPathFileName, "Subtitles", "dvdsub_" + b + ".mkv"));
                            }
                            catch { }
                        }

                        tempName = Path.Combine(TempPath, TempPathFileName, "Subtitles", "pgs_dvd_" + b + ".sup");
                    }

                    string resultcropped = line.Substring(line.LastIndexOf(',') + 1).Substring(0, 3);
                    int indexLang;
                    switch (resultcropped)
                    {
                        case "eng": indexLang = 1; break;
                        case "deu": indexLang = 2; break;
                        case "ger": indexLang = 2; break;
                        case "fre": indexLang = 3; break;
                        case "ita": indexLang = 4; break;
                        case "spa": indexLang = 5; break;
                        case "jpn": indexLang = 6; break;
                        case "chi": indexLang = 7; break;
                        case "kor": indexLang = 8; break;
                        default: indexLang = 0; break;
                    }

                    //Sets the TextBoxes
                    if (b == 0) { TextBoxSubtitleTrackOne.Text = tempName; ComboBoxSubTrackOneLanguage.SelectedIndex = indexLang; CheckBoxSubtitleActivatedOne.IsChecked = true; }
                    if (b == 1) { TextBoxSubtitleTrackTwo.Text = tempName; ComboBoxSubTrackTwoLanguage.SelectedIndex = indexLang; CheckBoxSubtitleActivatedTwo.IsChecked = true; }
                    if (b == 2) { TextBoxSubtitleTrackThree.Text = tempName; ComboBoxSubTrackThreeLanguage.SelectedIndex = indexLang; CheckBoxSubtitleActivatedThree.IsChecked = true; }
                    if (b == 3) { TextBoxSubtitleTrackFour.Text = tempName; ComboBoxSubTrackFourLanguage.SelectedIndex = indexLang; CheckBoxSubtitleActivatedFour.IsChecked = true; }
                    if (b == 4) { TextBoxSubtitleTrackFive.Text = tempName; ComboBoxSubTrackFiveLanguage.SelectedIndex = indexLang; CheckBoxSubtitleActivatedFive.IsChecked = true; }
                    b++;
                }
                a++;
            }
        }

        // ══════════════════════════════════ Encoder Settings ════════════════════════════════════

        private void SetEncoderSettings()
        {
            if (CheckBoxCustomVideoSettings.IsChecked == false)
            {
                // Sets the Encoder Settings
                if (ComboBoxVideoEncoder.SelectedIndex == 0) 
                { 
                    EncoderAomencCommand = SetAomencCommand();
                    SmallFunctions.Logging("Aomenc Settings : " + EncoderAomencCommand);
                }
                if (ComboBoxVideoEncoder.SelectedIndex == 1) 
                { 
                    EncoderRav1eCommand = SetRav1eCommand();
                    SmallFunctions.Logging("Rav1e Settings : " + EncoderRav1eCommand);
                }
                if (ComboBoxVideoEncoder.SelectedIndex == 2) 
                { 
                    EncoderSvtAV1Command = SetSvtAV1Command();
                    SmallFunctions.Logging("SVT-AV1 Settings : " + EncoderSvtAV1Command);
                }
            }
            else
            {
                // Custom Encoding Settings
                if (ComboBoxVideoEncoder.SelectedIndex == 0) 
                { 
                    EncoderAomencCommand = " " + TextBoxCustomVideoSettings.Text;
                    SmallFunctions.Logging("Aomenc Custom Settings : " + EncoderAomencCommand);
                }
                if (ComboBoxVideoEncoder.SelectedIndex == 1) 
                { 
                    EncoderRav1eCommand = " " + TextBoxCustomVideoSettings.Text;
                    SmallFunctions.Logging("Rav1e Custom Settings : " + EncoderRav1eCommand);
                }
                if (ComboBoxVideoEncoder.SelectedIndex == 2) 
                { 
                    EncoderSvtAV1Command = " " + TextBoxCustomVideoSettings.Text;
                    SmallFunctions.Logging("SVT-AV1 Custom Settings : " + EncoderSvtAV1Command);
                }
            }

        }

        private string SetAomencCommand()
        {
            // Aomenc Command
            string cmd = "";
            cmd += " --bit-depth=" + ComboBoxVideoBitDepth.Text;    // Bit-Depth
            cmd += " --cpu-used=" + SliderVideoSpeed.Value;         // Speed

            // Constant Quality or Target Bitrate
            if (RadioButtonVideoConstantQuality.IsChecked == true) { cmd += " --end-usage=q --cq-level=" + SliderVideoQuality.Value; }
            else if (RadioButtonVideoBitrate.IsChecked == true) { cmd += " --end-usage=vbr --target-bitrate=" + TextBoxVideoBitrate.Text; }

            if (CheckBoxVideoAdvancedSettings.IsChecked == false)
            {
                // Default params when User don't select advanced settings
                cmd += " --threads=4 --tile-columns=2 --tile-rows=1";
            }
            else
            {
                // Advanced Settings
                cmd += " --threads=" + ComboBoxAomencThreads.Text;                                      // Threads
                cmd += " --tile-columns=" + ComboBoxAomencTileColumns.Text;                             // Tile Columns
                cmd += " --tile-rows=" + ComboBoxAomencTileRows.Text;                                   // Tile Rows
                cmd += " --lag-in-frames=" + TextBoxAomencLagInFrames.Text;                             // Lag in Frames
                cmd += " --sharpness=" + ComboBoxAomencSharpness.Text;                                  // Sharpness (Filter)
                cmd += " --aq-mode=" + ComboBoxAomencAQMode.SelectedIndex;                              // AQ-Mode
                cmd += " --enable-keyframe-filtering=" + ComboBoxAomencKeyFiltering.SelectedIndex;      // Key Frame Filtering
                cmd += " --tune=" + ComboBoxAomencTune.Text;                                            // Tune
                if (TextBoxAomencMaxGOP.Text != "0")
                {
                    cmd += " --kf-max-dist=" + TextBoxAomencMaxGOP.Text;                                // Keyframe Interval
                }
                if (CheckBoxAomencRowMT.IsChecked == false)
                {
                    cmd += " --row-mt=0";                                                               // Row Based Multithreading
                }
                if (CheckBoxAomencCDEF.IsChecked == false)
                {
                    cmd += " --enable-cdef=0";                                                          // Constrained Directional Enhancement Filter
                }
                if (ComboBoxAomencColorPrimaries.SelectedIndex != 0)
                {
                    cmd += " --color-primaries=" + ComboBoxAomencColorPrimaries.Text;                   // Color Primaries
                }
                if (ComboBoxAomencColorTransfer.SelectedIndex != 0)
                {
                    cmd += " --transfer-characteristics=" + ComboBoxAomencColorTransfer.Text;           // Color Transfer
                }
                if (ComboBoxAomencColorMatrix.SelectedIndex != 0)
                {
                    cmd += " --matrix-coefficients=" + ComboBoxAomencColorMatrix.Text;                  // Color Matrix
                }
                if (ComboBoxAomencColorFormat.SelectedIndex != 0)
                {
                    cmd += " --" + ComboBoxAomencColorFormat.Text;                                      // Color Space
                }
                if (CheckBoxAomencARNRMax.IsChecked == true)
                {
                    cmd += " --arnr-maxframes=" + ComboBoxAomencARNRMax.Text;                           // ARNR Maxframes
                    cmd += " --arnr-strength=" + ComboBoxAomencARNRStrength.Text;                       // ARNR Strength
                }
                if (CheckBoxVideoAomencRealTime.IsChecked == true)
                {
                    cmd += " --rt";                                                                     // Real Time Mode
                }
            }

            return cmd;
        }

        private string SetRav1eCommand()
        {
            // Rav1e Command
            string cmd = "";
            cmd += " --speed " + SliderVideoSpeed.Value;    // Speed

            // Constant Quality or Target Bitrate
            if (RadioButtonVideoConstantQuality.IsChecked == true) { cmd += " --quantizer " + SliderVideoQuality.Value; }
            else if (RadioButtonVideoBitrate.IsChecked == true) { cmd += " --bitrate " + TextBoxVideoBitrate.Text; }

            if (CheckBoxVideoAdvancedSettings.IsChecked == false)
            {
                // Default params when User don't select advanced settings
                cmd += " --threads 4 --tile-cols 2 --tile-rows 1";
            }
            else
            {
                cmd += " --threads " + ComboBoxRav1eThreads.SelectedIndex;                              // Threads
                cmd += " --tile-cols " + ComboBoxRav1eTileColumns.SelectedIndex;                        // Tile Columns
                cmd += " --tile-rows " + ComboBoxRav1eTileRows.SelectedIndex;                           // Tile Rows
                cmd += " --rdo-lookahead-frames " + TextBoxRav1eLookahead.Text;                         // RDO Lookahead
                cmd += " --tune " + ComboBoxRav1eTune.Text;                                             // Tune
                if (TextBoxRav1eMaxGOP.Text != "0")
                {
                    cmd += " --keyint " + TextBoxRav1eMaxGOP.Text;                                      // Keyframe Interval
                }
                if (ComboBoxRav1eColorPrimaries.SelectedIndex != 0)
                {
                    cmd += " --primaries " + ComboBoxRav1eColorPrimaries.Text;                          // Color Primaries
                }
                if (ComboBoxRav1eColorTransfer.SelectedIndex != 0)
                {
                    cmd += " --transfer " + ComboBoxRav1eColorTransfer.Text;                            // Color Transfer
                }
                if (ComboBoxRav1eColorMatrix.SelectedIndex != 0)
                {
                    cmd += " --matrix " + ComboBoxRav1eColorMatrix.Text;                                // Color Matrix
                }
                if (CheckBoxRav1eMasteringDisplay.IsChecked == true)
                {
                    cmd += " --mastering-display G(" + TextBoxRav1eMasteringGx.Text + ",";              // Mastering Gx
                    cmd += TextBoxRav1eMasteringGy.Text + ")B(";                                        // Mastering Gy
                    cmd += TextBoxRav1eMasteringBx.Text + ",";                                          // Mastering Bx
                    cmd += TextBoxRav1eMasteringBy.Text + ")R(";                                        // Mastering By
                    cmd += TextBoxRav1eMasteringRx.Text + ",";                                          // Mastering Rx
                    cmd += TextBoxRav1eMasteringRy.Text + ")WP(";                                       // Mastering Ry
                    cmd += TextBoxRav1eMasteringWPx.Text + ",";                                         // Mastering WPx
                    cmd += TextBoxRav1eMasteringWPy.Text + ")L(";                                       // Mastering WPy
                    cmd += TextBoxRav1eMasteringLx.Text + ",";                                          // Mastering Lx
                    cmd += TextBoxRav1eMasteringLy.Text + ")";                                          // Mastering Ly
                }
                if (CheckBoxRav1eContentLight.IsChecked == true)
                {
                    cmd += " --content-light " + TextBoxRav1eContentLightCll.Text;                      // Content Light CLL
                    cmd += "," + TextBoxRav1eContentLightFall.Text;                                     // Content Light FALL
                }
            }

            return cmd;
        }

        private string SetSvtAV1Command()
        {
            string cmd = "";
            cmd += " --preset " + SliderVideoSpeed.Value;

            // Constant Quality or Target Bitrate
            if (RadioButtonVideoConstantQuality.IsChecked == true) { cmd += " --rc 0 -q " + SliderVideoQuality.Value; }
            else if (RadioButtonVideoBitrate.IsChecked == true) 
            {
                cmd += " --rc 1";
                cmd += " --tbr " + TextBoxVideoBitrate.Text; 
            }

            if (CheckBoxVideoAdvancedSettings.IsChecked == true)
            {
                cmd += " --tile-columns " + ComboBoxSVTAV1TileColumns.Text;                             // Tile Columns
                cmd += " --tile-rows " + ComboBoxSVTAV1TileRows.Text;                                   // Tile Rows
                cmd += " --keyint " + TextBoxSVTAV1MaxGOP.Text;                                         // Keyframe Interval
                cmd += " --lookahead " + TextBoxSVTAV1Lookahead.Text;                                   // Lookahead
                cmd += " --adaptive-quantization " + ComboBoxSVTAV1AQMode.SelectedIndex;                // AQ-Mode
                cmd += " --profile " + ComboBoxSVTAV1Profile.SelectedIndex;                             // Bitstream Profile
                if (ComboBoxSVTAV1AltRefLevel.SelectedIndex != 0)
                {
                    cmd += " --tf-level " + ComboBoxSVTAV1AltRefLevel.Text;                             // AltRef Level
                }
                if (ComboBoxSVTAV1AltRefStrength.SelectedIndex != 5)
                {
                    cmd += " --altref-strength " + ComboBoxSVTAV1AltRefStrength.SelectedIndex;          // AltRef Strength
                }
                if (ComboBoxSVTAV1AltRefFrames.SelectedIndex != 7)
                {
                    cmd += " --altref-nframes " + ComboBoxSVTAV1AltRefFrames.SelectedIndex;             // AltRef Frames
                }
                if (CheckBoxSVTAV1HDR.IsChecked == true)
                {
                    cmd += " --enable-hdr 1";                                                           // HDR
                }
            }

            return cmd;
        }

        // ══════════════════════════════════════ Buttons ═════════════════════════════════════════

        private void ButtonSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                XmlWriter writer = XmlWriter.Create(Path.Combine(Directory.GetCurrentDirectory(), "settings.xml"));
                writer.WriteStartElement("Settings");
                writer.WriteElementString("CustomTemp", CheckBoxCustomTempPath.IsChecked.ToString());
                writer.WriteElementString("CustomTempPath", TextBoxCustomTempPath.Text);
                writer.WriteElementString("DeleteTempFiles", CheckBoxSettingsDeleteTempFiles.IsChecked.ToString());
                writer.WriteElementString("PlaySound", CheckBoxSettingsUISounds.IsChecked.ToString());
                writer.WriteElementString("Logging", CheckBoxSettingsLogging.IsChecked.ToString());
                writer.WriteElementString("Shutdown", CheckBoxSettingsShutdownAfterEncode.IsChecked.ToString());
                writer.WriteElementString("TempPathActive", CheckBoxCustomTempPath.IsChecked.ToString());
                writer.WriteElementString("TempPath", TextBoxCustomTempPath.Text);
                writer.WriteElementString("Terminal", CheckBoxSettingsTerminal.IsChecked.ToString());
                writer.WriteElementString("ThemeAccent", ComboBoxAccentTheme.SelectedIndex.ToString());
                writer.WriteElementString("ThemeBase", ComboBoxBaseTheme.SelectedIndex.ToString());
                writer.WriteEndElement();
                writer.Close();
            }
            catch { }
        }

        private void LoadSettingsTab()
        {
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "settings.xml")))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(Path.Combine(Directory.GetCurrentDirectory(), "settings.xml"));
                XmlNodeList node = doc.GetElementsByTagName("Settings");
                foreach (XmlNode n in node[0].ChildNodes)
                {
                    switch (n.Name)
                    {
                        case "CustomTemp":      CheckBoxCustomTempPath.IsChecked = n.InnerText == "True"; break;
                        case "CustomTempPath":  TextBoxCustomTempPath.Text = n.InnerText; break;
                        case "DeleteTempFiles": CheckBoxSettingsDeleteTempFiles.IsChecked = n.InnerText == "True"; break;
                        case "PlaySound":       CheckBoxSettingsUISounds.IsChecked = n.InnerText == "True"; break;
                        case "Logging":         CheckBoxSettingsLogging.IsChecked = n.InnerText == "True"; break;
                        case "Shutdown":        CheckBoxSettingsShutdownAfterEncode.IsChecked = n.InnerText == "True"; break;
                        case "TempPathActive":  CheckBoxCustomTempPath.IsChecked = n.InnerText == "True"; break;
                        case "TempPath":        TextBoxCustomTempPath.Text = n.InnerText; break;
                        case "Terminal":        CheckBoxSettingsTerminal.IsChecked = n.InnerText == "True"; break;
                        case "ThemeAccent":     ComboBoxAccentTheme.SelectedIndex = int.Parse(n.InnerText); break;
                        case "ThemeBase":       ComboBoxBaseTheme.SelectedIndex = int.Parse(n.InnerText); break;
                        default: break;
                    }
                }
                ThemeManager.Current.ChangeTheme(this, ComboBoxBaseTheme.Text + "." + ComboBoxAccentTheme.Text);
            }
        }

        private void ButtonSetTheme_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Current.ChangeTheme(this, ComboBoxBaseTheme.Text + "." + ComboBoxAccentTheme.Text);

            // Changes the Color of the Custom Settings Textbox
            // Reasoning is that the color gets changed by the arg verification
            if (ComboBoxBaseTheme.SelectedIndex == 0)
            {
                // Lightmode
                TextBoxCustomVideoSettings.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            }
            else
            {
                // Darkmode
                TextBoxCustomVideoSettings.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            }

        }

        private void ButtonDeleteTempFiles_Click(object sender, RoutedEventArgs e)
        {
            SmallFunctions.DeleteTempFilesButton();
        }

        private void ButtonSavePreset_Click(object sender, RoutedEventArgs e)
        {
            // Creates a new SavePreset Window
            SavePreset savePreset = new SavePreset(ComboBoxBaseTheme.Text, ComboBoxAccentTheme.Text);
            // Displays the Window and awaits exit
            savePreset.ShowDialog();
            // Gets the Data from the SavePreset Window
            string result = savePreset.SaveName;
            bool cancel = savePreset.cancel;
            if (cancel == false)
            {
                // Saves a new preset
                SaveSettings(true, result);
            }
            LoadPresetsIntoComboBox();
        }

        private void ButtonOpenSource_Click(object sender, RoutedEventArgs e)
        {
            // Creates a new object of the type "OpenVideoWindow"
            OpenVideoWindow WindowVideoSource = new OpenVideoWindow();
            // Hides the main user interface
            this.Hide();
            // Shows the just created window object and awaits its exit
            WindowVideoSource.ShowDialog();
            // Shows the main user interface
            this.Show();
            // Uses the public get method in OpenVideoSource window to get variable
            string result = WindowVideoSource.VideoPath;
            bool resultProject = WindowVideoSource.ProjectFile;
            if (resultProject == false)
            {
                // Sets the label in the user interface
                // Note that this has to be edited once batch encoding is added as function
                if (WindowVideoSource.QuitCorrectly)
                {
                    VideoInputSet = true;
                    TextBoxVideoSource.Text = result;
                    VideoInput = result;
                    TempPathFileName = Path.GetFileNameWithoutExtension(result);
                    GetAudioInformation();
                    GetSubtitleTracks();
                }
            }
            else
            {
                if (WindowVideoSource.QuitCorrectly)
                {
                    LoadSettings(true, result);
                    GetAudioInformation();
                    GetSubtitleTracks();
                }
            }
            
        }

        private void ButtonOpenDestination_Click(object sender, RoutedEventArgs e)
        {
            // Note that this has to be edited once batch encoding is being implemented
            // Save File Dialog for single file saving
            SaveFileDialog saveVideoFileDialog = new SaveFileDialog();
            saveVideoFileDialog.Filter = "Video|*.mkv;*.webm;*.mp4";
            // Avoid NULL being returned resulting in crash
            Nullable<bool> result = saveVideoFileDialog.ShowDialog();
            if (result == true)
            {
                TextBoxVideoDestination.Text = saveVideoFileDialog.FileName;
                VideoOutput = saveVideoFileDialog.FileName;
                VideoOutputSet = true;
            }
        }

        private void ButtonOpenTempFolder_Click(object sender, RoutedEventArgs e)
        {
            // Opens the Temp Folder
            if (CheckBoxCustomTempPath.IsChecked == false) 
            {
                //Creates the temp directoy if not existent
                if (Directory.Exists(Path.Combine(Path.GetTempPath(), "NEAV1E")) == false) { Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "NEAV1E")); }
                Process.Start(Path.Combine(Path.GetTempPath(), "NEAV1E")); 
            }
            else 
            { 
                Process.Start(TextBoxCustomTempPath.Text); 
            }
        }

        private void ButtonSetTempPath_Click(object sender, RoutedEventArgs e)
        {
            // Custom Temp Path
            System.Windows.Forms.FolderBrowserDialog browseOutputFolder = new System.Windows.Forms.FolderBrowserDialog();
            if (browseOutputFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TextBoxCustomTempPath.Text = browseOutputFolder.SelectedPath;
            }
        }

        private void ButtonOpenProgramFolder_Click(object sender, RoutedEventArgs e)
        {
            // Opens Program Folder
            try { Process.Start(Directory.GetCurrentDirectory()); } catch { }
        }

        private void ButtonStartEncode_Click(object sender, RoutedEventArgs e)
        {
            if (VideoInputSet == true && VideoOutputSet == true) 
            {
                if (EncodeStarted != true)
                {
                    PreStart();
                }
                else
                {
                    SmallFunctions.PlayStopSound();
                    MessageBox.Show("Encode already started!", "Attention", MessageBoxButton.OK, MessageBoxImage.Information);
                }                
            }
            else 
            {
                SmallFunctions.PlayStopSound();
                MessageBox.Show("Input or Output not set!", "Attention", MessageBoxButton.OK, MessageBoxImage.Information); 
            }
        }
        private void ButtonStopEncode_Click(object sender, RoutedEventArgs e)
        {
            if (EncodeStarted == true)
            {
                // Sets the global Cancel Boolean
                SmallFunctions.Cancel.CancelAll = true;
                // Invokes Cancellationtoken cancel
                cancellationTokenSource.Cancel();
                // Kills all encoder instances
                SmallFunctions.KillInstances();
                // Sets that the encode has been finished
                EncodeStarted = false;
                // Cancel UI Routine
                CancelRoutine();
            }
            else
            {
                SmallFunctions.PlayStopSound();
                MessageBox.Show("Encode has not started yet!", "Attention", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ButtonUpdater_Click(object sender, RoutedEventArgs e)
        {
            // Opens the program Updater
            Updater updater = new Updater(ComboBoxBaseTheme.Text, ComboBoxAccentTheme.Text);
            updater.ShowDialog();
            CheckDependencies.Check();
        }

        // ═══════════════════════════════════ Progress Bar ═══════════════════════════════════════

        private void ProgressBarUpdating()
        {
            // Gets all Progress Files of ffmpeg
            string[] filePaths = Directory.GetFiles(Path.Combine(TempPath, TempPathFileName, "Progress"), "*.log", SearchOption.AllDirectories);

            int totalencodedframes = 0;

            // Sets the total framecount
            int totalframes = TotalFrames;

            foreach (string file in filePaths)
            {
                // Reads the progress file of ffmpeg without locking it up
                Stream stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                TextReader objstream = new StreamReader(stream);

                // Reads the content of the stream
                string text = objstream.ReadToEnd();

                // Closes the stream reader
                stream.Close();

                // Splits every line
                string[] lines = text.Split( new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None );

                string tempvalue = "";

                // Iterates over all lines
                foreach (var line in lines)
                {
                    // Checks if the line contains the word "frame="
                    if (line.Contains("frame=")) { tempvalue = line.Remove(0, 6); }
                }

                try
                {
                    // Adds the framecount to the total encoded frames
                    totalencodedframes += int.Parse(tempvalue);
                }
                catch { }
                objstream.Close();
            }

            // Gets the so far spent time
            TimeSpan timespent = DateTime.Now - StartTime;
            try
            {
                // Setting Label & Progressbar
                LabelProgressBar.Dispatcher.Invoke(() => LabelProgressBar.Content = totalencodedframes + " / " + totalframes + " Frames - " + Math.Round(totalencodedframes / timespent.TotalSeconds, 2) + "fps - " + Math.Round(((timespent.TotalSeconds / totalencodedframes) * (totalframes - totalencodedframes)) / 60, MidpointRounding.ToEven) + "min left");
                ProgressBar.Dispatcher.Invoke(() => ProgressBar.Value = totalencodedframes);
            }
            catch { }
        }

        // ══════════════════════════════════ Video Encoding ══════════════════════════════════════

        private void EncodeVideo()
        {
            // Starts "a timer" for eta / fps calculation
            DateTime starttime = DateTime.Now;
            StartTime = starttime;
            bool encodeStarted = true;
            Task taskProgressBar = new Task(() =>
            {
                while (encodeStarted)
                {
                    ProgressBarUpdating();
                    // Waits 1s before updating
                    Thread.Sleep(1000); 
                }
            });
            taskProgressBar.Start();
            // Main Encoding Function
            // Creates a new Thread Pool
            using (SemaphoreSlim concurrencySemaphore = new SemaphoreSlim(WorkerCount))
            {
                // Creates a tasks list
                List<Task> tasks = new List<Task>();
                // Iterates over all args in VideoChunks list
                foreach (var command in VideoChunks)
                {
                    concurrencySemaphore.Wait();
                    var task = Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            if (SmallFunctions.Cancel.CancelAll == false)
                            {
                                // We need the index of the command in the array
                                var index = Array.FindIndex(VideoChunks, row => row.Contains(command));
                                // Logic for resume mode - skips already encoded files
                                if (File.Exists(Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + ".ivf" + "_finished.log")) == false)
                                {
                                    // One Pass Encoding
                                    Process ffmpegProcess = new Process();
                                    ProcessStartInfo startInfo = new ProcessStartInfo();
                                    startInfo.UseShellExecute = true;
                                    startInfo.FileName = "cmd.exe";
                                    startInfo.WorkingDirectory = FFmpegPath;

                                    if (ShowTerminal == false)
                                        startInfo.WindowStyle = ProcessWindowStyle.Hidden;

                                    string InputVideo = "";
                                    // FFmpeg Scene Detect or PySceneDetect
                                    if (SplitMethod == 0 || SplitMethod == 1) { InputVideo = " -i " + '\u0022' + VideoInput + '\u0022' + " " + command; }
                                    else if (SplitMethod == 2) { InputVideo = " -i " + '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", command) + '\u0022'; } // Chunk based splitting

                                    string FFmpegProgress = " -progress " + '\u0022' + Path.Combine(TempPath, TempPathFileName, "Progress", "split" + index.ToString("D5") + "_progress.log") + '\u0022';

                                    // Logic to skip first pass encoding if "_finished" log file exists
                                    if (File.Exists(Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + "_stats.log" + "_finished.log")) == false)
                                    {
                                        if (EncodeMethod == 0) // aomenc
                                        {
                                            string aomencCMD = "";
                                            string output = "";
                                            string ffmpegPipe = InputVideo + " " + FilterCommand + PipeBitDepthCommand + " -color_range 0 -vsync 0 -f yuv4mpegpipe - | ";

                                            if (OnePass) // One Pass Encoding
                                            {
                                                aomencCMD = '\u0022' + Path.Combine(AomencPath, "aomenc.exe") + '\u0022' + " - --passes=1" + EncoderAomencCommand + " --output=";
                                                output = '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + ".ivf") + '\u0022';
                                            }
                                            else // Two Pass Encoding First Pass
                                            {
                                                aomencCMD = '\u0022' + Path.Combine(AomencPath, "aomenc.exe") + '\u0022' + " - --passes=2 --pass=1" + EncoderAomencCommand + " --fpf=";
                                                output = '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + "_stats.log") + '\u0022' + " --output=NUL";
                                            }
                                            startInfo.Arguments = "/C ffmpeg.exe" + FFmpegProgress + ffmpegPipe + aomencCMD + output;
                                        }
                                        else if (EncodeMethod == 1) // rav1e
                                        {

                                            string ffmpegPipe = InputVideo + " " + FilterCommand + PipeBitDepthCommand + " -color_range 0 -vsync 0 -f yuv4mpegpipe - | ";
                                            string rav1eCMD = '\u0022' + Path.Combine(Rav1ePath, "rav1e.exe") + '\u0022' + " - " + EncoderRav1eCommand + " --output ";
                                            string output = '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + ".ivf") + '\u0022';
                                            startInfo.Arguments = "/C ffmpeg.exe" + FFmpegProgress + ffmpegPipe + rav1eCMD + output;
                                        }
                                        else if (EncodeMethod == 2) // svt-av1
                                        {
                                            string svtav1CMD = "";
                                            string output = "";
                                            string ffmpegPipe = InputVideo + " " + FilterCommand + PipeBitDepthCommand + " -color_range 0 -nostdin -vsync 0 -f yuv4mpegpipe - | ";
                                            if (OnePass)
                                            {
                                                // One Pass Encoding
                                                svtav1CMD = '\u0022' + Path.Combine(Rav1ePath, "SvtAv1EncApp.exe") + '\u0022' + " -i stdin " + EncoderSvtAV1Command + " --passes 1 -b ";
                                                output = '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + ".ivf") + '\u0022';
                                            }
                                            else
                                            {
                                                // Two Pass Encoding First Pass
                                                svtav1CMD = '\u0022' + Path.Combine(Rav1ePath, "SvtAv1EncApp.exe") + '\u0022' + " -i stdin " + EncoderSvtAV1Command + " --irefresh-type 2 --pass 1 -b NUL --stats ";
                                                output = '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + "_stats.log") + '\u0022';
                                            }
                                            startInfo.Arguments = "/C ffmpeg.exe" + FFmpegProgress + ffmpegPipe + svtav1CMD + output;
                                        }
                                        SmallFunctions.Logging("Encoding Video: " + startInfo.Arguments);
                                        ffmpegProcess.StartInfo = startInfo;
                                        ffmpegProcess.Start();

                                        // Sets the process priority
                                        if (Priority == false)
                                            ffmpegProcess.PriorityClass = ProcessPriorityClass.BelowNormal;

                                        ffmpegProcess.WaitForExit();

                                        if (OnePass == false && SmallFunctions.Cancel.CancelAll == false)
                                        {
                                            // Writes log file if first pass is finished, to be able to skip them later if in resume mode
                                            SmallFunctions.WriteToFileThreadSafe("", Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + "_stats.log" + "_finished.log"));
                                        }

                                    }

                                    if (OnePass != true)
                                    {
                                        // Two Pass Encoding Second Pass
                                        if (EncodeMethod == 0) // aomenc
                                        {
                                            string ffmpegPipe = InputVideo + " " + FilterCommand + PipeBitDepthCommand + " -color_range 0 -vsync 0 -f yuv4mpegpipe - | ";
                                            string aomencCMD = '\u0022' + Path.Combine(AomencPath, "aomenc.exe") + '\u0022' + " - --passes=2 --pass=2" + EncoderAomencCommand + " --fpf=";
                                            string outputLog = '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + "_stats.log") + '\u0022';
                                            string outputVid = " --output=" + '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + ".ivf") + '\u0022';
                                            startInfo.Arguments = "/C ffmpeg.exe" + FFmpegProgress + ffmpegPipe + aomencCMD + outputLog + outputVid;
                                        }
                                        else if (EncodeMethod == 1) // rav1e
                                        {
                                            // Rav1e 2 Pass still broken
                                        }
                                        else if (EncodeMethod == 2) // svt-av1
                                        {
                                            string ffmpegPipe = InputVideo + " " + FilterCommand + PipeBitDepthCommand + " -color_range 0 -nostdin -vsync 0 -f yuv4mpegpipe - | ";
                                            string svtav1CMD = '\u0022' + Path.Combine(Rav1ePath, "SvtAv1EncApp.exe") + '\u0022' + " -i stdin " + EncoderSvtAV1Command + " --irefresh-type 2 --pass 2 --stats ";
                                            string stats = '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + "_stats.log") + '\u0022';
                                            string outputVid = " -b " + '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + ".ivf") + '\u0022';
                                            startInfo.Arguments = "/C ffmpeg.exe" + FFmpegProgress + ffmpegPipe + svtav1CMD + stats + outputVid;
                                        }
                                        SmallFunctions.Logging("Encoding Video: " + startInfo.Arguments);
                                        ffmpegProcess.StartInfo = startInfo;
                                        ffmpegProcess.Start();

                                        // Sets the process priority
                                        if (Priority == false)
                                            ffmpegProcess.PriorityClass = ProcessPriorityClass.BelowNormal;

                                        ffmpegProcess.WaitForExit();
                                    }
                                    if (SmallFunctions.Cancel.CancelAll == false)
                                    {
                                        // This function will write finished encodes to a log file, to be able to skip them if in resume mode
                                        SmallFunctions.WriteToFileThreadSafe("", Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + ".ivf" + "_finished.log"));
                                    }                                    
                                }
                            }
                            else
                            {
                                SmallFunctions.KillInstances();
                            }
                        }
                        finally { concurrencySemaphore.Release();}
                    });
                    tasks.Add(task);
                }
                Task.WaitAll(tasks.ToArray());
            }

            encodeStarted = false;
        }

        // ═════════════════════════════════════ Presets IO ═══════════════════════════════════════

        private void SaveSettings(bool SaveProfile, string SaveName)
        {
            string directory = "";
            if (SaveProfile)
            {
                // Path to Profile Save
                directory = Path.Combine(Directory.GetCurrentDirectory(), "Profiles", SaveName + ".xml");
                // Check Creates Profile Folder
                if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Profiles")))
                    Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Profiles"));
            }
            else
            {
                // Path to Project File
                directory = Path.Combine(Directory.GetCurrentDirectory(), "Jobs", SaveName + ".xml");
                // Check Creates Profile Folder
                if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Jobs")))
                    Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Jobs"));
            }

            // New XmlWriter instance
            XmlWriter writer = XmlWriter.Create(directory);
            // Write Start Element
            writer.WriteStartElement("Settings");

            if (SaveProfile == false)
            {
                // Project File / Resume File
                writer.WriteElementString("VideoInput",                 VideoInput);                                                    // Video Input
                writer.WriteElementString("VideoOutput",                VideoOutput);                                                   // Video Output
                // Subtitles
                writer.WriteElementString("SubOne",                     CheckBoxSubtitleActivatedOne.IsChecked.ToString());             // Subtitle Track One Active
                writer.WriteElementString("SubTwo",                     CheckBoxSubtitleActivatedTwo.IsChecked.ToString());             // Subtitle Track Two Active
                writer.WriteElementString("SubThree",                   CheckBoxSubtitleActivatedThree.IsChecked.ToString());           // Subtitle Track Three Active
                writer.WriteElementString("SubFour",                    CheckBoxSubtitleActivatedFour.IsChecked.ToString());            // Subtitle Track Four Active
                writer.WriteElementString("SubFive",                    CheckBoxSubtitleActivatedFive.IsChecked.ToString());            // Subtitle Track Five Active
                writer.WriteElementString("SubOnePath",                 TextBoxSubtitleTrackOne.Text);                                  // Subtitle Track One Path
                writer.WriteElementString("SubTwoPath",                 TextBoxSubtitleTrackTwo.Text);                                  // Subtitle Track Two Path
                writer.WriteElementString("SubThreePath",               TextBoxSubtitleTrackThree.Text);                                // Subtitle Track Three Path
                writer.WriteElementString("SubFourPath",                TextBoxSubtitleTrackFour.Text);                                 // Subtitle Track Four Path
                writer.WriteElementString("SubFivePath",                TextBoxSubtitleTrackFive.Text);                                 // Subtitle Track Five Path
                writer.WriteElementString("SubOneName",                 TextBoxSubOneName.Text);                                        // Subtitle Track One Name
                writer.WriteElementString("SubTwoName",                 TextBoxSubTwoName.Text);                                        // Subtitle Track Two Name
                writer.WriteElementString("SubThreeName",               TextBoxSubThreeName.Text);                                      // Subtitle Track Three Name
                writer.WriteElementString("SubFourName",                TextBoxSubFourName.Text);                                       // Subtitle Track Four Name
                writer.WriteElementString("SubFiveName",                TextBoxSubFiveName.Text);                                       // Subtitle Track Five Name
                writer.WriteElementString("SubOneLanguage",             ComboBoxSubTrackOneLanguage.SelectedIndex.ToString());          // Subtitle Track One Language
                writer.WriteElementString("SubTwoLanguage",             ComboBoxSubTrackTwoLanguage.SelectedIndex.ToString());          // Subtitle Track Two Language
                writer.WriteElementString("SubThreeLanguage",           ComboBoxSubTrackThreeLanguage.SelectedIndex.ToString());        // Subtitle Track Three Language
                writer.WriteElementString("SubFourLanguage",            ComboBoxSubTrackFourLanguage.SelectedIndex.ToString());         // Subtitle Track Four Language
                writer.WriteElementString("SubFFiveLanguage",           ComboBoxSubTrackFiveLanguage.SelectedIndex.ToString());         // Subtitle Track Five Language
                // Audio (for resume mode)
                writer.WriteElementString("AudioLangOne",               ComboBoxTrackOneLanguage.SelectedIndex.ToString());             // Audio Track One Language
                writer.WriteElementString("AudioLangTwo",               ComboBoxTrackTwoLanguage.SelectedIndex.ToString());             // Audio Track Two Language
                writer.WriteElementString("AudioLangThree",             ComboBoxTrackThreeLanguage.SelectedIndex.ToString());           // Audio Track Three Language
                writer.WriteElementString("AudioLangFour",              ComboBoxTrackFourLanguage.SelectedIndex.ToString());            // Audio Track Four Language
            }
            // ═══════════════════════════════════════════════════════════════════ Audio ══════════════════════════════════════════════════════════════════
            writer.WriteElementString("AudioTrackOne",                  CheckBoxAudioTrackOne.IsChecked.ToString());                    // Audio Track One Active
            writer.WriteElementString("AudioTrackTwo",                  CheckBoxAudioTrackTwo.IsChecked.ToString());                    // Audio Track Two Active
            writer.WriteElementString("AudioTrackThree",                CheckBoxAudioTrackThree.IsChecked.ToString());                  // Audio Track Three Active
            writer.WriteElementString("AudioTrackFour",                 CheckBoxAudioTrackFour.IsChecked.ToString());                   // Audio Track Four Active
            writer.WriteElementString("TrackOneCodec",                  ComboBoxAudioCodec.SelectedIndex.ToString());                   // Audio Track One Codec
            writer.WriteElementString("TrackTwoCodec",                  ComboBoxAudioCodecTrackTwo.SelectedIndex.ToString());           // Audio Track Two Codec
            writer.WriteElementString("TrackThreeCodec",                ComboBoxAudioCodecTrackThree.SelectedIndex.ToString());         // Audio Track Three Codec
            writer.WriteElementString("TrackFourCodec",                 ComboBoxAudioCodecTrackFour.SelectedIndex.ToString());          // Audio Track Four Codec
            writer.WriteElementString("TrackOneBitrate",                TextBoxAudioBitrate.Text);                                      // Audio Track One Bitrate
            writer.WriteElementString("TrackTwoBitrate",                TextBoxAudioBitrateTrackTwo.Text);                              // Audio Track Two Bitrate
            writer.WriteElementString("TrackThreeBitrate",              TextBoxAudioBitrateTrackThree.Text);                            // Audio Track Three Bitrate
            writer.WriteElementString("TrackFourBitrate",               TextBoxAudioBitrateTrackFour.Text);                             // Audio Track Four Bitrate
            writer.WriteElementString("TrackOneChannels",               ComboBoxTrackOneChannels.SelectedIndex.ToString());             // Audio Track One Channels
            writer.WriteElementString("TrackTwoChannels",               ComboBoxTrackTwoChannels.SelectedIndex.ToString());             // Audio Track Two Channels
            writer.WriteElementString("TrackThreeChannels",             ComboBoxTrackThreeChannels.SelectedIndex.ToString());           // Audio Track Three Channels
            writer.WriteElementString("TrackFourChannels",              ComboBoxTrackFourChannels.SelectedIndex.ToString());            // Audio Track Four Channels


            writer.WriteElementString("WorkerCount",                    ComboBoxWorkerCount.SelectedIndex.ToString());                  // Worker Count
            writer.WriteElementString("WorkerPriority",                 ComboBoxProcessPriority.SelectedIndex.ToString());              // Worker Priority

            // ═════════════════════════════════════════════════════════════════ Splitting ═════════════════════════════════════════════════════════════════
            writer.WriteElementString("SplittingMethod",                ComboBoxSplittingMethod.SelectedIndex.ToString());              // Splitting Method
            if (ComboBoxSplittingMethod.SelectedIndex == 0)
            {
                // FFmpeg Scene Detect
                writer.WriteElementString("SplittingThreshold",         TextBoxSplittingThreshold.Text);                                // Splitting Threshold
            }
            else if (ComboBoxSplittingMethod.SelectedIndex == 2)
            {
                // Chunking Method
                if (CheckBoxSplittingReencode.IsChecked == true)
                {
                    writer.WriteElementString("SplittingReencode",      ComboBoxSplittingReencodeMethod.SelectedIndex.ToString());      // Splitting Reencode Codec
                }
                writer.WriteElementString("SplittingReencodeActive",    CheckBoxSplittingReencode.IsChecked.ToString());                // Splitting Reencode Active
                writer.WriteElementString("SplittingReencodeLength",    TextBoxSplittingChunkLength.Text);                              // Splitting Chunk Length
            }
            // ══════════════════════════════════════════════════════════════════ Filters ══════════════════════════════════════════════════════════════════

            writer.WriteElementString("FilterCrop",                 CheckBoxFiltersCrop.IsChecked.ToString());                          // Filter Crop (Boolean)
            if (CheckBoxFiltersCrop.IsChecked == true)
            {
                // Cropping
                writer.WriteElementString("FilterCropTop",          TextBoxFiltersCropTop.Text);                                        // Filter Crop Top
                writer.WriteElementString("FilterCropBottom",       TextBoxFiltersCropBottom.Text);                                     // Filter Crop Bottom
                writer.WriteElementString("FilterCropLeft",         TextBoxFiltersCropLeft.Text);                                       // Filter Crop Left
                writer.WriteElementString("FilterCropRight",        TextBoxFiltersCropRight.Text);                                      // Filter Crop Right
            }

            writer.WriteElementString("FilterResize",               CheckBoxFiltersResize.IsChecked.ToString());                        // Filter Resize (Boolean)
            if (CheckBoxFiltersResize.IsChecked == true)
            {
                // Resize
                writer.WriteElementString("FilterResizeWidth",      TextBoxFiltersResizeWidth.Text);                                    // Filter Resize Width
                writer.WriteElementString("FilterResizeHeight",     TextBoxFiltersResizeHeight.Text);                                   // Filter Resize Height
                writer.WriteElementString("FilterResizeAlgo",       ComboBoxFiltersScaling.SelectedIndex.ToString());                   // Filter Resize Scaling Algorithm
            }

            writer.WriteElementString("FilterRotate",               CheckBoxFiltersRotate.IsChecked.ToString());                        // Filter Rotate (Boolean)
            if (CheckBoxFiltersRotate.IsChecked == true)
            {
                // Rotating
                writer.WriteElementString("FilterRotateAmount",     ComboBoxFiltersRotate.SelectedIndex.ToString());                    // Filter Rotate
            }

            writer.WriteElementString("FilterDeinterlace",          CheckBoxFiltersDeinterlace.IsChecked.ToString());                   // Filter Deinterlace (Boolean)
            if (CheckBoxFiltersDeinterlace.IsChecked == true)
            {
                // Deinterlacing
                writer.WriteElementString("FilterDeinterlaceType",  ComboBoxFiltersDeinterlace.SelectedIndex.ToString());               // Filter Deinterlace
            }

            // ═══════════════════════════════════════════════════════════ Basic Video Settings ════════════════════════════════════════════════════════════
            
            writer.WriteElementString("VideoEncoder",           ComboBoxVideoEncoder.SelectedIndex.ToString());                         // Video Encoder
            writer.WriteElementString("VideoBitDepth",          ComboBoxVideoBitDepth.SelectedIndex.ToString());                        // Video BitDepth
            writer.WriteElementString("VideoSpeed",             SliderVideoSpeed.Value.ToString());                                     // Video Speed
            writer.WriteElementString("VideoPasses",            ComboBoxVideoPasses.SelectedIndex.ToString());                          // Video Passes
            if (RadioButtonVideoConstantQuality.IsChecked == true)
                writer.WriteElementString("VideoQuality",       SliderVideoQuality.Value.ToString());                                   // Video Quality
            if (RadioButtonVideoBitrate.IsChecked == true)
                writer.WriteElementString("VideoBitrate",       TextBoxVideoBitrate.Text);                                              // Video Bitrate
            if (ComboBoxVideoEncoder.SelectedIndex == 0)
                writer.WriteElementString("VideoAomencRT",      CheckBoxVideoAomencRealTime.IsChecked.ToString());                      // Video Aomenc Real Time Mode
            // ══════════════════════════════════════════════════════════ Advanced Video Settings ══════════════════════════════════════════════════════════

            writer.WriteElementString("VideoAdvanced",          CheckBoxVideoAdvancedSettings.IsChecked.ToString());                    // Video Advanced Settings
            writer.WriteElementString("VideoAdvancedCustom",    CheckBoxCustomVideoSettings.IsChecked.ToString());                      // Video Advanced Settings Custom

            if (CheckBoxVideoAdvancedSettings.IsChecked == true && CheckBoxCustomVideoSettings.IsChecked == false)
            {
                // Custom Advanced Settings
                if (ComboBoxVideoEncoder.SelectedIndex == 0)
                {
                    // aomenc
                    writer.WriteElementString("VideoAdvancedAomencThreads",     ComboBoxAomencThreads.SelectedIndex.ToString());        // Video Advanced Settings Aomenc Threads
                    writer.WriteElementString("VideoAdvancedAomencTileCols",    ComboBoxAomencTileColumns.SelectedIndex.ToString());    // Video Advanced Settings Aomenc Tile Columns
                    writer.WriteElementString("VideoAdvancedAomencTileRows",    ComboBoxAomencTileRows.SelectedIndex.ToString());       // Video Advanced Settings Aomenc Tile Rows
                    writer.WriteElementString("VideoAdvancedAomencGOP",         TextBoxAomencMaxGOP.Text);                              // Video Advanced Settings Aomenc GOP
                    writer.WriteElementString("VideoAdvancedAomencLag",         TextBoxAomencLagInFrames.Text);                         // Video Advanced Settings Aomenc Lag in Frames
                    writer.WriteElementString("VideoAdvancedAomencSharpness",   ComboBoxAomencSharpness.SelectedIndex.ToString());      // Video Advanced Settings Aomenc Sharpness
                    writer.WriteElementString("VideoAdvancedAomencColorPrim",   ComboBoxAomencColorPrimaries.SelectedIndex.ToString()); // Video Advanced Settings Aomenc Color Primaries
                    writer.WriteElementString("VideoAdvancedAomencColorTrans",  ComboBoxAomencColorTransfer.SelectedIndex.ToString());  // Video Advanced Settings Aomenc Color Transfer
                    writer.WriteElementString("VideoAdvancedAomencColorMatrix", ComboBoxAomencColorMatrix.SelectedIndex.ToString());    // Video Advanced Settings Aomenc Color Matrix
                    writer.WriteElementString("VideoAdvancedAomencColorFormat", ComboBoxAomencColorFormat.SelectedIndex.ToString());    // Video Advanced Settings Aomenc Color Format
                    writer.WriteElementString("VideoAdvancedAomencAQMode",      ComboBoxAomencAQMode.SelectedIndex.ToString());         // Video Advanced Settings Aomenc AQ Mode
                    writer.WriteElementString("VideoAdvancedAomencKFFiltering", ComboBoxAomencKeyFiltering.SelectedIndex.ToString());   // Video Advanced Settings Aomenc Keyframe Filtering
                    writer.WriteElementString("VideoAdvancedAomencTune",        ComboBoxAomencTune.SelectedIndex.ToString());           // Video Advanced Settings Aomenc Tune
                    writer.WriteElementString("VideoAdvancedAomencARNR",        CheckBoxAomencARNRMax.IsChecked.ToString());            // Video Advanced Settings Aomenc ARNR
                    if (CheckBoxAomencARNRMax.IsChecked == true)
                    {
                        writer.WriteElementString("VideoAdvancedAomencARNRMax", ComboBoxAomencARNRMax.SelectedIndex.ToString());        // Video Advanced Settings Aomenc ARNR Max
                        writer.WriteElementString("VideoAdvancedAomencARNRStre", ComboBoxAomencARNRStrength.SelectedIndex.ToString());  // Video Advanced Settings Aomenc ARNR Strength
                    }
                    writer.WriteElementString("VideoAdvancedAomencRowMT",       CheckBoxAomencRowMT.IsChecked.ToString());              // Video Advanced Settings Aomenc Row Mt
                    writer.WriteElementString("VideoAdvancedAomencCDEF",        CheckBoxAomencCDEF.IsChecked.ToString());               // Video Advanced Settings Aomenc CDEF
                }
                else if (ComboBoxVideoEncoder.SelectedIndex == 1)
                {
                    // rav1e
                    writer.WriteElementString("VideoAdvancedRav1eThreads",      ComboBoxRav1eThreads.SelectedIndex.ToString());         // Video Advanced Settings Rav1e Threads
                    writer.WriteElementString("VideoAdvancedRav1eTileCols",     ComboBoxRav1eTileColumns.SelectedIndex.ToString());     // Video Advanced Settings Rav1e Tile Columns
                    writer.WriteElementString("VideoAdvancedRav1eTileRows",     ComboBoxRav1eTileRows.SelectedIndex.ToString());        // Video Advanced Settings Rav1e Tile Rows
                    writer.WriteElementString("VideoAdvancedRav1eGOP",          TextBoxRav1eMaxGOP.Text);                               // Video Advanced Settings Rav1e GOP
                    writer.WriteElementString("VideoAdvancedRav1eRDO",          TextBoxRav1eLookahead.Text);                            // Video Advanced Settings Rav1e RDO Lookahead
                    writer.WriteElementString("VideoAdvancedRav1eColorPrim",    ComboBoxRav1eColorPrimaries.SelectedIndex.ToString());  // Video Advanced Settings Rav1e Color Primaries
                    writer.WriteElementString("VideoAdvancedRav1eColorTrans",   ComboBoxRav1eColorTransfer.SelectedIndex.ToString());   // Video Advanced Settings Rav1e Color Transfer
                    writer.WriteElementString("VideoAdvancedRav1eColorMatrix",  ComboBoxRav1eColorMatrix.SelectedIndex.ToString());     // Video Advanced Settings Rav1e Color Matrix
                    writer.WriteElementString("VideoAdvancedRav1eColorFormat",  ComboBoxRav1eColorFormat.SelectedIndex.ToString());     // Video Advanced Settings Rav1e Color Format
                    writer.WriteElementString("VideoAdvancedRav1eTune",         ComboBoxRav1eTune.SelectedIndex.ToString());            // Video Advanced Settings Rav1e Tune
                    writer.WriteElementString("VideoAdvancedRav1eMastering",    CheckBoxRav1eMasteringDisplay.IsChecked.ToString());    // Video Advanced Settings Rav1e Mastering Display
                    if (CheckBoxRav1eMasteringDisplay.IsChecked == true)
                    {
                        writer.WriteElementString("VideoAdvancedRav1eMasteringGx",  TextBoxRav1eMasteringGx.Text);                      // Video Advanced Settings Rav1e Mastering Display Gx
                        writer.WriteElementString("VideoAdvancedRav1eMasteringGy",  TextBoxRav1eMasteringGy.Text);                      // Video Advanced Settings Rav1e Mastering Display Gy
                        writer.WriteElementString("VideoAdvancedRav1eMasteringBx",  TextBoxRav1eMasteringBx.Text);                      // Video Advanced Settings Rav1e Mastering Display Bx
                        writer.WriteElementString("VideoAdvancedRav1eMasteringBy",  TextBoxRav1eMasteringBy.Text);                      // Video Advanced Settings Rav1e Mastering Display By
                        writer.WriteElementString("VideoAdvancedRav1eMasteringRx",  TextBoxRav1eMasteringRx.Text);                      // Video Advanced Settings Rav1e Mastering Display Rx
                        writer.WriteElementString("VideoAdvancedRav1eMasteringRy",  TextBoxRav1eMasteringRy.Text);                      // Video Advanced Settings Rav1e Mastering Display Ry
                        writer.WriteElementString("VideoAdvancedRav1eMasteringWPx", TextBoxRav1eMasteringWPx.Text);                     // Video Advanced Settings Rav1e Mastering Display WPx
                        writer.WriteElementString("VideoAdvancedRav1eMasteringWPy", TextBoxRav1eMasteringWPy.Text);                     // Video Advanced Settings Rav1e Mastering Display WPy
                        writer.WriteElementString("VideoAdvancedRav1eMasteringLx",  TextBoxRav1eMasteringLx.Text);                      // Video Advanced Settings Rav1e Mastering Display Lx
                        writer.WriteElementString("VideoAdvancedRav1eMasteringLy",  TextBoxRav1eMasteringLy.Text);                      // Video Advanced Settings Rav1e Mastering Display Ly
                    }
                    writer.WriteElementString("VideoAdvancedRav1eLight",        CheckBoxRav1eContentLight.IsChecked.ToString());        // Video Advanced Settings Rav1e Mastering Content Light
                    if (CheckBoxRav1eMasteringDisplay.IsChecked == true)
                    {
                        writer.WriteElementString("VideoAdvancedRav1eLightCll",  TextBoxRav1eContentLightCll.Text);                     // Video Advanced Settings Rav1e Mastering Content Light Cll
                        writer.WriteElementString("VideoAdvancedRav1eLightFall", TextBoxRav1eContentLightFall.Text);                    // Video Advanced Settings Rav1e Mastering Content Light Fall
                    }
                }
                else if (ComboBoxVideoEncoder.SelectedIndex == 2)
                {
                    // svt-av1
                    writer.WriteElementString("VideoAdvancedSVTAV1TileCols",    ComboBoxSVTAV1TileColumns.SelectedIndex.ToString());    // Video Advanced Settings SVT-AV1 Tile Columns
                    writer.WriteElementString("VideoAdvancedSVTAV1TileRows",    ComboBoxSVTAV1TileRows.SelectedIndex.ToString());       // Video Advanced Settings SVT-AV1 Tile Rows
                    writer.WriteElementString("VideoAdvancedSVTAV1GOP",         TextBoxSVTAV1MaxGOP.Text);                              // Video Advanced Settings SVT-AV1 GOP
                    writer.WriteElementString("VideoAdvancedSVTAV1AQMode",      ComboBoxSVTAV1AQMode.SelectedIndex.ToString());         // Video Advanced Settings SVT-AV1 AQ-Mode
                    writer.WriteElementString("VideoAdvancedSVTAV1ColorFmt",    ComboBoxSVTAV1ColorFormat.SelectedIndex.ToString());    // Video Advanced Settings SVT-AV1 Color Format
                    writer.WriteElementString("VideoAdvancedSVTAV1Profile",     ComboBoxSVTAV1Profile.SelectedIndex.ToString());        // Video Advanced Settings SVT-AV1 Profile
                    writer.WriteElementString("VideoAdvancedSVTAV1AltRefLevel", ComboBoxSVTAV1AltRefLevel.SelectedIndex.ToString());    // Video Advanced Settings SVT-AV1 Alt Ref Level
                    writer.WriteElementString("VideoAdvancedSVTAV1AltRefStren", ComboBoxSVTAV1AltRefStrength.SelectedIndex.ToString()); // Video Advanced Settings SVT-AV1 Alt Ref Strength
                    writer.WriteElementString("VideoAdvancedSVTAV1AltRefFrame", ComboBoxSVTAV1AltRefFrames.SelectedIndex.ToString());   // Video Advanced Settings SVT-AV1 Alt Ref Frames
                    writer.WriteElementString("VideoAdvancedSVTAV1HDR",         CheckBoxSVTAV1HDR.IsChecked.ToString());                // Video Advanced Settings SVT-AV1 HDR
                }

            }
            else if (CheckBoxVideoAdvancedSettings.IsChecked == true && CheckBoxCustomVideoSettings.IsChecked == true)
            {
                writer.WriteElementString("VideoAdvancedCustomString",          TextBoxCustomVideoSettings.Text);                       // Video Advanced Settings Custom String
            }

            // Writes Ending XML Element
            writer.WriteEndElement();
            // Cloeses XML Writer
            writer.Close();
        }

        private void LoadSettings(bool LoadProfile, string SaveName)
        {
            string directory = "";
            if (LoadProfile)
            {
                // Path to Profile Save
                directory = Path.Combine(Directory.GetCurrentDirectory(), "Profiles", SaveName);
            }
            else
            {
                // Path to Project Save File
                directory = SaveName;
            }

            // Init XML Reader
            XmlDocument doc = new XmlDocument();
            // Read XML File
            doc.Load(directory);
            // Select Node
            XmlNodeList node = doc.GetElementsByTagName("Settings");
            // Iterates over all Args in XML Document
            foreach (XmlNode n in node[0].ChildNodes)
            {
                switch (n.Name)
                {
                    case "VideoInput":                      VideoInput = n.InnerText; TextBoxVideoSource.Text = n.InnerText; VideoInputSet = true;
                                                            TempPathFileName = Path.GetFileNameWithoutExtension(n.InnerText);       break;  // Video Input
                    case "VideoOutput":                     VideoOutput = n.InnerText; VideoOutputSet = true;
                                                            TextBoxVideoDestination.Text = n.InnerText;                             break;  // Video Output
                    case "WorkerCount":                     ComboBoxWorkerCount.SelectedIndex = int.Parse(n.InnerText);             break;  // Worker Count
                    case "WorkerPriority":                  ComboBoxProcessPriority.SelectedIndex = int.Parse(n.InnerText);         break;  // Worker Priority
                    // ═══════════════════════════════════════════════════════════════════ Audio ═══════════════════════════════════════════════════════════════════
                    case "AudioTrackOne":                   CheckBoxAudioTrackOne.IsChecked = n.InnerText == "True";                break;  // Audio Track One Active
                    case "AudioTrackTwo":                   CheckBoxAudioTrackTwo.IsChecked = n.InnerText == "True";                break;  // Audio Track Two Active
                    case "AudioTrackThree":                 CheckBoxAudioTrackThree.IsChecked = n.InnerText == "True";              break;  // Audio Track Three Active
                    case "AudioTrackFour":                  CheckBoxAudioTrackFour.IsChecked = n.InnerText == "True";               break;  // Audio Track Four Active
                    case "AudioLangOne":                    ComboBoxTrackOneLanguage.SelectedIndex = int.Parse(n.InnerText);        break;  // Audio Track One Language
                    case "AudioLangTwo":                    ComboBoxTrackTwoLanguage.SelectedIndex = int.Parse(n.InnerText);        break;  // Audio Track Two Language
                    case "AudioLangThree":                  ComboBoxTrackThreeLanguage.SelectedIndex = int.Parse(n.InnerText);      break;  // Audio Track Three Language
                    case "AudioLangFour":                   ComboBoxTrackFourLanguage.SelectedIndex = int.Parse(n.InnerText);       break;  // Audio Track Four Language
                    case "TrackOneCodec":                   ComboBoxAudioCodec.SelectedIndex = int.Parse(n.InnerText);              break;  // Audio Track One Codec
                    case "TrackTwoCodec":                   ComboBoxAudioCodecTrackTwo.SelectedIndex = int.Parse(n.InnerText);      break;  // Audio Track Two Codec
                    case "TrackThreeCodec":                 ComboBoxAudioCodecTrackThree.SelectedIndex = int.Parse(n.InnerText);    break;  // Audio Track Three Codec
                    case "TrackFourCodec":                  ComboBoxAudioCodecTrackFour.SelectedIndex = int.Parse(n.InnerText);     break;  // Audio Track Four Codec
                    case "TrackOneBitrate":                 TextBoxAudioBitrate.Text = n.InnerText;                                 break;  // Audio Track One Bitrate
                    case "TrackTwoBitrate":                 TextBoxAudioBitrateTrackTwo.Text = n.InnerText;                         break;  // Audio Track Two Bitrate
                    case "TrackThreeBitrate":               TextBoxAudioBitrateTrackThree.Text = n.InnerText;                       break;  // Audio Track Three Bitrate
                    case "TrackFourBitrate":                TextBoxAudioBitrateTrackFour.Text = n.InnerText;                        break;  // Audio Track Four Bitrate
                    case "TrackOneChannels":                ComboBoxTrackOneChannels.SelectedIndex = int.Parse(n.InnerText);        break;  // Audio Track One Channels
                    case "TrackTwoChannels":                ComboBoxTrackTwoChannels.SelectedIndex = int.Parse(n.InnerText);        break;  // Audio Track Two Channels
                    case "TrackThreeChannels":              ComboBoxTrackThreeChannels.SelectedIndex = int.Parse(n.InnerText);      break;  // Audio Track Three Channels
                    case "TrackFourChannels":               ComboBoxTrackFourChannels.SelectedIndex = int.Parse(n.InnerText);       break;  // Audio Track Four Channels
                    // ═════════════════════════════════════════════════════════════════ Splitting ═════════════════════════════════════════════════════════════════
                    case "SplittingMethod":                 ComboBoxSplittingMethod.SelectedIndex = int.Parse(n.InnerText);         break;  // Splitting Method
                    case "SplittingThreshold":              TextBoxSplittingThreshold.Text = n.InnerText;                           break;  // Splitting Threshold
                    case "SplittingReencode":               ComboBoxSplittingReencodeMethod.SelectedIndex = int.Parse(n.InnerText); break;  // Splitting Reencode Codec
                    case "SplittingReencodeActive":         CheckBoxSplittingReencode.IsChecked = n.InnerText == "True";            break;  // Splitting Reencode Active
                    case "SplittingReencodeLength":         TextBoxSplittingChunkLength.Text = n.InnerText;                         break;  // Splitting Chunk Length
                    // ══════════════════════════════════════════════════════════════════ Filters ══════════════════════════════════════════════════════════════════
                    case "FilterCrop":                      CheckBoxFiltersCrop.IsChecked = n.InnerText == "True";                  break;  // Filter Crop (Boolean)
                    case "FilterCropTop":                   TextBoxFiltersCropTop.Text = n.InnerText;                               break;  // Filter Crop Top
                    case "FilterCropBottom":                TextBoxFiltersCropBottom.Text = n.InnerText;                            break;  // Filter Crop Bottom
                    case "FilterCropLeft":                  TextBoxFiltersCropLeft.Text = n.InnerText;                              break;  // Filter Crop Left
                    case "FilterCropRight":                 TextBoxFiltersCropRight.Text = n.InnerText;                             break;  // Filter Crop Right
                    case "FilterResize":                    CheckBoxFiltersResize.IsChecked = n.InnerText == "True";                break;  // Filter Resize (Boolean)
                    case "FilterResizeWidth":               TextBoxFiltersResizeWidth.Text = n.InnerText;                           break;  // Filter Resize Width
                    case "FilterResizeHeight":              TextBoxFiltersResizeHeight.Text = n.InnerText;                          break;  // Filter Resize Height
                    case "FilterResizeAlgo":                ComboBoxFiltersScaling.SelectedIndex = int.Parse(n.InnerText);          break;  // Filter Resize Scaling Algorithm
                    case "FilterRotate":                    CheckBoxFiltersRotate.IsChecked = n.InnerText == "True";                break;  // Filter Rotate (Boolean)
                    case "FilterRotateAmount":              ComboBoxFiltersRotate.SelectedIndex = int.Parse(n.InnerText);           break;  // Filter Rotate
                    case "FilterDeinterlace":               CheckBoxFiltersDeinterlace.IsChecked = n.InnerText == "True";           break;  // Filter Deinterlace (Boolean)
                    case "FilterDeinterlaceType":           ComboBoxFiltersDeinterlace.SelectedIndex = int.Parse(n.InnerText);      break;  // Filter Deinterlace
                    // ═══════════════════════════════════════════════════════════ Basic Video Settings ════════════════════════════════════════════════════════════
                    case "VideoEncoder":                    ComboBoxVideoEncoder.SelectedIndex = int.Parse(n.InnerText);            break;  // Video Encoder
                    case "VideoBitDepth":                   ComboBoxVideoBitDepth.SelectedIndex = int.Parse(n.InnerText);           break;  // Video BitDepth
                    case "VideoSpeed":                      SliderVideoSpeed.Value = int.Parse(n.InnerText);                        break;  // Video Speed
                    case "VideoPasses":                     ComboBoxVideoPasses.SelectedIndex = int.Parse(n.InnerText);             break;  // Video Passes
                    case "VideoQuality":                    SliderVideoQuality.Value = int.Parse(n.InnerText);
                                                            RadioButtonVideoConstantQuality.IsChecked = true;                       break;  // Video Quality
                    case "VideoBitrate":                    TextBoxVideoBitrate.Text = n.InnerText;
                                                            RadioButtonVideoBitrate.IsChecked = true;                               break;  // Video Bitrate
                    case "VideoAomencRT":                   CheckBoxVideoAomencRealTime.IsChecked = n.InnerText == "True";          break;  // Video Aomenc Real Time Mode
                    // ═════════════════════════════════════════════════════════ Advanced Video Settings ═══════════════════════════════════════════════════════════
                    case "VideoAdvanced":                   CheckBoxVideoAdvancedSettings.IsChecked = n.InnerText == "True";        break;  // Video Advanced Settings
                    case "VideoAdvancedCustom":             CheckBoxCustomVideoSettings.IsChecked = n.InnerText == "True";          break;  // Video Advanced Settings Custom
                    case "VideoAdvancedAomencThreads":      ComboBoxAomencThreads.SelectedIndex = int.Parse(n.InnerText);           break;  // Video Advanced Settings Aomenc Threads
                    case "VideoAdvancedAomencTileCols":     ComboBoxAomencTileColumns.SelectedIndex = int.Parse(n.InnerText);       break;  // Video Advanced Settings Aomenc Tile Columns
                    case "VideoAdvancedAomencTileRows":     ComboBoxAomencTileRows.SelectedIndex = int.Parse(n.InnerText);          break;  // Video Advanced Settings Aomenc Tile Rows
                    case "VideoAdvancedAomencGOP":          TextBoxAomencMaxGOP.Text = n.InnerText;                                 break;  // Video Advanced Settings Aomenc GOP
                    case "VideoAdvancedAomencLag":          TextBoxAomencLagInFrames.Text = n.InnerText;                            break;  // Video Advanced Settings Aomenc Lag in Frames
                    case "VideoAdvancedAomencSharpness":    ComboBoxAomencSharpness.SelectedIndex = int.Parse(n.InnerText);         break;  // Video Advanced Settings Aomenc Sharpness
                    case "VideoAdvancedAomencColorPrim":    ComboBoxAomencColorPrimaries.SelectedIndex = int.Parse(n.InnerText);    break;  // Video Advanced Settings Aomenc Color Primaries
                    case "VideoAdvancedAomencColorTrans":   ComboBoxAomencColorTransfer.SelectedIndex = int.Parse(n.InnerText);     break;  // Video Advanced Settings Aomenc Color Transfer
                    case "VideoAdvancedAomencColorMatrix":  ComboBoxAomencColorMatrix.SelectedIndex = int.Parse(n.InnerText);       break;  // Video Advanced Settings Aomenc Color Matrix
                    case "VideoAdvancedAomencColorFormat":  ComboBoxAomencColorFormat.SelectedIndex = int.Parse(n.InnerText);       break;  // Video Advanced Settings Aomenc Color Format
                    case "VideoAdvancedAomencAQMode":       ComboBoxAomencAQMode.SelectedIndex = int.Parse(n.InnerText);            break;  // Video Advanced Settings Aomenc AQ Mode
                    case "VideoAdvancedAomencKFFiltering":  ComboBoxAomencKeyFiltering.SelectedIndex = int.Parse(n.InnerText);      break;  // Video Advanced Settings Aomenc Keyframe Filtering
                    case "VideoAdvancedAomencTune":         ComboBoxAomencTune.SelectedIndex = int.Parse(n.InnerText);              break;  // Video Advanced Settings Aomenc Tune
                    case "VideoAdvancedAomencARNR":         CheckBoxAomencARNRMax.IsChecked = n.InnerText == "True";                break;  // Video Advanced Settings Aomenc ARNR
                    case "VideoAdvancedAomencARNRMax":      ComboBoxAomencARNRMax.SelectedIndex = int.Parse(n.InnerText);           break;  // Video Advanced Settings Aomenc ARNR Max
                    case "VideoAdvancedAomencARNRStre":     ComboBoxAomencARNRStrength.SelectedIndex = int.Parse(n.InnerText);      break;  // Video Advanced Settings Aomenc ARNR Strength
                    case "VideoAdvancedAomencRowMT":        CheckBoxAomencRowMT.IsChecked = n.InnerText == "True";                  break;  // Video Advanced Settings Aomenc Row Mt
                    case "VideoAdvancedAomencCDEF":         CheckBoxAomencCDEF.IsChecked = n.InnerText == "True";                   break;  // Video Advanced Settings Aomenc CDEF
                    case "VideoAdvancedRav1eThreads":       ComboBoxRav1eThreads.SelectedIndex = int.Parse(n.InnerText);            break;  // Video Advanced Settings Rav1e Threads
                    case "VideoAdvancedRav1eTileCols":      ComboBoxRav1eTileColumns.SelectedIndex = int.Parse(n.InnerText);        break;  // Video Advanced Settings Rav1e Tile Columns
                    case "VideoAdvancedRav1eTileRows":      ComboBoxRav1eTileRows.SelectedIndex = int.Parse(n.InnerText);           break;  // Video Advanced Settings Rav1e Tile Rows
                    case "VideoAdvancedRav1eGOP":           TextBoxRav1eMaxGOP.Text = n.InnerText;                                  break;  // Video Advanced Settings Rav1e GOP
                    case "VideoAdvancedRav1eRDO":           TextBoxRav1eLookahead.Text = n.InnerText;                               break;  // Video Advanced Settings Rav1e RDO Lookahead
                    case "VideoAdvancedRav1eColorPrim":     ComboBoxRav1eColorPrimaries.SelectedIndex = int.Parse(n.InnerText);     break;  // Video Advanced Settings Rav1e Color Primaries
                    case "VideoAdvancedRav1eColorTrans":    ComboBoxRav1eColorTransfer.SelectedIndex = int.Parse(n.InnerText);      break;  // Video Advanced Settings Rav1e Color Transfer
                    case "VideoAdvancedRav1eColorMatrix":   ComboBoxRav1eColorMatrix.SelectedIndex = int.Parse(n.InnerText);        break;  // Video Advanced Settings Rav1e Color Matrix
                    case "VideoAdvancedRav1eColorFormat":   ComboBoxRav1eColorFormat.SelectedIndex = int.Parse(n.InnerText);        break;  // Video Advanced Settings Rav1e Color Format
                    case "VideoAdvancedRav1eTune":          ComboBoxRav1eTune.SelectedIndex = int.Parse(n.InnerText);               break;  // Video Advanced Settings Rav1e Tune
                    case "VideoAdvancedRav1eMastering":     CheckBoxRav1eMasteringDisplay.IsChecked = n.InnerText == "True";        break;  // Video Advanced Settings Rav1e Mastering Display
                    case "VideoAdvancedRav1eMasteringGx":   TextBoxRav1eMasteringGx.Text = n.InnerText;                             break;  // Video Advanced Settings Rav1e Mastering Display Gx
                    case "VideoAdvancedRav1eMasteringGy":   TextBoxRav1eMasteringGy.Text = n.InnerText;                             break;  // Video Advanced Settings Rav1e Mastering Display Gy
                    case "VideoAdvancedRav1eMasteringBx":   TextBoxRav1eMasteringBx.Text = n.InnerText;                             break;  // Video Advanced Settings Rav1e Mastering Display Bx
                    case "VideoAdvancedRav1eMasteringBy":   TextBoxRav1eMasteringBy.Text = n.InnerText;                             break;  // Video Advanced Settings Rav1e Mastering Display By
                    case "VideoAdvancedRav1eMasteringRx":   TextBoxRav1eMasteringRx.Text = n.InnerText;                             break;  // Video Advanced Settings Rav1e Mastering Display Rx
                    case "VideoAdvancedRav1eMasteringRy":   TextBoxRav1eMasteringRy.Text = n.InnerText;                             break;  // Video Advanced Settings Rav1e Mastering Display Ry
                    case "VideoAdvancedRav1eMasteringWPx":  TextBoxRav1eMasteringWPx.Text = n.InnerText;                            break;  // Video Advanced Settings Rav1e Mastering Display WPx
                    case "VideoAdvancedRav1eMasteringWPy":  TextBoxRav1eMasteringWPy.Text = n.InnerText;                            break;  // Video Advanced Settings Rav1e Mastering Display WPy
                    case "VideoAdvancedRav1eMasteringLx":   TextBoxRav1eMasteringLx.Text = n.InnerText;                             break;  // Video Advanced Settings Rav1e Mastering Display Lx
                    case "VideoAdvancedRav1eMasteringLy":   TextBoxRav1eMasteringLy.Text = n.InnerText;                             break;  // Video Advanced Settings Rav1e Mastering Display Ly
                    case "VideoAdvancedRav1eLight":         CheckBoxRav1eContentLight.IsChecked = n.InnerText == "True";            break;  // Video Advanced Settings Rav1e Mastering Content Light
                    case "VideoAdvancedRav1eLightCll":      TextBoxRav1eContentLightCll.Text = n.InnerText;                         break;  // Video Advanced Settings Rav1e Mastering Content Light Cll
                    case "VideoAdvancedRav1eLightFall":     TextBoxRav1eContentLightFall.Text = n.InnerText;                        break;  // Video Advanced Settings Rav1e Mastering Content Light Fall
                    case "VideoAdvancedSVTAV1TileCols":     ComboBoxSVTAV1TileColumns.SelectedIndex = int.Parse(n.InnerText);       break;  // Video Advanced Settings SVT-AV1 Tile Columns
                    case "VideoAdvancedSVTAV1TileRows":     ComboBoxSVTAV1TileRows.SelectedIndex = int.Parse(n.InnerText);          break;  // Video Advanced Settings SVT-AV1 Tile Rows
                    case "VideoAdvancedSVTAV1GOP":          TextBoxSVTAV1MaxGOP.Text = n.InnerText;                                 break;  // Video Advanced Settings SVT-AV1 GOP
                    case "VideoAdvancedSVTAV1AQMode":       ComboBoxSVTAV1AQMode.SelectedIndex = int.Parse(n.InnerText);            break;  // Video Advanced Settings SVT-AV1 AQ-Mode
                    case "VideoAdvancedSVTAV1ColorFmt":     ComboBoxSVTAV1ColorFormat.SelectedIndex = int.Parse(n.InnerText);       break;  // Video Advanced Settings SVT-AV1 Color Format
                    case "VideoAdvancedSVTAV1Profile":      ComboBoxSVTAV1Profile.SelectedIndex = int.Parse(n.InnerText);           break;  // Video Advanced Settings SVT-AV1 Profile
                    case "VideoAdvancedSVTAV1AltRefLevel":  ComboBoxSVTAV1AltRefLevel.SelectedIndex = int.Parse(n.InnerText);       break;  // Video Advanced Settings SVT-AV1 Alt Ref Level
                    case "VideoAdvancedSVTAV1AltRefStren":  ComboBoxSVTAV1AltRefStrength.SelectedIndex = int.Parse(n.InnerText);    break;  // Video Advanced Settings SVT-AV1 Alt Ref Strength
                    case "VideoAdvancedSVTAV1AltRefFrame":  ComboBoxSVTAV1AltRefFrames.SelectedIndex = int.Parse(n.InnerText);      break;  // Video Advanced Settings SVT-AV1 Alt Ref Frames
                    case "VideoAdvancedSVTAV1HDR":          CheckBoxSVTAV1HDR.IsChecked = n.InnerText == "True";                    break;  // Video Advanced Settings SVT-AV1 HDR
                    case "VideoAdvancedCustomString":       TextBoxCustomVideoSettings.Text = n.InnerText;                          break;  // Video Advanced Settings Custom String
                    // Subtitles
                    case "SubOne":                          CheckBoxSubtitleActivatedOne.IsChecked = n.InnerText == "True";         break;  // Subtitle Track One Active
                    case "SubTwo":                          CheckBoxSubtitleActivatedTwo.IsChecked = n.InnerText == "True";         break;  // Subtitle Track Two Active
                    case "SubThree":                        CheckBoxSubtitleActivatedThree.IsChecked = n.InnerText == "True";       break;  // Subtitle Track Three Active
                    case "SubFour":                         CheckBoxSubtitleActivatedFour.IsChecked = n.InnerText == "True";        break;  // Subtitle Track Four Active
                    case "SubFive":                         CheckBoxSubtitleActivatedFive.IsChecked = n.InnerText == "True";        break;  // Subtitle Track Five Active
                    case "SubOnePath":                      TextBoxSubtitleTrackOne.Text = n.InnerText;                             break;  // Subtitle Track One Path
                    case "SubTwoPath":                      TextBoxSubtitleTrackTwo.Text = n.InnerText;                             break;  // Subtitle Track Two Path
                    case "SubThreePath":                    TextBoxSubtitleTrackThree.Text = n.InnerText;                           break;  // Subtitle Track Three Path
                    case "SubFourPath":                     TextBoxSubtitleTrackFour.Text = n.InnerText;                            break;  // Subtitle Track Four Path
                    case "SubFivePath":                     TextBoxSubtitleTrackFive.Text = n.InnerText;                            break;  // Subtitle Track Five Path
                    case "SubOneName":                      TextBoxSubOneName.Text = n.InnerText;                                   break;  // Subtitle Track One Name
                    case "SubTwoName":                      TextBoxSubTwoName.Text = n.InnerText;                                   break;  // Subtitle Track Two Name
                    case "SubThreeName":                    TextBoxSubThreeName.Text = n.InnerText;                                 break;  // Subtitle Track Three Name
                    case "SubFourName":                     TextBoxSubFourName.Text = n.InnerText;                                  break;  // Subtitle Track Four Name
                    case "SubFiveName":                     TextBoxSubFiveName.Text = n.InnerText;                                  break;  // Subtitle Track Five Name
                    case "SubOneLanguage":                  ComboBoxSubTrackOneLanguage.SelectedIndex = int.Parse(n.InnerText);     break;  // Subtitle Track One Language
                    case "SubTwoLanguage":                  ComboBoxSubTrackTwoLanguage.SelectedIndex = int.Parse(n.InnerText);     break;  // Subtitle Track Two Language
                    case "SubThreeLanguage":                ComboBoxSubTrackThreeLanguage.SelectedIndex = int.Parse(n.InnerText);   break;  // Subtitle Track Three Language
                    case "SubFourLanguage":                 ComboBoxSubTrackFourLanguage.SelectedIndex = int.Parse(n.InnerText);    break;  // Subtitle Track Four Language
                    case "SubFiveLanguage":                 ComboBoxSubTrackFiveLanguage.SelectedIndex = int.Parse(n.InnerText);    break;  // Subtitle Track Five Language
                    default: break;
                }
            }
        }

    }
}
