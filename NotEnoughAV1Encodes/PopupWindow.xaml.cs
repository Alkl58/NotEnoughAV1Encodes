using ControlzEx.Theming;
using MahApps.Metro.Controls;
using System.IO;

namespace NotEnoughAV1Encodes
{
    public partial class PopupWindow : MetroWindow
    {
        public PopupWindow(string baseTheme, string accentTheme, string time, string frames, string avg, string fileName)
        {
            InitializeComponent();
            ThemeManager.Current.ChangeTheme(this, baseTheme + "." + accentTheme);
            LabelElapsedTime.Content = time;
            LabelEncodedFrames.Content = frames;
            LabelEncodedAVG.Content = avg;
            FileInfo fs = new FileInfo(fileName);
            LabelEncodedFilesize.Content = (fs.Length >> 20) + "MB";
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
