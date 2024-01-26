using System.IO;
using System.Windows;

namespace NotEnoughAV1Encodes.Encoders
{
    class SvtAV1 : IEncoder
    {
        public string GetCommand()
        {
            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            string settings = "-nostdin -f yuv4mpegpipe - | " +
                              "\"" + Path.Combine(Directory.GetCurrentDirectory(), "Apps", "svt-av1", "SvtAv1EncApp.exe") + "\" -i stdin";

            // Quality / Bitrate Selection
            string quality = mainWindow.ComboBoxQualityModeSVTAV1.SelectedIndex switch
            {
                0 => " --rc 0 --crf " + mainWindow.SliderQualitySVTAV1.Value,
                1 => " --rc 1 --tbr " + mainWindow.TextBoxBitrateSVTAV1.Text,
                _ => ""
            };

            // Preset
            settings += quality + " --preset " + mainWindow.SliderEncoderPreset.Value;

            // Advanced Settings
            if (mainWindow.ToggleSwitchAdvancedSettings.IsOn == false)
            {
                settings += " --keyint " + mainWindow.GenerateKeyFrameInerval();

            }
            else
            {
                settings += " --tile-columns " + mainWindow.ComboBoxSVTAV1TileColumns.Text +                             // Tile Columns
                            " --tile-rows " + mainWindow.ComboBoxSVTAV1TileRows.Text +                                   // Tile Rows
                            " --keyint " + mainWindow.TextBoxSVTAV1MaxGOP.Text +                                         // Keyframe Interval
                            " --lookahead " + mainWindow.TextBoxSVTAV1Lookahead.Text +                                   // Lookahead
                            " --aq-mode " + mainWindow.ComboBoxSVTAV1AQMode.Text +                                       // AQ Mode
                            " --film-grain " + mainWindow.TextBoxSVTAV1FilmGrain.Text +                                  // Film Grain
                            " --film-grain-denoise " + mainWindow.TextBoxSVTAV1FilmGrainDenoise.Text;                    // Film Grain Denoise                      
            }

            return settings;
        }
    }
}
