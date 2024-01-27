using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NotEnoughAV1Encodes.Controls.Partials
{
    public partial class VideoTabQuality : UserControl
    {
        public VideoTabQuality()
        {
            InitializeComponent();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Validates that the TextBox Input are only numbers
            Regex regex = new("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void ComboBoxQualityMode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (MainWindow.startupLock) return;
            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            // Hide all
            LabelQuantizer.Visibility = Visibility.Collapsed;
            SliderQualityAOMFFMPEG.Visibility = Visibility.Collapsed;
            LabelQuantizerPreview.Visibility = Visibility.Collapsed;
            LabelBitrateMin.Visibility = Visibility.Collapsed;
            TextBoxMinBitrateAOMFFMPEG.Visibility = Visibility.Collapsed;
            LabelBitrateAvg.Visibility = Visibility.Collapsed;
            TextBoxAVGBitrateAOMFFMPEG.Visibility = Visibility.Collapsed;
            LabelBitrateMax.Visibility = Visibility.Collapsed;
            TextBoxMaxBitrateAOMFFMPEG.Visibility = Visibility.Collapsed;
            LabelTargetVMAF.Visibility = Visibility.Collapsed;
            LabelTargetVMAFPreview.Visibility = Visibility.Collapsed;
            SliderTargetVMAF.Visibility = Visibility.Collapsed;
            LabelTargetVMAFProbes.Visibility = Visibility.Collapsed;
            SliderTargetVMAFProbes.Visibility = Visibility.Collapsed;
            LabelTargetVMAFProbesPreview.Visibility = Visibility.Collapsed;
            LabelTargetVMAFMinQ.Visibility = Visibility.Collapsed;
            SliderTargetVMAFMinQ.Visibility = Visibility.Collapsed;
            LabelTargetVMAFMinQPreview.Visibility = Visibility.Collapsed;
            LabelTargetVMAFMaxQ.Visibility = Visibility.Collapsed;
            SliderTargetVMAFMaxQ.Visibility = Visibility.Collapsed;
            LabelTargetVMAFMaxQPreview.Visibility = Visibility.Collapsed;
            LabelTargetVMAFMaxProbeLength.Visibility = Visibility.Collapsed;
            SliderTargetVMAFMaxProbeLength.Visibility = Visibility.Collapsed;
            LabelTargetVMAFMaxProbeLengthPreview.Visibility = Visibility.Collapsed;
            mainWindow.PresetSettings.TargetVMAF = false;

            if (ComboBoxQualityMode.SelectedIndex == 0)
            {
                // Constant Quality
                LabelQuantizer.Visibility = Visibility.Visible;
                SliderQualityAOMFFMPEG.Visibility = Visibility.Visible;
                LabelQuantizerPreview.Visibility = Visibility.Visible;
            }
            else if (ComboBoxQualityMode.SelectedIndex == 1)
            {
                // Constrained Quality
                TextBoxMaxBitrateAOMFFMPEG.Visibility = Visibility.Visible;
                LabelBitrateMax.Visibility = Visibility.Visible;
                LabelQuantizer.Visibility = Visibility.Visible;
                SliderQualityAOMFFMPEG.Visibility = Visibility.Visible;
                LabelQuantizerPreview.Visibility = Visibility.Visible;
            }
            else if (ComboBoxQualityMode.SelectedIndex == 2)
            {
                // Average Bitrate
                LabelBitrateAvg.Visibility = Visibility.Visible;
                TextBoxAVGBitrateAOMFFMPEG.Visibility = Visibility.Visible;
            }
            else if (ComboBoxQualityMode.SelectedIndex == 3)
            {
                // Constrained Bitrate
                LabelBitrateMin.Visibility = Visibility.Visible;
                TextBoxMinBitrateAOMFFMPEG.Visibility = Visibility.Visible;
                LabelBitrateAvg.Visibility = Visibility.Visible;
                TextBoxAVGBitrateAOMFFMPEG.Visibility = Visibility.Visible;
                LabelBitrateMax.Visibility = Visibility.Visible;
                TextBoxMaxBitrateAOMFFMPEG.Visibility = Visibility.Visible;
            }
            else if (ComboBoxQualityMode.SelectedIndex == 4)
            {
                // Target VMAF
                mainWindow.PresetSettings.TargetVMAF = true;
                LabelTargetVMAF.Visibility = Visibility.Visible;
                LabelTargetVMAFPreview.Visibility = Visibility.Visible;
                SliderTargetVMAF.Visibility = Visibility.Visible;
                LabelTargetVMAFProbes.Visibility = Visibility.Visible;
                SliderTargetVMAFProbes.Visibility = Visibility.Visible;
                LabelTargetVMAFProbesPreview.Visibility = Visibility.Visible;
                LabelTargetVMAFMinQ.Visibility = Visibility.Visible;
                SliderTargetVMAFMinQ.Visibility = Visibility.Visible;
                LabelTargetVMAFMinQPreview.Visibility = Visibility.Visible;
                LabelTargetVMAFMaxQ.Visibility = Visibility.Visible;
                SliderTargetVMAFMaxQ.Visibility = Visibility.Visible;
                LabelTargetVMAFMaxQPreview.Visibility = Visibility.Visible;
                LabelTargetVMAFMaxProbeLength.Visibility = Visibility.Visible;
                SliderTargetVMAFMaxProbeLength.Visibility = Visibility.Visible;
                LabelTargetVMAFMaxProbeLengthPreview.Visibility = Visibility.Visible;
            }
        }

        private void SliderTargetVMAFMinQ_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SliderTargetVMAF == null) return;
            if (SliderTargetVMAFMinQ.Value > SliderTargetVMAFMaxQ.Value)
            {
                SliderTargetVMAFMaxQ.Value = SliderTargetVMAFMinQ.Value;
            }
        }

        private void SliderTargetVMAFMaxQ_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SliderTargetVMAF == null) return;
            if (SliderTargetVMAFMinQ.Value > SliderTargetVMAFMaxQ.Value)
            {
                SliderTargetVMAFMinQ.Value = SliderTargetVMAFMaxQ.Value;
            }
        }

        private void ComboBoxQualityModeRAV1EFFMPEG_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ComboBoxQualityModeRAV1EFFMPEG.SelectedIndex == 0)
            {
                SliderQualityRAV1EFFMPEG.IsEnabled = true;
                TextBoxBitrateRAV1EFFMPEG.IsEnabled = false;
            }
            else if (ComboBoxQualityModeRAV1EFFMPEG.SelectedIndex == 1)
            {
                SliderQualityRAV1EFFMPEG.IsEnabled = false;
                TextBoxBitrateRAV1EFFMPEG.IsEnabled = true;
            }
        }

        private void ComboBoxQualityModeSVTAV1FFMPEG_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ComboBoxQualityModeSVTAV1FFMPEG.SelectedIndex == 0)
            {
                SliderQualitySVTAV1FFMPEG.IsEnabled = true;
                TextBoxBitrateSVTAV1FFMPEG.IsEnabled = false;
            }
            else if (ComboBoxQualityModeSVTAV1FFMPEG.SelectedIndex == 1)
            {
                SliderQualitySVTAV1FFMPEG.IsEnabled = false;
                TextBoxBitrateSVTAV1FFMPEG.IsEnabled = true;
            }
        }

        private void ComboBoxQualityModeVP9FFMPEG_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ComboBoxQualityModeVP9FFMPEG.SelectedIndex == 0)
            {
                SliderQualityVP9FFMPEG.IsEnabled = true;
                TextBoxAVGBitrateVP9FFMPEG.IsEnabled = false;
                TextBoxMaxBitrateVP9FFMPEG.IsEnabled = false;
                TextBoxMinBitrateVP9FFMPEG.IsEnabled = false;
            }
            else if (ComboBoxQualityModeVP9FFMPEG.SelectedIndex == 1)
            {
                SliderQualityVP9FFMPEG.IsEnabled = true;
                TextBoxAVGBitrateVP9FFMPEG.IsEnabled = false;
                TextBoxMaxBitrateVP9FFMPEG.IsEnabled = true;
                TextBoxMinBitrateVP9FFMPEG.IsEnabled = false;
            }
            else if (ComboBoxQualityModeVP9FFMPEG.SelectedIndex == 2)
            {
                SliderQualityVP9FFMPEG.IsEnabled = false;
                TextBoxAVGBitrateVP9FFMPEG.IsEnabled = true;
                TextBoxMaxBitrateVP9FFMPEG.IsEnabled = false;
                TextBoxMinBitrateVP9FFMPEG.IsEnabled = false;
            }
            else if (ComboBoxQualityModeVP9FFMPEG.SelectedIndex == 3)
            {
                SliderQualityVP9FFMPEG.IsEnabled = false;
                TextBoxAVGBitrateVP9FFMPEG.IsEnabled = true;
                TextBoxMaxBitrateVP9FFMPEG.IsEnabled = true;
                TextBoxMinBitrateVP9FFMPEG.IsEnabled = true;
            }
        }

        private void ComboBoxQualityModeAOMENC_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ComboBoxQualityModeAOMENC.SelectedIndex == 0)
            {
                SliderQualityAOMENC.IsEnabled = true;
                TextBoxBitrateAOMENC.IsEnabled = false;
            }
            else if (ComboBoxQualityModeAOMENC.SelectedIndex == 1)
            {
                SliderQualityAOMENC.IsEnabled = true;
                TextBoxBitrateAOMENC.IsEnabled = true;
            }
            else if (ComboBoxQualityModeAOMENC.SelectedIndex == 2)
            {
                SliderQualityAOMENC.IsEnabled = false;
                TextBoxBitrateAOMENC.IsEnabled = true;
            }
            else if (ComboBoxQualityModeAOMENC.SelectedIndex == 3)
            {
                SliderQualityAOMENC.IsEnabled = false;
                TextBoxBitrateAOMENC.IsEnabled = true;
            }
        }

        private void ComboBoxQualityModeRAV1E_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ComboBoxQualityModeRAV1E.SelectedIndex == 0)
            {
                SliderQualityRAV1E.IsEnabled = true;
                TextBoxBitrateRAV1E.IsEnabled = false;
            }
            else if (ComboBoxQualityModeRAV1E.SelectedIndex == 1)
            {
                SliderQualityRAV1E.IsEnabled = false;
                TextBoxBitrateRAV1E.IsEnabled = true;
            }
        }

        private void ComboBoxQualityModeSVTAV1_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ComboBoxQualityModeSVTAV1.SelectedIndex == 0)
            {
                SliderQualitySVTAV1.IsEnabled = true;
                TextBoxBitrateSVTAV1.IsEnabled = false;
            }
            else if (ComboBoxQualityModeSVTAV1.SelectedIndex == 1)
            {
                SliderQualitySVTAV1.IsEnabled = false;
                TextBoxBitrateSVTAV1.IsEnabled = true;
            }
        }

        private void ComboBoxQualityModeX26x_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (MainWindow.startupLock) return;

            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (ComboBoxQualityModeX26x.SelectedIndex == 0)
            {
                SliderQualityX26x.IsEnabled = true;
                TextBoxBitrateX26x.IsEnabled = false;
            }
            else if (ComboBoxQualityModeX26x.SelectedIndex == 1)
            {
                SliderQualityX26x.IsEnabled = false;
                TextBoxBitrateX26x.IsEnabled = true;
            }
            if (mainWindow.VideoTabVideoPartialControl.ComboBoxVideoEncoder.SelectedIndex is (int)Video.Encoders.X264 && ComboBoxQualityModeX26x.SelectedIndex == 1)
            {
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxTwoPassEncoding.IsEnabled = true;
            }
            else if (mainWindow.VideoTabVideoPartialControl.ComboBoxVideoEncoder.SelectedIndex is (int)Video.Encoders.X264 && ComboBoxQualityModeX26x.SelectedIndex != 1)
            {
                mainWindow.VideoTabVideoOptimizationControl.CheckBoxTwoPassEncoding.IsEnabled = false;
            }
        }

        private void ComboBoxQualityModeQSVAV1_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ComboBoxQualityModeQSVAV1.SelectedIndex == 0)
            {
                SliderQualityQSVAV1.IsEnabled = true;
                TextBoxBitrateQSVAV1.IsEnabled = false;
            }
            else if (ComboBoxQualityModeQSVAV1.SelectedIndex == 1)
            {
                SliderQualityQSVAV1.IsEnabled = true;
                TextBoxBitrateQSVAV1.IsEnabled = false;
            }
            else if (ComboBoxQualityModeQSVAV1.SelectedIndex == 2)
            {
                SliderQualityQSVAV1.IsEnabled = false;
                TextBoxBitrateQSVAV1.IsEnabled = true;
            }
            else if (ComboBoxQualityModeQSVAV1.SelectedIndex == 3)
            {
                SliderQualityQSVAV1.IsEnabled = false;
                TextBoxBitrateQSVAV1.IsEnabled = true;
            }
        }

        private void ComboBoxQualityModeNVENCAV1_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ComboBoxQualityModeNVENCAV1.SelectedIndex == 0)
            {
                SliderQualityNVENCAV1.IsEnabled = true;
                TextBoxBitrateNVENCAV1.IsEnabled = false;
            }
            else if (ComboBoxQualityModeNVENCAV1.SelectedIndex == 1)
            {
                SliderQualityNVENCAV1.IsEnabled = false;
                TextBoxBitrateNVENCAV1.IsEnabled = true;
            }
            else if (ComboBoxQualityModeNVENCAV1.SelectedIndex == 2)
            {
                SliderQualityNVENCAV1.IsEnabled = false;
                TextBoxBitrateNVENCAV1.IsEnabled = true;
            }
        }

        private void ComboBoxQualityModeAMFAV1_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            SliderQualityAMFAV1.IsEnabled = false;
            TextBoxBitrateAMFAV1.IsEnabled = false;

            if (ComboBoxQualityModeAMFAV1.SelectedIndex == 0)
            {
                // CQP - Constant Quantization
                // => QP Slider
                SliderQualityAMFAV1.IsEnabled = true;
            }
            else if (ComboBoxQualityModeAMFAV1.SelectedIndex == 1)
            {
                // CBR - Constant Bitrate
                // => Bitrate Box
                TextBoxBitrateAMFAV1.IsEnabled = true;
            }
            else if (ComboBoxQualityModeAMFAV1.SelectedIndex == 2)
            {
                // HQCBR - High Quality Constant Bitrate
                // => Bitrate Box
                TextBoxBitrateAMFAV1.IsEnabled = true;
            }
            else if (ComboBoxQualityModeAMFAV1.SelectedIndex == 3)
            {
                // QVBR - Quality Variable Bitrate
                // => Bitrate Box + QP Slider
                SliderQualityAMFAV1.IsEnabled = true;
                TextBoxBitrateAMFAV1.IsEnabled = true;
            }
            else if (ComboBoxQualityModeAMFAV1.SelectedIndex == 4)
            {
                // HQVBR - High Quality Variable Bitrate
                // => Bitrate Box
                TextBoxBitrateAMFAV1.IsEnabled = true;
            }
        }
    }
}
