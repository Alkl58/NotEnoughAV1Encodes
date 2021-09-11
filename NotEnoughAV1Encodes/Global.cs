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
    }
}
