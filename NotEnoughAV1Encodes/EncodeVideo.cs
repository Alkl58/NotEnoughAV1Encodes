using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NotEnoughAV1Encodes
{
    class EncodeVideo
    {
        public static void Encode()
        {
            // Main Encoding Function
            // Creates a new Thread Pool
            using (SemaphoreSlim concurrencySemaphore = new SemaphoreSlim(MainWindow.WorkerCount))
            {
                // Creates a tasks list
                List<Task> tasks = new List<Task>();
                // Iterates over all args in VideoChunks list
                foreach (var command in Global.Video_Chunks)
                {
                    concurrencySemaphore.Wait();
                    var task = Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            if (!SmallFunctions.Cancel.CancelAll)
                            {
                                // We need the index of the command in the array
                                var index = Array.FindIndex(Global.Video_Chunks, row => row.Contains(command));
                                // Logic for resume mode - skips already encoded files
                                if (File.Exists(Path.Combine(Global.temp_path, Global.temp_path_folder, "Chunks", "split" + index.ToString("D5") + ".ivf" + "_finished.log")) == false)
                                {
                                    // One Pass Encoding
                                    Process ffmpegProcess = new Process();
                                    ProcessStartInfo startInfo = new ProcessStartInfo();
                                    startInfo.UseShellExecute = true;

                                    // Changed from CMD to ffmpeg
                                    // This allows to properly track PIDs auf launched instances
                                    // Drawback: No Piping, completly switch to ffmpeg instead of using external encoders
                                    startInfo.FileName = "ffmpeg.exe";
                                    startInfo.WorkingDirectory = Global.FFmpeg_Path;

                                    if (!MainWindow.ShowTerminal)
                                        startInfo.WindowStyle = ProcessWindowStyle.Hidden;

                                    string InputVideo = "";

                                    if (Splitting.split_type >= 1)
                                    {
                                        // FFmpeg Scene Detect or PySceneDetect
                                        InputVideo = " -i " + '\u0022' + Global.Video_Path + '\u0022' + " " + command;
                                    }
                                    else if (Splitting.split_type == 0)
                                    {
                                        // Chunk based splitting
                                        InputVideo = " -i " + '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "Chunks", command) + '\u0022';
                                    }

                                    // Saves encoder progress to log file
                                    string ffmpeg_progress = " -progress " + '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "Progress", "split" + index.ToString("D5") + "_progress.log") + '\u0022';

                                    string ffmpeg_input = InputVideo + " " + MainWindow.FilterCommand + MainWindow.PipeBitDepthCommand + " " + MainWindow.VSYNC + " ";

                                    // Logic to skip first pass encoding if "_finished" log file exists
                                    if (File.Exists(Path.Combine(Global.temp_path, Global.temp_path_folder, "Chunks", "split" + index.ToString("D5") + "_stats.log" + "_finished.log")) == false)
                                    {
                                        string encoderCMD = "";

                                        if (MainWindow.EncodeMethod != 1)
                                        {
                                            if (MainWindow.OnePass) // One Pass Encoding
                                            {
                                                encoderCMD = " -y " + MainWindow.Final_Encoder_Command + " ";
                                                encoderCMD += '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "Chunks", "split" + index.ToString("D5") + ".webm") + '\u0022';
                                            }
                                            else // Two Pass Encoding First Pass
                                            {
                                                encoderCMD = " -y " + MainWindow.Final_Encoder_Command + " -pass 1 -passlogfile ";
                                                encoderCMD += '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "Chunks", "split" + index.ToString("D5") + "_stats.log") + '\u0022' + " -f webm NUL";
                                            }
                                        }
                                        else
                                        {
                                            // rav1e
                                            encoderCMD = " -y " + MainWindow.Final_Encoder_Command + " ";
                                            encoderCMD += '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "Chunks", "split" + index.ToString("D5") + ".webm") + '\u0022';
                                        }

                                        startInfo.Arguments = ffmpeg_progress + ffmpeg_input + encoderCMD;

                                        Helpers.Logging("Encoding Video: " + startInfo.Arguments);
                                        ffmpegProcess.StartInfo = startInfo;
                                        ffmpegProcess.Start();

                                        // Sets the process priority
                                        if (!MainWindow.Priority)
                                            ffmpegProcess.PriorityClass = ProcessPriorityClass.BelowNormal;

                                        // Get launched Process ID
                                        int temp_pid = ffmpegProcess.Id;

                                        // Add Process ID to Array, inorder to keep track / kill the instances
                                        Global.Launched_PIDs.Add(temp_pid);

                                        ffmpegProcess.WaitForExit();

                                        // Get Exit Code
                                        int exit_code = ffmpegProcess.ExitCode;

                                        // Remove PID from Array after Exit
                                        Global.Launched_PIDs.RemoveAll(i => i == temp_pid);

                                        if (MainWindow.OnePass == false && SmallFunctions.Cancel.CancelAll == false)
                                        {
                                            // Writes log file if first pass is finished, to be able to skip them later if in resume mode
                                            Helpers.WriteToFileThreadSafe("", Path.Combine(Global.temp_path, Global.temp_path_folder, "Chunks", "split" + index.ToString("D5") + "_stats.log" + "_finished.log"));
                                        }
                                    }


                                    if (!MainWindow.OnePass)
                                    {
                                        // Creates a different progress file for the second pass (avoids negative frame progressbar)
                                        ffmpeg_progress = " -progress " + '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "Progress", "split" + index.ToString("D5") + "_progress_2nd.log") + '\u0022';

                                        string encoderCMD = " -pass 2 " + MainWindow.Final_Encoder_Command;

                                        encoderCMD += " -passlogfile " + '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "Chunks", "split" + index.ToString("D5") + "_stats.log") + '\u0022';
                                        encoderCMD += " " + '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "Chunks", "split" + index.ToString("D5") + ".webm") + '\u0022';

                                        startInfo.Arguments = ffmpeg_progress + ffmpeg_input + encoderCMD;
                                        Helpers.Logging("Encoding Video: " + startInfo.Arguments);
                                        ffmpegProcess.StartInfo = startInfo;
                                        ffmpegProcess.Start();

                                        // Sets the process priority
                                        if (!MainWindow.Priority)
                                            ffmpegProcess.PriorityClass = ProcessPriorityClass.BelowNormal;

                                        // Get launched Process ID
                                        int temp_pid = ffmpegProcess.Id;

                                        // Add Process ID to Array, inorder to keep track / kill the instances
                                        Global.Launched_PIDs.Add(temp_pid);

                                        ffmpegProcess.WaitForExit();

                                        // Get Exit Code
                                        int exit_code = ffmpegProcess.ExitCode;

                                        // Remove PID from Array after Exit
                                        Global.Launched_PIDs.RemoveAll(i => i == temp_pid);
                                    }
                                    if (SmallFunctions.Cancel.CancelAll == false)
                                    {
                                        // This function will write finished encodes to a log file, to be able to skip them if in resume mode
                                        Helpers.WriteToFileThreadSafe("", Path.Combine(Global.temp_path, Global.temp_path_folder, "Chunks", "split" + index.ToString("D5") + ".ivf" + "_finished.log"));
                                    }
                                }
                            }
                            else
                            {
                                Kill.Kill_PID();
                            }
                        }
                        finally { concurrencySemaphore.Release(); }
                    });
                    tasks.Add(task);
                }
                Task.WaitAll(tasks.ToArray());
            }
        }
    }
}
