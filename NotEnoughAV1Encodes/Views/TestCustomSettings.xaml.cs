using ControlzEx.Theming;
using MahApps.Metro.Controls;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NotEnoughAV1Encodes.Views
{
    public partial class TestCustomSettings : MetroWindow
    {
        public TestCustomSettings(string theme, int encoder, string command)
        {
            InitializeComponent();
            try { ThemeManager.Current.ChangeTheme(this, theme); } catch { }
            Test(encoder, command);
        }

        private async void Test(int encoder, string command)
        {
            ProgressBar.IsIndeterminate = true;
            LabelProgressBar.Content = "Testing... Please wait.";

            int exitCode = await Task.Run(() => TestEncode(encoder, command));

            if (exitCode == 0)
            {
                LabelProgressBar.Foreground = new SolidColorBrush(Color.FromRgb(6, 176, 37));
                LabelProgressBar.Content = "Test was Successfull.";
            }
            else
            {
                LabelProgressBar.Foreground = new SolidColorBrush(Color.FromRgb(200, 0, 0));
                LabelProgressBar.Content = "Test Terminated with Error Code: " + exitCode.ToString() + " - Invalid settings?";
            }
            ProgressBar.IsIndeterminate = false;
        }

        private int TestEncode(int encoder, string command)
        {
            Process ffmpegProcess = new();
            ProcessStartInfo startInfo = new();
            startInfo.UseShellExecute = true;
            startInfo.FileName = "cmd.exe";
            startInfo.WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "FFmpeg");
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            string input = " -y -i \"" + Path.Combine(Directory.GetCurrentDirectory(), "sample", "test_sample.mp4") + "\"";
            string testCommand = " -t 00:00.30 " + command + " ";

            if (encoder <= 4)
            {
                // Internal Encoders
                testCommand += "\"" + Path.Combine(Directory.GetCurrentDirectory(), "sample", "test_sample_out.webm") + "\"";
            }
            else
            {
                // External Encoders
                string passesSettings = "";
                if (encoder is 5) { passesSettings = " --passes=1 --output="; }
                if (encoder is 6) { passesSettings = " --output "; }
                if (encoder is 7) { passesSettings = " --passes 1 --output "; }
                testCommand += passesSettings + "\"" + Path.Combine(Directory.GetCurrentDirectory(), "sample", "test_sample_out.ivf") + "\"";
            }

            startInfo.Arguments = "/C ffmpeg.exe " + input + testCommand;

            ffmpegProcess.StartInfo = startInfo;
            ffmpegProcess.Start();
            ffmpegProcess.WaitForExit();

            return ffmpegProcess.ExitCode;
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }
    }
}
