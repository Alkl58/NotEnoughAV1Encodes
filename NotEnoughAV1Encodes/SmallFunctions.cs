using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Windows;

namespace NotEnoughAV1Encodes
{
    internal class SmallFunctions
    {
        public static void ExecuteFfmpegTask(string ffmpegCommand)
        {
            //Run ffmpeg command
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                WorkingDirectory = Global.FFmpeg_Path,
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
                WorkingDirectory = Global.MKVToolNix_Path,
                Arguments = mkvmergeCommand
            };
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
        }

        public static void GetSourceFrameCount(string source)
        {
            // Skip Framecount Calculation if it already "exists" (Resume Mode)
            if (File.Exists(Path.Combine(Global.temp_path, Global.temp_path_folder, "framecount.log")) == false)
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
                        WorkingDirectory = Global.FFmpeg_Path,
                        Arguments = "/C ffmpeg.exe -i " + '\u0022' + source + '\u0022' + " -hide_banner -loglevel 32 -map 0:v:0 -f null -",
                        RedirectStandardError = true,
                        RedirectStandardOutput = true
                    }
                };
                process.Start();
                string stream = process.StandardError.ReadToEnd();
                process.WaitForExit();
                string tempStream = stream.Substring(stream.LastIndexOf("frame="));
                string data = GetBetween(tempStream, "frame=", "fps=");
                MainWindow.TotalFrames = int.Parse(data);
                Helpers.WriteToFileThreadSafe(data, Path.Combine(Global.temp_path, Global.temp_path_folder, "framecount.log"));
            }
            else
            {
                // Reads the first line of the framecount file
                MainWindow.TotalFrames = int.Parse(File.ReadLines(Path.Combine(Global.temp_path, Global.temp_path_folder, "framecount.log")).First());
            }
        }

        public static string GetBetween(string strSource, string strStart, string strEnd)
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
            if (File.Exists(Global.Video_Output))
            {
                FileInfo VideoOutput = new FileInfo(Global.Video_Output);
                if (VideoOutput.Length <= 50000)
                {
                    MessageBox.Show("Video Output is " + (VideoOutput.Length / 1000) + "KB.\nThere could be a muxing error.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    // Deletes Temp Files only if output exists which is bigger than 50KB
                    DeleteTempFiles();
                }
            }
            else
            {
                MessageBoxResult Result = MessageBox.Show("Muxing failed. Video output not detected!\nCommon issues:\n- Video is interlaced, please enable deinterlace filter\n- Missing dependencies\n- Video stream has broken parts\n- Incorrect encoding commands\n\nOpen Log File?", "Error", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (Result == MessageBoxResult.Yes)
                {
                    try
                    {
                        Process.Start(Global.Video_Output + ".log");
                    }
                    catch { }
                }
            }
        }

        public static void DeleteTempFiles()
        {
            // Deletes Temp Files
            try
            {
                if (MainWindow.DeleteTempFiles)
                {
                    DirectoryInfo tmp = new DirectoryInfo(Path.Combine(Global.temp_path, Global.temp_path_folder));
                    tmp.Delete(true);
                }
            }
            catch { }
        }

        public static void DeleteTempFilesButton()
        {
            // Deletes Temp Files
            try
            {
                DirectoryInfo tmp = new DirectoryInfo(Path.Combine(Global.temp_path));
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
    }
}