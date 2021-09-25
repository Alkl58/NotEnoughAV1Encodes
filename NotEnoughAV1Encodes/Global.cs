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
    }
}
