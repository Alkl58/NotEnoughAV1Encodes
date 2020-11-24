﻿using System.IO;

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
                int numberoftracksactive = 0, indexinteger = 0;

                //Counts the number of active audio tracks for audio mapping purposes
                if (MainWindow.trackOne == true) { numberoftracksactive += 1; }
                if (MainWindow.trackTwo == true) { numberoftracksactive += 1; }
                if (MainWindow.trackThree == true) { numberoftracksactive += 1; }
                if (MainWindow.trackFour == true) { numberoftracksactive += 1; }

                if (numberoftracksactive == 1)
                {
                    if (MainWindow.trackOne == true) { audioCodec = OneTrackCommandGenerator(MainWindow.audioBitrateTrackOne, "0", MainWindow.audioCodecTrackOne, MainWindow.audioChannelsTrackOne); }
                    if (MainWindow.trackTwo == true) { audioCodec = OneTrackCommandGenerator(MainWindow.audioBitrateTrackTwo, "1", MainWindow.audioCodecTrackTwo, MainWindow.audioChannelsTrackTwo); }
                    if (MainWindow.trackThree == true) { audioCodec = OneTrackCommandGenerator(MainWindow.audioBitrateTrackThree, "2", MainWindow.audioCodecTrackThree, MainWindow.audioChannelsTrackThree); }
                    if (MainWindow.trackFour == true) { audioCodec = OneTrackCommandGenerator(MainWindow.audioBitrateTrackFour, "3", MainWindow.audioCodecTrackFour, MainWindow.audioChannelsTrackFour); }
                }
                else
                {
                    if (MainWindow.trackOne == true)
                    {
                        audioCodec += MultipleTrackCommandGenerator(MainWindow.audioBitrateTrackOne, "0", indexinteger, MainWindow.audioCodecTrackOne, MainWindow.audioChannelsTrackOne, MainWindow.trackOneLanguage);
                        indexinteger += 1;
                    }
                    if (MainWindow.trackTwo == true)
                    {
                        audioCodec += MultipleTrackCommandGenerator(MainWindow.audioBitrateTrackTwo, "1", indexinteger, MainWindow.audioCodecTrackTwo, MainWindow.audioChannelsTrackTwo, MainWindow.trackTwoLanguage);
                        indexinteger += 1;
                    }
                    if (MainWindow.trackThree == true)
                    {
                        audioCodec += MultipleTrackCommandGenerator(MainWindow.audioBitrateTrackThree, "2", indexinteger, MainWindow.audioCodecTrackThree, MainWindow.audioChannelsTrackThree, MainWindow.trackThreeLanguage);
                        indexinteger += 1;
                    }
                    if (MainWindow.trackFour == true)
                    {
                        audioCodec += MultipleTrackCommandGenerator(MainWindow.audioBitrateTrackFour, "3", indexinteger, MainWindow.audioCodecTrackFour, MainWindow.audioChannelsTrackFour, MainWindow.trackFourLanguage);
                    }
                    if (MainWindow.audioCodecTrackOne != "Copy Audio" && MainWindow.audioCodecTrackTwo != "Copy Audio" && MainWindow.audioCodecTrackThree != "Copy Audio" && MainWindow.audioCodecTrackFour != "Copy Audio")
                    {
                        audioCodec += " -af aformat=channel_layouts=" + '\u0022' + "7.1|5.1|stereo|mono" + '\u0022' + " ";
                    }
                }

                // ══════════════════════════════════════ Audio Encoding ══════════════════════════════════════
                string ffmpegAudioCommands = "/C ffmpeg.exe -y -i " + '\u0022' + MainWindow.VideoInput + '\u0022' + " -map_metadata -1 -vn -sn -dn " + audioCodec + " " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Audio", "audio.mkv") + '\u0022';
                SmallFunctions.ExecuteFfmpegTask(ffmpegAudioCommands);
                // ════════════════════════════════════════════════════════════════════════════════════════════
            }
        }

        private static string audiocodecswitch = "";
        private static string SwitchCodec(string Codec, int track)
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
        private static string OneTrackCommandGenerator(int activetrackbitrate, string activetrackindex, string activtrackcodec, int channellayout)
        {
            // String Command Builder for a single Audio Track
            // Audio Mapping
            audioCodecCommand = "-map 0:a:" + activetrackindex + " -c:a ";
            // Codec
            audioCodecCommand += SwitchCodec(activtrackcodec, channellayout);
            // Channel Layout / Bitrate
            if (activtrackcodec != "Copy Audio") { audioCodecCommand += " -af aformat=channel_layouts=" + '\u0022' + "7.1|5.1|stereo|mono" + '\u0022' + " -b:a " + activetrackbitrate + "k"; }
            audioCodecCommand += " -ac " + channellayout;
            // Metadata
            if (MainWindow.trackOneLanguage != "unknown") 
            { 
                // Sets Language Metadata
                audioCodecCommand += " -metadata:s:a:0 language=" + MainWindow.trackOneLanguage;
                if (activtrackcodec != "Copy Audio")
                {
                    // Sets Track Name e.g. "[GER] Opus 128kbps"
                    audioCodecCommand += " -metadata:s:a:0 title=" + '\u0022' + "[" + MainWindow.trackOneLanguage.ToUpper() + "] " + activtrackcodec + " " + activetrackbitrate + "kbps" + '\u0022' + " ";
                }
            }
            else
            {
                if (activtrackcodec != "Copy Audio")
                {
                    audioCodecCommand += " -metadata:s:a:0 title=" + '\u0022' + "[UND] " + activtrackcodec + " " + activetrackbitrate + "kbps" + '\u0022' + " ";
                }  
            }
            
            return audioCodecCommand;
        }
        private static string MultipleTrackCommandGenerator(int activetrackbitrate, string activetrackindex, int activetrackaudioindex, string activtrackcodec, int channellayout, string lang)
        {
            // String Command Builder for multiple Audio Tracks
            // Audio Mapping
            audioCodecCommand = "-map 0:a:" + activetrackindex + " -c:a:" + activetrackaudioindex + " ";
            // Codec
            audioCodecCommand += SwitchCodec(activtrackcodec, channellayout);
            // Channel Layout / Bitrate
            if (activtrackcodec != "Copy Audio") { audioCodecCommand += " -b:a:" + activetrackaudioindex + " " + activetrackbitrate + "k"; }
            audioCodecCommand += " -ac:a:" + activetrackaudioindex + " " + channellayout + " ";
            // Metadata
            if (lang != "unknown") 
            { 
                audioCodecCommand += " -metadata:s:a:" + activetrackaudioindex + " language=" + lang;
                if (activtrackcodec != "Copy Audio")
                {
                    audioCodecCommand += " -metadata:s:a:" + activetrackaudioindex + " title=" + '\u0022' + "[" + lang.ToUpper() + "] " + activtrackcodec + " " + activetrackbitrate + "kbps" + '\u0022' + " ";
                } 
            }
            else
            {
                if (activtrackcodec != "Copy Audio")
                {
                    audioCodecCommand += " -metadata:s:a:" + activetrackaudioindex + " title=" + '\u0022' + "[UND] " + activtrackcodec + " " + activetrackbitrate + "kbps" + '\u0022' + " ";
                }     
            }
            return audioCodecCommand;
        }
    }
}
