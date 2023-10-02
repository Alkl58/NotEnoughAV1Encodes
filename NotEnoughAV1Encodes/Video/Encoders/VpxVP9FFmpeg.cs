namespace NotEnoughAV1Encodes.Video.Encoders
{
    internal class VpxVP9FFmpeg
    {
        public static string GenerateCommand(VideoSettings videoSettings, string keyFrameInterval)
        {
            string settings = "-c:v libvpx-vp9";

            // Quality / Bitrate Selection
            string quality = videoSettings.VP9FFMPEGQualityMode switch
            {
                0 => " -crf " + videoSettings.VP9FFMPEGQuantizer + " -b:v 0",
                1 => " -crf " + videoSettings.VP9FFMPEGQuantizer + " -b:v " + videoSettings.VP9FFMPEGMaxBitrate + "k",
                2 => " -b:v " + videoSettings.VP9FFMPEGAvgBitrate + "k",
                3 => " -minrate " + videoSettings.VP9FFMPEGMinBitrate + "k -b:v " + videoSettings.VP9FFMPEGAvgBitrate + "k -maxrate " + videoSettings.VP9FFMPEGMaxBitrate + "k",
                _ => ""
            };

            // Preset
            settings += quality + " -cpu-used " + videoSettings.SpeedPreset;

            // Advanced Settings
            if (! videoSettings.AdvancedSettings)
            {
                settings += " -threads 4 -tile-columns 2 -tile-rows 1 -g " + keyFrameInterval;
                return settings;
            }

            settings += " -threads " + (videoSettings.Vp9Threads + 1).ToString() +                       // Max Threads
                        " -tile-columns " + videoSettings.Vp9TileColumns +                               // Tile Columns
                        " -tile-rows " + videoSettings.Vp9TileRows +                                     // Tile Rows
                        " -lag-in-frames " + videoSettings.Vp9LagInFrames +                              // Lag in Frames
                        " -g " + videoSettings.Vp9MaxKf +                                                // Max GOP
                        " -aq-mode " + videoSettings.Vp9AQMode +                                         // AQ-Mode
                        " -tune " + videoSettings.Vp9Tune +                                              // Tune
                        " -tune-content " + videoSettings.Vp9TuneContent;                                // Tune-Content

            if (videoSettings.Vp9ARNR)
            {
                settings += " -arnr-maxframes " + (15 - videoSettings.Vp9ARNRIndex).ToString() +         // ARNR Max Frames
                            " -arnr-strength " + (6 - videoSettings.Vp9ARNRStrength).ToString() +        // ARNR Strength
                            " -arnr-type " + (1 + videoSettings.Vp9ARNRType);                            // ARNR Type
            }

            return settings;
        }
    }
}
