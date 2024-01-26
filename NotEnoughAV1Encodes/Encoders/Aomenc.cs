using System.IO;
using System.Windows;

namespace NotEnoughAV1Encodes.Encoders
{
    class Aomenc : IEncoder
    {
        public string GetCommand()
        {
            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            string settings = "-f yuv4mpegpipe - | " +
                              "\"" + Path.Combine(Directory.GetCurrentDirectory(), "Apps", "aomenc", "aomenc.exe") + "\" -";

            // Quality / Bitrate Selection
            string quality = mainWindow.ComboBoxQualityModeAOMENC.SelectedIndex switch
            {
                0 => " --cq-level=" + mainWindow.SliderQualityAOMENC.Value + " --end-usage=q",
                1 => " --cq-level=" + mainWindow.SliderQualityAOMENC.Value + " --target-bitrate=" + mainWindow.TextBoxBitrateAOMENC.Text + " --end-usage=cq",
                2 => " --target-bitrate=" + mainWindow.TextBoxBitrateAOMENC.Text + " --end-usage=vbr",
                3 => " --target-bitrate=" + mainWindow.TextBoxBitrateAOMENC.Text + " --end-usage=cbr",
                _ => ""
            };

            // Preset
            settings += quality + " --cpu-used=" + mainWindow.SliderEncoderPreset.Value;

            // Advanced Settings
            if (mainWindow.ToggleSwitchAdvancedSettings.IsOn == false)
            {
                settings += " --threads=4 --tile-columns=2 --tile-rows=1 --kf-max-dist=" + mainWindow.GenerateKeyFrameInerval();
            }
            else
            {
                settings += " --threads=" + mainWindow.ComboBoxAomencThreads.Text +                                     // Threads
                            " --tile-columns=" + mainWindow.ComboBoxAomencTileColumns.Text +                            // Tile Columns
                            " --tile-rows=" + mainWindow.ComboBoxAomencTileRows.Text +                                  // Tile Rows
                            " --lag-in-frames=" + mainWindow.TextBoxAomencLagInFrames.Text +                            // Lag in Frames
                            " --sharpness=" + mainWindow.ComboBoxAomencSharpness.Text +                                 // Sharpness (Filter)
                            " --aq-mode=" + mainWindow.ComboBoxAomencAQMode.SelectedIndex +                             // AQ-Mode
                            " --enable-keyframe-filtering=" + mainWindow.ComboBoxAomencKeyFiltering.SelectedIndex +     // Key Frame Filtering
                            " --tune=" + mainWindow.ComboBoxAomencTune.Text +                                           // Tune
                            " --tune-content=" + mainWindow.ComboBoxAomencTuneContent.Text;                             // Tune-Content

                if (mainWindow.TextBoxAomencMaxGOP.Text != "0")
                    settings += " --kf-max-dist=" + mainWindow.TextBoxAomencMaxGOP.Text;                                // Keyframe Interval
                if (mainWindow.CheckBoxAomencRowMT.IsChecked == false)
                    settings += " --row-mt=0";                                                               // Row Based Multithreading

                if (mainWindow.ComboBoxAomencColorPrimaries.SelectedIndex != 0)
                    settings += " --color-primaries=" + mainWindow.ComboBoxAomencColorPrimaries.Text;                   // Color Primaries
                if (mainWindow.ComboBoxAomencColorTransfer.SelectedIndex != 0)
                    settings += " --transfer-characteristics=" + mainWindow.ComboBoxAomencColorTransfer.Text;           // Color Transfer
                if (mainWindow.ComboBoxAomencColorMatrix.SelectedIndex != 0)
                    settings += " --matrix-coefficients=" + mainWindow.ComboBoxAomencColorMatrix.Text;                  // Color Matrix

                if (mainWindow.CheckBoxAomencCDEF.IsChecked == false)
                    settings += " --enable-cdef=0";                                                          // Constrained Directional Enhancement Filter

                if (mainWindow.CheckBoxAomencARNRMax.IsChecked == true)
                {
                    settings += " --arnr-maxframes=" + mainWindow.ComboBoxAomencARNRMax.Text;                           // ARNR Maxframes
                    settings += " --arnr-strength=" + mainWindow.ComboBoxAomencARNRStrength.Text;                       // ARNR Strength
                }

                if (mainWindow.CheckBoxRealTimeMode.IsOn)
                    settings += " --rt";                                                                     // Real Time Mode
            }

            return settings;
        }
    }
}
