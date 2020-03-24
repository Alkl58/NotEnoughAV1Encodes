using System.Diagnostics;

namespace NotEnoughAV1Encodes
{
    internal class ConcatVideo
    {
        public static void Concat()
        {
            if (SmallScripts.Cancel.CancelAll == false)
            {
                //Lists all ivf files in chunks.txt
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                startInfo.WorkingDirectory = MainWindow.exeffmpegPath;
                //FFmpeg Arguments
                startInfo.Arguments = "/C (for %i in (" + '\u0022' + MainWindow.chunksDir + "\\*.ivf" + '\u0022' + ") do @echo file '%i') > " + '\u0022' + MainWindow.chunksDir + "\\chunks.txt" + '\u0022';
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();

                //Concat the Videofiles for later muxing
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                startInfo.WorkingDirectory = MainWindow.exeffmpegPath;
                if (MainWindow.audioEncoding == false && MainWindow.subtitles == false)
                {
                    startInfo.Arguments = "/C ffmpeg.exe -f concat -safe 0 -i " + '\u0022' + MainWindow.chunksDir + "\\chunks.txt" + '\u0022' + " -c copy " + '\u0022' + MainWindow.videoOutput + '\u0022';
                }
                else
                {
                    startInfo.Arguments = "/C ffmpeg.exe -f concat -safe 0 -i " + '\u0022' + MainWindow.chunksDir + "\\chunks.txt" + '\u0022' + " -c copy " + '\u0022' + MainWindow.workingTempDirectory + "\\withoutaudio.mkv" + '\u0022';
                }
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();

                if (MainWindow.audioEncoding != false || MainWindow.subtitles != false)
                {
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo.FileName = "cmd.exe";
                    startInfo.WorkingDirectory = MainWindow.exeffmpegPath;
                    if (MainWindow.audioEncoding == true && MainWindow.subtitles == false)
                    {
                        startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + MainWindow.workingTempDirectory + "\\withoutaudio.mkv" + '\u0022' + " -i " + '\u0022' + MainWindow.workingTempDirectory + "\\AudioEncoded\\audio.mkv" + '\u0022' + " -map_metadata -1 -map 0:v -map 1:a -c copy " + '\u0022' + MainWindow.videoOutput + '\u0022';
                    }
                    else if (MainWindow.audioEncoding == true && MainWindow.subtitles == true && MainWindow.subtitleStreamCopy == true && MainWindow.subtitleCustom == false)
                    {
                        startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + MainWindow.workingTempDirectory + "\\withoutaudio.mkv" + '\u0022' + " -i " + '\u0022' + MainWindow.workingTempDirectory + "\\AudioEncoded\\audio.mkv" + '\u0022' + " -i " + MainWindow.workingTempDirectory + "\\Subtitles\\subtitle.mkv" + " -map_metadata -1 -map 0:v -map 1:a -map 2:s -c copy " + '\u0022' + MainWindow.videoOutput + '\u0022';
                    }
                    else if (MainWindow.audioEncoding == true && MainWindow.subtitles == true && MainWindow.subtitleStreamCopy == false && MainWindow.subtitleCustom == true)
                    {
                        startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + MainWindow.workingTempDirectory + "\\withoutaudio.mkv" + '\u0022' + " -i " + '\u0022' + MainWindow.workingTempDirectory + "\\AudioEncoded\\audio.mkv" + '\u0022' + " -i " + MainWindow.workingTempDirectory + "\\Subtitles\\subtitlecustom.mkv" + " -map_metadata -1 -map 0:v -map 1:a -map 2:s -c copy " + '\u0022' + MainWindow.videoOutput + '\u0022';
                    }
                    else if (MainWindow.audioEncoding == false && MainWindow.subtitles == true && MainWindow.subtitleStreamCopy == false && MainWindow.subtitleCustom == true)
                    {
                        startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + MainWindow.workingTempDirectory + "\\withoutaudio.mkv" + '\u0022' + " -i " + MainWindow.workingTempDirectory + "\\Subtitles\\subtitlecustom.mkv" + " -map_metadata -1 -map 0:v -map 1:s -c copy " + '\u0022' + MainWindow.videoOutput + '\u0022';
                    }
                    else if (MainWindow.audioEncoding == false && MainWindow.subtitles == true && MainWindow.subtitleStreamCopy == true && MainWindow.subtitleCustom == false)
                    {
                        startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + MainWindow.workingTempDirectory + "\\withoutaudio.mkv" + '\u0022' + " -i " + MainWindow.workingTempDirectory + "\\Subtitles\\subtitle.mkv" + " -map_metadata -1 -map 0:v -map 1:s -c copy " + '\u0022' + MainWindow.videoOutput + '\u0022';
                    }
                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();
                }
            }
        }
    }
}