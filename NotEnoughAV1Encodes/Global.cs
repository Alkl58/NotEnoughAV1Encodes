using System.Collections.Generic;
using System.IO;

namespace NotEnoughAV1Encodes
{
    class Global
    {
        // Root Temp Folder Path
        public static string temp_path = Path.Combine(Path.GetTempPath(), "NEAV1E");
        public static string temp_path_folder = null;

        // Dependecie Paths
        public static string FFmpeg_Path = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "ffmpeg", "ffmpeg.exe");
        public static string MKVToolNix_Path = null;

        // Video Input
        public static string Video_Path = "";

        // Video Output
        public static string Video_Output = "";

        // Video Chunks
        public static string[] Video_Chunks;

        // Current Active PIDs
        //public static int[] Launched_PIDs;
        public static List<int> Launched_PIDs = new List<int>();

        // Total Frame Count
        public static int Frame_Count = 0;
    }
}
