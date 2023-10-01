using NotEnoughAV1Encodes.Queue;
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
    class VideoEncode
    {
        private static bool FileAlreadyEncoded(int _index, string _uniqueIdentifier)
        {
            if (! File.Exists(Path.Combine(Global.Temp, "NEAV1E", _uniqueIdentifier, "Video", _index.ToString("D6") + "_finished.log"))) 
                return false;

            // Bad Implementation, however it's KISS
            if (File.Exists(Path.Combine(Global.Temp, "NEAV1E", _uniqueIdentifier, "Video", _index.ToString("D6") + ".webm")) == false &&
                File.Exists(Path.Combine(Global.Temp, "NEAV1E", _uniqueIdentifier, "Video", _index.ToString("D6") + ".ivf")) == false &&
                File.Exists(Path.Combine(Global.Temp, "NEAV1E", _uniqueIdentifier, "Video", _index.ToString("D6") + ".mp4")) == false)
            {
                return false;
            }

            return true;
        }

        private static string SetChunkInput(QueueElement _queueElement, bool _queueParallel, string _chunk, string _ChunkHardsubInput, Settings _settings)
        {
            if (_queueElement.ChunkingMethod == 0 || _queueElement.ChunkingMethod == 2 || _queueParallel || _queueElement.Preset.TargetVMAF)
            {
                // Input for Chunked Encoding or Parallel Queue Processing
                return "-i \"" + _chunk + "\"";
            }

            // Input for Scenebased Encoding (supports picture based hardsubbing)
            if (_settings.UseInputSeeking)
            {
                return _chunk.Split("-t")[0] + " -i \"" + _queueElement.VideoDB.InputPath + "\" " + "-t " + _chunk.Split("-t")[1] + " " + _ChunkHardsubInput;
            }
 
            return "-i \"" + _queueElement.VideoDB.InputPath + "\" " + _ChunkHardsubInput + _chunk;
        }

        private static string SetChunkOutput(QueueElement queueElement, int index)
        {
            string passesSettings = "";
            if (queueElement.Passes == 2)
            {
                string NULoutput = "";
                if (queueElement.EncodingMethod < 4)
                {
                    passesSettings = " -pass 1 -passlogfile ";
                    NULoutput = " -f webm NUL";
                } else {
                    if (queueElement.EncodingMethod == (int)Encoder.AOMENC) { passesSettings = " --passes=2 --pass=1 --fpf="; NULoutput = " --output=NUL"; }
                    if (queueElement.EncodingMethod == (int)Encoder.SVTAV1) { passesSettings = " --pass 1 --stats "; NULoutput = " --output NUL"; }
                    if (queueElement.EncodingMethod == (int)Encoder.X264) { passesSettings = " -pass 1 -passlogfile "; NULoutput = " -f mp4 NUL"; }
                }

                return passesSettings + "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + "_stats.log") + "\"" + NULoutput;
            }

            if (queueElement.EncodingMethod > 4)
            {
                if (queueElement.EncodingMethod == (int)Encoder.AOMENC) { passesSettings = " --passes=1 --output="; }
                if (queueElement.EncodingMethod == (int)Encoder.RAV1E) { passesSettings = " --output "; }
                if (queueElement.EncodingMethod == (int)Encoder.SVTAV1) { passesSettings = " --passes 1 --output "; }
                return passesSettings + "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + ".ivf") + "\"";
            }

            if (queueElement.EncodingMethod is (int)Encoder.X265 or (int)Encoder.X264)
            {
                return "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + ".mp4") + "\"";
            }

            if (queueElement.EncodingMethod is (int)Encoder.QSVAV1 or (int)Encoder.NVENCAV1)
            {
                return " -o \"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + ".webm") + "\"";
            }

            return "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + ".webm") + "\"";
        }

        private static string SetChunkOutputSecondPass(QueueElement queueElement, int index)
        {
            if (queueElement.EncodingMethod < 4)
            {
                return " -pass 2 -passlogfile " + "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + "_stats.log") + "\" "
                     + "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + ".webm") + "\"";
            }

            if (queueElement.EncodingMethod == (int)Encoder.X264)
            {
                return " -pass 2 -passlogfile " + "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + "_stats.log") + "\" "
                     + "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + ".mp4") + "\"";
            }

            string passesSettings = "";
            if (queueElement.EncodingMethod == (int)Encoder.AOMENC) { passesSettings = " --passes=2 --pass=2 --fpf=" + "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + "_stats.log") + "\" --output="; }
            if (queueElement.EncodingMethod == (int)Encoder.SVTAV1) { passesSettings = " --pass 2 --stats " + "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + "_stats.log") + "\" --output "; }

            return passesSettings + "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + ".ivf") + "\"";
        }

        public void Encode(int _workerCount, List<string> VideoChunks, QueueElement queueElement, bool queueParallel, bool normalPriority, Settings settings, CancellationToken token)
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

                        if (FileAlreadyEncoded(index, queueElement.UniqueIdentifier))
                        {
                            // Add Queue Progress Element to ChunkProgress List of already encoded chunks
                            try
                            {
                                int progress = int.Parse(File.ReadLines(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + "_finished.log")).First());
                                ChunkProgress chunkProgressTmp = new()
                                {
                                    ChunkName = chunk,
                                    Progress = progress
                                };

                                if (queueElement.Passes == 2)
                                {
                                    chunkProgressTmp.ProgressSecondPass = progress;
                                }

                                List<ChunkProgress> tempListTmp = queueElement.ChunkProgress.ToList();
                                if (!tempListTmp.Any(n => n.ChunkName == chunk))
                                {
                                    queueElement.ChunkProgress.Add(chunkProgressTmp);
                                }
                            }
                            catch { }

                            // Skip Chunk if already encoded
                            return;
                        }

                        Directory.CreateDirectory(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video"));

                        string ChunkInput = "";
                        string ChunkHardsubInput = "";
                        string ChunkOutput = "";
                        string ffmpegFilter = "";
                        int finalProgress = 0;

                        // Apply filter if the video has not been processed before
                        if (queueElement.ChunkingMethod == 1 || queueElement.ChunkingMethod == 2 || (queueElement.ChunkingMethod == 0 && queueElement.ReencodeMethod == 3))
                        {
                            ffmpegFilter = queueElement.FilterCommand;
                        }

                        // Subtitle Hardsubbing is only possible with Scenebased Encoding at this stage
                        if ((queueElement.ChunkingMethod == 1 || queueElement.ChunkingMethod == 2) && queueElement.SubtitleBurnCommand != null)
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
                            else if (!queueElement.SubtitleBurnCommand.Contains("-filter_complex"))
                            {
                                // Don't want to mix filter_complex with vf
                                // Prevents using "-vf" two times
                                ffmpegFilter += queueElement.SubtitleBurnCommand.Remove(0, 5);
                            }
                        }

                        // Set Chunk Input
                        ChunkInput = SetChunkInput(queueElement, queueParallel, chunk, ChunkHardsubInput, settings);

                        // Set Chunk Output
                        ChunkOutput = SetChunkOutput(queueElement, index);

                        string videoCommand = queueElement.VideoCommand;

                        // Target VMAF
                        if (queueElement.Preset.TargetVMAF && queueElement.EncodingMethod is (int)Encoder.AOMFFMPEG)
                        {
                            VMAF vmaf = new();
                            string calculatedQ = vmaf.Probe(queueElement, chunk, index, ffmpegFilter, settings, token);
                            videoCommand = videoCommand.Replace("{q_vmaf}", calculatedQ);
                        }

                        Process processVideo = new();
                        ProcessStartInfo startInfo = new()
                        {
                            WindowStyle = ProcessWindowStyle.Hidden,
                            FileName = "cmd.exe",
                            WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "FFmpeg"),
                            Arguments = "/C ffmpeg.exe -y " + ChunkInput + " " + ffmpegFilter + " -an -sn -map_metadata -1 " + videoCommand + " " + ChunkOutput,
                            RedirectStandardError = true,
                            RedirectStandardInput = true,
                            CreateNoWindow = true
                        };

                        string debugCommand = "/C ffmpeg.exe -y " + ChunkInput + " " + ffmpegFilter + " -an -sn -map_metadata -1 " + videoCommand + " " + ChunkOutput;
                        Global.Logger("INFO  - VideoEncode.Encode() => Command: " + debugCommand, queueElement.Output + ".log");

                        processVideo.StartInfo = startInfo;

                        token.Register(() => { try { processVideo.StandardInput.Write("q"); } catch { } });

                        // We want to use the event handler, this might / might not fix some blocking issues resulting in lost frames
                        // https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.outputdatareceived?view=netframework-4.7.2
                        // https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.errordatareceived?view=netframework-4.7.2
                        // Read stderr to get progress
                        processVideo.ErrorDataReceived += (s, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                            {

                                int processedFrames = Global.GetTotalFramesProcessed(e.Data);
                                if (processedFrames != 0)
                                {
                                    foreach (ChunkProgress progressElement in queueElement.ChunkProgress.Where(p => p.ChunkName == chunk))
                                    {
                                        progressElement.Progress = processedFrames;
                                        finalProgress = processedFrames;
                                    }
                                }
                            }
                        };

                        processVideo.Start();

                        // Set Proccess Priority
                        if (!normalPriority) processVideo.PriorityClass = ProcessPriorityClass.BelowNormal;

                        // Get launched Process ID
                        int _pid = processVideo.Id;

                        // Add Process ID to Array, inorder to keep track / kill the instances
                        Global.LaunchedPIDs.Add(_pid);

                        // Create Progress Object
                        ChunkProgress chunkProgress = new();
                        chunkProgress.ChunkName = chunk;
                        chunkProgress.Progress = 0;

                        List<ChunkProgress> tempList = queueElement.ChunkProgress.ToList();
                        if (!tempList.Any(n => n.ChunkName == chunk))
                        {
                            queueElement.ChunkProgress.Add(chunkProgress);
                        }

                        processVideo.BeginErrorReadLine();

                        processVideo.WaitForExit();

                        // Remove PID from Array after Exit
                        Global.LaunchedPIDs.RemoveAll(i => i == _pid);

                        // Second Pass
                        if (queueElement.Passes == 2 && token.IsCancellationRequested == false)
                        {
                            // Set Chunk Output
                            ChunkOutput = SetChunkOutputSecondPass(queueElement, index);

                            Process processVideo2ndPass = new();
                            startInfo = new()
                            {
                                WindowStyle = ProcessWindowStyle.Hidden,
                                FileName = "cmd.exe",
                                WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "FFmpeg"),
                                Arguments = "/C ffmpeg.exe -y " + ChunkInput + " " + ffmpegFilter + " -an -sn -map_metadata -1 " + videoCommand + " " + ChunkOutput,
                                RedirectStandardError = true,
                                RedirectStandardInput = true,
                                CreateNoWindow = true
                            };

                            string DebugCommand = "/C ffmpeg.exe -y " + ChunkInput + " " + ffmpegFilter + " -an -sn -map_metadata -1 " + videoCommand + " " + ChunkOutput;
                            Global.Logger("INFO  - VideoEncode.Encode() 2nd Pass => Command: " + DebugCommand, queueElement.Output + ".log");

                            processVideo2ndPass.StartInfo = startInfo;

                            token.Register(() => { try { processVideo2ndPass.StandardInput.Write("q"); } catch { } });

                            // Read stderr to get progress
                            processVideo2ndPass.ErrorDataReceived += (s, e) =>
                            {
                                if (!string.IsNullOrEmpty(e.Data))
                                {
                                    int processedFrames = Global.GetTotalFramesProcessed(e.Data);
                                    if (processedFrames != 0)
                                    {
                                        foreach (Queue.ChunkProgress progressElement in queueElement.ChunkProgress.Where(p => p.ChunkName == chunk))
                                        {
                                            progressElement.ProgressSecondPass = processedFrames;
                                        }
                                    }
                                }
                            };

                            processVideo2ndPass.Start();

                            // Set Proccess Priority
                            if (!normalPriority) processVideo2ndPass.PriorityClass = ProcessPriorityClass.BelowNormal;

                            // Get launched Process ID
                            _pid = processVideo2ndPass.Id;

                            // Add Process ID to Array, inorder to keep track / kill the instances
                            Global.LaunchedPIDs.Add(_pid);

                            processVideo2ndPass.BeginErrorReadLine();

                            processVideo2ndPass.WaitForExit();

                            Global.Logger("INFO  - VideoEncode.Encode() => Exit Code: " + processVideo2ndPass.ExitCode + "  Chunk: " + chunk, queueElement.Output + ".log");

                            // Remove PID from Array after Exit
                            Global.LaunchedPIDs.RemoveAll(i => i == _pid);
                        }

                        if (processVideo.ExitCode != 0 || token.IsCancellationRequested == true)
                        {
                            queueElement.Error = true;
                            queueElement.ErrorCount += 1;
                            Global.Logger("FATAL - VideoEncode.Encode() => Exit Code: " + processVideo.ExitCode + "  Chunk: " + chunk, queueElement.Output + ".log");
                            return;
                        }

                        // Save Finished Status
                        FileStream finishedLog = File.Create(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video", index.ToString("D6") + "_finished.log"));
                        var charBuffer = Encoding.UTF8.GetBytes(finalProgress.ToString());
                        finishedLog.Write(charBuffer, 0, charBuffer.Length);
                        finishedLog.Close();

                        Global.Logger("INFO  - VideoEncode.Encode() => Exit Code: 0  Chunk: " + chunk, queueElement.Output + ".log");
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
