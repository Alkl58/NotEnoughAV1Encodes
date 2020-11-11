using System;
using System.Diagnostics;
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
                //-----------------------------------------------------

                if (MainWindow.audioEncoding == false)
                {
                    if (MainWindow.subtitleEncoding == false)
                    {
                        //No Audio && No Softsubs
                        ffmpegCommand = "/C ffmpeg.exe -y -f concat -safe 0 -i " + '\u0022' + Path.Combine(MainWindow.tempPath, "Chunks", "chunks.txt") + '\u0022' + MainWindow.encoderMetadata + " -c copy " + '\u0022' + MainWindow.videoOutput + '\u0022';
                        SmallFunctions.Logging("VideoMuxing() Command: " + ffmpegCommand);
                        await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));
                    }
                    else
                    {
                        //No Audio && With Softsubs
                        ffmpegCommand = "/C ffmpeg.exe -y -f concat -safe 0 -i " + '\u0022' + Path.Combine(MainWindow.tempPath, "Chunks", "chunks.txt") + '\u0022' + MainWindow.encoderMetadata + " -c copy " + '\u0022' + Path.Combine(MainWindow.tempPath, "temp.mkv") + '\u0022';
                        SmallFunctions.Logging("VideoMuxing() Command: " + ffmpegCommand);
                        await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));

                        //Run mkvmerge command
                        Process mkvToolNix = new Process();
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            WindowStyle = ProcessWindowStyle.Hidden,
                            FileName = "cmd.exe",
                            WorkingDirectory = MainWindow.mkvToolNixPath,
                            Arguments = "/C mkvmerge.exe --output " + '\u0022' + MainWindow.videoOutput + '\u0022' + " --language 0:und --default-track 0:yes " + '\u0022' + Path.Combine(MainWindow.tempPath, "temp.mkv") + '\u0022' + " " + MainWindow.subtitleMuxingInput
                        };
                        mkvToolNix.StartInfo = startInfo;
                        mkvToolNix.Start();
                        mkvToolNix.WaitForExit();
                    }
                }
                else
                {
                    //Temp File
                    ffmpegCommand = "/C ffmpeg.exe -y -f concat -safe 0 -i " + '\u0022' + Path.Combine(MainWindow.tempPath, "Chunks", "chunks.txt") + '\u0022' + MainWindow.encoderMetadata + " -c copy " + '\u0022' + Path.Combine(MainWindow.tempPath, "temp.mkv") + '\u0022';
                    SmallFunctions.Logging("VideoMuxing() Command: " + ffmpegCommand);
                    await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));


                    if (MainWindow.subtitleEncoding == false)
                    {
                        //With Audio && No Softsubs
                        ffmpegCommand = "/C ffmpeg.exe -y -i " + '\u0022' + Path.Combine(MainWindow.tempPath, "temp.mkv") + '\u0022' + " -i " + '\u0022' + Path.Combine(MainWindow.tempPath, "AudioEncoded", "audio.mkv") + '\u0022' + MainWindow.encoderMetadata + " -map 0:v -map 1:a -c copy " + '\u0022' + MainWindow.videoOutput + '\u0022';
                        SmallFunctions.Logging("VideoMuxing() Command: " + ffmpegCommand);
                        await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));
                    }
                    else
                    {
                        //With Audio && With Softsubs
                        //Run mkvmerge command
                        Process mkvToolNix = new Process();
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            WindowStyle = ProcessWindowStyle.Hidden,
                            FileName = "cmd.exe",
                            WorkingDirectory = MainWindow.mkvToolNixPath,
                            Arguments = "/C mkvmerge.exe --output " + '\u0022' + MainWindow.videoOutput + '\u0022' + " --language 0:und --default-track 0:yes " + '\u0022' + Path.Combine(MainWindow.tempPath, "temp.mkv") + '\u0022' + " --default-track 0:yes " + '\u0022' + Path.Combine(MainWindow.tempPath, "AudioEncoded", "audio.mkv") + '\u0022' + " " + MainWindow.subtitleMuxingInput
                        };
                        mkvToolNix.StartInfo = startInfo;
                        SmallFunctions.Logging("mkvmerge.exe --output " + '\u0022' + MainWindow.videoOutput + '\u0022' + " --language 0:und --default-track 0:yes " + '\u0022' + Path.Combine(MainWindow.tempPath, "temp.mkv") + '\u0022' + " --default-track 0:yes " + '\u0022' + Path.Combine(MainWindow.tempPath, "AudioEncoded", "audio.mkv") + '\u0022' + " " + MainWindow.subtitleMuxingInput);
                        mkvToolNix.Start();
                        mkvToolNix.WaitForExit();
                    }
                }
            }
        }
    }
}