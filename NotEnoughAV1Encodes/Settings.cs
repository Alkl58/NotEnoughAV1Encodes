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
        public bool CustomSettingsActive { get; set; }
        public string CustomSettings { get; set; }
        public string PresetBatchName { get; set; } = "{filename}";

        // Audio
        public int AudioCodecMono { get; set; } = 0;
        public int AudioBitrateMono { get; set; } = 64;
        public int AudioCodecStereo { get; set; } = 0;
        public int AudioBitrateStereo { get; set; } = 128;
        public int AudioCodecSixChannel { get; set; } = 0;
        public int AudioBitrateSixChannel { get; set; } = 256;
        public int AudioCodecEightChannel { get; set; } = 0;
        public int AudioBitrateEightChannel { get; set; } = 450;


        // Filters
        public bool FilterCrop { get; set; }
        public bool FilterResize { get; set; }
        public bool FilterRotate { get; set; }
        public bool FilterDeinterlace { get; set; }
        public string FilterCropTop { get; set; } = "0";
        public string FilterCropRight { get; set; } = "0";
        public string FilterCropBottom { get; set; } = "0";
        public string FilterCropLeft { get; set; } = "0";
        public string FilterResizeWidth { get; set; } = "0";
        public string FilterResizeHeight { get; set; } = "1080";
        public int FilterResizeAlgorithm { get; set; } = 2;
        public int FilterRotateIndex { get; set; }
        public int FilterDeinterlaceIndex { get; set; }

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

        // Advanced SVT-AV1 Settings
        public int SvtAv1TileColumns { get; set; }
        public int SvtAv1TileRows { get; set; }
        public string SvtAv1KeyInt { get; set; } = "-1";
        public string SvtAv1Lookahead { get; set; } = "33";

        // Advanced VPX-VP9 Settings
        public int Vp9Threads { get; set; } = 5;
        public int Vp9TileColumns { get; set; }
        public int Vp9TileRows { get; set; }
        public string Vp9LagInFrames { get; set; } = "25";
        public bool Vp9ARNR { get; set; }
        public int Vp9ARNRIndex { get; set; }
        public int Vp9ARNRStrength { get; set; }
        public int Vp9ARNRType { get; set; }
        public int Vp9AQMode { get; set; }
        public int Vp9TuneContent { get; set; }
        public int Vp9Tune { get; set; }
        public string Vp9MaxKf { get; set; } = "240";
    }
}
