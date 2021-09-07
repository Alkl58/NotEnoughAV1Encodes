using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NotEnoughAV1Encodes.Video
{
    class VideoEncodePipe
    {
        public void Encode(int _workerCount, List<string> VideoChunks, Queue.QueueElement queueElement)
        {
            using SemaphoreSlim concurrencySemaphoreInner = new(_workerCount);
            // Creates a tasks list
            List<Task> tasksInner = new();

            foreach (string chunk in VideoChunks)
            {
                Debug.WriteLine("Video: " + chunk);
                concurrencySemaphoreInner.Wait();
                Task taskInner = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Directory.CreateDirectory(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video"));
                        Directory.CreateDirectory(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Progress"));

                        if (!File.Exists(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", Path.GetFileNameWithoutExtension(chunk) + "_finished.log")))
                        {
                            Process processVideo = new();
                            ProcessStartInfo startInfo = new()
                            {
                                WindowStyle = ProcessWindowStyle.Hidden,
                                FileName = "cmd.exe",
                                WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "FFmpeg"),
                                Arguments = "/C ffmpeg.exe -i \"" + chunk + "\" -an -sn -map_metadata -1 -c:v libx265 -crf 10 \"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", Path.GetFileNameWithoutExtension(chunk) + ".mkv") + "\"",
                                RedirectStandardError = true,
                                CreateNoWindow = true
                            };

                            processVideo.StartInfo = startInfo;
                            processVideo.Start();

                            StreamReader sr = processVideo.StandardError;

                            while (!sr.EndOfStream)
                            {
                                int processedFrames = GetTotalFramesProcessed(sr.ReadLine());
                                if (processedFrames != 0)
                                {
                                    File.WriteAllText(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Progress", Path.GetFileNameWithoutExtension(chunk) + ".log"), processedFrames.ToString());
                                }
                            }

                            processVideo.WaitForExit();

                            if (processVideo.ExitCode == 0)
                            {
                                File.Create(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", Path.GetFileNameWithoutExtension(chunk) + "_finished.log"));
                            }
                        }
                    }
                    finally
                    {
                        concurrencySemaphoreInner.Release();
                    }

                });
                tasksInner.Add(taskInner);
            }
            Task.WaitAll(tasksInner.ToArray());
        }

        private static int GetTotalFramesProcessed(string stderr)
        {
            try
            {
                int Start, End;
                Start = stderr.IndexOf("frame=", 0) + "frame=".Length;
                End = stderr.IndexOf("fps=", Start);
                return int.Parse(stderr.Substring(Start, End - Start));
            }
            catch
            {
                return 0;
            }
        }
    }
}
