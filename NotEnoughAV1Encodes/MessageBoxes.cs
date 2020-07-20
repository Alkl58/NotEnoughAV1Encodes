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
    }
}
