using System.Diagnostics;
using System.Threading.Tasks;

namespace NotEnoughAV1Encodes
{
    internal class SplitVideo
    {
        public static string ffmpegCommand = "";
        public static void StartSplitting(string videoInput, string tempFolderPath, int chunkLength, bool reencode, string ffmpegPath)
        {
            if (reencode == true)
            {
                ffmpegCommand = "/C ffmpeg.exe -i " + '\u0022' + videoInput + '\u0022' + " -c:v utvideo -f segment -segment_time " + chunkLength + " -an " + '\u0022' + tempFolderPath + "\\Chunks\\out%0d.mkv" + '\u0022';
            }
            else if (reencode == false)
            {
                ffmpegCommand = "/C ffmpeg.exe -i " + '\u0022' + videoInput + '\u0022' + " -vcodec copy -f segment -segment_time " + chunkLength + " -an " + '\u0022' + tempFolderPath + "\\Chunks\\out%0d.mkv" + '\u0022';
            }
            SmallScripts.ExecuteFfmpegTask(ffmpegCommand);

            if (SmallScripts.Cancel.CancelAll == false) { SmallScripts.WriteToFileThreadSafe("True", "splitted.log"); }
        }
    }
}