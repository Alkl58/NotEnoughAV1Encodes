using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotEnoughAV1Encodes.Subtitle
{
    internal class CommandGenerator
    {
        public string GenerateSoftsub(System.Windows.Controls.ItemCollection tracks)
        {
            bool noSubs = true;
            string command = "";
            foreach (SubtitleTracks track in tracks)
            {
                command += SoftsubGenerator(track.Index, resources.MediaLanguages.Languages[track.Language], track.CustomName, track.Default);
                noSubs = false;
            }

            // Only return non null, if subs actually exists
            // Needed for Muxing Logic in Video/VideoMuxer.cs
            return noSubs ? null : command;
        }

        public string GenerateHardsub(System.Windows.Controls.ItemCollection tracks)
        {
            // To Do
            return null;
        }

        private string SoftsubGenerator(int index, string language, string name, bool defaultSub)
        {
            string subDefault = defaultSub ? "yes" : "no";
            return " --language " + index + ":" + language + " --track-name " + index + ":\"" + name + "\" --default-track " + index + ":" + subDefault;
        }
    }
}
