using System.Diagnostics;
using System.Threading.Tasks;

namespace NotEnoughAV1Encodes
{
    internal class SplitVideo
    {
        public static string prereencodeCommand = "";
        public static string ffmpegCommand = "";
        public static void StartSplitting(string videoInput, string tempFolderPath, int chunkLength, bool reencode, bool prereencode, string reencodecodec, string prereencodecodec)
        {
            if (reencodecodec == "x264"){ reencodecodec = "libx264 -crf 0 -preset ultrafast"; }
            if (prereencodecodec == "x264") { prereencodecodec = "libx264 -crf 0 -preset ultrafast"; }

            switch (prereencode)
            {
                case true:
                    prereencodeCommand = "/C ffmpeg.exe -i " + '\u0022' + videoInput + '\u0022' + " -map_metadata -1 -c:v " + prereencodecodec + " -an " + '\u0022' + tempFolderPath + "\\temp_prereencode.mkv" + '\u0022';
                    SmallScripts.ExecuteFfmpegTask(prereencodeCommand);
                    if (reencode == true)
                    {
                        ffmpegCommand = "/C ffmpeg.exe -i " + '\u0022' + tempFolderPath + "\\temp_prereencode.mkv" + '\u0022' + " -map_metadata -1 -c:v " + reencodecodec + " -f segment -segment_time " + chunkLength + " -an " + '\u0022' + tempFolderPath + "\\Chunks\\out%0d.mkv" + '\u0022';
                    }
                    else if (reencode == false)
                    {
                        ffmpegCommand = "/C ffmpeg.exe -i " + '\u0022' + tempFolderPath + "\\temp_prereencode.mkv" + '\u0022' + " -map_metadata -1 -vcodec copy -f segment -segment_time " + chunkLength + " -an " + '\u0022' + tempFolderPath + "\\Chunks\\out%0d.mkv" + '\u0022';
                    }
                    break;
                case false:
                    if (reencode == true)
                    {
                        ffmpegCommand = "/C ffmpeg.exe -i " + '\u0022' + videoInput + '\u0022' + " -map_metadata -1 -c:v " + reencodecodec + " -f segment -segment_time " + chunkLength + " -an " + '\u0022' + tempFolderPath + "\\Chunks\\out%0d.mkv" + '\u0022';
                    }
                    else if (reencode == false)
                    {
                        ffmpegCommand = "/C ffmpeg.exe -i " + '\u0022' + videoInput + '\u0022' + " -map_metadata -1 -vcodec copy -f segment -segment_time " + chunkLength + " -an " + '\u0022' + tempFolderPath + "\\Chunks\\out%0d.mkv" + '\u0022';
                    }
                    break;
                default:
                    break;
            
            }
            SmallScripts.ExecuteFfmpegTask(ffmpegCommand);
            if (SmallScripts.Cancel.CancelAll == false) { SmallScripts.WriteToFileThreadSafe("True", "splitted.log"); }
        }
    }
}