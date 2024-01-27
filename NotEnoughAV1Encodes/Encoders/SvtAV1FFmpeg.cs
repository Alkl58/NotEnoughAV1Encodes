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
            string quality = mainWindow.VideoTabVideoQualityControl.ComboBoxQualityModeSVTAV1FFMPEG.SelectedIndex switch
            {
                0 => " -crf " + mainWindow.VideoTabVideoQualityControl.SliderQualitySVTAV1FFMPEG.Value,
                1 => " -b:v " + mainWindow.VideoTabVideoQualityControl.TextBoxBitrateSVTAV1FFMPEG.Text + "k",
                _ => ""
            };

            // Preset
            settings += quality + " -preset " + mainWindow.VideoTabVideoOptimizationControl.SliderEncoderPreset.Value;

            // Advanced Settings
            if (mainWindow.VideoTabVideoOptimizationControl.ToggleSwitchAdvancedSettings.IsOn == false)
            {
                settings += " -g " + mainWindow.VideoTabVideoPartialControl.GenerateKeyFrameInerval();
            }
            else
            {
                settings += " -tile_columns " + mainWindow.AdvancedTabControl.ComboBoxSVTAV1TileColumns.Text +                             // Tile Columns
                            " -tile_rows " + mainWindow.AdvancedTabControl.ComboBoxSVTAV1TileRows.Text +                                   // Tile Rows
                            " -g " + mainWindow.AdvancedTabControl.TextBoxSVTAV1MaxGOP.Text +                                              // Keyframe Interval
                            " -la_depth " + mainWindow.AdvancedTabControl.TextBoxSVTAV1Lookahead.Text +                                    // Lookahead
                            " -svtav1-params " +
                            "aq-mode=" + mainWindow.AdvancedTabControl.ComboBoxSVTAV1AQMode.Text +                                         // AQ Mode
                            ":film-grain=" + mainWindow.AdvancedTabControl.TextBoxSVTAV1FilmGrain.Text +                                   // Film Grain
                            ":film-grain-denoise=" + mainWindow.AdvancedTabControl.TextBoxSVTAV1FilmGrainDenoise.Text;                     // Film Grain Denoise
            }

            return settings;
        }
    }
}
