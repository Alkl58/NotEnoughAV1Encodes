using System;
using System.IO;

namespace NotEnoughAV1Encodes
{
    internal class EncodeAudio
    {
        public static void AudioEncode()
        {
            if (MainWindow.audioEncoding == true && MainWindow.resumeMode == false && SmallFunctions.Cancel.CancelAll == false)
            {
                //Creates AudioEncoded Directory in the temp dir
                if (!Directory.Exists(Path.Combine(MainWindow.tempPath, "AudioEncoded")))
                    Directory.CreateDirectory(Path.Combine(MainWindow.tempPath, "AudioEncoded"));

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
                        audioCodec += MultipleTrackCommandGenerator(MainWindow.audioBitrateTrackOne, "0", indexinteger, MainWindow.audioCodecTrackOne, MainWindow.audioChannelsTrackOne);
                        indexinteger += 1;
                    }
                    if (MainWindow.trackTwo == true)
                    {
                        audioCodec += MultipleTrackCommandGenerator(MainWindow.audioBitrateTrackTwo, "1", indexinteger, MainWindow.audioCodecTrackTwo, MainWindow.audioChannelsTrackTwo);
                        indexinteger += 1;
                    }
                    if (MainWindow.trackThree == true)
                    {
                        audioCodec += MultipleTrackCommandGenerator(MainWindow.audioBitrateTrackThree, "2", indexinteger, MainWindow.audioCodecTrackThree, MainWindow.audioChannelsTrackThree);
                        indexinteger += 1;
                    }
                    if (MainWindow.trackFour == true)
                    {
                        audioCodec += MultipleTrackCommandGenerator(MainWindow.audioBitrateTrackFour, "3", indexinteger, MainWindow.audioCodecTrackFour, MainWindow.audioChannelsTrackFour);
                        indexinteger += 1;
                    }
                }
                //----------------------------------------------------------------------------------------||
                //Audio Encoding -------------------------------------------------------------------------||
                string ffmpegAudioCommands = "/C ffmpeg.exe -y -i " + '\u0022' + MainWindow.videoInput + '\u0022' + " -map_metadata -1 -vn -sn -dn " + audioCodec + MainWindow.trimCommand + '\u0022' + Path.Combine(MainWindow.tempPath, "AudioEncoded", "audio.mkv") + '\u0022';
                SmallFunctions.Logging("AudioEncode() Command: " + ffmpegAudioCommands);
                SmallFunctions.ExecuteFfmpegTask(ffmpegAudioCommands);
                //----------------------------------------------------------------------------------------||
            }
        }
        private static string audiocodecswitch = "";
        private static string SwitchCodec(string Codec, int track)
        {
            switch (Codec)
            {
                case "Opus":
                    if (track == 2 || track == 1) { audiocodecswitch = "libopus"; }
                    else if (track == 6) { audiocodecswitch = "libopus -af channelmap=channel_layout=5.1"; }
                    else if (track == 8) { audiocodecswitch = "libopus -af channelmap=channel_layout=7.1"; }
                    break;
                case "AC3": audiocodecswitch = "ac3"; break;
                case "AAC": audiocodecswitch = "aac"; break;
                case "MP3": audiocodecswitch = "libmp3lame"; break;
                case "Copy Audio": audiocodecswitch = "copy"; break;
                default:
                    break;
            }

            return audiocodecswitch;
        }

        private static string audioCodecCommand = "";
        private static string OneTrackCommandGenerator(int activetrackbitrate, string activetrackindex, string activtrackcodec, int channellayout)
        {
            //String Command Builder for a single Audio Track
            audioCodecCommand = "-map 0:a:" + activetrackindex + " -c:a ";
            audioCodecCommand += SwitchCodec(activtrackcodec, channellayout);
            if (activtrackcodec != "Copy Audio") { audioCodecCommand += " -b:a " + activetrackbitrate + "k"; }
            audioCodecCommand += " -ac " + channellayout;
            return audioCodecCommand;
        }

        private static string MultipleTrackCommandGenerator(int activetrackbitrate, string activetrackindex, int activetrackaudioindex, string activtrackcodec, int channellayout)
        {
            //String Command Builder for multiple Audio Tracks
            audioCodecCommand = "-map 0:a:" + activetrackindex + " -c:a:" + activetrackaudioindex + " ";
            audioCodecCommand += SwitchCodec(activtrackcodec, channellayout);
            if (activtrackcodec != "Copy Audio") { audioCodecCommand += " -b:a:" + activetrackaudioindex + " " + activetrackbitrate + "k"; }
            audioCodecCommand += " -ac:a:" + activetrackaudioindex + " " + channellayout + " ";
            return audioCodecCommand;
        }
    }
}