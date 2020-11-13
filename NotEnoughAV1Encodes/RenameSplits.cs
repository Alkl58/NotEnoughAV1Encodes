using System.IO;
using System.Linq;

namespace NotEnoughAV1Encodes
{
    class RenameSplits
    {
        public static void Rename()
        {
            string[] chunks;
            string chunkDir = Path.Combine(MainWindow.TempPath, MainWindow.TempPathFileName, "Chunks");
            chunks = Directory.GetFiles(chunkDir, "*mkv", SearchOption.AllDirectories).Select(x => Path.GetFileName(x)).ToArray();
            DirectoryInfo d = new DirectoryInfo(chunkDir);
            FileInfo[] infos = d.GetFiles();
            int numberOfChunks = chunks.Count();

            foreach (FileInfo f in infos)
            {
                int count = f.ToString().Count();
                switch (count)
                {
                    case 9:
                        File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("split", "split00000"));
                        break;
                    case 10:
                        File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("split", "split0000"));
                        break;
                    case 11:
                        File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("split", "split000"));
                        break;
                    case 12:
                        File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("split", "split00"));
                        break;
                    case 13:
                        File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("split", "split0"));
                        break;
                }
            }
        }
    }
}
