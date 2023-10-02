namespace NotEnoughAV1Encodes.Video.Encoders
{
    internal class AomFFmpeg
    {
        public static string GenerateCommand(VideoSettings videoSettings, string keyFrameInterval)
        {
            string settings = "-c:v libaom-av1";

            // Quality / Bitrate Selection
            string quality = videoSettings.AOMFFMPEGQualityMode switch
            {
                0 => " -crf " + videoSettings.AOMFFMPEGQuantizer + " -b:v 0",
                1 => " -crf " + videoSettings.AOMFFMPEGQuantizer + " -b:v " + videoSettings.AOMFFMPEGMaxBitrate + "k",
                2 => " -b:v " + videoSettings.AOMFFMPEGMinBitrate + "k",
                3 => " -minrate " + videoSettings.AOMFFMPEGMinBitrate + "k -b:v " + videoSettings.AOMFFMPEGAvgBitrate + "k -maxrate " + videoSettings.AOMFFMPEGMaxBitrate + "k",
                4 => " -crf {q_vmaf} -b:v 0",
                _ => ""
            };

            // Preset
            settings += quality + " -cpu-used " + videoSettings.SpeedPreset;

            if (! videoSettings.AdvancedSettings)
            {
                settings += " -threads 4 -tile-columns 2 -tile-rows 1 -g " + keyFrameInterval;
                return settings;
            }

            // Advanced Settings
            settings += " -threads " + (videoSettings.AomencThreads + 1).ToString() +            // Threads
                " -tile-columns " + videoSettings.AomencTileColumns +                            // Tile Columns
                " -tile-rows " + videoSettings.AomencTileRows +                                  // Tile Rows
                " -lag-in-frames " + videoSettings.AomencLagInFrames +                           // Lag in Frames
                " -aq-mode " + videoSettings.AomencAQMode +                                      // AQ-Mode
                " -tune " + (videoSettings.AomencTune == 0 ? "psnr" : "ssim");                   // Tune

            if (videoSettings.AomencGOPSize != "0")
                settings += " -g " + videoSettings.AomencGOPSize;                                               // Keyframe Interval
            if (videoSettings.AomencRowBasedMultiThreading == false)
                settings += " -row-mt 0";                                                                       // Row Based Multithreading
            if (videoSettings.AomencCDEF == false)
                settings += " -enable-cdef 0";                                                                  // Constrained Directional Enhancement Filter
            if (videoSettings.AomencRTMode)
                settings += " -usage realtime ";                                                                // Real Time Mode

            if (videoSettings.AomencARNRMaxFrames == true)
            {
                settings += " -arnr-max-frames " + (15 - videoSettings.AomencARNRMaxFramesIndex).ToString();    // ARNR Maxframes
                settings += " -arnr-strength " + (6 - videoSettings.AomencARNRStrength).ToString();             // ARNR Strength
            }

            settings += " -aom-params " +
                        "tune-content=" + (videoSettings.AomencTuneContent == 0 ? "default" : "screen") +       // Tune-Content
                        ":sharpness=" + videoSettings.AomencSharpness +                                         // Sharpness (Filter)
                        ":enable-keyframe-filtering=" + videoSettings.AomencKeyFrameFiltering;                  // Key Frame Filtering

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

                settings += ":color-primaries=" + primaries;                                                    // Color Primaries
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

                settings += ":transfer-characteristics=" + transfer;                                            // Color Transfer
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

                settings += ":matrix-coefficients=" + matrix;                                                   // Color Matrix
            }

            return settings;
        }
    }
}
