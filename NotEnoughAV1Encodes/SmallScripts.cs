using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

namespace NotEnoughAV1Encodes
{
    internal class SmallScripts
    {
        public static void CreateDirectory(string currentPath, string foldername)
        {
            if (!Directory.Exists(Path.Combine(currentPath, foldername)))
                Directory.CreateDirectory(Path.Combine(currentPath, foldername));
        }

        public static void CountVideoChunks()
        {
            MainWindow.chunksDir = System.IO.Path.Combine(MainWindow.workingTempDirectory, "Chunks");
            MainWindow.videoChunks = Directory.GetFiles(MainWindow.chunksDir, "*mkv", SearchOption.AllDirectories).Select(x => Path.GetFileName(x)).ToArray();
            MainWindow.numberofvideoChunks = MainWindow.videoChunks.Count().ToString();

            if (MainWindow.resumeMode == true)
            {
                bool fileExist = File.Exists("encoded.log");
                if (fileExist)
                {
                    foreach (string line in File.ReadLines("encoded.log"))
                    {
                        MainWindow.videoChunks = MainWindow.videoChunks.Where(s => s != line).ToArray();
                    }
                    MainWindow.numberofvideoChunks = MainWindow.videoChunks.Count().ToString();
                }
            }
        }

        public static class Cancel
        {
            //Public Cancel boolean
            public static bool CancelAll = false;
        }

        public static void KillInstances()
        {
            //Kills all aomenc and ffmpeg instances
            try
            {
                foreach (var process in Process.GetProcessesByName("aomenc")) { process.Kill(); }
                foreach (var process in Process.GetProcessesByName("rav1e")) { process.Kill(); }
                foreach (var process in Process.GetProcessesByName("SvtAv1EncApp")) { process.Kill(); }
                foreach (var process in Process.GetProcessesByName("ffmpeg")) { process.Kill(); }
            }
            catch { }
        }

        private static ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();

        public static void WriteToFileThreadSafe(string text, string path)
        {
            //Some smaller Blackmagic, so parallel Workers won't lockdown files
            // Set Status to Locked
            _readWriteLock.EnterWriteLock();
            try
            {
                // Append text to the file
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.WriteLine(text);
                    sw.Close();
                }
            }
            finally
            {
                // Release lock
                _readWriteLock.ExitWriteLock();
            }
        }

        public static void GetStreamLength(string fileinput)
        {
            string input;

            input = '\u0022' + fileinput + '\u0022';

            Process process = new Process();
            process.StartInfo = new ProcessStartInfo()
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                WorkingDirectory = MainWindow.exeffprobePath,
                Arguments = "/C ffprobe.exe -i " + input + " -show_entries format=duration -v quiet -of csv=" + '\u0022' + "p=0" + '\u0022',
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            process.Start();
            string streamlength = process.StandardOutput.ReadLine();
            string value = new DataTable().Compute(streamlength, null).ToString();
            MainWindow.streamLength = Convert.ToInt64(Math.Round(Convert.ToDouble(value))).ToString();
            process.WaitForExit();
        }

        public static void DeleteTempFiles()
        {
            try
            {
                //Delete Files, because of lazy dump****
                if (File.Exists("splitted.log")) { File.Delete("splitted.log"); }
                if (File.Exists("encoded.log")) { File.Delete("encoded.log"); }
                if (File.Exists("no_audio.mkv")) { File.Delete("no_audio.mkv"); }
                if (Directory.Exists("Temp")) { Directory.Delete("Temp", true); }
                if (File.Exists("unfinishedjob.xml")) { File.Delete("unfinishedjob.xml"); }
            }
            catch { }
        }

        public static void DeleteTempFilesDir(string path)
        {
            try
            {
                if (Directory.Exists(path)) { Directory.Delete(path, true); }
            }
            catch { }
        }

        public static void CheckAudioEncode()
        {
            //Checks if Audio Encoding was successful
            if (File.Exists(MainWindow.workingTempDirectory + "\\AudioEncoded\\audio.mkv") == false)
            {
                if (MessageBox.Show("Something with the Audio Encoding went wrong! Skip Audio?", "Error", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    MainWindow.audioEncoding = false;
                }
                else
                {
                    Cancel.CancelAll = true;
                }
            }
            
        }

        public static void CheckSubtitleEncode()
        {
            //Checks if the Subtitle Muxing was successful
            if (File.Exists(MainWindow.workingTempDirectory + "\\Subtitles\\subtitle.mkv") == false && MainWindow.subtitleStreamCopy == true)
            {
                if (MessageBox.Show("Something with the Subtitles went wrong! Skip Subtitles?", "Error", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    MainWindow.subtitles = false;
                    MainWindow.subtitleStreamCopy = false;
                    MainWindow.subtitleCustom = false;
                }
                else
                {
                    Cancel.CancelAll = true;
                }
            }else if (File.Exists(MainWindow.workingTempDirectory + "\\Subtitles\\subtitlecustom.mkv") == false && MainWindow.subtitleCustom == true)
            {
                if (MessageBox.Show("Something with the Subtitles went wrong! Skip Subtitles?", "Error", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    MainWindow.subtitles = false;
                    MainWindow.subtitleStreamCopy = false;
                    MainWindow.subtitleCustom = false;
                }
                else
                {
                    Cancel.CancelAll = true;
                }
            }
        }

        public static void ExecuteFfmpegTask(string ffmpegCommand)
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.WorkingDirectory = MainWindow.exeffmpegPath;
            startInfo.Arguments = ffmpegCommand;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();

        }
    }
}