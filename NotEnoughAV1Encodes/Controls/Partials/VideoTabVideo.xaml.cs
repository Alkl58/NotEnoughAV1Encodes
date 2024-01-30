using System.Windows;
using System.Windows.Controls;

namespace NotEnoughAV1Encodes.Controls.Partials
{
    public partial class VideoTabVideo : UserControl
    {
        public VideoTabVideo()
        {
            InitializeComponent();
        }

        public string GenerateFFmpegFramerate()
        {
            string settings = "fps=" + ComboBoxVideoFrameRate.Text;
            if (ComboBoxVideoFrameRate.SelectedIndex == 6) { settings = "fps=24000/1001"; }
            if (ComboBoxVideoFrameRate.SelectedIndex == 9) { settings = "fps=30000/1001"; }
            if (ComboBoxVideoFrameRate.SelectedIndex == 13) { settings = "fps=60000/1001"; }

            return settings;
        }

        public string GenerateFFmpegColorSpace()
        {
            string settings = "-pix_fmt yuv4";

            if (ComboBoxColorFormat.SelectedIndex == 0)
            {
                settings += "20p";
            }
            else if (ComboBoxColorFormat.SelectedIndex == 1)
            {
                settings += "22p";
            }
            else if (ComboBoxColorFormat.SelectedIndex == 2)
            {
                settings += "44p";
            }

            if (ComboBoxVideoEncoder.SelectedIndex is (int)Video.Encoders.QSVAV1 or (int)Video.Encoders.X264)
            {
                if (ComboBoxVideoBitDepthLimited.SelectedIndex == 1)
                {
                    settings += "10le -strict -1";
                }
            }
            else
            {
                if (ComboBoxVideoBitDepth.SelectedIndex == 1)
                {
                    settings += "10le -strict -1";
                }
                else if (ComboBoxVideoBitDepth.SelectedIndex == 2)
                {
                    settings += "12le -strict -1";
                }
            }

            return settings;
        }

        public string GenerateKeyFrameInerval()
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
                }
                catch { }
            }

            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            // Framerate of Video if it's not VFR and MediaInfo Detected it
            if (!mainWindow.videoDB.MIIsVFR && !string.IsNullOrEmpty(mainWindow.videoDB.MIFramerate))
            {
                try
                {
                    int framerate = int.Parse(mainWindow.videoDB.MIFramerate);
                    int frames = framerate * seconds;
                    return frames.ToString();
                }
                catch { }
            }

            return "240";
        }

        private void ComboBoxVideoBitDepth_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainWindow.startupLock) return;

            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            mainWindow.SummaryTabControl.LabelBitDepth.Content = ComboBoxVideoBitDepth.SelectedIndex switch
            {
                0 => "8",
                1 => "10",
                2 => "12",
                _ => ""
            };
        }

        private void ComboBoxColorFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainWindow.startupLock) return;

            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            mainWindow.SummaryTabControl.LabelColorFormatOutput.Content = ComboBoxColorFormat.SelectedIndex switch
            {
                0 => "4:2:0",
                1 => "4:2:2",
                2 => "4:4:4",
                _ => ""
            };
        }

        private void ComboBoxVideoFrameRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainWindow.startupLock) return;

            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            mainWindow.SummaryTabControl.LabelFramerateOutput.Content = ComboBoxVideoFrameRate.SelectedIndex switch
            {
                0 => "Same as Source",
                1 => "5",
                2 => "10",
                3 => "12",
                4 => "15",
                5 => "20",
                6 => "23.976",
                7 => "24",
                8 => "25",
                9 => "29.97",
                10 => "30",
                11 => "48",
                12 => "50",
                13 => "59.94",
                14 => "60",
                15 => "72",
                16 => "75",
                17 => "90",
                18 => "100",
                19 => "120",
                20 => "144",
                21 => "240",
                22 => "360",
                _ => ""
            };
        }

        private void ComboBoxVideoEncoder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainWindow.startupLock) return;

            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (mainWindow.VideoTabVideoOptimizationControl.SliderEncoderPreset == null) return;

            ComboBoxColorFormat.IsEnabled = true;
            ComboBoxVideoBitDepth.IsEnabled = true;
            CheckBoxVideoHDR.IsEnabled = true;

            if (ComboBoxVideoEncoder.SelectedIndex is (int)Video.Encoders.AOMFFMPEG or (int)Video.Encoders.AOMENC)
            {
                //aom ffmpeg
                if (ComboBoxVideoEncoder.SelectedIndex == 0)
                {
                    mainWindow.VideoTabVideoOptimizationControl.SliderEncoderPreset.Maximum = 8;
                }
                else
                {
                    mainWindow.VideoTabVideoOptimizationControl.SliderEncoderPreset.Maximum = 9;
                }
                mainWindow.VideoTabVideoOptimizationControl.SliderEncoderPreset.Value = 4;
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxTwoPassEncoding.IsEnabled = true;
                ComboBoxVideoBitDepth.Visibility = Visibility.Visible;
                ComboBoxVideoBitDepthLimited.Visibility = Visibility.Collapsed;
            }
            else if (ComboBoxVideoEncoder.SelectedIndex is (int)Video.Encoders.RAV1EFFMPEG or (int)Video.Encoders.RAV1E)
            {
                //rav1e ffmpeg
                mainWindow.VideoTabVideoQualityControl.ComboBoxQualityMode.SelectedIndex = 0;
                mainWindow.VideoTabVideoOptimizationControl.SliderEncoderPreset.Maximum = 10;
                mainWindow.VideoTabVideoOptimizationControl.SliderEncoderPreset.Value = 5;
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxTwoPassEncoding.IsOn = false;
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxTwoPassEncoding.IsEnabled = false;
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxRealTimeMode.IsOn = false;
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxRealTimeMode.Visibility = Visibility.Collapsed;
                ComboBoxVideoBitDepth.Visibility = Visibility.Visible;
                ComboBoxVideoBitDepthLimited.Visibility = Visibility.Collapsed;
            }
            else if (ComboBoxVideoEncoder.SelectedIndex is (int)Video.Encoders.SVTAV1FFMPEG or (int)Video.Encoders.SVTAV1)
            {
                //svt-av1 ffmpeg
                mainWindow.VideoTabVideoQualityControl.ComboBoxQualityMode.SelectedIndex = 0;
                mainWindow.VideoTabVideoOptimizationControl.SliderEncoderPreset.Maximum = 13;
                mainWindow.VideoTabVideoOptimizationControl.SliderEncoderPreset.Value = 10;
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxTwoPassEncoding.IsEnabled = true;
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxTwoPassEncoding.IsOn = false;
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxRealTimeMode.IsOn = false;
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxRealTimeMode.Visibility = Visibility.Collapsed;
                ComboBoxVideoBitDepth.Visibility = Visibility.Visible;
                ComboBoxVideoBitDepthLimited.Visibility = Visibility.Collapsed;
            }
            else if (ComboBoxVideoEncoder.SelectedIndex is (int)Video.Encoders.VPXVP9FFMPEG)
            {
                //vpx-vp9 ffmpeg
                mainWindow.VideoTabVideoOptimizationControl.SliderEncoderPreset.Maximum = 8;
                mainWindow.VideoTabVideoOptimizationControl.SliderEncoderPreset.Value = 4;
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxTwoPassEncoding.IsEnabled = true;
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxRealTimeMode.IsOn = false;
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxRealTimeMode.Visibility = Visibility.Collapsed;
                ComboBoxVideoBitDepth.Visibility = Visibility.Visible;
                ComboBoxVideoBitDepthLimited.Visibility = Visibility.Collapsed;
            }
            else if (ComboBoxVideoEncoder.SelectedIndex is (int)Video.Encoders.X265 or (int)Video.Encoders.X264)
            {
                //libx265 libx264 ffmpeg
                mainWindow.VideoTabVideoOptimizationControl.SliderEncoderPreset.Maximum = 9;
                mainWindow.VideoTabVideoOptimizationControl.SliderEncoderPreset.Value = 4;
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxTwoPassEncoding.IsEnabled = false;
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxTwoPassEncoding.IsOn = false;
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxRealTimeMode.IsOn = false;
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxRealTimeMode.Visibility = Visibility.Collapsed;
                ComboBoxVideoBitDepth.Visibility = Visibility.Visible;
                ComboBoxVideoBitDepthLimited.Visibility = Visibility.Collapsed;
            }
            else if (ComboBoxVideoEncoder.SelectedIndex is (int)Video.Encoders.QSVAV1)
            {
                // av1 hardware (intel arc)
                mainWindow.VideoTabVideoOptimizationControl.SliderEncoderPreset.Maximum = 6;
                mainWindow.VideoTabVideoOptimizationControl.SliderEncoderPreset.Value = 3;
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxTwoPassEncoding.IsEnabled = false;
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxTwoPassEncoding.IsOn = false;
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxRealTimeMode.IsOn = false;
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxRealTimeMode.Visibility = Visibility.Collapsed;
                ComboBoxVideoBitDepth.Visibility = Visibility.Collapsed;
                ComboBoxVideoBitDepthLimited.Visibility = Visibility.Visible;
            }
            else if (ComboBoxVideoEncoder.SelectedIndex is (int)Video.Encoders.NVENCAV1)
            {
                // av1 hardware (nvenc rtx 4000)
                mainWindow.VideoTabVideoOptimizationControl.SliderEncoderPreset.Maximum = 2;
                mainWindow.VideoTabVideoOptimizationControl.SliderEncoderPreset.Value = 1;
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxTwoPassEncoding.IsEnabled = false;
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxTwoPassEncoding.IsOn = false;
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxRealTimeMode.IsOn = false;
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxRealTimeMode.Visibility = Visibility.Collapsed;
                ComboBoxVideoBitDepth.Visibility = Visibility.Collapsed;
                ComboBoxVideoBitDepthLimited.Visibility = Visibility.Visible;
            }
            else if (ComboBoxVideoEncoder.SelectedIndex is (int)Video.Encoders.AMFAV1)
            {
                // av1 hardware (amd)
                mainWindow.VideoTabVideoOptimizationControl.SliderEncoderPreset.Maximum = 3;
                mainWindow.VideoTabVideoOptimizationControl.SliderEncoderPreset.Value = 3;
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxTwoPassEncoding.IsEnabled = false;
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxTwoPassEncoding.IsOn = false;
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxRealTimeMode.IsOn = false;
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxRealTimeMode.Visibility = Visibility.Collapsed;
                ComboBoxVideoBitDepth.Visibility = Visibility.Collapsed;
                ComboBoxVideoBitDepthLimited.Visibility = Visibility.Visible;

                ComboBoxColorFormat.SelectedIndex = 0;
                ComboBoxColorFormat.IsEnabled = false;
                ComboBoxVideoBitDepth.SelectedIndex = 0;
                ComboBoxVideoBitDepth.IsEnabled = false;
                ComboBoxVideoBitDepthLimited.IsEnabled = false;
                ComboBoxVideoBitDepthLimited.SelectedIndex = 0;
                CheckBoxVideoHDR.IsChecked = false;
                CheckBoxVideoHDR.IsEnabled = false;

            }
            if (ComboBoxVideoEncoder.SelectedIndex is (int)Video.Encoders.X264)
            {
                if (mainWindow.VideoTabVideoQualityControl.ComboBoxQualityMode.SelectedIndex == 2)
                {
                    mainWindow.VideoTabVideoOptimizationControl.CheckBoxTwoPassEncoding.IsEnabled = true;
                }

                ComboBoxVideoBitDepth.Visibility = Visibility.Collapsed;
                ComboBoxVideoBitDepthLimited.Visibility = Visibility.Visible;
            }

            mainWindow.SummaryTabControl.LabelEncoder.Content = ComboBoxVideoEncoder.SelectedIndex switch
            {
                (int)Video.Encoders.AOMFFMPEG => "aom-av1 (ffmpeg)",
                (int)Video.Encoders.RAV1EFFMPEG => "rav1e (ffmpeg)",
                (int)Video.Encoders.SVTAV1FFMPEG => "svt-av1 (ffmpeg)",
                (int)Video.Encoders.VPXVP9FFMPEG => "vpx-vp9 (ffmpeg)",
                (int)Video.Encoders.AOMENC => "aomenc (AV1)",
                (int)Video.Encoders.RAV1E => "rav1e (AV1)",
                (int)Video.Encoders.SVTAV1 => "svt-av1 (AV1)",
                (int)Video.Encoders.X265 => "x265 (HEVC)",
                (int)Video.Encoders.X264 => "x264 (AVC)",
                (int)Video.Encoders.QSVAV1 => "QuickSync (AV1)",
                (int)Video.Encoders.NVENCAV1 => "NVENC (AV1)",
                (int)Video.Encoders.AMFAV1 => "AMF (AV1)",
                _ => ""
            };

            mainWindow.VideoTabVideoOptimizationControl.UpdateSpeedLabel();
        }
    }
}
