using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace NotEnoughAV1Encodes
{
    internal class Global
    {
        public static string AppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public static string Temp = Path.GetTempPath();

        // Current Active PIDs
        public static List<int> LaunchedPIDs = new();

        public static int GetTotalFramesProcessed(string stderr)
        {
            try
            {
                if (stderr.Contains("frame="))
                {
                    int Start, End;
                    Start = stderr.IndexOf("frame=", 0) + "frame=".Length;
                    End = stderr.IndexOf("fps=", Start);
                    return int.Parse(stderr[Start..End]);
                }
            }
            catch { }

            return 0;
        }

        public static int GetTotalTimeProcessed(string stderr, Queue.QueueElement queue)
        {
            try
            {
                if (stderr.Contains("time="))
                {
                    // Get Timespan of Video
                    TimeSpan length = TimeSpan.Parse(queue.VideoDB.MIDuration);

                    // Parse stderr Output of FFmpeg
                    int Start, End;
                    Start = stderr.IndexOf("time=", 0) + "time=".Length;
                    End = stderr.IndexOf("bitrate=", Start);
                    string ffmpegTime = stderr[Start..End];

                    // Convert FFmpeg time to Timespan
                    TimeSpan ts = TimeSpan.Parse(ffmpegTime);

                    // Progress in Percent
                    double prog = Math.Round(ts / length, 2) * 100;

                    // Convert Progress to amount of Frames (roughly)
                    int frameCount = Convert.ToInt32(prog) * (Convert.ToInt32(queue.VideoDB.MIFrameCount) / 100);

                    return frameCount;
                }
            }
            catch { }

            return 0;
        }

        private static readonly ReaderWriterLockSlim readWriteLock = new();
        public static void Logger(string logMessage, string logPath)
        {
            // We could use a better logging method with different logging levels
            // However for this "small" application this is enough

            if(MainWindow.Logging == false)
            {
                return;
            }

            // Set Status to Locked
            readWriteLock.EnterWriteLock();
            try
            {
                using StreamWriter sw = new(logPath, true);
                sw.WriteLine($"{DateTime.Now} : {logMessage}");
                sw.Close();
            }
            finally
            {
                // Release Lock
                readWriteLock.ExitWriteLock();
            }
        }
    }
}
