using System.IO;
using System.Windows;

namespace NotEnoughAV1Encodes.Encoders
{
    class Rav1e : IEncoder
    {
        public string GetCommand()
        {
            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            string settings = "-f yuv4mpegpipe - | " +
                               "\"" + Path.Combine(Directory.GetCurrentDirectory(), "Apps", "rav1e", "rav1e.exe") + "\" - -y";

            // Quality / Bitrate Selection
            string quality = mainWindow.VideoTabVideoQualityControl.ComboBoxQualityModeRAV1E.SelectedIndex switch
            {
                0 => " --quantizer " + mainWindow.VideoTabVideoQualityControl.SliderQualityRAV1E.Value,
                1 => " --bitrate " + mainWindow.VideoTabVideoQualityControl.TextBoxBitrateRAV1E.Text,
                _ => ""
            };

            // Preset
            settings += quality + " --speed " + mainWindow.VideoTabVideoOptimizationControl.SliderEncoderPreset.Value;

            // Advanced Settings
            if (mainWindow.VideoTabVideoOptimizationControl.ToggleSwitchAdvancedSettings.IsOn == false)
            {
                settings += " --threads 4 --tile-cols 2 --tile-rows 1 --keyint " + mainWindow.VideoTabVideoPartialControl.GenerateKeyFrameInerval();
            }
            else
            {
                settings += " --threads " + mainWindow.AdvancedTabControl.ComboBoxRav1eThreads.SelectedIndex +                             // Threads
                            " --tile-cols " + mainWindow.AdvancedTabControl.ComboBoxRav1eTileColumns.SelectedIndex +                       // Tile Columns
                            " --tile-rows " + mainWindow.AdvancedTabControl.ComboBoxRav1eTileRows.SelectedIndex +                          // Tile Rows
                            " --rdo-lookahead-frames " + mainWindow.AdvancedTabControl.TextBoxRav1eLookahead.Text +                        // RDO Lookahead
                            " --tune " + mainWindow.AdvancedTabControl.ComboBoxRav1eTune.Text;                                             // Tune

                if (mainWindow.AdvancedTabControl.TextBoxRav1eMaxGOP.Text != "0")
                    settings += " --keyint " + mainWindow.AdvancedTabControl.TextBoxRav1eMaxGOP.Text;                                      // Keyframe Interval

                if (mainWindow.AdvancedTabControl.ComboBoxRav1eColorPrimaries.SelectedIndex != 0)
                    settings += " --primaries " + mainWindow.AdvancedTabControl.ComboBoxRav1eColorPrimaries.Text;                          // Color Primaries
                if (mainWindow.AdvancedTabControl.ComboBoxRav1eColorTransfer.SelectedIndex != 0)
                    settings += " --transfer " + mainWindow.AdvancedTabControl.ComboBoxRav1eColorTransfer.Text;                            // Color Transfer
                if (mainWindow.AdvancedTabControl.ComboBoxRav1eColorMatrix.SelectedIndex != 0)
                    settings += " --matrix " + mainWindow.AdvancedTabControl.ComboBoxRav1eColorMatrix.Text;                                // Color Matrix
            }

            return settings;
        }
    }
}
