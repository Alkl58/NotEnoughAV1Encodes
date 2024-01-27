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
                settings += " --threads " + mainWindow.ComboBoxRav1eThreads.SelectedIndex +                             // Threads
                            " --tile-cols " + mainWindow.ComboBoxRav1eTileColumns.SelectedIndex +                       // Tile Columns
                            " --tile-rows " + mainWindow.ComboBoxRav1eTileRows.SelectedIndex +                          // Tile Rows
                            " --rdo-lookahead-frames " + mainWindow.TextBoxRav1eLookahead.Text +                        // RDO Lookahead
                            " --tune " + mainWindow.ComboBoxRav1eTune.Text;                                             // Tune

                if (mainWindow.TextBoxRav1eMaxGOP.Text != "0")
                    settings += " --keyint " + mainWindow.TextBoxRav1eMaxGOP.Text;                                      // Keyframe Interval

                if (mainWindow.ComboBoxRav1eColorPrimaries.SelectedIndex != 0)
                    settings += " --primaries " + mainWindow.ComboBoxRav1eColorPrimaries.Text;                          // Color Primaries
                if (mainWindow.ComboBoxRav1eColorTransfer.SelectedIndex != 0)
                    settings += " --transfer " + mainWindow.ComboBoxRav1eColorTransfer.Text;                            // Color Transfer
                if (mainWindow.ComboBoxRav1eColorMatrix.SelectedIndex != 0)
                    settings += " --matrix " + mainWindow.ComboBoxRav1eColorMatrix.Text;                                // Color Matrix
            }

            return settings;
        }
    }
}
