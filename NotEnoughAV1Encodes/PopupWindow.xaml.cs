using ControlzEx.Theming;
using MahApps.Metro.Controls;
using System.IO;

namespace NotEnoughAV1Encodes
{
    public partial class PopupWindow : MetroWindow
    {
        public static string fileLocation;
        public PopupWindow(string baseTheme, string accentTheme, string time, string frames, string avg, string fileName)
        {
            InitializeComponent();
            ThemeManager.Current.ChangeTheme(this, baseTheme + "." + accentTheme);
            LabelElapsedTime.Content = time;
            LabelEncodedFrames.Content = frames;
            LabelEncodedAVG.Content = avg;
            FileInfo fs = new FileInfo(fileName);
            fileLocation = fileName;
            LabelEncodedFilesize.Content = (fs.Length >> 20) + "MB";
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Closes this window
            this.Close();
        }

        private void Button_Click_1(object sender, System.Windows.RoutedEventArgs e)
        {
            // Opens the video output file location and select the video file
            System.Diagnostics.Process.Start("explorer.exe", "/select, \"" + fileLocation + "\"");
        }
    }
}
