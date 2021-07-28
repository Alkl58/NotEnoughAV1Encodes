using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;

namespace NotEnoughAV1Encodes
{
    internal class Helpers
    {
        public static BitmapImage Get_Uri_Source(string name)
        {
            // Returns lokal Bitmap Uri Images for UI usage
            var uriSource = new Uri(@"/NotEnoughAV1Encodes;component/img/" + name, UriKind.Relative);
            return new BitmapImage(uriSource);
        }

        public static void Create_Temp_Folder(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public static void Logging(string log)
        {
            // Logging Function
            if (MainWindow.Logging)
            {
                DateTime starttime = DateTime.Now;
                WriteToFileThreadSafe(starttime.ToString() + " : " + log, Global.Video_Output + ".log");
            }
        }

        public static int GetCoreCount()
        {
            // Gets Core Count
            int coreCount = 0;
            foreach (var item in new System.Management.ManagementObjectSearcher("Select * from Win32_Processor").Get())
            {
                coreCount += int.Parse(item["NumberOfCores"].ToString());
            }
            return coreCount;
        }

        public static void Check_Unicode(string file_name)
        {
            // This function checks if the provided video file has compatible unicode characters in the filename
            // Reference: codesnippets.fesslersoft.de/how-to-check-if-a-string-is-unicode-in-c-and-vb-net/
            var asciiBytesCount = Encoding.ASCII.GetByteCount(file_name);
            var unicodBytesCount = Encoding.UTF8.GetByteCount(file_name);
            if (asciiBytesCount != unicodBytesCount)
            {
                MessageBox.Show("The filename contains non unicode characters.\n\nPlease rename your file before proceeding to guarantee a successful encode!");
            }
        }

        private static ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();

        public static void WriteToFileThreadSafe(string text, string path)
        {
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