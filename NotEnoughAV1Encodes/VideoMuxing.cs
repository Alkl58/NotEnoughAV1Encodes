using System.IO;
using System.Threading.Tasks;

namespace NotEnoughAV1Encodes
{
    class VideoMuxing
    {
        public static async Task Concat()
        {
            // ══════════════════════════════════════ Chunk Parsing ══════════════════════════════════════
            // Writes all ivf files into chunks.txt for later concat
            string ffmpegCommand = "/C (for %i in (" + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Chunks") + "\\*.ivf" + '\u0022' + ") do @echo file '%i') > " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Chunks", "chunks.txt") + '\u0022';
            await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));

            // ════════════════════════════════════ Muxing with Audio ════════════════════════════════════
            if (MainWindow.trackOne || MainWindow.trackTwo || MainWindow.trackThree || MainWindow.trackFour)
            {
                // First Concats the video to a temp.mkv file
                ffmpegCommand = "/C ffmpeg.exe -y -f concat -safe 0 -i " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Chunks", "chunks.txt") + '\u0022' + " -c copy " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "temp.mkv") + '\u0022';
                await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));

                // Muxes Video & Audio together
                ffmpegCommand = "/C ffmpeg.exe -y -i " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "temp.mkv") + '\u0022' + " -i " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Audio", "audio.mkv") + '\u0022' + " -map 0:v -map 1:a -c copy " + '\u0022' + MainWindow.VideoOutput + '\u0022';
                await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));
            }
            else
            {
                // ═════════════════════════════════ Muxing without Audio ════════════════════════════════
                // Video Concat
                ffmpegCommand = "/C ffmpeg.exe -y -f concat -safe 0 -i " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Chunks", "chunks.txt") + '\u0022' + " -c copy " + '\u0022' + MainWindow.VideoOutput + '\u0022';
                await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));
            }
        }
    }
}
