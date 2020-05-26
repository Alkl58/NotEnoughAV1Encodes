using System;
using System.Diagnostics;
using System.IO;

namespace NotEnoughAV1Encodes
{
    internal class EncodeAudio
    {
        public static void AudioEncode()
        {
            if (MainWindow.audioEncoding == true)
            {
                string audioInput = "";
                string audioMapping = "";
                string audioCodec = "";
                string audioCodecTrackTwo = "";
                string audioCodecTrackThree = "";
                string audioCodecTrackFour = "";
                bool skipbitrate = false;
                bool skipbitrateTrackTwo = false;
                bool skipbitrateTrackThree = false;
                bool skipbitrateTrackFour = false;
                //Creates AudioEncoded Directory in the temp dir
                if (!Directory.Exists(Path.Combine(MainWindow.workingTempDirectory, "AudioEncoded")))
                    Directory.CreateDirectory(Path.Combine(MainWindow.workingTempDirectory, "AudioEncoded"));
                //Audio Encoder --------------------------------------------------------------------------||
                //Audio Codec Switch Track One
                switch (MainWindow.audioCodec)
                {
                    case "Opus":
                        audioCodec = "libopus";
                        break;
                    case "Opus 5.1":
                        audioCodec = "libopus -af channelmap=channel_layout=5.1";
                        break;
                    case "Opus Downmix":
                        audioCodec = "libopus -ac 2";
                        break;
                    case "AC3":
                        audioCodec = "ac3";
                        break;
                    case "AAC":
                        audioCodec = "aac";
                        break;
                    case "MP3":
                        audioCodec = "libmp3lame";
                        break;
                    case "Copy Audio":
                        audioCodec = "copy";
                        skipbitrate = true;
                        break;
                    default:
                        break;
                }
                //Audio Codec Switch Track Two
                switch (MainWindow.audioCodecTrackTwo)
                {
                    case "Opus":
                        audioCodecTrackTwo = "libopus";
                        break;
                    case "Opus 5.1":
                        audioCodecTrackTwo = "libopus -af channelmap=channel_layout=5.1";
                        break;
                    case "Opus Downmix":
                        audioCodecTrackTwo = "libopus -ac 2";
                        break;
                    case "AC3":
                        audioCodecTrackTwo = "ac3";
                        break;
                    case "AAC":
                        audioCodecTrackTwo = "aac";
                        break;
                    case "MP3":
                        audioCodecTrackTwo = "libmp3lame";
                        break;
                    case "Copy Audio":
                        audioCodecTrackTwo = "copy";
                        skipbitrateTrackTwo = true;
                        break;
                    default:
                        break;
                }
                //Audio Codec Switch Track Three
                switch (MainWindow.audioCodecTrackThree)
                {
                    case "Opus":
                        audioCodecTrackThree = "libopus";
                        break;
                    case "Opus 5.1":
                        audioCodecTrackThree = "libopus -af channelmap=channel_layout=5.1";
                        break;
                    case "Opus Downmix":
                        audioCodecTrackThree = "libopus -ac 2";
                        break;
                    case "AC3":
                        audioCodecTrackThree = "ac3";
                        break;
                    case "AAC":
                        audioCodecTrackThree = "aac";
                        break;
                    case "MP3":
                        audioCodecTrackThree = "libmp3lame";
                        break;
                    case "Copy Audio":
                        audioCodecTrackThree = "copy";
                        skipbitrateTrackThree = true;
                        break;
                    default:
                        break;
                }
                //Audio Codec Switch Track Four
                switch (MainWindow.audioCodecTrackFour)
                {
                    case "Opus":
                        audioCodecTrackFour = "libopus";
                        break;
                    case "Opus 5.1":
                        audioCodecTrackFour = "libopus -af channelmap=channel_layout=5.1";
                        break;
                    case "Opus Downmix":
                        audioCodecTrackFour = "libopus -ac 2";
                        break;
                    case "AC3":
                        audioCodecTrackFour = "ac3";
                        break;
                    case "AAC":
                        audioCodecTrackFour = "aac";
                        break;
                    case "MP3":
                        audioCodecTrackFour = "libmp3lame";
                        break;
                    case "Copy Audio":
                        audioCodecTrackFour = "copy";
                        skipbitrateTrackFour = true;
                        break;
                    default:
                        break;
                }
                //----------------------------------------------------------------------------------------||
                //Audio Mapping --------------------------------------------------------------------------||
                if (MainWindow.trackOne == true && MainWindow.detectedTrackOne == true)
                {
                    switch (skipbitrate)
                    {
                        case true:
                            audioInput += " -c:a:0 " + audioCodec;
                            break;
                        case false:
                            audioInput += " -c:a:0 " + audioCodec + " -b:a:0 " + MainWindow.audioBitrate + "k";
                            break;
                        default:
                            break;
                    }
                    
                    audioMapping += " -map 0:"+ MainWindow.firstTrackIndex + " ";
                }
                if (MainWindow.trackTwo == true && MainWindow.detectedTrackTwo == true)
                {
                    switch (skipbitrateTrackTwo)
                    {
                        case true:
                            audioInput += " -c:a:1 " + audioCodecTrackTwo;
                            break;
                        case false:
                            audioInput += " -c:a:1 " + audioCodecTrackTwo + " -b:a:1 " + MainWindow.audioBitrateTrackTwo + "k";
                            break;
                        default:
                            break;
                    }
                    audioMapping += " -map 0:" + MainWindow.secondTrackIndex + " ";
                }
                if (MainWindow.trackThree == true && MainWindow.detectedTrackThree == true)
                {
                    switch (skipbitrateTrackThree)
                    {
                        case true:
                            audioInput += " -c:a:2 " + audioCodecTrackThree;
                            break;
                        case false:
                            audioInput += " -c:a:2 " + audioCodecTrackThree + " -b:a:2 " + MainWindow.audioBitrateTrackThree + "k";
                            break;
                        default:
                            break;
                    }
                    audioMapping += " -map 0:" + MainWindow.thirdTrackIndex + " ";
                }
                if (MainWindow.trackFour == true && MainWindow.detectedTrackFour == true)
                {
                    switch (skipbitrateTrackFour)
                    {
                        case true:
                            audioInput += " -c:a:3 " + audioCodecTrackFour;
                            break;
                        case false:
                            audioInput += " -c:a:3 " + audioCodecTrackFour + " -b:a:3 " + MainWindow.audioBitrateTrackFour + "k";
                            break;
                        default:
                            break;
                    }
                    audioMapping += " -map 0:" + MainWindow.fourthTrackIndex + " ";
                }
                //----------------------------------------------------------------------------------------||
                //Audio Encoding -------------------------------------------------------------------------||
                string ffmpegAudioCommands = "/C ffmpeg.exe -y -i " + '\u0022' + MainWindow.videoInput + '\u0022' + " -map_metadata -1 -vn -sn -dn" + audioInput + audioMapping + '\u0022' + MainWindow.workingTempDirectory + "\\AudioEncoded\\audio.mkv" + '\u0022';
                SmallScripts.ExecuteFfmpegTask(ffmpegAudioCommands);
                //----------------------------------------------------------------------------------------||
            }
        }
    }
}
