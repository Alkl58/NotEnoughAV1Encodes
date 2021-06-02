using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NotEnoughAV1Encodes
{
    class EncodeVideoPipe
    {
        public static void Encode()
        {
            // Main Encoding Function
            // Creates a new Thread Pool
            using (SemaphoreSlim concurrencySemaphore = new SemaphoreSlim(EncodeVideo.Worker_Count))
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

                                    startInfo.FileName = "cmd.exe";
                                    startInfo.WorkingDirectory = Global.FFmpeg_Path;

                                    if (!EncodeVideo.Show_Terminal)
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
                                    // FFmpeg Pipe
                                    string ffmpeg_input = InputVideo + " " + MainWindow.FilterCommand + EncodeVideo.Pixel_Format + " -color_range 0 " + MainWindow.VSYNC + " -f yuv4mpegpipe - | ";

                                    // Logic to skip first pass encoding if "_finished" log file exists
                                    if (File.Exists(Path.Combine(Global.temp_path, Global.temp_path_folder, "Chunks", "split" + index.ToString("D5") + "_stats.log" + "_finished.log")) == false)
                                    {
                                        string encoderCMD = "";

                                        if (MainWindow.OnePass)
                                        {
                                            // One Pass Encoding

                                            if (MainWindow.EncodeMethod == 5)
                                            {
                                                // aomenc
                                                encoderCMD = '\u0022' + Path.Combine(Global.Aomenc_Path, "aomenc.exe") + '\u0022' + " - --passes=1 " + EncodeVideo.Final_Encoder_Command + " --output=";
                                            }
                                            else if (MainWindow.EncodeMethod == 7)
                                            {
                                                // svt-av1
                                                ffmpeg_input = InputVideo + " " + MainWindow.FilterCommand + EncodeVideo.Pixel_Format + " -color_range 0 -nostdin " + MainWindow.VSYNC + " -f yuv4mpegpipe - | ";
                                                encoderCMD = '\u0022' + Path.Combine(Global.SvtAv1_Path, "SvtAv1EncApp.exe") + '\u0022' + " -i stdin " + EncodeVideo.Final_Encoder_Command + " --passes 1 -b ";
                                            }

                                            encoderCMD += '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "Chunks", "split" + index.ToString("D5") + ".webm") + '\u0022';

                                            if (MainWindow.EncodeMethod == 6)
                                            {
                                                // rav1e
                                                encoderCMD = '\u0022' + Path.Combine(Global.Rav1e__Path, "rav1e.exe") + '\u0022' + " - " + EncodeVideo.Final_Encoder_Command + " --output ";
                                                encoderCMD += '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "Chunks", "split" + index.ToString("D5") + ".ivf") + '\u0022';
                                            }
                                        }
                                        else
                                        {
                                            // Two Pass Encoding - First Pass

                                            if (MainWindow.EncodeMethod == 5)
                                            {
                                                // aomenc
                                                encoderCMD = '\u0022' + Path.Combine(Global.Aomenc_Path, "aomenc.exe") + '\u0022' + " - --passes=2 --pass=1 " + EncodeVideo.Final_Encoder_Command + " --fpf=";
                                                encoderCMD += '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "Chunks", "split" + index.ToString("D5") + "_stats.log") + '\u0022' + " --output=NUL";
                                            }
                                            else if (MainWindow.EncodeMethod == 7)
                                            {
                                                // svt-av1
                                                ffmpeg_input = InputVideo + " " + MainWindow.FilterCommand + EncodeVideo.Pixel_Format + " -color_range 0 -nostdin " + MainWindow.VSYNC + " -f yuv4mpegpipe - | ";
                                                encoderCMD = '\u0022' + Path.Combine(Global.SvtAv1_Path, "SvtAv1EncApp.exe") + '\u0022' + " -i stdin " + EncodeVideo.Final_Encoder_Command + " --irefresh-type 2 --pass 1 -b NUL --stats ";
                                                encoderCMD += '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "Chunks", "split" + index.ToString("D5") + "_stats.log") + '\u0022';
                                            }
                                        }


                                        startInfo.Arguments = "/C ffmpeg.exe " + ffmpeg_progress + ffmpeg_input + encoderCMD;

                                        Helpers.Logging("Encoding Video: " + startInfo.Arguments);
                                        ffmpegProcess.StartInfo = startInfo;
                                        ffmpegProcess.Start();

                                        // Sets the process priority
                                        if (!EncodeVideo.Process_Priority)
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

                                        string encoderCMD = "";


                                        if (MainWindow.EncodeMethod == 5)
                                        {
                                            // aomenc
                                            encoderCMD = '\u0022' + Path.Combine(Global.Aomenc_Path, "aomenc.exe") + '\u0022' + " - --passes=2 --pass=2 " + EncodeVideo.Final_Encoder_Command + " --fpf=";
                                            encoderCMD += '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "Chunks", "split" + index.ToString("D5") + "_stats.log") + '\u0022' + " --output=";
                                        }
                                        else if(MainWindow.EncodeMethod == 7)
                                        {
                                            // svt-av1
                                            ffmpeg_input = InputVideo + " " + MainWindow.FilterCommand + EncodeVideo.Pixel_Format + " -color_range 0 -nostdin " + MainWindow.VSYNC + " -f yuv4mpegpipe - | ";
                                            encoderCMD = '\u0022' + Path.Combine(Global.SvtAv1_Path, "SvtAv1EncApp.exe") + '\u0022' + " -i stdin " + EncodeVideo.Final_Encoder_Command + " --irefresh-type 2 --pass 2 --stats ";
                                            encoderCMD += '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "Chunks", "split" + index.ToString("D5") + "_stats.log") + '\u0022' + " -b ";
                                        }

                                        encoderCMD += '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder, "Chunks", "split" + index.ToString("D5") + ".webm") + '\u0022';

                                        startInfo.Arguments = "/C ffmpeg.exe " + ffmpeg_progress + ffmpeg_input + encoderCMD;
                                        Helpers.Logging("Encoding Video: " + startInfo.Arguments);
                                        ffmpegProcess.StartInfo = startInfo;
                                        ffmpegProcess.Start();

                                        // Sets the process priority
                                        if (!EncodeVideo.Process_Priority)
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
