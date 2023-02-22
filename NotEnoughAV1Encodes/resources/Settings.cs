using System.Globalization;
using System.IO;

namespace NotEnoughAV1Encodes
{
    public class Settings
    {
        /// <summary>Sets if Temp Files should be deleted after a successfull encode.</summary>
        public bool DeleteTempFiles { get; set; } = true;
        /// <summary>Sets if System shutsdown after the queue finished.</summary>
        public bool ShutdownAfterEncode { get; set; }
        /// <summary>Program Theme e.g. "Dark.Blue"</summary>
        public string Theme { get; set; } = "Light.Blue";
        /// <summary>Program Base Theme (used by Settings Window)</summary>
        public int BaseTheme { get; set; }
        /// <summary>Program Accent Theme (used by Settings Window)</summary>
        public int AccentTheme { get; set; }
        /// <summary>Sets Background Image of Program</summary>
        public string BGImage { get; set; }
        /// <summary>Overrides Worker Count -> User can specify it manually</summary>
        public bool OverrideWorkerCount { get; set; }
        /// <summary>Specifies the Temp Folder used</summary>
        public string TempPath { get; set; } = Path.GetTempPath();
        /// <summary>Specifies the default Output Folder</summary>
        public string DefaultOutPath { get; set; } = "";
        /// <summary>Specifies the default Output Container</summary>
        public string DefaultOutContainer { get; set; } = ".mkv";
        /// <summary>Toggles Logging functionality</summary>
        public bool Logging { get; set; } = true;
        /// <summary>Toggles wether or not to clear the queue automatically</summary>
        public bool AutoClearQueue { get; set; } = true;
        /// <summary>Toggles Auto Resume Pause functionality</summary>
        public bool AutoResumePause { get; set; } = false;
        /// <summary>Toggles Process Priority (false => low)</summary>
        public bool PriorityNormal { get; set; } = true;
        /// <summary>CultureInfo for Language (Default: en-US)</summary>
        public CultureInfo CultureInfo { get; set; } = new("en");
        /// <summary>Default Preset to load on startup</summary>
        public string DefaultPreset { get; set; }
        /// <summary>Default Worker Count</summary>
        public int WorkerCount { get; set; } = 99999999;
        /// <summary>Default Chunking Method</summary>
        public int ChunkingMethod { get; set; }
        /// <summary>Default Reencode Method</summary>
        public int ReencodeMethod { get; set; }
        /// <summary>Default Chunk Length</summary>
        public string ChunkLength { get; set; } = "10";
        /// <summary>Default PySceneDetect Threshold</summary>
        public string PySceneDetectThreshold { get; set; } = "30";
        /// <summary>Toggles Queue Parallel Encoding</summary>
        public bool QueueParallel { get; set; }
        /// <summary>Toggles Input Seeking</summary>
        public bool UseInputSeeking { get; set; }
        /// <summary>Sort Queue By...</summary>
        public int SortQueueBy { get; set; }
        /// <summary>Include Subfolders when opening a Batch Folder</summary>
        public bool SubfolderBatch { get; set; }
    }
}
