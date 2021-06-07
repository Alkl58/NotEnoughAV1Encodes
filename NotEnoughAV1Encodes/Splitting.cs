using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace NotEnoughAV1Encodes
{
    class Splitting
    {
        public static int split_type = 0;
        public static int chunking_length = 0;
        public static int encode_method = 0;
        public static List<string> FFmpegArgs = new List<string>();
        public static string FFmpeg_Threshold = "";

        public static void Split()
        {
            if (split_type == 0)
            {
                FFmpegChunking();
            }
            else if(split_type == 1)
            {
                FFmpegSceneDetect();
            }
            else if (split_type == 2)
            {
                PySceneDetect();
            }
        }



        private static void FFmpegSceneDetect()
        {
            // Skip Scene Detect if the file already exist
            if (File.Exists(Path.Combine(Global.temp_path, Global.temp_path_folder, "splits.txt")) == false)
            {
                Helpers.Logging("Scene Detection with FFmpeg");

                List<string> scenes = new List<string>();

                // Starts FFmpeg Process
                Process FFmpegSceneDetect = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Global.FFmpeg_Path,
                    RedirectStandardError = true,
                    FileName = "cmd.exe",
                    Arguments = "/C ffmpeg.exe -i " + '\u0022' + Global.Video_Path + '\u0022' + " -hide_banner -loglevel 32 -filter_complex select=" + '\u0022' + "gt(scene\\," + FFmpeg_Threshold + "),select=eq(key\\,1),showinfo" + '\u0022' + " -an -f null -"
                };
                FFmpegSceneDetect.StartInfo = startInfo;
                FFmpegSceneDetect.Start();

                // Reads Standard Err from FFmpeg Output
                string stream = FFmpegSceneDetect.StandardError.ReadToEnd();

                FFmpegSceneDetect.WaitForExit();

                // Splits the Console Output by spaces
                string[] array = stream.Split(' ');

                // Searches for pts_time, if found it removes "pts_time:" to get only values
                foreach (string value in array) { if (value.Contains("pts_time:")) { scenes.Add(value.Remove(0, 9)); } }

                // Temporary value for Arg creation
                string previousScene = "0.000";

                // Clears the Args List to avoid conflicts in Batch Encode Mode
                FFmpegArgs.Clear();

                // Creates the seeking args for ffmpeg piping
                foreach (string sc in scenes)
                {
                    FFmpegArgs.Add("-ss " + previousScene + " -to " + sc);
                    previousScene = sc;
                }
                // Argument for seeking until the end of the video
                FFmpegArgs.Add("-ss " + previousScene);

                // Writes splitting arguments to text file
                foreach (string line in FFmpegArgs)
                {
                    using (StreamWriter sw = File.AppendText(Path.Combine(Global.temp_path, Global.temp_path_folder, "splits.txt")))
                    {
                        sw.WriteLine(line);
                        sw.Close();
                    }
                }

                if (File.Exists(Path.Combine(Global.temp_path, Global.temp_path_folder, "splits.txt")))
                {
                    Global.Video_Chunks = File.ReadAllLines(Path.Combine(Global.temp_path, Global.temp_path_folder, "splits.txt")); // Reads the split file for VideoEncode() function
                }
            }
        }

        private static void PySceneDetect()
        {
            // Skip Scene Detect if the file already exist
            if (File.Exists(Path.Combine(Global.temp_path, Global.temp_path_folder, "splits.txt")) == false)
            {
                Helpers.Logging("Scene Detection with PySceneDetect");
                // Detects the Scenes with PySceneDetect
                Process pySceneDetect = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    FileName = "cmd.exe",
                    Arguments = "/C scenedetect -i " + '\u0022' + Global.Video_Path + '\u0022' + " -o " + '\u0022' + Path.Combine(Global.temp_path, Global.temp_path_folder) + '\u0022' + " detect-content list-scenes"
                };
                pySceneDetect.StartInfo = startInfo;

                pySceneDetect.Start();

                pySceneDetect.WaitForExit();

                PySceneDetectParse();
            }

        }

        private static void PySceneDetectParse()
        {
            // Reads first line of the csv file generated by pyscenedetect
            string line = File.ReadLines(Path.Combine(Global.temp_path, Global.temp_path_folder, Global.temp_path_folder + "-Scenes.csv")).First();

            // Splits the line after "," and skips the first line, then adds the result to list
            List<string> scenes = line.Split(',').Skip(1).ToList<string>();

            // Temporary value used for creating the ffmpeg command line
            string previousScene = "00:00:00.000";

            // Clears the Args List to avoid conflicts in Batch Encode Mode
            FFmpegArgs.Clear();

            // Iterates over the list of time codes and creates the args for ffmpeg
            foreach (string sc in scenes)
            {
                FFmpegArgs.Add("-ss " + previousScene + " -to " + sc);
                previousScene = sc;
            }

            // Has to be last, to "tell" ffmpeg to seek / encode until end of video
            FFmpegArgs.Add("-ss " + previousScene);

            // Writes splitting arguments to text file
            foreach (string lineArg in FFmpegArgs)
            {
                using (StreamWriter sw = File.AppendText(Path.Combine(Global.temp_path, Global.temp_path_folder, "splits.txt")))
                {
                    sw.WriteLine(lineArg);
                    sw.Close();
                }
            }

            if (File.Exists(Path.Combine(Global.temp_path, Global.temp_path_folder, "splits.txt")))
            {
                Global.Video_Chunks = File.ReadAllLines(Path.Combine(Global.temp_path, Global.temp_path_folder, "splits.txt")); // Reads the split file for VideoEncode() function
            }
        }

        private static void FFmpegChunking()
        {
            // Skips Splitting of already existent
            if (!File.Exists(Path.Combine(Global.temp_path, Global.temp_path_folder, "splitting.log")))
            {
                // Create Chunks Folder
                Helpers.Create_Temp_Folder(Path.Combine(Global.temp_path, Global.temp_path_folder, "Chunks"));

                // Generate Command
                string ffmpeg_command = "/C ffmpeg.exe";
                ffmpeg_command += " -y -i " + '\u0022' + Global.Video_Path + '\u0022';                                              // Video Input
                ffmpeg_command += " -reset_timestamps 1 -map_metadata -1 -sn -an";                                                  // Remove unnecessary metadata etc

                if (encode_method == 0)
                {
                    ffmpeg_command += " -c:v libx264 -preset ultrafast -crf 0";                                                     // Re-Encoding - Needed because else it WILL loose frames
                }
                else if(encode_method == 1)
                {
                    ffmpeg_command += " -c:v ffv1 -level 3 -threads 4 -coder 1 -context 1 -slicecrc 0 -slices 4";                   // Re-Encoding - Needed because else it WILL loose frames
                }
                else if(encode_method == 2)
                {
                    ffmpeg_command += " -c:v utvideo";                                                                              // Re-Encoding - Needed because else it WILL loose frames
                }

                // Hardsub
                if (MainWindow.subHardSubEnabled)
                {
                    ffmpeg_command += " " + MainWindow.hardsub_command;
                }

                ffmpeg_command += " -sc_threshold 0 -g " + chunking_length;                                                         // Make Splitting more accurate
                ffmpeg_command += " -force_key_frames " + '\u0022' + "expr:gte(t, n_forced * " + chunking_length + ")" + '\u0022';  // Make Splitting more accurate
                ffmpeg_command += " -segment_time " + chunking_length + " -f segment " + '\u0022';                                  // Segmenting
                ffmpeg_command += Path.Combine(Global.temp_path, Global.temp_path_folder, "Chunks", "split%6d.mkv") + '\u0022';     // Video Output

                Helpers.Logging("Equal Chunking: " + ffmpeg_command);

                // Start Splitting
                Process chunking_process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    WorkingDirectory = Global.FFmpeg_Path,
                    Arguments = ffmpeg_command
                };
                chunking_process.StartInfo = startInfo;

                // Start Process
                chunking_process.Start();

                // Get launched Process ID
                int temp_pid = chunking_process.Id;

                // Add Process ID to Array, inorder to keep track / kill the instances
                Global.Launched_PIDs.Add(temp_pid);

                // Wait for Exit
                chunking_process.WaitForExit();

                // Get Exit Code
                int exit_code = chunking_process.ExitCode;

                // Remove PID from Array after Exit
                Global.Launched_PIDs.RemoveAll(i => i == temp_pid);

                // Write Save Point
                Helpers.WriteToFileThreadSafe("", Path.Combine(Global.temp_path, Global.temp_path_folder, "splitting.log"));
            }

            // Add Chunks to Array
            Global.Video_Chunks = Directory.GetFiles(Path.Combine(Global.temp_path, Global.temp_path_folder, "Chunks"), "*mkv", SearchOption.AllDirectories).Select(x => Path.GetFileName(x)).ToArray();
        }
    }
}
