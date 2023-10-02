namespace NotEnoughAV1Encodes.Video.Encoders
{
    internal class X265
    {
        public static string GenerateCommand(VideoSettings videoSettings)
        {
            string settings = "-c:v libx265";

            // Quality / Bitrate Selection
            string quality = videoSettings.X26xQualityMode switch
            {
                0 => " -crf " + videoSettings.X26xQuantizer,
                1 => " -b:v " + videoSettings.X26xBitrate + "k",
                _ => ""
            };

            // Preset
            settings += quality + " -preset " + EncoderSpeeds.GenerateMPEGEncoderSpeed(videoSettings);

            return settings;
        }
    }
}
