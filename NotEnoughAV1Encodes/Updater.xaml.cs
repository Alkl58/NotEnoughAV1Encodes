using Newtonsoft.Json;
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

namespace NotEnoughAV1Encodes
{
    public partial class Updater : MetroWindow
    {

        // --------------------------------------------------------------------
        //
        //  DOWNLOADS ARE PROVIDED BY jeremylee.sh
        //  THE OWNER OF THAT WEBSITE IS A MEMBER OF THE AV1 DISCORD SERVER
        //  LATELY MS DEFENDER DID MORE FALSE POSITIVES
        //
        // --------------------------------------------------------------------

        // NEAV1E Update
        public static double Neav1eUpdateVersion = 0.0;
        public static double Neav1eCurrentVersion = 1.6; // current neav1e version (hardcoded)
        // FFmpeg Update
        public static string FFmpegUpdateVersion;
        public static string FFmpegCurrentVersion;
        // aomenc Update
        public static string AomencUpdateVersion;
        public static string AomencCurrentVersion;
        // rav1e Update
        public static string Rav1eUpdateVersion;
        public static string Rav1eCurrentVersion;
        // SVT-AV1 Update
        public static string SVTAV1UpdateVersion;
        public static string SVTAV1CurrentVersion;
        // VP9 Update
        public static string VP9UpdateVersion;
        public static string VP9CurrentVersion;
        // Current Directory
        public static string CurrentDir = Directory.GetCurrentDirectory();

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
                var jsonWeb = new WebClient().DownloadString("https://jeremylee.sh/data/bin/packages.json");
                dynamic json = JsonConvert.DeserializeObject(jsonWeb);

                string ffmpegVersion = json.apps["ffmpeg.exe"].datetime;
                FFmpegUpdateVersion = ffmpegVersion.Replace("-", ".").Remove(ffmpegVersion.Length - 6);
                LabelUpdateFFmpegVersion.Content = FFmpegUpdateVersion;

                string aomencVersion = json.apps["aomenc.exe"].datetime;
                AomencUpdateVersion = aomencVersion.Replace("-", ".").Remove(aomencVersion.Length - 6);
                LabelUpdateAomencVersion.Content = AomencUpdateVersion;

                string rav1eVersion = json.apps["rav1e.exe"].datetime;
                Rav1eUpdateVersion = rav1eVersion.Replace("-", ".").Remove(rav1eVersion.Length - 6);
                LabelUpdateRav1eVersion.Content = Rav1eUpdateVersion;

                string svtav1Version = json.apps["SvtAv1EncApp.exe"].datetime;
                SVTAV1UpdateVersion = svtav1Version.Replace("-", ".").Remove(svtav1Version.Length - 6);
                LabelUpdateSVTAV1Version.Content = SVTAV1UpdateVersion;

                string svtvp9Version = json.apps["vpxenc.exe"].datetime;
                VP9UpdateVersion = svtvp9Version.Replace("-", ".").Remove(svtvp9Version.Length - 6);
                LabelUpdateVPXVersion.Content = VP9UpdateVersion;
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
            // aomenc
            if (File.Exists(Path.Combine(CurrentDir, "Apps", "aomenc", "aomenc.txt")))
            {
                AomencCurrentVersion = File.ReadAllText(Path.Combine(CurrentDir, "Apps", "aomenc", "aomenc.txt"));
                LabelCurrentAomencVersion.Content = AomencCurrentVersion;
                if (ParseDate(AomencCurrentVersion) < ParseDate(AomencUpdateVersion))
                {
                    // Update Version is newer
                    LabelCurrentAomencVersion.Foreground = Brushes.Red;
                    LabelUpdateAomencVersion.Foreground = Brushes.Green;
                }
                else if (ParseDate(AomencCurrentVersion) == ParseDate(AomencUpdateVersion))
                {
                    // Both Versions are identical
                    LabelCurrentAomencVersion.Foreground = Brushes.Green;
                    LabelUpdateAomencVersion.Foreground = Brushes.Green;
                }
                else if (ParseDate(AomencCurrentVersion) > ParseDate(AomencUpdateVersion))
                {
                    // Local Version is newer
                    LabelCurrentAomencVersion.Foreground = Brushes.Green;
                    LabelUpdateAomencVersion.Foreground = Brushes.Red;
                }
            }
            else { LabelCurrentAomencVersion.Content = "unknown"; LabelCurrentAomencVersion.Foreground = Brushes.Red; }
            // rav1e
            if (File.Exists(Path.Combine(CurrentDir, "Apps", "rav1e", "rav1e.txt")))
            {
                Rav1eCurrentVersion = File.ReadAllText(Path.Combine(CurrentDir, "Apps", "rav1e", "rav1e.txt"));
                LabelCurrentRav1eVersion.Content = Rav1eCurrentVersion;
                if (ParseDate(Rav1eCurrentVersion) < ParseDate(Rav1eUpdateVersion))
                {
                    // Update Version is newer
                    LabelCurrentRav1eVersion.Foreground = Brushes.Red;
                    LabelUpdateRav1eVersion.Foreground = Brushes.Green;
                }
                else if (ParseDate(Rav1eCurrentVersion) == ParseDate(Rav1eUpdateVersion))
                {
                    // Both Versions are identical
                    LabelCurrentRav1eVersion.Foreground = Brushes.Green;
                    LabelUpdateRav1eVersion.Foreground = Brushes.Green;
                }
                else if (ParseDate(Rav1eCurrentVersion) > ParseDate(Rav1eUpdateVersion))
                {
                    // Local Version is newer
                    LabelCurrentRav1eVersion.Foreground = Brushes.Green;
                    LabelUpdateRav1eVersion.Foreground = Brushes.Red;
                }
            }
            else { LabelCurrentRav1eVersion.Content = "unknown"; LabelCurrentRav1eVersion.Foreground = Brushes.Red; }
            // svt-av1
            if (File.Exists(Path.Combine(CurrentDir, "Apps", "svt-av1", "svt-av1.txt")))
            {
                SVTAV1CurrentVersion = File.ReadAllText(Path.Combine(CurrentDir, "Apps", "svt-av1", "svt-av1.txt"));
                LabelCurrentSVTAV1Version.Content = SVTAV1CurrentVersion;
                if (ParseDate(SVTAV1CurrentVersion) < ParseDate(SVTAV1UpdateVersion))
                {
                    // Update Version is newer
                    LabelCurrentSVTAV1Version.Foreground = Brushes.Red;
                    LabelUpdateSVTAV1Version.Foreground = Brushes.Green;
                }
                else if (ParseDate(SVTAV1CurrentVersion) == ParseDate(SVTAV1UpdateVersion))
                {
                    // Both Versions are identical
                    LabelCurrentSVTAV1Version.Foreground = Brushes.Green;
                    LabelUpdateSVTAV1Version.Foreground = Brushes.Green;
                }
                else if (ParseDate(SVTAV1CurrentVersion) > ParseDate(SVTAV1UpdateVersion))
                {
                    // Local Version is newer
                    LabelCurrentSVTAV1Version.Foreground = Brushes.Green;
                    LabelUpdateSVTAV1Version.Foreground = Brushes.Red;
                }
            }
            else { LabelCurrentSVTAV1Version.Content = "unknown"; LabelCurrentSVTAV1Version.Foreground = Brushes.Red; }
            // vpx
            if (File.Exists(Path.Combine(CurrentDir, "Apps", "vpx", "vpx.txt")))
            {
                VP9CurrentVersion = File.ReadAllText(Path.Combine(CurrentDir, "Apps", "vpx", "vpx.txt"));
                LabelCurrentVPXVersion.Content = VP9CurrentVersion;
                if (ParseDate(VP9CurrentVersion) < ParseDate(VP9UpdateVersion))
                {
                    // Update Version is newer
                    LabelCurrentVPXVersion.Foreground = Brushes.Red;
                    LabelUpdateVPXVersion.Foreground = Brushes.Green;
                }
                else if (ParseDate(VP9CurrentVersion) == ParseDate(VP9UpdateVersion))
                {
                    // Both Versions are identical
                    LabelCurrentVPXVersion.Foreground = Brushes.Green;
                    LabelUpdateVPXVersion.Foreground = Brushes.Green;
                }
                else if (ParseDate(VP9CurrentVersion) > ParseDate(VP9UpdateVersion))
                {
                    // Local Version is newer
                    LabelCurrentVPXVersion.Foreground = Brushes.Green;
                    LabelUpdateVPXVersion.Foreground = Brushes.Red;
                }
            }
            else { LabelCurrentVPXVersion.Content = "unknown"; LabelCurrentVPXVersion.Foreground = Brushes.Red; }
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
            await Task.Run(() => DownloadBin("https://jeremylee.sh/data/bin/ffmpeg.7z", Path.Combine(CurrentDir, "Apps", "ffmpeg.7z")));
            // Downloads ffprobe
            await Task.Run(() => DownloadBin("https://jeremylee.sh/data/bin/ffprobe.7z", Path.Combine(CurrentDir, "Apps", "ffprobe.7z")));
            if (File.Exists(Path.Combine(CurrentDir, "Apps", "ffmpeg.7z")))
            {
                // Extracts ffmpeg
                ExtractFile(Path.Combine(CurrentDir, "Apps", "ffmpeg.7z"), Path.Combine(Directory.GetCurrentDirectory(), "Apps", "ffmpeg"));
                // Writes the version to file
                if (File.Exists(Path.Combine(CurrentDir, "Apps", "ffmpeg", "ffmpeg.exe")))
                {
                    // Deletes txt file
                    if (File.Exists(Path.Combine(CurrentDir, "Apps", "ffmpeg", "ffmpeg.txt")))
                        File.Delete(Path.Combine(CurrentDir, "Apps", "ffmpeg", "ffmpeg.txt"));
                    File.WriteAllText(Path.Combine(CurrentDir, "Apps", "ffmpeg", "ffmpeg.txt"), FFmpegUpdateVersion);
                }
                // Deletes downloaded archive
                if (File.Exists(Path.Combine(CurrentDir, "Apps", "ffmpeg.7z")))
                    File.Delete(Path.Combine(CurrentDir, "Apps", "ffmpeg.7z"));
                CompareLocalVersion();
            }
            if (File.Exists(Path.Combine(CurrentDir, "Apps", "ffprobe.7z")))
            {
                // Extracts ffprobe
                ExtractFile(Path.Combine(CurrentDir, "Apps", "ffprobe.7z"), Path.Combine(Directory.GetCurrentDirectory(), "Apps", "ffmpeg"));
                // Deletes downloaded archive
                if (File.Exists(Path.Combine(CurrentDir, "Apps", "ffprobe.7z")))
                    File.Delete(Path.Combine(CurrentDir, "Apps", "ffprobe.7z"));
            }
            ProgressBar.IsIndeterminate = false;
        }

        private async void ButtonUpdateAomenc_Click(object sender, RoutedEventArgs e)
        {
            ProgressBar.IsIndeterminate = true;
            // Creates the aomenc folder if not existent
            if (!Directory.Exists(Path.Combine(CurrentDir, "Apps", "aomenc")))
                Directory.CreateDirectory(Path.Combine(CurrentDir, "Apps", "aomenc"));
            // Downloads aomenc
            await Task.Run(() => DownloadBin("https://jeremylee.sh/data/bin/aom.7z", Path.Combine(CurrentDir, "Apps", "aom.7z")));
            if (File.Exists(Path.Combine(CurrentDir, "Apps", "aom.7z")))
            {
                // Extracts aomenc
                ExtractFile(Path.Combine(CurrentDir, "Apps", "aom.7z"), Path.Combine(Directory.GetCurrentDirectory(), "Apps", "aomenc"));
                // Writes the version to file
                if (File.Exists(Path.Combine(CurrentDir, "Apps", "aomenc", "aomenc.exe")))
                {
                    // Deletes aomdec
                    if (File.Exists(Path.Combine(CurrentDir, "Apps", "aomenc", "aomdec.exe")))
                        File.Delete(Path.Combine(CurrentDir, "Apps", "aomenc", "aomdec.exe"));
                    // Deletes txt file
                    if (File.Exists(Path.Combine(CurrentDir, "Apps", "aomenc", "aomenc.txt")))
                        File.Delete(Path.Combine(CurrentDir, "Apps", "aomenc", "aomenc.txt"));
                    File.WriteAllText(Path.Combine(CurrentDir, "Apps", "aomenc", "aomenc.txt"), AomencUpdateVersion);
                }
                // Deletes downloaded archive
                if (File.Exists(Path.Combine(CurrentDir, "Apps", "aom.7z")))
                    File.Delete(Path.Combine(CurrentDir, "Apps", "aom.7z"));
                CompareLocalVersion();
            }
            ProgressBar.IsIndeterminate = false;
        }

        private async void ButtonUpdateRav1e_Click(object sender, RoutedEventArgs e)
        {
            ProgressBar.IsIndeterminate = true;
            // Creates the rav1e folder if not existent
            if (!Directory.Exists(Path.Combine(CurrentDir, "Apps", "rav1e")))
                Directory.CreateDirectory(Path.Combine(CurrentDir, "Apps", "rav1e"));
            // Downloads rav1e
            await Task.Run(() => DownloadBin("https://jeremylee.sh/data/bin/rav1e.7z", Path.Combine(CurrentDir, "Apps", "rav1e.7z")));
            if (File.Exists(Path.Combine(CurrentDir, "Apps", "rav1e.7z")))
            {
                // Extracts rav1e
                ExtractFile(Path.Combine(CurrentDir, "Apps", "rav1e.7z"), Path.Combine(Directory.GetCurrentDirectory(), "Apps", "rav1e"));
                // Writes the version to file
                if (File.Exists(Path.Combine(CurrentDir, "Apps", "rav1e", "rav1e.exe")))
                {
                    // Deletes txt file
                    if (File.Exists(Path.Combine(CurrentDir, "Apps", "rav1e", "rav1e.txt")))
                        File.Delete(Path.Combine(CurrentDir, "Apps", "rav1e", "rav1e.txt"));
                    File.WriteAllText(Path.Combine(CurrentDir, "Apps", "rav1e", "rav1e.txt"), Rav1eUpdateVersion);
                }
                // Deletes downloaded archive
                if (File.Exists(Path.Combine(CurrentDir, "Apps", "rav1e.7z")))
                    File.Delete(Path.Combine(CurrentDir, "Apps", "rav1e.7z"));
                CompareLocalVersion();
            }
            ProgressBar.IsIndeterminate = false;
        }

        private async void ButtonUpdateSVTAV1_Click(object sender, RoutedEventArgs e)
        {
            ProgressBar.IsIndeterminate = true;
            // Creates the svt-av1 folder if not existent
            if (!Directory.Exists(Path.Combine(CurrentDir, "Apps", "svt-av1")))
                Directory.CreateDirectory(Path.Combine(CurrentDir, "Apps", "svt-av1"));
            // Downloads rav1e
            await Task.Run(() => DownloadBin("https://jeremylee.sh/data/bin/svt-av1.7z", Path.Combine(CurrentDir, "Apps", "svt-av1.7z")));
            if (File.Exists(Path.Combine(CurrentDir, "Apps", "svt-av1.7z")))
            {
                // Extracts rav1e
                ExtractFile(Path.Combine(CurrentDir, "Apps", "svt-av1.7z"), Path.Combine(Directory.GetCurrentDirectory(), "Apps", "svt-av1"));
                // Writes the version to file
                if (File.Exists(Path.Combine(CurrentDir, "Apps", "svt-av1", "SvtAv1EncApp.exe")))
                {
                    // Deletes SVT-AV1 Decoder
                    if (File.Exists(Path.Combine(CurrentDir, "Apps", "svt-av1", "SvtAv1EncApp.exe")))
                        File.Delete(Path.Combine(CurrentDir, "Apps", "svt-av1", "SvtAv1DecApp.exe"));
                    // Deletes txt file
                    if (File.Exists(Path.Combine(CurrentDir, "Apps", "svt-av1", "svt-av1.txt")))
                        File.Delete(Path.Combine(CurrentDir, "Apps", "svt-av1", "svt-av1.txt"));
                    File.WriteAllText(Path.Combine(CurrentDir, "Apps", "svt-av1", "svt-av1.txt"), SVTAV1UpdateVersion);
                }
                // Deletes downloaded archive
                if (File.Exists(Path.Combine(CurrentDir, "Apps", "svt-av1.7z")))
                    File.Delete(Path.Combine(CurrentDir, "Apps", "svt-av1.7z"));
                CompareLocalVersion();
            }
            ProgressBar.IsIndeterminate = false;
        }

        private async void ButtonUpdateVPX_Click(object sender, RoutedEventArgs e)
        {
            ProgressBar.IsIndeterminate = true;
            // Creates the svt-av1 folder if not existent
            if (!Directory.Exists(Path.Combine(CurrentDir, "Apps", "vpx")))
                Directory.CreateDirectory(Path.Combine(CurrentDir, "Apps", "vpx"));
            // Downloads rav1e
            await Task.Run(() => DownloadBin("https://jeremylee.sh/data/bin/vpx.7z", Path.Combine(CurrentDir, "Apps", "vpx.7z")));
            if (File.Exists(Path.Combine(CurrentDir, "Apps", "vpx.7z")))
            {
                // Extracts rav1e
                ExtractFile(Path.Combine(CurrentDir, "Apps", "vpx.7z"), Path.Combine(Directory.GetCurrentDirectory(), "Apps", "vpx"));
                // Writes the version to file
                if (File.Exists(Path.Combine(CurrentDir, "Apps", "vpx", "vpxenc.exe")))
                {
                    // Deletes SVT-AV1 Decoder
                    if (File.Exists(Path.Combine(CurrentDir, "Apps", "vpx", "vpxdec.exe")))
                        File.Delete(Path.Combine(CurrentDir, "Apps", "vpx", "vpxdec.exe"));
                    // Deletes txt file
                    if (File.Exists(Path.Combine(CurrentDir, "Apps", "vpx", "vpx.txt")))
                        File.Delete(Path.Combine(CurrentDir, "Apps", "vpx", "vpx.txt"));
                    File.WriteAllText(Path.Combine(CurrentDir, "Apps", "vpx", "vpx.txt"), VP9UpdateVersion);
                }
                // Deletes downloaded archive
                if (File.Exists(Path.Combine(CurrentDir, "Apps", "vpx.7z")))
                    File.Delete(Path.Combine(CurrentDir, "Apps", "vpx.7z"));
                CompareLocalVersion();
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
                ProcessStartInfo pro = new ProcessStartInfo();
                pro.WindowStyle = ProcessWindowStyle.Hidden;
                pro.FileName = zPath;
                pro.Arguments = "x \"" + source + "\" -aoa -o" + '\u0022' + destination + '\u0022';
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
