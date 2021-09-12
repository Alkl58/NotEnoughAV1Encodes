using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NotEnoughAV1Encodes.Video
{
    class VideoEncodePipe
    {
        public static void Encode(int _workerCount, List<string> VideoChunks, Queue.QueueElement queueElement, CancellationToken _token, bool _queueParallel)
        {
            using SemaphoreSlim concurrencySemaphoreInner = new(_workerCount);
            // Creates a tasks list
            List<Task> tasksInner = new();

            foreach (string chunk in VideoChunks)
            {
                Debug.WriteLine("Video: " + chunk);
                concurrencySemaphoreInner.Wait(_token);
                Task taskInner = Task.Run(() =>
                {
                    try
                    {
                        Directory.CreateDirectory(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video"));

                        if (!File.Exists(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", Path.GetFileNameWithoutExtension(chunk) + "_finished.log")))
                        {
                            string ChunkInput = queueElement.ChunkingMethod == 0 || _queueParallel ? " \"" + chunk + "\"" : " \"" + queueElement.Input + "\" " + chunk;

                            Process processVideo = new();
                            ProcessStartInfo startInfo = new()
                            {
                                WindowStyle = ProcessWindowStyle.Hidden,
                                FileName = "cmd.exe",
                                WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "FFmpeg"),
                                Arguments = "/C ffmpeg.exe -y -i " + ChunkInput + " -an -sn -map_metadata -1 -c:v libvpx-vp9 -crf 10 \"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", Path.GetFileNameWithoutExtension(chunk) + ".webm") + "\"",
                                RedirectStandardError = true,
                                RedirectStandardInput = true,
                                CreateNoWindow = true
                            };

                            processVideo.StartInfo = startInfo;

                            _token.Register(() => { try { processVideo.StandardInput.Write("q"); } catch { } });

                            processVideo.Start();

                            // Get launched Process ID
                            int _pid = processVideo.Id;

                            // Add Process ID to Array, inorder to keep track / kill the instances
                            Global.LaunchedPIDs.Add(_pid);

                            // Create Progress Object
                            Queue.ChunkProgress chunkProgress = new();
                            chunkProgress.ChunkName = chunk;
                            chunkProgress.Progress = 0;

                            if (!queueElement.ChunkProgress.Any(n => n.ChunkName == chunk))
                            {
                                queueElement.ChunkProgress.Add(chunkProgress);
                            }

                            StreamReader sr = processVideo.StandardError;

                            while (!sr.EndOfStream)
                            {
                                int processedFrames = GetTotalFramesProcessed(sr.ReadLine());
                                if (processedFrames != 0)
                                {
                                    foreach (Queue.ChunkProgress progressElement in queueElement.ChunkProgress.Where(p => p.ChunkName == chunk))
                                    {
                                        progressElement.Progress = processedFrames;
                                    }
                                }
                            }

                            processVideo.WaitForExit();

                            // Remove PID from Array after Exit
                            Global.LaunchedPIDs.RemoveAll(i => i == _pid);

                            if (processVideo.ExitCode == 0 && _token.IsCancellationRequested == false)
                            {
                                FileStream _finishedLog = File.Create(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", Path.GetFileNameWithoutExtension(chunk) + "_finished.log"));
                                _finishedLog.Close();
                            }
                        }
                    }
                    finally
                    {
                        concurrencySemaphoreInner.Release();
                    }

                }, _token);
                tasksInner.Add(taskInner);
            }
            Task.WaitAll(tasksInner.ToArray(), _token);
        }

        private static int GetTotalFramesProcessed(string stderr)
        {
            try
            {
                if (stderr.Contains("frame="))
                {
                    int Start, End;
                    Start = stderr.IndexOf("frame=", 0) + "frame=".Length;
                    End = stderr.IndexOf("fps=", Start);
                    return int.Parse(stderr[Start..End]);
                }
            }
            catch { }

            return 0;
        }
    }
}
