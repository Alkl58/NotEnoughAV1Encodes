using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;

namespace NotEnoughAV1Encodes
{
    public partial class MainWindow : Window
    {
        //----- General Settings -------------------------------||
        public static string videoInput = "";
        public static string videoOutput = "";
        public static string workingTempDirectory = "";
        public static string exeffmpegPath = "";
        public static string exeffprobePath = "";
        public static string exeaomencPath = "";
        public static string exerav1ePath = "";
        public static string exesvtav1Path = "";
        public static string currentDir = "";
        public static string chunksDir = "";
        public static string streamFrameRate = "";
        public static string streamFrameRateLabel;
        public static string[] videoChunks;
        public static string numberofvideoChunks;
        public static string streamLength;
        public static int chunkLengthSplit = 120;
        public static int maxConcurrencyEncodes = 4;
        public static bool reencodeBeforeMainEncode = false;
        public static bool resumeMode = false;
        //------------------------------------------------------||
        //----- aomenc Settings --------------------------------||
        public static int numberOfPasses = 1;
        public static string aomenc = "";
        public static string aomencQualityMode = "";
        public static string allSettingsAom = "";
        //------------------------------------------------------||

        public MainWindow()
        {
            InitializeComponent();
            CheckFfprobe();
        }

        public void CheckFfprobe()
        {
            currentDir = Directory.GetCurrentDirectory();
            if (CheckBoxCustomFfprobePath.IsChecked == true)
            {
                exeffprobePath = TextBoxCustomFfprobePath.Text;
            }else if (CheckBoxCustomFfprobePath.IsChecked == false)
            {
                exeffprobePath = currentDir;
            }
        }

        private void RadioButtonBitrate_Checked(object sender, RoutedEventArgs e)
        {
            RadioButtonConstantQuality.IsChecked = false;
        }

        private void ButtonStartEncode_Click(object sender, RoutedEventArgs e)
        {
            //Main entry Point
            SetParametersBeforeEncode();            
            SetAomencParameters();
            AsyncClass();
        }

        public async void AsyncClass()
        {
            await Task.Run(() => SmallScripts.CreateDirectory(workingTempDirectory, "Chunks"));
            await Task.Run(() => SplitVideo.StartSplitting(videoInput, workingTempDirectory, chunkLengthSplit, reencodeBeforeMainEncode, exeffmpegPath));
            await Task.Run(() => RenameChunks.Rename(workingTempDirectory));
            await Task.Run(() => SmallScripts.CountVideoChunks());
            //await Task.Run(() => EncodeAomenc());
            //await Task.Run(() => ConcatVideo.Concat());
        }

        public void SetParametersBeforeEncode()
        {
            //Needed Parameters for Splitting --------------------------------------------------------||
            videoInput = TextBoxVideoInput.Text;
            //Sets the working directory
            if (CheckBoxCustomTempFolder.IsChecked == false && TextBoxCustomTempFolder.Text == "Temp Folder")
            {
                workingTempDirectory = System.IO.Path.Combine(currentDir, "Temp");
            }else if (CheckBoxCustomTempFolder.IsChecked == true && TextBoxCustomTempFolder.Text != "Temp Folder")
            {
                workingTempDirectory = System.IO.Path.Combine(TextBoxCustomTempFolder.Text, "Temp");
            }
            //Sets ffmpeg Path
            if (CheckBoxCustomFfmpegPath.IsChecked == false)
            {
                exeffmpegPath = currentDir;
            }else if (CheckBoxCustomFfmpegPath.IsChecked == true)
            {
                exeffmpegPath = TextBoxCustomFfmpegPath.Text;
            }
            chunkLengthSplit = Int16.Parse(TextBoxChunkLength.Text);
            reencodeBeforeMainEncode = CheckBoxReencode.IsChecked == true;
            //----------------------------------------------------------------------------------------||
            //Needed Parameters for Encoding ---------------------------------------------------------||
            streamFrameRate = TextBoxFramerate.Text;
            maxConcurrencyEncodes = Int16.Parse(TextBoxNumberOfWorkers.Text);
            //Sets the aomenc path
            if (CheckBoxCustomAomencPath.IsChecked == false)
            {
                aomenc = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "aomenc.exe");
            }
            else if (CheckBoxCustomAomencPath.IsChecked == true)
            {
                aomenc = System.IO.Path.Combine(exeaomencPath, "aomenc.exe");
            }
            //----------------------------------------------------------------------------------------||

        }

        public void SetAomencParameters()
        {
            //Sets 2-Pass Mode -----------------------------------------------------------------------||
            if (CheckBoxTwoPass.IsChecked == true)
            {
                numberOfPasses = 2;
            }
            //----------------------------------------------------------------------------------------||
            //Sets Quality Mode ----------------------------------------------------------------------||
            if (RadioButtonConstantQuality.IsChecked == true)
            {
                aomencQualityMode = " --end-usage=q --cq-level=" + SliderQuality.Value;
            }else if (RadioButtonBitrate.IsChecked == true)
            {
                if (CheckBoxCBR.IsChecked == true)
                {
                    aomencQualityMode = " --end-usage=cbr --target-bitrate=" + TextBoxBitrate.Text;
                }
                else
                {
                    aomencQualityMode = " --end-usage=vbr --target-bitrate=" + TextBoxBitrate.Text;
                }
            }
            //----------------------------------------------------------------------------------------||
            //Sets aomenc arguments ------------------------------------------------------------------||
            if (CheckBoxAdvancedSettings.IsChecked == false)
            {
                //Basic Settings
                allSettingsAom = " --cpu-used=" + SliderPreset.Value + " --bit-depth=" + ComboBoxBitDepth.Text + " --fps=" + TextBoxFramerate.Text + " --threads=2 --kf-max-dist=240 --tile-rows=1 --tile-columns=1" + aomencQualityMode;
            }else if (CheckBoxAdvancedSettings.IsChecked == true && CheckBoxCustomCommandLine.IsChecked == false)
            {
                string aqMode = "";
                if (ComboBoxAqMode.Text == "Off (Default)")
                {
                    aqMode = "0";
                }else if (ComboBoxAqMode.Text == "Variance")
                {
                    aqMode = "1";
                }else if (ComboBoxAqMode.Text == "Complexity")
                {
                    aqMode = "2";
                }else if (ComboBoxAqMode.Text == "Cyclic Refresh")
                {
                    aqMode = "3";
                }
                allSettingsAom = " --cpu-used=" + SliderPreset.Value + " --bit-depth=" + ComboBoxBitDepth.Text + " --fps=" + TextBoxFramerate.Text + " --threads=" + TextBoxThreads.Text + " --kf-max-dist=" + TextBoxKeyframeInterval.Text + " --tile-rows=" + TextBoxTileRows.Text + " --tile-columns=" + TextBoxTileColumns.Text + " --aq-mode=" + aqMode;
            }else if (CheckBoxAdvancedSettings.IsChecked == true && CheckBoxCustomCommandLine.IsChecked == true)
            {
                allSettingsAom = " "+TextBoxCustomCommand.Text;
            }
        }

        private void ButtonOpenSource_Click(object sender, RoutedEventArgs e)
        {
            //Open the OpenFileDialog to set the Videoinput
            OpenFileDialog openVideoFileDialog = new OpenFileDialog();

            Nullable<bool> result = openVideoFileDialog.ShowDialog();

            if (result == true)
            {
                TextBoxVideoInput.Text = openVideoFileDialog.FileName;
                GetStreamFps(TextBoxVideoInput.Text);
                SmallScripts.GetStreamLength(TextBoxVideoInput.Text);
            }
                
        }
        public void GetStreamFps(string fileinput)
        {
            //Sets the Streamframerate, so the user don't has to change it
            string input = '\u0022' + fileinput + '\u0022';
            Process getStreamFps = new Process();
            getStreamFps.StartInfo = new ProcessStartInfo()
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                WorkingDirectory = exeffprobePath,
                Arguments = "/C ffprobe.exe -i " + input + " -v 0 -of csv=p=0 -select_streams v:0 -show_entries stream=r_frame_rate",
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            getStreamFps.Start();
            string fpsOutput = getStreamFps.StandardOutput.ReadLine();
            TextBoxFramerate.Text = fpsOutput;
            string value = new DataTable().Compute(TextBoxFramerate.Text, null).ToString();
            streamFrameRateLabel = Convert.ToInt64(Math.Round(Convert.ToDouble(value))).ToString();
            getStreamFps.WaitForExit();
        }

        private void ButtonSaveEncodeTo_Click(object sender, RoutedEventArgs e)
        {
            //Open the OpenFileDialog to set the Videooutput
            SaveFileDialog saveVideoFileDialog = new SaveFileDialog();
            saveVideoFileDialog.Filter = "Matroska|*.mkv";

            Nullable<bool> result = saveVideoFileDialog.ShowDialog();

            if (result == true)
            {
                TextBoxVideoOutput.Text = saveVideoFileDialog.FileName;
                videoOutput = saveVideoFileDialog.FileName;
            }
        }

        private void ComboBoxEncoder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Sets the Maximum Quality Values, which are Encoder dependant
            string comboitem = (e.AddedItems[0] as ComboBoxItem).Content as string;

            if (comboitem == "aomenc")
            {
                if (SliderQuality != null)
                {
                    SliderQuality.Maximum = 61;
                    SliderQuality.Value = 30;
                }

            }else if (comboitem == "RAV1E")
            {
                SliderQuality.Maximum = 255;
                SliderQuality.Value = 100;
            }else if (comboitem == "SVT-AV1")
            {
                SliderQuality.Maximum = 63;
                SliderQuality.Value = 50;
            }
        }

        private void ButtonCustomTempFolder_Click(object sender, RoutedEventArgs e)
        {
            //Sets the Temp Folder
            System.Windows.Forms.FolderBrowserDialog browseTempFolder = new System.Windows.Forms.FolderBrowserDialog();

            if (browseTempFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TextBoxCustomTempFolder.Text = browseTempFolder.SelectedPath;
            }
        }

        private void ButtonCustomFfmpegPath_Click(object sender, RoutedEventArgs e)
        {
            //Sets the ffmpeg folder
            System.Windows.Forms.FolderBrowserDialog browseFfmpegFolder = new System.Windows.Forms.FolderBrowserDialog();

            if (browseFfmpegFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TextBoxCustomFfmpegPath.Text = browseFfmpegFolder.SelectedPath;

                bool FfmpegExist = File.Exists(TextBoxCustomFfmpegPath.Text + "\\ffmpeg.exe");

                if (FfmpegExist == false)
                {
                    MessageBox.Show("Couldn't find ffmpeg in that folder!", "Attention!", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

            }
        }

        private void ButtonCustomFfprobePath_Click(object sender, RoutedEventArgs e)
        {
            //Sets the ffprobe folder
            System.Windows.Forms.FolderBrowserDialog browseFfprobeFolder = new System.Windows.Forms.FolderBrowserDialog();

            if (browseFfprobeFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TextBoxCustomFfprobePath.Text = browseFfprobeFolder.SelectedPath;

                bool FfprobeExist = File.Exists(TextBoxCustomFfprobePath.Text + "\\ffprobe.exe");

                if (FfprobeExist == false)
                {
                    MessageBox.Show("Couldn't find ffprobe in that folder!", "Attention!", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void ButtonCustomAomencPath_Click(object sender, RoutedEventArgs e)
        {
            //Sets the aomenc folder
            System.Windows.Forms.FolderBrowserDialog browseAomencFolder = new System.Windows.Forms.FolderBrowserDialog();

            if (browseAomencFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TextBoxCustomAomencPath.Text = browseAomencFolder.SelectedPath;

                bool FfprobeExist = File.Exists(TextBoxCustomAomencPath.Text + "\\aomenc.exe");

                if (FfprobeExist == false)
                {
                    MessageBox.Show("Couldn't find aomenc in that folder!", "Attention!", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void EncodeAomenc()
        {
            MainProgressBar.Dispatcher.Invoke(() => MainProgressBar.Maximum = Int16.Parse(numberofvideoChunks), DispatcherPriority.Background);
            pLabel.Dispatcher.Invoke(() => pLabel.Content = "0 / " + MainProgressBar.Maximum, DispatcherPriority.Background);
            string labelstring = videoChunks.Count().ToString();
            //Sets the Time for later eta calculation
            DateTime starttime = DateTime.Now;
            using (SemaphoreSlim concurrencySemaphore = new SemaphoreSlim(maxConcurrencyEncodes))
            {
                List<Task> tasks = new List<Task>();
                foreach (var items in videoChunks)
                {
                    concurrencySemaphore.Wait();

                    var t = Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            if (SmallScripts.Cancel.CancelAll == false)
                            {
                                if (numberOfPasses == 1)
                                {
                                    Process process = new Process();
                                    ProcessStartInfo startInfo = new ProcessStartInfo();
                                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                    startInfo.UseShellExecute = true;
                                    startInfo.FileName = "cmd.exe";
                                    startInfo.WorkingDirectory = exeffmpegPath + "\\";
                                    startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + chunksDir + "\\" + items + '\u0022' + " -pix_fmt yuv420p -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + aomenc + '\u0022' + " - --passes=1" + allSettingsAom + " --output=" + '\u0022' + chunksDir + "\\" + items + "-av1.ivf" + '\u0022';
                                    process.StartInfo = startInfo;
                                    Console.WriteLine(startInfo.Arguments);
                                    process.Start();
                                    process.WaitForExit();

                                    //Progressbar +1
                                    MainProgressBar.Dispatcher.Invoke(() => MainProgressBar.Value += 1, DispatcherPriority.Background);
                                    //Label of Progressbar = Progressbar
                                    TimeSpan timespent = DateTime.Now - starttime;
   
                                    pLabel.Dispatcher.Invoke(() => pLabel.Content = MainProgressBar.Value + " / " + labelstring + " - " + Math.Round(Convert.ToDecimal(((((Int16.Parse(streamLength) * Int16.Parse(streamFrameRateLabel)) / Int16.Parse(labelstring)) * MainProgressBar.Value) / timespent.TotalSeconds)), 2).ToString() + "fps" + " - " + Math.Round((((timespent.TotalSeconds / MainProgressBar.Value) * (Int16.Parse(labelstring) - MainProgressBar.Value)) / 60), MidpointRounding.ToEven) + "min left", DispatcherPriority.Background);

                                    if (SmallScripts.Cancel.CancelAll == false)
                                    {
                                        //Write Item to file for later resume if something bad happens
                                        SmallScripts.WriteToFileThreadSafe(items, "Temp\\encoded.log");
                                    }
                                    else
                                    {
                                        SmallScripts.KillInstances();
                                    }
                                }
                                else if (numberOfPasses == 2)
                                {
                                    Process process = new Process();
                                    ProcessStartInfo startInfo = new ProcessStartInfo();

                                    bool FileExistFirstPass = File.Exists(chunksDir + "\\" + items + "_1pass_successfull.log");
                                    if (FileExistFirstPass != true)
                                    {

                                        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                        startInfo.FileName = "cmd.exe";
                                        startInfo.WorkingDirectory = exeffmpegPath + "\\";
                                        startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + chunksDir + "\\" + items + '\u0022' + " -pix_fmt yuv420p -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + aomenc + '\u0022' + " - --passes=2 --pass=1 --fpf=" + '\u0022' + chunksDir + "\\" + items + "_stats.log" + '\u0022' + allSettingsAom + " --output=NUL";
                                        process.StartInfo = startInfo;
                                        //Console.WriteLine(startInfo.Arguments);
                                        process.Start();
                                        process.WaitForExit();

                                        if (SmallScripts.Cancel.CancelAll == false)
                                        {
                                            //Write Item to file for later resume if something bad happens
                                            SmallScripts.WriteToFileThreadSafe("", chunksDir + "\\" + items + "_1pass_successfull.log");
                                        }
                                        else
                                        {
                                            SmallScripts.KillInstances();
                                        }
                                    }

                                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                    startInfo.FileName = "cmd.exe";
                                    startInfo.WorkingDirectory = exeffmpegPath + "\\";
                                    startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + chunksDir + "\\" + items + '\u0022' + " -pix_fmt yuv420p -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + aomenc + '\u0022' + " - --passes=2 --pass=2 --fpf=" + '\u0022' + chunksDir + "\\" + items + "_stats.log" + '\u0022' + allSettingsAom + " --output=" + '\u0022' + chunksDir + "\\" + items + "-av1.ivf" + '\u0022';
                                    process.StartInfo = startInfo;
                                    //Console.WriteLine(startInfo.Arguments);
                                    process.Start();
                                    process.WaitForExit();

                                    MainProgressBar.Dispatcher.Invoke(() => MainProgressBar.Value += 1, DispatcherPriority.Background);
                                    TimeSpan timespent = DateTime.Now - starttime;
                                    pLabel.Dispatcher.Invoke(() => pLabel.Content = MainProgressBar.Value + " / " + labelstring + " - " + Math.Round(Convert.ToDecimal(((((Int16.Parse(streamLength) * Int16.Parse(streamFrameRateLabel)) / Int16.Parse(labelstring)) * MainProgressBar.Value) / timespent.TotalSeconds)), 2).ToString() + "fps" + " - " + Math.Round((((timespent.TotalSeconds / MainProgressBar.Value) * (Int16.Parse(labelstring) - MainProgressBar.Value)) / 60), MidpointRounding.ToEven) + "min left", DispatcherPriority.Background);

                                    if (SmallScripts.Cancel.CancelAll == false)
                                    {
                                        //Write Item to file for later resume if something bad happens
                                        SmallScripts.WriteToFileThreadSafe(items, "Temp\\encoded.log");
                                    }
                                    else
                                    {
                                        SmallScripts.KillInstances();
                                    }
                                }
                            }
                        }
                        finally
                        {
                            concurrencySemaphore.Release();
                        }
                    });

                    tasks.Add(t);
                }

                Task.WaitAll(tasks.ToArray());
            }

        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            //SmallScripts.Cancel.CancelAll = true;
            //SmallScripts.KillInstances();
        }

        public void LoadSettings(string profileName, bool saveJob)
        {
            //Loads Settings from XML File -------------------------------------------------------------------------------------||
            XmlDocument doc = new XmlDocument();

            string directory = "";
            
            if (saveJob == true)
            {
                directory = currentDir + "\\unfinishedjob.xml";
            }
            else
            {
                directory = currentDir + "\\Profiles\\" + profileName;
            }

            doc.Load(directory);
            XmlNodeList node = doc.GetElementsByTagName("Settings");
            foreach (XmlNode n in node[0].ChildNodes)
            {
                if (n.Name == "ChunkLength"){ TextBoxChunkLength.Text = n.InnerText; }
                if (n.Name == "Reencode")
                {
                    if (n.InnerText == "True")
                    {
                        CheckBoxReencode.IsChecked = true;
                    }else if (n.InnerText == "False")
                    {
                        CheckBoxReencode.IsChecked = false;
                    }
                }
                if (n.Name == "Workers") { TextBoxNumberOfWorkers.Text = n.InnerText; }
                if (n.Name == "Encoder") 
                { 
                    if (n.InnerText == "aomenc") { ComboBoxEncoder.SelectedIndex = 0; }
                    if (n.InnerText == "RAV1E") { ComboBoxEncoder.SelectedIndex = 1; }
                    if (n.InnerText == "SVT-AV1") { ComboBoxEncoder.SelectedIndex = 2; }
                }
                if (n.Name == "BitDepth")
                {
                    if (n.InnerText == "8") { ComboBoxBitDepth.SelectedIndex = 0; }
                    if (n.InnerText == "10") { ComboBoxBitDepth.SelectedIndex = 1; }
                    if (n.InnerText == "12") { ComboBoxBitDepth.SelectedIndex = 2; }
                }
                if (n.Name == "Preset") { SliderPreset.Value = Int16.Parse(n.InnerText); }
                if (n.Name == "TwoPassEncoding") { if (n.InnerText == "True") { CheckBoxTwoPass.IsChecked = true; } else { CheckBoxTwoPass.IsChecked = false; } }
                if (n.Name == "QualityMode") { if (n.InnerText == "True") { RadioButtonConstantQuality.IsChecked = true; }else { RadioButtonConstantQuality.IsChecked = false; } }
                if (n.Name == "Quality") { SliderQuality.Value = Int16.Parse(n.InnerText); }
                if (n.Name == "BitrateMode") { if (n.InnerText == "True") { RadioButtonBitrate.IsChecked = true; } else { RadioButtonBitrate.IsChecked = false; } }
                if (n.Name == "Bitrate") { TextBoxBitrate.Text = n.InnerText; }
                if (n.Name == "CBRActive") { if (n.InnerText == "True") { CheckBoxCBR.IsChecked = true; }else { CheckBoxCBR.IsChecked = false; } }
                if (n.Name == "AdvancedSettingsActive") { if (n.InnerText == "True") { CheckBoxAdvancedSettings.IsChecked = true; } else { CheckBoxAdvancedSettings.IsChecked = false; } }
                if (n.Name == "AdvancedSettingsThreads") { TextBoxThreads.Text = n.InnerText; }
                if (n.Name == "AdvancedSettingsTileColumns") { TextBoxTileColumns.Text = n.InnerText; }
                if (n.Name == "AdvancedSettingsTileRows") { TextBoxTileRows.Text = n.InnerText; }
                if (n.Name == "AdvancedSettingsAQMode") { ComboBoxAqMode.Text = n.InnerText; }
                if (n.Name == "AdvancedSettingsKeyFrameInterval") { TextBoxKeyframeInterval.Text = n.InnerText; }
                if (n.Name == "AdvancedSettingsCustomCommandActive") { if (n.InnerText == "True") { CheckBoxCustomCommandLine.IsChecked = true; } else { CheckBoxCustomCommandLine.IsChecked = false; } }
                if (n.Name == "AdvancedSettingsCustomCommand") { TextBoxCustomCommand.Text = n.InnerText; }
                if (n.Name == "ShutdownAfterEncode") { if (n.InnerText == "True") { CheckBoxShutdownAfterEncode.IsChecked = true; } else { CheckBoxShutdownAfterEncode.IsChecked = false; } }
                if (n.Name == "DeleteTempFiles") { if (n.InnerText == "True") { CheckBoxDeleteTempFiles.IsChecked = true; } else { CheckBoxDeleteTempFiles.IsChecked = false; } }
                if (n.Name == "CustomFfmpegPathActive") { if (n.InnerText == "True") { CheckBoxCustomFfmpegPath.IsChecked = true; } else { CheckBoxCustomFfmpegPath.IsChecked = false; } }
                if (n.Name == "CustomFfmpegPath") { TextBoxCustomFfmpegPath.Text = n.InnerText; }
                if (n.Name == "CustomFfprobePathActive") { if (n.InnerText == "True") { CheckBoxCustomFfprobePath.IsChecked = true; } else { CheckBoxCustomFfprobePath.IsChecked = false; } }
                if (n.Name == "CustomFfprobePath") { TextBoxCustomFfprobePath.Text = n.InnerText; }
                if (n.Name == "CustomAomencPathActive") { if (n.InnerText == "True") { CheckBoxCustomAomencPath.IsChecked = true; } else { CheckBoxCustomAomencPath.IsChecked = false; } }
                if (n.Name == "CustomAomencPath") { TextBoxCustomAomencPath.Text = n.InnerText; }
                if (n.Name == "CustomTempPathActive") { if (n.InnerText == "True") { CheckBoxCustomTempFolder.IsChecked = true; } else { CheckBoxCustomTempFolder.IsChecked = false; } }
                if (n.Name == "CustomAomencPath") { TextBoxCustomTempFolder.Text = n.InnerText; }

                if (saveJob == true)
                {
                    if (n.Name == "VideoInput") { TextBoxVideoInput.Text = n.InnerText; }
                    if (n.Name == "VideoOutput") { TextBoxVideoOutput.Text = n.InnerText; }
                }
                //------------------------------------------------------------------------------------------------------------------||
            }
        }

        public void SaveSettings(string profileName, bool saveJob)
        {
            string directory = "";
            //Saves Settings to XML File ---------------------------------------------------------------------------------------||
            if (saveJob == true)
            {
                directory = currentDir + "\\unfinishedjob.xml";
            }
            else
            {
                directory = currentDir + "\\Profiles\\" + profileName;
            }

            XmlWriter writer = XmlWriter.Create(directory);

            writer.WriteStartElement("Settings");
            writer.WriteElementString("ChunkLength", TextBoxChunkLength.Text);
            writer.WriteElementString("Reencode", CheckBoxReencode.IsChecked.ToString());
            writer.WriteElementString("Workers", TextBoxNumberOfWorkers.Text);
            writer.WriteElementString("Encoder", ComboBoxEncoder.Text);
            writer.WriteElementString("BitDepth", ComboBoxBitDepth.Text);
            writer.WriteElementString("Preset", SliderPreset.Value.ToString());
            writer.WriteElementString("TwoPassEncoding", CheckBoxTwoPass.IsChecked.ToString());
            writer.WriteElementString("QualityMode", RadioButtonConstantQuality.IsChecked.ToString());
            writer.WriteElementString("Quality", SliderQuality.Value.ToString());
            writer.WriteElementString("BitrateMode", RadioButtonBitrate.IsChecked.ToString());
            writer.WriteElementString("Bitrate", TextBoxBitrate.Text);
            writer.WriteElementString("CBRActive", CheckBoxCBR.IsChecked.ToString());
            writer.WriteElementString("AdvancedSettingsActive", CheckBoxAdvancedSettings.IsChecked.ToString());
            writer.WriteElementString("AdvancedSettingsThreads", TextBoxThreads.Text);
            writer.WriteElementString("AdvancedSettingsTileColumns", TextBoxTileColumns.Text);
            writer.WriteElementString("AdvancedSettingsTileRows", TextBoxTileRows.Text);
            writer.WriteElementString("AdvancedSettingsAQMode", ComboBoxAqMode.Text);
            writer.WriteElementString("AdvancedSettingsKeyFrameInterval", TextBoxKeyframeInterval.Text);
            writer.WriteElementString("AdvancedSettingsCustomCommandActive", CheckBoxCustomCommandLine.IsChecked.ToString());
            writer.WriteElementString("AdvancedSettingsCustomCommand", TextBoxCustomCommand.Text);
            writer.WriteElementString("ShutdownAfterEncode", CheckBoxShutdownAfterEncode.IsChecked.ToString());
            writer.WriteElementString("DeleteTempFiles", CheckBoxDeleteTempFiles.IsChecked.ToString());
            writer.WriteElementString("CustomFfmpegPathActive", CheckBoxCustomFfmpegPath.IsChecked.ToString());
            writer.WriteElementString("CustomFfmpegPath", TextBoxCustomFfmpegPath.Text);
            writer.WriteElementString("CustomFfprobePathActive", CheckBoxCustomFfprobePath.IsChecked.ToString());
            writer.WriteElementString("CustomFfprobePath", TextBoxCustomFfprobePath.Text);
            writer.WriteElementString("CustomAomencPathActive", CheckBoxCustomAomencPath.IsChecked.ToString());
            writer.WriteElementString("CustomAomencPath", TextBoxCustomAomencPath.Text);
            writer.WriteElementString("CustomTempPathActive", CheckBoxCustomTempFolder.IsChecked.ToString());
            writer.WriteElementString("CustomTempPath", TextBoxCustomTempFolder.Text);
            if (saveJob == true)
            {
                writer.WriteElementString("VideoInput", TextBoxVideoInput.Text);
                writer.WriteElementString("VideoOutput", TextBoxVideoOutput.Text);
            }
            writer.WriteEndElement();
            writer.Close();
            //------------------------------------------------------------------------------------------------------------------||
        }

        private void ButtonSaveProfile_Click(object sender, RoutedEventArgs e)
        {
            SmallScripts.CreateDirectory(currentDir, "Profiles");
            SaveSettings(TextBoxProfiles.Text, false);
        }

        private void ButtonProfilesRefresh_Click(object sender, RoutedEventArgs e)
        {
            DirectoryInfo profiles = new DirectoryInfo(currentDir + "\\Profiles");
            FileInfo[] Files = profiles.GetFiles("*.xml"); //Getting XML
            ComboBoxProfiles.ItemsSource = Files;
        }

        private void ButtonLoadProfile_Click(object sender, RoutedEventArgs e)
        {
            LoadSettings(ComboBoxProfiles.Text, false);
        }
    }
}
