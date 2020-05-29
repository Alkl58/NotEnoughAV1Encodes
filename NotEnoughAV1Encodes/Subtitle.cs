using System.IO;

namespace NotEnoughAV1Encodes
{
    internal class Subtitle
    {
        public static void EncSubtitles()
        {
            SmallScripts.Logging("Landed in Subtitle Class.");
            //Creates Subtitle Directory in the temp dir
            if (!Directory.Exists(Path.Combine(MainWindow.workingTempDirectory, "Subtitles")))
                Directory.CreateDirectory(Path.Combine(MainWindow.workingTempDirectory, "Subtitles"));

            if (MainWindow.subtitleStreamCopy == true)
            {
                string subtitleCommand = "/C ffmpeg.exe -i " + '\u0022' + MainWindow.videoInput + '\u0022' + " -vn -an -dn -c copy " + '\u0022' + MainWindow.workingTempDirectory + "\\Subtitles\\subtitle.mkv" + '\u0022';
                SmallScripts.Logging("Subtitle Class Executing Command (StreamCopy) : " + subtitleCommand);
                SmallScripts.ExecuteFfmpegTask(subtitleCommand);
            }

            if (MainWindow.subtitleCustom == true)
            {
                string subtitleMapping = "";
                string subtitleInput = "";
                int subtitleAmount = 0;

                foreach (var items in MainWindow.SubtitleChunks)
                {
                    subtitleInput += " -i " + '\u0022' + items + '\u0022';
                    subtitleMapping += " -map " + subtitleAmount;
                    subtitleAmount += 1;
                }

                string subtitleCommand = "/C ffmpeg.exe" + subtitleInput + " -vn -an -dn -c copy " + subtitleMapping + " " + '\u0022' + MainWindow.workingTempDirectory + "\\Subtitles\\subtitlecustom.mkv" + '\u0022';
                SmallScripts.Logging("Subtitle Class Executing Command (Custom) : " + subtitleCommand);
                SmallScripts.ExecuteFfmpegTask(subtitleCommand);
            }
        }
    }
}