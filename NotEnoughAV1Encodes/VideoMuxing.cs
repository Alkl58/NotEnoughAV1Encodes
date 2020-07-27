using System.IO;
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
                    if(MainWindow.subtitleEncoding == false)
                    {
                        ffmpegCommand = "/C ffmpeg.exe -y -f concat -safe 0 -i " + '\u0022' + Path.Combine(MainWindow.tempPath, "Chunks") + "\\chunks.txt" + '\u0022' + " -c copy " + '\u0022' + MainWindow.videoOutput + '\u0022';
                        SmallFunctions.Logging("VideoMuxing() Command: " + ffmpegCommand);
                        await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));
                    }
                    else if (MainWindow.subtitleHardcoding == false)
                    {
                        ffmpegCommand = "/C ffmpeg.exe -y -f concat -safe 0 -i " + '\u0022' + Path.Combine(MainWindow.tempPath, "Chunks") + "\\chunks.txt" + '\u0022' + " -c copy " + '\u0022' + MainWindow.tempPath + "\\withoutaudio.mkv" + '\u0022';
                        SmallFunctions.Logging("VideoMuxing() Command: " + ffmpegCommand);
                        await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));
                        ffmpegCommand = "/C ffmpeg.exe -y -i " + '\u0022' + MainWindow.tempPath + "\\withoutaudio.mkv" + '\u0022' + " -i " + MainWindow.tempPath + "\\Subtitles\\subtitle.mkv" + " -map_metadata -1 -map 0:v -map 1:s -c copy " + '\u0022' + MainWindow.videoOutput + '\u0022';
                        SmallFunctions.Logging("VideoMuxing() Command: " + ffmpegCommand);
                        await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));
                    }
                    else
                    {
                        ffmpegCommand = "/C ffmpeg.exe -y -f concat -safe 0 -i " + '\u0022' + Path.Combine(MainWindow.tempPath, "Chunks") + "\\chunks.txt" + '\u0022' + " -c copy " + '\u0022' + MainWindow.videoOutput + '\u0022';
                        SmallFunctions.Logging("VideoMuxing() Command: " + ffmpegCommand);
                        await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));
                    }
                }
                else
                {
                    ffmpegCommand = "/C ffmpeg.exe -y -f concat -safe 0 -i " + '\u0022' + Path.Combine(MainWindow.tempPath, "Chunks") + "\\chunks.txt" + '\u0022' + " -c copy " + '\u0022' + MainWindow.tempPath + "\\withoutaudio.mkv" + '\u0022';
                    SmallFunctions.Logging("VideoMuxing() Command: " + ffmpegCommand);
                    await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));
                    if (MainWindow.subtitleEncoding == false)
                    {
                        ffmpegCommand = "/C ffmpeg.exe -y -i " + '\u0022' + MainWindow.tempPath + "\\withoutaudio.mkv" + '\u0022' + " -i " + '\u0022' + MainWindow.tempPath + "\\AudioEncoded\\audio.mkv" + '\u0022' + " -map_metadata -1 -map 0:v -map 1:a -c copy " + '\u0022' + MainWindow.videoOutput + '\u0022';
                        SmallFunctions.Logging("VideoMuxing() Command: " + ffmpegCommand);
                        await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));
                    }
                    else if (MainWindow.subtitleHardcoding == false)
                    {
                        ffmpegCommand = "/C ffmpeg.exe -y -i " + '\u0022' + MainWindow.tempPath + "\\withoutaudio.mkv" + '\u0022' + " -i " + '\u0022' + MainWindow.tempPath + "\\AudioEncoded\\audio.mkv" + '\u0022' + " -i " + MainWindow.tempPath + "\\Subtitles\\subtitlecustom.mkv" + " -map_metadata -1 -map 0:v -map 1:a -map 2:s -c copy " + '\u0022' + MainWindow.videoOutput + '\u0022';
                        SmallFunctions.Logging("VideoMuxing() Command: " + ffmpegCommand);
                        await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));
                    }
                    else
                    {
                        ffmpegCommand = "/C ffmpeg.exe -y -i " + '\u0022' + MainWindow.tempPath + "\\withoutaudio.mkv" + '\u0022' + " -i " + '\u0022' + MainWindow.tempPath + "\\AudioEncoded\\audio.mkv" + '\u0022' + " -map_metadata -1 -map 0:v -map 1:a -c copy " + '\u0022' + MainWindow.videoOutput + '\u0022';
                        SmallFunctions.Logging("VideoMuxing() Command: " + ffmpegCommand);
                        await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));
                    }
                }
            }
        }
    }
}
