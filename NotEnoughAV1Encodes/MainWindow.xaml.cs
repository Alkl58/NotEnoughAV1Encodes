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
using System.Windows.Media.Imaging;
using System.Globalization;

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
        public static string EncoderVP9Command = null;
        // Temp Settings
        public static int WorkerCount = 0;          // amount of workers
        public static int EncodeMethod = 0;         // 0 = aomenc, 1 = rav1e, 2 = svt-av1...
        public static int SplitMethod = 0;          // 0 = ffmpeg; 1 = pyscenedetect; 2 = chunking
        public static bool OnePass = true;          // true = Onepass, false = Twopass
        public static bool Priority = true;         // true = normal, false = below normal (process priority)
        public static bool Logging = true;          // Program Logging
        public static bool TrimEnabled = false;     // Trim Boolean
        public static bool VFRVideo = false;        // Wether or not timestamp file should be used
        public static string VSYNC = " -vsync 0 ";  // Default Piping Frame Sync Method
        public static string VFRCMD = "";           // VFR Muxing Command
        public static string[] VideoChunks;         // Array of command/videochunks
        public static string TrimCommand;           // Trim Parameters
        // Splitting Settings
        public static string FFmpegThreshold;
        public static string ChunkLength;
        public static string HardSubCMD;
        public static bool SplitReencode = false;
        public static int ReEncodeMethodSplitting;
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
        public static bool subMessageShowed = false;// Used to message the user when trying to do softsub in MP4 Container
        // IO Paths
        public static string BatchOutContainer = ".mkv";
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
        public static string VPXPath = null;        // Path to vpxenc
        public static bool PySceneFound = false;    // 
        // Temp Variables
        public static bool EncodeStarted = false;   // Encode Started Boolean
        public static bool BatchEncoding = false;   // Batch Encoding
        public static bool DeleteTempFiles = false; // Temp File Deletion
        public static bool PlayUISounds = false;    // UI Sounds (Finished Encoding / Error)
        public static bool ShowTerminal = false;    // Show / Hide Encoding Terminal
        public static bool CustomBG = false;        // Custom Image Background
        public static bool StartUp = true;          // Avoids conflicts with Settings Tab
        public static bool ReadTimeCode = false;    // Skips creating an image preview at start
        public static bool PopupWindow = false;     // Shows a popup window after encode finished
        public static bool Yadif1 = false;          // If true -> double the frames
        public static string TrimEndTemp = "00:23:00.000"; // Sets the maximum trim time
        public static int TotalFrames = 0;          // used for progressbar and frame check
        public DateTime StartTime;                  // used for eta calculation
        // Progress Cancellation
        private CancellationTokenSource cancellationTokenSource;

        public MainWindow()
        {
            InitializeComponent();
            Startup();
        }

        private void Startup()
        {
            // Sets the GUI Version
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            GroubBoxVersion.Header = "Version: " + version.Remove(version.Length - 2);

            CheckDependencies.Check();

            // Sets the workercount combobox
            int corecount = SmallFunctions.getCoreCount();
            for (int i = 1; i <= corecount; i++) { ComboBoxWorkerCount.Items.Add(i); }
            ComboBoxWorkerCount.SelectedItem = Convert.ToInt32(corecount * 75 / 100);

            LoadPresetsIntoComboBox();
            LoadDefaultProfile();
            LoadSettingsTab();

            StartUp = false;
        }

        // ═══════════════════════════════════════ UI Logic ═══════════════════════════════════════

        private void ToggleSwitchSubtitleActivatedOne_Toggled(object sender, RoutedEventArgs e)
        {
            if (VideoOutputSet && Path.GetExtension(VideoOutput) == ".mp4" && !subMessageShowed)
            {
                if (ToggleSwitchSubtitleActivatedOne.IsOn == true || ToggleSwitchSubtitleActivatedTwo.IsOn == true || ToggleSwitchSubtitleActivatedThree.IsOn == true || ToggleSwitchSubtitleActivatedFour.IsOn == true || ToggleSwitchSubtitleActivatedFive.IsOn == true)
                {
                    MessageBox.Show("Softsub not supported with MP4 as container", "Subtitles", MessageBoxButton.OK);
                    // Only show this Message once per instance
                    subMessageShowed = true;
                }
            }
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            // Drag & Drop Video Files into GUI
            List<string> filepaths = new List<string>();
            foreach (var s in (string[])e.Data.GetData(DataFormats.FileDrop, false)) { filepaths.Add(s); }
            int counter = 0;
            foreach (var item in filepaths)
            {
                if (counter == 0) { SingleFileInput(item); }
                counter += 1;
            }
            if (counter >= 2) { MessageBox.Show("Multiple Input Files currently not implemented.\nPlease use Batch encoding instead!"); }
        }

        private void SingleFileInput(string file)
        {
            VideoInputSet = true;
            TextBoxVideoSource.Text = file;
            VideoInput = file;
            TempPathFileName = Path.GetFileNameWithoutExtension(file);
            SmallFunctions.CheckUnicode(TempPathFileName);
            BatchEncoding = false;
            GetAudioInformation();
            GetSubtitleTracks();
            TextBoxTrimEnd.Text = SmallFunctions.GetVideoLengthAccurate(file);
            TrimEndTemp = TextBoxTrimEnd.Text;
            LabelVideoLength.Content = TrimEndTemp.Remove(TrimEndTemp.Length - 4);
            AutoSetBitDepthAndColorFormat(file);
            LabelVideoColorFomat.Content = FFprobe.GetPixelFormat(file);
            LabelVideoFramerate.Content = FFprobe.GetFrameRate(file);
            string res = FFprobe.GetResolution(file);
            LabelVideoResolution.Content = res;
            TextBoxFiltersResizeHeight.Text = res.Substring(res.LastIndexOf('x') + 1);
            ReadTimeCode = true;
        }

        private void AutoSetBitDepthAndColorFormat(string result)
        {
            // Get & Set correct Color Formats

            string format = FFprobe.GetPixelFormat(result);

            Regex rgx10bit = new Regex(@"10le$");
            Regex rgx12bit = new Regex(@"12le$");
            Regex rgxyuv420 = new Regex(@"^yuv420p");
            Regex rgxyuv422 = new Regex(@"^yuv422p");
            Regex rgxyuv444 = new Regex(@"^yuv444p");

            int rgxIndex = 0;
            int rgxBit = 0;

            if (rgx10bit.IsMatch(format)) {  rgxBit = 1; }
            else if (rgx12bit.IsMatch(format)) { rgxBit = 2; }

            if (rgxyuv420.IsMatch(format)) { rgxIndex = 0; }
            else if (rgxyuv422.IsMatch(format)) 
            {
                rgxIndex = 1;
                ToggleSwitchAdvancedVideoSettings.IsOn = true;
            }
            else if (rgxyuv444.IsMatch(format))
            {
                rgxIndex = 2;
                ToggleSwitchAdvancedVideoSettings.IsOn = true;
            }

            ComboBoxVideoBitDepth.SelectedIndex = rgxBit;
            ComboBoxAomencColorFormat.SelectedIndex = rgxIndex;
            ComboBoxRav1eColorFormat.SelectedIndex = rgxIndex;
            ComboBoxSVTAV1ColorFormat.SelectedIndex = rgxIndex;
            ComboBoxVP9ColorFormat.SelectedIndex = rgxIndex;
        }

        private void ComboBoxSplittingMethod_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if(ComboBoxSplittingMethod.SelectedIndex == 1){
                if (PySceneFound == false)
                {
                    MessageBox.Show("PySceneDetect seems to be missing on your System.\n\nPlease make sure that Python and PySceneDetect is installed.\n\nInstruction:\n1. Install Python 3.8.6\n2. In CMD: pip install scenedetect[opencv]\n3. In CMD: pip install numpy==1.19.3\n", "PySceneDetect", MessageBoxButton.OK);
                }
            }
        }

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

        private void MessageBoxUnsupportedSubtitleBurnIn()
        {
            if (BatchEncoding == false)
                MessageBox.Show("This subtitle is not supported for hardsubbing!", "Subtitle", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void CheckBoxBurnToggle(bool one, bool two, bool three, bool four, bool five)
        {
            CheckBoxSubOneBurn.IsChecked = one;
            CheckBoxSubTwoBurn.IsChecked = two;
            CheckBoxSubThreeBurn.IsChecked = three;
            CheckBoxSubFourBurn.IsChecked = four;
            CheckBoxSubFiveBurn.IsChecked = five;
        }

        private void CheckBoxSubOneBurn_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxBurnToggle(true, false, false, false, false);
            CheckBoxSubOneDefault.IsChecked = false;
            if (Path.GetExtension(TextBoxSubtitleTrackOne.Text) == ".sup")
            {
                MessageBoxUnsupportedSubtitleBurnIn();
                CheckBoxSubOneBurn.IsChecked = false;
            }
        }

        private void CheckBoxSubTwoBurn_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxBurnToggle(false, true, false, false, false);
            CheckBoxSubTwoDefault.IsChecked = false;
            if (Path.GetExtension(TextBoxSubtitleTrackTwo.Text) == ".sup")
            {
                MessageBoxUnsupportedSubtitleBurnIn();
                CheckBoxSubTwoBurn.IsChecked = false;
            }
        }

        private void CheckBoxSubThreeBurn_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxBurnToggle(false, false, true, false, false);
            CheckBoxSubThreeDefault.IsChecked = false;
            if (Path.GetExtension(TextBoxSubtitleTrackThree.Text) == ".sup")
            {
                MessageBoxUnsupportedSubtitleBurnIn();
                CheckBoxSubThreeBurn.IsChecked = false;
            }
        }

        private void CheckBoxSubFourBurn_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxBurnToggle(false, false, false, true, false);
            CheckBoxSubFourDefault.IsChecked = false;
            if (Path.GetExtension(TextBoxSubtitleTrackFour.Text) == ".sup")
            {
                MessageBoxUnsupportedSubtitleBurnIn();
                CheckBoxSubFourBurn.IsChecked = false;
            }
        }

        private void CheckBoxSubFiveBurn_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxBurnToggle(false, false, false, false, true);
            CheckBoxSubFiveDefault.IsChecked = false;
            if (Path.GetExtension(TextBoxSubtitleTrackFive.Text) == ".sup")
            {
                MessageBoxUnsupportedSubtitleBurnIn();
                CheckBoxSubFiveBurn.IsChecked = false;
            }
        }

        private void CheckBoxDefaultToggle(bool one, bool two, bool three, bool four, bool five)
        {
            CheckBoxSubOneDefault.IsChecked = one;
            CheckBoxSubTwoDefault.IsChecked = two;
            CheckBoxSubThreeDefault.IsChecked = three;
            CheckBoxSubFourDefault.IsChecked = four;
            CheckBoxSubFiveDefault.IsChecked = five;
        }

        private void CheckBoxSubOneDefault_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxDefaultToggle(true, false, false, false, false);
            CheckBoxSubOneBurn.IsChecked = false;
        }

        private void CheckBoxSubTwoDefault_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxDefaultToggle(false, true, false, false, false);
            CheckBoxSubTwoBurn.IsChecked = false;
        }

        private void CheckBoxSubThreeDefault_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxDefaultToggle(false, false, true, false, false);
            CheckBoxSubThreeBurn.IsChecked = false;
        }

        private void CheckBoxSubFourDefault_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxDefaultToggle(false, false, false, true, false);
            CheckBoxSubFourBurn.IsChecked = false;
        }

        private void CheckBoxSubFiveDefault_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxDefaultToggle(false, false, false, false, true);
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
                if (ComboBoxVideoEncoder.SelectedIndex == 1)
                {
                    // rav1e
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
            if (CheckBoxVideoAomencRealTime != null && ComboBoxVideoEncoder != null)
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
            if (MessageBox.Show("Are you sure you want to delete the selected preset?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
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

        private void ButtonGithub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Alkl58/NotEnoughAV1Encodes");
        }

        private void ButtonPayPal_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://paypal.me/alkl58");
        }

        private void ButtonDiscord_Click(object sender, RoutedEventArgs e)
        {
            // NEAV1E Discord
            Process.Start("https://discord.gg/yG27ArHBFe");
        }

        private void ComboBoxPresets_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (ComboBoxPresets.SelectedItem != null)
                {
                    // Temporary saves the state of toggleswitches
                    bool audio1 = ToggleSwitchAudioTrackOne.IsEnabled == true;
                    bool audio2 = ToggleSwitchAudioTrackTwo.IsEnabled == true;
                    bool audio3 = ToggleSwitchAudioTrackThree.IsEnabled == true;
                    bool audio4 = ToggleSwitchAudioTrackFour.IsEnabled == true;
                    // Loads the selected preset file
                    LoadSettings(true, ComboBoxPresets.SelectedItem.ToString());
                    // Reloads the saved states of the toogleswitches
                    if (!audio1) { ToggleSwitchAudioTrackOne.IsEnabled = false; ToggleSwitchAudioTrackOne.IsOn = false; }
                    if (!audio2) { ToggleSwitchAudioTrackTwo.IsEnabled = false; ToggleSwitchAudioTrackTwo.IsOn = false; }
                    if (!audio3) { ToggleSwitchAudioTrackThree.IsEnabled = false; ToggleSwitchAudioTrackThree.IsOn = false; }
                    if (!audio4) { ToggleSwitchAudioTrackFour.IsEnabled = false; ToggleSwitchAudioTrackFour.IsOn = false; }
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
                    if (AomencPath == null)
                    {
                        if (MessageBox.Show("Could not find aomenc!\nOpen Updater?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            Updater updater = new Updater(ComboBoxBaseTheme.Text, ComboBoxAccentTheme.Text);
                            updater.ShowDialog();
                            CheckDependencies.Check();
                        }
                    }
                }
                else if (ComboBoxVideoEncoder.SelectedIndex == 1)
                {
                    // rav1e
                    SliderVideoSpeed.Maximum = 10;
                    SliderVideoSpeed.Value = 6;
                    SliderVideoQuality.Maximum = 255;
                    SliderVideoQuality.Value = 100;
                    // rav1e can only do 1pass atm
                    ComboBoxVideoPasses.SelectedIndex = 0;
                    if (Rav1ePath == null)
                    {
                        if (MessageBox.Show("Could not find rav1e!\nOpen Updater?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            Updater updater = new Updater(ComboBoxBaseTheme.Text, ComboBoxAccentTheme.Text);
                            updater.ShowDialog();
                            CheckDependencies.Check();
                        }
                    }
                }
                else if (ComboBoxVideoEncoder.SelectedIndex == 2)
                {
                    // svt-av1
                    SliderVideoSpeed.Maximum = 8;
                    SliderVideoSpeed.Value = 8;
                    SliderVideoQuality.Value = 50;
                    SliderVideoQuality.Maximum = 63;
                    ComboBoxWorkerCount.SelectedIndex = 0;
                    if (SvtAV1Path == null)
                    {
                        if (MessageBox.Show("Could not find SVT-AV1!\nOpen Updater?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            Updater updater = new Updater(ComboBoxBaseTheme.Text, ComboBoxAccentTheme.Text);
                            updater.ShowDialog();
                            CheckDependencies.Check();
                        }
                    }
                }else if (ComboBoxVideoEncoder.SelectedIndex == 3)
                {
                    // vp9
                    SliderVideoSpeed.Maximum = 9;
                    SliderVideoSpeed.Value = 4;
                    SliderVideoQuality.Value = 30;
                    SliderVideoQuality.Maximum = 63;
                    if (VPXPath == null)
                    {
                        if (MessageBox.Show("Could not find vpxenc!\nOpen Updater?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            Updater updater = new Updater(ComboBoxBaseTheme.Text, ComboBoxAccentTheme.Text);
                            updater.ShowDialog();
                            CheckDependencies.Check();
                        }
                    }
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
                if (ComboBoxVideoEncoder.SelectedIndex == 3) { TextBoxCustomVideoSettings.Text = SetVP9Command(); }
            }
        }

        private void CheckBoxSettingsUISounds_Checked(object sender, RoutedEventArgs e)
        {
            PlayUISounds = ToggleSwitchUISounds.IsOn == true;
            SaveSettingsTab();
        }

        private void ToggleSwitchTempFolder_Toggled(object sender, RoutedEventArgs e)
        {
            SaveSettingsTab();
        }

        private void ToggleSwitchShutdownAfterEncode_Toggled(object sender, RoutedEventArgs e)
        {
            SaveSettingsTab();
        }

        private void CheckBoxSettingsDeleteTempFiles_Checked(object sender, RoutedEventArgs e)
        {
            SaveSettingsTab();
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

        // ════════════════════════════════════ Video Trimming ════════════════════════════════════
        private void ButtonTrimPlus_Click(object sender, RoutedEventArgs e)
        {
            TextBoxTrimStart.Text = DateTime.ParseExact(TextBoxTrimStart.Text, "HH:mm:ss.fff", CultureInfo.InvariantCulture).AddSeconds(1).ToString("HH:mm:ss.fff");
        }

        private void ButtonTrimMinus_Click(object sender, RoutedEventArgs e)
        {
            if (DateTime.ParseExact(TextBoxTrimStart.Text, "HH:mm:ss.fff", CultureInfo.InvariantCulture) > DateTime.ParseExact("00:00:00.999", "HH:mm:ss.fff", CultureInfo.InvariantCulture))
            {
                TextBoxTrimStart.Text = DateTime.ParseExact(TextBoxTrimStart.Text, "HH:mm:ss.fff", CultureInfo.InvariantCulture).AddSeconds(-1).ToString("HH:mm:ss.fff");
            }
            else { TextBoxTrimStart.Text = "00:00:00.000"; }
        }

        private void ButtonTrimPlusEnd_Click(object sender, RoutedEventArgs e)
        {
            if (DateTime.ParseExact(TextBoxTrimEnd.Text, "HH:mm:ss.fff", CultureInfo.InvariantCulture) < DateTime.ParseExact(TrimEndTemp, "HH:mm:ss.fff", CultureInfo.InvariantCulture))
            {
                TextBoxTrimEnd.Text = DateTime.ParseExact(TextBoxTrimEnd.Text, "HH:mm:ss.fff", CultureInfo.InvariantCulture).AddSeconds(1).ToString("HH:mm:ss.fff");
            }
            else { TextBoxTrimEnd.Text = TrimEndTemp; }
        }

        private void ButtonTrimMinusEnd_Click(object sender, RoutedEventArgs e)
        {
            TextBoxTrimEnd.Text = DateTime.ParseExact(TextBoxTrimEnd.Text, "HH:mm:ss.fff", CultureInfo.InvariantCulture).AddSeconds(-1).ToString("HH:mm:ss.fff");
        }

        private void TextBoxTrimEnd_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Main Entry for Changing the image preview
            if (TextBoxTrimEnd != null && ReadTimeCode && ToggleSwitchTrimming.IsOn == true) { setVideoLengthTrimmed(); }
        }

        private void setVideoLengthTrimmed()
        {
            if (StartUp == false)
            {
                try
                {
                    DateTime start = DateTime.ParseExact(TextBoxTrimStart.Text, "hh:mm:ss.fff", CultureInfo.InvariantCulture);
                    DateTime end = DateTime.ParseExact(TextBoxTrimEnd.Text, "hh:mm:ss.fff", CultureInfo.InvariantCulture);
                    if (start < end)
                    {
                        TextBoxTrimEnd.BorderBrush = new SolidColorBrush(Color.FromRgb(171, 173, 179));
                        TextBoxTrimStart.BorderBrush = new SolidColorBrush(Color.FromRgb(171, 173, 179));
                        TimeSpan result = end - start;
                        if (ToggleSwitchTempFolder != null && VideoInputSet) { setImagePreview(); }
                    }
                    else
                    {
                        TextBoxTrimEnd.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                        TextBoxTrimStart.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                    }
                }
                catch { }
            }
        }

        private void setImagePreview()
        {
            string tempPath = Path.Combine("NEAV1E", TempPathFileName);
            if (ToggleSwitchTempFolder.IsOn == true) { tempPath = Path.Combine(TextBoxCustomTempPath.Text, tempPath); }
            else { tempPath = Path.Combine(Path.GetTempPath(), tempPath); }

            Process getStartFrame = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    WorkingDirectory = FFmpegPath,
                    Arguments = "/C ffmpeg.exe -y -ss " + TextBoxTrimStart.Text + " -loglevel error -i " + '\u0022' + VideoInput + '\u0022' + " -vframes 1 -vf scale=\"min(240\\, iw):-1" + '\u0022' + " -sws_flags neighbor -threads 4 " + '\u0022' + Path.Combine(tempPath, "start.jpg") + '\u0022',
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            getStartFrame.Start();
            getStartFrame.WaitForExit();

            var uriSource = new Uri(Path.Combine(tempPath, "start.jpg"));
            BitmapImage imgTemp = new BitmapImage();
            imgTemp.BeginInit();
            imgTemp.CacheOption = BitmapCacheOption.OnLoad;
            imgTemp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            imgTemp.UriSource = uriSource;
            imgTemp.EndInit();
            ImagePreviewTrimStart.Source = imgTemp;

            if (TrimEndTemp != TextBoxTrimEnd.Text)
            {
                Process getEndFrame = new Process
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = "cmd.exe",
                        WorkingDirectory = FFmpegPath,
                        Arguments = "/C ffmpeg.exe -y -ss " + TextBoxTrimEnd.Text + " -loglevel error -i " + '\u0022' + VideoInput + '\u0022' + " -vframes 1 -vf scale=\"min(240\\, iw):-1" + '\u0022' + " -sws_flags neighbor -threads 4 " + '\u0022' + Path.Combine(tempPath, "end.jpg") + '\u0022',
                        RedirectStandardError = true,
                        RedirectStandardOutput = true
                    }
                };
                getEndFrame.Start();
                getEndFrame.WaitForExit();
                var uriSourceEnd = new Uri(Path.Combine(tempPath, "end.jpg"));
                BitmapImage imgTempEnd = new BitmapImage();
                imgTempEnd.BeginInit();
                imgTempEnd.CacheOption = BitmapCacheOption.OnLoad;
                imgTempEnd.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                imgTempEnd.UriSource = uriSourceEnd;
                imgTempEnd.EndInit();
                ImagePreviewTrimEnd.Source = imgTempEnd;
            }
        }

        private void CheckBoxTrimming_Checked(object sender, RoutedEventArgs e)
        {
            if (TextBoxTrimEnd != null && ReadTimeCode && ToggleSwitchTrimming.IsOn == true) { setVideoLengthTrimmed(); }
            ToggleSwitchSubtitleActivatedOne.IsOn = false;
            ToggleSwitchSubtitleActivatedTwo.IsOn = false;
            ToggleSwitchSubtitleActivatedThree.IsOn = false;
            ToggleSwitchSubtitleActivatedFour.IsOn = false;
            ToggleSwitchSubtitleActivatedFive.IsOn = false;
        }

        private void CheckBoxTrimming_Unchecked(object sender, RoutedEventArgs e)
        {
            BitmapImage image = new BitmapImage(new Uri("/NotEnoughAV1Encodes;component/img/offline.png", UriKind.Relative));
            ImagePreviewTrimStart.Source = image;
            ImagePreviewTrimEnd.Source = image;
        }

        // ══════════════════════════════════════ Main Logic ══════════════════════════════════════

        private async void PreStart()
        {
            // This Function is needed to be able to cancel everything later
            // Button Click is not async and thus can't await MainEntry
            // Thats why we have this function "inbetween"

            // Sets the Temp Path
            if (ToggleSwitchTempFolder.IsOn == true)
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
            // Sets if popup window appears
            PopupWindow = ToggleSwitchShowWindow.IsOn == true;
            // Sets that the encode has started
            EncodeStarted = true;
            // Sets the encoder (0 aomenc; 1 rav1e; 2 svt-av1; 3 vp9)
            EncodeMethod = ComboBoxVideoEncoder.SelectedIndex;
            // Sets the Split Method
            SplitMethod = ComboBoxSplittingMethod.SelectedIndex;
            // Sets if Video is VFR
            VFRVideo = ToggleSwitchVFR.IsOn == true;
            // Sets Trim Params
            TrimEnabled = ToggleSwitchTrimming.IsOn == true;
            if (TrimEnabled)
            {
                TrimCommand = "-ss " + TextBoxTrimStart.Text + " -to " + TextBoxTrimEnd.Text;
            }
            else
            {
                TrimCommand = "";
            }

            // Starts the Main Function
            if (BatchEncoding == false)
            {
                await MainEntry(cancellationTokenSource.Token);
            }
            else
            {
                // Set the output container for batch encoding example: .mkv
                BatchOutContainer = ComboBoxContainerBatchEncoding.Text;
                // Batch Encoding
                BatchEncode(cancellationTokenSource.Token);
            }
            
        }

        private async void BatchEncode(CancellationToken token)
        {
            // Gets all files in folder
            DirectoryInfo batchfiles = new DirectoryInfo(VideoInput);
            // Loops over all files in folder
            foreach (var file in batchfiles.GetFiles())
            {
                if (SmallFunctions.CheckFileType(file.ToString()) == true && SmallFunctions.Cancel.CancelAll == false)
                {
                    SmallFunctions.Logging("Batch Encoding: " + file);
                    // Reset Progressbar
                    ProgressBar.Maximum = 100;
                    ProgressBar.Value = 0;
                    EncodeStarted = true;
                    // Sets Input / Output
                    VideoInput = TextBoxVideoSource.Text + "\\" + file;
                    VideoOutput = TextBoxVideoDestination.Text + "\\" + file + "_av1" + BatchOutContainer;
                    // Sets Temp Filename for temp folder
                    TempPathFileName = Path.GetFileNameWithoutExtension(VideoInput);
                    // Get Source Information
                    GetAudioInformation();
                    // Reset Subtitle
                    GetSubtitleTracks();
                    // Don't want to burn in subtitles in Batch Encoding
                    CheckBoxSubOneBurn.IsChecked = false;
                    CheckBoxSubTwoBurn.IsChecked = false;
                    CheckBoxSubThreeBurn.IsChecked = false;
                    CheckBoxSubFourBurn.IsChecked = false;

                    // Set Bit-Depth and Color Format
                    AutoSetBitDepthAndColorFormat(VideoInput);

                    // Start encoding process
                    await MainEntry(cancellationTokenSource.Token);

                    SmallFunctions.Logging("Batch Encoding Finished: " + file);
                }
            }
            SmallFunctions.PlayFinishedSound();
            ButtonStartEncode.BorderBrush = new SolidColorBrush(Color.FromRgb(112, 112, 112));
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
                // Extracts VFR Timestamps
                ExtractVFRTimeStamps();
                // Saves the Project as file
                SaveSettings(false, TempPathFileName);

                if (subHardSubEnabled || TrimEnabled)
                {
                    // Reencodes the Video
                    await Task.Run(() => ReEncode());
                }

                // Split Video / Scene Detection
                SetSplitSettings();
                await Task.Run(() => { token.ThrowIfCancellationRequested(); StartSplittingDetect(); }, token);
                // Set other temporary settings
                SetTempSettings();
                // Get Source Framecount
                await Task.Run(() => { token.ThrowIfCancellationRequested(); SmallFunctions.GetSourceFrameCount(); }, token);
                if (trackOne || trackTwo || trackThree || trackFour)
                {
                    LabelProgressBar.Content = "Encoding Audio...";
                    await Task.Run(() => { token.ThrowIfCancellationRequested(); EncodeAudio.Encode(); }, token);
                    if (!File.Exists(Path.Combine(TempPath, TempPathFileName, "Audio", "audio.mkv")))
                    {
                        // This disables audio if audio encoding failed, thus still managing to output a video in the muxing process without audio
                        trackOne = false; trackTwo = false; trackThree = false; trackFour = false;
                        SmallFunctions.Logging("Attention: Tried to encode audio. Not audio output detected. Audio is now disabled.");
                    }
                }

                if (subHardSubEnabled || TrimEnabled)
                {                    
                    // Sets the new video input
                    VideoInput = Path.Combine(TempPath, TempPathFileName, "tmpsub.mkv");
                }

                ProgressBar.Foreground = new SolidColorBrush(Color.FromRgb(3, 112, 200));
                if (OnePass == true)
                {
                    ProgressBar.Dispatcher.Invoke(() => ProgressBar.Maximum = TotalFrames);
                }
                else
                {
                    ProgressBar.Dispatcher.Invoke(() => ProgressBar.Maximum = TotalFrames * 2);
                }

                if (Yadif1)
                    ProgressBar.Dispatcher.Invoke(() => ProgressBar.Maximum = ProgressBar.Maximum * 2);

                await Task.Run(() => { token.ThrowIfCancellationRequested(); EncodeVideo(); }, token);
                await Task.Run(async () => { token.ThrowIfCancellationRequested(); await VideoMuxing.Concat(); }, token);
                SmallFunctions.CheckVideoOutput();

                // Progressbar Label when encoding finished
                TimeSpan timespent = DateTime.Now - StartTime;
                LabelProgressBar.Content = "Finished Encoding - Elapsed Time " + timespent.ToString("hh\\:mm\\:ss") + " - avg " + Math.Round(TotalFrames / timespent.TotalSeconds, 2) + "fps";
                SmallFunctions.Logging(LabelProgressBar.Content.ToString());
                ProgressBar.Foreground = new SolidColorBrush(Color.FromRgb(6, 176, 37));
                ProgressBar.Value = 0;
                ProgressBar.Maximum = 10;
                ProgressBar.Value = 10;
                // Plays a sound if encoding has finished
                if (BatchEncoding == false)
                {
                    SmallFunctions.PlayFinishedSound();
                    ButtonStartEncode.BorderBrush = new SolidColorBrush(Color.FromRgb(112, 112, 112));
                    if (PopupWindow)
                    {
                        PopupWindow popupWindow = new PopupWindow(ComboBoxBaseTheme.Text, ComboBoxAccentTheme.Text, timespent.ToString("hh\\:mm\\:ss"), TotalFrames.ToString(), Math.Round(TotalFrames / timespent.TotalSeconds, 2).ToString(), VideoOutput);
                        popupWindow.ShowDialog();
                    }
                }
                if (ToggleSwitchShutdownAfterEncode.IsOn == true && BatchEncoding == false) { Process.Start("shutdown.exe", "/s /t 0"); }
            }
            catch { SmallFunctions.PlayStopSound(); }
            EncodeStarted = false;
        }

        private void SetSplitSettings()
        {
            // Temp Arguments for Splitting / Scenedetection
            SplitReencode = CheckBoxSplittingReencode.IsChecked == true;
            ReEncodeMethodSplitting = ComboBoxSplittingReencodeMethod.SelectedIndex;
            FFmpegThreshold = TextBoxSplittingThreshold.Text;
            ChunkLength = TextBoxSplittingChunkLength.Text;
            HardSubCMD = TrimCommand + subHardCommand;
        }

        private void ReEncode()
        {
            // It skips reencoding if chunking method is being used
            if (EncodeMethod != 2)
            {
                // Skips reencoding if the file already exists
                if (File.Exists(Path.Combine(TempPath, TempPathFileName, "tmpsub.mkv")) == false)
                {
                    ProgressBar.Dispatcher.Invoke(() => LabelProgressBar.Content = "Reencoding Video for Hardsubbing...");
                    string ffmpegCommand = "/C ffmpeg.exe -i " + '\u0022' + VideoInput + '\u0022' + " " + TrimCommand + " " + subHardCommand + " -map_metadata -1 -c:v libx264 -crf 0 -preset veryfast -an " + '\u0022' + Path.Combine(TempPath, TempPathFileName, "tmpsub.mkv") + '\u0022';
                    SmallFunctions.Logging("Subtitle Hardcoding Command: " + ffmpegCommand);
                    // Reencodes the Video
                    SmallFunctions.ExecuteFfmpegTask(ffmpegCommand);
                }
                else if (MessageBox.Show("The temp reencode seems to already exists!\nSkip reencoding?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    ProgressBar.Dispatcher.Invoke(() => LabelProgressBar.Content = "Reencoding Video for Hardsubbing...");
                    string ffmpegCommand = "/C ffmpeg.exe -i " + '\u0022' + VideoInput + '\u0022' + " " + TrimCommand + " " + subHardCommand + " -map_metadata -1 -c:v libx264 -crf 0 -preset veryfast -an " + '\u0022' + Path.Combine(TempPath, TempPathFileName, "tmpsub.mkv") + '\u0022';
                    SmallFunctions.Logging("Subtitle Hardcoding Command: " + ffmpegCommand);
                    // Reencodes the Video
                    SmallFunctions.ExecuteFfmpegTask(ffmpegCommand);
                }
            }

        }

        private void ExtractVFRTimeStamps()
        {
            if (VFRVideo)
            {
                // Skips extracting if file already exists
                if (File.Exists(Path.Combine(TempPath, TempPathFileName, "vsync.txt")) == false)
                {
                    // Run mkvextract command
                    Process mkvToolNix = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = "cmd.exe",
                        WorkingDirectory = MKVToolNixPath,
                        Arguments = "/C mkvextract.exe " + '\u0022' + VideoInput + '\u0022' + " timestamps_v2 0:" + '\u0022' + Path.Combine(TempPath, TempPathFileName, "vsync.txt") + '\u0022'
                    };
                    SmallFunctions.Logging("VSYNC Extract: " + startInfo.Arguments);
                    mkvToolNix.StartInfo = startInfo;
                    mkvToolNix.Start();
                    mkvToolNix.WaitForExit();
                }
                // Discards piping timestamp 
                VSYNC = "-vsync drop";
                VFRCMD = "--timestamps 0:" + '\u0022' + Path.Combine(TempPath, TempPathFileName, "vsync.txt") + '\u0022';
            }
            else
            {
                // Reset to default VSYNC piping
                VSYNC = "-vsync 0";
                VFRCMD = "";
            }
        }

        // ════════════════════════════════════ Temp Settings ═════════════════════════════════════

        private void SetTempSettings()
        {
            WorkerCount = int.Parse(ComboBoxWorkerCount.Text);                      // Sets the worker count
            OnePass = ComboBoxVideoPasses.SelectedIndex == 0;                       // Sets the amount of passes (true = 1, false = 2)
            Priority = ComboBoxProcessPriority.SelectedIndex == 0;                  // Sets the Process Priority
            DeleteTempFiles = ToggleSwitchDeleteTempFiles.IsOn == true;             // Sets if Temp Files should be deleted
            ShowTerminal = ToggleSwitchHideTerminal.IsOn == false;                  // Sets if Terminal shall be shown during encode
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
                if (ComboBoxAomencColorFormat.SelectedIndex == 0) { PipeBitDepthCommand += "420p"; }
                else if (ComboBoxAomencColorFormat.SelectedIndex == 1) { PipeBitDepthCommand += "422p"; }
                else if (ComboBoxAomencColorFormat.SelectedIndex == 2) { PipeBitDepthCommand += "444p"; }
            }

            if (ComboBoxVideoEncoder.SelectedIndex == 1)
            {
                // rav1e
                if (ComboBoxRav1eColorFormat.SelectedIndex == 0) { PipeBitDepthCommand += "420p"; }
                else if (ComboBoxRav1eColorFormat.SelectedIndex == 1) { PipeBitDepthCommand += "422p"; }
                else if (ComboBoxRav1eColorFormat.SelectedIndex == 2) { PipeBitDepthCommand += "444p"; }
            }

            if (ComboBoxVideoEncoder.SelectedIndex == 2)
            {
                // svt-av1
                if (ComboBoxSVTAV1ColorFormat.SelectedIndex == 0) { PipeBitDepthCommand += "420p"; }
                else if (ComboBoxSVTAV1ColorFormat.SelectedIndex == 1) { PipeBitDepthCommand += "422p"; }
                else if (ComboBoxSVTAV1ColorFormat.SelectedIndex == 2) { PipeBitDepthCommand += "444p"; }
            }

            if (ComboBoxVideoEncoder.SelectedIndex == 3)
            {
                // vp9
                if (ComboBoxVP9ColorFormat.SelectedIndex == 0) { PipeBitDepthCommand += "420p"; }
                else if (ComboBoxVP9ColorFormat.SelectedIndex == 1) { PipeBitDepthCommand += "422p"; }
                else if (ComboBoxVP9ColorFormat.SelectedIndex == 2) { PipeBitDepthCommand += "444p"; }
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
            trackOne = ToggleSwitchAudioTrackOne.IsOn == true;
            trackTwo = ToggleSwitchAudioTrackTwo.IsOn == true;
            trackThree = ToggleSwitchAudioTrackThree.IsOn == true;
            trackFour = ToggleSwitchAudioTrackFour.IsOn == true;
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
            bool crop = ToggleSwitchFilterCrop.IsOn == true;
            bool rotate = ToggleSwitchFilterRotate.IsOn == true;
            bool resize = ToggleSwitchFilterResize.IsOn == true;
            bool deinterlace = ToggleSwitchFilterDeinterlace.IsOn == true;
            Yadif1 = false;
            int tempCounter = 0;

            if (crop || rotate || resize || deinterlace )
            {
                FilterCommand = " -vf ";
                if (crop)
                {
                    FilterCommand += VideoFiltersCrop();
                    tempCounter += 1;
                }
                if (rotate)
                {
                    if (tempCounter != 0) { FilterCommand += ","; }
                    FilterCommand += VideoFiltersRotate();
                    tempCounter += 1;
                }
                if (deinterlace)
                {
                    if (tempCounter != 0) { FilterCommand += ","; }
                    FilterCommand += VideoFiltersDeinterlace();
                    tempCounter += 1;
                }
                if (resize)
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
            Yadif1 = ComboBoxFiltersDeinterlace.Text == "1";
            return "yadif=" + ComboBoxFiltersDeinterlace.Text;
        }

        private string VideoFiltersResize()
        {
            // Sets the values for scaling the video
            if (TextBoxFiltersResizeWidth.Text != "0")
            {
                // Custom Scale
                return "scale=" + TextBoxFiltersResizeWidth.Text + ":" + TextBoxFiltersResizeHeight.Text + " -sws_flags " + ComboBoxFiltersScaling.Text;
            }
            else
            {
                // Auto Scale
                return "scale=trunc(oh*a/2)*2:" + TextBoxFiltersResizeHeight.Text + " -sws_flags " + ComboBoxFiltersScaling.Text;
            }
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
                Arguments = "/C ffprobe.exe -i " + '\u0022' + VideoInput + '\u0022' + " -loglevel error -select_streams a -show_entries stream=index -of csv=p=1",
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
            if (trackone) { ToggleSwitchAudioTrackOne.IsEnabled = true; ToggleSwitchAudioTrackOne.IsOn = true; }
            else { ToggleSwitchAudioTrackOne.IsEnabled = false; ToggleSwitchAudioTrackOne.IsOn = false; }
            if (tracktwo) { ToggleSwitchAudioTrackTwo.IsEnabled = true; ToggleSwitchAudioTrackTwo.IsOn = true; }
            else { ToggleSwitchAudioTrackTwo.IsEnabled = false; ToggleSwitchAudioTrackTwo.IsOn = false; }
            if (trackthree) { ToggleSwitchAudioTrackThree.IsEnabled = true; ToggleSwitchAudioTrackThree.IsOn = true; }
            else { ToggleSwitchAudioTrackThree.IsEnabled = false; ToggleSwitchAudioTrackThree.IsOn = false; }
            if (trackfour) { ToggleSwitchAudioTrackFour.IsEnabled = true; ToggleSwitchAudioTrackFour.IsOn = true; }
            else { ToggleSwitchAudioTrackFour.IsEnabled = false; ToggleSwitchAudioTrackFour.IsOn = false; }
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

        private void ResetSubtitles()
        {
            // Clears Temp Subtitles information from GUI
            ToggleSwitchSubtitleActivatedOne.IsOn = false;
            ToggleSwitchSubtitleActivatedTwo.IsOn = false;
            ToggleSwitchSubtitleActivatedThree.IsOn = false;
            ToggleSwitchSubtitleActivatedFour.IsOn = false;
            ToggleSwitchSubtitleActivatedFive.IsOn = false;
            TextBoxSubtitleTrackOne.Text = "";
            TextBoxSubtitleTrackTwo.Text = "";
            TextBoxSubtitleTrackThree.Text = "";
            TextBoxSubtitleTrackFour.Text = "";
            TextBoxSubtitleTrackFive.Text = "";
            ComboBoxSubTrackOneLanguage.SelectedIndex = 0;
            ComboBoxSubTrackTwoLanguage.SelectedIndex = 0;
            ComboBoxSubTrackThreeLanguage.SelectedIndex = 0;
            ComboBoxSubTrackFourLanguage.SelectedIndex = 0;
            ComboBoxSubTrackFiveLanguage.SelectedIndex = 0;
        }

        private void SetSubtitleParameters()
        {
            // Has to be set, else it could create problems when running another encode in the same instance
            subSoftSubEnabled = false;
            subHardSubEnabled = false;
            subCommand = "";

            if (ToggleSwitchSubtitleActivatedOne.IsOn == true)
            {
                // 1st Subtitle
                if (CheckBoxSubOneBurn.IsChecked == false)
                {
                    // Softsub
                    subSoftSubEnabled = true;
                    subCommand += SoftSubCMDGenerator(ComboBoxSubTrackOneLanguage.Text, TextBoxSubOneName.Text, TextBoxSubtitleTrackOne.Text, CheckBoxSubOneDefault.IsChecked == true);
                }
                else { HardSubCMDGenerator(TextBoxSubtitleTrackOne.Text); }
            }

            if (ToggleSwitchSubtitleActivatedTwo.IsOn == true && CheckBoxSubTwoBurn.IsChecked != true)
            {
                // 2nd Subtitle
                if (CheckBoxSubTwoBurn.IsChecked == false)
                {
                    // Softsub
                    subSoftSubEnabled = true;
                    subCommand += SoftSubCMDGenerator(ComboBoxSubTrackTwoLanguage.Text, TextBoxSubTwoName.Text, TextBoxSubtitleTrackTwo.Text, CheckBoxSubTwoDefault.IsChecked == true);
                }
                else { HardSubCMDGenerator(TextBoxSubtitleTrackTwo.Text); }
            }

            if (ToggleSwitchSubtitleActivatedThree.IsOn == true && CheckBoxSubThreeBurn.IsChecked != true)
            {
                // 3rd Subtitle
                if (CheckBoxSubThreeBurn.IsChecked == false)
                {
                    // Softsub
                    subSoftSubEnabled = true;
                    subCommand += SoftSubCMDGenerator(ComboBoxSubTrackThreeLanguage.Text, TextBoxSubThreeName.Text, TextBoxSubtitleTrackThree.Text, CheckBoxSubThreeDefault.IsChecked == true);
                }
                else { HardSubCMDGenerator(TextBoxSubtitleTrackThree.Text); }
            }

            if (ToggleSwitchSubtitleActivatedFour.IsOn == true && CheckBoxSubFourBurn.IsChecked != true)
            {
                // 4th Subtitle
                if (CheckBoxSubFourBurn.IsChecked == false)
                {
                    // Softsub
                    subSoftSubEnabled = true;
                    subCommand += SoftSubCMDGenerator(ComboBoxSubTrackFourLanguage.Text, TextBoxSubFourName.Text, TextBoxSubtitleTrackFour.Text, CheckBoxSubFourDefault.IsChecked == true);
                }
                else { HardSubCMDGenerator(TextBoxSubtitleTrackFour.Text); }
                
            }

            if (ToggleSwitchSubtitleActivatedFive.IsOn == true && CheckBoxSubFiveBurn.IsChecked != true)
            {
                // 5th Subtitle
                if (CheckBoxSubFiveBurn.IsChecked == false)
                {
                    // Softsub
                    subSoftSubEnabled = true;
                    subCommand += SoftSubCMDGenerator(ComboBoxSubTrackFiveLanguage.Text, TextBoxSubFiveName.Text, TextBoxSubtitleTrackFive.Text, CheckBoxSubFiveDefault.IsChecked == true);
                }
                else { HardSubCMDGenerator(TextBoxSubtitleTrackFive.Text); }
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
            subHardSubEnabled = true;
        }

        public void GetSubtitleTracks()
        {
            //Creates Audio Directory in the temp dir
            if (!Directory.Exists(Path.Combine(TempPath, TempPathFileName, "Subtitles")))
                Directory.CreateDirectory(Path.Combine(TempPath, TempPathFileName, "Subtitles"));

            ResetSubtitles();

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
                    if (Path.GetExtension(VideoOutput) != ".mp4")
                    {
                        if (b == 0) { ToggleSwitchSubtitleActivatedOne.IsOn = true; }
                        if (b == 1) { ToggleSwitchSubtitleActivatedTwo.IsOn = true; }
                        if (b == 2) { ToggleSwitchSubtitleActivatedThree.IsOn = true; }
                        if (b == 3) { ToggleSwitchSubtitleActivatedFour.IsOn = true; }
                        if (b == 4) { ToggleSwitchSubtitleActivatedFive.IsOn = true; }
                    }
                    if (b == 0) { TextBoxSubtitleTrackOne.Text = tempName; ComboBoxSubTrackOneLanguage.SelectedIndex = indexLang; }
                    if (b == 1) { TextBoxSubtitleTrackTwo.Text = tempName; ComboBoxSubTrackTwoLanguage.SelectedIndex = indexLang; }
                    if (b == 2) { TextBoxSubtitleTrackThree.Text = tempName; ComboBoxSubTrackThreeLanguage.SelectedIndex = indexLang; }
                    if (b == 3) { TextBoxSubtitleTrackFour.Text = tempName; ComboBoxSubTrackFourLanguage.SelectedIndex = indexLang; }
                    if (b == 4) { TextBoxSubtitleTrackFive.Text = tempName; ComboBoxSubTrackFiveLanguage.SelectedIndex = indexLang; }
                    b++;
                }
                a++;
            }
            if (VideoOutputSet && Path.GetExtension(VideoOutput) == ".mp4")
            {
                ToggleSwitchSubtitleActivatedOne.IsOn = false;
                ToggleSwitchSubtitleActivatedTwo.IsOn = false;
                ToggleSwitchSubtitleActivatedThree.IsOn = false;
                ToggleSwitchSubtitleActivatedFour.IsOn = false;
                ToggleSwitchSubtitleActivatedFive.IsOn = false;
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
                if (ComboBoxVideoEncoder.SelectedIndex == 3)
                {
                    EncoderVP9Command = SetVP9Command();
                    SmallFunctions.Logging("VP9 Settings : " + EncoderVP9Command);
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
                if (ComboBoxVideoEncoder.SelectedIndex == 3)
                {
                    EncoderVP9Command = " " + TextBoxCustomVideoSettings.Text;
                    SmallFunctions.Logging("VP9 Custom Settings : " + EncoderVP9Command);
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

            if (ToggleSwitchAdvancedVideoSettings.IsOn == false)
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
                cmd += " --tune-content=" + ComboBoxAomencTuneContent.Text;                             // Tune-Content
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

            if (ToggleSwitchAdvancedVideoSettings.IsOn == false)
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

            if (ToggleSwitchAdvancedVideoSettings.IsOn == true)
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

        private string SetVP9Command()
        {
            string cmd = "";
            cmd += " --bit-depth=" + ComboBoxVideoBitDepth.Text;    // Bit-Depth

            if (ComboBoxVP9ColorFormat.SelectedIndex == 0)
            {
                // yuv420p
                if (ComboBoxVideoBitDepth.SelectedIndex == 1 || ComboBoxVideoBitDepth.SelectedIndex == 2)
                {
                    // profile=2: 10bit / 12bit
                    cmd += " --profile=2";
                }
            }
            else
            {
                // yuv420p / yuv422p / yuv444p
                if (ComboBoxVideoBitDepth.SelectedIndex == 1 || ComboBoxVideoBitDepth.SelectedIndex == 2)
                {
                    // profile=3: 10bit / 12bit
                    cmd += " --profile=3";
                }
            }

            cmd += " --cpu-used=" + SliderVideoSpeed.Value;         // Speed

            // Constant Quality or Target Bitrate
            if (RadioButtonVideoConstantQuality.IsChecked == true) { cmd += " --end-usage=q --cq-level=" + SliderVideoQuality.Value; }
            else if (RadioButtonVideoBitrate.IsChecked == true) { cmd += " --end-usage=vbr --target-bitrate=" + TextBoxVideoBitrate.Text; }

            if (ToggleSwitchAdvancedVideoSettings.IsOn == false)
            {
                // Default params when User don't select advanced settings
                cmd += " --threads=4 --tile-columns=2 --tile-rows=1";
            }
            else
            {
                cmd += " --" + ComboBoxVP9ColorFormat.Text;                         // Color Format
                cmd += " --threads=" + ComboBoxVP9Threads.Text;                     // Max Threads
                cmd += " --tile-columns=" + ComboBoxVP9TileColumns.SelectedIndex;   // Tile Columns
                cmd += " --tile-rows=" + ComboBoxVP9TileRows.SelectedIndex;         // Tile Rows
                cmd += " --lag-in-frames=" + TextBoxVP9LagInFrames.Text;            // Lag in Frames
                cmd += " --kf-max-dist=" + TextBoxVP9MaxKF.Text;                    // Max GOP
                cmd += " --aq-mode=" + ComboBoxVP9AQMode.SelectedIndex;             // AQ-Mode
                cmd += " --tune=" + ComboBoxVP9ATune.Text;                          // Tune
                cmd += " --tune-content=" + ComboBoxVP9ATuneContent.Text;           // Tune-Content
                if (ComboBoxVP9Space.SelectedIndex != 0)
                {
                    cmd += " --color-space=" + ComboBoxVP9Space.Text;               // Color Space
                }
                if (CheckBoxVP9ARNR.IsChecked == true)
                {
                    cmd += " --arnr-maxframes=" + ComboBoxAomencVP9Max.Text;        // ARNR Max Frames
                    cmd += " --arnr-strength=" + ComboBoxAomencVP9Strength.Text;    // ARNR Strength
                    cmd += " --arnr-type=" + ComboBoxAomencVP9ARNRType.Text;        // ARNR Type
                }
            }

            return cmd;
        }

        // ══════════════════════════════════════ Buttons ═════════════════════════════════════════

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
            if (CustomBG)
                SetBackGroundColor();
            SaveSettingsTab();
        }

        private void ButtonSetBGImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                if (openFileDialog.ShowDialog() == true)
                {
                    Uri fileUri = new Uri(openFileDialog.FileName);
                    imgDynamic.Source = new BitmapImage(fileUri);
                    CustomBG = true;
                    SetBackGroundColor();
                    if (File.Exists("background.txt")) { File.Delete("background.txt"); }
                    SmallFunctions.WriteToFileThreadSafe(openFileDialog.FileName, "background.txt");
                }
                else
                {
                    // Reset BG Image
                    if (File.Exists("background.txt")) { try { File.Delete("background.txt"); } catch { } }
                    imgDynamic.Source = null;
                    CustomBG = false;
                    SolidColorBrush transparent = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                    MetroTab.Background = transparent;
                }
            }
            catch { }
        }

        private void SetBackGroundColor()
        {
            if (ComboBoxBaseTheme.SelectedIndex == 0)
            {
                // Light Theme
                SolidColorBrush transparentWhite = new SolidColorBrush(Color.FromArgb(65, 100, 100, 100));
                SolidColorBrush tab = new SolidColorBrush(Color.FromArgb(90, 10, 10, 10));
                MetroTab.Background = tab;
                Grid0.Background = transparentWhite;
            }
            else
            {
                // Dark Theme
                SolidColorBrush transparentBlack = new SolidColorBrush(Color.FromArgb(65, 30, 30, 30));
                SolidColorBrush tab = new SolidColorBrush(Color.FromArgb(90, 10, 10, 10));
                MetroTab.Background = tab;
                Grid0.Background = transparentBlack;
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
            OpenVideoWindow WindowVideoSource = new OpenVideoWindow(ComboBoxBaseTheme.Text, ComboBoxAccentTheme.Text);
            // Hides the main user interface
            this.Hide();
            // Shows the just created window object and awaits its exit
            WindowVideoSource.ShowDialog();
            // Shows the main user interface
            this.Show();
            // Uses the public get method in OpenVideoSource window to get variable
            string result = WindowVideoSource.VideoPath;
            bool resultProject = WindowVideoSource.ProjectFile;
            bool batchFolder = WindowVideoSource.BatchFolder;
            if (resultProject == false && batchFolder == false)
            {
                // Single Video File Input
                if (WindowVideoSource.QuitCorrectly)
                {
                    SingleFileInput(result);
                }
            }
            else if (batchFolder == false && resultProject == true)
            {
                // Project File
                if (WindowVideoSource.QuitCorrectly)
                {
                    BatchEncoding = false;
                    LoadSettings(true, result);
                    TextBoxTrimEnd.Text = SmallFunctions.GetVideoLengthAccurate(VideoInput);
                    TrimEndTemp = TextBoxTrimEnd.Text;
                    LabelVideoLength.Content = TrimEndTemp.Remove(TrimEndTemp.Length - 4);
                    AutoSetBitDepthAndColorFormat(VideoInput);
                    LabelVideoColorFomat.Content = FFprobe.GetPixelFormat(VideoInput);
                    LabelVideoFramerate.Content = FFprobe.GetFrameRate(VideoInput);
                    string res = FFprobe.GetResolution(VideoInput);
                    LabelVideoResolution.Content = res;
                    TextBoxFiltersResizeHeight.Text = res.Substring(res.LastIndexOf('x') + 1);
                    ReadTimeCode = true;
                }
            }else if (batchFolder == true && resultProject == false)
            {
                // Batch Folder Input
                if (WindowVideoSource.QuitCorrectly)
                {
                    VideoInputSet = true;
                    TextBoxVideoSource.Text = result;
                    VideoInput = result;
                    BatchEncoding = true;
                }

            }
            
        }

        private void ButtonOpenDestination_Click(object sender, RoutedEventArgs e)
        {
            if (BatchEncoding == false)
            {
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
                    if (Path.GetExtension(VideoOutput) == ".mp4")
                    {
                        // Disables VFR, as only MKV is supported
                        ToggleSwitchVFR.IsOn = false;
                        ToggleSwitchVFR.IsEnabled = false;
                        ToggleSwitchSubtitleActivatedOne.IsOn = false;
                        ToggleSwitchSubtitleActivatedTwo.IsOn = false;
                        ToggleSwitchSubtitleActivatedThree.IsOn = false;
                        ToggleSwitchSubtitleActivatedFour.IsOn = false;
                        ToggleSwitchSubtitleActivatedFive.IsOn = false;
                    }
                    else
                    {
                        ToggleSwitchVFR.IsEnabled = true;
                    }
                }
            }
            else
            {
                // Batch Encoding Output
                //Sets the Batch Encoding Output Folder
                System.Windows.Forms.FolderBrowserDialog browseOutputFolder = new System.Windows.Forms.FolderBrowserDialog();
                if (browseOutputFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    VideoOutput = browseOutputFolder.SelectedPath;
                    TextBoxVideoDestination.Text = VideoOutput;
                    VideoOutputSet = true;
                }
            }

        }

        private void ButtonOpenTempFolder_Click(object sender, RoutedEventArgs e)
        {
            // Opens the Temp Folder
            if (ToggleSwitchTempFolder.IsOn == false) 
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

        private void ButtonOpenLogFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Path.Combine(Directory.GetCurrentDirectory(), "Logging"));
        }

        private void ButtonSetTempPath_Click(object sender, RoutedEventArgs e)
        {
            // Custom Temp Path
            System.Windows.Forms.FolderBrowserDialog browseOutputFolder = new System.Windows.Forms.FolderBrowserDialog();
            if (browseOutputFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TextBoxCustomTempPath.Text = browseOutputFolder.SelectedPath;
                SaveSettingsTab();
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
            if (!EncodeStarted)
            {
                Updater updater = new Updater(ComboBoxBaseTheme.Text, ComboBoxAccentTheme.Text);
                updater.ShowDialog();
                CheckDependencies.Check();
            }
        }

        // ══════════════════════════════════ Video Splitting ═════════════════════════════════════

        List<string> FFmpegArgs = new List<string>();

        private void StartSplittingDetect()
        {
            ProgressBar.Dispatcher.Invoke(() => ProgressBar.IsIndeterminate = true);
            // Main Function
            if (SplitMethod == 0)
                FFmpegSceneDetect();
            if (SplitMethod == 1)
                // PyScenedetect
                PySceneDetect();
            if (SplitMethod == 2)
                // FFmpeg Chunking
                FFmpegChunking();
            ProgressBar.Dispatcher.Invoke(() => ProgressBar.IsIndeterminate = false);
        }

        private void FFmpegSceneDetect()
        {
            // Skip Scene Detect if the file already exist
            if (File.Exists(Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "splits.txt")) == false)
            {
                SmallFunctions.Logging("Scene Detection with FFmpeg");
                LabelProgressBar.Dispatcher.Invoke(() => LabelProgressBar.Content = "Detecting Scenes... this might take a while!");

                List<string> scenes = new List<string>();

                // Starts FFmpeg Process
                Process FFmpegSceneDetect = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = FFmpegPath,
                    RedirectStandardError = true,
                    FileName = "cmd.exe",
                    Arguments = "/C ffmpeg.exe -i " + '\u0022' + VideoInput + '\u0022' + " -hide_banner -loglevel 32 -filter_complex select=" + '\u0022' + "gt(scene\\," + FFmpegThreshold + "),select=eq(key\\,1),showinfo" + '\u0022' + " -an -f null -"
                };
                FFmpegSceneDetect.StartInfo = startInfo;
                FFmpegSceneDetect.Start();

                // Reads Standard Err from FFmpeg Output
                string stream = FFmpegSceneDetect.StandardError.ReadToEnd();

                FFmpegSceneDetect.WaitForExit();

                // Splits the Console Output by spaces
                string[] array = stream.Split(' ');

                // Searches for pts_time, if found it removes "pts_time:" to get only values
                foreach (string value in array) { if (value.Contains("pts_time:")) { scenes.Add(value.Remove(0, 9)); } }

                // Temporary value for Arg creation
                string previousScene = "0.000";

                // Clears the Args List to avoid conflicts in Batch Encode Mode
                FFmpegArgs.Clear();

                // Creates the seeking args for ffmpeg piping
                foreach (string sc in scenes)
                {
                    FFmpegArgs.Add("-ss " + previousScene + " -to " + sc);
                    previousScene = sc;
                }
                // Argument for seeking until the end of the video
                FFmpegArgs.Add("-ss " + previousScene);

                // Writes splitting arguments to text file
                foreach (string line in FFmpegArgs)
                {
                    using (StreamWriter sw = File.AppendText(Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "splits.txt")))
                    {
                        sw.WriteLine(line);
                        sw.Close();
                    }
                }
            }
        }

        private void PySceneDetect()
        {
            // Skip Scene Detect if the file already exist
            if (File.Exists(Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "splits.txt")) == false)
            {
                SmallFunctions.Logging("Scene Detection with PySceneDetect");
                // Detects the Scenes with PySceneDetect
                Process pySceneDetect = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    FileName = "cmd.exe",
                    Arguments = "/C scenedetect -i " + '\u0022' + VideoInput + '\u0022' + " -o " + '\u0022' + Path.Combine(TempPath, TempPathFileName) + '\u0022' + " detect-content list-scenes"
                };
                pySceneDetect.StartInfo = startInfo;

                pySceneDetect.Start();

                // Reads the Stderr and sets the TextBox
                do { LabelProgressBar.Dispatcher.Invoke(() => LabelProgressBar.Content = pySceneDetect.StandardError.ReadLine()); } while (!pySceneDetect.HasExited);

                pySceneDetect.WaitForExit();

                PySceneDetectParse();
            }

        }

        private void PySceneDetectParse()
        {
            // Reads first line of the csv file generated by pyscenedetect
            string line = File.ReadLines(Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, MainWindow.TempPathFileName + "-Scenes.csv")).First();

            // Splits the line after "," and skips the first line, then adds the result to list
            List<string> scenes = line.Split(',').Skip(1).ToList<string>();

            // Temporary value used for creating the ffmpeg command line
            string previousScene = "00:00:00.000";

            // Argument for seeking until the end of the video
            FFmpegArgs.Add("-ss " + previousScene);

            // Iterates over the list of time codes and creates the args for ffmpeg
            foreach (string sc in scenes)
            {
                FFmpegArgs.Add("-ss " + previousScene + " -to " + sc);
                previousScene = sc;
            }

            // Has to be last, to "tell" ffmpeg to seek / encode until end of video
            FFmpegArgs.Add("-ss " + previousScene);

            // Writes splitting arguments to text file
            foreach (string lineArg in FFmpegArgs)
            {
                using (StreamWriter sw = File.AppendText(Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "splits.txt")))
                {
                    sw.WriteLine(lineArg);
                    sw.Close();
                }
            }
        }

        private void FFmpegChunking()
        {
            // Skip splitting if already splitted
            if (File.Exists(Path.Combine(TempPath, TempPathFileName, "finished_splitting.log")) == false)
            {
                // Sets the TextBox, has to be done as Dispatcher, else it will lock up the thread
                LabelProgressBar.Dispatcher.Invoke(() => LabelProgressBar.Content = "Started Splitting... this might take a while!");

                string EncodeCMD = null;

                // Sets the Reencode Params
                if (SplitReencode == true)
                {
                    if (ReEncodeMethodSplitting == 0)
                        EncodeCMD = "-c:v libx264 -crf 0 -preset ultrafast -g 9 -sc_threshold 0 -force_key_frames " + '\u0022' + "expr:gte(t, n_forced * 9)" + '\u0022';
                    if (ReEncodeMethodSplitting == 1)
                        EncodeCMD = "-c:v ffv1 -level 3 -threads 4 -coder 1 -context 1 -g 1 -slicecrc 0 -slices 4";
                    if (ReEncodeMethodSplitting == 2)
                        EncodeCMD = "-c:v utvideo";
                }
                else { EncodeCMD = "-c:v copy"; }

                //Run ffmpeg command
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    WorkingDirectory = FFmpegPath,
                    Arguments = "/C ffmpeg.exe -i " + '\u0022' + VideoInput + '\u0022' + " " + HardSubCMD + " -map_metadata -1 -an " + EncodeCMD + " -f segment -segment_time " + ChunkLength + " " + '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split%6d.mkv") + '\u0022'
                };
                SmallFunctions.Logging("Splitting with FFmpeg Chunking: " + startInfo.Arguments);
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
                // The rename function has been removed, as the FFmpeg Command above now creates longer filenames %6d instead of %0d
            }
            // Resume stuff to skip splitting in resume mode
            SmallFunctions.WriteToFileThreadSafe("", Path.Combine(TempPath, TempPathFileName, "finished_splitting.log"));
        }


        // ═══════════════════════════════════ Progress Bar ═══════════════════════════════════════

        private void ProgressBarUpdating()
        {
            // Gets all Progress Files of ffmpeg
            string[] filePaths = Directory.GetFiles(Path.Combine(TempPath, TempPathFileName, "Progress"), "*.log", SearchOption.AllDirectories);

            int totalencodedframes = 0;

            // Sets the total framecount
            int totalframes = TotalFrames;

            // The amount of frames doubles when in two pass mode
            if (!OnePass)
                totalframes *= 2;

            // Doibles the amount of frames, when deinterlacing is set to 1
            if (Yadif1)
                totalframes *= 2;

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
                string fileName = "";
                if (BatchEncoding == true)
                    fileName = "Encoding: " + TempPathFileName + " - ";
                LabelProgressBar.Dispatcher.Invoke(() => LabelProgressBar.Content = fileName + totalencodedframes + " / " + totalframes + " Frames - " + Math.Round(totalencodedframes / timespent.TotalSeconds, 2) + "fps - " + Math.Round(((timespent.TotalSeconds / totalencodedframes) * (totalframes - totalencodedframes)) / 60, MidpointRounding.ToEven) + "min left");
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
                                    if (SplitMethod <= 1) { InputVideo = " -i " + '\u0022' + VideoInput + '\u0022' + " " + command; }
                                    else if (SplitMethod == 2) { InputVideo = " -i " + '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", command) + '\u0022'; } // Chunk based splitting

                                    string FFmpegProgress = " -progress " + '\u0022' + Path.Combine(TempPath, TempPathFileName, "Progress", "split" + index.ToString("D5") + "_progress.log") + '\u0022';

                                    string ffmpegPipe = InputVideo + " " + FilterCommand + PipeBitDepthCommand + " -color_range 0 " + VSYNC + " -f yuv4mpegpipe - | ";

                                    // Logic to skip first pass encoding if "_finished" log file exists
                                    if (File.Exists(Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + "_stats.log" + "_finished.log")) == false)
                                    {
                                        string encoderCMD = "";

                                        if (EncodeMethod == 0) // aomenc
                                        {
                                            if (OnePass) // One Pass Encoding
                                            {
                                                encoderCMD = '\u0022' + Path.Combine(AomencPath, "aomenc.exe") + '\u0022' + " - --passes=1" + EncoderAomencCommand + " --output=";
                                                encoderCMD += '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + ".ivf") + '\u0022';
                                            }
                                            else // Two Pass Encoding First Pass
                                            {
                                                encoderCMD = '\u0022' + Path.Combine(AomencPath, "aomenc.exe") + '\u0022' + " - --passes=2 --pass=1" + EncoderAomencCommand + " --fpf=";
                                                encoderCMD += '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + "_stats.log") + '\u0022' + " --output=NUL";
                                            }
                                        }
                                        else if (EncodeMethod == 1) // rav1e
                                        {
                                            encoderCMD = '\u0022' + Path.Combine(Rav1ePath, "rav1e.exe") + '\u0022' + " - " + EncoderRav1eCommand + " --output ";
                                            encoderCMD += '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + ".ivf") + '\u0022';
                                        }
                                        else if (EncodeMethod == 2) // svt-av1
                                        {
                                            ffmpegPipe = InputVideo + " " + FilterCommand + PipeBitDepthCommand + " -color_range 0 -nostdin " + VSYNC + " -f yuv4mpegpipe - | ";
                                            if (OnePass)
                                            {
                                                // One Pass Encoding
                                                encoderCMD = '\u0022' + Path.Combine(SvtAV1Path, "SvtAv1EncApp.exe") + '\u0022' + " -i stdin " + EncoderSvtAV1Command + " --passes 1 -b ";
                                                encoderCMD += '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + ".ivf") + '\u0022';
                                            }
                                            else
                                            {
                                                // Two Pass Encoding First Pass
                                                encoderCMD = '\u0022' + Path.Combine(SvtAV1Path, "SvtAv1EncApp.exe") + '\u0022' + " -i stdin " + EncoderSvtAV1Command + " --irefresh-type 2 --pass 1 -b NUL --stats ";
                                                encoderCMD += '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + "_stats.log") + '\u0022';
                                            }
                                            
                                        }
                                        else if (EncodeMethod == 3) // vp9
                                        {
                                            if (OnePass) // One Pass Encoding
                                            {
                                                encoderCMD = '\u0022' + Path.Combine(VPXPath, "vpxenc.exe") + '\u0022' + " - --passes=1" + EncoderVP9Command + " --output=";
                                                encoderCMD += '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + ".ivf") + '\u0022';
                                            }
                                            else // Two Pass Encoding First Pass
                                            {
                                                encoderCMD = '\u0022' + Path.Combine(VPXPath, "vpxenc.exe") + '\u0022' + " - --passes=2 --pass=1" + EncoderVP9Command + " --fpf=";
                                                encoderCMD += '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + "_stats.log") + '\u0022' + " --output=NUL";
                                            }
                                        }

                                        startInfo.Arguments = "/C ffmpeg.exe" + FFmpegProgress + ffmpegPipe + encoderCMD;

                                        SmallFunctions.Logging("Encoding Video: " + startInfo.Arguments);
                                        ffmpegProcess.StartInfo = startInfo;
                                        ffmpegProcess.Start();

                                        // Sets the process priority
                                        if (!Priority)
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
                                        // Creates a different progress file for the second pass (avoids negative frame progressbar)
                                        FFmpegProgress = " -progress " + '\u0022' + Path.Combine(TempPath, TempPathFileName, "Progress", "split" + index.ToString("D5") + "_progress_2nd.log") + '\u0022';

                                        string encoderCMD = "";
                                        
                                        // Two Pass Encoding Second Pass
                                        if (EncodeMethod == 0) // aomenc
                                        {
                                            encoderCMD = '\u0022' + Path.Combine(AomencPath, "aomenc.exe") + '\u0022' + " - --passes=2 --pass=2" + EncoderAomencCommand + " --fpf=";
                                            encoderCMD += '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + "_stats.log") + '\u0022';
                                            encoderCMD += " --output=" + '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + ".ivf") + '\u0022';
                                        }
                                        else if (EncodeMethod == 1) // rav1e
                                        {
                                            // Rav1e 2 Pass still broken
                                        }
                                        else if (EncodeMethod == 2) // svt-av1
                                        {
                                            ffmpegPipe = InputVideo + " " + FilterCommand + PipeBitDepthCommand + " -color_range 0 -nostdin " + VSYNC + " -f yuv4mpegpipe - | ";
                                            encoderCMD = '\u0022' + Path.Combine(SvtAV1Path, "SvtAv1EncApp.exe") + '\u0022' + " -i stdin " + EncoderSvtAV1Command + " --irefresh-type 2 --pass 2 --stats ";
                                            encoderCMD += '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + "_stats.log") + '\u0022';
                                            encoderCMD += " -b " + '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + ".ivf") + '\u0022';
                                        }
                                        else if (EncodeMethod == 3) // vp9
                                        {
                                            encoderCMD = '\u0022' + Path.Combine(VPXPath, "vpxenc.exe") + '\u0022' + " - --passes=2 --pass=2" + EncoderVP9Command + " --fpf=";
                                            encoderCMD += '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + "_stats.log") + '\u0022';
                                            encoderCMD += " --output=" + '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + ".ivf") + '\u0022';
                                        }

                                        startInfo.Arguments = "/C ffmpeg.exe" + FFmpegProgress + ffmpegPipe + encoderCMD;
                                        SmallFunctions.Logging("Encoding Video: " + startInfo.Arguments);
                                        ffmpegProcess.StartInfo = startInfo;
                                        ffmpegProcess.Start();

                                        // Sets the process priority
                                        if (!Priority)
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

        private void SaveSettingsTab()
        {
            try
            {
                if (StartUp != true)
                {
                    XmlWriter writer = XmlWriter.Create(Path.Combine(Directory.GetCurrentDirectory(), "settings.xml"));
                    writer.WriteStartElement("Settings");
                    writer.WriteElementString("DeleteTempFiles",    ToggleSwitchDeleteTempFiles.IsOn.ToString());
                    writer.WriteElementString("PlaySound",          ToggleSwitchUISounds.IsOn.ToString());
                    writer.WriteElementString("Logging",            ToggleSwitchLogging.IsOn.ToString());
                    writer.WriteElementString("ShowDialog",         ToggleSwitchShowWindow.IsOn.ToString());
                    writer.WriteElementString("Shutdown",           ToggleSwitchShutdownAfterEncode.IsOn.ToString());
                    writer.WriteElementString("TempPathActive",     ToggleSwitchTempFolder.IsOn.ToString());
                    writer.WriteElementString("TempPath",           TextBoxCustomTempPath.Text);
                    writer.WriteElementString("Terminal",           ToggleSwitchHideTerminal.IsOn.ToString());
                    writer.WriteElementString("ThemeAccent",        ComboBoxAccentTheme.SelectedIndex.ToString());
                    writer.WriteElementString("ThemeBase",          ComboBoxBaseTheme.SelectedIndex.ToString());
                    writer.WriteElementString("BatchContainer",     ComboBoxContainerBatchEncoding.SelectedIndex.ToString());
                    writer.WriteEndElement();
                    writer.Close();
                }

            }
            catch { }
        }

        private void LoadSettingsTab()
        {
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "settings.xml")))
            {
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(Path.Combine(Directory.GetCurrentDirectory(), "settings.xml"));
                    XmlNodeList node = doc.GetElementsByTagName("Settings");
                    foreach (XmlNode n in node[0].ChildNodes)
                    {
                        switch (n.Name)
                        {
                            case "DeleteTempFiles": ToggleSwitchDeleteTempFiles.IsOn = n.InnerText == "True"; break;
                            case "PlaySound":       ToggleSwitchUISounds.IsOn = n.InnerText == "True"; break;
                            case "Logging":         ToggleSwitchLogging.IsOn = n.InnerText == "True"; break;
                            case "ShowDialog":      ToggleSwitchShowWindow.IsOn = n.InnerText == "True"; break;
                            case "Shutdown":        ToggleSwitchShutdownAfterEncode.IsOn = n.InnerText == "True"; break;
                            case "TempPathActive":  ToggleSwitchTempFolder.IsOn = n.InnerText == "True"; break;
                            case "TempPath":        TextBoxCustomTempPath.Text = n.InnerText; break;
                            case "Terminal":        ToggleSwitchHideTerminal.IsOn = n.InnerText == "True"; break;
                            case "ThemeAccent":     ComboBoxAccentTheme.SelectedIndex = int.Parse(n.InnerText); break;
                            case "ThemeBase":       ComboBoxBaseTheme.SelectedIndex = int.Parse(n.InnerText); break;
                            case "BatchContainer":  ComboBoxContainerBatchEncoding.SelectedIndex = int.Parse(n.InnerText); break;
                            default: break;
                        }
                    }
                }
                catch { }

                ThemeManager.Current.ChangeTheme(this, ComboBoxBaseTheme.Text + "." + ComboBoxAccentTheme.Text);
            }

            // Custom BG
            if (File.Exists("background.txt"))
            {
                try
                {
                    Uri fileUri = new Uri(File.ReadAllText("background.txt"));
                    imgDynamic.Source = new BitmapImage(fileUri);
                    CustomBG = true;
                    SetBackGroundColor();
                }
                catch { }
            }
        }

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
                writer.WriteElementString("SubOne",                     ToggleSwitchSubtitleActivatedOne.IsOn.ToString());              // Subtitle Track One Active
                writer.WriteElementString("SubTwo",                     ToggleSwitchSubtitleActivatedTwo.IsOn.ToString());              // Subtitle Track Two Active
                writer.WriteElementString("SubThree",                   ToggleSwitchSubtitleActivatedThree.IsOn.ToString());            // Subtitle Track Three Active
                writer.WriteElementString("SubFour",                    ToggleSwitchSubtitleActivatedFour.IsOn.ToString());             // Subtitle Track Four Active
                writer.WriteElementString("SubFive",                    ToggleSwitchSubtitleActivatedFive.IsOn.ToString());             // Subtitle Track Five Active
                writer.WriteElementString("SubOneBurn",                 CheckBoxSubOneBurn.IsChecked.ToString());                       // Subtitle Track One Burn
                writer.WriteElementString("SubTwoBurn",                 CheckBoxSubTwoBurn.IsChecked.ToString());                       // Subtitle Track One Burn
                writer.WriteElementString("SubThreeBurn",               CheckBoxSubThreeBurn.IsChecked.ToString());                     // Subtitle Track One Burn
                writer.WriteElementString("SubFourBurn",                CheckBoxSubFourBurn.IsChecked.ToString());                      // Subtitle Track One Burn
                writer.WriteElementString("SubFiveBurn",                CheckBoxSubFiveBurn.IsChecked.ToString());                      // Subtitle Track One Burn
                writer.WriteElementString("SubOneDefault",              CheckBoxSubOneDefault.IsChecked.ToString());                    // Subtitle Track One Default
                writer.WriteElementString("SubTwoDefault",              CheckBoxSubTwoDefault.IsChecked.ToString());                    // Subtitle Track One Default
                writer.WriteElementString("SubThreeDefault",            CheckBoxSubThreeDefault.IsChecked.ToString());                  // Subtitle Track One Default
                writer.WriteElementString("SubFourDefault",             CheckBoxSubFourDefault.IsChecked.ToString());                   // Subtitle Track One Default
                writer.WriteElementString("SubFiveDefault",             CheckBoxSubFiveDefault.IsChecked.ToString());                   // Subtitle Track One Default
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
                writer.WriteElementString("SubFiveLanguage",           ComboBoxSubTrackFiveLanguage.SelectedIndex.ToString());          // Subtitle Track Five Language
                // Audio (for resume mode)
                writer.WriteElementString("AudioLangOne",               ComboBoxTrackOneLanguage.SelectedIndex.ToString());             // Audio Track One Language
                writer.WriteElementString("AudioLangTwo",               ComboBoxTrackTwoLanguage.SelectedIndex.ToString());             // Audio Track Two Language
                writer.WriteElementString("AudioLangThree",             ComboBoxTrackThreeLanguage.SelectedIndex.ToString());           // Audio Track Three Language
                writer.WriteElementString("AudioLangFour",              ComboBoxTrackFourLanguage.SelectedIndex.ToString());            // Audio Track Four Language
            }
            // ═══════════════════════════════════════════════════════════════════ Audio ══════════════════════════════════════════════════════════════════
            writer.WriteElementString("AudioTrackOne",                  ToggleSwitchAudioTrackOne.IsOn.ToString());                    // Audio Track One Active
            writer.WriteElementString("AudioTrackTwo",                  ToggleSwitchAudioTrackTwo.IsOn.ToString());                    // Audio Track Two Active
            writer.WriteElementString("AudioTrackThree",                ToggleSwitchAudioTrackThree.IsOn.ToString());                  // Audio Track Three Active
            writer.WriteElementString("AudioTrackFour",                 ToggleSwitchAudioTrackFour.IsOn.ToString());                   // Audio Track Four Active
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

            writer.WriteElementString("FilterCrop",                 ToggleSwitchFilterCrop.IsOn.ToString());                            // Filter Crop (Boolean)
            if (ToggleSwitchFilterCrop.IsOn == true)
            {
                // Cropping
                writer.WriteElementString("FilterCropTop",          TextBoxFiltersCropTop.Text);                                        // Filter Crop Top
                writer.WriteElementString("FilterCropBottom",       TextBoxFiltersCropBottom.Text);                                     // Filter Crop Bottom
                writer.WriteElementString("FilterCropLeft",         TextBoxFiltersCropLeft.Text);                                       // Filter Crop Left
                writer.WriteElementString("FilterCropRight",        TextBoxFiltersCropRight.Text);                                      // Filter Crop Right
            }

            writer.WriteElementString("FilterResize",               ToggleSwitchFilterResize.IsOn.ToString());                          // Filter Resize (Boolean)
            if (ToggleSwitchFilterResize.IsOn == true)
            {
                // Resize
                writer.WriteElementString("FilterResizeWidth",      TextBoxFiltersResizeWidth.Text);                                    // Filter Resize Width
                writer.WriteElementString("FilterResizeHeight",     TextBoxFiltersResizeHeight.Text);                                   // Filter Resize Height
                writer.WriteElementString("FilterResizeAlgo",       ComboBoxFiltersScaling.SelectedIndex.ToString());                   // Filter Resize Scaling Algorithm
            }

            writer.WriteElementString("FilterRotate",               ToggleSwitchFilterRotate.IsOn.ToString());                          // Filter Rotate (Boolean)
            if (ToggleSwitchFilterRotate.IsOn == true)
            {
                // Rotating
                writer.WriteElementString("FilterRotateAmount",     ComboBoxFiltersRotate.SelectedIndex.ToString());                    // Filter Rotate
            }

            writer.WriteElementString("FilterDeinterlace",          ToggleSwitchFilterDeinterlace.IsOn.ToString());                     // Filter Deinterlace (Boolean)
            if (ToggleSwitchFilterDeinterlace.IsOn == true)
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
            writer.WriteElementString("VideoVFR",               ToggleSwitchVFR.IsOn.ToString());                                       // Video Variable Framerate

            writer.WriteElementString("WorkerCount", ComboBoxWorkerCount.SelectedIndex.ToString());                                     // Worker Count
            writer.WriteElementString("WorkerPriority", ComboBoxProcessPriority.SelectedIndex.ToString());                              // Worker Priority
            // ══════════════════════════════════════════════════════════ Advanced Video Settings ══════════════════════════════════════════════════════════

            writer.WriteElementString("VideoAdvanced",          ToggleSwitchAdvancedVideoSettings.IsOn.ToString());                     // Video Advanced Settings
            writer.WriteElementString("VideoAdvancedCustom",    CheckBoxCustomVideoSettings.IsChecked.ToString());                      // Video Advanced Settings Custom

            if (ToggleSwitchAdvancedVideoSettings.IsOn == true && CheckBoxCustomVideoSettings.IsChecked == false)
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
                    writer.WriteElementString("VideoAdvancedAomencTuneContent", ComboBoxAomencTuneContent.SelectedIndex.ToString());    // Video Advanced Settings Aomenc Tune
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
                else if (ComboBoxVideoEncoder.SelectedIndex == 3)
                {
                    // vp9
                    writer.WriteElementString("VideoAdvancedVP9Threads",        ComboBoxVP9Threads.SelectedIndex.ToString());           // Video Advanced Settings VP9 Threads
                    writer.WriteElementString("VideoAdvancedVP9TileCols",       ComboBoxVP9TileColumns.SelectedIndex.ToString());       // Video Advanced Settings VP9 Tile Columns
                    writer.WriteElementString("VideoAdvancedVP9TileRows",       ComboBoxVP9TileRows.SelectedIndex.ToString());          // Video Advanced Settings VP9 Tile Rows
                    writer.WriteElementString("VideoAdvancedVP9GOP",            TextBoxVP9MaxKF.Text);                                  // Video Advanced Settings VP9 GOP
                    writer.WriteElementString("VideoAdvancedVP9Lag",            TextBoxVP9LagInFrames.Text);                            // Video Advanced Settings VP9 Lag in Frames
                    writer.WriteElementString("VideoAdvancedVP9AQMode",         ComboBoxVP9AQMode.SelectedIndex.ToString());            // Video Advanced Settings VP9 AQ Mode
                    writer.WriteElementString("VideoAdvancedVP9Tune",           ComboBoxVP9ATune.SelectedIndex.ToString());             // Video Advanced Settings VP9 Tune
                    writer.WriteElementString("VideoAdvancedVP9TuneContent",    ComboBoxVP9ATuneContent.SelectedIndex.ToString());      // Video Advanced Settings VP9 Tune Content
                    writer.WriteElementString("VideoAdvancedVP9ColorFormat",    ComboBoxVP9ColorFormat.SelectedIndex.ToString());       // Video Advanced Settings VP9 Color Format
                    writer.WriteElementString("VideoAdvancedVP9ColorSpace",     ComboBoxVP9Space.SelectedIndex.ToString());             // Video Advanced Settings VP9 Color Space
                    writer.WriteElementString("VideoAdvancedVP9ARNR",           CheckBoxVP9ARNR.IsChecked.ToString());                  // Video Advanced Settings VP9 ARNR
                    if (CheckBoxAomencARNRMax.IsChecked == true)
                    {
                        writer.WriteElementString("VideoAdvancedVP9ARNRMax",    ComboBoxAomencVP9Max.SelectedIndex.ToString());         // Video Advanced Settings VP9 ARNR Max
                        writer.WriteElementString("VideoAdvancedVP9ARNRStre",   ComboBoxAomencVP9Strength.SelectedIndex.ToString());    // Video Advanced Settings VP9 ARNR Strength
                        writer.WriteElementString("VideoAdvancedVP9ARNRType",   ComboBoxAomencVP9ARNRType.SelectedIndex.ToString());    // Video Advanced Settings VP9 ARNR Type
                    }
                }

            }
            else if (ToggleSwitchAdvancedVideoSettings.IsOn == true && CheckBoxCustomVideoSettings.IsChecked == true)
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
                    case "AudioTrackOne":                   ToggleSwitchAudioTrackOne.IsOn = n.InnerText == "True";                 break;  // Audio Track One Active
                    case "AudioTrackTwo":                   ToggleSwitchAudioTrackTwo.IsOn = n.InnerText == "True";                 break;  // Audio Track Two Active
                    case "AudioTrackThree":                 ToggleSwitchAudioTrackThree.IsOn = n.InnerText == "True";               break;  // Audio Track Three Active
                    case "AudioTrackFour":                  ToggleSwitchAudioTrackFour.IsOn = n.InnerText == "True";                break;  // Audio Track Four Active
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
                    case "FilterCrop":                      ToggleSwitchFilterCrop.IsOn = n.InnerText == "True";                    break;  // Filter Crop (Boolean)
                    case "FilterCropTop":                   TextBoxFiltersCropTop.Text = n.InnerText;                               break;  // Filter Crop Top
                    case "FilterCropBottom":                TextBoxFiltersCropBottom.Text = n.InnerText;                            break;  // Filter Crop Bottom
                    case "FilterCropLeft":                  TextBoxFiltersCropLeft.Text = n.InnerText;                              break;  // Filter Crop Left
                    case "FilterCropRight":                 TextBoxFiltersCropRight.Text = n.InnerText;                             break;  // Filter Crop Right
                    case "FilterResize":                    ToggleSwitchFilterResize.IsOn = n.InnerText == "True";                  break;  // Filter Resize (Boolean)
                    case "FilterResizeWidth":               TextBoxFiltersResizeWidth.Text = n.InnerText;                           break;  // Filter Resize Width
                    case "FilterResizeHeight":              TextBoxFiltersResizeHeight.Text = n.InnerText;                          break;  // Filter Resize Height
                    case "FilterResizeAlgo":                ComboBoxFiltersScaling.SelectedIndex = int.Parse(n.InnerText);          break;  // Filter Resize Scaling Algorithm
                    case "FilterRotate":                    ToggleSwitchFilterRotate.IsOn = n.InnerText == "True";                  break;  // Filter Rotate (Boolean)
                    case "FilterRotateAmount":              ComboBoxFiltersRotate.SelectedIndex = int.Parse(n.InnerText);           break;  // Filter Rotate
                    case "FilterDeinterlace":               ToggleSwitchFilterDeinterlace.IsOn = n.InnerText == "True";             break;  // Filter Deinterlace (Boolean)
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
                    case "VideoVFR":                        ToggleSwitchVFR.IsOn = n.InnerText == "True";                           break;  // VIdeo Variable Framerate
                    // ═════════════════════════════════════════════════════════ Advanced Video Settings ═══════════════════════════════════════════════════════════
                    case "VideoAdvanced":                   ToggleSwitchAdvancedVideoSettings.IsOn = n.InnerText == "True";         break;  // Video Advanced Settings
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
                    case "VideoAdvancedAomencTuneContent":  ComboBoxAomencTuneContent.SelectedIndex = int.Parse(n.InnerText);       break;  // Video Advanced Settings Aomenc Tune Content
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
                    case "VideoAdvancedVP9Threads":         ComboBoxVP9Threads.SelectedIndex = int.Parse(n.InnerText);              break;  // Video Advanced Settings VP9 Threads
                    case "VideoAdvancedVP9TileCols":        ComboBoxVP9TileColumns.SelectedIndex = int.Parse(n.InnerText);          break;  // Video Advanced Settings VP9 Tile Columns
                    case "VideoAdvancedVP9TileRows":        ComboBoxVP9TileRows.SelectedIndex = int.Parse(n.InnerText);             break;  // Video Advanced Settings VP9 Tile Rows
                    case "VideoAdvancedVP9GOP":             TextBoxVP9MaxKF.Text = n.InnerText;                                     break;  // Video Advanced Settings VP9 GOP
                    case "VideoAdvancedVP9Lag":             TextBoxVP9LagInFrames.Text = n.InnerText;                               break;  // Video Advanced Settings VP9 Lag in Frames
                    case "VideoAdvancedVP9AQMode":          ComboBoxVP9AQMode.SelectedIndex = int.Parse(n.InnerText);               break;  // Video Advanced Settings VP9 AQ Mode
                    case "VideoAdvancedVP9Tune":            ComboBoxVP9ATune.SelectedIndex = int.Parse(n.InnerText);                break;  // Video Advanced Settings VP9 Tune
                    case "VideoAdvancedVP9TuneContent":     ComboBoxVP9ATuneContent.SelectedIndex = int.Parse(n.InnerText);         break;  // Video Advanced Settings VP9 Tune Content
                    case "VideoAdvancedVP9ColorFormat":     ComboBoxVP9ColorFormat.SelectedIndex = int.Parse(n.InnerText);          break;  // Video Advanced Settings VP9 Color Format
                    case "VideoAdvancedVP9ColorSpace":      ComboBoxVP9Space.SelectedIndex = int.Parse(n.InnerText);                break;  // Video Advanced Settings VP9 Color Space
                    case "VideoAdvancedVP9ARNR":            CheckBoxVP9ARNR.IsChecked = n.InnerText == "True";                      break;  // Video Advanced Settings VP9 ARNR
                    case "VideoAdvancedVP9ARNRMax":         ComboBoxAomencVP9Max.SelectedIndex = int.Parse(n.InnerText);            break;  // Video Advanced Settings VP9 ARNR Max
                    case "VideoAdvancedVP9ARNRStre":        ComboBoxAomencVP9Strength.SelectedIndex = int.Parse(n.InnerText);       break;  // Video Advanced Settings VP9 ARNR Strength
                    case "VideoAdvancedVP9ARNRType": ComboBoxAomencVP9ARNRType.SelectedIndex = int.Parse(n.InnerText);              break;  // Video Advanced Settings VP9 ARNR Type
                    case "VideoAdvancedCustomString":       TextBoxCustomVideoSettings.Text = n.InnerText;                          break;  // Video Advanced Settings Custom String
                    // Subtitles
                    case "SubOne":                          ToggleSwitchSubtitleActivatedOne.IsOn = n.InnerText == "True";          break;  // Subtitle Track One Active
                    case "SubTwo":                          ToggleSwitchSubtitleActivatedTwo.IsOn = n.InnerText == "True";          break;  // Subtitle Track Two Active
                    case "SubThree":                        ToggleSwitchSubtitleActivatedThree.IsOn = n.InnerText == "True";        break;  // Subtitle Track Three Active
                    case "SubFour":                         ToggleSwitchSubtitleActivatedFour.IsOn = n.InnerText == "True";         break;  // Subtitle Track Four Active
                    case "SubFive":                         ToggleSwitchSubtitleActivatedFive.IsOn = n.InnerText == "True";         break;  // Subtitle Track Five Active
                    case "SubOneBurn":                      CheckBoxSubOneBurn.IsChecked = n.InnerText == "True";                   break;  // Subtitle Track One Burn
                    case "SubTwoBurn":                      CheckBoxSubTwoBurn.IsChecked = n.InnerText == "True";                   break;  // Subtitle Track Two Burn
                    case "SubThreeBurn":                    CheckBoxSubThreeBurn.IsChecked = n.InnerText == "True";                 break;  // Subtitle Track Three Burn
                    case "SubFourBurn":                     CheckBoxSubFourBurn.IsChecked = n.InnerText == "True";                  break;  // Subtitle Track Four Burn
                    case "SubFiveBurn":                     CheckBoxSubFiveBurn.IsChecked = n.InnerText == "True";                  break;  // Subtitle Track Five Burn
                    case "SubOneDefault":                   CheckBoxSubOneDefault.IsChecked = n.InnerText == "True";                break;  // Subtitle Track One Default
                    case "SubTwoDefault":                   CheckBoxSubTwoDefault.IsChecked = n.InnerText == "True";                break;  // Subtitle Track Two Default
                    case "SubThreeDefault":                 CheckBoxSubThreeDefault.IsChecked = n.InnerText == "True";              break;  // Subtitle Track Three Default
                    case "SubFourDefault":                  CheckBoxSubFourDefault.IsChecked = n.InnerText == "True";               break;  // Subtitle Track Four Default
                    case "SubFiveDefault":                  CheckBoxSubFiveDefault.IsChecked = n.InnerText == "True";               break;  // Subtitle Track Five Default
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
