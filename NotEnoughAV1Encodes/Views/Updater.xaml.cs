using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ControlzEx.Theming;
using MahApps.Metro.Controls;
using Newtonsoft.Json;
using Octokit;

namespace NotEnoughAV1Encodes.Views
{
    public partial class Updater : MetroWindow
    {
        // NEAV1E Update
        private string UpdateVersion = "0";
        private string CurrentVersion = "2.0.0"; // current neav1e version (hardcoded)

        // FFmpeg Update
        public static string FFmpegUpdateVersion;

        public static string FFmpegCurrentVersion;

        // aomenc Update
        private static string AomencUpdateVersion;

        private static string AomencCurrentVersion;

        // rav1e Update
        private static string Rav1eUpdateVersion;

        private static string Rav1eCurrentVersion;

        // SVT-AV1 Update
        private static string SVTAV1UpdateVersion;

        private static string SVTAV1CurrentVersion;

        // MKVToolnix Update
        private static string MKVToolnixUpdateVersion;

        private static string MKVToolnixCurrentVersion;

        // Current Directory
        private static readonly string CurrentDir = Directory.GetCurrentDirectory();

        public Updater(string baseTheme, string accentTheme)
        {
            InitializeComponent();
            LabelCurrentProgramVersion.Content = CurrentVersion;
            ThemeManager.Current.ChangeTheme(this, baseTheme + "." + accentTheme);
            LabelProgressBar.Content = "Downloading Version Lists...";
            ProgressBar.IsIndeterminate = true;
            ParseEverything();
        }

        private async void ParseEverything()
        {
            ParseNEAV1EGithub();
            await ParseJeremyleeJSONAsync();
            await GetOnlineMKVToolnixVersion();
            GetLocalMKVToolnixVersion();
            CompareLocalVersion();
            LabelProgressBar.Content = "";
            ProgressBar.IsIndeterminate = false;
        }

        private void ToggleAllButtons(bool _toggle)
        {
            ButtonUpdateProgram.IsEnabled = _toggle;
            ButtonUpdateFFmpeg.IsEnabled = _toggle;
            ButtonUpdateAomenc.IsEnabled = _toggle;
            ButtonUpdateRav1e.IsEnabled = _toggle;
            ButtonUpdateSVTAV1.IsEnabled = _toggle;
        }

        private void ParseNEAV1EGithub()
        {
            try
            {
                //Parses the latest neav1e release date directly from Github
                GitHubClient client = new(new ProductHeaderValue("NotEnoughAV1Encodes"));
                IReadOnlyList<Release> releases = client.Repository.Release.GetAll("Alkl58", "NotEnoughAV1Encodes").Result;
                Release latest = releases[0];

                string tmpUpdateVersion = latest.TagName.Remove(0, 1);
                if(tmpUpdateVersion.Length == 3)
                {
                    // This is only for backwards compatibility
                    tmpUpdateVersion += ".0";
                }

                LabelUpdateProgramVersion.Content = tmpUpdateVersion;
                UpdateVersion = GetNumbers(tmpUpdateVersion);

                // Compares NEAV1E Versions and sets the color of the labels
                if (int.Parse(GetNumbers(UpdateVersion)) > int.Parse(GetNumbers(CurrentVersion)))
                {
                    LabelCurrentProgramVersion.Foreground = Brushes.Red;
                    LabelUpdateProgramVersion.Foreground = Brushes.Green;
                }
                else if (int.Parse(GetNumbers(UpdateVersion)) == int.Parse(GetNumbers(CurrentVersion)))
                {
                    LabelCurrentProgramVersion.Foreground = Brushes.Green;
                    LabelUpdateProgramVersion.Foreground = Brushes.Green;
                }
                else if (int.Parse(GetNumbers(UpdateVersion)) < int.Parse(GetNumbers(CurrentVersion)))
                {
                    LabelCurrentProgramVersion.Foreground = Brushes.Green;
                    LabelUpdateProgramVersion.Foreground = Brushes.Red;
                }
            }
            catch { }
        }

        private static string GetNumbers(string input)
        {
            return new string(input.Where(c => char.IsDigit(c)).ToArray());
        }

        private async Task ParseJeremyleeJSONAsync()
        {
            try
            {
                HttpClient client = new();
                HttpResponseMessage response = await client.GetAsync("https://jeremylee.sh/bins/manifest.json");
                string jsonWeb = await response.Content.ReadAsStringAsync();
                dynamic json = JsonConvert.DeserializeObject(jsonWeb);

                string ffmpegVersion = json.files["ffmpeg.exe"].datetime;
                FFmpegUpdateVersion = ffmpegVersion.Replace("-", ".").Remove(ffmpegVersion.Length - 6);
                LabelUpdateFFmpegVersion.Content = FFmpegUpdateVersion;

                string aomencVersion = json.files["aomenc.exe"].datetime;
                AomencUpdateVersion = aomencVersion.Replace("-", ".").Remove(aomencVersion.Length - 6);
                LabelUpdateAomencVersion.Content = AomencUpdateVersion;

                string rav1eVersion = json.files["rav1e.exe"].datetime;
                Rav1eUpdateVersion = rav1eVersion.Replace("-", ".").Remove(rav1eVersion.Length - 6);
                LabelUpdateRav1eVersion.Content = Rav1eUpdateVersion;

                string svtav1Version = json.files["SvtAv1EncApp.exe"].datetime;
                SVTAV1UpdateVersion = svtav1Version.Replace("-", ".").Remove(svtav1Version.Length - 6);
                LabelUpdateSVTAV1Version.Content = SVTAV1UpdateVersion;
            }
            catch { }
        }

        private void GetLocalMKVToolnixVersion()
        {
            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(Path.Combine(CurrentDir, "Apps", "mkvtoolnix", "mkvmerge.exe"));
                if (versionInfo != null)
                {
                    string version = versionInfo.FileVersion;
                    LabelCurrentMKVToolnixVersion.Content = version;
                    MKVToolnixCurrentVersion = version;
                }
            } catch { }
        }

        private async Task GetOnlineMKVToolnixVersion()
        {
            try
            {
                HttpClient client = new();
                // Download Release Notes
                HttpResponseMessage response = await client.GetAsync("https://mkvtoolnix.download/doc/NEWS.md");

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    // Split by Line
                    string[] lines = content.Split(new char[] { '\r', '\n' }, StringSplitOptions.None);
                    string version = "";
                    // Iterate over Lines
                    foreach (string line in lines)
                    {
                        if (line.Contains("# Version"))
                        {
                            version = line;
                            break;
                        }
                    }
                    //Remove First 10 Characters
                    version = version[10..];
                    version = version.Split()[0];
                    LabelUpdateMKVToolnixVersion.Content = version;
                    MKVToolnixUpdateVersion = version;
                }
            }
            catch { }
        }

        private void CompareLocalVersion()
        {
            // ffmpeg
            if (File.Exists(Path.Combine(CurrentDir, "Apps", "FFmpeg", "ffmpeg.txt")))
            {
                FFmpegCurrentVersion = File.ReadAllText(Path.Combine(CurrentDir, "Apps", "FFmpeg", "ffmpeg.txt"));
                LabelCurrentFFmpegVersion.Content = FFmpegCurrentVersion;
                if (ParseDate(FFmpegCurrentVersion) < ParseDate(FFmpegUpdateVersion))
                {
                    // Update Version is newer
                    LabelCurrentFFmpegVersion.Foreground = Brushes.Red;
                    LabelUpdateFFmpegVersion.Foreground = Brushes.Green;
                }
                else if (ParseDate(FFmpegCurrentVersion) == ParseDate(FFmpegUpdateVersion))
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

            // MKVToolnix
            if (MKVToolnixCurrentVersion != null && MKVToolnixUpdateVersion != null)
            {
                try
                {
                    int update = int.Parse(MKVToolnixUpdateVersion.Replace(".", string.Empty));
                    int current = int.Parse(MKVToolnixCurrentVersion.Replace(".", string.Empty));
                    // Simple Version Comparison Logic
                    if (current == update)
                    {
                        LabelCurrentMKVToolnixVersion.Foreground = Brushes.Green;
                        LabelUpdateMKVToolnixVersion.Foreground = Brushes.Green;
                    }
                    else if (current > update)
                    {
                        LabelCurrentMKVToolnixVersion.Foreground = Brushes.Green;
                        LabelUpdateMKVToolnixVersion.Foreground = Brushes.Red;
                    }
                    else if (update > current)
                    {
                        LabelCurrentMKVToolnixVersion.Foreground = Brushes.Red;
                        LabelUpdateMKVToolnixVersion.Foreground = Brushes.Green;
                    }
                }
                catch { }
            }
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
            ProcessStartInfo psi = new()
            {
                FileName = "https://github.com/Alkl58/NotEnoughAV1Encodes/releases",
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        private async void ButtonUpdateFFmpeg_Click(object sender, RoutedEventArgs e)
        {
            ToggleAllButtons(false);

            // Creates the ffmpeg folder if not existent
            if (!Directory.Exists(Path.Combine(CurrentDir, "Apps", "FFmpeg")))
            {
                Directory.CreateDirectory(Path.Combine(CurrentDir, "Apps", "FFmpeg"));
            }

            // Downloads ffmpeg
            await Task.Run(() => DownloadBin("https://jeremylee.sh/bins/ffmpeg.7z", Path.Combine(CurrentDir, "Apps", "ffmpeg.7z")));

            if (File.Exists(Path.Combine(CurrentDir, "Apps", "ffmpeg.7z")))
            {
                // Extracts ffmpeg
                ExtractFile(Path.Combine(CurrentDir, "Apps", "ffmpeg.7z"), Path.Combine(Directory.GetCurrentDirectory(), "Apps", "FFmpeg"));

                if (File.Exists(Path.Combine(CurrentDir, "Apps", "FFmpeg", "ffmpeg.exe")))
                {
                    if (File.Exists(Path.Combine(CurrentDir, "Apps", "FFmpeg", "ffmpeg.exe")))
                    {
                        File.Delete(Path.Combine(CurrentDir, "Apps", "FFmpeg", "ffmpeg.txt"));
                    }

                    File.WriteAllText(Path.Combine(CurrentDir, "Apps", "FFmpeg", "ffmpeg.txt"), FFmpegUpdateVersion);

                    File.Delete(Path.Combine(CurrentDir, "Apps", "ffmpeg.7z"));

                    CompareLocalVersion();
                }
            }

            ToggleAllButtons(true);

            LabelProgressBar.Dispatcher.Invoke(() => LabelProgressBar.Content = "Finished updating FFmpeg");
        }

        private async void ButtonUpdateAomenc_Click(object sender, RoutedEventArgs e)
        {
            ToggleAllButtons(false);

            // Creates the aomenc folder if not existent
            if (!Directory.Exists(Path.Combine(CurrentDir, "Apps", "aomenc")))
                Directory.CreateDirectory(Path.Combine(CurrentDir, "Apps", "aomenc"));
            // Downloads aomenc
            await Task.Run(() => DownloadBin("https://jeremylee.sh/bins/aom.7z", Path.Combine(CurrentDir, "Apps", "aom.7z")));
            if (File.Exists(Path.Combine(CurrentDir, "Apps", "aom.7z")))
            {
                // Extracts aomenc
                ExtractFile(Path.Combine(CurrentDir, "Apps", "aom.7z"), Path.Combine(Directory.GetCurrentDirectory(), "Apps", "aomenc"));
                // Writes the version to file
                if (File.Exists(Path.Combine(CurrentDir, "Apps", "aomenc", "aomenc.exe")))
                {
                    // Deletes aomdec
                    if (File.Exists(Path.Combine(CurrentDir, "Apps", "aomenc", "aomdec.exe")))
                    {
                        File.Delete(Path.Combine(CurrentDir, "Apps", "aomenc", "aomdec.exe"));
                    }
                    // Deletes txt file
                    if (File.Exists(Path.Combine(CurrentDir, "Apps", "aomenc", "aomenc.txt")))
                    {
                        File.Delete(Path.Combine(CurrentDir, "Apps", "aomenc", "aomenc.txt"));
                    }

                    File.WriteAllText(Path.Combine(CurrentDir, "Apps", "aomenc", "aomenc.txt"), AomencUpdateVersion);
                }
                // Deletes downloaded archive
                if (File.Exists(Path.Combine(CurrentDir, "Apps", "aom.7z")))
                {
                    File.Delete(Path.Combine(CurrentDir, "Apps", "aom.7z"));
                }

                CompareLocalVersion();
            }

            ToggleAllButtons(true);

            LabelProgressBar.Dispatcher.Invoke(() => LabelProgressBar.Content = "Finished updating Aomenc");
        }

        private async void ButtonUpdateRav1e_Click(object sender, RoutedEventArgs e)
        {
            ToggleAllButtons(false);

            // Creates the rav1e folder if not existent
            if (!Directory.Exists(Path.Combine(CurrentDir, "Apps", "rav1e")))
                Directory.CreateDirectory(Path.Combine(CurrentDir, "Apps", "rav1e"));
            // Downloads rav1e
            await Task.Run(() => DownloadBin("https://jeremylee.sh/bins/rav1e.7z", Path.Combine(CurrentDir, "Apps", "rav1e.7z")));
            if (File.Exists(Path.Combine(CurrentDir, "Apps", "rav1e.7z")))
            {
                // Extracts rav1e
                ExtractFile(Path.Combine(CurrentDir, "Apps", "rav1e.7z"), Path.Combine(Directory.GetCurrentDirectory(), "Apps", "rav1e"));
                // Writes the version to file
                if (File.Exists(Path.Combine(CurrentDir, "Apps", "rav1e", "rav1e.exe")))
                {
                    File.WriteAllText(Path.Combine(CurrentDir, "Apps", "rav1e", "rav1e.txt"), Rav1eUpdateVersion);
                }
                // Deletes downloaded archive
                if (File.Exists(Path.Combine(CurrentDir, "Apps", "rav1e.7z")))
                {
                    File.Delete(Path.Combine(CurrentDir, "Apps", "rav1e.7z"));
                }

                CompareLocalVersion();
            }

            ToggleAllButtons(true);

            LabelProgressBar.Dispatcher.Invoke(() => LabelProgressBar.Content = "Finished updating Rav1e");
        }

        private async void ButtonUpdateSVTAV1_Click(object sender, RoutedEventArgs e)
        {
            ToggleAllButtons(false);

            // Creates the svt-av1 folder if not existent
            Directory.CreateDirectory(Path.Combine(CurrentDir, "Apps", "svt-av1"));

            // Downloads rav1e
            await Task.Run(() => DownloadBin("https://jeremylee.sh/bins/svt-av1.7z", Path.Combine(CurrentDir, "Apps", "svt-av1.7z")));
            if (File.Exists(Path.Combine(CurrentDir, "Apps", "svt-av1.7z")))
            {
                // Extracts rav1e
                ExtractFile(Path.Combine(CurrentDir, "Apps", "svt-av1.7z"), Path.Combine(Directory.GetCurrentDirectory(), "Apps", "svt-av1"));
                // Writes the version to file
                if (File.Exists(Path.Combine(CurrentDir, "Apps", "svt-av1", "SvtAv1EncApp.exe")))
                {
                    // Deletes SVT-AV1 Decoder
                    if (File.Exists(Path.Combine(CurrentDir, "Apps", "svt-av1", "SvtAv1EncApp.exe")))
                    {
                        File.Delete(Path.Combine(CurrentDir, "Apps", "svt-av1", "SvtAv1DecApp.exe"));
                    }
                    // Deletes txt file
                    if (File.Exists(Path.Combine(CurrentDir, "Apps", "svt-av1", "svt-av1.txt")))
                    {
                        File.Delete(Path.Combine(CurrentDir, "Apps", "svt-av1", "svt-av1.txt"));
                    }

                    File.WriteAllText(Path.Combine(CurrentDir, "Apps", "svt-av1", "svt-av1.txt"), SVTAV1UpdateVersion);
                }
                // Deletes downloaded archive
                if (File.Exists(Path.Combine(CurrentDir, "Apps", "svt-av1.7z")))
                {
                    File.Delete(Path.Combine(CurrentDir, "Apps", "svt-av1.7z"));
                }

                CompareLocalVersion();
            }

            ToggleAllButtons(true);

            LabelProgressBar.Dispatcher.Invoke(() => LabelProgressBar.Content = "Finished updating SVT-AV1");
        }

        private void ButtonUpdateMKVToolnix_Click(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo psi = new()
            {
                FileName = "https://www.fosshub.com/MKVToolNix.html",
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        private async Task DownloadBin(string DownloadURL, string PathToFile)
        {
            // Downloads the archive provided in the Link
            try
            {
                WebClient webClient = new();
                webClient.DownloadProgressChanged += (s, e) =>
                {
                    ProgressBar.Dispatcher.Invoke(() => ProgressBar.Value = e.ProgressPercentage);
                    LabelProgressBar.Dispatcher.Invoke(() => LabelProgressBar.Content = Math.Round(e.BytesReceived / 1024f / 1024f, 1) + "MB / " + Math.Round(e.TotalBytesToReceive / 1024f / 1024f, 1) + "MB - " + e.ProgressPercentage + "%");
                    Dispatcher.Invoke(() => Title = "Updater " + e.ProgressPercentage + "%");
                };
                webClient.DownloadFileCompleted += (s, e) =>
                {
                    ProgressBar.Dispatcher.Invoke(() => ProgressBar.Value = 0);
                    LabelProgressBar.Dispatcher.Invoke(() => LabelProgressBar.Content = "Extracting...");
                    Dispatcher.Invoke(() => Title = "Updater");
                };
                
                await webClient.DownloadFileTaskAsync(new Uri(DownloadURL), PathToFile);
            }
            catch { }
        }

        private void ExtractFile(string source, string destination)
        {
            // Extracts the downloaded archives with 7zip
            string zPath = Path.Combine(CurrentDir, "Apps", "7zip", "7za.exe");
            // change the path and give yours
            try
            {
                ProcessStartInfo pro = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
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
