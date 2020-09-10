using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotEnoughAV1Encodes
{
    class Ffprobe
    {
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
                    WorkingDirectory = MainWindow.ffprobePath,
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

        public static string GetAudioInfo(string videoInput)
        {
            Process getAudioInfo = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    WorkingDirectory = MainWindow.ffprobePath,
                    Arguments = "/C ffprobe.exe -i " + '\u0022' + videoInput + '\u0022' + " -v error -select_streams a:0 -show_entries stream=codec_name -of default=noprint_wrappers=1:nokey=1",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            getAudioInfo.Start();
            string audio = getAudioInfo.StandardOutput.ReadLine();
            getAudioInfo.WaitForExit();
            return audio;
        }

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
                    WorkingDirectory = MainWindow.ffprobePath,
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

        public static string GetVideoLength(string videoInput)
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    WorkingDirectory = MainWindow.ffprobePath,
                    Arguments = "/C ffprobe.exe -i " + '\u0022' + videoInput + '\u0022' + " -show_entries format=duration -v quiet -of csv=" + '\u0022' + "p=0" + '\u0022',
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            process.Start();
            string stream = process.StandardOutput.ReadLine();
            string value = new DataTable().Compute(stream, null).ToString();
            process.WaitForExit();
            return Convert.ToInt64(Math.Round(Convert.ToDouble(value))).ToString();
        }

        public static string GetVideoLengthAccurate(string videoInput)
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    WorkingDirectory = MainWindow.ffprobePath,
                    Arguments = "/C ffprobe.exe -i " + '\u0022' + videoInput + '\u0022' + " -v error -sexagesimal -show_entries format=duration -of default=noprint_wrappers=1:nokey=1",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            process.Start();
            string stream = "0";
            stream += process.StandardOutput.ReadLine();
            process.WaitForExit();
            stream = stream.Substring(0, (stream.Length - 6));
            stream += "000";
            return stream;
        }
    }
}
