using HtmlAgilityPack;
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
        public static string aomUrlAppVeyor, rav1eUrlAppVeyorGithub, svtav1UrlAppveyorGithub, svtav1UrlAppveyorGithubLib, ffmpegUrlAppveyor;
        public static string aomVersionUpdate, rav1eVersionUpdate, svtav1VersionUpdate, ffmpegVersionUpdate;
        public static string aomVersionUpdateJeremy, rav1eVersionUpdateJeremy, svtav1VersionUpdateJeremy, ffmpegVersionUpdateJeremy;
        public static string aomVersionCurrent, rav1eVersionCurrent, svtav1VersionCurrent, ffmpegVersionCurrent;
        public static string currentDir = Directory.GetCurrentDirectory();
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
            DownloadUpdateXML();
            ParseUpdateXML();
            ParseHTML();
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
                        case "GithubRav1eVersion": rav1eVersionUpdate = n.InnerText; break;
                        case "GithubRav1eUrl":  rav1eUrlAppVeyorGithub = n.InnerText; break;
                        case "GithubSVTVersion":   svtav1VersionUpdate = n.InnerText; break;
                        case "GihubSVTUrlEnc": svtav1UrlAppveyorGithub = n.InnerText; break;
                        case "GithubSVTUrlLib": svtav1UrlAppveyorGithubLib = n.InnerText; break;
                        case "ZeranoeVersion": ffmpegVersionUpdate = n.InnerText; break;
                        case "ZeranoeVersionUrl": ffmpegUrlAppveyor = n.InnerText; break;
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
                await Task.Run(() => DownloadAom(aomUrlAppVeyor, Path.Combine(currentDir, "Apps", "Encoder", "aomencnew.exe")));
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
                await Task.Run(() => DownloadAom("https://jeremylee.sh/data/bin/aom.7z", Path.Combine(currentDir, "Apps", "aom.7z")));
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
                await Task.Run(() => DownloadAom(rav1eUrlAppVeyorGithub, Path.Combine(currentDir, "Apps", "Encoder", "rav1enew.exe")));
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
                await Task.Run(() => DownloadAom("https://jeremylee.sh/data/bin/rav1e.7z", Path.Combine(currentDir, "Apps", "rav1e.7z")));
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
                await Task.Run(() => DownloadAom(svtav1UrlAppveyorGithub, Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1EncAppnew.exe")));
                await Task.Run(() => DownloadAom(svtav1UrlAppveyorGithubLib, Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1Encnew.lib")));
                if (File.Exists(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1EncAppnew.exe")))
                {
                    File.Delete(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1EncApp.exe"));
                    File.Move(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1EncAppnew.exe"), Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1EncApp.exe"));

                    if (File.Exists(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1Encnew.lib")))
                    {
                        if (File.Exists(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1Enc.lib")))
                            File.Delete(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1Enc.lib"));
                        File.Move(Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1Encnew.lib"), Path.Combine(currentDir, "Apps", "Encoder", "SvtAv1Enc.lib"));
                    }
                    
                    
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
                await Task.Run(() => DownloadAom("https://jeremylee.sh/data/bin/svt-av1.7z", Path.Combine(currentDir, "Apps", "svtav1.7z")));
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
                await Task.Run(() => DownloadAom(ffmpegUrlAppveyor, Path.Combine(currentDir, "Apps", "ffmpeg.zip")));
                ExtractFile(Path.Combine(currentDir, "Apps", "ffmpeg.zip"), Path.Combine(Directory.GetCurrentDirectory(), "Apps", "ffmpeg"));
                string archiveName = ffmpegUrlAppveyor;
                archiveName = archiveName.Replace("https://ffmpeg.zeranoe.com/builds/win64/static/", "");
                archiveName = archiveName.Replace(".zip", "");
                if (File.Exists(Path.Combine(currentDir, "Apps", "ffmpeg", archiveName, "bin", "ffmpeg.exe")))
                {
                    if (File.Exists(Path.Combine(currentDir, "Apps", "ffmpeg.zip")))
                        File.Delete(Path.Combine(currentDir, "Apps", "ffmpeg.zip"));
                    if (File.Exists(Path.Combine(currentDir, "Apps", "ffmpeg", "ffmpeg.exe")))
                        File.Delete(Path.Combine(currentDir, "Apps", "ffmpeg", "ffmpeg.exe"));
                    if (File.Exists(Path.Combine(currentDir, "Apps", "ffmpeg", "ffprobe.exe")))
                        File.Delete(Path.Combine(currentDir, "Apps", "ffmpeg", "ffprobe.exe"));
                    File.Move(Path.Combine(currentDir, "Apps", "ffmpeg", archiveName, "bin", "ffmpeg.exe"), Path.Combine(currentDir, "Apps", "ffmpeg", "ffmpeg.exe"));
                    File.Move(Path.Combine(currentDir, "Apps", "ffmpeg", archiveName, "bin", "ffprobe.exe"), Path.Combine(currentDir, "Apps", "ffmpeg", "ffprobe.exe"));
                    if (File.Exists(Path.Combine(currentDir, "Apps", "ffmpeg", "ffmpeg.txt")))
                        File.Delete(Path.Combine(currentDir, "Apps", "ffmpeg", "ffmpeg.txt"));
                    File.WriteAllText(Path.Combine(currentDir, "Apps", "ffmpeg", "ffmpeg.txt"), ffmpegVersionUpdate);
                    Directory.Delete(Path.Combine(currentDir, "Apps", "ffmpeg", archiveName), true);
                }                
            }
            else
            {
                await Task.Run(() => DownloadAom("https://jeremylee.sh/data/bin/ffmpeg.7z", Path.Combine(currentDir, "Apps", "ffmpeg.7z")));
                ExtractFile(Path.Combine(currentDir, "Apps", "ffmpeg.7z"), Path.Combine(Directory.GetCurrentDirectory(), "Apps", "ffmpeg"));
                if (File.Exists(Path.Combine(currentDir, "Apps", "ffmpeg.7z")))
                    File.Delete(Path.Combine(currentDir, "Apps", "ffmpeg.7z"));
                await Task.Run(() => DownloadAom("https://jeremylee.sh/data/bin/ffprobe.7z", Path.Combine(currentDir, "Apps", "ffprobe.7z")));
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

            ProgressBarDownload.IsIndeterminate = false;
            getLocalVersion();
            setVersionLabels();
            CompareVersion();
        }

        private async Task DownloadAom(string DownloadURL, string PathToFile)
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

        private void ParseHTML()
        {
            try
            {
                HtmlWeb web = new HtmlWeb();
                HtmlAgilityPack.HtmlDocument doc = web.Load("https://jeremylee.sh/bin.html");

                var nodeffmpeg = doc.DocumentNode.SelectSingleNode("/html/body/fieldset/pre[1]/span[1]");
                var nodeAom = doc.DocumentNode.SelectSingleNode("/html/body/fieldset/pre[1]/span[19]");
                var nodeRav1e = doc.DocumentNode.SelectSingleNode("/html/body/fieldset/pre[1]/span[25]");
                var nodeSvtav1 = doc.DocumentNode.SelectSingleNode("/html/body/fieldset/pre[1]/span[31]");

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
            catch { }
        }
    }
}
