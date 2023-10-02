using System.IO;

namespace NotEnoughAV1Encodes.Video.Encoders
{
    internal class QSVEncAV1
    {
        public static string GenerateCommand(VideoSettings videoSettings)
        {
            string settings = "-f yuv4mpegpipe - | " +
                    "\"" + Path.Combine(Directory.GetCurrentDirectory(), "Apps", "qsvenc", "QSVEncC64.exe") + "\" --y4m -i -";

            // Codec
            settings += " --codec av1";

            // Quality / Bitrate Selection
            string quality = videoSettings.QSVAV1QualityMode switch
            {
                0 => " --cqp " + videoSettings.QSVAV1Quantizer,
                1 => " --icq " + videoSettings.QSVAV1Quantizer,
                2 => " --vbr " + videoSettings.QSVAV1Bitrate,
                3 => " --cbr " + videoSettings.QSVAV1Bitrate,
                _ => ""
            };

            // Preset
            settings += quality + " --quality " + EncoderSpeeds.GenerateQuickSyncEncoderSpeed(videoSettings);

            // Bit-Depth
            settings += " --output-depth ";
            settings += videoSettings.BitDepthLimited switch
            {
                0 => "8",
                1 => "10",
                _ => "8"
            };

            // Output Colorspace
            settings += " --output-csp ";
            settings += videoSettings.ColorFormat switch
            {
                0 => "i420",
                1 => "i422",
                2 => "i444",
                _ => "i420"
            };

            return settings;
        }
    }
}
