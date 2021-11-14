using System;
using System.Collections.Generic;
using System.IO;

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

        public static void Logger(string logMessage, string logPath)
        {
            // We could use a better logging method with different logging levels
            // However for this "small" application this is enough

            using StreamWriter sw = new(logPath, true);
            sw.WriteLine($"{DateTime.Now} : {logMessage}");
            sw.Close();
        }
    }
}
