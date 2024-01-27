using NotEnoughAV1Encodes.Encoders;
using System.Windows;
using System.Windows.Controls;

namespace NotEnoughAV1Encodes.Controls.Partials
{
    public partial class VideoTabOptimization : UserControl
    {
        public VideoTabOptimization()
        {
            InitializeComponent();
        }

        private void SliderEncoderPreset_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MainWindow.startupLock) return;

            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            // Shows / Hides Real Time Mode CheckBox
            if (CheckBoxRealTimeMode != null && mainWindow.VideoTabVideoPartialControl.ComboBoxVideoEncoder != null)
            {
                if (mainWindow.VideoTabVideoPartialControl.ComboBoxVideoEncoder.SelectedIndex == (int)Video.Encoders.AOMFFMPEG || mainWindow.VideoTabVideoPartialControl.ComboBoxVideoEncoder.SelectedIndex == (int)Video.Encoders.AOMENC)
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

            // x264 / x265
            if (mainWindow.VideoTabVideoPartialControl.ComboBoxVideoEncoder.SelectedIndex is (int)Video.Encoders.X265 or (int)Video.Encoders.X264)
            {
                LabelSpeedValue.Content = mainWindow.GenerateMPEGEncoderSpeed();
            }

            // av1 hardware (Intel Arc)
            if (mainWindow.VideoTabVideoPartialControl.ComboBoxVideoEncoder.SelectedIndex is (int)Video.Encoders.QSVAV1)
            {
                LabelSpeedValue.Content = mainWindow.GenerateQuickSyncEncoderSpeed();
            }

            // av1 hardware (nvenc rtx 4000)
            if (mainWindow.VideoTabVideoPartialControl.ComboBoxVideoEncoder.SelectedIndex is (int)Video.Encoders.NVENCAV1)
            {
                LabelSpeedValue.Content = mainWindow.GenerateNVENCEncoderSpeed();
            }

            // av1 hardware (AMD AMF)
            if (mainWindow.VideoTabVideoPartialControl.ComboBoxVideoEncoder.SelectedIndex is (int)Video.Encoders.AMFAV1)
            {
                LabelSpeedValue.Content = AMFAV1.GetSpeed(SliderEncoderPreset.Value);
            }
        }

        private void CheckBoxTwoPassEncoding_Toggled(object sender, RoutedEventArgs e)
        {
            if (MainWindow.startupLock) return;

            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (mainWindow.VideoTabVideoPartialControl.ComboBoxVideoEncoder.SelectedIndex == (int)Video.Encoders.SVTAV1 && mainWindow.VideoTabVideoQualityControl.ComboBoxQualityModeSVTAV1.SelectedIndex == 0 && CheckBoxTwoPassEncoding.IsOn)
            {
                CheckBoxTwoPassEncoding.IsOn = false;
            }

            if (mainWindow.VideoTabVideoPartialControl.ComboBoxVideoEncoder.SelectedIndex == (int)Video.Encoders.SVTAV1FFMPEG && mainWindow.VideoTabVideoQualityControl.ComboBoxQualityModeSVTAV1FFMPEG.SelectedIndex == 0 && CheckBoxTwoPassEncoding.IsOn)
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
    }
}
