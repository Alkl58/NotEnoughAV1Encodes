using System.IO;

namespace NotEnoughAV1Encodes
{
    class EncodeAudio
    {
        public static void Encode()
        {
            // Skips Audio Encoding if the audio file already exist
            if (File.Exists(Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Audio", "audio.mkv")) == false)
            {
                //Creates Audio Directory in the temp dir
                if (!Directory.Exists(Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Audio")))
                    Directory.CreateDirectory(Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Audio"));
                string audioCodec = "";
                int activeTracks = 0;

                //Counts the number of active audio tracks for audio mapping purposes
                if (MainWindow.trackOne) { activeTracks += 1; }
                if (MainWindow.trackTwo) { activeTracks += 1; }
                if (MainWindow.trackThree) { activeTracks += 1; }
                if (MainWindow.trackFour) { activeTracks += 1; }

                if (activeTracks == 1)
                {
                    audioCodec = OneTrackCommandGenerator(MainWindow.audioBitrateTrackOne, MainWindow.audioCodecTrackOne, MainWindow.audioChannelsTrackOne);
                }
                else if (activeTracks >= 2)
                {
                    if (MainWindow.trackOne == true)
                    {
                        audioCodec += MultipleTrackCommandGenerator(MainWindow.audioBitrateTrackOne, "0", MainWindow.audioCodecTrackOne, MainWindow.audioChannelsTrackOne, MainWindow.trackOneLanguage, MainWindow.trackOneName);
                    }
                    if (MainWindow.trackTwo == true)
                    {
                        audioCodec += MultipleTrackCommandGenerator(MainWindow.audioBitrateTrackTwo, "1", MainWindow.audioCodecTrackTwo, MainWindow.audioChannelsTrackTwo, MainWindow.trackTwoLanguage, MainWindow.trackTwoName);
                    }
                    if (MainWindow.trackThree == true)
                    {
                        audioCodec += MultipleTrackCommandGenerator(MainWindow.audioBitrateTrackThree, "2", MainWindow.audioCodecTrackThree, MainWindow.audioChannelsTrackThree, MainWindow.trackThreeLanguage, MainWindow.trackThreeName);
                    }
                    if (MainWindow.trackFour == true)
                    {
                        audioCodec += MultipleTrackCommandGenerator(MainWindow.audioBitrateTrackFour, "3", MainWindow.audioCodecTrackFour, MainWindow.audioChannelsTrackFour, MainWindow.trackFourLanguage, MainWindow.trackFourName);
                    }
                    if (MainWindow.audioCodecTrackOne != "Copy Audio" && MainWindow.audioCodecTrackTwo != "Copy Audio" && MainWindow.audioCodecTrackThree != "Copy Audio" && MainWindow.audioCodecTrackFour != "Copy Audio")
                    {
                        audioCodec += " -af aformat=channel_layouts=" + '\u0022' + "7.1|5.1|stereo|mono" + '\u0022' + " ";
                    }
                }

                // ══════════════════════════════════════ Audio Encoding ══════════════════════════════════════
                string ffmpegAudioCommands = "/C ffmpeg.exe -y -i " + '\u0022' + MainWindow.VideoInput + '\u0022' + " " + MainWindow.TrimCommand + " -map_metadata -1 -vn -sn -dn " + audioCodec + " " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Audio", "audio.mkv") + '\u0022';
                SmallFunctions.Logging("Encoding Audio: " + ffmpegAudioCommands);
                SmallFunctions.ExecuteFfmpegTask(ffmpegAudioCommands);
                // ════════════════════════════════════════════════════════════════════════════════════════════
            }
        }

        private static string audiocodecswitch = "";
        private static string SwitchCodec(string Codec)
        {
            switch (Codec)
            {
                case "Opus": audiocodecswitch = "libopus"; break;
                case "AC3": audiocodecswitch = "ac3"; break;
                case "AAC": audiocodecswitch = "aac"; break;
                case "MP3": audiocodecswitch = "libmp3lame"; break;
                case "Copy Audio": if (MainWindow.pcmBluray) { audiocodecswitch = "pcm_s16le"; } else { audiocodecswitch = "copy"; } break;
                default: break;
            }
            return audiocodecswitch;
        }

        private static string audioCodecCommand = "";
        private static string OneTrackCommandGenerator(int activetrackbitrate, string activtrackcodec, int channellayout)
        {
            // String Command Builder for a single Audio Track
            // Audio Mapping + Codec
            audioCodecCommand = "-map 0:a:0 -c:a " + SwitchCodec(activtrackcodec);
            // Channel Layout / Bitrate
            if (activtrackcodec != "Copy Audio") { audioCodecCommand += " -af aformat=channel_layouts=" + '\u0022' + "7.1|5.1|stereo|mono" + '\u0022' + " -b:a " + activetrackbitrate + "k"; }
            audioCodecCommand += " -ac " + channellayout;
            // Sets Language Metadata
            audioCodecCommand += " -metadata:s:a:0 language=" + MainWindow.trackOneLanguage;
            audioCodecCommand += " -metadata:s:a:0 title=" + '\u0022' + MainWindow.trackOneName + '\u0022' + " ";
            return audioCodecCommand;
        }
        private static string MultipleTrackCommandGenerator(int activetrackbitrate, string activetrackindex, string activtrackcodec, int channellayout, string lang, string track_name)
        {
            // String Command Builder for multiple Audio Tracks
            // Audio Mapping
            audioCodecCommand = " -map 0:a:" + activetrackindex + " -c:a:" + activetrackindex + " ";
            // Codec
            audioCodecCommand += SwitchCodec(activtrackcodec);
            // Channel Layout / Bitrate
            if (activtrackcodec != "Copy Audio") { audioCodecCommand += " -b:a:" + activetrackindex + " " + activetrackbitrate + "k"; }
            audioCodecCommand += " -ac:a:" + activetrackindex + " " + channellayout + " ";
            // Metadata
            audioCodecCommand += " -metadata:s:a:" + activetrackindex + " language=" + lang;
            if (activtrackcodec != "Copy Audio")
            {
                audioCodecCommand += " -metadata:s:a:" + activetrackindex + " title=" + '\u0022' + track_name + '\u0022' + " ";
            }
            return audioCodecCommand;
        }
    }
}
