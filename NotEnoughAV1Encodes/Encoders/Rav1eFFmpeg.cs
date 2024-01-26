using System.Windows;

namespace NotEnoughAV1Encodes.Encoders
{
    class Rav1eFFmpeg : IEncoder
    {
        public string GetCommand()
        {
            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            string settings = "-c:v librav1e";

            // Quality / Bitrate Selection
            string quality = mainWindow.ComboBoxQualityModeRAV1EFFMPEG.SelectedIndex switch
            {
                0 => " -qp " + mainWindow.SliderQualityRAV1EFFMPEG.Value,
                1 => " -b:v " + mainWindow.TextBoxBitrateRAV1EFFMPEG.Text + "k",
                _ => ""
            };

            // Preset
            settings += quality + " -speed " + mainWindow.SliderEncoderPreset.Value;

            // Advanced Settings
            if (mainWindow.ToggleSwitchAdvancedSettings.IsOn == false)
            {
                settings += " -tile-columns 2 -tile-rows 1 -g " + mainWindow.GenerateKeyFrameInerval() + " -rav1e-params threads=4";
            }
            else
            {
                settings += " -tile-columns " + mainWindow.ComboBoxRav1eTileColumns.SelectedIndex +                     // Tile Columns
                            " -tile-rows " + mainWindow.ComboBoxRav1eTileRows.SelectedIndex;                            // Tile Rows

                settings += " -rav1e-params " +
                            "threads=" + mainWindow.ComboBoxRav1eThreads.SelectedIndex +                                // Threads
                            ":rdo-lookahead-frames=" + mainWindow.TextBoxRav1eLookahead.Text +                          // RDO Lookahead
                            ":tune=" + mainWindow.ComboBoxRav1eTune.Text;                                               // Tune

                if (mainWindow.TextBoxRav1eMaxGOP.Text != "0")
                    settings += ":keyint=" + mainWindow.TextBoxRav1eMaxGOP.Text;                                        // Keyframe Interval

                if (mainWindow.ComboBoxRav1eColorPrimaries.SelectedIndex != 0)
                    settings += ":primaries=" + mainWindow.ComboBoxRav1eColorPrimaries.Text;                            // Color Primaries
                if (mainWindow.ComboBoxRav1eColorTransfer.SelectedIndex != 0)
                    settings += ":transfer=" + mainWindow.ComboBoxRav1eColorTransfer.Text;                              // Color Transfer
                if (mainWindow.ComboBoxRav1eColorMatrix.SelectedIndex != 0)
                    settings += ":matrix=" + mainWindow.ComboBoxRav1eColorMatrix.Text;                                  // Color Matrix
            }

            return settings;
        }
    }
}
