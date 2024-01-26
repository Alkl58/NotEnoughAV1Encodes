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
            string quality = mainWindow.ComboBoxQualityModeVP9FFMPEG.SelectedIndex switch
            {
                0 => " -crf " + mainWindow.SliderQualityVP9FFMPEG.Value + " -b:v 0",
                1 => " -crf " + mainWindow.SliderQualityVP9FFMPEG.Value + " -b:v " + mainWindow.TextBoxMaxBitrateVP9FFMPEG.Text + "k",
                2 => " -b:v " + mainWindow.TextBoxAVGBitrateVP9FFMPEG.Text + "k",
                3 => " -minrate " + mainWindow.TextBoxMinBitrateVP9FFMPEG.Text + "k -b:v " + mainWindow.TextBoxAVGBitrateVP9FFMPEG.Text + "k -maxrate " + mainWindow.TextBoxMaxBitrateVP9FFMPEG.Text + "k",
                _ => ""
            };

            // Preset
            settings += quality + " -cpu-used " + mainWindow.SliderEncoderPreset.Value;

            // Advanced Settings
            if (mainWindow.ToggleSwitchAdvancedSettings.IsOn == false)
            {
                settings += " -threads 4 -tile-columns 2 -tile-rows 1 -g " + mainWindow.GenerateKeyFrameInerval();
            }
            else
            {
                settings += " -threads " + mainWindow.ComboBoxVP9Threads.Text +                                         // Max Threads
                            " -tile-columns " + mainWindow.ComboBoxVP9TileColumns.SelectedIndex +                       // Tile Columns
                            " -tile-rows " + mainWindow.ComboBoxVP9TileRows.SelectedIndex +                             // Tile Rows
                            " -lag-in-frames " + mainWindow.TextBoxVP9LagInFrames.Text +                                // Lag in Frames
                            " -g " + mainWindow.TextBoxVP9MaxKF.Text +                                                  // Max GOP
                            " -aq-mode " + mainWindow.ComboBoxVP9AQMode.SelectedIndex +                                 // AQ-Mode
                            " -tune " + mainWindow.ComboBoxVP9ATune.SelectedIndex +                                     // Tune
                            " -tune-content " + mainWindow.ComboBoxVP9ATuneContent.SelectedIndex;                       // Tune-Content

                if (mainWindow.CheckBoxVP9ARNR.IsChecked == true)
                {
                    settings += " -arnr-maxframes " + mainWindow.ComboBoxAomencVP9Max.Text +                            // ARNR Max Frames
                                " -arnr-strength " + mainWindow.ComboBoxAomencVP9Strength.Text +                        // ARNR Strength
                                " -arnr-type " + mainWindow.ComboBoxAomencVP9ARNRType.Text;                             // ARNR Type
                }
            }

            return settings;
        }
    }
}
