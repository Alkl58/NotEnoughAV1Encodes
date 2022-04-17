using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NotEnoughAV1Encodes.Video
{
    class VideoEncode
    {
        public void Encode(int _workerCount, List<string> VideoChunks, Queue.QueueElement queueElement, bool queueParallel, bool normalPriority, Settings settings, CancellationToken token)
        {
            Global.Logger("TRACE - VideoEncode.Encode()", queueElement.Output + ".log");
            using SemaphoreSlim concurrencySemaphoreInner = new(_workerCount);
            // Creates a tasks list
            List<Task> tasksInner = new();

            foreach (string chunk in VideoChunks)
            {
                Global.Logger("INFO  - VideoEncode.Encode() => Chunk: " + chunk, queueElement.Output + ".log");
                try { concurrencySemaphoreInner.Wait(token); }
                catch (OperationCanceledException) { }
                
                Task taskInner = Task.Run(() =>
                {
                    try
                    {
                        int index = VideoChunks.IndexOf(chunk);
                        bool alreadyEncoded = false;

                        Directory.CreateDirectory(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video"));

                        // Read Finished Status
                        List<Queue.ChunkProgress> tempList = queueElement.ChunkProgress.ToList();
                        if (tempList.Any(n => n.ChunkName == chunk && n.Finished == true))
                        {
                            alreadyEncoded = true;
                        }

                        // Checks if Output really exists
                        if (alreadyEncoded)
                        {
                            // Bad Implementation, however it's KISS
                            if (File.Exists(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + ".webm")) == false &&
                                File.Exists(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + ".ivf")) == false && 
                                File.Exists(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + ".mp4")) == false )
                            {
                                alreadyEncoded = false;
                            }
                        }

                        // Skip Chunk if already encoded (finished.log)
                        if (!alreadyEncoded)
                        {
                            string ChunkInput = "";
                            string ChunkHardsubInput = "";
                            string ChunkOutput = "";
                            string passesSettings = "";
                            string ffmpegFilter = "";

                            // Apply filter if the video has not been processed before
                            if (queueElement.ChunkingMethod == 1 || (queueElement.ChunkingMethod == 0 && queueElement.ReencodeMethod == 3))
                            {
                                ffmpegFilter = queueElement.FilterCommand;
                            }

                            // Subtitle Hardsubbing is only possible with Scenebased Encoding at this stage
                            if (queueElement.ChunkingMethod == 1 && queueElement.SubtitleBurnCommand != null)
                            {
                                // Only allow Picture Based Burn in, if no other Filter is being used
                                if (string.IsNullOrEmpty(queueElement.FilterCommand))
                                {
                                    ffmpegFilter += queueElement.SubtitleBurnCommand;

                                    if (queueElement.SubtitleBurnCommand.Contains("-filter_complex"))
                                    {
                                        ChunkHardsubInput = "-i \"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Subtitles", "subs.mkv") + "\" ";
                                    }
                                }
                                else if(!queueElement.SubtitleBurnCommand.Contains("-filter_complex"))
                                {
                                    // Don't want to mix filter_complex with vf
                                    // Prevents using "-vf" two times
                                    ffmpegFilter += queueElement.SubtitleBurnCommand.Remove(0, 5);
                                }
                            }

                            // Set Chunk Input
                            if (queueElement.ChunkingMethod == 0 || queueParallel)
                            {
                                // Input for Chunked Encoding or Parallel Queue Processing
                                ChunkInput = "-i \"" + chunk + "\"";
                            }
                            else
                            {
                                // Input for Scenebased Encoding (supports picture based hardsubbing)
                                if (settings.UseInputSeeking)
                                {
                                    ChunkInput = chunk + " -i \"" + queueElement.VideoDB.InputPath + "\" " + ChunkHardsubInput;
                                }
                                else
                                {
                                    ChunkInput = "-i \"" + queueElement.VideoDB.InputPath + "\" " + ChunkHardsubInput + chunk;
                                }
                            }

                            // Set Chunk Output
                            if (queueElement.Passes == 1)
                            {
                                ChunkOutput = "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + ".webm") + "\"";

                                if (queueElement.EncodingMethod > 4)
                                {
                                    if (queueElement.EncodingMethod is 5) { passesSettings = " --passes=1 --output="; }
                                    if (queueElement.EncodingMethod is 6) { passesSettings = " --output "; }
                                    if (queueElement.EncodingMethod is 7) { passesSettings = " --passes 1 --output "; }
                                    ChunkOutput = passesSettings + "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + ".ivf") + "\"";
                                }
                                if (queueElement.EncodingMethod is 9 or 10)
                                {
                                    ChunkOutput = "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + ".mp4") + "\"";
                                }
                            }
                            else if (queueElement.Passes == 2)
                            {
                                string _NULoutput = "";
                                if (queueElement.EncodingMethod < 4)
                                {
                                    passesSettings = " -pass 1 -passlogfile ";
                                    _NULoutput = " -f webm NUL";
                                }
                                else if(queueElement.EncodingMethod > 4)
                                {
                                    if (queueElement.EncodingMethod == 5) { passesSettings = " --passes=2 --pass=1 --fpf="; _NULoutput = " --output=NUL"; }
                                    if (queueElement.EncodingMethod == 7) { passesSettings = " --pass 1 --stats "; _NULoutput = " --output NUL"; }
                                    if (queueElement.EncodingMethod == 10)
                                    {
                                        passesSettings = " -pass 1 -passlogfile ";
                                        _NULoutput = " -f mp4 NUL";
                                    }
                                }

                                ChunkOutput = passesSettings + "\"" +  Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + "_stats.log") + "\"" + _NULoutput;
                            }


                            Process processVideo = new();
                            ProcessStartInfo startInfo = new()
                            {
                                WindowStyle = ProcessWindowStyle.Hidden,
                                FileName = "cmd.exe",
                                WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "FFmpeg"),
                                Arguments = "/C ffmpeg.exe -y " + ChunkInput + " " + ffmpegFilter + " -an -sn -map_metadata -1 " + queueElement.VideoCommand + " " + ChunkOutput,
                                RedirectStandardError = true,
                                RedirectStandardInput = true,
                                CreateNoWindow = true
                            };

                            string debugCommand = "/C ffmpeg.exe -y " + ChunkInput + " " + ffmpegFilter + " -an -sn -map_metadata -1 " + queueElement.VideoCommand + " " + ChunkOutput;
                            Global.Logger("INFO  - VideoEncode.Encode() => Command: " + debugCommand, queueElement.Output + ".log");

                            processVideo.StartInfo = startInfo;

                            token.Register(() => { try { processVideo.StandardInput.Write("q"); } catch { } });

                            processVideo.Start();

                            // Set Proccess Priority
                            if(!normalPriority) processVideo.PriorityClass = ProcessPriorityClass.BelowNormal;

                            // Get launched Process ID
                            int _pid = processVideo.Id;

                            // Add Process ID to Array, inorder to keep track / kill the instances
                            Global.LaunchedPIDs.Add(_pid);
                            Global.Logger("TRACE - VideoEncode.Encode() => Added PID: " + _pid + "  Chunk: " + chunk, queueElement.Output + ".log");

                            // Create Progress Object if does not exist
                            if (!tempList.Any(n => n.ChunkName == chunk))
                            {
                                Queue.ChunkProgress chunkProgress = new();
                                chunkProgress.ChunkName = chunk;
                                queueElement.ChunkProgress.Add(chunkProgress);
                            }

                            StreamReader sr = processVideo.StandardError;

                            while (!sr.EndOfStream)
                            {
                                int processedFrames = Global.GetTotalFramesProcessed(sr.ReadLine());
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
                            Global.Logger("TRACE - VideoEncode.Encode() => Removed PID: " + _pid + "  Chunk: " + chunk, queueElement.Output + ".log");

                            // Second Pass
                            if (queueElement.Passes == 2 && token.IsCancellationRequested == false)
                            {
                                if (queueElement.EncodingMethod < 4)
                                {
                                    passesSettings = " -pass 2 -passlogfile " + "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + "_stats.log") + "\" ";
                                    ChunkOutput = passesSettings + "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + ".webm") + "\"";
                                }
                                else if (queueElement.EncodingMethod > 4)
                                {
                                    if (queueElement.EncodingMethod == 5) { passesSettings = " --passes=2 --pass=2 --fpf=" + "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + "_stats.log") + "\" --output="; }
                                    if (queueElement.EncodingMethod == 7) { passesSettings = " --pass 2 --stats " + "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + "_stats.log") + "\" --output "; }

                                    ChunkOutput = passesSettings + "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + ".ivf") + "\"";

                                    if (queueElement.EncodingMethod == 10)
                                    {
                                        passesSettings = " -pass 2 -passlogfile " + "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + "_stats.log") + "\" ";
                                        ChunkOutput = passesSettings + "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + ".mp4") + "\"";
                                    }                 
                                }

                                Process processVideo2ndPass = new();
                                startInfo = new()
                                {
                                    WindowStyle = ProcessWindowStyle.Hidden,
                                    FileName = "cmd.exe",
                                    WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "FFmpeg"),
                                    Arguments = "/C ffmpeg.exe -y " + ChunkInput + " " + ffmpegFilter + " -an -sn -map_metadata -1 " + queueElement.VideoCommand + " " + ChunkOutput,
                                    RedirectStandardError = true,
                                    RedirectStandardInput = true,
                                    CreateNoWindow = true
                                };

                                string DebugCommand = "/C ffmpeg.exe -y " + ChunkInput + " " + ffmpegFilter + " -an -sn -map_metadata -1 " + queueElement.VideoCommand + " " + ChunkOutput;
                                Global.Logger("INFO  - VideoEncode.Encode() 2nd Pass => Command: " + DebugCommand, queueElement.Output + ".log");

                                processVideo2ndPass.StartInfo = startInfo;

                                token.Register(() => { try { processVideo2ndPass.StandardInput.Write("q"); } catch { } });

                                processVideo2ndPass.Start();

                                // Set Proccess Priority
                                if (!normalPriority) processVideo2ndPass.PriorityClass = ProcessPriorityClass.BelowNormal;

                                // Get launched Process ID
                                _pid = processVideo2ndPass.Id;

                                // Add Process ID to Array, inorder to keep track / kill the instances
                                Global.LaunchedPIDs.Add(_pid);
                                Global.Logger("TRACE - VideoEncode.Encode() 2nd Pass => Added PID: " + _pid + "  Chunk: " + chunk, queueElement.Output + ".log");

                                sr = processVideo2ndPass.StandardError;

                                while (!sr.EndOfStream)
                                {
                                    int processedFrames = Global.GetTotalFramesProcessed(sr.ReadLine());
                                    if (processedFrames != 0)
                                    {
                                        foreach (Queue.ChunkProgress progressElement in queueElement.ChunkProgress.Where(p => p.ChunkName == chunk))
                                        {
                                            progressElement.ProgressSecondPass = processedFrames;
                                        }
                                    }
                                }

                                processVideo2ndPass.WaitForExit();

                                Global.Logger("INFO  - VideoEncode.Encode() => Exit Code: " + processVideo2ndPass.ExitCode + "  Chunk: " + chunk, queueElement.Output + ".log");

                                // Remove PID from Array after Exit
                                Global.LaunchedPIDs.RemoveAll(i => i == _pid);
                                Global.Logger("TRACE - VideoEncode.Encode() 2nd Pass => Removed PID: " + _pid + "  Chunk: " + chunk, queueElement.Output + ".log");
                            }

                            if (processVideo.ExitCode == 0 && token.IsCancellationRequested == false)
                            {
                                // Save Finished Status
                                foreach (Queue.ChunkProgress progressElement in queueElement.ChunkProgress.Where(p => p.ChunkName == chunk))
                                {
                                    if (File.Exists(ChunkOutput))
                                    {
                                        progressElement.Finished = true;
                                    }
                                    else
                                    {
                                        Global.Logger("FATAL ?! - VideoEncode.Encode() => Exit Code: 0 - This shouldn't have happened at all. Chunk: " + chunk, queueElement.Output + ".log");
                                    }
                                }

                                Global.Logger("INFO  - VideoEncode.Encode() => Exit Code: 0  Chunk: " + chunk, queueElement.Output + ".log");
                            }
                            else
                            {
                                Global.Logger("FATAL - VideoEncode.Encode() => Exit Code: " + processVideo.ExitCode + "  Chunk: " + chunk, queueElement.Output + ".log");
                            }
                        }
                    }
                    finally
                    {
                        try
                        {
                            concurrencySemaphoreInner.Release();
                        }
                        catch { }
                    }

                }, token);
                tasksInner.Add(taskInner);
            }

            try
            {
                Task.WaitAll(tasksInner.ToArray(), token);
            }
            catch (OperationCanceledException) { }
        }
    }
}
