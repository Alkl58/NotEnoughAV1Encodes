using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotEnoughAV1Encodes.Video.Encoders
{
    internal class NVEncAV1
    {
        public static string GenerateCommand(VideoSettings videoSettings)
        {
            string settings = "-f yuv4mpegpipe - | " +
                    "\"" + Path.Combine(Directory.GetCurrentDirectory(), "Apps", "nvenc", "NVEncC64.exe") + "\" --y4m -i -";

            // Codec
            settings += " --codec av1";

            // Quality / Bitrate Selection
            string quality = videoSettings.QSVAV1QualityMode switch
            {
                0 => " --cqp " + videoSettings.QSVAV1Quantizer,
                1 => " --vbr " + videoSettings.QSVAV1Bitrate,
                2 => " --cbr " + videoSettings.QSVAV1Bitrate,
                _ => ""
            };

            // Preset
            settings += quality + " --preset " + EncoderSpeeds.GenerateNVENCEncoderSpeed(videoSettings);

            // Bit-Depth
            settings += " --output-depth ";
            settings += videoSettings.BitDepthLimited switch
            {
                0 => "8",
                1 => "10",
                _ => "8"
            };

            return settings;
        }
    }
}
