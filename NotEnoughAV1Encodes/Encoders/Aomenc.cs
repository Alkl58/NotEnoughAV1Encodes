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
            string quality = mainWindow.VideoTabVideoQualityControl.ComboBoxQualityModeAOMENC.SelectedIndex switch
            {
                0 => " --cq-level=" + mainWindow.VideoTabVideoQualityControl.SliderQualityAOMENC.Value + " --end-usage=q",
                1 => " --cq-level=" + mainWindow.VideoTabVideoQualityControl.SliderQualityAOMENC.Value + " --target-bitrate=" + mainWindow.VideoTabVideoQualityControl.TextBoxBitrateAOMENC.Text + " --end-usage=cq",
                2 => " --target-bitrate=" + mainWindow.VideoTabVideoQualityControl.TextBoxBitrateAOMENC.Text + " --end-usage=vbr",
                3 => " --target-bitrate=" + mainWindow.VideoTabVideoQualityControl.TextBoxBitrateAOMENC.Text + " --end-usage=cbr",
                _ => ""
            };

            // Preset
            settings += quality + " --cpu-used=" + mainWindow.VideoTabVideoOptimizationControl.SliderEncoderPreset.Value;

            // Advanced Settings
            if (mainWindow.VideoTabVideoOptimizationControl.ToggleSwitchAdvancedSettings.IsOn == false)
            {
                settings += " --threads=4 --tile-columns=2 --tile-rows=1 --kf-max-dist=" + mainWindow.VideoTabVideoPartialControl.GenerateKeyFrameInerval();
            }
            else
            {
                settings += " --threads=" + mainWindow.AdvancedTabControl.ComboBoxAomencThreads.Text +                                     // Threads
                            " --tile-columns=" + mainWindow.AdvancedTabControl.ComboBoxAomencTileColumns.Text +                            // Tile Columns
                            " --tile-rows=" + mainWindow.AdvancedTabControl.ComboBoxAomencTileRows.Text +                                  // Tile Rows
                            " --lag-in-frames=" + mainWindow.AdvancedTabControl.TextBoxAomencLagInFrames.Text +                            // Lag in Frames
                            " --sharpness=" + mainWindow.AdvancedTabControl.ComboBoxAomencSharpness.Text +                                 // Sharpness (Filter)
                            " --aq-mode=" + mainWindow.AdvancedTabControl.ComboBoxAomencAQMode.SelectedIndex +                             // AQ-Mode
                            " --enable-keyframe-filtering=" + mainWindow.AdvancedTabControl.ComboBoxAomencKeyFiltering.SelectedIndex +     // Key Frame Filtering
                            " --tune=" + mainWindow.AdvancedTabControl.ComboBoxAomencTune.Text +                                           // Tune
                            " --tune-content=" + mainWindow.AdvancedTabControl.ComboBoxAomencTuneContent.Text;                             // Tune-Content

                if (mainWindow.AdvancedTabControl.TextBoxAomencMaxGOP.Text != "0")
                    settings += " --kf-max-dist=" + mainWindow.AdvancedTabControl.TextBoxAomencMaxGOP.Text;                                // Keyframe Interval
                if (mainWindow.AdvancedTabControl.CheckBoxAomencRowMT.IsChecked == false)
                    settings += " --row-mt=0";                                                                                             // Row Based Multithreading

                if (mainWindow.AdvancedTabControl.ComboBoxAomencColorPrimaries.SelectedIndex != 0)
                    settings += " --color-primaries=" + mainWindow.AdvancedTabControl.ComboBoxAomencColorPrimaries.Text;                   // Color Primaries
                if (mainWindow.AdvancedTabControl.ComboBoxAomencColorTransfer.SelectedIndex != 0)
                    settings += " --transfer-characteristics=" + mainWindow.AdvancedTabControl.ComboBoxAomencColorTransfer.Text;           // Color Transfer
                if (mainWindow.AdvancedTabControl.ComboBoxAomencColorMatrix.SelectedIndex != 0)
                    settings += " --matrix-coefficients=" + mainWindow.AdvancedTabControl.ComboBoxAomencColorMatrix.Text;                  // Color Matrix

                if (mainWindow.AdvancedTabControl.CheckBoxAomencCDEF.IsChecked == false)
                    settings += " --enable-cdef=0";                                                                                        // Constrained Directional Enhancement Filter

                if (mainWindow.AdvancedTabControl.CheckBoxAomencARNRMax.IsChecked == true)
                {
                    settings += " --arnr-maxframes=" + mainWindow.AdvancedTabControl.ComboBoxAomencARNRMax.Text;                           // ARNR Maxframes
                    settings += " --arnr-strength=" + mainWindow.AdvancedTabControl.ComboBoxAomencARNRStrength.Text;                       // ARNR Strength
                }

                if (mainWindow.VideoTabVideoOptimizationControl.CheckBoxRealTimeMode.IsOn)
                    settings += " --rt";                                                                                                   // Real Time Mode
            }

            return settings;
        }
    }
}
