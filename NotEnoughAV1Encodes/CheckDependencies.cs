using System;
using System.IO;
using System.Windows;

namespace NotEnoughAV1Encodes
{
    class CheckDependencies
    {
        public static void Check()
        {
            // Sets / Checks ffmpeg Path
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Apps", "ffmpeg", "ffmpeg.exe"))) 
            { 
                Global.FFmpeg_Path = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "ffmpeg"); 
            }
            else if (ExistsOnPath("ffmpeg.exe")) {
                Global.FFmpeg_Path = GetFullPathWithOutName("ffmpeg.exe"); 
            }
            else 
            {
                Global.FFmpeg_Path = null; 
            }

            // Sets / Checks mkvtoolnix Path
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Apps", "mkvtoolnix", "mkvmerge.exe"))) 
            { 
                Global.MKVToolNix_Path = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "mkvtoolnix"); 
            }
            else if (ExistsOnPath("mkvmerge.exe")) 
            {
                Global.MKVToolNix_Path = GetFullPathWithOutName("mkvmerge.exe"); 
            }
            else if (File.Exists(@"C:\Program Files\MKVToolNix\mkvmerge.exe")) 
            {
                Global.MKVToolNix_Path = @"C:\Program Files\MKVToolNix\"; 
            }
            else 
            {
                Global.MKVToolNix_Path = null; 
            }

            // Checks if PySceneDetect is found in the Windows PATH environment
            if (ExistsOnPath("scenedetect.exe")) { MainWindow.PySceneFound = true; }
            
            NotifyUser();
        }

        public static void NotifyUser()
        {
            if (Global.FFmpeg_Path == null)
            {
                if (MessageBox.Show("Could not find ffmpeg!\nOpen Updater?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    Updater updater = new Updater("light", "blue");
                    updater.ShowDialog();
                    Check();
                }
            }
        }

        private static bool ExistsOnPath(string fileName)
        {
            // Checks if file exists in PATH Environment
            return GetFullPath(fileName) != null;
        }

        private static string GetFullPath(string fileName)
        {
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);

            var values = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in values.Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            return null;
        }

        private static string GetFullPathWithOutName(string fileName)
        {
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);

            var values = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in values.Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                    return path; // Returns the PATH without Filename
            }
            return null;
        }
    }
}
