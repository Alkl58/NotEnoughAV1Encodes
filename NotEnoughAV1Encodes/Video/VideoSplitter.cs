using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;

namespace NotEnoughAV1Encodes.Video
{
    class VideoSplitter
    {
        private Queue.QueueElement queueElement = new();
        public void Split(Queue.QueueElement _queueElement, CancellationToken token)
        {
            queueElement = _queueElement;
            Global.Logger("INFO  - VideoSplitter.Split()", queueElement.Output + ".log");

            if (queueElement.ChunkingMethod == 0)
            {
                // Equal Chunking
                FFmpegChunking(token);
            }
            else if(queueElement.ChunkingMethod == 1)
            {
                // PySceneDetect
                PySceneDetect(token);
            }
        }

        private void PySceneDetect(CancellationToken _token)
        {
            Global.Logger("DEBUG - VideoSplitter.Split() => PySceneDetect()", queueElement.Output + ".log");
            // Skip Scene Detect if the file already exist
            if (!File.Exists(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "splits.txt")))
            {
                // Detects the Scenes with PySceneDetect
                Process pySceneDetect = new();
                ProcessStartInfo startInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    FileName = "cmd.exe",
                    WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "pyscenedetect"),
                    Arguments = "/C scenedetect -i \"" + queueElement.VideoDB.InputPath + "\" -o \"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier) + '\u0022' + " detect-content -t " + queueElement.PySceneDetectThreshold.ToString() + " list-scenes"
                };
                pySceneDetect.StartInfo = startInfo;

                pySceneDetect.Start();

                _token.Register(() => { KillProcessAndChildren(pySceneDetect.Id); });

                StreamReader sr = pySceneDetect.StandardError;

                while (!sr.EndOfStream)
                {
                    try
                    {
                        queueElement.Status = "Splitting - " + sr.ReadLine();
                    }
                    catch { }
                }

                pySceneDetect.WaitForExit();

                sr.Close();

                if (!_token.IsCancellationRequested)
                {
                    PySceneDetectParse();
                }
                else
                {
                    Global.Logger("FATAL - Cancellation Requested - Currently in VideoSplitter.Split() => PySceneDetect()", queueElement.Output + ".log");
                }
            }
            else
            {
                Global.Logger("WARN  - VideoSplitter.Split() => PySceneDetect() => File already exist - Resuming?", queueElement.Output + ".log");
            }
        }

        private void PySceneDetectParse()
        {
            List<string> FFmpegArgs = new();

            using (TextFieldParser parser = new TextFieldParser(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, Path.GetFileNameWithoutExtension(queueElement.VideoDB.InputPath) + "-Scenes.csv")))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");

                // KISS
                bool firstLine = true;
                bool secondLine = true;

                while (!parser.EndOfData)
                {
                    //Process row
                    string[] fields = parser.ReadFields();

                    if (firstLine)
                    {
                        firstLine = false;
                        continue;
                    }

                    if (secondLine)
                    {
                        secondLine = false;
                        continue;
                    }

                    int counter = 0;

                    string seek = "";
                    foreach (string field in fields)
                    {
                        counter++;

                        if (counter == 4)
                        {
                            // Start Time (seconds)
                            seek = field;
                        }

                        if (counter == 10)
                        {
                            // Length (seconds)
                            FFmpegArgs.Add("-ss " + seek + " -t " + field);
                            counter = 0;
                        }
                    }
                }
            }

            // Writes splitting arguments to text file
            foreach (string lineArg in FFmpegArgs)
            {
                using (StreamWriter sw = File.AppendText(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "splits.txt")))
                {
                    sw.WriteLine(lineArg);
                    sw.Close();
                }
            }
        }

        private void FFmpegChunking(CancellationToken _token)
        {
            Global.Logger("DEBUG - VideoSplitter.Split() => FFmpegChunking()", queueElement.Output + ".log");
            // Skips Splitting of already existent
            if (!File.Exists(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "splitted.log")))
            {
                // Create Chunks Folder
                Directory.CreateDirectory(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Chunks"));
                Global.Logger("DEBUG - VideoSplitter.Split() => FFmpegChunking() => Path: " + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Chunks"), queueElement.Output + ".log");

                // Generate Command
                string ffmpegCommand = "/C ffmpeg.exe -y -i \"" + queueElement.VideoDB.InputPath + "\"";

                // Subtitle Input for Hardcoding
                if(!string.IsNullOrEmpty(queueElement.SubtitleBurnCommand))
                {
                    // Filter not compatible with filter_complex
                    if (string.IsNullOrEmpty(queueElement.FilterCommand))
                    {
                        if (queueElement.SubtitleBurnCommand.Contains("-filter_complex"))
                        {
                            ffmpegCommand += " -i \"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Subtitles", "subs.mkv") + "\"";
                        }
                    }
                }

                // Set Encoder
                string encoder = queueElement.ReencodeMethod switch
                {
                    0 => " -c:v libx264 -preset ultrafast -crf 0",
                    1 => " -c:v ffv1 -level 3 -threads 4 -coder 1 -context 1 -slicecrc 0 -slices 4",
                    2 => " -c:v utvideo",
                    3 => " -c:v copy",
                    _ => " -c:v copy"
                };

                ffmpegCommand += " -reset_timestamps 1 -map_metadata -1 -sn -an" + encoder;


                if (queueElement.ReencodeMethod != 3)
                {
                    ffmpegCommand += " -sc_threshold 0 -g " + queueElement.ChunkLength.ToString();
                    ffmpegCommand += " -force_key_frames " + '\u0022' + "expr:gte(t, n_forced * " + queueElement.ChunkLength.ToString() + ")" + '\u0022';
                    ffmpegCommand += queueElement.FilterCommand;
                    if (queueElement.SubtitleBurnCommand != null)
                    {
                        if (string.IsNullOrEmpty(queueElement.FilterCommand))
                        {
                            ffmpegCommand += queueElement.SubtitleBurnCommand;
                        }
                        else
                        {
                            // Don't want to mix filter_complex with vf
                            if (!queueElement.SubtitleBurnCommand.Contains("-filter_complex"))
                            {
                                // Prevents using "-vf" two times
                                ffmpegCommand += queueElement.SubtitleBurnCommand.Remove(0, 5);
                            }
                        }

                    }

                }

                ffmpegCommand += " -segment_time " + queueElement.ChunkLength.ToString() + " -f segment \"";
                ffmpegCommand += Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Chunks", "split%6d.mkv") + "\"";

                Global.Logger("INFO  - VideoSplitter.Split() => FFmpegChunking() => FFmpeg Command: " + ffmpegCommand, queueElement.Output + ".log");

                // Start Splitting
                Process chunkingProcess = new();
                ProcessStartInfo startInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "FFmpeg"),
                    Arguments = ffmpegCommand
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
                string stderr = "\n";
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (! line.Contains("frame=") && !line.Contains("[segment"))
                        stderr += line + "\n";
                    int processedFrames = Global.GetTotalFramesProcessed(line);
                    if (processedFrames != 0)
                    {
                        queueElement.Progress = Convert.ToDouble(processedFrames);
                        queueElement.Status = "Splitting - " + ((decimal)queueElement.Progress / queueElement.FrameCount).ToString("0.00%");
                    }
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
                    Global.Logger("DEBUG - VideoSplitter.Split() => FFmpegChunking() => FFmpeg Exit Code: " + exit_code, queueElement.Output + ".log");
                }
                else
                {
                    queueElement.Error = true;
                    queueElement.ErrorCount += 1;
                    Global.Logger("FATAL - VideoSplitter.Split() => FFmpegChunking() => FFmpeg Exit Code: " + exit_code, queueElement.Output + ".log");
                    Global.Logger("==========================================================" + stderr, queueElement.Output + ".log");
                    Global.Logger("==========================================================", queueElement.Output + ".log");
                }
            }
            else
            {
                Global.Logger("WARN - VideoSplitter.Split() => FFmpegChunking() => Skipped Splitting - Resuming?", queueElement.Output + ".log");
            }
        }

        private static void KillProcessAndChildren(int pid)
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher
              ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();
            foreach (ManagementObject mo in moc)
            {
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
            }
            try
            {
                Process proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }
    }
}
