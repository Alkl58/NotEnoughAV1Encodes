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
    }
}
