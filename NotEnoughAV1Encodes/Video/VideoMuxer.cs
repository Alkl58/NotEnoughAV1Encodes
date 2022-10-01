using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace NotEnoughAV1Encodes.Video
{
    class VideoMuxer
    {
        public void Concat(Queue.QueueElement queueElement)
        {
            Global.Logger("DEBUG - VideoMuxer.Concat()", queueElement.Output + ".log");
            queueElement.Progress = 0.0;
            queueElement.Status = "Muxing files. Please wait.";

            // Ordered Chunk List
            IOrderedEnumerable<string> sortedChunks = GetSortedVideoChunks(queueElement);

            // Write Chunks to file for muxing
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "chunks.txt")))
            {
                int counter = 0;
                Global.Logger("DEBUG - VideoMuxer.Concat() => Writing chunks.txt ...", queueElement.Output + ".log");
                foreach (string fileTemp in sortedChunks)
                {
                    string tempName = fileTemp.Replace("'", "'\\''");
                    outputFile.WriteLine("file '" + tempName + "'");
                    Global.Logger("TRACE - VideoMuxer.Concat() => Wrote " + counter.ToString() + " => " + tempName + " Chunk to chunks.txt", queueElement.Output + ".log");
                    counter++;
                }
                Global.Logger("DEBUG - VideoMuxer.Concat() => Wrote " + counter.ToString() + " Chunk(s) to chunks.txt", queueElement.Output + ".log");
            }

            // Mux Chunks
            MuxFFmpegChunks(queueElement);

            bool MuxWithMKVMerge = false;
            bool MuxWithFFmpeg = true;
            string audioFFmpegMapping = "";
            string audioMuxCommand = "";
            string subsMuxCommand = "";
            string vfrMuxCommand = "";

            if (queueElement.AudioCommand != null)
            {
                if (Path.GetExtension(queueElement.VideoDB.OutputPath).ToLower() == ".mp4" && queueElement.VFR == false && queueElement.SubtitleCommand == null)
                {
                    audioMuxCommand = " -i \"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Audio", "audio.mkv") + "\"";
                    audioFFmpegMapping = " -map 1:a";
                    MuxWithFFmpeg = true;
                    MuxWithMKVMerge = false;
                }
                else
                {
                    audioMuxCommand = "--default-track 0:yes \"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Audio", "audio.mkv") + "\"";
                    MuxWithMKVMerge = true;
                    MuxWithFFmpeg = false;
                }
            }
            
            if (queueElement.SubtitleCommand != null)
            {
                if (File.Exists(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Subtitles", "subs.mkv")))
                {
                    subsMuxCommand = queueElement.SubtitleCommand + " \"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Subtitles", "subs.mkv") + "\"";
                    MuxWithMKVMerge = true;
                    MuxWithFFmpeg = false;
                }
                else
                {
                    Global.Logger("ERROR - VideoMuxer.Concat() => Could not find subtitles! Skipping...", queueElement.Output + ".log");
                }
            }
            
            if (queueElement.VFR)
            {
                vfrMuxCommand = "--timestamps 0:\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "vsync.txt") + "\"";
                MuxWithMKVMerge = true;
                MuxWithFFmpeg = false;
            }

            if (! string.IsNullOrWhiteSpace(queueElement.VideoHDRMuxCommand))
            {
                MuxWithMKVMerge = true;
                MuxWithFFmpeg = false;
            }

            Global.Logger("DEBUG - VideoMuxer.Concat() => MuxWithMKVMerge? : " + MuxWithMKVMerge.ToString(), queueElement.Output + ".log");
            Global.Logger("DEBUG - VideoMuxer.Concat() => MuxWithFFmpeg?   : " + MuxWithFFmpeg.ToString(), queueElement.Output + ".log");
            Global.Logger("DEBUG - VideoMuxer.Concat() => AudioCommand?    : " + audioMuxCommand, queueElement.Output + ".log");
            Global.Logger("DEBUG - VideoMuxer.Concat() => SubsCommand?     : " + subsMuxCommand, queueElement.Output + ".log");
            Global.Logger("DEBUG - VideoMuxer.Concat() => VFRCommand?      : " + vfrMuxCommand, queueElement.Output + ".log");

            MuxFFmpeg(MuxWithFFmpeg, queueElement, audioMuxCommand, audioFFmpegMapping);

            MuxMKVMerge(MuxWithMKVMerge, queueElement, audioMuxCommand, vfrMuxCommand, subsMuxCommand, queueElement.VideoHDRMuxCommand);
        }

        private static void MuxFFmpegChunks(Queue.QueueElement queueElement)
        {
            // Setting Output for FFmpeg
            string FFmpegOutput = Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "temp_mux.mkv");
            if (queueElement.AudioCommand == null && queueElement.SubtitleCommand == null && queueElement.VFR == false && string.IsNullOrEmpty(queueElement.VideoHDRMuxCommand))
            {
                FFmpegOutput = queueElement.VideoDB.OutputPath;
            }

            string DAR = "";
            // Set Display Aspect Ratio for external encoders
            if (queueElement.EncodingMethod is 5 or 6 or 7 && !string.IsNullOrEmpty(queueElement.VideoDB.MIDisplayAspectRatio))
            {
                if (queueElement.VideoDB.MIPixelAspectRatio != "1.000")
                {
                    DAR = " -aspect " + queueElement.VideoDB.MIDisplayAspectRatio;
                }
            }

            // Muxing Chunks
            Process processVideo = new();
            ProcessStartInfo startInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "FFmpeg"),
                RedirectStandardError = true,
                Arguments = "/C ffmpeg.exe -y -f concat -safe 0 -i \"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "chunks.txt") + "\"" + DAR + " -c copy \"" + FFmpegOutput + "\"",
                CreateNoWindow = true
            };

            Global.Logger("DEBUG - VideoMuxer.Concat() => Command: ffmpeg.exe -y -f concat -safe 0 -i \"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "chunks.txt") + "\"" + DAR + " -c copy \"" + FFmpegOutput + "\"", queueElement.Output + ".log");

            processVideo.StartInfo = startInfo;
            processVideo.Start();

            StreamReader sr = processVideo.StandardError;
            while (!sr.EndOfStream)
            {
                int processedFrames = Global.GetTotalFramesProcessed(sr.ReadLine());
                if (processedFrames != 0)
                {
                    queueElement.Progress = Convert.ToDouble(processedFrames);
                    queueElement.Status = "Muxing Chunks - " + ((decimal)queueElement.Progress / queueElement.FrameCount).ToString("0.00%");
                }
            }

            processVideo.WaitForExit();
        }

        private static void MuxFFmpeg(bool mux, Queue.QueueElement queueElement, string audioMuxCommand, string audioFFmpegMapping)
        {
            if (!mux) return;

            string ffmpegCommand = "/C ffmpeg.exe -y -i \"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "temp_mux.mkv") + "\"" + audioMuxCommand + " -map 0:v" + audioFFmpegMapping + " -c copy \"" + queueElement.VideoDB.OutputPath + "\"";
            // Muxing Chunks
            Process processVideo = new();
            ProcessStartInfo startInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "FFmpeg"),
                RedirectStandardError = true,
                Arguments = ffmpegCommand,
                CreateNoWindow = true
            };

            Global.Logger("DEBUG - VideoMuxer.Concat() => Command: " + ffmpegCommand, queueElement.Output + ".log");

            processVideo.StartInfo = startInfo;
            processVideo.Start();

            StreamReader sr = processVideo.StandardError;
            while (!sr.EndOfStream)
            {
                int processedFrames = Global.GetTotalFramesProcessed(sr.ReadLine());
                if (processedFrames != 0)
                {
                    queueElement.Progress = Convert.ToDouble(processedFrames);
                    queueElement.Status = "Muxing Video - " + ((decimal)queueElement.Progress / queueElement.FrameCount).ToString("0.00%");
                }
            }

            processVideo.WaitForExit();
        }

        private static void MuxMKVMerge(bool mux, Queue.QueueElement queueElement, string audioMuxCommand, string vfrMuxCommand, string subsMuxCommand, string videoHDRCommand)
        {
            if (!mux) return;

            string webmcmd = "";
            if (Path.GetExtension(queueElement.VideoDB.OutputPath).ToLower() == ".webm")
            {
                webmcmd = " --webm ";
            }

            //Run mkvmerge command
            Process processMKVMerge = new();
            ProcessStartInfo startInfoMKVMerge = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "MKVToolNix"),
                Arguments = "/C mkvmerge.exe " + webmcmd + " --output \"" + queueElement.VideoDB.OutputPath + "\" --language 0:und --default-track 0:yes \"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "temp_mux.mkv") + "\" " + videoHDRCommand + audioMuxCommand + " " + vfrMuxCommand + " " + subsMuxCommand
            };
            processMKVMerge.StartInfo = startInfoMKVMerge;

            processMKVMerge.Start();
            string _output = processMKVMerge.StandardOutput.ReadToEnd();
            processMKVMerge.WaitForExit();

            if (processMKVMerge.ExitCode != 0)
            {
                MessageBox.Show(_output, "mkvmerge", MessageBoxButton.OK, MessageBoxImage.Warning);
                Global.Logger("FATAL - VideoMuxer.Concat() => Exit Code: " + processMKVMerge.ExitCode, queueElement.Output + ".log");
                Global.Logger("FATAL - VideoMuxer.Concat() => STDOUT: " + _output, queueElement.Output + ".log");
            }
            else
            {
                Global.Logger("DEBUG - VideoMuxer.Concat() => Exit Code: 0", queueElement.Output + ".log");
                Global.Logger("DEBUG - VideoMuxer.Concat() => STDOUT: " + _output, queueElement.Output + ".log");
            }
        }

        private static IOrderedEnumerable<string> GetSortedVideoChunks(Queue.QueueElement queueElement)
        {
            // Getting all Chunks
            IOrderedEnumerable<string> sortedChunks = null;

            // FFmpeg AOM, Rav1e, SVT-AV1, VPX-VP9
            if (queueElement.EncodingMethod is 0 or 1 or 2 or 3)
            {
                Global.Logger("DEBUG - VideoMuxer.Concat() => Reading Chunk Directory by *.webm files", queueElement.Output + ".log");
                sortedChunks = Directory.GetFiles(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video"), "*.webm").OrderBy(f => f);
            }
            
            // Aomenc, Rav1e, SVT-AV1 (External)
            if (queueElement.EncodingMethod is 5 or 6 or 7)
            {
                Global.Logger("DEBUG - VideoMuxer.Concat() => Reading Chunk Directory by *.ivf files", queueElement.Output + ".log");
                sortedChunks = Directory.GetFiles(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video"), "*.ivf").OrderBy(f => f);
            }
            
            // x265, x264
            if (queueElement.EncodingMethod is 9 or 10)
            {
                Global.Logger("DEBUG - VideoMuxer.Concat() => Reading Chunk Directory by *.mp4 files", queueElement.Output + ".log");
                sortedChunks = Directory.GetFiles(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video"), "*.mp4").OrderBy(f => f);
            }

            return sortedChunks;
        }
    }
}
