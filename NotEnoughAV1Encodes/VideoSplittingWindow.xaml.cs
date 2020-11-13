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
        public string FFmpegThreshold = "0.4";
        public int DetectMethod = 0; // scene detect ffmpeg
        public int ReencodeMethod = 0; // x264
        public bool Reencode = false;
        public VideoSplittingWindow(int method, bool reencode, int reencodemethod, string thresholdFfmpeg)
        {
            InitializeComponent();

            FFmpegThreshold = thresholdFfmpeg;
            DetectMethod = method;
            ReencodeMethod = reencodemethod;
            Reencode = reencode;

            StartDetect();
        }

        private async void StartDetect()
        {
            if (DetectMethod == 0)
                await Task.Run(() => FFmpegSceneDetect());
            if (DetectMethod == 1)
                await Task.Run(() => PySceneDetect());

            await Task.Run(() => FFmpegSceneSplit());
            await Task.Run(() => RenameSplits.Rename());
            this.Close();
        }
        List<string> FFmpegArgs = new List<string>();
        private void FFmpegSceneDetect()
        {
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
                Arguments = "/C ffmpeg.exe -i " + '\u0022' + MainWindow.VideoInput + '\u0022' + " -progress " + '\u0022' + Path.Combine(MainWindow.TempPath, "progress.txt") + '\u0022' + " -hide_banner -loglevel 32 -filter_complex select=" + '\u0022' + "gt(scene\\," + FFmpegThreshold + "),select=eq(key\\,1),showinfo" + '\u0022' + " -an -f null -"
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

            // Creates the seeking args for ffmpeg
            foreach (string sc in scenes)
            {
                FFmpegArgs.Add("-ss " + previousScene + " -to " + sc);
                previousScene = sc;
            }
            // Argument for seeking until the end of the video
            FFmpegArgs.Add("-ss " + previousScene);
        }

        private void FFmpegSceneSplit()
        {
            TextBoxConsole.Dispatcher.Invoke(() => TextBoxConsole.Text = "Started Splitting... this might take a while!");

            string EncodeCMD = "";

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


            int counter = 0;
            foreach (string arg in FFmpegArgs)
            {
                //Run ffmpeg command
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    WorkingDirectory = MainWindow.FFmpegPath,
                    Arguments = "/C ffmpeg.exe -i " + '\u0022' + MainWindow.VideoInput + '\u0022' + " " + arg + " -map_metadata -1 -an " + EncodeCMD + " " + '\u0022' + Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Chunks", "split" + counter + ".mkv") + '\u0022'
                };
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
                counter += 1;
            }
        }

        private void PySceneDetect()
        {
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

            do { TextBoxConsole.Dispatcher.Invoke(() => TextBoxConsole.Text = pySceneDetect.StandardError.ReadLine()); } while (!pySceneDetect.HasExited);

            pySceneDetect.WaitForExit();

            PySceneDetectParse();
        }

        private void PySceneDetectParse()
        {
            // Reads first line of the csv file generated by pyscenedetect
            string line = File.ReadLines(Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, MainWindow.TempPathFileName + "-Scenes.csv")).First();

            List<string> scenes = line.Split(',').Skip(1).ToList<string>();

            string previousScene = "00:00:00.000";

            foreach (string sc in scenes)
            {
                FFmpegArgs.Add("-ss " + previousScene + " -to " + sc);
                previousScene = sc;
            }
            FFmpegArgs.Add("-ss " + previousScene);
        }
    }
}
