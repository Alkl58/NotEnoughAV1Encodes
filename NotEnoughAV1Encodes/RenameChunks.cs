using System.IO;
using System.Linq;

namespace NotEnoughAV1Encodes
{
    class RenameChunks
    {
        //Renames all Chunk files accordingly, so while muxing they are in the right order
        public static void Rename()
        {
            if (SmallFunctions.Cancel.CancelAll == false)
            {
                SmallFunctions.Logging("Rename Chunks");
                string[] chunks;
                string chunkDir = Path.Combine(MainWindow.tempPath, "Chunks");
                //Add all Files in Chunks Folder to array
                chunks = Directory.GetFiles(chunkDir, "*mkv", SearchOption.AllDirectories).Select(x => Path.GetFileName(x)).ToArray();
                DirectoryInfo d = new DirectoryInfo(chunkDir);
                FileInfo[] infos = d.GetFiles();
                int numberOfChunks = chunks.Count();
                SmallFunctions.Logging("Rename Chunks Count: " + numberOfChunks);
                if (numberOfChunks >= 10 && numberOfChunks <= 99) { foreach (FileInfo f in infos) { int count = f.ToString().Count(); if (count == 8) { File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out0")); } } }
                else if (numberOfChunks >= 100 && numberOfChunks <= 999) //If you have more than 100 Chunks and less than 999
                {
                    foreach (FileInfo f in infos)
                    {
                        int count = f.ToString().Count();
                        switch (count)
                        {
                            case 8:
                                File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out00"));
                                break;
                            case 9:
                                File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out0"));
                                break;
                            default:
                                break;
                        }
                    }
                }
                else if (numberOfChunks >= 1000 && numberOfChunks <= 9999) //If you have more than 1.000 Chunks and less than 9.999
                {
                    foreach (FileInfo f in infos)
                    {
                        int count = f.ToString().Count();
                        switch (count)
                        {
                            case 8:
                                File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out000"));
                                break;
                            case 9:
                                File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out00"));
                                break;
                            case 10:
                                File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out0"));
                                break;
                            default:
                                break;
                        }
                    }
                }
                else if (numberOfChunks >= 10000 && numberOfChunks <= 99999) //If you have more than 10.000 Chunks and less than 99.999
                {
                    foreach (FileInfo f in infos)
                    {
                        int count = f.ToString().Count();
                        switch (count)
                        {
                            case 8:
                                File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out0000"));
                                break;
                            case 9:
                                File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out000"));
                                break;
                            case 10:
                                File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out00"));
                                break;
                            case 11:
                                File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out0"));
                                break;
                        }
                    }
                }
                else if (numberOfChunks >= 100000 && numberOfChunks <= 999999) //If you have more than 100.000 Chunks and less than 999.999
                {
                    foreach (FileInfo f in infos)
                    {
                        int count = f.ToString().Count();
                        switch (count)
                        {
                            case 8:
                                File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out00000"));
                                break;
                            case 9:
                                File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out0000"));
                                break;
                            case 10:
                                File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out000"));
                                break;
                            case 11:
                                File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out00"));
                                break;
                            case 12:
                                File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out0"));
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }
    }
}
