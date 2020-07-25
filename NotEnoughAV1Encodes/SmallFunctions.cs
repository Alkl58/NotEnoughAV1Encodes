using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading;
using System.Windows;

namespace NotEnoughAV1Encodes
{
    class SmallFunctions
    {
        private static ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();

        public static void WriteToFileThreadSafe(string text, string path)
        {
            //Some smaller Blackmagic, so parallel Workers won't deadlock files
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

        public static class Cancel
        {
            //Public Cancel boolean
            public static bool CancelAll = false;
        }

        public static string getFilename(string videoInput)
        {
            return Path.GetFileNameWithoutExtension(videoInput);
        }

        public static void checkDependeciesStartup()
        {
            bool ffmpegExists, ffprobeExists;
            ffmpegExists = File.Exists(MainWindow.ffmpegPath + "\\ffmpeg.exe");
            ffprobeExists = File.Exists(MainWindow.ffprobePath + "\\ffprobe.exe");
            if (ffmpegExists == false || ffprobeExists == false)
            {
                MessageBox.Show("Could not find all dependencies! Please check if the dependencies ffprobe and ffmpeg are located in: \n" + Directory.GetCurrentDirectory() + "\\Apps\\ffmpeg\\ \nor in the Windows PATH environment!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static bool checkDependencies(string encoder)
        {
            bool av1encoderexists = false, ffmpegExists, ffprobeExists;
            ffmpegExists = File.Exists(MainWindow.ffmpegPath + "\\ffmpeg.exe");
            ffprobeExists = File.Exists(MainWindow.ffprobePath + "\\ffprobe.exe");
            switch (encoder)
            {
                case "aomenc":
                    av1encoderexists = File.Exists(MainWindow.aomencPath + "\\aomenc.exe");
                    break;
                case "rav1e":
                    av1encoderexists = File.Exists(MainWindow.rav1ePath + "\\rav1e.exe");
                    break;
                case "svt-av1":
                    av1encoderexists = File.Exists(MainWindow.svtav1Path + "\\SvtAv1EncApp.exe");
                    break;
                case "aomenc (ffmpeg)":
                    av1encoderexists = ffmpegExists;
                    break;
                default:
                    break;
            }
            if (ffmpegExists && ffprobeExists && av1encoderexists) { return true; }
            else 
            {
                MessageBox.Show("Could not find all dependencies: \n ffmpeg found: " + ffmpegExists + " \n ffprobe found: " + ffprobeExists + " \n " + encoder + " found: " + av1encoderexists, "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false; 
            }
            
        }

        public static string getFrameRate(string videoInput)
        {
            string input = '\u0022' + videoInput + '\u0022';
            Process getStreamFps = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    WorkingDirectory = MainWindow.ffprobePath,
                    Arguments = "/C ffprobe.exe -i " + input + " -v error -select_streams v -of default=noprint_wrappers=1:nokey=1 -show_entries stream=r_frame_rate",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            getStreamFps.Start();
            string framerate = getStreamFps.StandardOutput.ReadLine();
            getStreamFps.WaitForExit();
            return framerate;
        }

        public static string getPixelFormat(string videoInput)
        {
            string input = '\u0022' + videoInput + '\u0022';
            Process getPixelFormat = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    WorkingDirectory = MainWindow.ffprobePath,
                    Arguments = "/C ffprobe.exe -i " + input + " -v error -select_streams v -of default=noprint_wrappers=1:nokey=1 -show_entries stream=pix_fmt",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            getPixelFormat.Start();
            string pixfmt = getPixelFormat.StandardOutput.ReadLine();
            getPixelFormat.WaitForExit();
            return pixfmt;
        }

        public static string getVideoLength(string videoInput)
        {
            string input = '\u0022' + videoInput + '\u0022';
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    WorkingDirectory = MainWindow.ffprobePath,
                    Arguments = "/C ffprobe.exe -i " + input + " -show_entries format=duration -v quiet -of csv=" + '\u0022' + "p=0" + '\u0022',
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            process.Start();
            string stream = process.StandardOutput.ReadLine();
            string value = new DataTable().Compute(stream, null).ToString();
            process.WaitForExit();
            return Convert.ToInt64(Math.Round(Convert.ToDouble(value))).ToString();
        }

        public static void checkCreateFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
        }

        public static void ExecuteFfmpegTask(string ffmpegCommand)
        {
            //Run ffmpeg command
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                WorkingDirectory = MainWindow.ffmpegPath,
                Arguments = ffmpegCommand
            };
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
        }

        public static void CountVideoChunks()
        {
            MainWindow.videoChunks = Directory.GetFiles(Path.Combine(MainWindow.tempPath, "Chunks"), "*mkv", SearchOption.AllDirectories).Select(x => Path.GetFileName(x)).ToArray();
            MainWindow.videoChunksCount = MainWindow.videoChunks.Count();
            //Removes all chunks from chunklist which are in encoded.log
            if (MainWindow.resumeMode == true)
            {
                bool fileExist = File.Exists(Path.Combine(MainWindow.tempPath, "encoded.log"));
                if (fileExist)
                {
                    foreach (string line in File.ReadLines(Path.Combine(MainWindow.tempPath, "encoded.log")))
                    {
                        MainWindow.videoChunks = MainWindow.videoChunks.Where(s => s != line).ToArray();
                    }
                    MainWindow.videoChunksCount = MainWindow.videoChunks.Count();
                }
            }
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

        public static bool CheckVideoOutput()
        {
            if(File.Exists(MainWindow.videoOutput)) { File.Delete("unfinishedjob.xml"); return true; } else { MessageBox.Show("No Output File found!"); return false; }
        }

        public static bool CheckAudioOutput()
        {
            if (MainWindow.audioEncoding)
            {
                if (File.Exists(Path.Combine(MainWindow.tempPath, "AudioEncoded", "audio.mkv")))
                { return true; }
                else { return false; }
            }else { return true; }
        }

        public static bool CheckFileFolder()
        {
            try { 
                if (!Directory.EnumerateFiles(Path.Combine(MainWindow.tempPath, "Chunks")).Any()) 
                { return true; } else { return false; } 
            } catch { return true; }
            
        }

        public static void DeleteChunkFolderContent()
        {
            try
            {
                //Deletes all Files in & above Chunk Folder
                DirectoryInfo tmp = new DirectoryInfo(Path.Combine(MainWindow.tempPath, "Chunks"));
                DirectoryInfo tmp2 = new DirectoryInfo(MainWindow.tempPath);
                foreach (FileInfo file in tmp.GetFiles()) { file.Delete(); }
                foreach (FileInfo file in tmp2.GetFiles()) { file.Delete(); }
            }
            catch (IOException ex) { MessageBox.Show(ex.Message); }

        }

        public static void DeleteTempFiles()
        {
            try
            {
                DirectoryInfo tmp = new DirectoryInfo(MainWindow.tempPath);
                tmp.Delete(true);
            }
            catch (IOException ex) { MessageBox.Show("Could not delete all files: " + ex.Message); }
        }

        public static void PlayFinishedSound()
        {
            SoundPlayer playSound = new SoundPlayer(Properties.Resources.finished);
            playSound.Play();
        }

        public static bool ExistsOnPath(string fileName)
        {
            return GetFullPath(fileName) != null;
        }

        public static string GetFullPath(string fileName)
        {
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);

            var values = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in values.Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            return null;
        }

        public static string GetFullPathWithOutName(string fileName)
        {
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);

            var values = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in values.Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                    return path;
            }
            return null;
        }
    }
}
