using System.Windows;

namespace NotEnoughAV1Encodes.Encoders
{
    class SvtAV1FFmpeg : IEncoder
    {
        public string GetCommand()
        {
            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            string settings = "-c:v libsvtav1";

            // Quality / Bitrate Selection
            string quality = mainWindow.ComboBoxQualityModeSVTAV1FFMPEG.SelectedIndex switch
            {
                0 => " -crf " + mainWindow.SliderQualitySVTAV1FFMPEG.Value,
                1 => " -b:v " + mainWindow.TextBoxBitrateSVTAV1FFMPEG.Text + "k",
                _ => ""
            };

            // Preset
            settings += quality + " -preset " + mainWindow.SliderEncoderPreset.Value;

            // Advanced Settings
            if (mainWindow.ToggleSwitchAdvancedSettings.IsOn == false)
            {
                settings += " -g " + mainWindow.GenerateKeyFrameInerval();
            }
            else
            {
                settings += " -tile_columns " + mainWindow.ComboBoxSVTAV1TileColumns.Text +                             // Tile Columns
                            " -tile_rows " + mainWindow.ComboBoxSVTAV1TileRows.Text +                                   // Tile Rows
                            " -g " + mainWindow.TextBoxSVTAV1MaxGOP.Text +                                              // Keyframe Interval
                            " -la_depth " + mainWindow.TextBoxSVTAV1Lookahead.Text +                                    // Lookahead
                            " -svtav1-params " +
                            "aq-mode=" + mainWindow.ComboBoxSVTAV1AQMode.Text +                                         // AQ Mode
                            ":film-grain=" + mainWindow.TextBoxSVTAV1FilmGrain.Text +                                   // Film Grain
                            ":film-grain-denoise=" + mainWindow.TextBoxSVTAV1FilmGrainDenoise.Text;                     // Film Grain Denoise
            }

            return settings;
        }
    }
}
