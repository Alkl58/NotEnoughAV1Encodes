using System.Windows;

namespace NotEnoughAV1Encodes.Encoders
{
    class HEVCFFmpeg : IEncoder
    {
        public string GetCommand()
        {
            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            string settings = "-c:v libx265";

            // Quality / Bitrate Selection
            string quality = mainWindow.ComboBoxQualityModeX26x.SelectedIndex switch
            {
                0 => " -crf " + mainWindow.SliderQualityX26x.Value,
                1 => " -b:v " + mainWindow.TextBoxBitrateX26x.Text + "k",
                _ => ""
            };

            // Preset
            settings += quality + " -preset " + mainWindow.GenerateMPEGEncoderSpeed();

            return settings;
        }
    }
}
