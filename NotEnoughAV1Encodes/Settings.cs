namespace NotEnoughAV1Encodes
{
    public class Settings
    {
        public int Encoder { get; set; }
        public int BitDepth { get; set; }
        public int ColorFormat { get; set; }
        public int FrameRate { get; set; }
        public int SpeedPreset { get; set; } = 5;
        public bool TwoPassEncoding { get; set; }
        public int QualityMode { get; set; }
        public int Quantizer { get; set; } = 25;
        public string MinBitrate { get; set; } = "500";
        public string MaxBitrate { get; set; } = "1500";
        public string AvgBitrate { get; set; } = "1000";
    }
}
