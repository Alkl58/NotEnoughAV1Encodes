﻿using System;
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
        public void Encode(int _workerCount, List<string> VideoChunks, Queue.QueueElement queueElement, CancellationToken _token, bool _queueParallel)
        {
            Global.Logger("TRACE - VideoEncode.Encode()", queueElement.Output + ".log");
            using SemaphoreSlim concurrencySemaphoreInner = new(_workerCount);
            // Creates a tasks list
            List<Task> tasksInner = new();

            foreach (string chunk in VideoChunks)
            {
                Global.Logger("INFO  - VideoEncode.Encode() => Chunk: " + chunk, queueElement.Output + ".log");
                try
                {
                    concurrencySemaphoreInner.Wait(_token);
                }
                catch (OperationCanceledException) { }
                
                Task taskInner = Task.Run(() =>
                {
                    try
                    {
                        Directory.CreateDirectory(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video"));

                        int index = VideoChunks.IndexOf(chunk);

                        if (!File.Exists(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + "_finished.log")))
                        {
                            string ChunkInput = queueElement.ChunkingMethod == 0 || _queueParallel ? " \"" + chunk + "\"" : " \"" + queueElement.VideoDB.InputPath + "\" " + chunk;

                            string ChunkOutput = "";
                            string _passSettings = "";

                            if (queueElement.Passes == 1)
                            {
                                ChunkOutput = "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + ".webm") + "\"";

                                if (queueElement.EncodingMethod > 4)
                                {
                                    if (queueElement.EncodingMethod is 5) { _passSettings = " --passes=1 --output="; }
                                    if (queueElement.EncodingMethod is 6) { _passSettings = " --output "; }
                                    if (queueElement.EncodingMethod is 7) { _passSettings = " --passes 1 --output "; }
                                    ChunkOutput = _passSettings + "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + ".ivf") + "\"";
                                }
                            }
                            else if (queueElement.Passes == 2)
                            {
                                string _NULoutput = "";
                                if (queueElement.EncodingMethod < 4)
                                {
                                    _passSettings = " -pass 1 -passlogfile ";
                                    _NULoutput = " -f webm NUL";
                                }
                                else if(queueElement.EncodingMethod > 4)
                                {
                                    if (queueElement.EncodingMethod == 5) { _passSettings = " --passes=2 --pass=1 --fpf="; _NULoutput = " --output=NUL"; }
                                    if (queueElement.EncodingMethod == 7) { _passSettings = " --pass 1 --stats "; _NULoutput = " --output NUL"; }
                                }

                                ChunkOutput = _passSettings + "\"" +  Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + "_stats.log") + "\"" + _NULoutput;
                            }

                            string _filter = "";
                            // Only apply filter if the video has not been processed before
                            if (queueElement.ChunkingMethod == 1 || (queueElement.ChunkingMethod == 0 && queueElement.ReencodeMethod == 3))
                            {
                                _filter = queueElement.FilterCommand;
                            }

                            Process processVideo = new();
                            ProcessStartInfo startInfo = new()
                            {
                                WindowStyle = ProcessWindowStyle.Hidden,
                                FileName = "cmd.exe",
                                WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "FFmpeg"),
                                Arguments = "/C ffmpeg.exe -y -i " + ChunkInput + " " + _filter + " -an -sn -map_metadata -1 " + queueElement.VideoCommand + " " + ChunkOutput,
                                RedirectStandardError = true,
                                RedirectStandardInput = true,
                                CreateNoWindow = true
                            };

                            string debugCommand = "/C ffmpeg.exe -y -i " + ChunkInput + " " + _filter + " -an -sn -map_metadata -1 " + queueElement.VideoCommand + " " + ChunkOutput;
                            Global.Logger("INFO  - VideoEncode.Encode() => Command: " + debugCommand, queueElement.Output + ".log");

                            processVideo.StartInfo = startInfo;

                            _token.Register(() => { try { processVideo.StandardInput.Write("q"); } catch { } });

                            processVideo.Start();

                            // Get launched Process ID
                            int _pid = processVideo.Id;

                            // Add Process ID to Array, inorder to keep track / kill the instances
                            Global.LaunchedPIDs.Add(_pid);
                            Global.Logger("TRACE - VideoEncode.Encode() => Added PID: " + _pid + "  Chunk: " + chunk, queueElement.Output + ".log");

                            // Create Progress Object
                            Queue.ChunkProgress chunkProgress = new();
                            chunkProgress.ChunkName = chunk;
                            chunkProgress.Progress = 0;

                            List<Queue.ChunkProgress> _tempList = queueElement.ChunkProgress;
                            if (!_tempList.Any(n => n.ChunkName == chunk))
                            {
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
                            if (queueElement.Passes == 2 && _token.IsCancellationRequested == false)
                            {
                                if (queueElement.EncodingMethod < 4)
                                {
                                    _passSettings = " -pass 2 -passlogfile " + "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + "_stats.log") + "\" ";
                                    ChunkOutput = _passSettings + "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + ".webm") + "\"";
                                }
                                else if (queueElement.EncodingMethod > 4)
                                {
                                    if (queueElement.EncodingMethod == 5) { _passSettings = " --passes=2 --pass=2 --fpf=" + "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + "_stats.log") + "\" --output="; }
                                    if (queueElement.EncodingMethod == 7) { _passSettings = " --pass 2 --stats " + "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + "_stats.log") + "\" --output "; }

                                    ChunkOutput = _passSettings + "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + ".ivf") + "\"";
                                }

                                Process processVideo2ndPass = new();
                                startInfo = new()
                                {
                                    WindowStyle = ProcessWindowStyle.Hidden,
                                    FileName = "cmd.exe",
                                    WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "FFmpeg"),
                                    Arguments = "/C ffmpeg.exe -y -i " + ChunkInput + " " + _filter + " -an -sn -map_metadata -1 " + queueElement.VideoCommand + " " + ChunkOutput,
                                    RedirectStandardError = true,
                                    RedirectStandardInput = true,
                                    CreateNoWindow = true
                                };

                                string DebugCommand = "/C ffmpeg.exe -y -i " + ChunkInput + " " + _filter + " -an -sn -map_metadata -1 " + queueElement.VideoCommand + " " + ChunkOutput;
                                Global.Logger("INFO  - VideoEncode.Encode() 2nd Pass => Command: " + DebugCommand, queueElement.Output + ".log");

                                processVideo2ndPass.StartInfo = startInfo;

                                _token.Register(() => { try { processVideo2ndPass.StandardInput.Write("q"); } catch { } });

                                processVideo2ndPass.Start();

                                // Get launched Process ID
                                _pid = processVideo2ndPass.Id;

                                // Add Process ID to Array, inorder to keep track / kill the instances
                                Global.LaunchedPIDs.Add(_pid);
                                Global.Logger("TRACE - VideoEncode.Encode() 2nd Pass => Added PID: " + _pid + "  Chunk: " + chunk, queueElement.Output + ".log");

                                // Create Progress Object
                                Queue.ChunkProgress chunkProgress2ndPass = new();
                                chunkProgress2ndPass.ChunkName = chunk + "_2ndpass";
                                chunkProgress2ndPass.Progress = 0;

                                List<Queue.ChunkProgress> _tempList2ndPass = queueElement.ChunkProgress;
                                if (!_tempList2ndPass.Any(n => n.ChunkName == chunk + "_2ndpass"))
                                {
                                    queueElement.ChunkProgress.Add(chunkProgress2ndPass);
                                }

                                sr = processVideo2ndPass.StandardError;

                                while (!sr.EndOfStream)
                                {
                                    int processedFrames = Global.GetTotalFramesProcessed(sr.ReadLine());
                                    if (processedFrames != 0)
                                    {
                                        foreach (Queue.ChunkProgress progressElement in queueElement.ChunkProgress.Where(p => p.ChunkName == chunk + "_2ndpass"))
                                        {
                                            progressElement.Progress = processedFrames;
                                        }
                                    }
                                }

                                processVideo2ndPass.WaitForExit();

                                Global.Logger("INFO  - VideoEncode.Encode() => Exit Code: " + processVideo2ndPass.ExitCode + "  Chunk: " + chunk, queueElement.Output + ".log");

                                // Remove PID from Array after Exit
                                Global.LaunchedPIDs.RemoveAll(i => i == _pid);
                                Global.Logger("TRACE - VideoEncode.Encode() 2nd Pass => Removed PID: " + _pid + "  Chunk: " + chunk, queueElement.Output + ".log");
                            }


                            if (processVideo.ExitCode == 0 && _token.IsCancellationRequested == false)
                            {
                                FileStream _finishedLog = File.Create(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + "_finished.log"));
                                _finishedLog.Close();
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
                        concurrencySemaphoreInner.Release();
                    }

                }, _token);
                tasksInner.Add(taskInner);
            }

            try
            {
                Task.WaitAll(tasksInner.ToArray(), _token);
            }
            catch (OperationCanceledException) { }
        }
    }
}
