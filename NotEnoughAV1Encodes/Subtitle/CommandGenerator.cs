using System.IO;

namespace NotEnoughAV1Encodes.Subtitle
{
    internal class CommandGenerator
    {
        public string GenerateSoftsub(System.Windows.Controls.ItemCollection tracks)
        {
            bool noSubs = true;
            string command = "";
            bool firstMap = true;
            string map = " --subtitle-tracks ";
            foreach (SubtitleTracks track in tracks)
            {
                // Skip Subtitle Track if not active
                if (track.Active == false) continue;
                // Skip Subtitle Track if burned in
                if (track.BurnIn == true) continue;

                // Mapping Subtitles
                map += firstMap ? track.Index : "," + track.Index;
                firstMap = false;
               
                command += SoftsubGenerator(track.Index, resources.MediaLanguages.Languages[track.Language], track.CustomName, track.Default);
                noSubs = false;
            }

            // Only return non null, if subs actually exists
            // Needed for Muxing Logic in Video/VideoMuxer.cs
            return noSubs ? null : map + command;
        }

        public string GenerateHardsub(System.Windows.Controls.ItemCollection tracks, string identifier)
        {
            bool noSubs = true;
            string command = "";

            foreach (SubtitleTracks track in tracks)
            {
                // Skip Subtitle Track if not active
                if (track.Active == false) continue;
                // Skip Subtitle Track if not burned in
                if (track.BurnIn == false) continue;
                // Skip Subtitle Track if not empty
                if(!string.IsNullOrEmpty(command)) continue;

                string subPath = Path.Combine(Global.Temp, "NEAV1E", identifier, "Subtitles", "subs.mkv");
                command = HardsubGenerator(track.PictureBased, track.Index, subPath);
                noSubs = false;
            }

            return noSubs ? null : command;
        }

        private static string SoftsubGenerator(int index, string language, string name, bool defaultSub)
        {
            // mkvmerge commands
            string subDefault = defaultSub ? "yes" : "no";
            return " --language " + index + ":" + language + " --track-name " + index + ":\"" + name + "\" --default-track " + index + ":" + subDefault;
        }

        private string HardsubGenerator(bool pictureBased, int index, string input)
        {
            // FFmpeg Path Escaping Hell
            // Should look something like this: "C\\\:Users\\\\Username\\\\..."
            input = input.Replace("\u005c", "\u005c\u005c\u005c\u005c").Replace(":", "\u005c\u005c\u005c:");

            // ffmpeg filter commands
            if (pictureBased)
                return " -filter_complex \"[0:v][1:s:" + index + "]overlay[v]\" -map \"[v]\" ";
            return " -vf subtitles=\"" + input + "\":si=" + index;
        }
    }
}
