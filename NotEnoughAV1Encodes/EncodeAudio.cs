using System.IO;

namespace NotEnoughAV1Encodes
{
    class EncodeAudio
    {
        public static bool trackOne;                // Audio Track One active
        public static bool trackTwo;                // Audio Track Two active
        public static bool trackThree;              // Audio Track Three active
        public static bool trackFour;               // Audio Track Four active

        public static string trackOneLanguage;      // Audio Track One Language
        public static string trackTwoLanguage;      // Audio Track Two Language
        public static string trackThreeLanguage;    // Audio Track Three Language
        public static string trackFourLanguage;     // Audio Track Four Language

        public static int audioBitrateTrackOne;     // Audio Track One Bitrate
        public static int audioBitrateTrackTwo;     // Audio Track Two Bitrate
        public static int audioBitrateTrackThree;   // Audio Track Three Bitrate
        public static int audioBitrateTrackFour;    // Audio Track Four Bitrate

        public static int audioChannelsTrackOne;    // Audio Track One Channels
        public static int audioChannelsTrackTwo;    // Audio Track Two Channels
        public static int audioChannelsTrackThree;  // Audio Track Three Channels
        public static int audioChannelsTrackFour;   // Audio Track Four Channels

        public static string audioCodecTrackOne;    // Audio Track One Codec
        public static string audioCodecTrackTwo;    // Audio Track Two Codec
        public static string audioCodecTrackThree;  // Audio Track Three Codec
        public static string audioCodecTrackFour;   // Audio Track Four Codec

        public static string trackOneName;          // Audio Track One Name
        public static string trackTwoName;          // Audio Track Two Name
        public static string trackThreeName;        // Audio Track Three Name
        public static string trackFourName;         // Audio Track Four Name

        // Explanation: When copying pcm_bluray the codec has to be set to pcm_s16le
        //              else it will fail. (ffmpeg issue)
        public static bool pcm_bluray_1 = false;    // PCM_BluRay Check Track One
        public static bool pcm_bluray_2 = false;    // PCM_BluRay Check Track Two
        public static bool pcm_bluray_3 = false;    // PCM_BluRay Check Track Three
        public static bool pcm_bluray_4 = false;    // PCM_BluRay Check Track Four

        public static void Encode()
        {
            // Skips Audio Encoding if the audio file already exist
            if (File.Exists(Path.Combine(Global.temp_path, Global.temp_path_folder, "Audio", "audio.mkv")) == false)
            {
                //Creates Audio Directory in the temp dir
                if (!Directory.Exists(Path.Combine(Global.temp_path, Global.temp_path_folder, "Audio")))
                    Directory.CreateDirectory(Path.Combine(Global.temp_path, Global.temp_path_folder, "Audio"));
                string audio_command = "";

                int end_index = 0;
                if (trackOne)
                {
                    audio_command += MultipleTrackCommandGenerator(audioBitrateTrackOne, 0, end_index, audioCodecTrackOne, audioChannelsTrackOne, trackOneLanguage, trackOneName, pcm_bluray_1);
                    end_index += 1;
                }
                if (trackTwo)
                {
                    audio_command += MultipleTrackCommandGenerator(audioBitrateTrackTwo, 1, end_index, audioCodecTrackTwo, audioChannelsTrackTwo, trackTwoLanguage, trackTwoName, pcm_bluray_2);
                    end_index += 1;
                }
                if (trackThree)
                {
                    audio_command += MultipleTrackCommandGenerator(audioBitrateTrackThree, 2, end_index, audioCodecTrackThree, audioChannelsTrackThree, trackThreeLanguage, trackThreeName, pcm_bluray_3);
                    end_index += 1;
                }
                if (trackFour)
                {
                    audio_command += MultipleTrackCommandGenerator(audioBitrateTrackFour, 3, end_index, audioCodecTrackFour, audioChannelsTrackFour, trackFourLanguage, trackFourName, pcm_bluray_4);
                }

                if (audioCodecTrackOne != "Copy Audio" && audioCodecTrackTwo != "Copy Audio" && audioCodecTrackThree != "Copy Audio" && audioCodecTrackFour != "Copy Audio")
                {
                    audio_command += " -af aformat=channel_layouts=" + '\u0022' + "7.1|5.1|stereo|mono" + '\u0022' + " ";
                }

                // ══════════════════════════════════════ Audio Encoding ══════════════════════════════════════
                string ffmpegAudioCommands = "/C ffmpeg.exe -y -i " + '\u0022' + Global.Video_Path + '\u0022' + " -map_metadata -1 -vn -sn -dn " + audio_command + " " + '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "Audio", "audio.mkv") + '\u0022';
                Helpers.Logging("Encoding Audio: " + ffmpegAudioCommands);
                SmallFunctions.ExecuteFfmpegTask(ffmpegAudioCommands);
                // ════════════════════════════════════════════════════════════════════════════════════════════
            }
        }

        private static string SwitchCodec(string audio_codec, bool pcm_bluray)
        {
            string audio_codec_switch = "";
            switch (audio_codec)
            {
                case "Opus": audio_codec_switch = "libopus"; break;
                case "AC3": audio_codec_switch = "ac3"; break;
                case "AAC": audio_codec_switch = "aac"; break;
                case "MP3": audio_codec_switch = "libmp3lame"; break;
                case "Copy Audio": 
                    if (pcm_bluray) { audio_codec_switch = "pcm_s16le"; } 
                    else { audio_codec_switch = "copy"; } 
                    break;
                default: break;
            }
            return audio_codec_switch;
        }

        private static string audioCodecCommand = "";
        private static string MultipleTrackCommandGenerator(int activetrackbitrate, int map_index, int end_index, string activtrackcodec, int channellayout, string lang, string track_name, bool pcm_bluray)
        {
            // Command Builder for Audio
            // Audio Mapping
            audioCodecCommand = " -map 0:a:" + map_index + " -c:a:" + end_index + " ";
            // Codec
            audioCodecCommand += SwitchCodec(activtrackcodec, pcm_bluray);
            // Channel Layout / Bitrate
            if (activtrackcodec != "Copy Audio") { audioCodecCommand += " -b:a:" + end_index + " " + activetrackbitrate + "k"; }
            audioCodecCommand += " -ac:a:" + end_index + " " + SetChannelLayout(channellayout) + " ";
            // Metadata
            audioCodecCommand += " -metadata:s:a:" + end_index + " language=" + lang;
            if (activtrackcodec != "Copy Audio")
            {
                audioCodecCommand += " -metadata:s:a:" + end_index + " title=" + '\u0022' + track_name + '\u0022' + " ";
            }
            return audioCodecCommand;
        }

        private static string SetChannelLayout(int layout)
        {
            string returnLayout;
            switch (layout)
            {
                case 0: returnLayout = "1"; break;
                case 1: returnLayout = "2"; break;
                case 2: returnLayout = "6"; break;
                case 3: returnLayout = "8"; break;
                default: returnLayout = "2";  break;
            }
            return returnLayout;
        }
    }
}
