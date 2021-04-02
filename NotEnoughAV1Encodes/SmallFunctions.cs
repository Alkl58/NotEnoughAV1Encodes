using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Windows;

namespace NotEnoughAV1Encodes
{
    class SmallFunctions
    {
        public static void CheckUnicode(string fileName)
        {
            // This function checks if the provided video file has compatible unicode characters in the filename
            // Reference: codesnippets.fesslersoft.de/how-to-check-if-a-string-is-unicode-in-c-and-vb-net/
            var asciiBytesCount = Encoding.ASCII.GetByteCount(fileName);
            var unicodBytesCount = Encoding.UTF8.GetByteCount(fileName);
            if (asciiBytesCount != unicodBytesCount)
            {
                MessageBox.Show("The filename contains non unicode characters.\n\nPlease rename your file before proceeding to guarantee a successful encode!", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public static int getCoreCount()
        {
            // Gets Core Count
            int coreCount = 0;
            foreach (var item in new System.Management.ManagementObjectSearcher("Select * from Win32_Processor").Get())
            { 
                coreCount += int.Parse(item["NumberOfCores"].ToString()); 
            }
            return coreCount;
        }
        public static void setVideoChunks(int SplitMethod)
        {
            // This function sets the array of videochunks or commands
            // The VideoEncode() function will iterate over it to "encode them"

            if (SplitMethod == 0 || SplitMethod == 1)
            {
                // Scene based splitting
                if (File.Exists(Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "splits.txt")))
                    MainWindow.VideoChunks = File.ReadAllLines(Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "splits.txt")); // Reads the split file for VideoEncode() function
            }
            else if (SplitMethod == 2)
            {
                // Chunk based splitting
                MainWindow.VideoChunks = Directory.GetFiles(Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Chunks"), "*mkv", SearchOption.AllDirectories).Select(x => Path.GetFileName(x)).ToArray();
            }
        }

        private static ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();
        public static void WriteToFileThreadSafe(string text, string path)
        {
            // Some smaller Blackmagic, so parallel Workers won't deadlock files
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

        public static void Logging(string log)
        {
            // Logging Function
            if (MainWindow.Logging)
            {
                DateTime starttime = DateTime.Now;
                if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Logging")))
                    Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Logging"));
                WriteToFileThreadSafe(starttime.ToString() + " : " + log, Path.Combine(Directory.GetCurrentDirectory(), "Logging", "program.log"));
            }
        }

        public static void ExecuteFfmpegTask(string ffmpegCommand)
        {
            //Run ffmpeg command
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                WorkingDirectory = MainWindow.FFmpegPath,
                Arguments = ffmpegCommand
            };
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
        }

        public static void ExecuteMKVMergeTask(string mkvmergeCommand)
        {
            //Run ffmpeg command
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                WorkingDirectory = MainWindow.MKVToolNixPath,
                Arguments = mkvmergeCommand
            };
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
        }

        public static void GetSourceFrameCount(string source)
        {
            // Skip Framecount Calculation if it already "exists" (Resume Mode)
            if (File.Exists(Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "framecount.log")) == false)
            {
                // This function calculates the total number of frames
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = "cmd.exe",
                        WorkingDirectory = MainWindow.FFmpegPath,
                        Arguments = "/C ffmpeg.exe -i " + '\u0022' + source + '\u0022' + " -hide_banner -loglevel 32 -map 0:v:0 -f null -",
                        RedirectStandardError = true,
                        RedirectStandardOutput = true
                    }
                };
                process.Start();
                string stream = process.StandardError.ReadToEnd();
                process.WaitForExit();
                string tempStream = stream.Substring(stream.LastIndexOf("frame="));
                string data = getBetween(tempStream, "frame=", "fps=");
                MainWindow.TotalFrames = int.Parse(data);
                WriteToFileThreadSafe(data, Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "framecount.log"));
            }
            else
            {
                // Reads the first line of the framecount file
                MainWindow.TotalFrames = int.Parse(File.ReadLines(Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "framecount.log")).First());
            }

        }

        public static string getBetween(string strSource, string strStart, string strEnd)
        {
            // This function parses data between two points
            if (strSource.Contains(strStart) && strSource.Contains(strEnd))
            {
                int Start, End;
                Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                End = strSource.IndexOf(strEnd, Start);
                return strSource.Substring(Start, End - Start);
            }
            return "";
        }

        public static void CheckVideoOutput()
        {
            // This checks if the video muxer created an output file
            if (File.Exists(MainWindow.VideoOutput))
            {
                FileInfo VideoOutput = new FileInfo(MainWindow.VideoOutput);
                if (VideoOutput.Length <= 50000)
                {
                    MessageBox.Show("Video Output is " + (VideoOutput.Length /1000) + "KB.\nThere could be a muxing error.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    // Deletes Temp Files only if output exists which is bigger than 50KB
                    DeleteTempFiles();
                }
            }
            else
            {
                MessageBox.Show("Muxing failed. Video output not detected!\nCommon issues:\n- Video is interlaced, please enable deinterlace filter\n- Missing dependencies\n- Video stream has broken parts", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public static void KillInstances()
        {
            //Kills all ffmpeg instances
            try
            {
                foreach (var process in Process.GetProcessesByName("aomenc")) { process.Kill(); }
                foreach (var process in Process.GetProcessesByName("rav1e")) { process.Kill(); }
                foreach (var process in Process.GetProcessesByName("SvtAv1EncApp")) { process.Kill(); }
                foreach (var process in Process.GetProcessesByName("ffmpeg")) { process.Kill(); }
            }
            catch { }
        }

        public static void DeleteTempFiles()
        {
            // Deletes Temp Files
            try
            {
                if (MainWindow.DeleteTempFiles == true)
                {
                    DirectoryInfo tmp = new DirectoryInfo(Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName));
                    tmp.Delete(true);
                }
            }
            catch {  }
        }

        public static void DeleteTempFilesButton()
        {
            // Deletes Temp Files
            try
            {
                DirectoryInfo tmp = new DirectoryInfo(Path.Combine(MainWindow.TempPath));
                tmp.Delete(true);
            }
            catch { }
        }

        public static class Cancel
        {
            //Public Cancel boolean
            public static bool CancelAll = false;
        }

        public static void PlayFinishedSound()
        {
            // Plays a sound when program has finished encoding / muxing
            if (MainWindow.PlayUISounds == true)
            {
                SoundPlayer playSound = new SoundPlayer(Properties.Resources.finished);
                playSound.Play();
            }
        }

        public static void PlayStopSound()
        {
            // Plays a sound when program has finished encoding / muxing
            if (MainWindow.PlayUISounds == true)
            {
                SoundPlayer playSound = new SoundPlayer(Properties.Resources.stop);
                playSound.Play();
            }
        }

        public static bool CheckFileType(string fileName)
        {
            // Checks if the input is a video (batch encoding)
            string ext = Path.GetExtension(fileName);
            string[] exts = { ".mp4", ".m4v", ".mkv", ".webm", ".m2ts", ".flv", ".avi", ".wmv", ".ts", ".yuv", ".mov" };
            return exts.Contains(ext.ToLower());
        }

        public static string GetVideoLengthAccurate(string videoInput)
        {
            // Gets the Video Length for trimming purposes
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    WorkingDirectory = MainWindow.FFmpegPath,
                    Arguments = "/C ffprobe.exe -i " + '\u0022' + videoInput + '\u0022' + " -v error -sexagesimal -show_entries format=duration -of default=noprint_wrappers=1:nokey=1",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            process.Start();
            string stream = "0";
            stream += process.StandardOutput.ReadLine();
            process.WaitForExit();
            stream = stream.Substring(0, (stream.Length - 6));
            stream += "000";
            return stream;
        }
    }
}
