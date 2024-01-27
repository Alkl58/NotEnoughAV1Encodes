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
            string quality = mainWindow.VideoTabVideoQualityControl.ComboBoxQualityModeRAV1EFFMPEG.SelectedIndex switch
            {
                0 => " -qp " + mainWindow.VideoTabVideoQualityControl.SliderQualityRAV1EFFMPEG.Value,
                1 => " -b:v " + mainWindow.VideoTabVideoQualityControl.TextBoxBitrateRAV1EFFMPEG.Text + "k",
                _ => ""
            };

            // Preset
            settings += quality + " -speed " + mainWindow.VideoTabVideoOptimizationControl.SliderEncoderPreset.Value;

            // Advanced Settings
            if (mainWindow.VideoTabVideoOptimizationControl.ToggleSwitchAdvancedSettings.IsOn == false)
            {
                settings += " -tile-columns 2 -tile-rows 1 -g " + mainWindow.VideoTabVideoPartialControl.GenerateKeyFrameInerval() + " -rav1e-params threads=4";
            }
            else
            {
                settings += " -tile-columns " + mainWindow.AdvancedTabControl.ComboBoxRav1eTileColumns.SelectedIndex +                     // Tile Columns
                            " -tile-rows " + mainWindow.AdvancedTabControl.ComboBoxRav1eTileRows.SelectedIndex;                            // Tile Rows

                settings += " -rav1e-params " +
                            "threads=" + mainWindow.AdvancedTabControl.ComboBoxRav1eThreads.SelectedIndex +                                // Threads
                            ":rdo-lookahead-frames=" + mainWindow.AdvancedTabControl.TextBoxRav1eLookahead.Text +                          // RDO Lookahead
                            ":tune=" + mainWindow.AdvancedTabControl.ComboBoxRav1eTune.Text;                                               // Tune

                if (mainWindow.AdvancedTabControl.TextBoxRav1eMaxGOP.Text != "0")
                    settings += ":keyint=" + mainWindow.AdvancedTabControl.TextBoxRav1eMaxGOP.Text;                                        // Keyframe Interval

                if (mainWindow.AdvancedTabControl.ComboBoxRav1eColorPrimaries.SelectedIndex != 0)
                    settings += ":primaries=" + mainWindow.AdvancedTabControl.ComboBoxRav1eColorPrimaries.Text;                            // Color Primaries
                if (mainWindow.AdvancedTabControl.ComboBoxRav1eColorTransfer.SelectedIndex != 0)
                    settings += ":transfer=" + mainWindow.AdvancedTabControl.ComboBoxRav1eColorTransfer.Text;                              // Color Transfer
                if (mainWindow.AdvancedTabControl.ComboBoxRav1eColorMatrix.SelectedIndex != 0)
                    settings += ":matrix=" + mainWindow.AdvancedTabControl.ComboBoxRav1eColorMatrix.Text;                                  // Color Matrix
            }

            return settings;
        }
    }
}
