using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NotEnoughAV1Encodes.Video
{
    class VideoSplitter
    {
        private Queue.QueueElement queueElement = new();
        public void Split(Queue.QueueElement _queueElement, CancellationToken _token)
        {
            queueElement = _queueElement;

            if (queueElement.ChunkingMethod == 0)
            {
                // Equal Chunking
                FFmpegChunking(_token);
            }
        }

        private static int GetTotalFramesProcessed(string stderr)
        {
            try
            {
                int Start, End;
                Start = stderr.IndexOf("frame=", 0) + "frame=".Length;
                End = stderr.IndexOf("fps=", Start);
                return int.Parse(stderr[Start..End]);
            }
            catch
            {
                return 0;
            }
        }

        private void FFmpegChunking(CancellationToken _token)
        {
            // Skips Splitting of already existent
            if (!File.Exists(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "splitted.log")))
            {
                // Create Chunks Folder
                Directory.CreateDirectory(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Chunks"));

                // Generate Command
                string ffmpeg_command = "/C ffmpeg.exe";
                ffmpeg_command += " -y -i " + '\u0022' + queueElement.Input + '\u0022';
                ffmpeg_command += " -reset_timestamps 1 -map_metadata -1 -sn -an";

                if (queueElement.ChunkingMethod == 0)
                {
                    ffmpeg_command += " -c:v libx264 -preset ultrafast -crf 0";
                }
                else if(queueElement.ChunkingMethod == 1)
                {
                    ffmpeg_command += " -c:v ffv1 -level 3 -threads 4 -coder 1 -context 1 -slicecrc 0 -slices 4";
                }
                else if (queueElement.ChunkingMethod == 2)
                {
                    ffmpeg_command += " -c:v utvideo";
                }
                else if (queueElement.ChunkingMethod == 3)
                {
                    ffmpeg_command += " -c:v copy";
                }

                if (queueElement.ChunkingMethod != 3)
                {
                    ffmpeg_command += " -sc_threshold 0 -g " + queueElement.ChunkLength.ToString();
                    ffmpeg_command += " -force_key_frames " + '\u0022' + "expr:gte(t, n_forced * " + queueElement.ChunkLength.ToString() + ")" + '\u0022';
                }

                ffmpeg_command += " -segment_time " + queueElement.ChunkLength.ToString() + " -f segment " + '\u0022';
                ffmpeg_command += Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Chunks", "split%6d.mkv") + '\u0022';

                // Start Splitting
                Process chunkingProcess = new();
                ProcessStartInfo startInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "FFmpeg"),
                    Arguments = ffmpeg_command
                };
                chunkingProcess.StartInfo = startInfo;

                _token.Register(() => { try { chunkingProcess.StandardInput.Write("q"); } catch { } });

                // Start Process
                chunkingProcess.Start();

                // Get launched Process ID
                int tempPID = chunkingProcess.Id;

                // Add Process ID to Array, inorder to keep track / kill the instances
                Global.LaunchedPIDs.Add(tempPID);

                StreamReader sr = chunkingProcess.StandardError;
                while (!sr.EndOfStream)
                {
                    queueElement.Progress = Convert.ToDouble(GetTotalFramesProcessed(sr.ReadLine()));
                    queueElement.Status = "Splitting - " + ((decimal)queueElement.Progress / queueElement.FrameCount).ToString("0.00%");
                }

                // Wait for Exit
                chunkingProcess.WaitForExit();

                // Get Exit Code
                int exit_code = chunkingProcess.ExitCode;

                // Remove PID from Array after Exit
                Global.LaunchedPIDs.RemoveAll(i => i == tempPID);

                // Write Save Point
                if (chunkingProcess.ExitCode == 0 && _token.IsCancellationRequested == false)
                {
                    FileStream _finishedLog = File.Create(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "splitted.log"));
                    _finishedLog.Close();
                }
            }
        }
    }
}
