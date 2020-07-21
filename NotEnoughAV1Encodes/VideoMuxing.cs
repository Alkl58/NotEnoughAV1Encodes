using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotEnoughAV1Encodes
{
    class VideoMuxing
    {
        public static async Task Concat()
        {
            if (SmallFunctions.Cancel.CancelAll == false)
            {
                //Writes all ivf files into chunks.txt for later concat
                string ffmpegCommand = "/C (for %i in (" + '\u0022' + Path.Combine(MainWindow.tempPath, "Chunks") + "\\*.ivf" + '\u0022' + ") do @echo file '%i') > " + '\u0022' + Path.Combine(MainWindow.tempPath, "Chunks") + "\\chunks.txt" + '\u0022';
                await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));

                if (MainWindow.audioEncoding == false)
                {
                    ffmpegCommand = "/C ffmpeg.exe -f concat -safe 0 -i " + '\u0022' + Path.Combine(MainWindow.tempPath, "Chunks") + "\\chunks.txt" + '\u0022' + " -c copy " + '\u0022' + MainWindow.videoOutput + '\u0022';
                    await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));
                }
                else
                {
                    ffmpegCommand = "/C ffmpeg.exe -f concat -safe 0 -i " + '\u0022' + Path.Combine(MainWindow.tempPath, "Chunks") + "\\chunks.txt" + '\u0022' + " -c copy " + '\u0022' + MainWindow.tempPath + "\\withoutaudio.mkv" + '\u0022';
                    await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));
                    ffmpegCommand = "/C ffmpeg.exe -i " + '\u0022' + MainWindow.tempPath + "\\withoutaudio.mkv" + '\u0022' + " -i " + '\u0022' + MainWindow.tempPath + "\\AudioEncoded\\audio.mkv" + '\u0022' + " -map_metadata -1 -map 0:v -map 1:a -c copy " + '\u0022' + MainWindow.videoOutput + '\u0022';
                    await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));
                }
            }
        }
    }
}
