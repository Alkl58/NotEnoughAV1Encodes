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
    }
}
