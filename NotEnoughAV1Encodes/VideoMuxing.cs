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
            // ══════════════════════════════════════ Chunk Parsing ══════════════════════════════════════
            // Writes all ivf files into chunks.txt for later concat
            string ffmpegCommand = "/C (for %i in (" + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Chunks") + "\\*.ivf" + '\u0022' + ") do @echo file '%i') > " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Chunks", "chunks.txt") + '\u0022';
            await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));

            // ════════════════════════════════════ Muxing with Audio ════════════════════════════════════
            if (MainWindow.trackOne || MainWindow.trackTwo || MainWindow.trackThree || MainWindow.trackFour)
            {
                // First Concats the video to a temp.mkv file
                ffmpegCommand = "/C ffmpeg.exe -y -f concat -safe 0 -i " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Chunks", "chunks.txt") + '\u0022' + " -c copy " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "temp.mkv") + '\u0022';
                SmallFunctions.Logging("Muxing: " + ffmpegCommand);
                await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));

                if (MainWindow.subSoftSubEnabled != true)
                {
                    // Muxes Video & Audio together
                    // Run mkvmerge command
                    Process mkvToolNix = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = "cmd.exe",
                        WorkingDirectory = MainWindow.MKVToolNixPath,
                        Arguments = "/C mkvmerge.exe --output " + '\u0022' + MainWindow.VideoOutput + '\u0022' + " " + MainWindow.VFRCMD + " --language 0:und --default-track 0:yes " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "temp.mkv") + '\u0022' + " --default-track 0:yes " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Audio", "audio.mkv") + '\u0022'
                    };
                    SmallFunctions.Logging("Muxing: " + startInfo.Arguments);
                    mkvToolNix.StartInfo = startInfo;
                    mkvToolNix.Start();
                    mkvToolNix.WaitForExit();
                }
                else
                {
                    // Muxes Video & Audio & Subtitles together
                    // Run mkvmerge command
                    Process mkvToolNix = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = "cmd.exe",
                        WorkingDirectory = MainWindow.MKVToolNixPath,
                        Arguments = "/C mkvmerge.exe --output " + '\u0022' + MainWindow.VideoOutput + '\u0022' + " " + MainWindow.VFRCMD + " --language 0:und --default-track 0:yes " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "temp.mkv") + '\u0022' + " --default-track 0:yes " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Audio", "audio.mkv") + '\u0022' + " " + MainWindow.subCommand
                    };
                    SmallFunctions.Logging("Muxing: " + startInfo.Arguments);
                    mkvToolNix.StartInfo = startInfo;
                    mkvToolNix.Start();
                    mkvToolNix.WaitForExit();
                }
            }
            else
            {
                // ═════════════════════════════════ Muxing without Audio ════════════════════════════════
                // Video Concat
                if (MainWindow.subSoftSubEnabled != true)
                {
                    // Only Video Output
                    if (MainWindow.VFRVideo == false)
                    {
                        ffmpegCommand = "/C ffmpeg.exe -y -f concat -safe 0 -i " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Chunks", "chunks.txt") + '\u0022' + " -c copy " + '\u0022' + MainWindow.VideoOutput + '\u0022';
                        await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));
                    }
                    else
                    {
                        // First Concats the video to a temp.mkv file
                        ffmpegCommand = "/C ffmpeg.exe -y -f concat -safe 0 -i " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Chunks", "chunks.txt") + '\u0022' + " -c copy " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "temp.mkv") + '\u0022';
                        await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));

                        // Run mkvmerge command with VFR Support
                        Process mkvToolNix = new Process();
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            WindowStyle = ProcessWindowStyle.Hidden,
                            FileName = "cmd.exe",
                            WorkingDirectory = MainWindow.MKVToolNixPath,
                            Arguments = "/C mkvmerge.exe --output " + '\u0022' + MainWindow.VideoOutput + '\u0022' + " " + MainWindow.VFRCMD + " --language 0:und --default-track 0:yes " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "temp.mkv") + '\u0022'
                        };
                        SmallFunctions.Logging("Muxing: " + startInfo.Arguments);
                        mkvToolNix.StartInfo = startInfo;
                        mkvToolNix.Start();
                        mkvToolNix.WaitForExit();
                    }
                }
                else
                {
                    // First Concats the video to a temp.mkv file
                    ffmpegCommand = "/C ffmpeg.exe -y -f concat -safe 0 -i " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Chunks", "chunks.txt") + '\u0022' + " -c copy " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "temp.mkv") + '\u0022';
                    await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));

                    // Muxes Video & Subtitles together
                    // Run mkvmerge command
                    Process mkvToolNix = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = "cmd.exe",
                        WorkingDirectory = MainWindow.MKVToolNixPath,
                        Arguments = "/C mkvmerge.exe --output " + '\u0022' + MainWindow.VideoOutput + '\u0022' + " " + MainWindow.VFRCMD + " --language 0:und --default-track 0:yes " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "temp.mkv") + '\u0022' + " " + MainWindow.subCommand
                    };
                    SmallFunctions.Logging("Muxing: " + startInfo.Arguments);
                    mkvToolNix.StartInfo = startInfo;
                    mkvToolNix.Start();
                    mkvToolNix.WaitForExit();
                }

            }
        }
    }
}
