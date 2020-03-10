using System.IO;
using System.Linq;

namespace NotEnoughAV1Encodes
{
    internal class RenameChunks
    {
        //Renames all Chunk files accordingly, so while muxing they are in the right order
        public static void Rename(string currentPath)
        {
            //Create Array List with all Chunks
            string[] chunks;
            //Sets the Chunks directory
            string sdira = currentPath + "\\Chunks";
            //Add all Files in Chunks Folder to array
            chunks = Directory.GetFiles(sdira, "*mkv", SearchOption.AllDirectories).Select(x => Path.GetFileName(x)).ToArray();
            DirectoryInfo d = new DirectoryInfo(currentPath + "\\Chunks");
            FileInfo[] infos = d.GetFiles();

            int numberOfChunks = chunks.Count();

            //outx.mkv = 8 | outxx.mkv = 9 (99) | outxxx.mkv = 10 (999) | outxxxx.mkv = 11 (9999) | outxxxxx.mkv = 12 (99999)

            //int numberOfChunks = 20000;

            if (numberOfChunks >= 10 && numberOfChunks <= 99)
            {
                foreach (FileInfo f in infos)
                {
                    int count = f.ToString().Count();

                    if (count == 8)
                    {
                        File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out0"));
                    }
                }
            }
            else if (numberOfChunks >= 100 && numberOfChunks <= 999) //If you have more than 100 Chunks and less than 999
            {
                foreach (FileInfo f in infos)
                {
                    int count = f.ToString().Count();

                    if (count == 8)
                    {
                        File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out00"));
                    }

                    if (count == 9)
                    {
                        File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out0"));
                    }
                }
            }
            else if (numberOfChunks >= 1000 && numberOfChunks <= 9999) //If you have more than 1.000 Chunks and less than 9.999
            {
                foreach (FileInfo f in infos)
                {
                    int count = f.ToString().Count();

                    if (count == 8)
                    {
                        File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out000"));
                    }

                    if (count == 9)
                    {
                        File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out00"));
                    }

                    if (count == 10)
                    {
                        File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out0"));
                    }
                }
            }
            else if (numberOfChunks >= 10000 && numberOfChunks <= 99999) //If you have more than 10.000 Chunks and less than 99.999
            {
                foreach (FileInfo f in infos)
                {
                    int count = f.ToString().Count();

                    if (count == 8)
                    {
                        File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out0000"));
                    }

                    if (count == 9)
                    {
                        File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out000"));
                    }

                    if (count == 10)
                    {
                        File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out00"));
                    }

                    if (count == 11)
                    {
                        File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out0"));
                    }
                }
            }
            else if (numberOfChunks >= 100000 && numberOfChunks <= 999999)
            {
                foreach (FileInfo f in infos)
                {
                    int count = f.ToString().Count();
                    //If you have more than 100.000 Chunks and less than 999.999
                    //BTW are fu*** insane?
                    if (count == 8)
                    {
                        File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out00000"));
                    }

                    if (count == 9)
                    {
                        File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out0000"));
                    }

                    if (count == 10)
                    {
                        File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out000"));
                    }

                    if (count == 11)
                    {
                        File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out00"));
                    }
                    if (count == 12)
                    {
                        File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out0"));
                    }
                }
            }
        }
    }
}