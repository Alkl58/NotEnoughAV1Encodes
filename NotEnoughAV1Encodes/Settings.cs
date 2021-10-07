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
        public bool AdvancedSettings { get; set; }
        public string CustomSettings { get; set; }

        // Advanced Aomenc Settings
        public bool AomencRTMode { get; set; }
        public int AomencThreads { get; set; } = 3;
        public int AomencTileColumns { get; set; }
        public int AomencTileRows { get; set; }
        public string AomencGOPSize { get; set; } = "0";
        public string AomencLagInFrames { get; set; } = "25";
        public int AomencSharpness { get; set; }
        public int AomencColorPrimaries { get; set; }
        public int AomencColorTransfer { get; set; }
        public int AomencColorMatrix { get; set; }
        public int AomencAQMode { get; set; }
        public int AomencTuneContent { get; set; }
        public int AomencTune { get; set; }
        public bool AomencARNRMaxFrames { get; set; }
        public int AomencARNRMaxFramesIndex { get; set; }
        public int AomencARNRStrength { get; set; }
        public int AomencKeyFrameFiltering { get; set; } = 1;
        public bool AomencRowBasedMultiThreading { get; set; } = true;
        public bool AomencCDEF { get; set; }

    }
}
