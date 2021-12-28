using System.Globalization;
using System.IO;

namespace NotEnoughAV1Encodes
{
    public class SettingsDB
    {
        /// <summary>Sets if Temp Files should be deleted after a successfull encode.</summary>
        public bool DeleteTempFiles { get; set; } = true;
        /// <summary>Sets if System shutsdown after the queue finished.</summary>
        public bool ShutdownAfterEncode { get; set; }
        /// <summary>Program Theme e.g. "Dark.Blue"</summary>
        public string Theme { get; set; } = "Light.Blue";
        public int BaseTheme { get; set; }
        public int AccentTheme { get; set; }
        /// <summary>Sets Background Image of Program</summary>
        public string BGImage { get; set; }
        /// <summary>Overrides Worker Count -> User can specify it manually</summary>
        public bool OverrideWorkerCount { get; set; }
        /// <summary>Specifies the Temp Folder used</summary>
        public string TempPath { get; set; } = Path.GetTempPath();
        /// <summary>Toggles Logging functionality</summary>
        public bool Logging { get; set; } = true;
        /// <summary>Toggles Process Priority (false => low)</summary>
        public bool PriorityNormal { get; set; } = true;
        /// <summary>CultureInfo for Language (Default: en-US)</summary>
        public CultureInfo CultureInfo { get; set; } = new("en");
        /// <summary>Default Preset to load on startup</summary>
        public string DefaultPreset { get; set; }
    }
}
