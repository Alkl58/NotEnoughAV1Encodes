using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotEnoughAV1Encodes
{
    class Subtitle
    {
        public static void EncSubtitles()
        {
            //Creates Subtitle Directory in the temp dir
            if (!Directory.Exists(Path.Combine(MainWindow.workingTempDirectory, "Subtitles")))
                Directory.CreateDirectory(Path.Combine(MainWindow.workingTempDirectory, "Subtitles"));

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.UseShellExecute = true;
            startInfo.FileName = "cmd.exe";
            startInfo.WorkingDirectory = MainWindow.exeffmpegPath + "\\";
            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + MainWindow.videoInput + '\u0022' + " -vn -an -dn -c copy " + '\u0022' + MainWindow.workingTempDirectory + "\\Subtitles\\subtitle.mkv" + '\u0022';
            process.StartInfo = startInfo;
            //Console.WriteLine(startInfo.Arguments);
            process.Start();
            process.WaitForExit();
        }

    }
}
