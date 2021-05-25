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
            string cmd = "/C ffprobe.exe -i " + '\u0022' + videoInput + '\u0022' + " -v error -select_streams v -of default=noprint_wrappers=1:nokey=1 -show_entries stream=pix_fmt";
            return FFprobeExe(cmd);
        }

        public static string GetFrameRate(string videoInput)
        {
            string cmd = "/C ffprobe.exe -i " + '\u0022' + videoInput + '\u0022' + " -v error -select_streams v -of default=noprint_wrappers=1:nokey=1 -show_entries stream=r_frame_rate";
            return FFprobeExe(cmd);
        }

        public static string GetResolution(string videoInput)
        {
            string cmd = "/C ffprobe.exe -i " + '\u0022' + videoInput + '\u0022' + " -v error -select_streams v:0 -of csv=p=0 -show_entries stream=width,height";
            string resolution = FFprobeExe(cmd);
            resolution = resolution.Replace(",", "x");
            return resolution;
        }

        private static string FFprobeExe(string command)
        {
            Process ffprobe = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    WorkingDirectory = Global.FFmpeg_Path,
                    Arguments = command,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            ffprobe.Start();
            string resolution = ffprobe.StandardOutput.ReadLine();
            ffprobe.WaitForExit();
            return resolution;
        }
    }
}
