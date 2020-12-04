using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace NotEnoughAV1Encodes
{
    public partial class VideoSplittingWindow : Window
    {
        // Temp variables
        public string FFmpegThreshold = "0.4";  // scene threshold
        public string ChunkLength = "10";       // chunking (length in seconds)
        public string HardSubCMD = "";          // Hardsub FFmpeg Command (only chunking method)
        public int DetectMethod = 0;            // scene detect ffmpeg
        public int ReencodeMethod = 0;          // x264
        public bool Reencode = false;           // reencoding boolean
        public bool HardSub = false;            // hardsub boolean (only chunking method)

        public VideoSplittingWindow(int method, bool reencode, int reencodemethod, string thresholdFfmpeg, string chunk, bool subHardSub, string subHardSubCMD)
        {
            InitializeComponent();

            // Sets the values from constructor
            FFmpegThreshold = thresholdFfmpeg;
            ReencodeMethod = reencodemethod;
            DetectMethod = method;
            Reencode = reencode;
            ChunkLength = chunk;
            HardSub = subHardSub;
            HardSubCMD = subHardSubCMD;

            // Starts the Main Function
            StartDetect();
        }

        private async void StartDetect()
        {
            // Main Function
            if (DetectMethod == 0)
                await Task.Run(() => FFmpegSceneDetect());
            if (DetectMethod == 1)
                await Task.Run(() => PySceneDetect());
            if (DetectMethod == 2)
                await Task.Run(() => FFmpegChunking());
            this.Close();
        }

        List<string> FFmpegArgs = new List<string>();

        // ═════════════════════════════════ FFMmpeg Scene Detect ═══════════════════════════════════

        private void FFmpegSceneDetect()
        {
            // Skip Scene Detect if the file already exist
            if (File.Exists(Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "splits.txt")) == false)
            {
                SmallFunctions.Logging("Scene Detection with FFmpeg");
                TextBoxConsole.Dispatcher.Invoke(() => TextBoxConsole.Text = "Detecting Scenes... this might take a while!");

                List<string> scenes = new List<string>();

                // Starts FFmpeg Process
                Process FFmpegSceneDetect = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = MainWindow.FFmpegPath,
                    RedirectStandardError = true,
                    FileName = "cmd.exe",
                    Arguments = "/C ffmpeg.exe -i " + '\u0022' + MainWindow.VideoInput + '\u0022' + " -hide_banner -loglevel 32 -filter_complex select=" + '\u0022' + "gt(scene\\," + FFmpegThreshold + "),select=eq(key\\,1),showinfo" + '\u0022' + " -an -f null -"
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
                    using (StreamWriter sw = File.AppendText(Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "splits.txt")))
                    {
                        sw.WriteLine(line);
                        sw.Close();
                    }
                }
            }
        }

        // ════════════════════════════════════ PySceneDetect ═══════════════════════════════════════
        
        private void PySceneDetect()
        {
            // Skip Scene Detect if the file already exist
            if (File.Exists(Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "splits.txt")) == false)
            {
                SmallFunctions.Logging("Scene Detection with PySceneDetect");
                // Detects the Scenes with PySceneDetect
                Process pySceneDetect = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    FileName = "cmd.exe",
                    Arguments = "/C scenedetect -i " + '\u0022' + MainWindow.VideoInput + '\u0022' + " -o " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName) + '\u0022' + " detect-content list-scenes"
                };
                pySceneDetect.StartInfo = startInfo;

                pySceneDetect.Start();

                // Reads the Stderr and sets the TextBox
                do { TextBoxConsole.Dispatcher.Invoke(() => TextBoxConsole.Text = pySceneDetect.StandardError.ReadLine()); } while (!pySceneDetect.HasExited);

                pySceneDetect.WaitForExit();

                PySceneDetectParse();
            }

        }

        private void PySceneDetectParse()
        {
            // Reads first line of the csv file generated by pyscenedetect
            string line = File.ReadLines(Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, MainWindow.TempPathFileName + "-Scenes.csv")).First();

            // Splits the line after "," and skips the first line, then adds the result to list
            List<string> scenes = line.Split(',').Skip(1).ToList<string>();

            // Temporary value used for creating the ffmpeg command line
            string previousScene = "00:00:00.000";

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
                using (StreamWriter sw = File.AppendText(Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "splits.txt")))
                {
                    sw.WriteLine(lineArg);
                    sw.Close();
                }
            }
        }

        // ═══════════════════════════════════════ Chunking ═════════════════════════════════════════

        private void FFmpegChunking()
        {
            // Skip splitting if already splitted
            if (File.Exists(Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "finished_splitting.log")) == false)
            {
                // This function is used to split the source video into parts (chunking method)
                // Sets the label, has to be done as Dispatcher, else it will lock up the thread
                LabelSplittingMethod.Dispatcher.Invoke(() => LabelSplittingMethod.Content = "Splitting with Chunking Method");

                // Sets the TextBox, has to be done as Dispatcher, else it will lock up the thread
                TextBoxConsole.Dispatcher.Invoke(() => TextBoxConsole.Text = "Started Splitting... this might take a while!");

                string EncodeCMD = null;

                // Sets the Reencode Params
                if (Reencode == true)
                {
                    if (ReencodeMethod == 0)
                        EncodeCMD = "-c:v libx264 -crf 0 -preset ultrafast -g 9 -sc_threshold 0 -force_key_frames " + '\u0022' + "expr:gte(t, n_forced * 9)" + '\u0022';
                    if (ReencodeMethod == 1)
                        EncodeCMD = "-c:v ffv1 -level 3 -threads 4 -coder 1 -context 1 -g 1 -slicecrc 0 -slices 4";
                    if (ReencodeMethod == 2)
                        EncodeCMD = "-c:v utvideo";
                }
                else
                {
                    EncodeCMD = "-c:v copy";
                }

                //Run ffmpeg command
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    WorkingDirectory = MainWindow.FFmpegPath,
                    Arguments = "/C ffmpeg.exe -i " + '\u0022' + MainWindow.VideoInput + '\u0022' + " " + HardSubCMD + " -map_metadata -1 -an " + EncodeCMD + " -f segment -segment_time " + ChunkLength + " " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Chunks", "split%0d.mkv") + '\u0022'
                };
                SmallFunctions.Logging("Splitting with FFmpeg Chunking: " + startInfo.Arguments);
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();

                // Starts renaming the splits
                RenameSplits.Rename();
            }
            // Resume stuff to skip splitting in resume mode
            SmallFunctions.WriteToFileThreadSafe("", Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName,  "finished_splitting.log"));
        }
    }
}
