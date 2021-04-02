using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NotEnoughAV1Encodes
{
    class VideoMuxing
    {
        public static async Task Concat()
        {
            // ══════════════════════════════════════ Chunk Parsing ══════════════════════════════════════
            // Writes all ivf files into chunks.txt for later concat
            IOrderedEnumerable<string> sorted = null;
            if (MainWindow.EncodeMethod != 3)
            {
                sorted = Directory.GetFiles(Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Chunks"), "*.ivf").OrderBy(f => f);
            }
            else
            {
                sorted = Directory.GetFiles(Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Chunks"), "*.webm").OrderBy(f => f);
            }
            
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Chunks"), "chunks.txt")))
            {
                foreach (var fileTemp in sorted)
                {
                    string tempName = fileTemp.Replace("'", "'\\''");
                    outputFile.WriteLine("file '" + tempName + "'");
                }
            }

            bool audio = MainWindow.trackOne || MainWindow.trackTwo || MainWindow.trackThree || MainWindow.trackFour;
            bool vfr = MainWindow.VFRVideo;
            bool sub = MainWindow.subSoftSubEnabled;
            string ffmpegCommand;

            // Replace ' with "'", else muxing will fail with single quotes in filename
            MainWindow.TempPathFileName.Replace("'", "\"'\"");

            if (!audio && !vfr && !sub)
            {
                ffmpegCommand = "/C ffmpeg.exe -y -f concat -safe 0 -i " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Chunks", "chunks.txt") + '\u0022' + " -c copy " + '\u0022' + MainWindow.VideoOutput + '\u0022';
                SmallFunctions.Logging("Muxing: " + ffmpegCommand);
                await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));
            }
            else
            {
                // First Concats the video to a temp.mkv file
                ffmpegCommand = "/C ffmpeg.exe -y -f concat -safe 0 -i " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Chunks", "chunks.txt") + '\u0022' + " -c copy " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "temp.mkv") + '\u0022';
                SmallFunctions.Logging("Muxing: " + ffmpegCommand);
                await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));
            }


            if (audio)
            {
                if (!sub)
                {
                    if (!vfr)
                    {
                        // Muxes Video & Audio together (required for MP4 output)
                        ffmpegCommand = "/C ffmpeg.exe -y -i " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "temp.mkv") + '\u0022' + " -i " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Audio", "audio.mkv") + '\u0022' + " -map 0:v -map 1:a -c copy " + '\u0022' + MainWindow.VideoOutput + '\u0022';
                        SmallFunctions.Logging("Muxing: " + ffmpegCommand);
                        await Task.Run(() => SmallFunctions.ExecuteFfmpegTask(ffmpegCommand));
                    }
                    else
                    {
                        // Run mkvmerge command - only supports mkv / webm
                        string mkvmergeCommand = "/C mkvmerge.exe --output " + '\u0022' + MainWindow.VideoOutput + '\u0022' + " " + MainWindow.VFRCMD + " --language 0:und --default-track 0:yes " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "temp.mkv") + '\u0022' + " --default-track 0:yes " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Audio", "audio.mkv") + '\u0022';
                        SmallFunctions.Logging("Muxing: " + mkvmergeCommand);
                        await Task.Run(() => SmallFunctions.ExecuteMKVMergeTask(mkvmergeCommand));
                    }
                }
                else
                {
                    // Muxes Video & Audio & Subtitles together - MP4 not supported - also supports VFR
                    string mkvmergeCommand = "/C mkvmerge.exe --output " + '\u0022' + MainWindow.VideoOutput + '\u0022' + " " + MainWindow.VFRCMD + " --language 0:und --default-track 0:yes " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "temp.mkv") + '\u0022' + " --default-track 0:yes " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Audio", "audio.mkv") + '\u0022' + " " + MainWindow.subCommand;
                    SmallFunctions.Logging("Muxing: " + mkvmergeCommand);
                    await Task.Run(() => SmallFunctions.ExecuteMKVMergeTask(mkvmergeCommand));
                }
            }
            else if (!audio)
            {
                if (!sub)
                {
                    if (vfr)
                    {
                        // Run mkvmerge command with VFR Support
                        string mkvmergeCommand = "/C mkvmerge.exe --output " + '\u0022' + MainWindow.VideoOutput + '\u0022' + " " + MainWindow.VFRCMD + " --language 0:und --default-track 0:yes " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "temp.mkv") + '\u0022';
                        SmallFunctions.Logging("Muxing: " + mkvmergeCommand);
                        await Task.Run(() => SmallFunctions.ExecuteMKVMergeTask(mkvmergeCommand));
                    }
                }
                else
                {
                    // Muxes Video & Subtitles together
                    string mkvmergeCommand = "/C mkvmerge.exe --output " + '\u0022' + MainWindow.VideoOutput + '\u0022' + " " + MainWindow.VFRCMD + " --language 0:und --default-track 0:yes " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "temp.mkv") + '\u0022' + " " + MainWindow.subCommand;
                    SmallFunctions.Logging("Muxing: " + mkvmergeCommand);
                    await Task.Run(() => SmallFunctions.ExecuteMKVMergeTask(mkvmergeCommand));
                }
            }
        }
    }
}
