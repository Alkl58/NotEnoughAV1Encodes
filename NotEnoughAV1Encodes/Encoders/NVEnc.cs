using System.IO;
using System.Windows;

namespace NotEnoughAV1Encodes.Encoders
{
    class NVEnc : IEncoder
    {
        public string GetCommand()
        {
            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            string settings = "-f yuv4mpegpipe - | " +
                "\"" + Path.Combine(Directory.GetCurrentDirectory(), "Apps", "nvenc", "NVEncC64.exe") + "\" --y4m -i -";

            // Codec
            settings += " --codec av1";

            // Quality / Bitrate Selection
            string quality = mainWindow.VideoTabVideoQualityControl.ComboBoxQualityModeQSVAV1.SelectedIndex switch
            {
                0 => " --cqp " + mainWindow.VideoTabVideoQualityControl.SliderQualityQSVAV1.Value,
                1 => " --vbr " + mainWindow.VideoTabVideoQualityControl.TextBoxBitrateQSVAV1.Text,
                2 => " --cbr " + mainWindow.VideoTabVideoQualityControl.TextBoxBitrateQSVAV1.Text,
                _ => ""
            };

            // Preset
            settings += quality + " --preset " + mainWindow.GenerateNVENCEncoderSpeed();

            // Bit-Depth
            settings += " --output-depth ";
            settings += mainWindow.VideoTabVideoPartialControl.ComboBoxVideoBitDepthLimited.SelectedIndex switch
            {
                0 => "8",
                1 => "10",
                _ => "8"
            };

            return settings;
        }
    }
}
