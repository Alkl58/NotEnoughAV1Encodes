using System.IO;
using System.Linq;

namespace NotEnoughAV1Encodes
{
    class RenameSplits
    {
        public static void Rename()
        {
            // Sets the Path of the Directory with the Video Chunks
            string chunkDir = Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Chunks");

            // Creates a new directory info for the dir with the chunks
            DirectoryInfo fileDir = new DirectoryInfo(chunkDir);

            // Creates a FileInfo Array with the files from the DirectoryInfo
            FileInfo[] infos = fileDir.GetFiles();

            // Iterates over all files in Chunk Directory
            foreach (FileInfo file in infos)
            {
                // Gets the length of the filename
                int count = file.ToString().Count();

                // Depending on the file length it will rename them
                switch (count)
                {
                    case 9: File.Move(fileDir + "\\" + file, fileDir + "\\" + file.Name.Replace("split", "split00000")); break;
                    case 10: File.Move(fileDir + "\\" + file, fileDir + "\\" + file.Name.Replace("split", "split0000")); break;
                    case 11: File.Move(fileDir + "\\" + file, fileDir + "\\" + file.Name.Replace("split", "split000")); break;
                    case 12: File.Move(fileDir + "\\" + file, fileDir + "\\" + file.Name.Replace("split", "split00")); break;
                    case 13: File.Move(fileDir + "\\" + file, fileDir + "\\" + file.Name.Replace("split", "split0")); break;
                }
            }
        }
    }
}
