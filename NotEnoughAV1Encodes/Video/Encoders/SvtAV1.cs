using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotEnoughAV1Encodes.Video.Encoders
{
    internal class SvtAV1
    {
        public static string GenerateCommand(VideoSettings videoSettings, string keyFrameInterval)
        {
            string settings = "-nostdin -f yuv4mpegpipe - | " +
                              "\"" + Path.Combine(Directory.GetCurrentDirectory(), "Apps", "svt-av1", "SvtAv1EncApp.exe") + "\" -i stdin";

            // Quality / Bitrate Selection
            string quality = videoSettings.SVTAV1QualityMode switch
            {
                0 => " --rc 0 --crf " + videoSettings.SVTAV1Quantizer,
                1 => " --rc 1 --tbr " + videoSettings.SVTAV1Bitrate,
                _ => ""
            };

            // Preset
            settings += quality + " --preset " + videoSettings.SpeedPreset;

            // Advanced Settings
            if (! videoSettings.AdvancedSettings)
            {
                settings += " --keyint " + keyFrameInterval;
                return settings;
            }

            settings += " --tile-columns " + videoSettings.SvtAv1TileColumns +                            // Tile Columns
                        " --tile-rows " + videoSettings.SvtAv1TileRows +                                  // Tile Rows
                        " --keyint " + videoSettings.SvtAv1KeyInt +                                       // Keyframe Interval
                        " --lookahead " + videoSettings.SvtAv1Lookahead +                                 // Lookahead
                        " --aq-mode " + videoSettings.SvtAv1AqMode +                                      // AQ Mode
                        " --film-grain " + videoSettings.SvtAv1FilmGrain +                                // Film Grain
                        " --film-grain-denoise " + videoSettings.SvtAv1FilmGrainDenoise;                  // Film Grain Denoise    

            return settings;
        }
    }
}
