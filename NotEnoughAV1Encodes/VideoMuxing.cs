using System.IO;
using System.Threading.Tasks;

namespace NotEnoughAV1Encodes
{
    class VideoMuxing
    {
        public static async Task Concat()
        {
            // Writes all ivf files into chunks.txt for later concat
            string ffmpegCommand = "/C (for %i in (" + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Chunks") + "\\*.ivf" + '\u0022' + ") do @echo file '%i') > " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Chunks", "chunks.txt") + '\u0022';
            await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));

            // Basic ffmpeg concat
            ffmpegCommand = "/C ffmpeg.exe -y -f concat -safe 0 -i " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Chunks", "chunks.txt") + '\u0022' + " -c copy " + '\u0022' + MainWindow.VideoOutput + '\u0022';
            await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));
        }
    }
}
