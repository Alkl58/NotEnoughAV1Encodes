﻿using System;
using System.IO;

namespace NotEnoughAV1Encodes
{
    class CheckDependencies
    {
        public static void Check()
        {
            // Sets / Checks ffmpeg Path
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "ffmpeg.exe"))) { MainWindow.FFmpegPath = Directory.GetCurrentDirectory(); }
            else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Apps", "ffmpeg", "ffmpeg.exe"))) { MainWindow.FFmpegPath = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "ffmpeg"); }
            else if (ExistsOnPath("ffmpeg.exe")) { MainWindow.FFmpegPath = GetFullPathWithOutName("ffmpeg.exe"); }

            // Sets / Checks aomenc Path
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "aomenc.exe"))) { MainWindow.AomencPath = Directory.GetCurrentDirectory(); }
            else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Apps", "Encoder", "aomenc.exe"))) { MainWindow.AomencPath = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "Encoder"); }
            else if (ExistsOnPath("aomenc.exe")) { MainWindow.AomencPath = GetFullPathWithOutName("aomenc.exe"); }

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