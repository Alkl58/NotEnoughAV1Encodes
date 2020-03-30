using System.Diagnostics;
using System.Threading.Tasks;

namespace NotEnoughAV1Encodes
{
    internal class ConcatVideo
    {
        public static async Task Concat()
        {
            if (SmallScripts.Cancel.CancelAll == false)
            {
                string ffmpegCommand = "/C (for %i in (" + '\u0022' + MainWindow.chunksDir + "\\*.ivf" + '\u0022' + ") do @echo file '%i') > " + '\u0022' + MainWindow.chunksDir + "\\chunks.txt" + '\u0022';
                await Task.Run(() => SmallScripts.ExecuteFfmpegTask(ffmpegCommand));


                if (MainWindow.audioEncoding == false && MainWindow.subtitles == false)
                {
                    ffmpegCommand = "/C ffmpeg.exe -f concat -safe 0 -i " + '\u0022' + MainWindow.chunksDir + "\\chunks.txt" + '\u0022' + " -c copy " + '\u0022' + MainWindow.videoOutput + '\u0022';
                }
                else
                {
                    ffmpegCommand = "/C ffmpeg.exe -f concat -safe 0 -i " + '\u0022' + MainWindow.chunksDir + "\\chunks.txt" + '\u0022' + " -c copy " + '\u0022' + MainWindow.workingTempDirectory + "\\withoutaudio.mkv" + '\u0022';
                }
                await Task.Run(() => SmallScripts.ExecuteFfmpegTask(ffmpegCommand));


                if (MainWindow.audioEncoding != false || MainWindow.subtitles != false)
                {
                    if (MainWindow.audioEncoding == true && MainWindow.subtitles == false)
                    {
                        ffmpegCommand = "/C ffmpeg.exe -i " + '\u0022' + MainWindow.workingTempDirectory + "\\withoutaudio.mkv" + '\u0022' + " -i " + '\u0022' + MainWindow.workingTempDirectory + "\\AudioEncoded\\audio.mkv" + '\u0022' + " -map_metadata -1 -map 0:v -map 1:a -c copy " + '\u0022' + MainWindow.videoOutput + '\u0022';
                    }
                    else if (MainWindow.audioEncoding == true && MainWindow.subtitles == true && MainWindow.subtitleStreamCopy == true && MainWindow.subtitleCustom == false)
                    {
                        ffmpegCommand = "/C ffmpeg.exe -i " + '\u0022' + MainWindow.workingTempDirectory + "\\withoutaudio.mkv" + '\u0022' + " -i " + '\u0022' + MainWindow.workingTempDirectory + "\\AudioEncoded\\audio.mkv" + '\u0022' + " -i " + MainWindow.workingTempDirectory + "\\Subtitles\\subtitle.mkv" + " -map_metadata -1 -map 0:v -map 1:a -map 2:s -c copy " + '\u0022' + MainWindow.videoOutput + '\u0022';
                    }
                    else if (MainWindow.audioEncoding == true && MainWindow.subtitles == true && MainWindow.subtitleStreamCopy == false && MainWindow.subtitleCustom == true)
                    {
                        ffmpegCommand = "/C ffmpeg.exe -i " + '\u0022' + MainWindow.workingTempDirectory + "\\withoutaudio.mkv" + '\u0022' + " -i " + '\u0022' + MainWindow.workingTempDirectory + "\\AudioEncoded\\audio.mkv" + '\u0022' + " -i " + MainWindow.workingTempDirectory + "\\Subtitles\\subtitlecustom.mkv" + " -map_metadata -1 -map 0:v -map 1:a -map 2:s -c copy " + '\u0022' + MainWindow.videoOutput + '\u0022';
                    }
                    else if (MainWindow.audioEncoding == false && MainWindow.subtitles == true && MainWindow.subtitleStreamCopy == false && MainWindow.subtitleCustom == true)
                    {
                        ffmpegCommand = "/C ffmpeg.exe -i " + '\u0022' + MainWindow.workingTempDirectory + "\\withoutaudio.mkv" + '\u0022' + " -i " + MainWindow.workingTempDirectory + "\\Subtitles\\subtitlecustom.mkv" + " -map_metadata -1 -map 0:v -map 1:s -c copy " + '\u0022' + MainWindow.videoOutput + '\u0022';
                    }
                    else if (MainWindow.audioEncoding == false && MainWindow.subtitles == true && MainWindow.subtitleStreamCopy == true && MainWindow.subtitleCustom == false)
                    {
                        ffmpegCommand = "/C ffmpeg.exe -i " + '\u0022' + MainWindow.workingTempDirectory + "\\withoutaudio.mkv" + '\u0022' + " -i " + MainWindow.workingTempDirectory + "\\Subtitles\\subtitle.mkv" + " -map_metadata -1 -map 0:v -map 1:s -c copy " + '\u0022' + MainWindow.videoOutput + '\u0022';
                    }
                    await Task.Run(() => SmallScripts.ExecuteFfmpegTask(ffmpegCommand));
                    SmallScripts.CheckSuccessfulEncode();
                }
            }
        }
    }
}