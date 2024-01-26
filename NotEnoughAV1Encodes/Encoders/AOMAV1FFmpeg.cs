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
            string quality = mainWindow.ComboBoxQualityMode.SelectedIndex switch
            {
                0 => " -crf " + mainWindow.SliderQualityAOMFFMPEG.Value + " -b:v 0",
                1 => " -crf " + mainWindow.SliderQualityAOMFFMPEG.Value + " -b:v " + mainWindow.TextBoxMaxBitrateAOMFFMPEG.Text + "k",
                2 => " -b:v " + mainWindow.TextBoxMinBitrateAOMFFMPEG.Text + "k",
                3 => " -minrate " + mainWindow.TextBoxMinBitrateAOMFFMPEG.Text + "k -b:v " + mainWindow.TextBoxAVGBitrateAOMFFMPEG.Text + "k -maxrate " + mainWindow.TextBoxMaxBitrateAOMFFMPEG.Text + "k",
                4 => " -crf {q_vmaf} -b:v 0",
                _ => ""
            };

            // Preset
            settings += quality + " -cpu-used " + mainWindow.SliderEncoderPreset.Value;

            // Advanced Settings
            if (mainWindow.ToggleSwitchAdvancedSettings.IsOn == false)
            {
                settings += " -threads 4 -tile-columns 2 -tile-rows 1 -g " + mainWindow.GenerateKeyFrameInerval();
            }
            else
            {
                settings += " -threads " + mainWindow.ComboBoxAomencThreads.Text +                                      // Threads
                            " -tile-columns " + mainWindow.ComboBoxAomencTileColumns.Text +                             // Tile Columns
                            " -tile-rows " + mainWindow.ComboBoxAomencTileRows.Text +                                   // Tile Rows
                            " -lag-in-frames " + mainWindow.TextBoxAomencLagInFrames.Text +                             // Lag in Frames
                            " -aq-mode " + mainWindow.ComboBoxAomencAQMode.SelectedIndex +                              // AQ-Mode
                            " -tune " + mainWindow.ComboBoxAomencTune.Text;                                             // Tune

                if (mainWindow.TextBoxAomencMaxGOP.Text != "0")
                    settings += " -g " + mainWindow.TextBoxAomencMaxGOP.Text;                                           // Keyframe Interval
                if (mainWindow.CheckBoxAomencRowMT.IsChecked == false)
                    settings += " -row-mt 0";                                                                           // Row Based Multithreading
                if (mainWindow.CheckBoxAomencCDEF.IsChecked == false)
                    settings += " -enable-cdef 0";                                                                      // Constrained Directional Enhancement Filter
                if (mainWindow.CheckBoxRealTimeMode.IsOn)
                    settings += " -usage realtime ";                                                                    // Real Time Mode

                if (mainWindow.CheckBoxAomencARNRMax.IsChecked == true)
                {
                    settings += " -arnr-max-frames " + mainWindow.ComboBoxAomencARNRMax.Text;                           // ARNR Maxframes
                    settings += " -arnr-strength " + mainWindow.ComboBoxAomencARNRStrength.Text;                        // ARNR Strength
                }

                settings += " -aom-params " +
                            "tune-content=" + mainWindow.ComboBoxAomencTuneContent.Text +                               // Tune-Content
                            ":sharpness=" + mainWindow.ComboBoxAomencSharpness.Text +                                   // Sharpness (Filter)
                            ":enable-keyframe-filtering=" + mainWindow.ComboBoxAomencKeyFiltering.SelectedIndex;        // Key Frame Filtering

                if (mainWindow.ComboBoxAomencColorPrimaries.SelectedIndex != 0)
                    settings += ":color-primaries=" + mainWindow.ComboBoxAomencColorPrimaries.Text;                     // Color Primaries
                if (mainWindow.ComboBoxAomencColorTransfer.SelectedIndex != 0)
                    settings += ":transfer-characteristics=" + mainWindow.ComboBoxAomencColorTransfer.Text;             // Color Transfer
                if (mainWindow.ComboBoxAomencColorMatrix.SelectedIndex != 0)
                    settings += ":matrix-coefficients=" + mainWindow.ComboBoxAomencColorMatrix.Text;                    // Color Matrix
            }

            return settings;
        }
    }
}
