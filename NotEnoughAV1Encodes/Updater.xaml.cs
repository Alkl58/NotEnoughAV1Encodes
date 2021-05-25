using Octokit;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using MahApps.Metro.Controls;
using ControlzEx.Theming;
using System.Text;

namespace NotEnoughAV1Encodes
{
    public partial class Updater : MetroWindow
    {

        // NEAV1E Update
        private static double Neav1eUpdateVersion = 0.0;
        private static double Neav1eCurrentVersion = 1.9; // current neav1e version (hardcoded)
        // FFmpeg Update
        private static string FFmpegUpdateVersion;
        private static string FFmpegCurrentVersion;

        private static string Git_FFmpeg_Name = "";

        // Current Directory
        private static string CurrentDir = Directory.GetCurrentDirectory();

        public Updater(string baseTheme, string accentTheme)
        {
            InitializeComponent();
            LabelCurrentProgramVersion.Content = Neav1eCurrentVersion;
            ThemeManager.Current.ChangeTheme(this, baseTheme + "." + accentTheme);
            ParseNEAV1EGithub();
            ParseJeremyleeJSON();
            CompareLocalVersion();
        }

        private void ParseNEAV1EGithub()
        {
            try
            {
                //Parses the latest neav1e release date directly from Github
                var client = new GitHubClient(new ProductHeaderValue("NotEnoughAV1Encodes"));
                var releases = client.Repository.Release.GetAll("Alkl58", "NotEnoughAV1Encodes").Result;
                var latest = releases[0];
                Neav1eUpdateVersion = Convert.ToDouble(latest.TagName.Remove(0, 1).Replace(".", ","));
                LabelUpdateProgramVersion.Content = Neav1eUpdateVersion;
                // Compares NEAV1E Versions and sets the color of the labels
                if (Neav1eUpdateVersion > Neav1eCurrentVersion)
                {
                    LabelCurrentProgramVersion.Foreground = Brushes.Red;
                    LabelUpdateProgramVersion.Foreground = Brushes.Green;
                }
                else if (Neav1eUpdateVersion == Neav1eCurrentVersion)
                {
                    LabelCurrentProgramVersion.Foreground = Brushes.Green;
                    LabelUpdateProgramVersion.Foreground = Brushes.Green;
                }
            }
            catch {  }

        }

        private void ParseJeremyleeJSON()
        {
            try
            {
                WebClient wc = new WebClient();
                byte[] data = wc.DownloadData("https://www.gyan.dev/ffmpeg/builds/git-version");
                string temp_date = Encoding.UTF8.GetString(data);

                // Required to later have the correct path
                Git_FFmpeg_Name = "ffmpeg-" + temp_date + "-full_build";

                temp_date = temp_date.Replace("-", ".").Remove(temp_date.Length - 15);

                FFmpegUpdateVersion = temp_date;
                LabelUpdateFFmpegVersion.Content = FFmpegUpdateVersion;
            }
            catch { }
        }

        private void CompareLocalVersion()
        {
            // ffmpeg
            if (File.Exists(Path.Combine(CurrentDir, "Apps", "ffmpeg", "ffmpeg.txt")))
            {
                FFmpegCurrentVersion = File.ReadAllText(Path.Combine(CurrentDir, "Apps", "ffmpeg", "ffmpeg.txt"));
                LabelCurrentFFmpegVersion.Content = FFmpegCurrentVersion;
                if (ParseDate(FFmpegCurrentVersion) < ParseDate(FFmpegUpdateVersion))
                {
                    // Update Version is newer
                    LabelCurrentFFmpegVersion.Foreground = Brushes.Red;
                    LabelUpdateFFmpegVersion.Foreground = Brushes.Green;
                }
                else if(ParseDate(FFmpegCurrentVersion) == ParseDate(FFmpegUpdateVersion))
                {
                    // Both Versions are identical
                    LabelCurrentFFmpegVersion.Foreground = Brushes.Green;
                    LabelUpdateFFmpegVersion.Foreground = Brushes.Green;
                }
                else if (ParseDate(FFmpegCurrentVersion) > ParseDate(FFmpegUpdateVersion))
                {
                    // Local Version is newer
                    LabelCurrentFFmpegVersion.Foreground = Brushes.Green;
                    LabelUpdateFFmpegVersion.Foreground = Brushes.Red;
                }
            }
            else { LabelCurrentFFmpegVersion.Content = "unknown"; LabelCurrentFFmpegVersion.Foreground = Brushes.Red; }
        }

        private DateTime? ParseDate(string input)
        {
            // Converts string to datetime
            try
            {
                DateTime myDate = DateTime.ParseExact(input, "yyyy.MM.dd", System.Globalization.CultureInfo.InvariantCulture);
                return myDate;
            }
            catch
            {
                return null;
            }
        }
        private void ButtonUpdateProgram_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Alkl58/NotEnoughAV1Encodes/releases");
        }
        private async void ButtonUpdateFFmpeg_Click(object sender, RoutedEventArgs e)
        {
            ProgressBar.IsIndeterminate = true;
            // Creates the ffmpeg folder if not existent
            if (!Directory.Exists(Path.Combine(CurrentDir, "Apps", "ffmpeg")))
                Directory.CreateDirectory(Path.Combine(CurrentDir, "Apps", "ffmpeg"));

            // Downloads ffmpeg
            await Task.Run(() => DownloadBin("https://www.gyan.dev/ffmpeg/builds/ffmpeg-git-full.7z", Path.Combine(CurrentDir, "Apps", "ffmpeg-git-full.7z")));

            if (File.Exists(Path.Combine(CurrentDir, "Apps", "ffmpeg-git-full.7z")))
            {
                // Extracts ffmpeg
                ExtractFile(Path.Combine(CurrentDir, "Apps", "ffmpeg-git-full.7z"), Path.Combine(Directory.GetCurrentDirectory(), "Apps", "ffmpeg"));

                if (File.Exists(Path.Combine(CurrentDir, "Apps", "ffmpeg", Git_FFmpeg_Name, "bin", "ffmpeg.exe")))
                {
                    if (File.Exists(Path.Combine(CurrentDir, "Apps", "ffmpeg", "ffmpeg.exe")))
                    {
                        File.Delete(Path.Combine(CurrentDir, "Apps", "ffmpeg", "ffmpeg.txt"));
                        File.Delete(Path.Combine(CurrentDir, "Apps", "ffmpeg", "ffmpeg.exe"));
                    }

                    File.Move(Path.Combine(CurrentDir, "Apps", "ffmpeg", Git_FFmpeg_Name, "bin", "ffmpeg.exe"), Path.Combine(CurrentDir, "Apps", "ffmpeg", "ffmpeg.exe"));
                    File.Move(Path.Combine(CurrentDir, "Apps", "ffmpeg", Git_FFmpeg_Name, "bin", "ffprobe.exe"), Path.Combine(CurrentDir, "Apps", "ffmpeg", "ffprobe.exe"));

                    File.WriteAllText(Path.Combine(CurrentDir, "Apps", "ffmpeg", "ffmpeg.txt"), FFmpegUpdateVersion);

                    File.Delete(Path.Combine(CurrentDir, "Apps", "ffmpeg-git-full.7z"));
                    Directory.Delete(Path.Combine(CurrentDir, "Apps", "ffmpeg", Git_FFmpeg_Name), true);

                    CompareLocalVersion();
                }
            }

            ProgressBar.IsIndeterminate = false;
        }

        private async Task DownloadBin(string DownloadURL, string PathToFile)
        {
            // Downloads the archive provided in the Link
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    var ddl = new Uri(DownloadURL);
                    await webClient.DownloadFileTaskAsync(ddl, PathToFile);
                }
            }
            catch { }
        }

        public void ExtractFile(string source, string destination)
        {
            // Extracts the downloaded archives with 7zip
            string zPath = Path.Combine(CurrentDir, "Apps", "7zip", "7za.exe");
            // change the path and give yours 
            try
            {
                ProcessStartInfo pro = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = zPath,
                    Arguments = "x \"" + source + "\" -aoa -o" + '\u0022' + destination + '\u0022'
                };
                Process x = Process.Start(pro);
                x.WaitForExit();
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
