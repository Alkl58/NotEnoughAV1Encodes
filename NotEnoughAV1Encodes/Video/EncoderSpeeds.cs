namespace NotEnoughAV1Encodes.Video
{
    internal class EncoderSpeeds
    {
        public static string GenerateMPEGEncoderSpeed(VideoSettings videoSettings)
        {
            return videoSettings.SpeedPreset switch
            {
                0 => "placebo",
                1 => "veryslow",
                2 => "slower",
                3 => "slow",
                4 => "medium",
                5 => "fast",
                6 => "faster",
                7 => "veryfast",
                8 => "superfast",
                9 => "ultrafast",
                _ => "medium",
            };
        }

        public static string GenerateQuickSyncEncoderSpeed(VideoSettings videoSettings)
        {
            return videoSettings.SpeedPreset switch
            {
                0 => "best",
                1 => "higher",
                2 => "high",
                3 => "balanced",
                4 => "fast",
                5 => "faster",
                6 => "fastest",
                _ => "balanced",
            };
        }

        public static string GenerateNVENCEncoderSpeed(VideoSettings videoSettings)
        {
            return videoSettings.SpeedPreset switch
            {
                0 => "quality",
                1 => "default",
                2 => "performance",
                _ => "default"
            };
        }
    }
}
