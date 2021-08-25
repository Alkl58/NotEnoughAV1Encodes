using ControlzEx.Theming;
using MahApps.Metro.Controls;
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
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Xml;

namespace NotEnoughAV1Encodes
{
    public partial class MainWindow : MetroWindow
    {
        // ══════════════════════════════ Public Static ══════════════════════════════

        public static string FilterCommand = null;
        public static string UiLanguage = null;
        public static string VSYNC = " -vsync 0 ";
        public static string VFRCMD = "";
        public static string HardSubCommand;
        public static string SubCommand;

        public static int EncodeMethod = 0;
        public static int TotalFrames = 0;

        public static bool DeleteTempFiles = false;
        public static bool PlayUISounds = false;
        public static bool VFRVideo = false;
        public static bool OnePass = true;
        public static bool Logging = true;
        public static bool StartUp = true;

        public static bool subSoftSubEnabled;
        public static bool subHardSubEnabled;
        public static bool subMessageShowed = false;
        public static bool reencodeMessage = false;

        public static bool PySceneFound = false;
        public static bool BatchWithDifferentPresets = false;

        public static System.Windows.Controls.ItemCollection BatchPresets = null;

        // ═════════════════════════════════ Private ═════════════════════════════════

        private string BatchOutContainer = ".mkv";
        private string CustomTempPath = null;
        private string AccentTheme = "Blue";
        private string BaseTheme = "Light";
        private string AudioCommand = null;
        private int DefaultAudioCodec = 0;
        private string DefaultBitrateCodec = "128";

        private bool SkipSubtitleExtraction = false;
        private bool ShutdownAfterEncode = false;
        private bool UseCustomTempPath = false;
        private bool WorkerOverride = false;
        private bool BatchEncoding = false;
        private bool PopupWindow = false;
        private bool Yadif1 = false;
        private bool VideoInputSet = false;
        private bool VideoOutputSet = false;

        private DateTime StartTime;

        private CancellationTokenSource cancellationTokenSource;

        private readonly Dictionary<string, string> AudioLanguages = new Dictionary<string, string>();

        public MainWindow()
        {
            InitializeComponent();
            Startup();
        }

        private void Startup()
        {
            CheckDependencies.Check();

            // Sets the workercount combobox
            int corecount = Helpers.GetCoreCount();
            for (int i = 1; i <= corecount; i++) { ComboBoxWorkerCount.Items.Add(i); }
            ComboBoxWorkerCount.SelectedItem = Convert.ToInt32(corecount * 75 / 100);

            LoadPresetsIntoComboBox();
            LoadDefaultProfile();
            LoadSettingsTab();
            FillLanguagesStartup();
            SetLanguageStartup();
            StartUp = false;
        }

        private void SetLanguageDictionary(string lang)
        {
            ResourceDictionary dict = new ResourceDictionary();
            switch (lang)
            {
                case "Deutsch":
                case "de-CH":
                case "de-LU":
                case "de-LI":
                case "de-AT":
                case "de-DE":
                    dict.Source = new Uri("..\\lang\\de-DE.xaml", UriKind.Relative);
                    break;

                case "Français":
                case "fr-BE":
                case "fr-CA":
                case "fr-LU":
                case "fr-MC":
                case "fr-CH":
                case "fr-FR":
                    dict.Source = new Uri("..\\lang\\fr-FR.xaml", UriKind.Relative);
                    break;

                case "English":
                default:
                    dict.Source = new Uri("..\\lang\\en-US.xaml", UriKind.Relative);
                    break;
            }
            Resources.MergedDictionaries.Add(dict);
        }

        private void SetLanguageStartup()
        {
            SetLanguageDictionary(Thread.CurrentThread.CurrentCulture.ToString());

            if (UiLanguage != null)
            {
                SetLanguageDictionary(UiLanguage);
            }
        }

        private void FillLanguagesStartup()
        {
            // Languages in ISO 639-2 Format: https://en.wikipedia.org/wiki/List_of_ISO_639-2_codes
            AudioLanguages.Add("English", "eng");
            AudioLanguages.Add("Bosnian", "bos");
            AudioLanguages.Add("Bulgarian", "bul");
            AudioLanguages.Add("Chinese", "zho");
            AudioLanguages.Add("Czech", "ces");
            AudioLanguages.Add("Greek", "ell");
            AudioLanguages.Add("Estonian", "est");
            AudioLanguages.Add("Persian", "per");
            AudioLanguages.Add("Filipino", "fil");
            AudioLanguages.Add("Finnish", "fin");
            AudioLanguages.Add("French", "fra");
            AudioLanguages.Add("Georgian", "kat");
            AudioLanguages.Add("German", "deu");
            AudioLanguages.Add("Croatian", "hrv");
            AudioLanguages.Add("Hungarian", "hun");
            AudioLanguages.Add("Indonesian", "ind");
            AudioLanguages.Add("Icelandic", "isl");
            AudioLanguages.Add("Italian", "ita");
            AudioLanguages.Add("Japanese", "jpn");
            AudioLanguages.Add("Korean", "kor");
            AudioLanguages.Add("Latin", "lat");
            AudioLanguages.Add("Latvian", "lav");
            AudioLanguages.Add("Lithuanian", "lit");
            AudioLanguages.Add("Dutch", "nld");
            AudioLanguages.Add("Norwegian", "nob");
            AudioLanguages.Add("Polish", "pol");
            AudioLanguages.Add("Portuguese", "por");
            AudioLanguages.Add("Romanian", "ron");
            AudioLanguages.Add("Russian", "rus");
            AudioLanguages.Add("Slovak", "slk");
            AudioLanguages.Add("Slovenian", "slv");
            AudioLanguages.Add("Spanish", "spa");
            AudioLanguages.Add("Swedish", "swe");
            AudioLanguages.Add("Thai", "tha");
            AudioLanguages.Add("Turkish", "tur");
            AudioLanguages.Add("Ukrainian", "ukr");
            AudioLanguages.Add("Vietnamese", "vie");

            Dictionary<string, string>.KeyCollection keys = AudioLanguages.Keys;
            foreach (string lang in keys)
            {
                ComboBoxSubTrackOneLanguage.Items.Add(lang);
                ComboBoxSubTrackTwoLanguage.Items.Add(lang);
                ComboBoxSubTrackThreeLanguage.Items.Add(lang);
                ComboBoxSubTrackFourLanguage.Items.Add(lang);
                ComboBoxSubTrackFiveLanguage.Items.Add(lang);
            }
            AudioLanguages.Add("und", "und");
        }

        // ═══════════════════════════════════════ UI Logic ═══════════════════════════════════════

        private void ButtonProgramSettings_Click(object sender, RoutedEventArgs e)
        {
            Views.Settings settings = new Views.Settings(BaseTheme, AccentTheme);
            _ = settings.ShowDialog();
            LoadSettingsTab();
            SetLanguageStartup();
        }

        private void CheckBoxSkipReencode_Checked(object sender, RoutedEventArgs e)
        {
            if (CheckBoxSkipReencode.IsChecked == true && !reencodeMessage)
            {
                MessageBox.Show("Disabling reencoding can lead to frameloss, if the video source has incorrect frame-metadata.\n\nPossible issues which are only visible after encoding:\n- Audio get's more and more desynced\n- Video stream is shorter", "Attention", MessageBoxButton.OK, MessageBoxImage.Information);
                reencodeMessage = true;
                Views.Settings settings = new Views.Settings(BaseTheme, AccentTheme);
                settings.SaveSettingsTab();
                settings.Close();
            }
        }

        private void ToggleSwitchSubtitleActivatedOne_Toggled(object sender, RoutedEventArgs e)
        {
            if (VideoOutputSet && Path.GetExtension(Global.Video_Path) == ".mp4" && !subMessageShowed)
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
            foreach (string s in (string[])e.Data.GetData(DataFormats.FileDrop, false)) { filepaths.Add(s); }
            int counter = 0;
            foreach (string item in filepaths)
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
            Global.Video_Path = file;
            Global.temp_path_folder = Path.GetFileNameWithoutExtension(file);
            Helpers.Check_Unicode(Global.temp_path_folder);
            BatchEncoding = false;
            GetSourceInformation(false);
            GetSubtitleTracks();
            AutoSetBitDepthAndColorFormat(file);
        }

        private void AutoSetBitDepthAndColorFormat(string result)
        {
            // Get & Set correct Color Formats
            MediaInfo mediaInfo = new MediaInfo();
            mediaInfo.Open(Global.Video_Path);

            string mediainfo_chroma_subsampling = mediaInfo.Get(StreamKind.Video, 0, "ChromaSubsampling");
            if (string.IsNullOrEmpty(mediainfo_chroma_subsampling))
            {
                mediainfo_chroma_subsampling = "4:2:0";
            }

            int mediainfo_bit_depth = 8;
            try
            {
                mediainfo_bit_depth = int.Parse(mediaInfo.Get(StreamKind.Video, 0, "BitDepth"));
            }
            catch { }

            mediaInfo.Close();

            LabelVideoColorFomat.Content = mediainfo_chroma_subsampling;

            int chroma_subsampling_index = 0;
            int bit_depth_index = 0;

            if (mediainfo_bit_depth == 10)
            {
                bit_depth_index = 1;
            }
            else if (mediainfo_bit_depth == 12)
            {
                bit_depth_index = 2;
            }

            if (mediainfo_chroma_subsampling == "4:2:0")
            {
                chroma_subsampling_index = 0;
            }
            else if (mediainfo_chroma_subsampling == "4:2:2")
            {
                chroma_subsampling_index = 1;
            }
            else if (mediainfo_chroma_subsampling == "4:4:4")
            {
                chroma_subsampling_index = 2;
            }

            ComboBoxVideoBitDepth.SelectedIndex = bit_depth_index;
            ComboBoxColorFormat.SelectedIndex = chroma_subsampling_index;
        }

        private void ComboBoxSplittingMethod_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ComboBoxSplittingMethod.SelectedIndex == 2)
            {
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
            OpenFileDialog openSubtitleFileDialog = new OpenFileDialog
            {
                Filter = "Subtitle Files|*.pgs;*.srt;*.sup;*.ass;*.ssa;|All Files|*.*"
            };
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
                if (ComboBoxVideoEncoder.SelectedIndex == 1 || ComboBoxVideoEncoder.SelectedIndex == 6)
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
                if (ComboBoxVideoEncoder.SelectedIndex == 0 || ComboBoxVideoEncoder.SelectedIndex == 5)
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

        private void ComboBoxPresets_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (ComboBoxPresets.SelectedItem != null)
                {
                    // Temporary saves the state of toggleswitches
                    //bool audio1 = ToggleSwitchAudioTrackOne.IsEnabled == true;
                    //bool audio2 = ToggleSwitchAudioTrackTwo.IsEnabled == true;
                    //bool audio3 = ToggleSwitchAudioTrackThree.IsEnabled == true;
                    //bool audio4 = ToggleSwitchAudioTrackFour.IsEnabled == true;
                    // Loads the selected preset file
                    LoadSettings(true, ComboBoxPresets.SelectedItem.ToString());
                    // Reloads the saved states of the toogleswitches
                    //if (!audio1) { ToggleSwitchAudioTrackOne.IsEnabled = false; ToggleSwitchAudioTrackOne.IsOn = false; }
                    //if (!audio2) { ToggleSwitchAudioTrackTwo.IsEnabled = false; ToggleSwitchAudioTrackTwo.IsOn = false; }
                    //if (!audio3) { ToggleSwitchAudioTrackThree.IsEnabled = false; ToggleSwitchAudioTrackThree.IsOn = false; }
                    //if (!audio4) { ToggleSwitchAudioTrackFour.IsEnabled = false; ToggleSwitchAudioTrackFour.IsOn = false; }
                }
                else { }
            }
            catch { }
        }

        private void ComboBoxVideoEncoder_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (SliderVideoSpeed != null)
            {
                if (ComboBoxVideoEncoder.SelectedIndex == 0 || ComboBoxVideoEncoder.SelectedIndex == 5)
                {
                    // aomenc
                    SliderVideoSpeed.Maximum = 9;
                    SliderVideoSpeed.Value = 4;
                    SliderVideoQuality.Value = 28;
                    SliderVideoQuality.Maximum = 63;
                }
                else if (ComboBoxVideoEncoder.SelectedIndex == 1 || ComboBoxVideoEncoder.SelectedIndex == 6)
                {
                    // rav1e
                    SliderVideoSpeed.Maximum = 10;
                    SliderVideoSpeed.Value = 6;
                    SliderVideoQuality.Maximum = 255;
                    SliderVideoQuality.Value = 100;
                    // rav1e can only do 1pass atm
                    ComboBoxVideoPasses.SelectedIndex = 0;
                }
                else if (ComboBoxVideoEncoder.SelectedIndex == 2 || ComboBoxVideoEncoder.SelectedIndex == 7)
                {
                    // svt-av1
                    SliderVideoSpeed.Maximum = 8;
                    SliderVideoSpeed.Value = 8;
                    SliderVideoQuality.Value = 50;
                    SliderVideoQuality.Maximum = 63;
                    ComboBoxWorkerCount.SelectedIndex = 0;
                    TextBoxWorkerCountOverride.Text = "1";
                }
                else if (ComboBoxVideoEncoder.SelectedIndex == 3)
                {
                    // vp9
                    SliderVideoSpeed.Maximum = 9;
                    SliderVideoSpeed.Value = 4;
                    SliderVideoQuality.Value = 30;
                    SliderVideoQuality.Maximum = 63;
                }
            }
        }

        private void CheckBoxCustomVideoSettings_Checked(object sender, RoutedEventArgs e)
        {
            // When Checking the custom encoding settings checkbox it will write the current settings to it
            if (CheckBoxCustomVideoSettings.IsChecked == true)
            {
                // Sets the Encoder Settings
                if (ComboBoxVideoEncoder.SelectedIndex == 0) { TextBoxCustomVideoSettings.Text = SetLibAomCommand(); }
                if (ComboBoxVideoEncoder.SelectedIndex == 1) { TextBoxCustomVideoSettings.Text = SetLibRav1eCommand(); }
                if (ComboBoxVideoEncoder.SelectedIndex == 2) { TextBoxCustomVideoSettings.Text = SetLibSvtAV1Command(); }
                if (ComboBoxVideoEncoder.SelectedIndex == 3) { TextBoxCustomVideoSettings.Text = SetVP9Command(); }
                if (ComboBoxVideoEncoder.SelectedIndex == 5) { TextBoxCustomVideoSettings.Text = SetAomencCommand(); }
                if (ComboBoxVideoEncoder.SelectedIndex == 6) { TextBoxCustomVideoSettings.Text = SetRav1eCommand(); }
                if (ComboBoxVideoEncoder.SelectedIndex == 7) { TextBoxCustomVideoSettings.Text = SetSvtAV1Command(); }
            }
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
                if (BaseTheme == "Light")
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
            if (UseCustomTempPath)
            {
                Global.temp_path = CustomTempPath;
            }

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
            // Sets if Video is VFR
            VFRVideo = ToggleSwitchVFR.IsOn == true;

            Audio.CommandGenerator commandgenerator = new Audio.CommandGenerator();

            AudioCommand = commandgenerator.Generate(ListBoxAudioTracks.Items);

            // Starts the Main Function
            if (BatchEncoding == false)
            {
                await MainEntry(cancellationTokenSource.Token);
            }
            else
            {
                // Toggle Delete Temp Files to prevent conflicts
                DeleteTempFiles = true;
                // Batch Encoding
                BatchEncode(cancellationTokenSource.Token);
            }
        }

        private async void BatchEncode(CancellationToken token)
        {
            // Gets all files in folder
            DirectoryInfo batchfiles = new DirectoryInfo(TextBoxVideoSource.Text);
            // Loops over all files in folder
            foreach (FileInfo file in batchfiles.GetFiles())
            {
                if (SmallFunctions.CheckFileType(file.ToString()) && SmallFunctions.Cancel.CancelAll == false)
                {
                    if (BatchWithDifferentPresets == false)
                    {
                        // Normal Batch Encoding

                        // Reset Progressbar
                        ProgressBar.Maximum = 100;
                        ProgressBar.Value = 0;

                        // Set Input
                        Global.Video_Path = Path.Combine(TextBoxVideoSource.Text, file.ToString());

                        // Set Output
                        Global.Video_Output = Path.Combine(TextBoxVideoDestination.Text, Path.GetFileNameWithoutExtension(file.Name) + "_" + ComboBoxVideoEncoder.Text + BatchOutContainer);

                        Helpers.Logging("Batch Encoding: " + file);

                        // Sets Temp Filename for temp folder
                        Global.temp_path_folder = Path.GetFileNameWithoutExtension(Global.Video_Path);

                        // Get Source Information
                        GetSourceInformation(true);

                        // Set Subtitle
                        if (BatchOutContainer != ".mkv")
                        {
                            // Resets Subtitle Settings, as it is mostly not compatible with .webm or .mp4
                            ResetSubtitles();
                        }
                        else { GetSubtitleTracks(); }
                        // Don't want to burn in subtitles in Batch Encoding
                        CheckBoxSubOneBurn.IsChecked = false;
                        CheckBoxSubTwoBurn.IsChecked = false;
                        CheckBoxSubThreeBurn.IsChecked = false;
                        CheckBoxSubFourBurn.IsChecked = false;

                        // Start encoding process
                        await MainEntry(cancellationTokenSource.Token);

                        Helpers.Logging("Batch Encoding Finished: " + file);
                    }
                    else
                    {
                        List<string> encode_presets_to_encode = new List<string>();

                        // Fill List with Presets to use
                        foreach (object preset in BatchPresets)
                        {
                            System.Windows.Controls.CheckBox pre = (System.Windows.Controls.CheckBox)preset;

                            if (pre.IsChecked == true)
                            {
                                encode_presets_to_encode.Add(pre.Content.ToString());
                            }
                        }

                        // Encode each file with the selected presets
                        foreach (string preset in encode_presets_to_encode)
                        {
                            // Reset Progressbar
                            ProgressBar.Maximum = 100;
                            ProgressBar.Value = 0;

                            // Set Input
                            Global.Video_Path = Path.Combine(TextBoxVideoSource.Text, file.ToString());

                            // Set Output
                            Global.Video_Output = Path.Combine(TextBoxVideoDestination.Text, Path.GetFileNameWithoutExtension(file.Name) + "_" + Path.GetFileNameWithoutExtension(preset) + BatchOutContainer);

                            Helpers.Logging("Batch Encoding: " + file + " with Preset: " + preset);

                            // Sets Temp Filename for temp folder
                            Global.temp_path_folder = Path.GetFileNameWithoutExtension(Global.Video_Path);

                            // Load Preset
                            LoadSettings(true, preset);

                            // Get Source Information
                            GetSourceInformation(true);

                            // Set Subtitle
                            if (BatchOutContainer != ".mkv")
                            {
                                // Resets Subtitle Settings, as it is mostly not compatible with .webm or .mp4
                                ResetSubtitles();
                            }
                            else { GetSubtitleTracks(); }
                            // Don't want to burn in subtitles in Batch Encoding
                            CheckBoxSubOneBurn.IsChecked = false;
                            CheckBoxSubTwoBurn.IsChecked = false;
                            CheckBoxSubThreeBurn.IsChecked = false;
                            CheckBoxSubFourBurn.IsChecked = false;

                            // Start encoding process
                            await MainEntry(cancellationTokenSource.Token);

                            Helpers.Logging("Batch Encoding Finished: " + file);
                        }
                    }
                }
            }
            SmallFunctions.PlayFinishedSound();
            ButtonStartEncode.BorderBrush = new SolidColorBrush(Color.FromRgb(112, 112, 112));
            // Reset Start / Pause / Resume Button
            ImageStartPause.Source = Helpers.Get_Uri_Source("play.png");
            LabelStartPause.Content = "Start";
            // Set Encoding State to IDLE
            encode_state = 0;
        }

        public async Task MainEntry(CancellationToken token)
        {
            try
            {
                Helpers.Logging("Temp Path: " + Global.temp_path);

                // Temp Folder Creation
                if (!Directory.Exists(Path.Combine(Global.temp_path, Global.temp_path_folder, "Chunks")))
                    Directory.CreateDirectory(Path.Combine(Global.temp_path, Global.temp_path_folder, "Chunks"));
                Directory.CreateDirectory(Path.Combine(Global.temp_path, Global.temp_path_folder, "Progress"));
                // Sets Temp Settings
                SetEncoderSettings();
                // Set Video Filters
                SetVideoFilters();
                // Set Subtitle Parameters
                SetSubtitleParameters();
                // Extracts VFR Timestamps
                ExtractVFRTimeStamps();
                // Saves the Project as file
                SaveSettings(false, Global.temp_path_folder, null, null);

                if (subHardSubEnabled)
                {
                    // Reencodes the Video
                    await Task.Run(() => ReEncode());
                }

                // Split Video / Scene Detection
                SetSplitSettings();

                ProgressBar.Dispatcher.Invoke(() => ProgressBar.IsIndeterminate = true);

                LabelProgressBar.Content = "Processing Video... this might take a while!";
                await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();
                    Splitting.Split();
                }, token);

                // Set other temporary settings
                SetTempSettings();

                LabelProgressBar.Content = "Calculating Frame Count...";

                MediaInfo mediaInfo = new MediaInfo();

                if (subHardSubEnabled && Splitting.split_type != 0)
                {
                    mediaInfo.Open(Path.Combine(Global.temp_path, Global.temp_path_folder, "tmpsub.mkv"));

                    try
                    {
                        int frame_count = int.Parse(mediaInfo.Get(StreamKind.Video, 0, "FrameCount"));
                        if (frame_count != 0)
                        {
                            TotalFrames = frame_count;
                            Helpers.Logging("MediaInfo Framecount: " + frame_count.ToString());
                        }
                    }
                    catch
                    {
                        // Get Framecount from Reencoded Video
                        await Task.Run(() => { token.ThrowIfCancellationRequested(); SmallFunctions.GetSourceFrameCount(Path.Combine(Global.temp_path, Global.temp_path_folder, "tmpsub.mkv")); }, token);
                        Helpers.Logging("FFmpeg Framecount: " + TotalFrames.ToString());
                    }
                }
                else
                {
                    mediaInfo.Open(Global.Video_Path);

                    // Framecount
                    try
                    {
                        int frame_count = int.Parse(mediaInfo.Get(StreamKind.Video, 0, "FrameCount"));
                        if (frame_count != 0)
                        {
                            TotalFrames = frame_count;
                            Helpers.Logging("MediaInfo Framecount: " + frame_count.ToString());
                        }
                    }
                    catch
                    {
                        // Get Source Framecount
                        await Task.Run(() => { token.ThrowIfCancellationRequested(); SmallFunctions.GetSourceFrameCount(Global.Video_Path); }, token);
                        Helpers.Logging("FFmpeg Framecount: " + TotalFrames.ToString());
                    }
                }

                mediaInfo.Close();


                LabelProgressBar.Content = "Encoding Audio...";

                await Task.Run(() => { token.ThrowIfCancellationRequested(); EncodeAudio.Encode(AudioCommand); }, token);

                if (!File.Exists(Path.Combine(Global.temp_path, Global.temp_path_folder, "Audio", "audio.mkv")))
                {
                    // This disables audio if audio encoding failed, thus still managing to output a video in the muxing process without audio
                    Helpers.Logging("Attention: Tried to encode audio. Not audio output detected. Audio is now disabled.");
                }

                ProgressBar.Dispatcher.Invoke(() => ProgressBar.IsIndeterminate = false);

                if (subHardSubEnabled)
                {
                    // Sets the new video input
                    Global.Video_Path = Path.Combine(Global.temp_path, Global.temp_path_folder, "tmpsub.mkv");
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
                    ProgressBar.Dispatcher.Invoke(() => ProgressBar.Maximum *= 2);

                await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();
                    // Starts "a timer" for eta / fps calculation
                    DateTime starttime = DateTime.Now;
                    StartTime = starttime;
                    System.Timers.Timer aTimer = new System.Timers.Timer();
                    aTimer.Elapsed += new ElapsedEventHandler(ProgressBarUpdating);
                    aTimer.Interval = 1000;
                    aTimer.Start();

                    if (EncodeMethod <= 4)
                    {
                        EncodeVideo.Encode();
                    }
                    else
                    {
                        EncodeVideoPipe.Encode();
                    }

                    aTimer.Stop();
                }, token);

                await Task.Run(async () => { token.ThrowIfCancellationRequested(); await VideoMuxing.Concat(); }, token);
                SmallFunctions.CheckVideoOutput();

                // Progressbar Label when encoding finished
                TimeSpan timespent = DateTime.Now - StartTime;
                LabelProgressBar.Content = "Finished Encoding - Elapsed Time " + timespent.ToString("hh\\:mm\\:ss") + " - avg " + Math.Round(TotalFrames / timespent.TotalSeconds, 2) + "fps";
                Title = "NEAV1E";
                Helpers.Logging(LabelProgressBar.Content.ToString());
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
                        PopupWindow popupWindow = new PopupWindow(BaseTheme, AccentTheme, timespent.ToString("hh\\:mm\\:ss"), TotalFrames.ToString(), Math.Round(TotalFrames / timespent.TotalSeconds, 2).ToString(), Global.Video_Output);
                        popupWindow.ShowDialog();
                    }

                    // Reset Start / Pause / Resume Button
                    ImageStartPause.Source = Helpers.Get_Uri_Source("play.png");
                    LabelStartPause.Content = "Start";
                    // Set Encoding State to IDLE
                    encode_state = 0;
                }
                if (ShutdownAfterEncode && BatchEncoding == false) { Process.Start("shutdown.exe", "/s /t 0"); }
            }
            catch { SmallFunctions.PlayStopSound(); }
        }

        private void SetSplitSettings()
        {
            // Temp Arguments for Splitting / Scenedetection
            Splitting.split_type = ComboBoxSplittingMethod.SelectedIndex;
            Splitting.encode_method = ComboBoxSplittingReencodeMethod.SelectedIndex;
            Splitting.FFmpeg_Threshold = TextBoxSplittingThreshold.Text;
            Splitting.chunking_length = int.Parse(TextBoxSplittingChunkLength.Text);
            Splitting.skip_reencode = CheckBoxSkipReencode.IsChecked == true;
        }

        private void ReEncode()
        {
            // It skips reencoding if chunking method is being used
            if (Splitting.split_type != 0)
            {
                // Skips reencoding if the file already exists
                if (File.Exists(Path.Combine(Global.temp_path, Global.temp_path_folder, "tmpsub.mkv")) == false)
                {
                    ProgressBar.Dispatcher.Invoke(() => LabelProgressBar.Content = "Reencoding Video for Hardsubbing...");
                    string ffmpegCommand = "/C ffmpeg.exe -i " + '\u0022' + Global.Video_Path + '\u0022' + " " + HardSubCommand + " -map_metadata -1 -c:v libx264 -crf 0 -preset veryfast -an " + '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "tmpsub.mkv") + '\u0022';
                    Helpers.Logging("Subtitle Hardcoding Command: " + ffmpegCommand);
                    // Reencodes the Video
                    SmallFunctions.ExecuteFfmpegTask(ffmpegCommand);
                }
                else if (MessageBox.Show("The temp reencode seems to already exists!\nSkip reencoding?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    ProgressBar.Dispatcher.Invoke(() => LabelProgressBar.Content = "Reencoding Video for Hardsubbing...");
                    string ffmpegCommand = "/C ffmpeg.exe -i " + '\u0022' + Global.Video_Path + '\u0022' + " " + HardSubCommand + " -map_metadata -1 -c:v libx264 -crf 0 -preset veryfast -an " + '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "tmpsub.mkv") + '\u0022';
                    Helpers.Logging("Subtitle Hardcoding Command: " + ffmpegCommand);
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
                if (File.Exists(Path.Combine(Global.temp_path, Global.temp_path_folder, "vsync.txt")) == false)
                {
                    // Run mkvextract command
                    Process mkvToolNix = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = "cmd.exe",
                        WorkingDirectory = Global.MKVToolNix_Path,
                        Arguments = "/C mkvextract.exe " + '\u0022' + Global.Video_Path + '\u0022' + " timestamps_v2 0:" + '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "vsync.txt") + '\u0022'
                    };
                    Helpers.Logging("VSYNC Extract: " + startInfo.Arguments);
                    mkvToolNix.StartInfo = startInfo;
                    mkvToolNix.Start();
                    mkvToolNix.WaitForExit();
                }
                // Discards piping timestamp
                VSYNC = "-vsync drop";
                VFRCMD = "--timestamps 0:" + '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "vsync.txt") + '\u0022';
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
            // Sets the worker count
            if (!WorkerOverride)
            {
                EncodeVideo.Worker_Count = int.Parse(ComboBoxWorkerCount.Text);
            }
            else
            {
                EncodeVideo.Worker_Count = int.Parse(TextBoxWorkerCountOverride.Text);
            }
           
            OnePass = ComboBoxVideoPasses.SelectedIndex == 0;                           // Sets the amount of passes (true = 1, false = 2)
            EncodeVideo.Process_Priority = ComboBoxProcessPriority.SelectedIndex == 0;  // Sets the Process Priority
            SetPipeCommand();
        }

        private void SetPipeCommand()
        {
            EncodeVideo.Pixel_Format = " -pix_fmt yuv";

            if (ComboBoxColorFormat.SelectedIndex == 0) { EncodeVideo.Pixel_Format += "420p"; }
            else if (ComboBoxColorFormat.SelectedIndex == 1) { EncodeVideo.Pixel_Format += "422p"; }
            else if (ComboBoxColorFormat.SelectedIndex == 2) { EncodeVideo.Pixel_Format += "444p"; }

            if (ComboBoxVideoBitDepth.SelectedIndex == 1)
            {
                // 10bit
                EncodeVideo.Pixel_Format += "10le -strict -1";
            }
            else if (ComboBoxVideoBitDepth.SelectedIndex == 2)
            {
                // 12bit
                EncodeVideo.Pixel_Format += "12le -strict -1";
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

            if (crop || rotate || resize || deinterlace)
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
            Helpers.Logging("Filter Command: " + FilterCommand);
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

        private void GetSourceInformation(bool batch_skip)
        {
            MediaInfo mediaInfo = new MediaInfo();
            mediaInfo.Open(Global.Video_Path);

            // Video ═════════════════════════════════════

            // Duration
            try
            {
                LabelVideoLength.Content = mediaInfo.Get(StreamKind.Video, 0, "Duration/String3");
            }
            catch { }

            // Resolution
            try
            {
                if (!batch_skip)
                {
                    string width = mediaInfo.Get(StreamKind.Video, 0, "Width");
                    string height = mediaInfo.Get(StreamKind.Video, 0, "Height");
                    LabelVideoResolution.Content = width + "x" + height;
                    TextBoxFiltersResizeHeight.Text = height;
                }
            }
            catch { }

            // Framerate
            try
            {
                LabelVideoFramerate.Content = mediaInfo.Get(StreamKind.Video, 0, "FrameRate");
            }
            catch { }

            // Audio ═════════════════════════════════════

            int audio_count = mediaInfo.Count_Get(StreamKind.Audio);
            string lang;

            if (audio_count >= 1)
            {
                List<string> Languages = new List<string>();
                Dictionary<string, string>.KeyCollection keys = AudioLanguages.Keys;

                foreach (string language in keys)
                {
                    Languages.Add(language);
                }

                List<Audio.AudioTracks> items = new List<Audio.AudioTracks>();

                for (int i = 0; i < audio_count; i++)
                {
                    try
                    {
                        string name = "";
                        int channels = 1;
                        bool pcm = false;

                        // Check if Audio is PCM/Blu-Ray
                        try
                        {
                            pcm = mediaInfo.Get(StreamKind.Audio, i, "Format") == "PCM" && mediaInfo.Get(StreamKind.Audio, i, "MuxingMode") == "Blu-ray";
                        }
                        catch { }

                        // Get Audio Language
                        try
                        {
                            if (string.IsNullOrEmpty(lang = mediaInfo.Get(StreamKind.Audio, i, "Language/String3")))
                            {
                                lang = "und";
                            }
                            lang = AudioLanguages.FirstOrDefault(x => x.Value == lang).Key;
                        }
                        catch {
                            lang = AudioLanguages.FirstOrDefault(x => x.Value == "und").Key;
                        }

                        // Get Track Name
                        try
                        {
                            name = mediaInfo.Get(StreamKind.Audio, i, "Title");
                        }
                        catch { }

                        // Get Channel Layout
                        try
                        {
                            string channel = mediaInfo.Get(StreamKind.Audio, i, "Channels");
                            switch (channel)
                            {
                                case "1":
                                    channels = 0;
                                    break;
                                case "2":
                                    channels = 1;
                                    break;
                                case "6":
                                    channels = 2;
                                    break;
                                case "8":
                                    channels = 3;
                                    break;
                                default:
                                    break;
                            }
                        }
                        catch { }

                        items.Add(new Audio.AudioTracks() {
                            Active = true,
                            Index = i,
                            Codec = DefaultAudioCodec,
                            Bitrate = DefaultBitrateCodec,
                            Languages = Languages,
                            Language = lang,
                            CustomName = name,
                            Channels = channels,
                            PCM = pcm
                        });
                    }
                    catch { }
                }

                ListBoxAudioTracks.ItemsSource = items;
            }

            // Subtitles ═════════════════════════════════

            ResetSubtitles();

            int sub_count = mediaInfo.Count_Get(StreamKind.Text);
            if (sub_count >= 1)
            {
                try
                {
                    if (string.IsNullOrEmpty(lang = mediaInfo.Get(StreamKind.Text, 0, "Language/String3"))) { lang = "und"; }
                    ComboBoxSubTrackOneLanguage.SelectedItem = AudioLanguages.FirstOrDefault(x => x.Value == lang).Key;
                }
                catch { }
            }

            if (sub_count >= 2)
            {
                try
                {
                    if (string.IsNullOrEmpty(lang = mediaInfo.Get(StreamKind.Text, 1, "Language/String3"))) { lang = "und"; }
                    ComboBoxSubTrackTwoLanguage.SelectedItem = AudioLanguages.FirstOrDefault(x => x.Value == lang).Key;
                }
                catch { }
            }

            if (sub_count >= 3)
            {
                try
                {
                    if (string.IsNullOrEmpty(lang = mediaInfo.Get(StreamKind.Text, 2, "Language/String3"))) { lang = "und"; }
                    ComboBoxSubTrackThreeLanguage.SelectedItem = AudioLanguages.FirstOrDefault(x => x.Value == lang).Key;
                }
                catch { }
            }

            if (sub_count >= 4)
            {
                try
                {
                    if (string.IsNullOrEmpty(lang = mediaInfo.Get(StreamKind.Text, 3, "Language/String3"))) { lang = "und"; }
                    ComboBoxSubTrackFourLanguage.SelectedItem = AudioLanguages.FirstOrDefault(x => x.Value == lang).Key;
                }
                catch { }
            }

            if (sub_count >= 5)
            {
                try
                {
                    if (string.IsNullOrEmpty(lang = mediaInfo.Get(StreamKind.Text, 4, "Language/String3"))) { lang = "und"; }
                    ComboBoxSubTrackFiveLanguage.SelectedItem = AudioLanguages.FirstOrDefault(x => x.Value == lang).Key;
                }
                catch { }
            }

            mediaInfo.Close();
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
            SubCommand = "";

            if (ToggleSwitchSubtitleActivatedOne.IsOn == true)
            {
                // 1st Subtitle
                if (CheckBoxSubOneBurn.IsChecked == false)
                {
                    // Softsub
                    subSoftSubEnabled = true;
                    SubCommand += SoftSubCMDGenerator(AudioLanguages[ComboBoxSubTrackOneLanguage.Text], TextBoxSubOneName.Text, TextBoxSubtitleTrackOne.Text, CheckBoxSubOneDefault.IsChecked == true);
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
                    SubCommand += SoftSubCMDGenerator(AudioLanguages[ComboBoxSubTrackTwoLanguage.Text], TextBoxSubTwoName.Text, TextBoxSubtitleTrackTwo.Text, CheckBoxSubTwoDefault.IsChecked == true);
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
                    SubCommand += SoftSubCMDGenerator(AudioLanguages[ComboBoxSubTrackThreeLanguage.Text], TextBoxSubThreeName.Text, TextBoxSubtitleTrackThree.Text, CheckBoxSubThreeDefault.IsChecked == true);
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
                    SubCommand += SoftSubCMDGenerator(AudioLanguages[ComboBoxSubTrackFourLanguage.Text], TextBoxSubFourName.Text, TextBoxSubtitleTrackFour.Text, CheckBoxSubFourDefault.IsChecked == true);
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
                    SubCommand += SoftSubCMDGenerator(AudioLanguages[ComboBoxSubTrackFiveLanguage.Text], TextBoxSubFiveName.Text, TextBoxSubtitleTrackFive.Text, CheckBoxSubFiveDefault.IsChecked == true);
                }
                else { HardSubCMDGenerator(TextBoxSubtitleTrackFive.Text); }
            }
            Helpers.Logging("Subtitle Command: " + SubCommand);
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
                HardSubCommand = "-vf ass=" + '\u0022' + subInput + '\u0022';
                HardSubCommand = HardSubCommand.Replace("\u005c", "\u005c\u005c\u005c\u005c");
                HardSubCommand = HardSubCommand.Replace(":", "\u005c\u005c\u005c:");
            }
            else if (ext == ".srt")
            {
                HardSubCommand = "-vf subtitles=" + '\u0022' + subInput + '\u0022';
                HardSubCommand = HardSubCommand.Replace("\u005c", "\u005c\u005c\u005c\u005c");
                HardSubCommand = HardSubCommand.Replace(":", "\u005c\u005c\u005c:");
            }
            // Theoretically PGS Subtitles can be hardcoded
            // The Problem is that ffmpeg disregards the potential offset
            // thus resulting in too early appearing subtitles
            // Softsub works, as it is handled by mkvmerge and not ffmpeg
            subHardSubEnabled = true;
        }

        public void GetSubtitleTracks()
        {
            if (!SkipSubtitleExtraction)
            {
                MediaInfo mediaInfo = new MediaInfo();
                mediaInfo.Open(Global.Video_Path);

                List<string> subs_mediainfo = new List<string>();

                foreach (int index in Enumerable.Range(0, 5))
                {
                    try
                    {
                        subs_mediainfo.Add(mediaInfo.Get(StreamKind.Text, index, "Format"));
                    }
                    catch { }
                }

                mediaInfo.Close();

                //Creates Audio Directory in the temp dir
                if (!Directory.Exists(Path.Combine(Global.temp_path, Global.temp_path_folder, "Subtitles")) && subs_mediainfo.Any())
                    Directory.CreateDirectory(Path.Combine(Global.temp_path, Global.temp_path_folder, "Subtitles"));

                int a = 0;
                int b = 0;
                //Iterates over the lines from the splitted output
                foreach (string line in subs_mediainfo)
                {
                    if (line.Contains("PGS") || line.Contains("ASS") || line.Contains("SSA") || line.Contains("UTF-8") || line.Contains("VobSub"))
                    {
                        string tempName = "";
                        Process process = new Process();
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            WindowStyle = ProcessWindowStyle.Hidden,
                            UseShellExecute = true,
                            FileName = "cmd.exe",
                            WorkingDirectory = Global.FFmpeg_Path
                        };

                        if (line.Contains("PGS"))
                        {
                            startInfo.Arguments = "/C ffmpeg.exe -y -i " + '\u0022' + Global.Video_Path + '\u0022' + " -map 0:s:" + a + " -c:s copy " + '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "Subtitles", "pgs_" + b + ".sup") + '\u0022';
                            tempName = Path.Combine(Global.temp_path, Global.temp_path_folder, "Subtitles", "pgs_" + b + ".sup");
                        }
                        else if (line.Contains("ASS"))
                        {
                            startInfo.Arguments = "/C ffmpeg.exe -y -i " + '\u0022' + Global.Video_Path + '\u0022' + " -map 0:s:" + a + " -c:s copy " + '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "Subtitles", "ass_" + b + ".ass") + '\u0022';
                            tempName = Path.Combine(Global.temp_path, Global.temp_path_folder, "Subtitles", "ass_" + b + ".ass");
                        }
                        else if (line.Contains("UTF-8"))
                        {
                            startInfo.Arguments = "/C ffmpeg.exe -y -i " + '\u0022' + Global.Video_Path + '\u0022' + " -map 0:s:" + a + " -c:s copy " + '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "Subtitles", "subrip_" + b + ".srt") + '\u0022';
                            tempName = Path.Combine(Global.temp_path, Global.temp_path_folder, "Subtitles", "subrip_" + b + ".srt");
                        }
                        else if (line.Contains("SSA"))
                        {
                            startInfo.Arguments = "/C ffmpeg.exe -y -i " + '\u0022' + Global.Video_Path + '\u0022' + " -map 0:s:" + a + " -c:s copy " + '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "Subtitles", "ssa_" + b + ".ssa") + '\u0022';
                            tempName = Path.Combine(Global.temp_path, Global.temp_path_folder, "Subtitles", "ssa_" + b + ".ssa");
                        }
                        else if (line.Contains("VobSub"))
                        {
                            startInfo.Arguments = "/C ffmpeg.exe -y -i " + '\u0022' + Global.Video_Path + '\u0022' + " -map 0:s:" + a + " -c:s copy " + '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "Subtitles", "dvdsub_" + b + ".mkv") + '\u0022';
                        }

                        process.StartInfo = startInfo;
                        process.Start();
                        process.WaitForExit();

                        if (line.Contains("VobSub") == true)
                        {
                            // Extract dvdsub from mkv with mkvextract
                            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                            startInfo.UseShellExecute = true;
                            startInfo.FileName = "cmd.exe";
                            startInfo.WorkingDirectory = Global.MKVToolNix_Path;

                            startInfo.Arguments = "/C mkvextract.exe " + '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "Subtitles", "dvdsub_" + b + ".mkv") + '\u0022' + " tracks 0:" + '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "Subtitles", "dvdsub_" + b + ".sub") + '\u0022';

                            process.StartInfo = startInfo;
                            process.Start();
                            process.WaitForExit();

                            // Convert dvdsub to bluraysub
                            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                            startInfo.UseShellExecute = true;
                            startInfo.FileName = "cmd.exe";
                            startInfo.WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "bdsup2sub");

                            startInfo.Arguments = "/C bdsup2sub.exe -o " + '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "Subtitles", "pgs_dvd_" + b + ".sup") + '\u0022' + " " + '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "Subtitles", "dvdsub_" + b + ".sub") + '\u0022';
                            process.StartInfo = startInfo;
                            process.Start();
                            process.WaitForExit();

                            // Cleanup
                            if (File.Exists(Path.Combine(Global.temp_path, Global.temp_path_folder, "Subtitles", "pgs_dvd_" + b + ".sup")))
                            {
                                try
                                {
                                    File.Delete(Path.Combine(Global.temp_path, Global.temp_path_folder, "Subtitles", "dvdsub_" + b + ".sub"));
                                    File.Delete(Path.Combine(Global.temp_path, Global.temp_path_folder, "Subtitles", "dvdsub_" + b + ".idx"));
                                    File.Delete(Path.Combine(Global.temp_path, Global.temp_path_folder, "Subtitles", "dvdsub_" + b + ".mkv"));
                                }
                                catch { }
                            }

                            tempName = Path.Combine(Global.temp_path, Global.temp_path_folder, "Subtitles", "pgs_dvd_" + b + ".sup");
                        }

                        //Sets the ToggleSwitches
                        if (Path.GetExtension(Global.Video_Output) != ".mp4")
                        {
                            if (b == 0) { ToggleSwitchSubtitleActivatedOne.IsOn = true; }
                            if (b == 1) { ToggleSwitchSubtitleActivatedTwo.IsOn = true; }
                            if (b == 2) { ToggleSwitchSubtitleActivatedThree.IsOn = true; }
                            if (b == 3) { ToggleSwitchSubtitleActivatedFour.IsOn = true; }
                            if (b == 4) { ToggleSwitchSubtitleActivatedFive.IsOn = true; }
                        }

                        try
                        {
                            if (b == 0) { TextBoxSubtitleTrackOne.Text = tempName; }
                            if (b == 1) { TextBoxSubtitleTrackTwo.Text = tempName; }
                            if (b == 2) { TextBoxSubtitleTrackThree.Text = tempName; }
                            if (b == 3) { TextBoxSubtitleTrackFour.Text = tempName; }
                            if (b == 4) { TextBoxSubtitleTrackFive.Text = tempName; }
                        }
                        catch { }
                        b++;
                    }
                    a++;
                }
                if (VideoOutputSet && Path.GetExtension(Global.Video_Output) == ".mp4")
                {
                    ToggleSwitchSubtitleActivatedOne.IsOn = false;
                    ToggleSwitchSubtitleActivatedTwo.IsOn = false;
                    ToggleSwitchSubtitleActivatedThree.IsOn = false;
                    ToggleSwitchSubtitleActivatedFour.IsOn = false;
                    ToggleSwitchSubtitleActivatedFive.IsOn = false;
                }
            } 
        }

        // ══════════════════════════════════ Encoder Settings ════════════════════════════════════

        private void SetEncoderSettings()
        {
            int selected_encoder = ComboBoxVideoEncoder.SelectedIndex;

            // Sets the encoder (0 libaom; 1 librav1e; 2 libsvt-av1; 3 libvpx-vp9; 5 aomenc; 6 rav1e; 7 svt-av1)
            EncodeMethod = selected_encoder;

            if (CheckBoxCustomVideoSettings.IsChecked == false)
            {
                // Sets the Encoder Settings
                if (selected_encoder == 0)
                {
                    EncodeVideo.Final_Encoder_Command = " -c:v libaom-av1 " + SetLibAomCommand();
                    Helpers.Logging("Libaom Settings : " + EncodeVideo.Final_Encoder_Command);
                }
                if (selected_encoder == 1)
                {
                    EncodeVideo.Final_Encoder_Command = " -c:v librav1e " + SetLibRav1eCommand();
                    Helpers.Logging("Rav1e Settings : " + EncodeVideo.Final_Encoder_Command);
                }
                if (selected_encoder == 2)
                {
                    EncodeVideo.Final_Encoder_Command = " -c:v libsvtav1 " + SetLibSvtAV1Command();
                    Helpers.Logging("SVT-AV1 Settings : " + EncodeVideo.Final_Encoder_Command);
                }
                if (selected_encoder == 3)
                {
                    EncodeVideo.Final_Encoder_Command = " -c:v libvpx-vp9 " + SetVP9Command();
                    Helpers.Logging("VP9 Settings : " + EncodeVideo.Final_Encoder_Command);
                }
                if (selected_encoder == 5)
                {
                    EncodeVideo.Final_Encoder_Command = SetAomencCommand();
                    Helpers.Logging("Aomenc Settings : " + EncodeVideo.Final_Encoder_Command);
                }
                if (selected_encoder == 6)
                {
                    EncodeVideo.Final_Encoder_Command = SetRav1eCommand();
                    Helpers.Logging("Rav1e Settings : " + EncodeVideo.Final_Encoder_Command);
                }
                if (selected_encoder == 7)
                {
                    EncodeVideo.Final_Encoder_Command = SetSvtAV1Command();
                    Helpers.Logging("SVT-AV1 Settings : " + EncodeVideo.Final_Encoder_Command);
                }
            }
            else
            {
                // Custom Encoding Settings
                if (selected_encoder == 0)
                {
                    EncodeVideo.Final_Encoder_Command = " -c:v libaom-av1 " + TextBoxCustomVideoSettings.Text;
                    Helpers.Logging("Aomenc Custom Settings : " + EncodeVideo.Final_Encoder_Command);
                }
                if (selected_encoder == 1)
                {
                    EncodeVideo.Final_Encoder_Command = " -c:v librav1e " + TextBoxCustomVideoSettings.Text;
                    Helpers.Logging("Rav1e Custom Settings : " + EncodeVideo.Final_Encoder_Command);
                }
                if (selected_encoder == 2)
                {
                    EncodeVideo.Final_Encoder_Command = " -c:v libsvtav1 " + TextBoxCustomVideoSettings.Text;
                    Helpers.Logging("SVT-AV1 Custom Settings : " + EncodeVideo.Final_Encoder_Command);
                }
                if (selected_encoder == 3)
                {
                    EncodeVideo.Final_Encoder_Command = " -c:v libvpx-vp9 " + TextBoxCustomVideoSettings.Text;
                    Helpers.Logging("VP9 Custom Settings : " + EncodeVideo.Final_Encoder_Command);
                }
                if (selected_encoder == 5)
                {
                    EncodeVideo.Final_Encoder_Command = TextBoxCustomVideoSettings.Text;
                    Helpers.Logging("Aomenc Settings : " + EncodeVideo.Final_Encoder_Command);
                }
                if (selected_encoder == 6)
                {
                    EncodeVideo.Final_Encoder_Command = TextBoxCustomVideoSettings.Text;
                    Helpers.Logging("Rav1e Settings : " + EncodeVideo.Final_Encoder_Command);
                }
                if (selected_encoder == 7)
                {
                    EncodeVideo.Final_Encoder_Command = TextBoxCustomVideoSettings.Text;
                    Helpers.Logging("SVT-AV1 Settings : " + EncodeVideo.Final_Encoder_Command);
                }
            }
        }

        private string SetLibAomCommand()
        {
            // Aomenc Command
            string cmd = "";

            cmd += " -cpu-used " + SliderVideoSpeed.Value;         // Speed

            // Constant Quality or Target Bitrate
            if (RadioButtonVideoConstantQuality.IsChecked == true)
            {
                cmd += " -b:v 0 -crf " + SliderVideoQuality.Value;
            }
            else if (RadioButtonVideoBitrate.IsChecked == true)
            {
                cmd += " -b:v " + TextBoxVideoBitrate.Text + "k";
            }

            if (ToggleSwitchAdvancedVideoSettings.IsOn == false)
            {
                // Default params when User don't select advanced settings
                cmd += " -threads 4 -tile-columns 2 -tile-rows 1";
            }
            else
            {
                // Advanced Settings
                cmd += " -threads " + ComboBoxAomencThreads.Text;                                      // Threads
                cmd += " -tile-columns " + ComboBoxAomencTileColumns.Text;                             // Tile Columns
                cmd += " -tile-rows " + ComboBoxAomencTileRows.Text;                                   // Tile Rows
                cmd += " -lag-in-frames " + TextBoxAomencLagInFrames.Text;                             // Lag in Frames
                cmd += " -aq-mode " + ComboBoxAomencAQMode.SelectedIndex;                              // AQ-Mode
                cmd += " -tune " + ComboBoxAomencTune.Text;                                            // Tune

                if (TextBoxAomencMaxGOP.Text != "0")
                {
                    cmd += " -g " + TextBoxAomencMaxGOP.Text;                                           // Keyframe Interval
                }
                if (CheckBoxAomencRowMT.IsChecked == false)
                {
                    cmd += " -row-mt 0";                                                                // Row Based Multithreading
                }
                if (CheckBoxAomencCDEF.IsChecked == false)
                {
                    cmd += " -enable-cdef 0";                                                           // Constrained Directional Enhancement Filter
                }

                if (CheckBoxAomencARNRMax.IsChecked == true)
                {
                    cmd += " -arnr-max-frames " + ComboBoxAomencARNRMax.Text;                           // ARNR Maxframes
                    cmd += " -arnr-strength " + ComboBoxAomencARNRStrength.Text;                        // ARNR Strength
                }
                if (CheckBoxVideoAomencRealTime.IsChecked == true)
                {
                    cmd += " -usage realtime ";                                                         // Real Time Mode
                }

                cmd += " -aom-params ";
                cmd += " tune-content=" + ComboBoxAomencTuneContent.Text;                             // Tune-Content
                cmd += ":sharpness=" + ComboBoxAomencSharpness.Text;                                  // Sharpness (Filter)
                cmd += ":enable-keyframe-filtering=" + ComboBoxAomencKeyFiltering.SelectedIndex;      // Key Frame Filtering
                if (ComboBoxAomencColorPrimaries.SelectedIndex != 0)
                {
                    cmd += ":color-primaries=" + ComboBoxAomencColorPrimaries.Text;                   // Color Primaries
                }
                if (ComboBoxAomencColorTransfer.SelectedIndex != 0)
                {
                    cmd += ":transfer-characteristics=" + ComboBoxAomencColorTransfer.Text;           // Color Transfer
                }
                if (ComboBoxAomencColorMatrix.SelectedIndex != 0)
                {
                    cmd += ":matrix-coefficients=" + ComboBoxAomencColorMatrix.Text;                  // Color Matrix
                }
            }

            return cmd;
        }

        private string SetLibRav1eCommand()
        {
            // Rav1e Command
            string cmd = "";
            cmd += " -speed " + SliderVideoSpeed.Value;    // Speed

            // Constant Quality or Target Bitrate
            if (RadioButtonVideoConstantQuality.IsChecked == true)
            {
                cmd += " -qp " + SliderVideoQuality.Value;
            }
            else if (RadioButtonVideoBitrate.IsChecked == true)
            {
                cmd += " -b:v " + TextBoxVideoBitrate.Text + "k";
            }

            if (ToggleSwitchAdvancedVideoSettings.IsOn == false)
            {
                // Default params when User don't select advanced settings
                cmd += " -tile-columns 2 -tile-rows 1 -rav1e-params threads=4";
            }
            else
            {
                // Kinda disappointing that only ~4 options are really implemented in ffmpeg
                cmd += " -tile-columns " + ComboBoxRav1eTileColumns.SelectedIndex;                     // Tile Columns
                cmd += " -tile-rows " + ComboBoxRav1eTileRows.SelectedIndex;                           // Tile Rows

                cmd += " -rav1e-params ";
                cmd += "threads=" + ComboBoxRav1eThreads.SelectedIndex;                                 // Threads
                cmd += ":rdo-lookahead-frames=" + TextBoxRav1eLookahead.Text;                           // RDO Lookahead
                cmd += ":tune=" + ComboBoxRav1eTune.Text;                                               // Tune
                if (TextBoxRav1eMaxGOP.Text != "0")
                {
                    cmd += ":keyint=" + TextBoxRav1eMaxGOP.Text;                                        // Keyframe Interval
                }
                if (ComboBoxRav1eColorPrimaries.SelectedIndex != 0)
                {
                    cmd += ":primaries=" + ComboBoxRav1eColorPrimaries.Text;                            // Color Primaries
                }
                if (ComboBoxRav1eColorTransfer.SelectedIndex != 0)
                {
                    cmd += ":transfer=" + ComboBoxRav1eColorTransfer.Text;                              // Color Transfer
                }
                if (ComboBoxRav1eColorMatrix.SelectedIndex != 0)
                {
                    cmd += ":matrix=" + ComboBoxRav1eColorMatrix.Text;                                  // Color Matrix
                }
                if (CheckBoxRav1eMasteringDisplay.IsChecked == true)
                {
                    cmd += ":mastering-display=G(" + TextBoxRav1eMasteringGx.Text + ",";                // Mastering Gx
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
                    cmd += ":content-light=" + TextBoxRav1eContentLightCll.Text;                        // Content Light CLL
                    cmd += "," + TextBoxRav1eContentLightFall.Text;                                     // Content Light FALL
                }
            }

            return cmd;
        }

        private string SetLibSvtAV1Command()
        {
            string cmd = "";
            cmd += " -preset " + SliderVideoSpeed.Value;

            // Constant Quality or Target Bitrate
            if (RadioButtonVideoConstantQuality.IsChecked == true)
            {
                cmd += " -rc 0 -qp " + SliderVideoQuality.Value;
            }
            else if (RadioButtonVideoBitrate.IsChecked == true)
            {
                cmd += " -rc 1 -b:v " + TextBoxVideoBitrate.Text + "k";
            }

            if (ToggleSwitchAdvancedVideoSettings.IsOn == true)
            {
                cmd += " -tile_columns " + ComboBoxSVTAV1TileColumns.Text;                              // Tile Columns
                cmd += " -tile_rows " + ComboBoxSVTAV1TileRows.Text;                                    // Tile Rows
                cmd += " -g " + TextBoxSVTAV1MaxGOP.Text;                                               // Keyframe Interval
                cmd += " -la_depth " + TextBoxSVTAV1Lookahead.Text;                                     // Lookahead
            }

            return cmd;
        }

        private string SetVP9Command()
        {
            string cmd = "";

            cmd += " -cpu-used " + SliderVideoSpeed.Value;         // Speed

            // Constant Quality or Target Bitrate
            if (RadioButtonVideoConstantQuality.IsChecked == true) { cmd += " -b:v 0 -crf " + SliderVideoQuality.Value; }
            else if (RadioButtonVideoBitrate.IsChecked == true) { cmd += " -b:v " + TextBoxVideoBitrate.Text + "k"; }

            if (ToggleSwitchAdvancedVideoSettings.IsOn == false)
            {
                // Default params when User don't select advanced settings
                cmd += " -threads 4 -tile-columns 2 -tile-rows 1";
            }
            else
            {
                cmd += " -threads " + ComboBoxVP9Threads.Text;                      // Max Threads
                cmd += " -tile-columns " + ComboBoxVP9TileColumns.SelectedIndex;    // Tile Columns
                cmd += " -tile-rows " + ComboBoxVP9TileRows.SelectedIndex;          // Tile Rows
                cmd += " -lag-in-frames " + TextBoxVP9LagInFrames.Text;             // Lag in Frames
                cmd += " -g " + TextBoxVP9MaxKF.Text;                               // Max GOP
                cmd += " -aq-mode " + ComboBoxVP9AQMode.SelectedIndex;              // AQ-Mode
                cmd += " -tune " + ComboBoxVP9ATune.SelectedIndex;                  // Tune
                cmd += " -tune-content " + ComboBoxVP9ATuneContent.SelectedIndex;   // Tune-Content
                if (CheckBoxVP9ARNR.IsChecked == true)
                {
                    cmd += " -arnr-maxframes " + ComboBoxAomencVP9Max.Text;        // ARNR Max Frames
                    cmd += " -arnr-strength " + ComboBoxAomencVP9Strength.Text;    // ARNR Strength
                    cmd += " -arnr-type " + ComboBoxAomencVP9ARNRType.Text;        // ARNR Type
                }
            }

            return cmd;
        }

        private string SetAomencCommand()
        {
            string cmd = "";
            cmd += " --bit-depth=" + ComboBoxVideoBitDepth.Text;
            cmd += " --cpu-used=" + SliderVideoSpeed.Value;
            if (RadioButtonVideoConstantQuality.IsChecked == true) { cmd += " --end-usage=q --cq-level=" + SliderVideoQuality.Value; }
            else if (RadioButtonVideoBitrate.IsChecked == true) { cmd += " --end-usage=vbr --target-bitrate=" + TextBoxVideoBitrate.Text; }
            if (ToggleSwitchAdvancedVideoSettings.IsOn == false)
            {
                cmd += " --threads=4 --tile-columns=2 --tile-rows=1";
            }
            else
            {
                cmd += " --threads=" + ComboBoxAomencThreads.Text;                                      // Threads
                cmd += " --tile-columns=" + ComboBoxAomencTileColumns.Text;                             // Tile Columns
                cmd += " --tile-rows=" + ComboBoxAomencTileRows.Text;                                   // Tile Rows
                cmd += " --lag-in-frames=" + TextBoxAomencLagInFrames.Text;                             // Lag in Frames
                cmd += " --sharpness=" + ComboBoxAomencSharpness.Text;                                  // Sharpness (Filter)
                cmd += " --aq-mode=" + ComboBoxAomencAQMode.SelectedIndex;                              // AQ-Mode
                cmd += " --enable-keyframe-filtering=" + ComboBoxAomencKeyFiltering.SelectedIndex;      // Key Frame Filtering
                cmd += " --tune=" + ComboBoxAomencTune.Text;                                            // Tune
                cmd += " --tune-content=" + ComboBoxAomencTuneContent.Text;                             // Tune-Content
            }
            if (TextBoxAomencMaxGOP.Text != "0")
            {
                cmd += " --kf-max-dist=" + TextBoxAomencMaxGOP.Text;                                // Keyframe Interval
            }
            if (CheckBoxAomencRowMT.IsChecked == false)
            {
                cmd += " --row-mt=0";                                                               // Row Based Multithreading
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
            if (CheckBoxAomencCDEF.IsChecked == false)
            {
                cmd += " --enable-cdef=0";                                                          // Constrained Directional Enhancement Filter
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

            if (RadioButtonVideoConstantQuality.IsChecked == true) { cmd += " --rc 0 -q " + SliderVideoQuality.Value; }
            else if (RadioButtonVideoBitrate.IsChecked == true) { cmd += " --rc 1 --tbr " + TextBoxVideoBitrate.Text; }

            if (ToggleSwitchAdvancedVideoSettings.IsOn == true)
            {
                cmd += " --tile-columns " + ComboBoxSVTAV1TileColumns.Text;                             // Tile Columns
                cmd += " --tile-rows " + ComboBoxSVTAV1TileRows.Text;                                   // Tile Rows
                cmd += " --keyint " + TextBoxSVTAV1MaxGOP.Text;                                         // Keyframe Interval
                cmd += " --lookahead " + TextBoxSVTAV1Lookahead.Text;                                   // Lookahead
            }

            return cmd;
        }

        // ══════════════════════════════════════ Buttons ═════════════════════════════════════════

        private async void ButtonTestCustomSettings_Click(object sender, RoutedEventArgs e)
        {
            // Testing Custom Settings
            ProgressBar.IsIndeterminate = true;
            LabelProgressBar.Content = "Testing... Please wait.";

            int encoder_index = ComboBoxVideoEncoder.SelectedIndex;
            string test_command = TextBoxCustomVideoSettings.Text;
            int exit_code = await Task.Run(() => Test_Encode(encoder_index, test_command));

            if (exit_code == 0)
            {
                LabelProgressBar.Content = "Test was Successfull.";
            }
            else
            {
                LabelProgressBar.Content = "Test Terminated with Error Code: " + exit_code.ToString() + " - Invalid settings?";
            }
            ProgressBar.IsIndeterminate = false;
        }

        private int Test_Encode(int encoder_index, string command)
        {
            // Test Video
            string input_test = " -y -i " + '\u0022' + Path.Combine(Directory.GetCurrentDirectory(), "sample", "test_sample.mp4") + '\u0022';

            Process ffmpegProcess = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = "cmd.exe",
                WorkingDirectory = Global.FFmpeg_Path,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            string test_command = " -t 00:00.30 -pix_fmt yuv420p";
            if (encoder_index <= 4)
            {
                // Internal Encoders
                if (encoder_index == 0)
                {
                    test_command += " -c:v libaom-av1 " + command;
                }
                if (encoder_index == 1)
                {
                    test_command += " -c:v librav1e " + command;
                }
                if (encoder_index == 2)
                {
                    test_command += " -c:v libsvtav1 " + command;
                }
                if (encoder_index == 3)
                {
                    test_command += " -c:v libvpx-vp9 " + command;
                }

                test_command += " " + '\u0022' + Path.Combine(Directory.GetCurrentDirectory(), "sample", "test_sample_out.webm") + '\u0022';
            }
            else
            {
                // External Encoders
                if (encoder_index == 5)
                {
                    test_command += " -color_range 0 -vsync 0 -f yuv4mpegpipe - | ";
                    test_command += '\u0022' + Path.Combine(Global.Aomenc_Path, "aomenc.exe") + '\u0022' + " - --passes=1 " + command + " --output=";
                }
                if (encoder_index == 6)
                {
                    test_command += " -color_range 0 -vsync 0 -f yuv4mpegpipe - | ";
                    test_command += '\u0022' + Path.Combine(Global.Rav1e__Path, "rav1e.exe") + '\u0022' + " - -y " + command + " --output ";
                }
                if (encoder_index == 7)
                {
                    test_command += " -color_range 0 -vsync 0 -nostdin -f yuv4mpegpipe - | ";
                    test_command += '\u0022' + Path.Combine(Global.SvtAv1_Path, "SvtAv1EncApp.exe") + '\u0022' + " -i stdin " + command + " --passes 1 -b ";
                }

                test_command += '\u0022' + Path.Combine(Directory.GetCurrentDirectory(), "sample", "test_sample_out.ivf") + '\u0022';
            }

            startInfo.Arguments = "/C ffmpeg.exe " + input_test + test_command;

            ffmpegProcess.StartInfo = startInfo;
            ffmpegProcess.Start();
            ffmpegProcess.WaitForExit();

            return ffmpegProcess.ExitCode;
        }

        private void SetBackGroundColor()
        {
            if (BaseTheme == "Light")
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

        private void ButtonSavePreset_Click(object sender, RoutedEventArgs e)
        {
            // Creates a new SavePreset Window
            SavePreset savePreset = new SavePreset(BaseTheme, AccentTheme);
            // Displays the Window and awaits exit
            savePreset.ShowDialog();
            // Gets the Data from the SavePreset Window
            string result = savePreset.SaveName;
            string codec = savePreset.AudioCodec;
            string bitrate = savePreset.AudioBitrate;
            bool cancel = savePreset.Cancel;
            if (cancel == false)
            {
                // Saves a new preset
                SaveSettings(true, result, codec, bitrate);
            }
            LoadPresetsIntoComboBox();
        }

        private void ButtonOpenSource_Click(object sender, RoutedEventArgs e)
        {
            // Creates a new object of the type "OpenVideoWindow"
            OpenVideoWindow WindowVideoSource = new OpenVideoWindow(BaseTheme, AccentTheme);

            // Shows the just created window object and awaits its exit
            WindowVideoSource.ShowDialog();

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
            else if (batchFolder == false && resultProject)
            {
                // Project File
                if (WindowVideoSource.QuitCorrectly)
                {
                    BatchEncoding = false;
                    LoadSettings(true, result);
                    AutoSetBitDepthAndColorFormat(Global.Video_Path);
                }
            }
            else if (batchFolder == true && resultProject == false)
            {
                // Batch Folder Input
                if (WindowVideoSource.QuitCorrectly)
                {
                    VideoInputSet = true;
                    TextBoxVideoSource.Text = Global.Video_Path = result;
                    BatchEncoding = true;
                }
            }
        }

        private void ButtonOpenDestination_Click(object sender, RoutedEventArgs e)
        {
            if (BatchEncoding == false)
            {
                // Save File Dialog for single file saving
                SaveFileDialog saveVideoFileDialog = new SaveFileDialog
                {
                    Filter = "Video|*.mkv;*.webm;*.mp4"
                };
                // Avoid NULL being returned resulting in crash
                Nullable<bool> result = saveVideoFileDialog.ShowDialog();
                if (result == true)
                {
                    Global.Video_Output = TextBoxVideoDestination.Text = saveVideoFileDialog.FileName;
                    VideoOutputSet = true;
                    if (Path.GetExtension(Global.Video_Output) == ".mp4")
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
                    TextBoxVideoDestination.Text = Global.Video_Output = browseOutputFolder.SelectedPath;
                    VideoOutputSet = true;
                }
            }
        }

        // Current State of Program - 0 = IDLE ; 1 = Encoding ; 2 = Paused
        public static int encode_state = 0;

        private void ButtonStartEncode_Click(object sender, RoutedEventArgs e)
        {
            if (VideoInputSet && VideoOutputSet)
            {
                if (encode_state == 0 || encode_state == 2)
                {
                    ImageStartPause.Source = Helpers.Get_Uri_Source("pause.png");
                    LabelStartPause.Content = "Pause";

                    if (encode_state == 0)
                    {
                        // Main Entry
                        PreStart();
                    }

                    if (encode_state == 2)
                    {
                        // Resume all PIDs
                        foreach (int pid in Global.Launched_PIDs)
                        {
                            Resume.ResumeProcessTree(pid);
                        }
                    }

                    // Set as encoding
                    encode_state = 1;
                }
                else if (encode_state == 1)
                {
                    ImageStartPause.Source = Helpers.Get_Uri_Source("resume.png");
                    LabelStartPause.Content = "Resume";

                    // Pause all PIDs
                    foreach (int pid in Global.Launched_PIDs)
                    {
                        Suspend.SuspendProcessTree(pid);
                    }

                    // Set as paused
                    encode_state = 2;
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
            if (encode_state != 0)
            {
                // Sets the global Cancel Boolean
                SmallFunctions.Cancel.CancelAll = true;
                // Invokes Cancellationtoken cancel
                cancellationTokenSource.Cancel();
                // Kills all encoder instances
                Kill.Kill_PID();
                // Sets that the encode has been finished
                encode_state = 0;
                ImageStartPause.Source = Helpers.Get_Uri_Source("play.png");
                LabelStartPause.Content = "Start";
                // Cancel UI Routine
                CancelRoutine();
            }
            else
            {
                SmallFunctions.PlayStopSound();
                MessageBox.Show("Encode has not started yet!", "Attention", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // ═══════════════════════════════════ Progress Bar ═══════════════════════════════════════

        private void ProgressBarUpdating(object sender, EventArgs e)
        {
            // Gets all Progress Files of ffmpeg
            string[] filePaths = Directory.GetFiles(Path.Combine(Global.temp_path, Global.temp_path_folder, "Progress"), "*.log", SearchOption.AllDirectories);

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
                string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                string tempvalue = "";

                // Iterates over all lines
                foreach (string line in lines)
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
                    fileName = "Encoding: " + Global.temp_path_folder + " - ";
                LabelProgressBar.Dispatcher.Invoke(() => LabelProgressBar.Content = fileName + totalencodedframes + " / " + totalframes + " Frames - avg " + Math.Round(totalencodedframes / timespent.TotalSeconds, 2) + "fps - Elapsed Time " + timespent.ToString("hh\\:mm\\:ss") + " - " + ((decimal)totalencodedframes / (decimal)totalframes).ToString("0.00%"));
                this.Dispatcher.Invoke(() => Title = "NEAV1E - " + ((decimal)totalencodedframes / (decimal)totalframes).ToString("0.00%"));
                ProgressBar.Dispatcher.Invoke(() => ProgressBar.Value = totalencodedframes);
            }
            catch { }
        }

        // ═════════════════════════════════════ Presets IO ═══════════════════════════════════════

        private void LoadSettingsTab()
        {
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "settings.xml")))
            {
                int BaseTheme = 0;
                int AccentTheme = 0;
                int BatchContainer = 0;
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(Path.Combine(Directory.GetCurrentDirectory(), "settings.xml"));
                    XmlNodeList node = doc.GetElementsByTagName("Settings");
                    foreach (XmlNode n in node[0].ChildNodes)
                    {
                        switch (n.Name)
                        {
                            case "DeleteTempFiles":
                                DeleteTempFiles = n.InnerText == "True";
                                break;
                            case "PlaySound":
                                PlayUISounds = n.InnerText == "True";
                                break;
                            case "Logging":
                                Logging = n.InnerText == "True";
                                break;
                            case "ShowDialog":
                                PopupWindow = n.InnerText == "True";
                                break;
                            case "Shutdown":
                                ShutdownAfterEncode = n.InnerText == "True";
                                break;
                            case "TempPathActive":
                                UseCustomTempPath = n.InnerText == "True";
                                break;
                            case "TempPath":
                                CustomTempPath = n.InnerText;
                                break;
                            case "Terminal":
                                EncodeVideo.Show_Terminal = n.InnerText == "False";
                                break;
                            case "ThemeAccent":
                                AccentTheme = int.Parse(n.InnerText);
                                break;
                            case "ThemeBase":
                                BaseTheme = int.Parse(n.InnerText);
                                break;
                            case "BatchContainer":
                                BatchContainer = int.Parse(n.InnerText);
                                break;
                            case "ReencodeMessage":
                                reencodeMessage = n.InnerText == "True";
                                break;
                            case "Language":
                                UiLanguage = n.InnerText;
                                break;
                            case "SkipSubtitles":
                                SkipSubtitleExtraction = n.InnerText == "True";
                                break;
                            case "OverrideWorkerCount":
                                WorkerOverride = n.InnerText == "True";
                                break;
                            default: break;
                        }
                    }
                }
                catch { }

                switch (BaseTheme)
                {
                    case 0:
                        this.BaseTheme = "Light";
                        break;
                    case 1:
                        this.BaseTheme = "Dark";
                        break;
                    default:
                        this.BaseTheme = "Light";
                        break;
                }

                switch (AccentTheme)
                {
                    case 0:
                        this.AccentTheme = "Blue";
                        break;
                    case 1:
                        this.AccentTheme = "Red";
                        break;
                    case 2:
                        this.AccentTheme = "Green";
                        break;
                    case 3:
                        this.AccentTheme = "Purple";
                        break;
                    case 4:
                        this.AccentTheme = "Orange";
                        break;
                    case 5:
                        this.AccentTheme = "Lime";
                        break;
                    case 6:
                        this.AccentTheme = "Emerald";
                        break;
                    case 7:
                        this.AccentTheme = "Teal";
                        break;
                    case 8:
                        this.AccentTheme = "Cyan";
                        break;
                    case 9:
                        this.AccentTheme = "Cobalt";
                        break;
                    case 10:
                        this.AccentTheme = "Indigo";
                        break;
                    case 11:
                        this.AccentTheme = "Violet";
                        break;
                    case 12:
                        this.AccentTheme = "Pink";
                        break;
                    case 13:
                        this.AccentTheme = "Magenta";
                        break;
                    case 14:
                        this.AccentTheme = "Crimson";
                        break;
                    case 15:
                        this.AccentTheme = "Amber";
                        break;
                    case 16:
                        this.AccentTheme = "Yellow";
                        break;
                    case 17:
                        this.AccentTheme = "Brown";
                        break;
                    case 18:
                        this.AccentTheme = "Olive";
                        break;
                    case 19:
                        this.AccentTheme = "Steel";
                        break;
                    case 20:
                        this.AccentTheme = "Mauve";
                        break;
                    case 21:
                        this.AccentTheme = "Taupe";
                        break;
                    case 22:
                        this.AccentTheme = "Sienna";
                        break;
                    default:
                        this.AccentTheme = "Blue";
                        break;
                }

                switch (BatchContainer)
                {
                    case 0:
                        BatchOutContainer = ".mkv";
                        break;
                    case 1:
                        BatchOutContainer = ".mp4";
                        break;
                    case 2:
                        BatchOutContainer = ".webm";
                        break;
                    default:
                        break;
                }

                if (WorkerOverride)
                {
                    ComboBoxWorkerCount.Visibility = Visibility.Hidden;
                    TextBoxWorkerCountOverride.Visibility = Visibility.Visible;
                }
                else
                {
                    ComboBoxWorkerCount.Visibility = Visibility.Visible;
                    TextBoxWorkerCountOverride.Visibility = Visibility.Hidden;
                }

                ThemeManager.Current.ChangeTheme(this, this.BaseTheme + "." + this.AccentTheme);
            }

            // Custom BG
            if (File.Exists("background.txt"))
            {
                try
                {
                    Uri fileUri = new Uri(File.ReadAllText("background.txt"));
                    imgDynamic.Source = new BitmapImage(fileUri);
                    SetBackGroundColor();
                }
                catch { }
            }
            else
            {
                imgDynamic.Source = null;
                SolidColorBrush transparent = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                MetroTab.Background = transparent;
            }

            if (BaseTheme == "Light")
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

        private void SaveSettings(bool SaveProfile, string SaveName, string AudioCodec, string AudioBitrate)
        {
            string directory;
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

            if (!SaveProfile)
            {
                // Project File / Resume File
                writer.WriteElementString("VideoInput", Global.Video_Path);                                             // Video Input
                writer.WriteElementString("VideoOutput", Global.Video_Output);                                           // Video Output
                // Subtitles
                writer.WriteElementString("SubOne", ToggleSwitchSubtitleActivatedOne.IsOn.ToString());              // Subtitle Track One Active
                writer.WriteElementString("SubTwo", ToggleSwitchSubtitleActivatedTwo.IsOn.ToString());              // Subtitle Track Two Active
                writer.WriteElementString("SubThree", ToggleSwitchSubtitleActivatedThree.IsOn.ToString());            // Subtitle Track Three Active
                writer.WriteElementString("SubFour", ToggleSwitchSubtitleActivatedFour.IsOn.ToString());             // Subtitle Track Four Active
                writer.WriteElementString("SubFive", ToggleSwitchSubtitleActivatedFive.IsOn.ToString());             // Subtitle Track Five Active
                writer.WriteElementString("SubOneBurn", CheckBoxSubOneBurn.IsChecked.ToString());                       // Subtitle Track One Burn
                writer.WriteElementString("SubTwoBurn", CheckBoxSubTwoBurn.IsChecked.ToString());                       // Subtitle Track One Burn
                writer.WriteElementString("SubThreeBurn", CheckBoxSubThreeBurn.IsChecked.ToString());                     // Subtitle Track One Burn
                writer.WriteElementString("SubFourBurn", CheckBoxSubFourBurn.IsChecked.ToString());                      // Subtitle Track One Burn
                writer.WriteElementString("SubFiveBurn", CheckBoxSubFiveBurn.IsChecked.ToString());                      // Subtitle Track One Burn
                writer.WriteElementString("SubOneDefault", CheckBoxSubOneDefault.IsChecked.ToString());                    // Subtitle Track One Default
                writer.WriteElementString("SubTwoDefault", CheckBoxSubTwoDefault.IsChecked.ToString());                    // Subtitle Track One Default
                writer.WriteElementString("SubThreeDefault", CheckBoxSubThreeDefault.IsChecked.ToString());                  // Subtitle Track One Default
                writer.WriteElementString("SubFourDefault", CheckBoxSubFourDefault.IsChecked.ToString());                   // Subtitle Track One Default
                writer.WriteElementString("SubFiveDefault", CheckBoxSubFiveDefault.IsChecked.ToString());                   // Subtitle Track One Default
                writer.WriteElementString("SubOnePath", TextBoxSubtitleTrackOne.Text);                                  // Subtitle Track One Path
                writer.WriteElementString("SubTwoPath", TextBoxSubtitleTrackTwo.Text);                                  // Subtitle Track Two Path
                writer.WriteElementString("SubThreePath", TextBoxSubtitleTrackThree.Text);                                // Subtitle Track Three Path
                writer.WriteElementString("SubFourPath", TextBoxSubtitleTrackFour.Text);                                 // Subtitle Track Four Path
                writer.WriteElementString("SubFivePath", TextBoxSubtitleTrackFive.Text);                                 // Subtitle Track Five Path
                writer.WriteElementString("SubOneName", TextBoxSubOneName.Text);                                        // Subtitle Track One Name
                writer.WriteElementString("SubTwoName", TextBoxSubTwoName.Text);                                        // Subtitle Track Two Name
                writer.WriteElementString("SubThreeName", TextBoxSubThreeName.Text);                                      // Subtitle Track Three Name
                writer.WriteElementString("SubFourName", TextBoxSubFourName.Text);                                       // Subtitle Track Four Name
                writer.WriteElementString("SubFiveName", TextBoxSubFiveName.Text);                                       // Subtitle Track Five Name
                writer.WriteElementString("SubOneLanguage", ComboBoxSubTrackOneLanguage.SelectedIndex.ToString());          // Subtitle Track One Language
                writer.WriteElementString("SubTwoLanguage", ComboBoxSubTrackTwoLanguage.SelectedIndex.ToString());          // Subtitle Track Two Language
                writer.WriteElementString("SubThreeLanguage", ComboBoxSubTrackThreeLanguage.SelectedIndex.ToString());        // Subtitle Track Three Language
                writer.WriteElementString("SubFourLanguage", ComboBoxSubTrackFourLanguage.SelectedIndex.ToString());         // Subtitle Track Four Language
                writer.WriteElementString("SubFiveLanguage", ComboBoxSubTrackFiveLanguage.SelectedIndex.ToString());         // Subtitle Track Five Language
            }
            else
            {
                writer.WriteElementString("DefaultAudioCodec", AudioCodec);
                writer.WriteElementString("DefaultAudioBitrate", AudioBitrate);
            }

            // ═════════════════════════════════════════════════════════════════ Splitting ═════════════════════════════════════════════════════════════════
            writer.WriteElementString("SplittingMethod", ComboBoxSplittingMethod.SelectedIndex.ToString());              // Splitting Method
            writer.WriteElementString("SplittingThreshold", TextBoxSplittingThreshold.Text);                                // Splitting Threshold
            writer.WriteElementString("SplittingReencode", ComboBoxSplittingReencodeMethod.SelectedIndex.ToString());      // Splitting Reencode Codec
            writer.WriteElementString("SplittingReencodeLength", TextBoxSplittingChunkLength.Text);                              // Splitting Chunk Length
            writer.WriteElementString("SplittingReencodeSkip", CheckBoxSkipReencode.IsChecked.ToString());                     // Splitting Skip Reencode
            // ══════════════════════════════════════════════════════════════════ Filters ══════════════════════════════════════════════════════════════════

            writer.WriteElementString("FilterCrop", ToggleSwitchFilterCrop.IsOn.ToString());                            // Filter Crop (Boolean)
            if (ToggleSwitchFilterCrop.IsOn)
            {
                // Cropping
                writer.WriteElementString("FilterCropTop", TextBoxFiltersCropTop.Text);                                        // Filter Crop Top
                writer.WriteElementString("FilterCropBottom", TextBoxFiltersCropBottom.Text);                                     // Filter Crop Bottom
                writer.WriteElementString("FilterCropLeft", TextBoxFiltersCropLeft.Text);                                       // Filter Crop Left
                writer.WriteElementString("FilterCropRight", TextBoxFiltersCropRight.Text);                                      // Filter Crop Right
            }

            writer.WriteElementString("FilterResize", ToggleSwitchFilterResize.IsOn.ToString());                          // Filter Resize (Boolean)
            if (ToggleSwitchFilterResize.IsOn)
            {
                // Resize
                writer.WriteElementString("FilterResizeWidth", TextBoxFiltersResizeWidth.Text);                                    // Filter Resize Width
                writer.WriteElementString("FilterResizeHeight", TextBoxFiltersResizeHeight.Text);                                   // Filter Resize Height
                writer.WriteElementString("FilterResizeAlgo", ComboBoxFiltersScaling.SelectedIndex.ToString());                   // Filter Resize Scaling Algorithm
            }

            writer.WriteElementString("FilterRotate", ToggleSwitchFilterRotate.IsOn.ToString());                          // Filter Rotate (Boolean)
            if (ToggleSwitchFilterRotate.IsOn)
            {
                // Rotating
                writer.WriteElementString("FilterRotateAmount", ComboBoxFiltersRotate.SelectedIndex.ToString());                    // Filter Rotate
            }

            writer.WriteElementString("FilterDeinterlace", ToggleSwitchFilterDeinterlace.IsOn.ToString());                     // Filter Deinterlace (Boolean)
            if (ToggleSwitchFilterDeinterlace.IsOn)
            {
                // Deinterlacing
                writer.WriteElementString("FilterDeinterlaceType", ComboBoxFiltersDeinterlace.SelectedIndex.ToString());               // Filter Deinterlace
            }

            // ═══════════════════════════════════════════════════════════ Basic Video Settings ════════════════════════════════════════════════════════════

            writer.WriteElementString("VideoEncoder", ComboBoxVideoEncoder.SelectedIndex.ToString());                         // Video Encoder
            writer.WriteElementString("VideoBitDepth", ComboBoxVideoBitDepth.SelectedIndex.ToString());                        // Video BitDepth
            writer.WriteElementString("VideoColorFormat", ComboBoxColorFormat.SelectedIndex.ToString());                          // Video Color Format
            writer.WriteElementString("VideoSpeed", SliderVideoSpeed.Value.ToString());                                     // Video Speed
            writer.WriteElementString("VideoPasses", ComboBoxVideoPasses.SelectedIndex.ToString());                          // Video Passes
            if (RadioButtonVideoConstantQuality.IsChecked == true)
                writer.WriteElementString("VideoQuality", SliderVideoQuality.Value.ToString());                                   // Video Quality
            if (RadioButtonVideoBitrate.IsChecked == true)
                writer.WriteElementString("VideoBitrate", TextBoxVideoBitrate.Text);                                              // Video Bitrate
            if (ComboBoxVideoEncoder.SelectedIndex == 0)
                writer.WriteElementString("VideoAomencRT", CheckBoxVideoAomencRealTime.IsChecked.ToString());                      // Video Aomenc Real Time Mode
            writer.WriteElementString("VideoVFR", ToggleSwitchVFR.IsOn.ToString());                                       // Video Variable Framerate

            writer.WriteElementString("WorkerCount", ComboBoxWorkerCount.SelectedIndex.ToString());                                     // Worker Count
            writer.WriteElementString("WorkerPriority", ComboBoxProcessPriority.SelectedIndex.ToString());                              // Worker Priority
            // ══════════════════════════════════════════════════════════ Advanced Video Settings ══════════════════════════════════════════════════════════

            writer.WriteElementString("VideoAdvanced", ToggleSwitchAdvancedVideoSettings.IsOn.ToString());                     // Video Advanced Settings
            writer.WriteElementString("VideoAdvancedCustom", CheckBoxCustomVideoSettings.IsChecked.ToString());                      // Video Advanced Settings Custom

            if (ToggleSwitchAdvancedVideoSettings.IsOn == true && CheckBoxCustomVideoSettings.IsChecked == false)
            {
                // Custom Advanced Settings
                if (ComboBoxVideoEncoder.SelectedIndex == 0 || ComboBoxVideoEncoder.SelectedIndex == 5)
                {
                    // aomenc
                    writer.WriteElementString("VideoAdvancedAomencThreads", ComboBoxAomencThreads.SelectedIndex.ToString());        // Video Advanced Settings Aomenc Threads
                    writer.WriteElementString("VideoAdvancedAomencTileCols", ComboBoxAomencTileColumns.SelectedIndex.ToString());    // Video Advanced Settings Aomenc Tile Columns
                    writer.WriteElementString("VideoAdvancedAomencTileRows", ComboBoxAomencTileRows.SelectedIndex.ToString());       // Video Advanced Settings Aomenc Tile Rows
                    writer.WriteElementString("VideoAdvancedAomencGOP", TextBoxAomencMaxGOP.Text);                              // Video Advanced Settings Aomenc GOP
                    writer.WriteElementString("VideoAdvancedAomencLag", TextBoxAomencLagInFrames.Text);                         // Video Advanced Settings Aomenc Lag in Frames
                    writer.WriteElementString("VideoAdvancedAomencSharpness", ComboBoxAomencSharpness.SelectedIndex.ToString());      // Video Advanced Settings Aomenc Sharpness
                    writer.WriteElementString("VideoAdvancedAomencAQMode", ComboBoxAomencAQMode.SelectedIndex.ToString());         // Video Advanced Settings Aomenc AQ Mode
                    writer.WriteElementString("VideoAdvancedAomencKFFiltering", ComboBoxAomencKeyFiltering.SelectedIndex.ToString());   // Video Advanced Settings Aomenc Keyframe Filtering
                    writer.WriteElementString("VideoAdvancedAomencTune", ComboBoxAomencTune.SelectedIndex.ToString());           // Video Advanced Settings Aomenc Tune
                    writer.WriteElementString("VideoAdvancedAomencTuneContent", ComboBoxAomencTuneContent.SelectedIndex.ToString());    // Video Advanced Settings Aomenc Tune
                    writer.WriteElementString("VideoAdvancedAomencARNR", CheckBoxAomencARNRMax.IsChecked.ToString());            // Video Advanced Settings Aomenc ARNR
                    writer.WriteElementString("VideoAdvancedAomencColorPrim", ComboBoxAomencColorPrimaries.SelectedIndex.ToString()); // Video Advanced Settings Aomenc Color Primaries
                    writer.WriteElementString("VideoAdvancedAomencColorTrans", ComboBoxAomencColorTransfer.SelectedIndex.ToString());  // Video Advanced Settings Aomenc Color Transfer
                    writer.WriteElementString("VideoAdvancedAomencColorMatrix", ComboBoxAomencColorMatrix.SelectedIndex.ToString());    // Video Advanced Settings Aomenc Color Matrix
                    if (CheckBoxAomencARNRMax.IsChecked == true)
                    {
                        writer.WriteElementString("VideoAdvancedAomencARNRMax", ComboBoxAomencARNRMax.SelectedIndex.ToString());        // Video Advanced Settings Aomenc ARNR Max
                        writer.WriteElementString("VideoAdvancedAomencARNRStre", ComboBoxAomencARNRStrength.SelectedIndex.ToString());  // Video Advanced Settings Aomenc ARNR Strength
                    }
                    writer.WriteElementString("VideoAdvancedAomencRowMT", CheckBoxAomencRowMT.IsChecked.ToString());              // Video Advanced Settings Aomenc Row Mt
                    writer.WriteElementString("VideoAdvancedAomencCDEF", CheckBoxAomencCDEF.IsChecked.ToString());               // Video Advanced Settings Aomenc CDEF
                }
                else if (ComboBoxVideoEncoder.SelectedIndex == 1 || ComboBoxVideoEncoder.SelectedIndex == 6)
                {
                    // rav1e
                    writer.WriteElementString("VideoAdvancedRav1eThreads", ComboBoxRav1eThreads.SelectedIndex.ToString());         // Video Advanced Settings Rav1e Threads
                    writer.WriteElementString("VideoAdvancedRav1eTileCols", ComboBoxRav1eTileColumns.SelectedIndex.ToString());     // Video Advanced Settings Rav1e Tile Columns
                    writer.WriteElementString("VideoAdvancedRav1eTileRows", ComboBoxRav1eTileRows.SelectedIndex.ToString());        // Video Advanced Settings Rav1e Tile Rows
                    writer.WriteElementString("VideoAdvancedRav1eGOP", TextBoxRav1eMaxGOP.Text);                               // Video Advanced Settings Rav1e GOP
                    writer.WriteElementString("VideoAdvancedRav1eRDO", TextBoxRav1eLookahead.Text);                            // Video Advanced Settings Rav1e RDO Lookahead
                    writer.WriteElementString("VideoAdvancedRav1eColorPrim", ComboBoxRav1eColorPrimaries.SelectedIndex.ToString());  // Video Advanced Settings Rav1e Color Primaries
                    writer.WriteElementString("VideoAdvancedRav1eColorTrans", ComboBoxRav1eColorTransfer.SelectedIndex.ToString());   // Video Advanced Settings Rav1e Color Transfer
                    writer.WriteElementString("VideoAdvancedRav1eColorMatrix", ComboBoxRav1eColorMatrix.SelectedIndex.ToString());     // Video Advanced Settings Rav1e Color Matrix
                    writer.WriteElementString("VideoAdvancedRav1eTune", ComboBoxRav1eTune.SelectedIndex.ToString());            // Video Advanced Settings Rav1e Tune
                    writer.WriteElementString("VideoAdvancedRav1eMastering", CheckBoxRav1eMasteringDisplay.IsChecked.ToString());    // Video Advanced Settings Rav1e Mastering Display
                    if (CheckBoxRav1eMasteringDisplay.IsChecked == true)
                    {
                        writer.WriteElementString("VideoAdvancedRav1eMasteringGx", TextBoxRav1eMasteringGx.Text);                      // Video Advanced Settings Rav1e Mastering Display Gx
                        writer.WriteElementString("VideoAdvancedRav1eMasteringGy", TextBoxRav1eMasteringGy.Text);                      // Video Advanced Settings Rav1e Mastering Display Gy
                        writer.WriteElementString("VideoAdvancedRav1eMasteringBx", TextBoxRav1eMasteringBx.Text);                      // Video Advanced Settings Rav1e Mastering Display Bx
                        writer.WriteElementString("VideoAdvancedRav1eMasteringBy", TextBoxRav1eMasteringBy.Text);                      // Video Advanced Settings Rav1e Mastering Display By
                        writer.WriteElementString("VideoAdvancedRav1eMasteringRx", TextBoxRav1eMasteringRx.Text);                      // Video Advanced Settings Rav1e Mastering Display Rx
                        writer.WriteElementString("VideoAdvancedRav1eMasteringRy", TextBoxRav1eMasteringRy.Text);                      // Video Advanced Settings Rav1e Mastering Display Ry
                        writer.WriteElementString("VideoAdvancedRav1eMasteringWPx", TextBoxRav1eMasteringWPx.Text);                     // Video Advanced Settings Rav1e Mastering Display WPx
                        writer.WriteElementString("VideoAdvancedRav1eMasteringWPy", TextBoxRav1eMasteringWPy.Text);                     // Video Advanced Settings Rav1e Mastering Display WPy
                        writer.WriteElementString("VideoAdvancedRav1eMasteringLx", TextBoxRav1eMasteringLx.Text);                      // Video Advanced Settings Rav1e Mastering Display Lx
                        writer.WriteElementString("VideoAdvancedRav1eMasteringLy", TextBoxRav1eMasteringLy.Text);                      // Video Advanced Settings Rav1e Mastering Display Ly
                    }
                    writer.WriteElementString("VideoAdvancedRav1eLight", CheckBoxRav1eContentLight.IsChecked.ToString());        // Video Advanced Settings Rav1e Mastering Content Light
                    if (CheckBoxRav1eMasteringDisplay.IsChecked == true)
                    {
                        writer.WriteElementString("VideoAdvancedRav1eLightCll", TextBoxRav1eContentLightCll.Text);                     // Video Advanced Settings Rav1e Mastering Content Light Cll
                        writer.WriteElementString("VideoAdvancedRav1eLightFall", TextBoxRav1eContentLightFall.Text);                    // Video Advanced Settings Rav1e Mastering Content Light Fall
                    }
                }
                else if (ComboBoxVideoEncoder.SelectedIndex == 2 || ComboBoxVideoEncoder.SelectedIndex == 7)
                {
                    // svt-av1
                    writer.WriteElementString("VideoAdvancedSVTAV1TileCols", ComboBoxSVTAV1TileColumns.SelectedIndex.ToString());    // Video Advanced Settings SVT-AV1 Tile Columns
                    writer.WriteElementString("VideoAdvancedSVTAV1TileRows", ComboBoxSVTAV1TileRows.SelectedIndex.ToString());       // Video Advanced Settings SVT-AV1 Tile Rows
                    writer.WriteElementString("VideoAdvancedSVTAV1GOP", TextBoxSVTAV1MaxGOP.Text);                              // Video Advanced Settings SVT-AV1 GOP
                }
                else if (ComboBoxVideoEncoder.SelectedIndex == 3)
                {
                    // vp9
                    writer.WriteElementString("VideoAdvancedVP9Threads", ComboBoxVP9Threads.SelectedIndex.ToString());           // Video Advanced Settings VP9 Threads
                    writer.WriteElementString("VideoAdvancedVP9TileCols", ComboBoxVP9TileColumns.SelectedIndex.ToString());       // Video Advanced Settings VP9 Tile Columns
                    writer.WriteElementString("VideoAdvancedVP9TileRows", ComboBoxVP9TileRows.SelectedIndex.ToString());          // Video Advanced Settings VP9 Tile Rows
                    writer.WriteElementString("VideoAdvancedVP9GOP", TextBoxVP9MaxKF.Text);                                  // Video Advanced Settings VP9 GOP
                    writer.WriteElementString("VideoAdvancedVP9Lag", TextBoxVP9LagInFrames.Text);                            // Video Advanced Settings VP9 Lag in Frames
                    writer.WriteElementString("VideoAdvancedVP9AQMode", ComboBoxVP9AQMode.SelectedIndex.ToString());            // Video Advanced Settings VP9 AQ Mode
                    writer.WriteElementString("VideoAdvancedVP9Tune", ComboBoxVP9ATune.SelectedIndex.ToString());             // Video Advanced Settings VP9 Tune
                    writer.WriteElementString("VideoAdvancedVP9TuneContent", ComboBoxVP9ATuneContent.SelectedIndex.ToString());      // Video Advanced Settings VP9 Tune Content
                    writer.WriteElementString("VideoAdvancedVP9ARNR", CheckBoxVP9ARNR.IsChecked.ToString());                  // Video Advanced Settings VP9 ARNR
                    if (CheckBoxAomencARNRMax.IsChecked == true)
                    {
                        writer.WriteElementString("VideoAdvancedVP9ARNRMax", ComboBoxAomencVP9Max.SelectedIndex.ToString());         // Video Advanced Settings VP9 ARNR Max
                        writer.WriteElementString("VideoAdvancedVP9ARNRStre", ComboBoxAomencVP9Strength.SelectedIndex.ToString());    // Video Advanced Settings VP9 ARNR Strength
                        writer.WriteElementString("VideoAdvancedVP9ARNRType", ComboBoxAomencVP9ARNRType.SelectedIndex.ToString());    // Video Advanced Settings VP9 ARNR Type
                    }
                }
            }
            else if (ToggleSwitchAdvancedVideoSettings.IsOn == true && CheckBoxCustomVideoSettings.IsChecked == true)
            {
                writer.WriteElementString("VideoAdvancedCustomString", TextBoxCustomVideoSettings.Text);                       // Video Advanced Settings Custom String
            }

            // Writes Ending XML Element
            writer.WriteEndElement();
            // Cloeses XML Writer
            writer.Close();
        }

        private void LoadSettings(bool LoadProfile, string SaveName)
        {
            string directory;
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
                    case "VideoInput":
                        Global.Video_Path = n.InnerText; TextBoxVideoSource.Text = n.InnerText; VideoInputSet = true;
                        Global.temp_path_folder = Path.GetFileNameWithoutExtension(n.InnerText); break;  // Video Input
                    case "VideoOutput":
                        Global.Video_Output = n.InnerText; VideoOutputSet = true;
                        TextBoxVideoDestination.Text = n.InnerText; break;  // Video Output
                    case "WorkerCount": ComboBoxWorkerCount.SelectedIndex = int.Parse(n.InnerText); break;  // Worker Count
                    case "WorkerPriority": ComboBoxProcessPriority.SelectedIndex = int.Parse(n.InnerText); break;  // Worker Priority
                    // ═══════════════════════════════════════════════════════════════════ Audio ═══════════════════════════════════════════════════════════════════
                    case "DefaultAudioCodec": DefaultAudioCodec = int.Parse(n.InnerText); break;
                    case "DefaultAudioBitrate": DefaultBitrateCodec = n.InnerText; break;
                    // ═════════════════════════════════════════════════════════════════ Splitting ═════════════════════════════════════════════════════════════════
                    case "SplittingMethod": ComboBoxSplittingMethod.SelectedIndex = int.Parse(n.InnerText); break;  // Splitting Method
                    case "SplittingThreshold": TextBoxSplittingThreshold.Text = n.InnerText; break;  // Splitting Threshold
                    case "SplittingReencode": ComboBoxSplittingReencodeMethod.SelectedIndex = int.Parse(n.InnerText); break;  // Splitting Reencode Codec
                    case "SplittingReencodeLength": TextBoxSplittingChunkLength.Text = n.InnerText; break;  // Splitting Chunk Length
                    case "SplittingReencodeSkip": CheckBoxSkipReencode.IsChecked = n.InnerText == "True"; break;  // Splitting Skip Reencode
                    // ══════════════════════════════════════════════════════════════════ Filters ══════════════════════════════════════════════════════════════════
                    case "FilterCrop": ToggleSwitchFilterCrop.IsOn = n.InnerText == "True"; break;  // Filter Crop (Boolean)
                    case "FilterCropTop": TextBoxFiltersCropTop.Text = n.InnerText; break;  // Filter Crop Top
                    case "FilterCropBottom": TextBoxFiltersCropBottom.Text = n.InnerText; break;  // Filter Crop Bottom
                    case "FilterCropLeft": TextBoxFiltersCropLeft.Text = n.InnerText; break;  // Filter Crop Left
                    case "FilterCropRight": TextBoxFiltersCropRight.Text = n.InnerText; break;  // Filter Crop Right
                    case "FilterResize": ToggleSwitchFilterResize.IsOn = n.InnerText == "True"; break;  // Filter Resize (Boolean)
                    case "FilterResizeWidth": TextBoxFiltersResizeWidth.Text = n.InnerText; break;  // Filter Resize Width
                    case "FilterResizeHeight": TextBoxFiltersResizeHeight.Text = n.InnerText; break;  // Filter Resize Height
                    case "FilterResizeAlgo": ComboBoxFiltersScaling.SelectedIndex = int.Parse(n.InnerText); break;  // Filter Resize Scaling Algorithm
                    case "FilterRotate": ToggleSwitchFilterRotate.IsOn = n.InnerText == "True"; break;  // Filter Rotate (Boolean)
                    case "FilterRotateAmount": ComboBoxFiltersRotate.SelectedIndex = int.Parse(n.InnerText); break;  // Filter Rotate
                    case "FilterDeinterlace": ToggleSwitchFilterDeinterlace.IsOn = n.InnerText == "True"; break;  // Filter Deinterlace (Boolean)
                    case "FilterDeinterlaceType": ComboBoxFiltersDeinterlace.SelectedIndex = int.Parse(n.InnerText); break;  // Filter Deinterlace
                    // ═══════════════════════════════════════════════════════════ Basic Video Settings ════════════════════════════════════════════════════════════
                    case "VideoEncoder": ComboBoxVideoEncoder.SelectedIndex = int.Parse(n.InnerText); break;  // Video Encoder
                    case "VideoBitDepth": ComboBoxVideoBitDepth.SelectedIndex = int.Parse(n.InnerText); break;  // Video BitDepth
                    case "VideoColorFormat": ComboBoxColorFormat.SelectedIndex = int.Parse(n.InnerText); break;  // Video Color Format
                    case "VideoSpeed": SliderVideoSpeed.Value = int.Parse(n.InnerText); break;  // Video Speed
                    case "VideoPasses": ComboBoxVideoPasses.SelectedIndex = int.Parse(n.InnerText); break;  // Video Passes
                    case "VideoQuality":
                        SliderVideoQuality.Value = int.Parse(n.InnerText);
                        RadioButtonVideoConstantQuality.IsChecked = true; break;  // Video Quality
                    case "VideoBitrate":
                        TextBoxVideoBitrate.Text = n.InnerText;
                        RadioButtonVideoBitrate.IsChecked = true; break;  // Video Bitrate
                    case "VideoAomencRT": CheckBoxVideoAomencRealTime.IsChecked = n.InnerText == "True"; break;  // Video Aomenc Real Time Mode
                    case "VideoVFR": ToggleSwitchVFR.IsOn = n.InnerText == "True"; break;  // VIdeo Variable Framerate
                    // ═════════════════════════════════════════════════════════ Advanced Video Settings ═══════════════════════════════════════════════════════════
                    case "VideoAdvanced": ToggleSwitchAdvancedVideoSettings.IsOn = n.InnerText == "True"; break;  // Video Advanced Settings
                    case "VideoAdvancedCustom": CheckBoxCustomVideoSettings.IsChecked = n.InnerText == "True"; break;  // Video Advanced Settings Custom
                    case "VideoAdvancedAomencThreads": ComboBoxAomencThreads.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings Aomenc Threads
                    case "VideoAdvancedAomencTileCols": ComboBoxAomencTileColumns.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings Aomenc Tile Columns
                    case "VideoAdvancedAomencTileRows": ComboBoxAomencTileRows.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings Aomenc Tile Rows
                    case "VideoAdvancedAomencGOP": TextBoxAomencMaxGOP.Text = n.InnerText; break;  // Video Advanced Settings Aomenc GOP
                    case "VideoAdvancedAomencLag": TextBoxAomencLagInFrames.Text = n.InnerText; break;  // Video Advanced Settings Aomenc Lag in Frames
                    case "VideoAdvancedAomencSharpness": ComboBoxAomencSharpness.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings Aomenc Sharpness
                    case "VideoAdvancedAomencColorPrim": ComboBoxAomencColorPrimaries.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings Aomenc Color Primaries
                    case "VideoAdvancedAomencColorTrans": ComboBoxAomencColorTransfer.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings Aomenc Color Transfer
                    case "VideoAdvancedAomencColorMatrix": ComboBoxAomencColorMatrix.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings Aomenc Color Matrix
                    case "VideoAdvancedAomencAQMode": ComboBoxAomencAQMode.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings Aomenc AQ Mode
                    case "VideoAdvancedAomencKFFiltering": ComboBoxAomencKeyFiltering.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings Aomenc Keyframe Filtering
                    case "VideoAdvancedAomencTune": ComboBoxAomencTune.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings Aomenc Tune
                    case "VideoAdvancedAomencTuneContent": ComboBoxAomencTuneContent.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings Aomenc Tune Content
                    case "VideoAdvancedAomencARNR": CheckBoxAomencARNRMax.IsChecked = n.InnerText == "True"; break;  // Video Advanced Settings Aomenc ARNR
                    case "VideoAdvancedAomencARNRMax": ComboBoxAomencARNRMax.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings Aomenc ARNR Max
                    case "VideoAdvancedAomencARNRStre": ComboBoxAomencARNRStrength.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings Aomenc ARNR Strength
                    case "VideoAdvancedAomencRowMT": CheckBoxAomencRowMT.IsChecked = n.InnerText == "True"; break;  // Video Advanced Settings Aomenc Row Mt
                    case "VideoAdvancedAomencCDEF": CheckBoxAomencCDEF.IsChecked = n.InnerText == "True"; break;  // Video Advanced Settings Aomenc CDEF
                    case "VideoAdvancedRav1eThreads": ComboBoxRav1eThreads.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings Rav1e Threads
                    case "VideoAdvancedRav1eTileCols": ComboBoxRav1eTileColumns.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings Rav1e Tile Columns
                    case "VideoAdvancedRav1eTileRows": ComboBoxRav1eTileRows.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings Rav1e Tile Rows
                    case "VideoAdvancedRav1eGOP": TextBoxRav1eMaxGOP.Text = n.InnerText; break;  // Video Advanced Settings Rav1e GOP
                    case "VideoAdvancedRav1eRDO": TextBoxRav1eLookahead.Text = n.InnerText; break;  // Video Advanced Settings Rav1e RDO Lookahead
                    case "VideoAdvancedRav1eTune": ComboBoxRav1eTune.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings Rav1e Tune
                    case "VideoAdvancedRav1eColorPrim": ComboBoxRav1eColorPrimaries.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings Rav1e Color Primaries
                    case "VideoAdvancedRav1eColorTrans": ComboBoxRav1eColorTransfer.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings Rav1e Color Transfer
                    case "VideoAdvancedRav1eColorMatrix": ComboBoxRav1eColorMatrix.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings Rav1e Color Matrix
                    case "VideoAdvancedRav1eMastering": CheckBoxRav1eMasteringDisplay.IsChecked = n.InnerText == "True"; break;  // Video Advanced Settings Rav1e Mastering Display
                    case "VideoAdvancedRav1eMasteringGx": TextBoxRav1eMasteringGx.Text = n.InnerText; break;  // Video Advanced Settings Rav1e Mastering Display Gx
                    case "VideoAdvancedRav1eMasteringGy": TextBoxRav1eMasteringGy.Text = n.InnerText; break;  // Video Advanced Settings Rav1e Mastering Display Gy
                    case "VideoAdvancedRav1eMasteringBx": TextBoxRav1eMasteringBx.Text = n.InnerText; break;  // Video Advanced Settings Rav1e Mastering Display Bx
                    case "VideoAdvancedRav1eMasteringBy": TextBoxRav1eMasteringBy.Text = n.InnerText; break;  // Video Advanced Settings Rav1e Mastering Display By
                    case "VideoAdvancedRav1eMasteringRx": TextBoxRav1eMasteringRx.Text = n.InnerText; break;  // Video Advanced Settings Rav1e Mastering Display Rx
                    case "VideoAdvancedRav1eMasteringRy": TextBoxRav1eMasteringRy.Text = n.InnerText; break;  // Video Advanced Settings Rav1e Mastering Display Ry
                    case "VideoAdvancedRav1eMasteringWPx": TextBoxRav1eMasteringWPx.Text = n.InnerText; break;  // Video Advanced Settings Rav1e Mastering Display WPx
                    case "VideoAdvancedRav1eMasteringWPy": TextBoxRav1eMasteringWPy.Text = n.InnerText; break;  // Video Advanced Settings Rav1e Mastering Display WPy
                    case "VideoAdvancedRav1eMasteringLx": TextBoxRav1eMasteringLx.Text = n.InnerText; break;  // Video Advanced Settings Rav1e Mastering Display Lx
                    case "VideoAdvancedRav1eMasteringLy": TextBoxRav1eMasteringLy.Text = n.InnerText; break;  // Video Advanced Settings Rav1e Mastering Display Ly
                    case "VideoAdvancedRav1eLight": CheckBoxRav1eContentLight.IsChecked = n.InnerText == "True"; break;  // Video Advanced Settings Rav1e Mastering Content Light
                    case "VideoAdvancedRav1eLightCll": TextBoxRav1eContentLightCll.Text = n.InnerText; break;  // Video Advanced Settings Rav1e Mastering Content Light Cll
                    case "VideoAdvancedRav1eLightFall": TextBoxRav1eContentLightFall.Text = n.InnerText; break;  // Video Advanced Settings Rav1e Mastering Content Light Fall
                    case "VideoAdvancedSVTAV1TileCols": ComboBoxSVTAV1TileColumns.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings SVT-AV1 Tile Columns
                    case "VideoAdvancedSVTAV1TileRows": ComboBoxSVTAV1TileRows.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings SVT-AV1 Tile Rows
                    case "VideoAdvancedSVTAV1GOP": TextBoxSVTAV1MaxGOP.Text = n.InnerText; break;  // Video Advanced Settings SVT-AV1 GOP
                    case "VideoAdvancedVP9Threads": ComboBoxVP9Threads.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings VP9 Threads
                    case "VideoAdvancedVP9TileCols": ComboBoxVP9TileColumns.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings VP9 Tile Columns
                    case "VideoAdvancedVP9TileRows": ComboBoxVP9TileRows.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings VP9 Tile Rows
                    case "VideoAdvancedVP9GOP": TextBoxVP9MaxKF.Text = n.InnerText; break;  // Video Advanced Settings VP9 GOP
                    case "VideoAdvancedVP9Lag": TextBoxVP9LagInFrames.Text = n.InnerText; break;  // Video Advanced Settings VP9 Lag in Frames
                    case "VideoAdvancedVP9AQMode": ComboBoxVP9AQMode.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings VP9 AQ Mode
                    case "VideoAdvancedVP9Tune": ComboBoxVP9ATune.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings VP9 Tune
                    case "VideoAdvancedVP9TuneContent": ComboBoxVP9ATuneContent.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings VP9 Tune Content
                    case "VideoAdvancedVP9ARNR": CheckBoxVP9ARNR.IsChecked = n.InnerText == "True"; break;  // Video Advanced Settings VP9 ARNR
                    case "VideoAdvancedVP9ARNRMax": ComboBoxAomencVP9Max.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings VP9 ARNR Max
                    case "VideoAdvancedVP9ARNRStre": ComboBoxAomencVP9Strength.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings VP9 ARNR Strength
                    case "VideoAdvancedVP9ARNRType": ComboBoxAomencVP9ARNRType.SelectedIndex = int.Parse(n.InnerText); break;  // Video Advanced Settings VP9 ARNR Type
                    case "VideoAdvancedCustomString": TextBoxCustomVideoSettings.Text = n.InnerText; break;  // Video Advanced Settings Custom String
                    // Subtitles
                    case "SubOne": ToggleSwitchSubtitleActivatedOne.IsOn = n.InnerText == "True"; break;  // Subtitle Track One Active
                    case "SubTwo": ToggleSwitchSubtitleActivatedTwo.IsOn = n.InnerText == "True"; break;  // Subtitle Track Two Active
                    case "SubThree": ToggleSwitchSubtitleActivatedThree.IsOn = n.InnerText == "True"; break;  // Subtitle Track Three Active
                    case "SubFour": ToggleSwitchSubtitleActivatedFour.IsOn = n.InnerText == "True"; break;  // Subtitle Track Four Active
                    case "SubFive": ToggleSwitchSubtitleActivatedFive.IsOn = n.InnerText == "True"; break;  // Subtitle Track Five Active
                    case "SubOneBurn": CheckBoxSubOneBurn.IsChecked = n.InnerText == "True"; break;  // Subtitle Track One Burn
                    case "SubTwoBurn": CheckBoxSubTwoBurn.IsChecked = n.InnerText == "True"; break;  // Subtitle Track Two Burn
                    case "SubThreeBurn": CheckBoxSubThreeBurn.IsChecked = n.InnerText == "True"; break;  // Subtitle Track Three Burn
                    case "SubFourBurn": CheckBoxSubFourBurn.IsChecked = n.InnerText == "True"; break;  // Subtitle Track Four Burn
                    case "SubFiveBurn": CheckBoxSubFiveBurn.IsChecked = n.InnerText == "True"; break;  // Subtitle Track Five Burn
                    case "SubOneDefault": CheckBoxSubOneDefault.IsChecked = n.InnerText == "True"; break;  // Subtitle Track One Default
                    case "SubTwoDefault": CheckBoxSubTwoDefault.IsChecked = n.InnerText == "True"; break;  // Subtitle Track Two Default
                    case "SubThreeDefault": CheckBoxSubThreeDefault.IsChecked = n.InnerText == "True"; break;  // Subtitle Track Three Default
                    case "SubFourDefault": CheckBoxSubFourDefault.IsChecked = n.InnerText == "True"; break;  // Subtitle Track Four Default
                    case "SubFiveDefault": CheckBoxSubFiveDefault.IsChecked = n.InnerText == "True"; break;  // Subtitle Track Five Default
                    case "SubOnePath": TextBoxSubtitleTrackOne.Text = n.InnerText; break;  // Subtitle Track One Path
                    case "SubTwoPath": TextBoxSubtitleTrackTwo.Text = n.InnerText; break;  // Subtitle Track Two Path
                    case "SubThreePath": TextBoxSubtitleTrackThree.Text = n.InnerText; break;  // Subtitle Track Three Path
                    case "SubFourPath": TextBoxSubtitleTrackFour.Text = n.InnerText; break;  // Subtitle Track Four Path
                    case "SubFivePath": TextBoxSubtitleTrackFive.Text = n.InnerText; break;  // Subtitle Track Five Path
                    case "SubOneName": TextBoxSubOneName.Text = n.InnerText; break;  // Subtitle Track One Name
                    case "SubTwoName": TextBoxSubTwoName.Text = n.InnerText; break;  // Subtitle Track Two Name
                    case "SubThreeName": TextBoxSubThreeName.Text = n.InnerText; break;  // Subtitle Track Three Name
                    case "SubFourName": TextBoxSubFourName.Text = n.InnerText; break;  // Subtitle Track Four Name
                    case "SubFiveName": TextBoxSubFiveName.Text = n.InnerText; break;  // Subtitle Track Five Name
                    case "SubOneLanguage": ComboBoxSubTrackOneLanguage.SelectedIndex = int.Parse(n.InnerText); break;  // Subtitle Track One Language
                    case "SubTwoLanguage": ComboBoxSubTrackTwoLanguage.SelectedIndex = int.Parse(n.InnerText); break;  // Subtitle Track Two Language
                    case "SubThreeLanguage": ComboBoxSubTrackThreeLanguage.SelectedIndex = int.Parse(n.InnerText); break;  // Subtitle Track Three Language
                    case "SubFourLanguage": ComboBoxSubTrackFourLanguage.SelectedIndex = int.Parse(n.InnerText); break;  // Subtitle Track Four Language
                    case "SubFiveLanguage": ComboBoxSubTrackFiveLanguage.SelectedIndex = int.Parse(n.InnerText); break;  // Subtitle Track Five Language
                    default: break;
                }
            }
        }
    }
}