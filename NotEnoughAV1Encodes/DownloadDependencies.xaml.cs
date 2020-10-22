using HtmlAgilityPack;
using Octokit;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml;

namespace NotEnoughAV1Encodes
{
    public partial class DownloadDependencies : Window
    {
        public static string aomUrlAppVeyor, rav1eUrlGithub, svtav1UrlGithub, svtav1UrlGithubLib, ffmpegUrlAppveyor;
        public static string aomVersionUpdate, rav1eVersionUpdate, svtav1VersionUpdate, ffmpegVersionUpdate;
        public static string aomVersionUpdateJeremy, rav1eVersionUpdateJeremy, svtav1VersionUpdateJeremy, ffmpegVersionUpdateJeremy;
        public static string aomVersionCurrent, rav1eVersionCurrent, svtav1VersionCurrent, ffmpegVersionCurrent;
        public static string currentDir = Directory.GetCurrentDirectory();
        public static string gyanDevffmpegName; //Because of the annoying subfolder structure it needs the complete git build name
        bool startup = true;

        private void ComboBoxUpdateSource_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            setVersionLabels();
            CompareVersion();
        }

        public DownloadDependencies(bool darkMode)
        {
            InitializeComponent();
            startup = false;
            if (darkMode)
            {
                SolidColorBrush white = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                SolidColorBrush dark = new SolidColorBrush(Color.FromRgb(33, 33, 33));
                WindowUpdate.Background = new SolidColorBrush(Color.FromRgb(25, 25, 25));
                ButtonUpdateAom.Background = dark;
                ButtonUpdateAom.Foreground = white;
                ButtonUpdateFfmpeg.Background = dark;
                ButtonUpdateFfmpeg.Foreground = white;
                ButtonUpdateRav1e.Background = dark;
                ButtonUpdateRav1e.Foreground = white;
                ButtonUpdateSVT.Background = dark;
                ButtonUpdateSVT.Foreground = white;
                ProgressBarDownload.Background = dark;
                LabelSource.Foreground = white;
                LabelUpdateAomenc.Foreground = white;
                LabelUpdateFfmpeg.Foreground = white;
                LabelUpdateRav1e.Foreground = white;
                LabelUpdateSvtav1.Foreground = white;
                LabelCurrentVersionAomenc.Foreground = white;
                LabelCurrentVersionffmpeg.Foreground = white;
                LabelCurrentVersionRav1e.Foreground = white;
                LabelCurrentVersionSVT.Foreground = white;
            }
            SmallFunctions.checkCreateFolder(Path.Combine(currentDir, "Apps"));

            //Most websites require SSL/TLS, but .NET does not support it by default on Windows 7
            //It has to be manually activated, inorder to avoid webclient exception / crash for the update parsing
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; 
            //-----------------------------------------------------------------

            DownloadUpdateXML();
            ParseUpdateXML();
            ParseHTMLJeremylee();
            ParseRav1eGithub();
            ParseSVTAV1Github();
            ParseFFMPEGGyanDev();
            getLocalVersion();
            setVersionLabels();
            CompareVersion();            
        }

        private void DownloadUpdateXML()
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    //Downloads the recent update file
                    var update = new Uri("https://raw.githubusercontent.com/Alkl58/LatestAV1Builds/master/update.xml");
                    if (File.Exists(Path.Combine(currentDir, "Apps", "update.xml")))
                        File.Delete(Path.Combine(currentDir, "Apps", "update.xml"));
                    webClient.DownloadFile(update, Path.Combine(currentDir, "Apps", "update.xml"));
                }
            }
            catch { }
        }

        private void ParseUpdateXML()
        {
            //The only reason for this is, that I have not figured out yet, how to parse directly from appveyor
            if (File.Exists(Path.Combine(currentDir, "Apps", "update.xml")))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(Path.Combine(currentDir, "Apps","update.xml"));
                XmlNodeList node = doc.GetElementsByTagName("Update");
                foreach (XmlNode n in node[0].ChildNodes)
                {
                    switch (n.Name)
                    {
                        case "AppVeyorAomVersion": aomVersionUpdate = n.InnerText;  break;
                        case "AppVeyorAomUrl": aomUrlAppVeyor = n.InnerText; break;
                    }
                }
            }
        }

        private void getLocalVersion()
        {
            if (File.Exists(Path.Combine(currentDir, "Apps", "Encoder", "aomenc.txt"))) 
            {
                aomVersionCurrent = File.ReadAllText(Path.Combine(currentDir, "Apps", "Encoder", "aomenc.txt"));
            }
            else { aomVersionCurrent = "unknown"; }
            if (File.Exists(Path.Combine(currentDir, "Apps", "Encoder", "rav1e.txt")))
            {
                rav1eVersionCurrent = File.ReadAllText(Path.Combine(currentDir, "Apps", "Encoder", "rav1e.txt"));
            }
            else { rav1eVersionCurrent = "unknown"; }
            if (File.Exists(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1EncApp.txt")))
            {
                svtav1VersionCurrent = File.ReadAllText(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1EncApp.txt"));
            }
            else { svtav1VersionCurrent = "unknown"; }
            if (File.Exists(Path.Combine(currentDir, "Apps", "ffmpeg", "ffmpeg.txt")))
            {
                ffmpegVersionCurrent = File.ReadAllText(Path.Combine(currentDir, "Apps", "ffmpeg", "ffmpeg.txt"));
            }
            else { ffmpegVersionCurrent = "unknown"; }
        }

        private void setVersionLabels()
        {
            if (startup == false)
            {
                LabelCurrentVersionAomenc.Content = "Current Version: " + aomVersionCurrent;
                LabelCurrentVersionRav1e.Content = "Current Version: " + rav1eVersionCurrent;
                LabelCurrentVersionSVT.Content = "Current Version: " + svtav1VersionCurrent;
                LabelCurrentVersionffmpeg.Content = "Current Version: " + ffmpegVersionCurrent;
                
                if (ComboBoxUpdateSource.SelectedIndex == 0)
                {
                    LabelUpdateAomenc.Content = "Update Version: " + aomVersionUpdate;
                    LabelUpdateRav1e.Content = "Update Version: " + rav1eVersionUpdate;
                    LabelUpdateSvtav1.Content = "Update Version: " + svtav1VersionUpdate;
                    LabelUpdateFfmpeg.Content = "Update Version: " + ffmpegVersionUpdate;
                }
                else
                {
                    LabelUpdateAomenc.Content = "Update Version: " + aomVersionUpdateJeremy;
                    LabelUpdateRav1e.Content = "Update Version: " + rav1eVersionUpdateJeremy;
                    LabelUpdateSvtav1.Content = "Update Version: " + svtav1VersionUpdateJeremy;
                    LabelUpdateFfmpeg.Content = "Update Version: " + ffmpegVersionUpdateJeremy;
                }
            }
        }

        private void CompareVersion()
        {
            if (LabelCurrentVersionAomenc != null)
            {
                if (ComboBoxUpdateSource.SelectedIndex == 0)
                {
                    if (ParseDate(aomVersionUpdate) > ParseDate(aomVersionCurrent)) { LabelCurrentVersionAomenc.Foreground = Brushes.Red; LabelUpdateAomenc.Foreground = Brushes.Green; }
                    else { LabelCurrentVersionAomenc.Foreground = Brushes.Green; }
                    if (ParseDate(rav1eVersionUpdate) > ParseDate(rav1eVersionCurrent)) { LabelCurrentVersionRav1e.Foreground = Brushes.Red; LabelUpdateRav1e.Foreground = Brushes.Green; }
                    else { LabelCurrentVersionRav1e.Foreground = Brushes.Green; }
                    if (ParseDate(svtav1VersionUpdate) > ParseDate(svtav1VersionCurrent)) { LabelCurrentVersionSVT.Foreground = Brushes.Red; LabelUpdateSvtav1.Foreground = Brushes.Green; }
                    else { LabelCurrentVersionSVT.Foreground = Brushes.Green; }
                    if (ParseDate(ffmpegVersionUpdate) > ParseDate(ffmpegVersionCurrent)) { LabelCurrentVersionffmpeg.Foreground = Brushes.Red; LabelUpdateFfmpeg.Foreground = Brushes.Green; }
                    else { LabelCurrentVersionffmpeg.Foreground = Brushes.Green; }
                }
                else
                {
                    if (ParseDate(aomVersionUpdateJeremy) > ParseDate(aomVersionCurrent)) { LabelCurrentVersionAomenc.Foreground = Brushes.Red; LabelUpdateAomenc.Foreground = Brushes.Green; }
                    else { LabelCurrentVersionAomenc.Foreground = Brushes.Green; }
                    if (ParseDate(rav1eVersionUpdateJeremy) > ParseDate(rav1eVersionCurrent)) { LabelCurrentVersionRav1e.Foreground = Brushes.Red; LabelUpdateRav1e.Foreground = Brushes.Green; }
                    else { LabelCurrentVersionRav1e.Foreground = Brushes.Green; }
                    if (ParseDate(svtav1VersionUpdateJeremy) > ParseDate(svtav1VersionCurrent)) { LabelCurrentVersionSVT.Foreground = Brushes.Red; LabelUpdateSvtav1.Foreground = Brushes.Green; }
                    else { LabelCurrentVersionSVT.Foreground = Brushes.Green; }
                    if (ParseDate(ffmpegVersionUpdateJeremy) > ParseDate(ffmpegVersionCurrent)) { LabelCurrentVersionffmpeg.Foreground = Brushes.Red; LabelUpdateFfmpeg.Foreground = Brushes.Green; }
                    else { LabelCurrentVersionffmpeg.Foreground = Brushes.Green; }
                }
                
            }
        }

        private DateTime? ParseDate(string input) {

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

        private async void ButtonUpdateAom_Click(object sender, RoutedEventArgs e)
        {
            SmallFunctions.checkCreateFolder(Path.Combine(currentDir, "Apps", "Encoder"));
            ProgressBarDownload.IsIndeterminate = true;
            if (ComboBoxUpdateSource.SelectedIndex == 0)
            {
                await Task.Run(() => DownloadBin(aomUrlAppVeyor, Path.Combine(currentDir, "Apps", "Encoder", "aomencnew.exe")));
                if (File.Exists(Path.Combine(currentDir, "Apps", "Encoder", "aomencnew.exe"))) 
                {
                    File.Delete(Path.Combine(currentDir, "Apps", "Encoder", "aomenc.exe"));
                    File.Move(Path.Combine(currentDir, "Apps", "Encoder", "aomencnew.exe"), Path.Combine(currentDir, "Apps", "Encoder", "aomenc.exe"));
                    if (File.Exists(Path.Combine(currentDir, "Apps", "Encoder", "aomenc.exe")))
                    {
                        if (File.Exists(Path.Combine(currentDir, "Apps", "Encoder", "aomenc.txt")))
                            File.Delete(Path.Combine(currentDir, "Apps", "Encoder", "aomenc.txt"));
                        File.WriteAllText(Path.Combine(currentDir, "Apps", "Encoder", "aomenc.txt"), aomVersionUpdate);
                    }
                }
            }
            else
            {
                await Task.Run(() => DownloadBin("https://jeremylee.sh/data/bin/aom.7z", Path.Combine(currentDir, "Apps", "aom.7z")));
                if (File.Exists(Path.Combine(currentDir, "Apps", "aom.7z")))
                {
                    ExtractFile(Path.Combine(currentDir, "Apps", "aom.7z"), Path.Combine(currentDir, "Apps", "Encoder"));
                    if (File.Exists(Path.Combine(currentDir, "Apps", "Encoder", "aomdec.exe")))
                        File.Delete(Path.Combine(currentDir, "Apps", "Encoder", "aomdec.exe"));
                    if (File.Exists(Path.Combine(currentDir, "Apps", "aom.7z")))
                        File.Delete(Path.Combine(currentDir, "Apps", "aom.7z"));
                    if (File.Exists(Path.Combine(currentDir, "Apps", "Encoder", "aomenc.exe")))
                    {
                        if (File.Exists(Path.Combine(currentDir, "Apps", "Encoder", "aomenc.txt")))
                            File.Delete(Path.Combine(currentDir, "Apps", "Encoder", "aomenc.txt"));
                        File.WriteAllText(Path.Combine(currentDir, "Apps", "Encoder", "aomenc.txt"), aomVersionUpdateJeremy);
                    }
                }
            }

            ProgressBarDownload.IsIndeterminate = false;
            getLocalVersion();
            setVersionLabels();
            CompareVersion();
        }

        private async void ButtonUpdateRav1e_Click(object sender, RoutedEventArgs e)
        {
            SmallFunctions.checkCreateFolder(Path.Combine(currentDir, "Apps", "Encoder"));
            ProgressBarDownload.IsIndeterminate = true;
            if (ComboBoxUpdateSource.SelectedIndex == 0)
            {
                await Task.Run(() => DownloadBin(rav1eUrlGithub, Path.Combine(currentDir, "Apps", "Encoder", "rav1enew.exe")));
                if (File.Exists(Path.Combine(currentDir, "Apps", "Encoder", "rav1enew.exe")))
                {
                    File.Delete(Path.Combine(currentDir, "Apps", "Encoder", "rav1e.exe"));
                    File.Move(Path.Combine(currentDir, "Apps", "Encoder", "rav1enew.exe"), Path.Combine(currentDir, "Apps", "Encoder", "rav1e.exe"));
                    if (File.Exists(Path.Combine(currentDir, "Apps", "Encoder", "rav1e.exe")))
                    {
                        if (File.Exists(Path.Combine(currentDir, "Apps", "Encoder", "rav1e.txt")))
                            File.Delete(Path.Combine(currentDir, "Apps", "Encoder", "rav1e.txt"));
                        File.WriteAllText(Path.Combine(currentDir, "Apps", "Encoder", "rav1e.txt"), rav1eVersionUpdate);
                    }
                }
            }
            else
            {
                await Task.Run(() => DownloadBin("https://jeremylee.sh/data/bin/rav1e.7z", Path.Combine(currentDir, "Apps", "rav1e.7z")));
                ExtractFile(Path.Combine(currentDir, "Apps", "rav1e.7z"), Path.Combine(currentDir, "Apps", "Encoder"));
                if (File.Exists(Path.Combine(currentDir, "Apps", "rav1e.7z")))
                    File.Delete(Path.Combine(currentDir, "Apps", "rav1e.7z"));
                if (File.Exists(Path.Combine(currentDir, "Apps", "Encoder", "rav1e.exe")))
                {
                    if (File.Exists(Path.Combine(currentDir, "Apps", "Encoder", "rav1e.txt")))
                        File.Delete(Path.Combine(currentDir, "Apps", "Encoder", "rav1e.txt"));
                    File.WriteAllText(Path.Combine(currentDir, "Apps", "Encoder", "rav1e.txt"), rav1eVersionUpdateJeremy);
                }
            }

            ProgressBarDownload.IsIndeterminate = false;
            getLocalVersion();
            setVersionLabels();
            CompareVersion();
        }

        private async void ButtonUpdateSVT_Click(object sender, RoutedEventArgs e)
        {
            SmallFunctions.checkCreateFolder(Path.Combine(currentDir, "Apps", "Encoder"));
            ProgressBarDownload.IsIndeterminate = true;
            if (ComboBoxUpdateSource.SelectedIndex == 0)
            {
                await Task.Run(() => DownloadBin(svtav1UrlGithub, Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1EncApp.zip")));
                await Task.Run(() => DownloadBin(svtav1UrlGithubLib, Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1Enclib.zip")));
                if (File.Exists(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1EncApp.zip")))
                {

                    File.Delete(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1EncApp.exe"));

                    //Extracts the downloaded zip files
                    ExtractFile(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1EncApp.zip"), Path.Combine(currentDir, "Apps", "Encoder"));
                    ExtractFile(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1Enclib.zip"), Path.Combine(currentDir, "Apps", "Encoder"));

                    //Deletes the zip files if it finds the extracted dependencies
                    if (File.Exists(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1EncApp.exe")))
                        File.Delete(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1EncApp.zip"));
                    if (File.Exists(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1Enc.lib")))
                        File.Delete(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1Enclib.zip"));

                    //Writes the Version to txt file
                    if (File.Exists(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1EncApp.exe")))
                    {
                        if (File.Exists(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1EncApp.txt")))
                            File.Delete(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1EncApp.txt"));
                        File.WriteAllText(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1EncApp.txt"), svtav1VersionUpdate);
                    }
                }
            }
            else
            {
                await Task.Run(() => DownloadBin("https://jeremylee.sh/data/bin/svt-av1.7z", Path.Combine(currentDir, "Apps", "svtav1.7z")));
                ExtractFile(Path.Combine(currentDir, "Apps", "svtav1.7z"), Path.Combine(currentDir, "Apps", "Encoder"));
                if (File.Exists(Path.Combine(currentDir, "Apps", "svtav1.7z")))
                    File.Delete(Path.Combine(currentDir, "Apps", "svtav1.7z"));
                if (File.Exists(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1EncApp.exe")))
                {
                    if (File.Exists(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1Enc.lib")))
                        File.Delete(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1Enc.lib"));
                    if (File.Exists(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1DecApp.exe")))
                        File.Delete(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1DecApp.exe"));
                    if (File.Exists(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1EncApp.txt")))
                        File.Delete(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1EncApp.txt"));
                    File.WriteAllText(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1EncApp.txt"), svtav1VersionUpdateJeremy);
                }
            }

            ProgressBarDownload.IsIndeterminate = false;
            getLocalVersion();
            setVersionLabels();
            CompareVersion();
        }

        private async void ButtonUpdateFfmpeg_Click(object sender, RoutedEventArgs e)
        {
            SmallFunctions.checkCreateFolder(Path.Combine(currentDir, "Apps", "ffmpeg"));
            ProgressBarDownload.IsIndeterminate = true;

            if (ComboBoxUpdateSource.SelectedIndex == 0)
            {
                await Task.Run(() => DownloadBin("https://www.gyan.dev/ffmpeg/builds/ffmpeg-git-full.7z", Path.Combine(currentDir, "Apps", "ffmpeg.7z")));
                ExtractFile(Path.Combine(currentDir, "Apps", "ffmpeg.7z"), Path.Combine(Directory.GetCurrentDirectory(), "Apps", "ffmpeg"));
                File.Move(Path.Combine(currentDir, "Apps", "ffmpeg", gyanDevffmpegName, "bin", "ffmpeg.exe"), Path.Combine(currentDir, "Apps", "ffmpeg", "ffmpeg.exe"));
                File.Move(Path.Combine(currentDir, "Apps", "ffmpeg", gyanDevffmpegName, "bin", "ffprobe.exe"), Path.Combine(currentDir, "Apps", "ffmpeg", "ffprobe.exe"));
                if (File.Exists(Path.Combine(currentDir, "Apps", "ffmpeg", "ffmpeg.exe")))
                {
                    if (File.Exists(Path.Combine(currentDir, "Apps", "ffmpeg", "ffmpeg.txt")))
                        File.Delete(Path.Combine(currentDir, "Apps", "ffmpeg", "ffmpeg.txt"));
                    File.WriteAllText(Path.Combine(currentDir, "Apps", "ffmpeg", "ffmpeg.txt"), ffmpegVersionUpdate);
                    Directory.Delete(Path.Combine(currentDir, "Apps", "ffmpeg", gyanDevffmpegName), true);
                }
            }
            else
            {
                await Task.Run(() => DownloadBin("https://jeremylee.sh/data/bin/ffmpeg.7z", Path.Combine(currentDir, "Apps", "ffmpeg.7z")));
                ExtractFile(Path.Combine(currentDir, "Apps", "ffmpeg.7z"), Path.Combine(Directory.GetCurrentDirectory(), "Apps", "ffmpeg"));
                await Task.Run(() => DownloadBin("https://jeremylee.sh/data/bin/ffprobe.7z", Path.Combine(currentDir, "Apps", "ffprobe.7z")));
                ExtractFile(Path.Combine(currentDir, "Apps", "ffprobe.7z"), Path.Combine(currentDir, "Apps", "ffmpeg"));
                if (File.Exists(Path.Combine(currentDir, "Apps", "ffprobe.7z")))
                    File.Delete(Path.Combine(currentDir, "Apps", "ffprobe.7z"));
                if (File.Exists(Path.Combine(currentDir, "Apps", "ffmpeg", "ffmpeg.exe")))
                {
                    if (File.Exists(Path.Combine(currentDir, "Apps", "ffmpeg", "ffmpeg.txt")))
                        File.Delete(Path.Combine(currentDir, "Apps", "ffmpeg", "ffmpeg.txt"));
                    File.WriteAllText(Path.Combine(currentDir, "Apps", "ffmpeg", "ffmpeg.txt"), ffmpegVersionUpdateJeremy);
                }
            }
            if (File.Exists(Path.Combine(currentDir, "Apps", "ffmpeg.7z")))
                File.Delete(Path.Combine(currentDir, "Apps", "ffmpeg.7z"));
            ProgressBarDownload.IsIndeterminate = false;
            getLocalVersion();
            setVersionLabels();
            CompareVersion();
        }

        private async Task DownloadBin(string DownloadURL, string PathToFile)
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    var aom = new Uri(DownloadURL);
                    await webClient.DownloadFileTaskAsync(aom, PathToFile);
                }
            }
            catch { }
        }

        public void ExtractFile(string source, string destination)
        {
            string zPath = @"C:\Program Files\7-Zip\7zG.exe";
            // change the path and give yours 
            try
            {
                ProcessStartInfo pro = new ProcessStartInfo();
                pro.WindowStyle = ProcessWindowStyle.Hidden;
                pro.FileName = zPath;
                pro.Arguments = "x \"" +  source + "\" -o" + '\u0022' + destination + '\u0022';
                Process x = Process.Start(pro);
                x.WaitForExit();
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ParseHTMLJeremylee()
        {
            try
            {
                HtmlWeb web = new HtmlWeb();
                HtmlDocument doc = web.Load("https://jeremylee.sh/bin.html");

                //Full XPATH Node selection - will break if owner of website rearrange stuff
                var nodeffmpeg = doc.DocumentNode.SelectSingleNode("/html/body/fieldset/pre[1]/span[2]");
                var nodeAom = doc.DocumentNode.SelectSingleNode("/html/body/fieldset/pre[1]/span[33]");
                var nodeRav1e = doc.DocumentNode.SelectSingleNode("/html/body/fieldset/pre[1]/span[45]");
                var nodeSvtav1 = doc.DocumentNode.SelectSingleNode("/html/body/fieldset/pre[1]/span[71]");

                string ffmpegVersion = nodeffmpeg.InnerHtml;
                ffmpegVersion = ffmpegVersion.Replace("-", ".");
                ffmpegVersionUpdateJeremy = ffmpegVersion.Split(' ')[0];

                string aomencVersion = nodeAom.InnerHtml;
                aomencVersion = aomencVersion.Replace("-", ".");
                aomVersionUpdateJeremy = aomencVersion.Split(' ')[0];

                string rav1eVersion = nodeRav1e.InnerHtml;
                rav1eVersion = rav1eVersion.Replace("-", ".");
                rav1eVersionUpdateJeremy = rav1eVersion.Split(' ')[0];

                string svtav1Version = nodeSvtav1.InnerHtml;
                svtav1Version = svtav1Version.Replace("-", ".");
                svtav1VersionUpdateJeremy = svtav1Version.Split(' ')[0];
            }
            catch (Exception ex) { SmallFunctions.Logging(ex.Message); }
        }

        private void ParseRav1eGithub()
        {
            try
            {
                //Parses the latest rav1e Release directly from Github
                var client = new GitHubClient(new ProductHeaderValue("neav1e"));
                var releases = client.Repository.Release.GetAll("xiph", "rav1e").Result;
                var latest = releases[0];
                string rav1eUrlRepo = latest.HtmlUrl;

                rav1eUrlGithub = rav1eUrlRepo.Replace("tag", "download") + "/rav1e.exe"; //The download Path for the latest rav1e build (hopefully)
                rav1eVersionUpdate = latest.CreatedAt.ToString("yyyy.MM.dd");
            }
            catch (Exception ex) { SmallFunctions.Logging(ex.Message); }

        }

        private void ParseSVTAV1Github()
        {
            try
            {
                //Parses the latest SVT-AV1 Release directly from Github
                var client = new GitHubClient(new ProductHeaderValue("neav1e"));
                var releases = client.Repository.Release.GetAll("AOMediaCodec", "SVT-AV1").Result;
                var latest = releases[0];
                string svtUrlRepo = latest.HtmlUrl;

                svtav1VersionUpdate = latest.CreatedAt.ToString("yyyy.MM.dd");
                svtav1UrlGithub = svtUrlRepo.Replace("tag", "download") + "/SvtAv1EncApp.zip"; //The download url for the latest svt-av1 build (hopefully)
                svtav1UrlGithubLib = svtUrlRepo.Replace("tag", "download") + "/SvtAv1Enclib.zip";
            }
            catch (Exception ex) { SmallFunctions.Logging(ex.Message); }

        }

        private void ParseFFMPEGGyanDev()
        {
            //Parses the version info from GyanDev
            WebClient wc = new WebClient();
            byte[] raw = wc.DownloadData("https://www.gyan.dev/ffmpeg/builds/git-version");
            string GyanDev = System.Text.Encoding.UTF8.GetString(raw);
            ffmpegVersionUpdate = GyanDev.Replace("-", ".").Remove(GyanDev.Length - 15); // Some basic formatting
            gyanDevffmpegName = "ffmpeg-" + GyanDev + "-full_build"; //For later correct exe extracting the complete name of the folder is needed
        }
    }
}
