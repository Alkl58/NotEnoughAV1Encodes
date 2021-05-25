using System.IO;

namespace NotEnoughAV1Encodes
{
    class EncodeAudio
    {
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
                if (MainWindow.trackOne)
                {
                    audio_command += MultipleTrackCommandGenerator(MainWindow.audioBitrateTrackOne, 0, end_index, MainWindow.audioCodecTrackOne, MainWindow.audioChannelsTrackOne, MainWindow.trackOneLanguage, MainWindow.trackOneName);
                    end_index += 1;
                }
                if (MainWindow.trackTwo)
                {
                    audio_command += MultipleTrackCommandGenerator(MainWindow.audioBitrateTrackTwo, 1, end_index, MainWindow.audioCodecTrackTwo, MainWindow.audioChannelsTrackTwo, MainWindow.trackTwoLanguage, MainWindow.trackTwoName);
                    end_index += 1;
                }
                if (MainWindow.trackThree)
                {
                    audio_command += MultipleTrackCommandGenerator(MainWindow.audioBitrateTrackThree, 2, end_index, MainWindow.audioCodecTrackThree, MainWindow.audioChannelsTrackThree, MainWindow.trackThreeLanguage, MainWindow.trackThreeName);
                    end_index += 1;
                }
                if (MainWindow.trackFour)
                {
                    audio_command += MultipleTrackCommandGenerator(MainWindow.audioBitrateTrackFour, 3, end_index, MainWindow.audioCodecTrackFour, MainWindow.audioChannelsTrackFour, MainWindow.trackFourLanguage, MainWindow.trackFourName);
                }

                if (MainWindow.audioCodecTrackOne != "Copy Audio" && MainWindow.audioCodecTrackTwo != "Copy Audio" && MainWindow.audioCodecTrackThree != "Copy Audio" && MainWindow.audioCodecTrackFour != "Copy Audio")
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

        private static string SwitchCodec(string audio_codec)
        {
            string audio_codec_switch = "";
            switch (audio_codec)
            {
                case "Opus": audio_codec_switch = "libopus"; break;
                case "AC3": audio_codec_switch = "ac3"; break;
                case "AAC": audio_codec_switch = "aac"; break;
                case "MP3": audio_codec_switch = "libmp3lame"; break;
                case "Copy Audio": if (MainWindow.pcmBluray) { audio_codec_switch = "pcm_s16le"; } else { audio_codec_switch = "copy"; } break;
                default: break;
            }
            return audio_codec_switch;
        }

        private static string audioCodecCommand = "";
        private static string MultipleTrackCommandGenerator(int activetrackbitrate, int map_index, int end_index, string activtrackcodec, int channellayout, string lang, string track_name)
        {
            // Command Builder for Audio
            // Audio Mapping
            audioCodecCommand = " -map 0:a:" + map_index + " -c:a:" + end_index + " ";
            // Codec
            audioCodecCommand += SwitchCodec(activtrackcodec);
            // Channel Layout / Bitrate
            if (activtrackcodec != "Copy Audio") { audioCodecCommand += " -b:a:" + end_index + " " + activetrackbitrate + "k"; }
            audioCodecCommand += " -ac:a:" + end_index + " " + channellayout + " ";
            // Metadata
            audioCodecCommand += " -metadata:s:a:" + end_index + " language=" + lang;
            if (activtrackcodec != "Copy Audio")
            {
                audioCodecCommand += " -metadata:s:a:" + end_index + " title=" + '\u0022' + track_name + '\u0022' + " ";
            }
            return audioCodecCommand;
        }
    }
}
