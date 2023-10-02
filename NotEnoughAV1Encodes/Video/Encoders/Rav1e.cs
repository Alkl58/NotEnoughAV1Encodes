using System.IO;

namespace NotEnoughAV1Encodes.Video.Encoders
{
    internal class Rav1e
    {
        public static string GenerateCommand(VideoSettings videoSettings, string keyFrameInterval)
        {
            string settings = "-f yuv4mpegpipe - | " +
                               "\"" + Path.Combine(Directory.GetCurrentDirectory(), "Apps", "rav1e", "rav1e.exe") + "\" - -y";

            // Quality / Bitrate Selection
            string quality = videoSettings.RAV1EQualityMode switch
            {
                0 => " --quantizer " + videoSettings.RAV1EQuantizer,
                1 => " --bitrate " + videoSettings.RAV1EBitrate,
                _ => ""
            };

            // Preset
            settings += quality + " --speed " + videoSettings.SpeedPreset;

            // Advanced Settings
            if (! videoSettings.AdvancedSettings)
            {
                settings += " --threads 4 --tile-cols 2 --tile-rows 1 --keyint " + keyFrameInterval;
                return settings;
            }

            settings += " --threads " + videoSettings.Rav1eThreads +                                     // Threads
                        " --tile-cols " + videoSettings.Rav1eTileColumns +                               // Tile Columns
                        " --tile-rows " + videoSettings.Rav1eTileRows +                                  // Tile Rows
                        " --rdo-lookahead-frames " + videoSettings.Rav1eLookahead +                      // RDO Lookahead
                        " --tune " + (videoSettings.Rav1eTune == 0 ? "Psychovisual" : "Psnr");           // Tune

            if (videoSettings.Rav1eMaxGOP != "0")
                settings += " --keyint " + videoSettings.Rav1eMaxGOP;                                    // Keyframe Interval

            if (videoSettings.Rav1eColorPrimaries != 0)
            {
                string primaries = videoSettings.Rav1eColorPrimaries switch
                {
                    1 => "BT470M",
                    2 => "BT470BG",
                    3 => "BT601",
                    4 => "BT709",
                    5 => "BT2020",
                    6 => "SMPTE240",
                    7 => "SMPTE431",
                    8 => "SMPTE432",
                    9 => "EBU3213",
                    10 => "GenericFilm",
                    11 => "XYZ",
                    _ => "",
                };

                settings += " --primaries " + primaries;                                                 // Color Primaries
            }

            if (videoSettings.Rav1eColorTransfer != 0)
            {
                string transfer = videoSettings.Rav1eColorTransfer switch
                {
                    1 => "BT470M",
                    2 => "BT470BG",
                    3 => "BT601",
                    4 => "BT709",
                    5 => "BT1361",
                    6 => "BT2020_10Bit",
                    7 => "BT2020_12Bit",
                    8 => "SMPTE240",
                    9 => "SMPTE428",
                    10 => "SMPTE2084",
                    11 => "Linear",
                    12 => "Log100",
                    13 => "Log100Sqrt10",
                    14 => "IEC61966",
                    15 => "SRGB",
                    16 => "HLG",
                    _ => "",
                };

                settings += " --transfer " + transfer;                                                   // Color Transfer
            }

            if (videoSettings.Rav1eColorMatrix != 0)
            {
                string matrix = videoSettings.Rav1eColorMatrix switch
                {
                    1 => "BT470BG",
                    2 => "BT601",
                    3 => "BT709",
                    4 => "BT2020NCL",
                    5 => "BT2020CL",
                    6 => "SMPTE240",
                    7 => "SMPTE2085",
                    8 => "YCgCo",
                    9 => "ChromatNCL",
                    10 => "ChromatCL",
                    11 => "ICtCp",
                    12 => "Identity",
                    _ => "",
                };

                settings += " --matrix " + matrix;                                                       // Color Matrix
            }

            return settings;
        }
    }
}
