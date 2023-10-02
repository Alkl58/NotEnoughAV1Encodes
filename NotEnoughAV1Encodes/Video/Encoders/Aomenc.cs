using System.IO;

namespace NotEnoughAV1Encodes.Video.Encoders
{
    internal class Aomenc
    {
        public static string GenerateCommand(VideoSettings videoSettings, string keyFrameInterval)
        {
            string settings = "-f yuv4mpegpipe - | " +
                              "\"" + Path.Combine(Directory.GetCurrentDirectory(), "Apps", "aomenc", "aomenc.exe") + "\" -";

            // Quality / Bitrate Selection
            string quality = videoSettings.AOMENCQualityMode switch
            {
                0 => " --cq-level=" + videoSettings.AOMENCQuantizer + " --end-usage=q",
                1 => " --cq-level=" + videoSettings.AOMENCQuantizer + " --target-bitrate=" + videoSettings.AOMENCBitrate + " --end-usage=cq",
                2 => " --target-bitrate=" + videoSettings.AOMENCBitrate + " --end-usage=vbr",
                3 => " --target-bitrate=" + videoSettings.AOMENCBitrate + " --end-usage=cbr",
                _ => ""
            };

            // Preset
            settings += quality + " --cpu-used=" + videoSettings.SpeedPreset;

            // Advanced Settings
            if (! videoSettings.AdvancedSettings)
            {
                settings += " --threads=4 --tile-columns=2 --tile-rows=1 --kf-max-dist=" + keyFrameInterval;
                return settings;
            }

            settings += " --threads=" + (1 + videoSettings.AomencThreads).ToString() +                          // Threads
                        " --tile-columns=" + videoSettings.AomencTileColumns +                                  // Tile Columns
                        " --tile-rows=" + videoSettings.AomencTileRows +                                        // Tile Rows
                        " --lag-in-frames=" + videoSettings.AomencLagInFrames +                                 // Lag in Frames
                        " --sharpness=" + videoSettings.AomencSharpness +                                       // Sharpness (Filter)
                        " --aq-mode=" + videoSettings.AomencAQMode +                                            // AQ-Mode
                        " --enable-keyframe-filtering=" + videoSettings.AomencKeyFrameFiltering +               // Key Frame Filtering
                        " --tune=" + (videoSettings.AomencTune == 0 ? "psnr" : "ssim") +                        // Tune
                        " --tune-content=" + (videoSettings.AomencTuneContent == 0 ? "default" : "screen");     // Tune-Content

            if (videoSettings.AomencGOPSize != "0")
                settings += " --kf-max-dist=" + videoSettings.AomencGOPSize;                                    // Keyframe Interval
            if (! videoSettings.AomencRowBasedMultiThreading)
                settings += " --row-mt=0";                                                                      // Row Based Multithreading

            if (videoSettings.AomencColorPrimaries != 0)
            {
                string primaries = videoSettings.AomencColorPrimaries switch
                {
                    1 => "bt470m",
                    2 => "bt470bg",
                    3 => "bt601",
                    4 => "bt709",
                    5 => "bt2020",
                    6 => "smpte240",
                    7 => "smpte431",
                    8 => "smpte432",
                    9 => "ebu3213",
                    10 => "film",
                    11 => "xyz",
                    _ => "",
                };

                settings += " --color-primaries=" + primaries;                                              // Color Primaries
            }

            if (videoSettings.AomencColorTransfer != 0)
            {
                string transfer = videoSettings.AomencColorTransfer switch
                {
                    1 => "bt470m",
                    2 => "bt470bg",
                    3 => "bt601",
                    4 => "bt709",
                    5 => "bt1361",
                    6 => "bt2020-10bit",
                    7 => "bt2020-12bit",
                    8 => "smpte240",
                    9 => "smpte428",
                    10 => "smpte2084",
                    11 => "lin",
                    12 => "log100",
                    13 => "log100sq10",
                    14 => "iec61966",
                    15 => "srgb",
                    16 => "hlg",
                    _ => "",
                };

                settings += " --transfer-characteristics=" + transfer;                                          // Color Transfer
            }

            if (videoSettings.AomencColorMatrix != 0)
            {
                string matrix = videoSettings.AomencColorMatrix switch
                {
                    1 => "bt470bg",
                    2 => "bt601",
                    3 => "bt709",
                    4 => "bt2020ncl",
                    5 => "bt2020cl",
                    6 => "smpte240",
                    7 => "smpte2085",
                    8 => "ycgco",
                    9 => "chromncl",
                    10 => "chromcl",
                    11 => "ictcp",
                    12 => "fcc73",
                    13 => "identity",
                    _ => "",
                };

                settings += " --matrix-coefficients=" + matrix;                                                 // Color Matrix
            }

            if (! videoSettings.AomencCDEF)
                settings += " --enable-cdef=0";                                                                 // Constrained Directional Enhancement Filter

            if (videoSettings.AomencARNRMaxFrames)
            {
                settings += " --arnr-maxframes=" + (15 - videoSettings.AomencARNRMaxFramesIndex).ToString();    // ARNR Maxframes
                settings += " --arnr-strength=" + (6 - videoSettings.AomencARNRStrength).ToString();            // ARNR Strength
            }

            if (videoSettings.AomencRTMode)
                settings += " --rt";                                                                            // Real Time Mode

            return settings;
        }
    }
}
