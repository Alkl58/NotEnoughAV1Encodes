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

        // Advanved Rav1e Settings
        public int Rav1eThreads { get; set; } = 5;
        public int Rav1eTileColumns { get; set; }
        public int Rav1eTileRows { get; set; }
        public string Rav1eMaxGOP { get; set; } = "0";
        public string Rav1eLookahead { get; set; } = "40";
        public int Rav1eColorPrimaries { get; set; }
        public int Rav1eColorTransfer { get; set; }
        public int Rav1eColorMatrix { get; set; }
        public int Rav1eTune { get; set; }
        public bool Rav1eMasteringDisplay { get; set; }
        public string Rav1eMasteringGx { get; set; } = "0";
        public string Rav1eMasteringGy { get; set; } = "0";
        public string Rav1eMasteringBx { get; set; } = "0";
        public string Rav1eMasteringBy { get; set; } = "0";
        public string Rav1eMasteringRx { get; set; } = "0";
        public string Rav1eMasteringRy { get; set; } = "0";
        public string Rav1eMasteringWPx { get; set; } = "0";
        public string Rav1eMasteringWPy { get; set; } = "0";
        public string Rav1eMasteringLx { get; set; } = "0";
        public string Rav1eMasteringLy { get; set; } = "0";
        public bool Rav1eContentLight { get; set; }
        public string Rav1eContentLightCll { get; set; } = "0";
        public string Rav1eContentLightFall { get; set; } = "0";
    }
}
