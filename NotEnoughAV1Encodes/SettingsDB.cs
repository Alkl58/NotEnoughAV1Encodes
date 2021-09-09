using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotEnoughAV1Encodes
{
    public class SettingsDB
    {
        /// <summary>Sets if Temp Files should be deleted after a successfull encode.</summary>
        public bool DeleteTempFiles { get; set; }
        /// <summary>Sets if System shutsdown after the queue finished.</summary>
        public bool ShutdownAfterEncode { get; set; }
        /// <summary>Program Theme e.g. "Dark.Blue"</summary>
        public string Theme { get; set; }
        public int BaseTheme { get; set; }
        public int AccentTheme { get; set; }
    }
}
