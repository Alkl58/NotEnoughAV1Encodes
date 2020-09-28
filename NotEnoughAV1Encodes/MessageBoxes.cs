using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;

namespace NotEnoughAV1Encodes
{
    class MessageBoxes
    {
        public static void MessageVideoInput()
        {
            MessageBox.Show("Input Video not Set!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public static void MessageVideoOutput()
        {
            MessageBox.Show("Output Video not Set!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public static void MessageVideoBadFramerate()
        {
            MessageBox.Show("It seems that your Video has an unknown framerate. \nPlease make sure that your Video has a constant Framerate!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static void MessageSVTWorkers()
        {
            MessageBox.Show("It is not necessary to have more than one Worker for SVT-AV1. \nIf you really want more Workers check the Program Settings Page.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void MessageHardcodeSubtitles()
        {
            MessageBox.Show("You can only hardcode one subtitle!\nDisable hardcoding to be able to add more subtitles!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void MessageHardcodeSubtitlesCheckBox()
        {
            MessageBox.Show("You can only hardcode one subtitle!\nYou have added too many subtitles.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public static void MessageNoSubtitlesToDelete()
        {
            MessageBox.Show("No Subtitle selected or no subtitles to delete.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void MessageDeinterlacingWithoutReencoding()
        {
            MessageBox.Show("You need reencoding for deinterlacing to work!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void MessageCustomSubtitleBatchMode()
        {
            MessageBox.Show("Custom Subtitles is not available in Batch Encoding Mode!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void MessageCustomSubtitleHardCodeNotSupported()
        {
            MessageBox.Show("The selected subtitle for hardcoding is currently not supported!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void Message7zNotFound()
        {
            if (MessageBox.Show("It seems that you don't have 7zip installed. To use the Updater function 7zip is required. \n\nDownload & Install 7zip now? (~5MB)", "7zip", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    using (WebClient webClient = new WebClient())
                    {
                        webClient.DownloadFile("https://www.7-zip.org/a/7z1900-x64.exe", Path.Combine(Path.GetTempPath(), "7z1900-x64.exe"));
                        Process p = new Process();
                        p.StartInfo.FileName = Path.Combine(Path.GetTempPath(), "7z1900-x64.exe");
                        p.Start();
                        p.WaitForExit();
                        File.Delete(Path.Combine(Path.GetTempPath(), "7z1900-x64.exe"));
                        if (File.Exists(@"C:\Program Files\7-Zip\7zG.exe")) { MainWindow.found7z = true; }
                        DownloadDependencies egg = new DownloadDependencies(true);
                        egg.ShowDialog();
                        MainWindow.setEncoderPath();
                    }
                }
                catch { }
            }
        }

        public static void MessageSpaceOnDrive()
        {
            MessageBox.Show("It seems that you have less than 50GB Free space. \nDepending on the Content Length and the Splitting Method, it is recommended to have at least 100GB excluding some extra spare space.\nThis program is not stopping you from Encoding, regardless of this message. \nYou can also select a custom Temp Path on another Drive if you want.", "Free Space", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void MessagePCMBluray()
        {
            MessageBox.Show("Detected PCM_Bluray Audio Format! \n\nIf you want to Stream Copy, it will reencode to pcm_s16le. \n\nElse feel free to use a lossy format.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void MessageCountNotAvailableWhenTrimming()
        {
            MessageBox.Show("The frame count option is not available when trimming! \n\nFrame counting is now disabled.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void MessageMoreSubtitles()
        {
            MessageBox.Show("More than four subtitles extracted. \nYou can select the other subtitles manually.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
