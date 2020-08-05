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
            MessageBox.Show("It seems that you don't have 7zip installed. Please install 7zip to use the update functionality!", "7zip", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void MessageSpaceOnDrive()
        {
            MessageBox.Show("It seems that you have less than 50GB Free space. \nDepending on the Content Length and the Splitting Method, it is recommended to have at least 100GB excluding some extra spare space.\nThis program is not stopping you from Encoding, regardless of this message. \nYou can also select a custom Temp Path on another Drive if you want.", "Free Space", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
