using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotEnoughAV1Encodes
{
    class FFprobe
    {
        public static string GetPixelFormat(string videoInput)
        {
            Process getPixelFormat = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    WorkingDirectory = MainWindow.FFmpegPath,
                    Arguments = "/C ffprobe.exe -i " + '\u0022' + videoInput + '\u0022' + " -v error -select_streams v -of default=noprint_wrappers=1:nokey=1 -show_entries stream=pix_fmt",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            getPixelFormat.Start();
            string pixfmt = getPixelFormat.StandardOutput.ReadLine();
            getPixelFormat.WaitForExit();
            return pixfmt;
        }

        public static string GetFrameRate(string videoInput)
        {
            Process getStreamFps = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    WorkingDirectory = MainWindow.FFmpegPath,
                    Arguments = "/C ffprobe.exe -i " + '\u0022' + videoInput + '\u0022' + " -v error -select_streams v -of default=noprint_wrappers=1:nokey=1 -show_entries stream=r_frame_rate",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            getStreamFps.Start();
            string framerate = getStreamFps.StandardOutput.ReadLine();
            getStreamFps.WaitForExit();
            return framerate;
        }

        public static string GetResolution(string videoInput)
        {
            Process getResolution = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    WorkingDirectory = MainWindow.FFmpegPath,
                    Arguments = "/C ffprobe.exe -i " + '\u0022' + videoInput + '\u0022' + " -v error -select_streams v:0 -of csv=p=0 -show_entries stream=width,height",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            getResolution.Start();
            string resolution = getResolution.StandardOutput.ReadLine();
            getResolution.WaitForExit();
            resolution = resolution.Replace(",", "x");
            return resolution;
        }
    }
}
