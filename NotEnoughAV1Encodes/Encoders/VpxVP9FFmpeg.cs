using System.Windows;

namespace NotEnoughAV1Encodes.Encoders
{
    class VpxVP9FFmpeg : IEncoder
    {
        public string GetCommand()
        {
            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            string settings = "-c:v libvpx-vp9";

            // Quality / Bitrate Selection
            string quality = mainWindow.VideoTabVideoQualityControl.ComboBoxQualityModeVP9FFMPEG.SelectedIndex switch
            {
                0 => " -crf " + mainWindow.VideoTabVideoQualityControl.SliderQualityVP9FFMPEG.Value + " -b:v 0",
                1 => " -crf " + mainWindow.VideoTabVideoQualityControl.SliderQualityVP9FFMPEG.Value + " -b:v " + mainWindow.VideoTabVideoQualityControl.TextBoxMaxBitrateVP9FFMPEG.Text + "k",
                2 => " -b:v " + mainWindow.VideoTabVideoQualityControl.TextBoxAVGBitrateVP9FFMPEG.Text + "k",
                3 => " -minrate " + mainWindow.VideoTabVideoQualityControl.TextBoxMinBitrateVP9FFMPEG.Text + "k -b:v " + mainWindow.VideoTabVideoQualityControl.TextBoxAVGBitrateVP9FFMPEG.Text + "k -maxrate " + mainWindow.VideoTabVideoQualityControl.TextBoxMaxBitrateVP9FFMPEG.Text + "k",
                _ => ""
            };

            // Preset
            settings += quality + " -cpu-used " + mainWindow.VideoTabVideoOptimizationControl.SliderEncoderPreset.Value;

            // Advanced Settings
            if (mainWindow.VideoTabVideoOptimizationControl.ToggleSwitchAdvancedSettings.IsOn == false)
            {
                settings += " -threads 4 -tile-columns 2 -tile-rows 1 -g " + mainWindow.VideoTabVideoPartialControl.GenerateKeyFrameInerval();
            }
            else
            {
                settings += " -threads " + mainWindow.AdvancedTabControl.ComboBoxVP9Threads.Text +                                         // Max Threads
                            " -tile-columns " + mainWindow.AdvancedTabControl.ComboBoxVP9TileColumns.SelectedIndex +                       // Tile Columns
                            " -tile-rows " + mainWindow.AdvancedTabControl.ComboBoxVP9TileRows.SelectedIndex +                             // Tile Rows
                            " -lag-in-frames " + mainWindow.AdvancedTabControl.TextBoxVP9LagInFrames.Text +                                // Lag in Frames
                            " -g " + mainWindow.AdvancedTabControl.TextBoxVP9MaxKF.Text +                                                  // Max GOP
                            " -aq-mode " + mainWindow.AdvancedTabControl.ComboBoxVP9AQMode.SelectedIndex +                                 // AQ-Mode
                            " -tune " + mainWindow.AdvancedTabControl.ComboBoxVP9ATune.SelectedIndex +                                     // Tune
                            " -tune-content " + mainWindow.AdvancedTabControl.ComboBoxVP9ATuneContent.SelectedIndex;                       // Tune-Content

                if (mainWindow.AdvancedTabControl.CheckBoxVP9ARNR.IsChecked == true)
                {
                    settings += " -arnr-maxframes " + mainWindow.AdvancedTabControl.ComboBoxAomencVP9Max.Text +                            // ARNR Max Frames
                                " -arnr-strength " + mainWindow.AdvancedTabControl.ComboBoxAomencVP9Strength.Text +                        // ARNR Strength
                                " -arnr-type " + mainWindow.AdvancedTabControl.ComboBoxAomencVP9ARNRType.Text;                             // ARNR Type
                }
            }

            return settings;
        }
    }
}
