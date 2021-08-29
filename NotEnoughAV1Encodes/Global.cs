using System;
using System.IO;

namespace NotEnoughAV1Encodes
{
    class Global
    {
        public static string AppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public static string Temp = Path.GetTempPath();
    }
}
