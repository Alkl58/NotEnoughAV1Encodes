namespace NotEnoughAV1Encodes.Video.Encoders
{
    internal class SvtAV1FFmpeg
    {
        public static string GenerateCommand(VideoSettings videoSettings, string keyFrameInterval)
        {
            string settings = "-c:v libsvtav1";

            // Quality / Bitrate Selection
            string quality = videoSettings.SVTAV1FFMPEGQualityMode switch
            {
                0 => " -rc 0 -qp " + videoSettings.SVTAV1FFMPEGQuantizer,
                1 => " -rc 1 -b:v " + videoSettings.SVTAV1FFMPEGBitrate + "k",
                _ => ""
            };

            // Preset
            settings += quality + " -preset " + videoSettings.SpeedPreset;

            // Advanced Settings
            if (! videoSettings.AdvancedSettings)
            {
                settings += " -g " + keyFrameInterval;
                return settings;
            }

            settings += " -tile_columns " + videoSettings.SvtAv1TileColumns +                            // Tile Columns
                        " -tile_rows " + videoSettings.SvtAv1TileRows +                                  // Tile Rows
                        " -g " + videoSettings.SvtAv1KeyInt +                                            // Keyframe Interval
                        " -la_depth " + videoSettings.SvtAv1Lookahead +                                  // Lookahead
                        " -svtav1-params " +
                        "aq-mode=" + videoSettings.SvtAv1AqMode +                                        // AQ Mode
                        ":film-grain=" + videoSettings.SvtAv1FilmGrain +                                 // Film Grain
                        ":film-grain-denoise=" + videoSettings.SvtAv1FilmGrainDenoise;                   // Film Grain Denoise

            return settings;
        }
    }
}
