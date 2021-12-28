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

            // Getting all Chunks
            IOrderedEnumerable<string> sortedChunks = null;

            if(queueElement.EncodingMethod <= 4)
            {
                Global.Logger("DEBUG - VideoMuxer.Concat() => Reading Chunk Directory by *.webm files", queueElement.Output + ".log");
                sortedChunks = Directory.GetFiles(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video"), "*.webm").OrderBy(f => f);
            }
            else if (queueElement.EncodingMethod > 4 && queueElement.EncodingMethod < 9)
            {
                Global.Logger("DEBUG - VideoMuxer.Concat() => Reading Chunk Directory by *.ivf files", queueElement.Output + ".log");
                sortedChunks = Directory.GetFiles(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video"), "*.ivf").OrderBy(f => f);
            }
            else if (queueElement.EncodingMethod is 9 or 10)
            {
                Global.Logger("DEBUG - VideoMuxer.Concat() => Reading Chunk Directory by *.mp4 files", queueElement.Output + ".log");
                sortedChunks = Directory.GetFiles(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video"), "*.mp4").OrderBy(f => f);
            }

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

            // Setting Output for FFmpeg
            string FFmpegOutput = Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "temp_mux.mkv");
            if (queueElement.AudioCommand == null && queueElement.SubtitleCommand == null && queueElement.VFR == false)
            {
                FFmpegOutput = queueElement.VideoDB.OutputPath;
            }

            // Muxing Chunks
            Process processVideo = new();
            ProcessStartInfo startInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "FFmpeg"),
                RedirectStandardError = true,
                Arguments = "/C ffmpeg.exe -y -f concat -safe 0 -i \"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "chunks.txt") + "\" -c copy \"" + FFmpegOutput + "\"",
                CreateNoWindow = true
            };

            Global.Logger("DEBUG - VideoMuxer.Concat() => Command: ffmpeg.exe -y -f concat -safe 0 -i \"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "chunks.txt") + "\" -c copy \"" + FFmpegOutput + "\"" , queueElement.Output + ".log");

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
            

            bool MuxWithMKVMerge = false;

            string audioMuxCommand = "";
            if (queueElement.AudioCommand != null)
            {
                audioMuxCommand = "--default-track 0:yes \"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Audio", "audio.mkv") + "\"";
                MuxWithMKVMerge = true;
            }

            string subsMuxCommand = "";
            if (queueElement.SubtitleCommand != null)
            {
                subsMuxCommand = queueElement.SubtitleCommand + " \"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Subtitles", "subs.mkv") + "\"";
                MuxWithMKVMerge = true;
            }

            string vfrMuxCommand = "";
            if (queueElement.VFR)
            {
                vfrMuxCommand = "--timestamps 0:\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "vsync.txt") + "\"";
            }

            Global.Logger("DEBUG - VideoMuxer.Concat() => MuxWithMKVMerge? : " + MuxWithMKVMerge.ToString(), queueElement.Output + ".log");
            Global.Logger("DEBUG - VideoMuxer.Concat() => AudioCommand?    : " + audioMuxCommand, queueElement.Output + ".log");
            Global.Logger("DEBUG - VideoMuxer.Concat() => SubsCommand?     : " + subsMuxCommand, queueElement.Output + ".log");
            Global.Logger("DEBUG - VideoMuxer.Concat() => VFRCommand?      : " + vfrMuxCommand, queueElement.Output + ".log");

            if (MuxWithMKVMerge)
            {
                string _webmcmd = "";
                if (Path.GetExtension(queueElement.VideoDB.OutputPath).ToLower() == ".webm")
                {
                    _webmcmd = " --webm ";
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
                    Arguments = "/C mkvmerge.exe " + _webmcmd + " --output \"" + queueElement.VideoDB.OutputPath + "\" --language 0:und --default-track 0:yes \"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "temp_mux.mkv") + "\" " + audioMuxCommand + " " + vfrMuxCommand + " " + subsMuxCommand
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
        }
    }
}
