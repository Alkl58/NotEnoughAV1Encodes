using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ControlzEx.Theming;
using MahApps.Metro.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NotEnoughAV1Encodes.resources.lang;
using Octokit;
using HtmlAgilityPack;

namespace NotEnoughAV1Encodes.Views
{
    public partial class Updater : MetroWindow, INotifyPropertyChanged
    {
        private static readonly string FFMPEG_LAST_BUILD_VERSION_URL = "https://www.gyan.dev/ffmpeg/builds/last-build-update";
        private static readonly string FFMPEG_LAST_BUILD_DOWNLOAD_URL = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-git-full.7z";
        private static readonly string MKVTOOLNIX_DOWNLOADS_WEBPAGE = "https://mkvtoolnix.download/downloads.html";
        private static readonly string MKVTOOLNIX_ROOT_URL = "https://mkvtoolnix.download";
        private static readonly string ASSEMBLY_VERSION = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        private static readonly string CURRENT_DIR = Directory.GetCurrentDirectory();

        public event PropertyChangedEventHandler PropertyChanged;

        private string ffmpegUpdateVersion;
        private string ffmpegCurrentVersion;
        private string aomencUpdateVersion;
        private string aomencCurrentVersion;
        private string rav1eUpdateVersion;
        private string rav1eCurrentVersion;
        private string svtav1UpdateVersion;
        private string svtav1CurrentVersion;
        private string qsvenccUpdateVersion;
        private string qsvenccCurrentVersion;
        private string mkvtoolnixUpdateVersion;
        private string mkvtoolnixCurrentVersion;

        private string QSVReleaseAPIGithub;
        private string MKVToolnixDownloadURL;

        private string UpdateVersion = "0";
        private readonly string CurrentVersion = ASSEMBLY_VERSION.Remove(ASSEMBLY_VERSION.Length - 2);

        public string FFmpegUpdateVersion 
        { 
            get { return ffmpegUpdateVersion; }
            set  {  ffmpegUpdateVersion = value;  OnPropertyChanged(); }
        }

        public string FFmpegCurrentVersion 
        { 
            get { return ffmpegCurrentVersion; } 
            set {  ffmpegCurrentVersion = value; OnPropertyChanged(); }
        }

        public string AomencUpdateVersion
        {
            get { return aomencUpdateVersion; }
            set { aomencUpdateVersion = value; OnPropertyChanged(); }
        }

        public string AomencCurrentVersion
        {
            get { return aomencCurrentVersion; }
            set { aomencCurrentVersion = value; OnPropertyChanged(); }
        }

        public string Rav1eUpdateVersion 
        {
            get { return rav1eUpdateVersion; }
            set { rav1eUpdateVersion = value; OnPropertyChanged(); }
        }

        public string Rav1eCurrentVersion 
        { 
            get { return rav1eCurrentVersion; }
            set { rav1eCurrentVersion = value; OnPropertyChanged(); }
        }

        public string SVTAV1UpdateVersion 
        { 
            get { return svtav1UpdateVersion; }
            set { svtav1UpdateVersion = value; OnPropertyChanged(); }
        }

        public string SVTAV1CurrentVersion 
        { 
            get { return svtav1CurrentVersion; }
            set { svtav1CurrentVersion = value; OnPropertyChanged(); }
        }

        public string QSVEncCUpdateVersion 
        { 
            get { return qsvenccUpdateVersion; }
            set { qsvenccUpdateVersion = value; OnPropertyChanged(); }
        }

        public string QSVEncCCurrentVersion 
        { 
            get { return qsvenccCurrentVersion; }
            set { qsvenccCurrentVersion = value; OnPropertyChanged(); }
        }

        public string MKVToolnixUpdateVersion 
        { 
            get { return mkvtoolnixUpdateVersion; }
            set {  mkvtoolnixUpdateVersion = value; OnPropertyChanged(); }
        }

        public string MKVToolnixCurrentVersion 
        { 
            get { return mkvtoolnixCurrentVersion; }
            set { mkvtoolnixCurrentVersion = value; OnPropertyChanged(); }
        }


        public Updater(string baseTheme, string accentTheme)
        {
            InitializeComponent();
            DataContext = this;
            LabelCurrentProgramVersion.Content = CurrentVersion;
            ThemeManager.Current.ChangeTheme(this, baseTheme + "." + accentTheme);
            LabelProgressBar.Content = "Downloading Version Lists...";
            ProgressBar.IsIndeterminate = true;
            ParseEverything();
        }

        private async void ParseEverything()
        {
            ParseNEAV1EGithub();
            ParseQSVEncCGithub();
            await ParseFFMPEGVersion();
            await ParseJeremyleeJSONAsync();
            ParseMKVToolnixWebsite();
            GetLocalMKVToolnixVersion();
            GetLocalQSVEncCVersion();
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
            ButtonUpdateQSVEnc.IsEnabled = _toggle;
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

        private void ParseQSVEncCGithub()
        {
            try
            {
                //Parses the latest neav1e release date directly from Github
                GitHubClient client = new(new ProductHeaderValue("QSVEnc"));
                IReadOnlyList<Release> releases = client.Repository.Release.GetAll("rigaya", "QSVEnc").Result;
                Release latest = releases[0];

                QSVEncCUpdateVersion = latest.TagName;
                QSVReleaseAPIGithub = latest.AssetsUrl;
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

                string aomencVersion = json.files["aomenc.exe"].datetime;
                AomencUpdateVersion = aomencVersion.Replace("-", ".").Remove(aomencVersion.Length - 6);

                string rav1eVersion = json.files["rav1e.exe"].datetime;
                Rav1eUpdateVersion = rav1eVersion.Replace("-", ".").Remove(rav1eVersion.Length - 6);

                string svtav1Version = json.files["SvtAv1EncApp.exe"].datetime;
                SVTAV1UpdateVersion = svtav1Version.Replace("-", ".").Remove(svtav1Version.Length - 6);
            }
            catch { }
        }

        private async Task ParseFFMPEGVersion()
        {
            try
            {
                HttpClient client = new();
                HttpResponseMessage response = await client.GetAsync(FFMPEG_LAST_BUILD_VERSION_URL);
                string ffmpegVersion = await response.Content.ReadAsStringAsync();
                FFmpegUpdateVersion = ffmpegVersion.Replace("-", ".");
            }
            catch { }
        }

        private void ParseMKVToolnixWebsite()
        {
            try
            {
                HttpClient client = new();
                string html = client.GetStringAsync(MKVTOOLNIX_DOWNLOADS_WEBPAGE).Result;

                HtmlDocument doc = new();
                doc.LoadHtml(html);

                // XPath to select the <tr> with "Portable (64-bit)"
                string xpath = "//tr[td='Portable (64-bit)']";

                HtmlNode portableRow = doc.DocumentNode.SelectSingleNode(xpath);

                if (portableRow != null)
                {
                    // Get the href value from the <a> tag within the selected <tr>
                    HtmlNode hrefNode = portableRow.SelectSingleNode("td/a[@href]");

                    if (hrefNode != null)
                    {
                        string hrefValue = hrefNode.GetAttributeValue("href", "");
                        MKVToolnixDownloadURL = MKVTOOLNIX_ROOT_URL + "/" + hrefValue;

                        var split = hrefValue.Split('/');
                        MKVToolnixUpdateVersion = split[2];
                    }
                }
            }
            catch { }
        }

        private void GetLocalMKVToolnixVersion()
        {
            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(Path.Combine(CURRENT_DIR, "Apps", "mkvtoolnix", "mkvmerge.exe"));
                if (versionInfo != null)
                {
                    string version = versionInfo.FileVersion;
                    MKVToolnixCurrentVersion = version;
                }
            } catch { }
        }

        private void GetLocalQSVEncCVersion()
        {
            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(Path.Combine(CURRENT_DIR, "Apps", "qsvenc", "QSVEncC64.exe"));
                if (versionInfo != null)
                {
                    QSVEncCCurrentVersion = versionInfo.FileVersion;
                }
            }
            catch { }
        }

        private void CompareLocalVersion()
        {
            // ffmpeg
            if (File.Exists(Path.Combine(CURRENT_DIR, "Apps", "ffmpeg", "ffmpeg.txt")))
            {
                FFmpegCurrentVersion = File.ReadAllText(Path.Combine(CURRENT_DIR, "Apps", "FFmpeg", "ffmpeg.txt"));
                //LabelCurrentFFmpegVersion.Content = FFmpegCurrentVersion;
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
            if (File.Exists(Path.Combine(CURRENT_DIR, "Apps", "aomenc", "aomenc.txt")))
            {
                AomencCurrentVersion = File.ReadAllText(Path.Combine(CURRENT_DIR, "Apps", "aomenc", "aomenc.txt"));
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
            if (File.Exists(Path.Combine(CURRENT_DIR, "Apps", "rav1e", "rav1e.txt")))
            {
                Rav1eCurrentVersion = File.ReadAllText(Path.Combine(CURRENT_DIR, "Apps", "rav1e", "rav1e.txt"));
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
            if (File.Exists(Path.Combine(CURRENT_DIR, "Apps", "svt-av1", "svt-av1.txt")))
            {
                SVTAV1CurrentVersion = File.ReadAllText(Path.Combine(CURRENT_DIR, "Apps", "svt-av1", "svt-av1.txt"));
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

            // QSVEnc
            if (QSVEncCCurrentVersion != null && QSVEncCUpdateVersion != null)
            {
                try
                {
                    double update = double.Parse(QSVEncCUpdateVersion);
                    double current = double.Parse(QSVEncCCurrentVersion);
                    // Simple Version Comparison Logic
                    if (current.Equals(update))
                    {
                        LabelCurrentQSVEncCVersion.Foreground = Brushes.Green;
                        LabelUpdateQSVEncCVersion.Foreground = Brushes.Green;
                    }
                    else if (current.IsGreaterThan(update))
                    {
                        LabelCurrentQSVEncCVersion.Foreground = Brushes.Green;
                        LabelUpdateQSVEncCVersion.Foreground = Brushes.Red;
                    }
                    else if (update.IsGreaterThan(current))
                    {
                        LabelCurrentQSVEncCVersion.Foreground = Brushes.Red;
                        LabelUpdateQSVEncCVersion.Foreground = Brushes.Green;
                    }
                }
                catch { }
            }
        }

        private static DateTime? ParseDate(string input)
        {
            // Converts string to datetime
            try
            {
                return DateTime.ParseExact(input, "yyyy.MM.dd", System.Globalization.CultureInfo.InvariantCulture);
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
            if (!Directory.Exists(Path.Combine(CURRENT_DIR, "Apps", "ffmpeg")))
            {
                Directory.CreateDirectory(Path.Combine(CURRENT_DIR, "Apps", "ffmpeg"));
            }

            string rootFolder = Path.Combine(CURRENT_DIR, "Apps", "ffmpeg", "_extracted");

            try
            {
                // Downloads ffmpeg
                await Task.Run(() => DownloadBin(FFMPEG_LAST_BUILD_DOWNLOAD_URL, Path.Combine(CURRENT_DIR, "Apps", "ffmpeg.7z")));

                // Should never happen
                if (! File.Exists(Path.Combine(CURRENT_DIR, "Apps", "ffmpeg.7z")))
                {
                    return;
                }

                // Extract downloaded 7z file
                ExtractFile(Path.Combine(CURRENT_DIR, "Apps", "ffmpeg.7z"), rootFolder);

                // Extracted folder has a subfolder which is unknown to us, so we get the first subfolder
                string ffmpegBin = Path.Combine(Directory.GetDirectories(rootFolder).First(), "bin", "ffmpeg.exe");

                // Should never happen
                if (! File.Exists(ffmpegBin))
                {
                    return;
                }

                // Delete old files
                string oldFFmpegBin = Path.Combine(CURRENT_DIR, "Apps", "ffmpeg", "ffmpeg.exe");
                if (File.Exists(oldFFmpegBin))
                {
                    File.Delete(oldFFmpegBin);
                }

                string oldFFmpegBinVersionFile = Path.Combine(CURRENT_DIR, "Apps", "ffmpeg", "ffmpeg.txt");
                if (File.Exists(oldFFmpegBinVersionFile))
                {
                    File.Delete(oldFFmpegBinVersionFile);
                }

                // Move new file
                File.Move(ffmpegBin, oldFFmpegBin);

                // Update version file
                File.WriteAllText(oldFFmpegBinVersionFile, FFmpegUpdateVersion);

                // Cleanup
                File.Delete(Path.Combine(CURRENT_DIR, "Apps", "ffmpeg.7z"));
                Directory.Delete(rootFolder, true);

                CompareLocalVersion();
            }
            catch (Exception ex)
            {
                LabelProgressBar.Content = ex.Message;
                return;
            }

            ToggleAllButtons(true);

            LabelProgressBar.Dispatcher.Invoke(() => LabelProgressBar.Content = "Finished updating FFmpeg");
        }

        private async void ButtonUpdateAomenc_Click(object sender, RoutedEventArgs e)
        {
            ToggleAllButtons(false);

            // Creates the aomenc folder if not existent
            if (!Directory.Exists(Path.Combine(CURRENT_DIR, "Apps", "aomenc")))
                Directory.CreateDirectory(Path.Combine(CURRENT_DIR, "Apps", "aomenc"));
            // Downloads aomenc
            await Task.Run(() => DownloadBin("https://jeremylee.sh/bins/aom.7z", Path.Combine(CURRENT_DIR, "Apps", "aom.7z")));
            if (File.Exists(Path.Combine(CURRENT_DIR, "Apps", "aom.7z")))
            {
                // Extracts aomenc
                ExtractFile(Path.Combine(CURRENT_DIR, "Apps", "aom.7z"), Path.Combine(Directory.GetCurrentDirectory(), "Apps", "aomenc"));
                // Writes the version to file
                if (File.Exists(Path.Combine(CURRENT_DIR, "Apps", "aomenc", "aomenc.exe")))
                {
                    // Deletes aomdec
                    if (File.Exists(Path.Combine(CURRENT_DIR, "Apps", "aomenc", "aomdec.exe")))
                    {
                        File.Delete(Path.Combine(CURRENT_DIR, "Apps", "aomenc", "aomdec.exe"));
                    }
                    // Deletes txt file
                    if (File.Exists(Path.Combine(CURRENT_DIR, "Apps", "aomenc", "aomenc.txt")))
                    {
                        File.Delete(Path.Combine(CURRENT_DIR, "Apps", "aomenc", "aomenc.txt"));
                    }

                    File.WriteAllText(Path.Combine(CURRENT_DIR, "Apps", "aomenc", "aomenc.txt"), AomencUpdateVersion);
                }
                // Deletes downloaded archive
                if (File.Exists(Path.Combine(CURRENT_DIR, "Apps", "aom.7z")))
                {
                    File.Delete(Path.Combine(CURRENT_DIR, "Apps", "aom.7z"));
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
            if (!Directory.Exists(Path.Combine(CURRENT_DIR, "Apps", "rav1e")))
                Directory.CreateDirectory(Path.Combine(CURRENT_DIR, "Apps", "rav1e"));
            // Downloads rav1e
            await Task.Run(() => DownloadBin("https://jeremylee.sh/bins/rav1e.7z", Path.Combine(CURRENT_DIR, "Apps", "rav1e.7z")));
            if (File.Exists(Path.Combine(CURRENT_DIR, "Apps", "rav1e.7z")))
            {
                // Extracts rav1e
                ExtractFile(Path.Combine(CURRENT_DIR, "Apps", "rav1e.7z"), Path.Combine(Directory.GetCurrentDirectory(), "Apps", "rav1e"));
                // Writes the version to file
                if (File.Exists(Path.Combine(CURRENT_DIR, "Apps", "rav1e", "rav1e.exe")))
                {
                    File.WriteAllText(Path.Combine(CURRENT_DIR, "Apps", "rav1e", "rav1e.txt"), Rav1eUpdateVersion);
                }
                // Deletes downloaded archive
                if (File.Exists(Path.Combine(CURRENT_DIR, "Apps", "rav1e.7z")))
                {
                    File.Delete(Path.Combine(CURRENT_DIR, "Apps", "rav1e.7z"));
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
            Directory.CreateDirectory(Path.Combine(CURRENT_DIR, "Apps", "svt-av1"));

            // Downloads rav1e
            await Task.Run(() => DownloadBin("https://jeremylee.sh/bins/svt-av1.7z", Path.Combine(CURRENT_DIR, "Apps", "svt-av1.7z")));
            if (File.Exists(Path.Combine(CURRENT_DIR, "Apps", "svt-av1.7z")))
            {
                // Extracts rav1e
                ExtractFile(Path.Combine(CURRENT_DIR, "Apps", "svt-av1.7z"), Path.Combine(Directory.GetCurrentDirectory(), "Apps", "svt-av1"));
                // Writes the version to file
                if (File.Exists(Path.Combine(CURRENT_DIR, "Apps", "svt-av1", "SvtAv1EncApp.exe")))
                {
                    // Deletes SVT-AV1 Decoder
                    if (File.Exists(Path.Combine(CURRENT_DIR, "Apps", "svt-av1", "SvtAv1EncApp.exe")))
                    {
                        File.Delete(Path.Combine(CURRENT_DIR, "Apps", "svt-av1", "SvtAv1DecApp.exe"));
                    }
                    // Deletes txt file
                    if (File.Exists(Path.Combine(CURRENT_DIR, "Apps", "svt-av1", "svt-av1.txt")))
                    {
                        File.Delete(Path.Combine(CURRENT_DIR, "Apps", "svt-av1", "svt-av1.txt"));
                    }

                    File.WriteAllText(Path.Combine(CURRENT_DIR, "Apps", "svt-av1", "svt-av1.txt"), SVTAV1UpdateVersion);
                }
                // Deletes downloaded archive
                if (File.Exists(Path.Combine(CURRENT_DIR, "Apps", "svt-av1.7z")))
                {
                    File.Delete(Path.Combine(CURRENT_DIR, "Apps", "svt-av1.7z"));
                }

                CompareLocalVersion();
            }

            ToggleAllButtons(true);

            LabelProgressBar.Dispatcher.Invoke(() => LabelProgressBar.Content = "Finished updating SVT-AV1");
        }

        private async void ButtonUpdateQSVEnc_Click(object sender, RoutedEventArgs e)
        {
            ToggleAllButtons(false);

            // Creates the ffmpeg folder if not existent
            if (!Directory.Exists(Path.Combine(CURRENT_DIR, "Apps", "qsvenc")))
            {
                Directory.CreateDirectory(Path.Combine(CURRENT_DIR, "Apps", "qsvenc"));
            }

            if (string.IsNullOrEmpty(QSVReleaseAPIGithub))
            {
                LabelProgressBar.Content = "Error: API URL was Null or Empty";
                return;
            }

            string downloadUrl = "";
            try
            {
                // Parse API
                HttpClient client = new();
                client.DefaultRequestHeaders.Add("User-Agent", "NotEnoughAV1Encodes");
                HttpResponseMessage response = await client.GetAsync(QSVReleaseAPIGithub);
                string jsonContent = await response.Content.ReadAsStringAsync();
                JArray releasesArray = JArray.Parse(jsonContent);
                downloadUrl = FindQSVDownloadUrl(releasesArray);
            }
            catch (Exception ex) { LabelProgressBar.Content = ex.Message; }

            if (string.IsNullOrEmpty(downloadUrl))
            {
                LabelProgressBar.Content = "Could not find download url!";
                return;
            }

            try
            {
                // Downloads qsvenc
                await Task.Run(() => DownloadBin(downloadUrl, Path.Combine(CURRENT_DIR, "Apps", "qsvenc.7z")));

                // Should never happen
                if (!File.Exists(Path.Combine(CURRENT_DIR, "Apps", "qsvenc.7z")))
                {
                    LabelProgressBar.Content = "Downloaded file not found!";
                    return;
                }

                Directory.Delete(Path.Combine(CURRENT_DIR, "Apps", "qsvenc"), true);
                Directory.CreateDirectory(Path.Combine(CURRENT_DIR, "Apps", "qsvenc"));

                // Extract downloaded 7z file
                ExtractFile(Path.Combine(CURRENT_DIR, "Apps", "qsvenc.7z"), Path.Combine(CURRENT_DIR, "Apps", "qsvenc"));

                // Cleanup
                File.Delete(Path.Combine(CURRENT_DIR, "Apps", "qsvenc.7z"));

                GetLocalQSVEncCVersion();
                CompareLocalVersion();
            }
            catch { }

            ToggleAllButtons(true);

            LabelProgressBar.Dispatcher.Invoke(() => LabelProgressBar.Content = "Finished updating QSVEncC");
        }

        private async void ButtonUpdateMKVToolnix_Click(object sender, RoutedEventArgs e)
        {
            ToggleAllButtons(false);

            if (string.IsNullOrEmpty(MKVToolnixDownloadURL))
            {
                LabelProgressBar.Content = "Error: Download URL was Null or Empty";
                return;
            }

            try
            {
                // Downloads qsvenc
                await Task.Run(() => DownloadBin(MKVToolnixDownloadURL, Path.Combine(CURRENT_DIR, "Apps", "mkvtoolnix.7z")));

                // Should never happen
                if (!File.Exists(Path.Combine(CURRENT_DIR, "Apps", "mkvtoolnix.7z")))
                {
                    LabelProgressBar.Content = "Downloaded file not found!";
                    return;
                }

                if (Directory.Exists(Path.Combine(CURRENT_DIR, "Apps", "mkvtoolnix")))
                {
                    Directory.Delete(Path.Combine(CURRENT_DIR, "Apps", "mkvtoolnix"), true);
                }
                
                Directory.CreateDirectory(Path.Combine(CURRENT_DIR, "Apps", "mkvtoolnix"));

                // Extract downloaded 7z file
                ExtractFile(Path.Combine(CURRENT_DIR, "Apps", "mkvtoolnix.7z"), Path.Combine(CURRENT_DIR, "Apps"));

                // Cleanup
                File.Delete(Path.Combine(CURRENT_DIR, "Apps", "mkvtoolnix.7z"));

                GetLocalMKVToolnixVersion();
                CompareLocalVersion();
            }
            catch { }

            ToggleAllButtons(true);

            LabelProgressBar.Dispatcher.Invoke(() => LabelProgressBar.Content = "Finished updating MKVToolNix");
        }

        private static string FindQSVDownloadUrl(JArray releasesArray)
        {
            foreach (var release in releasesArray)
            {
                JToken browserDownloadUrl = release["browser_download_url"];
                if (browserDownloadUrl != null && browserDownloadUrl.Type == JTokenType.String)
                {
                    string url = browserDownloadUrl.Value<string>();
                    if (url.Contains("x64"))
                    {
                        return url;
                    }
                }
            }

            return null;
        }

        private async Task DownloadBin(string DownloadURL, string PathToFile)
        {
            // Downloads the archive provided in the Link
            try
            {
                using (var httpClient = new HttpClient())
                {
                    using (var response = await httpClient.GetAsync(DownloadURL, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        long? contentLength = response.Content.Headers.ContentLength;

                        using (var stream = await response.Content.ReadAsStreamAsync())
                        {
                            using (var fileStream = new FileStream(PathToFile, System.IO.FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                            {
                                var buffer = new byte[8192];
                                int bytesRead;
                                long totalBytesRead = 0;

                                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                {
                                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                                    totalBytesRead += bytesRead;

                                    // Calculate progress
                                    double progressPercentage = (double)totalBytesRead / contentLength.Value * 100;

                                    // Update UI
                                    Dispatcher.Invoke(() =>
                                    {
                                        ProgressBar.Value = progressPercentage;
                                        LabelProgressBar.Content = $"{Math.Round(totalBytesRead / 1024f / 1024f, 1)}MB / {Math.Round(contentLength.Value / 1024f / 1024f, 1)}MB - {progressPercentage:F1}%";
                                        Title = $"Updater {progressPercentage:F1}%";
                                    });
                                }
                            }
                        }
                    }
                }

                // After download completes
                Dispatcher.Invoke(() =>
                {
                    ProgressBar.Value = 0;
                    LabelProgressBar.Content = "Extracting...";
                    Title = "Updater";
                });
            }
            catch 
            {
                Dispatcher.Invoke(() =>
                {
                    ProgressBar.Value = 0;
                    LabelProgressBar.Content = "Error while downloading!";
                    Title = "Updater";
                });

            }
        }

        private static void ExtractFile(string source, string destination)
        {
            // Extracts the downloaded archives with 7zip
            string zPath = Path.Combine(CURRENT_DIR, "Apps", "7zip", "7za.exe");
            
            // detect if we have 7-zip
            if (File.Exists(zPath))
            {
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
            else
            {
                MessageBox.Show(Strings._7ZipNotDetectedMessage + Path.Combine(CURRENT_DIR, "Apps", "7zip"), Strings._7ZipNotDetectedTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
