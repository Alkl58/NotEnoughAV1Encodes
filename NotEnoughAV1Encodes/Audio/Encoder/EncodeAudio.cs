using System.IO;

namespace NotEnoughAV1Encodes
{
    internal class EncodeAudio
    {
        public static bool noaudio = true;
        public static void Encode(string audio_command)
        {
            // Skips Audio Encoding if the audio file already exist
            if (File.Exists(Path.Combine(Global.temp_path, Global.temp_path_folder, "Audio", "audio.mkv")) == false)
            {
                //Creates Audio Directory in the temp dir
                if (!Directory.Exists(Path.Combine(Global.temp_path, Global.temp_path_folder, "Audio")))
                    Directory.CreateDirectory(Path.Combine(Global.temp_path, Global.temp_path_folder, "Audio"));

                if (audio_command != null)
                {
                    noaudio = false;
                }

                // ══════════════════════════════════════ Audio Encoding ══════════════════════════════════════
                if (!noaudio)
                {
                    string ffmpegAudioCommands = "/C ffmpeg.exe -y -i " + '\u0022' + Global.Video_Path + '\u0022' + " -map_metadata -1 -vn -sn -dn " + audio_command + " " + '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "Audio", "audio.mkv") + '\u0022';
                    Helpers.Logging("Encoding Audio: " + ffmpegAudioCommands);
                    SmallFunctions.ExecuteFfmpegTask(ffmpegAudioCommands);
                }
            }
        }
    }
}