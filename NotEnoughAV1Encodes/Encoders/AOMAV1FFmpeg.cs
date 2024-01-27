using System.Windows;

namespace NotEnoughAV1Encodes.Encoders
{
    class AOMAV1FFmpeg : IEncoder
    {
        public string GetCommand()
        {
            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            string settings = "-c:v libaom-av1";

            // Quality / Bitrate Selection
            string quality = mainWindow.VideoTabVideoQualityControl.ComboBoxQualityMode.SelectedIndex switch
            {
                0 => " -crf " + mainWindow.VideoTabVideoQualityControl.SliderQualityAOMFFMPEG.Value + " -b:v 0",
                1 => " -crf " + mainWindow.VideoTabVideoQualityControl.SliderQualityAOMFFMPEG.Value + " -b:v " + mainWindow.VideoTabVideoQualityControl.TextBoxMaxBitrateAOMFFMPEG.Text + "k",
                2 => " -b:v " + mainWindow.VideoTabVideoQualityControl.TextBoxMinBitrateAOMFFMPEG.Text + "k",
                3 => " -minrate " + mainWindow.VideoTabVideoQualityControl.TextBoxMinBitrateAOMFFMPEG.Text + "k -b:v " + mainWindow.VideoTabVideoQualityControl.TextBoxAVGBitrateAOMFFMPEG.Text + "k -maxrate " + mainWindow.VideoTabVideoQualityControl.TextBoxMaxBitrateAOMFFMPEG.Text + "k",
                4 => " -crf {q_vmaf} -b:v 0",
                _ => ""
            };

            // Preset
            settings += quality + " -cpu-used " + mainWindow.VideoTabVideoOptimizationControl.SliderEncoderPreset.Value;

            // Advanced Settings
            if (mainWindow.VideoTabVideoOptimizationControl.ToggleSwitchAdvancedSettings.IsOn == false)
            {
                settings += " -threads 4 -tile-columns 2 -tile-rows 1 -g " + mainWindow.VideoTabVideoPartialControl.GenerateKeyFrameInerval();
            }
            else
            {
                settings += " -threads " + mainWindow.AdvancedTabControl.ComboBoxAomencThreads.Text +                                      // Threads
                            " -tile-columns " + mainWindow.AdvancedTabControl.ComboBoxAomencTileColumns.Text +                             // Tile Columns
                            " -tile-rows " + mainWindow.AdvancedTabControl.ComboBoxAomencTileRows.Text +                                   // Tile Rows
                            " -lag-in-frames " + mainWindow.AdvancedTabControl.TextBoxAomencLagInFrames.Text +                             // Lag in Frames
                            " -aq-mode " + mainWindow.AdvancedTabControl.ComboBoxAomencAQMode.SelectedIndex +                              // AQ-Mode
                            " -tune " + mainWindow.AdvancedTabControl.ComboBoxAomencTune.Text;                                             // Tune

                if (mainWindow.AdvancedTabControl.TextBoxAomencMaxGOP.Text != "0")
                    settings += " -g " + mainWindow.AdvancedTabControl.TextBoxAomencMaxGOP.Text;                                           // Keyframe Interval
                if (mainWindow.AdvancedTabControl.CheckBoxAomencRowMT.IsChecked == false)
                    settings += " -row-mt 0";                                                                                              // Row Based Multithreading
                if (mainWindow.AdvancedTabControl.CheckBoxAomencCDEF.IsChecked == false)
                    settings += " -enable-cdef 0";                                                                                         // Constrained Directional Enhancement Filter
                if (mainWindow.VideoTabVideoOptimizationControl.CheckBoxRealTimeMode.IsOn)
                    settings += " -usage realtime ";                                                                                       // Real Time Mode

                if (mainWindow.AdvancedTabControl.CheckBoxAomencARNRMax.IsChecked == true)
                {
                    settings += " -arnr-max-frames " + mainWindow.AdvancedTabControl.ComboBoxAomencARNRMax.Text;                           // ARNR Maxframes
                    settings += " -arnr-strength " + mainWindow.AdvancedTabControl.ComboBoxAomencARNRStrength.Text;                        // ARNR Strength
                }

                settings += " -aom-params " +
                            "tune-content=" + mainWindow.AdvancedTabControl.ComboBoxAomencTuneContent.Text +                               // Tune-Content
                            ":sharpness=" + mainWindow.AdvancedTabControl.ComboBoxAomencSharpness.Text +                                   // Sharpness (Filter)
                            ":enable-keyframe-filtering=" + mainWindow.AdvancedTabControl.ComboBoxAomencKeyFiltering.SelectedIndex;        // Key Frame Filtering

                if (mainWindow.AdvancedTabControl.ComboBoxAomencColorPrimaries.SelectedIndex != 0)
                    settings += ":color-primaries=" + mainWindow.AdvancedTabControl.ComboBoxAomencColorPrimaries.Text;                     // Color Primaries
                if (mainWindow.AdvancedTabControl.ComboBoxAomencColorTransfer.SelectedIndex != 0)
                    settings += ":transfer-characteristics=" + mainWindow.AdvancedTabControl.ComboBoxAomencColorTransfer.Text;             // Color Transfer
                if (mainWindow.AdvancedTabControl.ComboBoxAomencColorMatrix.SelectedIndex != 0)
                    settings += ":matrix-coefficients=" + mainWindow.AdvancedTabControl.ComboBoxAomencColorMatrix.Text;                    // Color Matrix
            }

            return settings;
        }
    }
}
