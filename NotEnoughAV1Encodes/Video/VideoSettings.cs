namespace NotEnoughAV1Encodes
{
    public class VideoSettings
    {
        public int Encoder { get; set; }
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

        // Video Bit-Depth
        /// <summary>Bit-Depth Setting for Encoders</summary>
        public int BitDepth { get; set; }
        /// <summary>Bit-Depth Setting for Encoders who go only up to 10bit</summary>
        public int BitDepthLimited { get; set; }

        // Video Quality Settings AOM FFMPEG
        public int AOMFFMPEGQualityMode { get; set; }
        public int AOMFFMPEGQuantizer { get; set; } = 25;
        public string AOMFFMPEGMinBitrate { get; set; } = "500";
        public string AOMFFMPEGMaxBitrate { get; set; } = "1500";
        public string AOMFFMPEGAvgBitrate { get; set; } = "1000";

        // Video Quality Settings RAV1E FFMPEG
        public int RAV1EFFMPEGQualityMode { get; set; }
        public int RAV1EFFMPEGQuantizer { get; set; } = 80;
        public string RAV1EFFMPEGBitrate { get; set; } = "1500";

        // Video Quality Settings SVT-AV1 FFMPEG
        public int SVTAV1FFMPEGQualityMode { get; set; }
        public int SVTAV1FFMPEGQuantizer { get; set; } = 40;
        public string SVTAV1FFMPEGBitrate { get; set; } = "1500";

        // Video Quality Settings VP9 FFMPEG
        public int VP9FFMPEGQualityMode { get; set; }
        public int VP9FFMPEGQuantizer { get; set; } = 25;
        public string VP9FFMPEGMinBitrate { get; set; } = "1000";
        public string VP9FFMPEGMaxBitrate { get; set; } = "2000";
        public string VP9FFMPEGAvgBitrate { get; set; } = "1500";

        // Video Quality Settings Aomenc
        public int AOMENCQualityMode { get; set; }
        public int AOMENCQuantizer { get; set; } = 25;
        public string AOMENCBitrate { get; set; } = "1500";

        // Video Quality Settings Rav1e
        public int RAV1EQualityMode { get; set; }
        public int RAV1EQuantizer { get; set; } = 80;
        public string RAV1EBitrate { get; set; } = "1500";

        // Video Quality Settings SVT-AV1
        public int SVTAV1QualityMode { get; set; }
        public int SVTAV1Quantizer { get; set; } = 40;
        public string SVTAV1Bitrate { get; set; } = "1500";

        // Video Quality Settings x265 x264
        public int X26xQualityMode { get; set; }
        public int X26xQuantizer { get; set; } = 18;
        public string X26xBitrate { get; set; } = "3500";

        // Video Quality Settings QSV AV1
        public int QSVAV1QualityMode { get; set; }
        public int QSVAV1Quantizer { get; set; } = 24;
        public string QSVAV1Bitrate { get; set; } = "2000";

        // Video Quality Settings NVENC AV1
        public int NVENCAV1QualityMode { get; set; }
        public int NVENCAV1Quantizer { get; set; } = 24;
        public string NVENCAV1Bitrate { get; set; } = "2000";

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

        // HDR
        public string MasteringGx { get; set; }
        public string MasteringGy { get; set; }
        public string MasteringBx { get; set; }
        public string MasteringBy { get; set; }
        public string MasteringRx { get; set; }
        public string MasteringRy { get; set; }
        public string MasteringWPx { get; set; }
        public string MasteringWPy { get; set; }
        public bool HDR { get; set; }
        public bool MasteringDisplay { get; set; }
        public bool WhiteMasteringDisplay { get; set; }
        public bool MaxContentLight { get; set; }
        public bool Luminance { get; set; }
        public bool MaxFrameLight { get; set; }

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

        // Advanced SVT-AV1 Settings

        /// <summary>Number of tile columns to use, TileCol == log2(x), default changes per resolution</summary>
        public int SvtAv1TileColumns { get; set; }
        /// <summary>Number of tile rows to use, TileRow == log2(x), default changes per resolution</summary>
        public int SvtAv1TileRows { get; set; }
        /// <summary>GOP size (frames), use s suffix for seconds (SvtAv1EncApp only) [-2: ~5 seconds, -1: "infinite" only for CRF, 0: == -1]</summary>
        public string SvtAv1KeyInt { get; set; } = "-1";
        /// <summary>Number of frames in the future to look ahead, beyond minigop, temporal filtering, and rate control [-1: auto]</summary>
        public string SvtAv1Lookahead { get; set; } = "33";
        /// <summary>Set adaptive QP level [0: off, 1: variance base using AV1 segments, 2: deltaq pred efficiency]</summary>
        public int SvtAv1AqMode { get; set; } = 2;
        /// <summary>Enable film grain [0: off, 1-50: level of denoising for film grain]</summary>
        public int SvtAv1FilmGrain { get; set; } = 0;
        /// <summary>Apply denoising when film grain is ON, default is 1 [0: no denoising, film grain data sent in frame header, 1: level of denoising is set by the film-grain parameter]</summary>
        public int SvtAv1FilmGrainDenoise { get; set; } = 1;

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

        // Target VMAF

        /// <summary>Target VMAF Active.</summary>
        public bool TargetVMAF { get; set; }
        /// <summary>Target VMAF Use user Encoder Settings (false uses hardcoded settings)</summary>
        public bool TargetVMAFUserEncoderSettings { get; set; }
        /// <summary>Target VMAF Score.</summary>
        public double TargetVMAFScore { get; set; } = 97.0;
        /// <summary>Target VMAF Tries.</summary>
        public int TargetVMAFProbes { get; set; } = 4;
        /// <summary>Target VMAF Minimum Q (Highest Quality).</summary>
        public int TargetVMAFMinQ { get; set; } = 20;
        /// <summary>Target VMAF Maximum Q (Lowest Quality).</summary>
        public int TargetVMAFMaxQ { get; set; } = 40;
    }
}
