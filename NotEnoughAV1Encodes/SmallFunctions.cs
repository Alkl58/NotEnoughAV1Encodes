using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace NotEnoughAV1Encodes
{
    class SmallFunctions
    {
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

        public static void GetSourceFrameCount()
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
                    Arguments = "/C ffmpeg.exe -i " + '\u0022' + MainWindow.VideoInput + '\u0022' + " -hide_banner -loglevel 32 -map 0:v:0 -c copy -f null -",
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
    }
}
