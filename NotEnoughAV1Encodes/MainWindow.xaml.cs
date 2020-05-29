using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
        public static bool preencodeBeforeMainEncode = false;
        public static string reencodecodec = "utvideo";
        public static string prereencodecodec = "utvideo";
        public static bool resumeMode = false;
        public static bool inputSet = false;
        public static bool outputSet = false;
        public static string processPriority = "Normal";
        public static bool logging = true; //default so problems before it would be activated are written
        //------------------------------------------------------||
        //----- Color Settings ---------------------------------||
        public static string yuvcolorspace = " yuv420p";
        public static string colorprimaries = "bt709";
        public static string colortransfer = "bt709";
        public static string colormatrix = "bt709";
        //------------------------------------------------------||
        //----- aomenc Settings --------------------------------||
        public static int numberOfPasses = 1;
        public static string aomenc = "";
        public static string aomencQualityMode = "";
        public static string allSettingsAom = "";
        public static bool aomEncode = false;
        public static string aomColormatrix = "bt709";
        public static string aomColortransfer = "bt709";
        public static string aomChromaSubsample = "--i420";
        public static string keyframefilt = "1";
        public static string autoaltref = "0";
        public static string frameboost = "0";
        //------------------------------------------------------||
        //----- RAV1E Settings ---------------------------------||
        public static string ravie = "";
        public static string ravieQualityMode = "";
        public static string allSettingsRavie = "";
        public static string pipeBitDepth = " yuv420p";
        public static bool rav1eEncode = false;
        public static string rav1eColormatrix = "BT709";
        public static string rav1eColortransfer = "BT709";
        //------------------------------------------------------||
        //----- SVT-AV1 Settings -------------------------------||
        public static string svtav1 = "";
        public static string svtav1QualityMode = "";
        public static string allSettingsSvtav1 = "";
        public static string allSettingsSvtav1SecondPass = "";
        //------------------------------------------------------||
        //----- libaom Settings --------------------------------||
        public static string libaom = "";
        public static string libaomQualityMode = "";
        public static string allSettingslibaom = "";
        public static bool libaomEncode = false;
        //------------------------------------------------------||
        //----- Custom Background ------------------------------||
        public static string PathToBackground = "";
        public static bool customBackground = false;
        //------------------------------------------------------||
        //----- Audio Settings ---------------------------------||
        public static bool audioEncoding = false;
        public static string audioCodec = "";
        public static string audioCodecTrackTwo = "";
        public static string audioCodecTrackThree = "";
        public static string audioCodecTrackFour = "";
        public static int audioBitrate = 0;
        public static int audioBitrateTrackTwo = 0;
        public static int audioBitrateTrackThree = 0;
        public static int audioBitrateTrackFour = 0;
        public static bool trackOne = false;
        public static bool trackTwo = false;
        public static bool trackThree = false;
        public static bool trackFour = false;
        public static string firstTrackIndex = "1";
        public static string secondTrackIndex = "2";
        public static string thirdTrackIndex = "3";
        public static string fourthTrackIndex = "4";
        public static bool detectedTrackOne = false;
        public static bool detectedTrackTwo = false;
        public static bool detectedTrackThree = false;
        public static bool detectedTrackFour = false;
        public static int audioChannelsTrackOne = 2;
        public static int audioChannelsTrackTwo = 2;
        public static int audioChannelsTrackThree = 2;
        public static int audioChannelsTrackFour = 2;
        //------------------------------------------------------||
        //----- Resizing ---------------------------------------||
        public static string videoResize = "";
        //------------------------------------------------------||
        //----- Subtitles --------------------------------------||
        public static bool subtitleStreamCopy = false;
        public static bool subtitleCustom = false;
        public static bool subtitles = false;
        public static int customsubtitleadded = 0;
        public static string[] SubtitleChunks;
        public static int subtitleAmount = 0;
        //----- Shutdown ---------------------------------------||
        public static bool shutDownAfterEncode = false;
        //------------------------------------------------------||
        //----- Batch Encoding ---------------------------------||
        public static string batchVideoInput = "";
        public static string batchVideoOutput = "";
        public static bool deleteTempAfterEncode = false;
        //------------------------------------------------------||
        public DateTime starttimea;

        public MainWindow()
        {
            InitializeComponent();
            SmallScripts.Logging("-----------------------------------------------");
            SmallScripts.Logging(LabelProgramVersion.Content.ToString());
            SmallScripts.Logging("-----------------------------------------------");
            CheckFfprobe();
            LoadProfiles();
            LoadProfileStartup();
            CheckForResumeFile();
        }

        public async Task AsyncClass()
        {
            SmallScripts.Logging("Landed in AsyncClass() function.");
            if (CheckBoxAudioEncoding.IsChecked == true && resumeMode == false)
            {
                SmallScripts.Logging("AsyncClass : Started Audio Encoding");
                pLabel.Dispatcher.Invoke(() => pLabel.Content = "Started Audio Encoding ...", DispatcherPriority.Background);
                await Task.Run(() => EncodeAudio.AudioEncode());
                SmallScripts.CheckAudioEncode();
                SmallScripts.Logging("AsyncClass : Finished Audio Encoding");
            }
            if (CheckBoxEnableSubtitles.IsChecked == true && resumeMode == false && subtitles == true)
            {
                SmallScripts.Logging("AsyncClass : Started Subtitle Copying");
                pLabel.Dispatcher.Invoke(() => pLabel.Content = "Started Subtitle Copying ...", DispatcherPriority.Background);
                await Task.Run(() => Subtitle.EncSubtitles());
                SmallScripts.CheckSubtitleEncode();
                SmallScripts.Logging("AsyncClass : Finished Subtitle Copying");
            }
            if (resumeMode == false)
            {
                SaveSettings("", true, false, "");
                SmallScripts.Logging("AsyncClass : Creating Temo Working Directory");
                await Task.Run(() => SmallScripts.CreateDirectory(workingTempDirectory, "Chunks"));
                SmallScripts.Logging("AsyncClass : Started Splitting");
                pLabel.Dispatcher.Invoke(() => pLabel.Content = "Started Splitting ...", DispatcherPriority.Background);
                await Task.Run(() => SplitVideo.StartSplitting(videoInput, workingTempDirectory, chunkLengthSplit, preencodeBeforeMainEncode, reencodeBeforeMainEncode, reencodecodec, prereencodecodec));
                SmallScripts.Logging("AsyncClass : Finished Splitting");
                SmallScripts.Logging("AsyncClass : Started Renaming Chunks");
                pLabel.Dispatcher.Invoke(() => pLabel.Content = "Renaming Chunks ...", DispatcherPriority.Background);
                await Task.Run(() => RenameChunks.Rename(workingTempDirectory));
                SmallScripts.Logging("AsyncClass : Finished Renaming Chunks");
            }
            SmallScripts.Logging("AsyncClass : Counting Video Chunks.");
            await Task.Run(() => SmallScripts.CountVideoChunks());
            if (SmallScripts.Cancel.CancelAll == false)
            {
                if (ComboBoxEncoder.Text == "aomenc" || ComboBoxEncoder.Text == "libaom")
                {
                    SmallScripts.Logging("AsyncClass : Invoking Encoding aomenc...");
                    pLabel.Dispatcher.Invoke(() => pLabel.Content = "Encoding Started aomenc...", DispatcherPriority.Background);
                    await Task.Run(() => EncodeAomencOrRav1e());
                }
                else if (ComboBoxEncoder.Text == "RAV1E")
                {
                    SmallScripts.Logging("AsyncClass : Invoking Encoding RAV1E...");
                    pLabel.Dispatcher.Invoke(() => pLabel.Content = "Encoding Started RAV1E...", DispatcherPriority.Background);
                    await Task.Run(() => EncodeAomencOrRav1e());
                }
                else if (ComboBoxEncoder.Text == "SVT-AV1")
                {
                    SmallScripts.Logging("AsyncClass : Invoking Encoding SVT-AV1...");
                    pLabel.Dispatcher.Invoke(() => pLabel.Content = "Encoding Started SVT-AV1...", DispatcherPriority.Background);
                    await Task.Run(() => EncodeSVTAV1());
                }
            }
            else
            {
                pLabel.Dispatcher.Invoke(() => pLabel.Content = "Canceled!", DispatcherPriority.Background);
            }

            if (SmallScripts.Cancel.CancelAll == false)
            {
                SmallScripts.Logging("AsyncClass : Muxing Started");
                pLabel.Dispatcher.Invoke(() => pLabel.Content = "Muxing Started...", DispatcherPriority.Background);
                await Task.Run(() => ConcatVideo.Concat());
                pLabel.Dispatcher.Invoke(() => pLabel.Content = "Muxing completed! Elapsed Time: " + (DateTime.Now - starttimea).ToString("hh\\:mm\\:ss") + " - " + Math.Round(Convert.ToDecimal((((Int16.Parse(streamLength) * Int16.Parse(streamFrameRateLabel)) / (DateTime.Now - starttimea).TotalSeconds))), 2).ToString() + "fps", DispatcherPriority.Background);
                SmallScripts.Logging("AsyncClass : Muxing Finished! Elapsed Time: " + (DateTime.Now - starttimea).ToString("hh\\:mm\\:ss") + " - " + Math.Round(Convert.ToDecimal((((Int16.Parse(streamLength) * Int16.Parse(streamFrameRateLabel)) / (DateTime.Now - starttimea).TotalSeconds))), 2).ToString() + "fps");
                if (File.Exists("unfinishedjob.xml"))
                {
                    SmallScripts.Logging("AsyncClass : Deleting unfinishedjob.xml");
                    File.Delete("unfinishedjob.xml");
                }
                if (CheckBoxDeleteTempFiles.IsChecked == true || deleteTempAfterEncode == true)
                {
                    if (SmallScripts.Cancel.CancelAll == false)
                    {
                        SmallScripts.Logging("AsyncClass : Deleting Temp Files");
                        SmallScripts.DeleteTempFiles();
                    }
                    
                    if (CheckBoxCustomTempFolder.IsChecked == true && SmallScripts.Cancel.CancelAll == false)
                    {
                        SmallScripts.Logging("AsyncClass : Deleting Temp Files in Custom Folder");
                        SmallScripts.DeleteTempFilesDir(workingTempDirectory);
                    }
                }
                if (CheckBoxEnableFinishedSound.IsChecked == true && CheckBoxQueueMode.IsChecked == false)
                {
                    //Plays finished sound
                    SoundPlayer playSound = new SoundPlayer(Properties.Resources.finished);
                    playSound.Play();
                }
                if (shutDownAfterEncode == true && CheckBoxQueueMode.IsChecked == false)
                {
                    if (SmallScripts.Cancel.CancelAll == false)
                    {
                        Process.Start("shutdown.exe", "/s /t 0");
                    }
                }
            }
            else
            {
                pLabel.Dispatcher.Invoke(() => pLabel.Content = "Canceled!", DispatcherPriority.Background);
            }
        }

        public async void BatchEncode()
        {
            SmallScripts.Logging("Landed in BatchEncode() function.");
            DirectoryInfo batchfiles = new DirectoryInfo(TextBoxVideoInput.Text);
            foreach (var file in batchfiles.GetFiles())
            {
                SmallScripts.Cancel.CancelAll = false;
                ResetProgressBar();
                SetParametersBeforeEncode();
                SetAudioParameters();

                SmallScripts.Logging("BatchEncode Item : " + file.ToString());

                videoInput = TextBoxVideoInput.Text + "\\" + file;
                videoOutput = TextBoxVideoOutput.Text + "\\" + file + "-av1.mkv";
                deleteTempAfterEncode = true;

                GetStreamFps(videoInput);
                CheckAudioTracks(videoInput);
                SmallScripts.GetStreamLength(videoInput);

                if (CheckBoxAutomaticChunkLength.IsChecked == true)
                {
                    TextBoxChunkLength.Text = (Int16.Parse(streamLength) / Int16.Parse(TextBoxNumberOfWorkers.Text)).ToString();
                }

                if (SmallScripts.Cancel.CancelAll == false)
                {
                    if (ComboBoxEncoder.Text == "aomenc")
                    {
                        SetAomencParameters();
                    }
                    else if (ComboBoxEncoder.Text == "RAV1E")
                    {
                        SetRavieParameters();
                    }
                    else if (ComboBoxEncoder.Text == "SVT-AV1")
                    {
                        SetSVTAV1Parameters();
                    }
                    else if (ComboBoxEncoder.Text == "libaom")
                    {
                        SetLibAomParameters();
                    }
                    if (SmallScripts.Cancel.CancelAll == false)
                    {
                        await AsyncClass();
                    }
                }
            }
            SmallScripts.Logging("BatchEncode finished!");
            //Plays finished sound
            if (CheckBoxEnableFinishedSound.IsChecked == true)
            {
                SoundPlayer playSound = new SoundPlayer(Properties.Resources.finished);
                playSound.Play();
            }
        }

        public async void QueueEncode()
        {
            SmallScripts.Logging("Landed in QueueEncode() function.");
            foreach (var item in ListBoxQueue.Items)
            {
                LoadSettings("", false, true, item.ToString());
                SmallScripts.Cancel.CancelAll = false;
                SmallScripts.Logging("QueueEncode Item : " + item.ToString());
                ResetProgressBar();
                SetParametersBeforeEncode();
                SetAudioParameters();

                deleteTempAfterEncode = true;

                GetStreamFps(TextBoxVideoInput.Text);
                CheckAudioTracks(TextBoxVideoInput.Text);
                SmallScripts.GetStreamLength(TextBoxVideoInput.Text);

                videoInput = TextBoxVideoInput.Text;
                videoOutput = TextBoxVideoOutput.Text;

                if (CheckBoxAutomaticChunkLength.IsChecked == true)
                {
                    TextBoxChunkLength.Text = (Int16.Parse(streamLength) / Int16.Parse(TextBoxNumberOfWorkers.Text)).ToString();
                }
                if (ComboBoxEncoder.Text == "aomenc")
                {
                    SetAomencParameters();
                }
                else if (ComboBoxEncoder.Text == "RAV1E")
                {
                    SetRavieParameters();
                }
                else if (ComboBoxEncoder.Text == "SVT-AV1")
                {
                    SetSVTAV1Parameters();
                }
                else if (ComboBoxEncoder.Text == "libaom")
                {
                    SetLibAomParameters();
                }
                if (SmallScripts.Cancel.CancelAll == false)
                {
                    await AsyncClass();
                }
            }
            SmallScripts.Logging("QueueEncode finished!");
            if (CheckBoxEnableFinishedSound.IsChecked == true)
            {
                SoundPlayer playSound = new SoundPlayer(Properties.Resources.finished);
                playSound.Play();
            } 
        }

        //-------------------------------------- Small Functions ------------------------------------------||

        private void CheckResume()
        {
            SmallScripts.Logging("Landed in CheckResume() function.");
            if (CheckBoxResumeMode.IsChecked == true)
            {
                resumeMode = true;
                inputSet = true;
                outputSet = true;
                bool encodedExist = File.Exists("encoded.log");
                bool splittedExist = File.Exists("splitted.log");
                SmallScripts.Logging("encoded.log found? : " + encodedExist.ToString());
                SmallScripts.Logging("splitted.log found? : " + splittedExist.ToString());
                if (encodedExist && splittedExist)
                {
                    SmallScripts.Logging("Found encoded.log and splitted.log... Resuming...");
                    pLabel.Dispatcher.Invoke(() => pLabel.Content = "Resuming...", DispatcherPriority.Background);
                    GetStreamFps(TextBoxVideoInput.Text);
                    CheckAudioTracks(TextBoxVideoInput.Text);
                    SmallScripts.GetStreamLength(TextBoxVideoInput.Text);
                    videoOutput = TextBoxVideoOutput.Text;
                }
                else if (encodedExist == false && splittedExist == true)
                {
                    if (MessageBox.Show("It appears that you toggled the resume mode, but there are no encoded chunks. Press Yes, to start Encoding of all Chunks. Press No, if you want to cancel!", "Resume", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        SmallScripts.Logging("Found splitted.log Missing encoded.log - Toggled the resume mode, but there are no encoded chunks?");
                        pLabel.Dispatcher.Invoke(() => pLabel.Content = "Restarting...", DispatcherPriority.Background);
                        GetStreamFps(TextBoxVideoInput.Text);
                        CheckAudioTracks(TextBoxVideoInput.Text);
                        SmallScripts.GetStreamLength(TextBoxVideoInput.Text);
                        videoOutput = TextBoxVideoOutput.Text;
                        //This will be set if you Press Encode after a already finished encode
                    }
                    else
                    {
                        SmallScripts.Logging("Cancelling!");
                        SmallScripts.Cancel.CancelAll = true;
                    }
                }
                else if (encodedExist == false && splittedExist == false)
                {
                    if (MessageBox.Show("It appears that you toggled the resume mode, but there are no encoded chunks and no information about a successfull split. Press Yes, to start Encoding of all Chunks. Press No, if you want to cancel!", "Resume", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        SmallScripts.Logging("Missing splitted.log Missing encoded.log - Toggled the resume mode, but there are no encoded chunks and no information about a successfull split.");
                        pLabel.Dispatcher.Invoke(() => pLabel.Content = "Restarting...", DispatcherPriority.Background);
                        GetStreamFps(TextBoxVideoInput.Text);
                        CheckAudioTracks(TextBoxVideoInput.Text);
                        SmallScripts.GetStreamLength(TextBoxVideoInput.Text);
                        videoOutput = TextBoxVideoOutput.Text;
                        //This will be set if you Press Encode after a already finished encode
                    }
                    else
                    {
                        SmallScripts.Logging("Cancelling!");
                        SmallScripts.Cancel.CancelAll = true;
                    }
                }
            }
            else if (CheckBoxResumeMode.IsChecked == false)
            {
                resumeMode = false;
            }
        }

        private void CheckForResumeFile()
        {
            SmallScripts.Logging("Landed in CheckForResumeFile() function.");
            bool jobfileExist = File.Exists("unfinishedjob.xml");
            SmallScripts.Logging("unfinishedjob.xml found? : " + jobfileExist.ToString());
            if (jobfileExist)
            {
                if (MessageBox.Show("Unfinished Job detected! Load unfinished Job?",
                        "Resume", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    SmallScripts.Logging("Loading unfinished Job.");
                    LoadSettings("", true, false, "");
                    CheckBoxResumeMode.IsChecked = true;
                }
            }

            if (Directory.Exists("Queue") == true)
            {
                try
                {
                    DirectoryInfo queueFiles = new DirectoryInfo("Queue");
                    foreach (var file in queueFiles.GetFiles())
                    {
                        ListBoxQueue.Items.Add(file);
                        SmallScripts.Logging("Found Queue file: " + file.ToString());
                    }
                }
                catch { }
            }
        }

        private void ResetProgressBar()
        {
            SmallScripts.Logging("Landed in ResetProgressBar() function.");
            if (MainProgressBar.Value != 0)
            {
                MainProgressBar.Value = 0;
                MainProgressBar.Maximum = 100;
                pLabel.Dispatcher.Invoke(() => pLabel.Content = "Starting ...", DispatcherPriority.Background);
            }
        }

        public void GetStreamFps(string fileinput)
        {
            SmallScripts.Logging("Landed in GetStreamFps() function");
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
            SmallScripts.Logging("streamFrameRate: " + streamFrameRateLabel);
        }

        public void CheckFfprobe()
        {
            SmallScripts.Logging("Landed in CheckFfprobe() function.");
            currentDir = Directory.GetCurrentDirectory();
            if (CheckBoxCustomFfprobePath.IsChecked == true)
            {
                exeffprobePath = TextBoxCustomFfprobePath.Text;
            }
            else if (CheckBoxCustomFfprobePath.IsChecked == false)
            {
                exeffprobePath = currentDir;
            }
        }

        public void CheckDependencies()
        {
            SmallScripts.Logging("Landed in CheckDependencies() function");
            bool aomencExist = false;
            bool ffmpegExist = false;
            bool ffprobeExist = false;
            bool ravieExist = false;
            bool svtav1Exist = false;

            if (ComboBoxEncoder.Text == "aomenc")
            {
                if (CheckBoxCustomAomencPath.IsChecked == false) { aomencExist = File.Exists("aomenc.exe"); } 
                else if (CheckBoxCustomAomencPath.IsChecked == true) { aomencExist = File.Exists(TextBoxCustomAomencPath.Text + "\\aomenc.exe"); }

            } else if (ComboBoxEncoder.Text == "RAV1E")
            {
                if (CheckBoxCustomRaviePath.IsChecked == false) { ravieExist = File.Exists("rav1e.exe"); }
                else if (CheckBoxCustomRaviePath.IsChecked == true) { ravieExist = File.Exists(TextBoxCustomRaviePath.Text + "\\rav1e.exe"); }
            } else if (ComboBoxEncoder.Text == "SVT-AV1")
            {
                if (CheckBoxCustomSVTPath.IsChecked == false) { svtav1Exist = File.Exists("SvtAv1EncApp.exe"); }
                else if (CheckBoxCustomSVTPath.IsChecked == true) { svtav1Exist = File.Exists(TextBoxCustomRaviePath.Text + "\\SvtAv1EncApp.exe"); }
            }

            if (CheckBoxCustomFfmpegPath.IsChecked == false) { ffmpegExist = File.Exists("ffmpeg.exe"); } 
            else if (CheckBoxCustomFfmpegPath.IsChecked == true) { ffmpegExist = File.Exists(TextBoxCustomFfmpegPath.Text + "\\ffmpeg.exe"); }

            if (CheckBoxCustomFfprobePath.IsChecked == false) { ffprobeExist = File.Exists("ffprobe.exe"); }
            else if (CheckBoxCustomFfprobePath.IsChecked == true) { ffprobeExist = File.Exists(TextBoxCustomFfmpegPath.Text + "\\ffprobe.exe"); }

            if (ComboBoxEncoder.Text == "aomenc" && inputSet == true)
            {
                if (aomencExist == false || ffmpegExist == false || ffprobeExist == false)
                {
                    MessageBox.Show("Couldn't find all depedencies: \n aomenc found: " + aomencExist + "\n ffmpeg found: " + ffmpegExist + " \n ffprobe found: " + ffprobeExist);
                    SmallScripts.Cancel.CancelAll = true;
                }
            } else if (ComboBoxEncoder.Text == "RAV1E")
            {
                if (ravieExist == false || ffmpegExist == false || ffprobeExist == false)
                {
                    MessageBox.Show("Couldn't find all depedencies: \n rav1e found: " + ravieExist + "\n ffmpeg found: " + ffmpegExist + " \n ffprobe found: " + ffprobeExist);
                    SmallScripts.Cancel.CancelAll = true;
                }
            }else if (ComboBoxEncoder.Text == "SVT-AV1")
            {
                if (svtav1Exist == false || ffmpegExist == false || ffprobeExist == false)
                {
                    MessageBox.Show("Couldn't find all depedencies: \n SVT-AV1 found: " + svtav1Exist + "\n ffmpeg found: " + ffmpegExist + " \n ffprobe found: " + ffprobeExist);
                    SmallScripts.Cancel.CancelAll = true;
                }
            }else if (ComboBoxEncoder.Text == "libaom")
            {
                if (ffmpegExist == false || ffprobeExist == false)
                {
                    MessageBox.Show("Couldn't find all depedencies: \n ffmpeg found: " + ffmpegExist + " \n ffprobe found: " + ffprobeExist);
                    SmallScripts.Cancel.CancelAll = true;
                }
            }
            SmallScripts.Logging("aomencExist: " + aomencExist.ToString());
            SmallScripts.Logging("ffmpegExist: " + ffmpegExist.ToString());
            SmallScripts.Logging("ffprobeExist: " + ffprobeExist.ToString());
            SmallScripts.Logging("rav1eExist: " + ravieExist.ToString());
            SmallScripts.Logging("svtav1Exist: " + svtav1Exist.ToString());
        }

        public void CheckAudioTracks(string fileinput)
        {
            
            CheckSubtitleTracks(fileinput);
            SmallScripts.Logging("Landed in CheckAudioTracks() function");
            //Gets the AudioIndexes of the Input Video, because people may use bad videofiles with wrong indexes
            string input = '\u0022' + fileinput + '\u0022';
            Process getAudioIndexes = new Process();
            getAudioIndexes.StartInfo = new ProcessStartInfo()
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                WorkingDirectory = exeffprobePath,
                Arguments = "/C ffprobe.exe -i " + input + " -loglevel error -select_streams a -show_streams -show_entries stream=index:tags=:disposition= -of csv",
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            getAudioIndexes.Start();

            //Reads the Console Output
            string audioIndexes = getAudioIndexes.StandardOutput.ReadToEnd();

            //Splits the Console Output
            string[] audioIndexesFixed = audioIndexes.Split(new string[] { " ", "stream," }, StringSplitOptions.RemoveEmptyEntries);

            List<string> audioIndexList = new List<string>();

            int detectedTracks = 0;
            foreach (var item in audioIndexesFixed)
            {
                //Console.WriteLine(item.Trim());
                audioIndexList.Add(item.Trim());

                if (detectedTracks == 0)
                {
                    firstTrackIndex = item.Trim();
                    detectedTrackOne = true;
                }
                if (detectedTracks == 1)
                {
                    secondTrackIndex = item.Trim();
                    detectedTrackTwo = true;
                }
                if (detectedTracks == 2)
                {
                    thirdTrackIndex = item.Trim();
                    detectedTrackThree = true;
                }
                if (detectedTracks == 3)
                {
                    fourthTrackIndex = item.Trim();
                    detectedTrackFour = true;
                }

                detectedTracks += 1;
            }

            getAudioIndexes.WaitForExit();

            //Sets the Checkboxes
            if (detectedTrackOne == false)
            {
                CheckBoxAudioTrackOne.IsEnabled = false;
                CheckBoxAudioTrackOne.IsChecked = false;
            }else
            {
                SmallScripts.Logging("detectedTrackOne");
                CheckBoxAudioTrackOne.IsEnabled = true;
            }

            if (detectedTrackTwo == false)
            {
                CheckBoxAudioTrackTwo.IsEnabled = false;
                CheckBoxAudioTrackTwo.IsChecked = false;
            }
            else
            {
                SmallScripts.Logging("detectedTrackTwo");
                CheckBoxAudioTrackTwo.IsEnabled = true;
            }

            if (detectedTrackThree == false)
            {
                CheckBoxAudioTrackThree.IsEnabled = false;
                CheckBoxAudioTrackThree.IsChecked = false;
            }
            else
            {
                SmallScripts.Logging("detectedTrackThree");
                CheckBoxAudioTrackThree.IsEnabled = true;
            }

            if (detectedTrackFour == false)
            {
                CheckBoxAudioTrackFour.IsEnabled = false;
                CheckBoxAudioTrackFour.IsChecked = false;
            }
            else
            {
                SmallScripts.Logging("detectedTrackFour");
                CheckBoxAudioTrackFour.IsEnabled = true;
            }

            if (detectedTrackFour == false && detectedTrackThree == false && detectedTrackTwo == false && detectedTrackOne == false)
            {
                SmallScripts.Logging("No Audio Detected!");
                CheckBoxAudioEncoding.IsChecked = false;
                CheckBoxAudioEncoding.IsEnabled = false;
                CheckboxAudiofound.IsChecked = false;
            }else
            {
                CheckBoxAudioEncoding.IsEnabled = true;
                CheckboxAudiofound.IsChecked = true;
            }

        }

        public void CheckSubtitleTracks(string fileinput)
        {
            SmallScripts.Logging("Landed in CheckSubtitleTracks() function");
            //Gets the SubtitleIndexes of the Input Video, because people may enable subtitles, even they don't exist
            string input = '\u0022' + fileinput + '\u0022';
            Process getSubtitleIndexes = new Process();
            getSubtitleIndexes.StartInfo = new ProcessStartInfo()
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                WorkingDirectory = exeffprobePath,
                Arguments = "/C ffprobe.exe -i " + input + " -loglevel error -select_streams s -show_streams -show_entries stream=index:tags=:disposition= -of csv",
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            getSubtitleIndexes.Start();

            //Reads the Console Output
            string subtitleIndexes = getSubtitleIndexes.StandardOutput.ReadToEnd();

            if (subtitleIndexes == "")
            {
                //If the Source video doesnt have Subtitles embedded, then Stream Copy Subtitles will be disabled.
                RadioButtonStreamCopySubtitle.IsChecked = false;
                RadioButtonStreamCopySubtitle.IsEnabled = false;
                SmallScripts.Logging("No embedded Subtitles found!");
            }
            else if(subtitleIndexes != "")
            {
                RadioButtonStreamCopySubtitle.IsEnabled = true;
                SmallScripts.Logging("Found ebedded Subtitles!");
            }
            getSubtitleIndexes.WaitForExit();
        }


        //-------------------------------------------------------------------------------------------------||

        //------------------------------------- Encoder Settings ------------------------------------------||

        public void SetParametersBeforeEncode()
        {
            SmallScripts.Logging("Landed in SetParametersBeforeEncode() function");
            //Needed Parameters for Splitting --------------------------------------------------------||
            videoInput = TextBoxVideoInput.Text;
            //Sets the working directory
            if (CheckBoxCustomTempFolder.IsChecked == false)
            {
                workingTempDirectory = System.IO.Path.Combine(currentDir, "Temp");
            }
            else if (CheckBoxCustomTempFolder.IsChecked == true && TextBoxCustomTempFolder.Text != "Temp Folder")
            {
                workingTempDirectory = System.IO.Path.Combine(TextBoxCustomTempFolder.Text, "Temp");
            }
            //Sets ffmpeg Path
            if (CheckBoxCustomFfmpegPath.IsChecked == false)
            {
                exeffmpegPath = currentDir;
            }
            else if (CheckBoxCustomFfmpegPath.IsChecked == true)
            {
                exeffmpegPath = TextBoxCustomFfmpegPath.Text;
            }
            chunkLengthSplit = Int16.Parse(TextBoxChunkLength.Text);
            //Reencoding
            reencodeBeforeMainEncode = CheckBoxReencode.IsChecked == true;
            preencodeBeforeMainEncode = CheckBoxPreReencode.IsEnabled == true;
            reencodecodec = ComboBoxReencodingMethod.Text;
            prereencodecodec = ComboBoxPreReencodingMethod.Text;
            CheckFfprobe();
            //----------------------------------------------------------------------------------------||
            //Needed Parameters for aomenc Encoding --------------------------------------------------||
            streamFrameRate = TextBoxFramerate.Text;
            maxConcurrencyEncodes = Int16.Parse(TextBoxNumberOfWorkers.Text);
            //Sets the aomenc path
            if (CheckBoxCustomAomencPath.IsChecked == false && ComboBoxEncoder.Text == "aomenc")
            {
                aomenc = Path.Combine(Directory.GetCurrentDirectory(), "aomenc.exe");
                aomEncode = true;
                rav1eEncode = false;
            }
            else if (CheckBoxCustomAomencPath.IsChecked == true && ComboBoxEncoder.Text == "aomenc")
            {
                exeaomencPath = TextBoxCustomAomencPath.Text;
                aomenc = System.IO.Path.Combine(exeaomencPath, "aomenc.exe");
                aomEncode = true;
                rav1eEncode = false;
            }
            //----------------------------------------------------------------------------------------||
            //Needed Parameters for libaom Encoding --------------------------------------------------||
            if (CheckBoxCustomFfmpegPath.IsChecked == false && ComboBoxEncoder.Text == "libaom")
            {
                libaom = Path.Combine(Directory.GetCurrentDirectory(), "ffmpeg.exe");
                libaomEncode = true;
                rav1eEncode = false;
                aomEncode = false;
            }
            //----------------------------------------------------------------------------------------||
            //Needed Parameters for rav1e Encoding ---------------------------------------------------||
            if (CheckBoxCustomRaviePath.IsChecked == false && ComboBoxEncoder.Text == "RAV1E")
            {
                ravie = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "rav1e.exe");
                rav1eEncode = true;
                aomEncode = false;
            }
            else if (CheckBoxCustomRaviePath.IsChecked == true && ComboBoxEncoder.Text == "RAV1E")
            {
                exerav1ePath = TextBoxCustomRaviePath.Text;
                ravie = System.IO.Path.Combine(exerav1ePath, "rav1e.exe");
                rav1eEncode = true;
                aomEncode = false;
            }
            //----------------------------------------------------------------------------------------||
            //Needed Parameters for svt-av1 Encoding -------------------------------------------------||
            if (CheckBoxCustomSVTPath.IsChecked == false)
            {
                svtav1 = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "SvtAv1EncApp.exe");
            }
            else if (CheckBoxCustomSVTPath.IsChecked == true)
            {
                exesvtav1Path = TextBoxCustomSVTPath.Text;
                svtav1 = System.IO.Path.Combine(exesvtav1Path, "SvtAv1EncApp.exe");
            }
            //----------------------------------------------------------------------------------------||
            //Audio Encoding -------------------------------------------------------------------------||
            SetAudioParameters();
            //Video Resizing -------------------------------------------------------------------------||
            if (CheckBoxVideoResize.IsChecked == true)
            {
                videoResize = " -vf scale=" + TextBoxFrameWidth.Text + ":" + TextBoxFrameHeight.Text;
            }
            //----------------------------------------------------------------------------------------||
            //Subtitles ------------------------------------------------------------------------------||
            if (CheckBoxEnableSubtitles.IsChecked == true)
            {
                subtitles = true;
                if (RadioButtonStreamCopySubtitle.IsChecked == true)
                {
                    subtitleStreamCopy = true;
                    SetSubtitleParameters();
                }
                if (RadioButtonCustomSubtitles.IsChecked == true)
                {
                    if (customsubtitleadded != 0)
                    {
                        subtitleCustom = true;
                        SetSubtitleParameters();
                    }
                    else {
                        if (MessageBox.Show("You enabled Subtitles, but it seems there is no Subtitles in the List! Skip Subtitles?", "Error", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            subtitles = false;
                            subtitleStreamCopy = false;
                            subtitleCustom = false;
                        }
                        else
                        {
                            SmallScripts.Cancel.CancelAll = true;
                        }
                    }
                }
            }
            //----------------------------------------------------------------------------------------||
            //Shutdown -------------------------------------------------------------------------------||
            if (CheckBoxShutdownAfterEncode.IsChecked == true)
            {
                shutDownAfterEncode = true;
            }
            CheckDependencies();
            //----------------------------------------------------------------------------------------||
            //Color ----------------------------------------------------------------------------------||
            switch (ComboBoxChroma.Text)
            {
                case "4:2:0":
                    yuvcolorspace = " yuv420p";
                    pipeBitDepth = " yuv420p";
                    if (ComboBoxBitDepth.Text == "10") { pipeBitDepth = " yuv420p10le -strict -1"; }
                    if (ComboBoxBitDepth.Text == "12") { pipeBitDepth = " yuv420p12le -strict -1"; }
                    break;
                case "4:2:2":
                    yuvcolorspace = " yuv422p";
                    pipeBitDepth = " yuv422p -strict -1";
                    if (ComboBoxBitDepth.Text == "10") { pipeBitDepth = " yuv422p10le -strict -1"; }
                    if (ComboBoxBitDepth.Text == "12") { pipeBitDepth = " yuv422p12le -strict -1"; }
                    break;
                case "4:4:4":
                    yuvcolorspace = " yuv444p";
                    pipeBitDepth = " yuv444p -strict -1";
                    if (ComboBoxBitDepth.Text == "10") { pipeBitDepth = " yuv444p10le -strict -1"; }
                    if (ComboBoxBitDepth.Text == "12") { pipeBitDepth = " yuv444p12le -strict -1"; }
                    break;
                default:
                    break;
            }
            switch (ComboBoxColorPrim.Text)
            {
                case "BT.709":
                    colorprimaries = "bt709";
                    break;
                case "BT.2020":
                    colorprimaries = "bt2020";
                    break;
                default:
                    break;
            }
            switch (ComboBoxColorSpace.Text)
            {
                case "BT.709":
                    colormatrix = "bt709";
                    break;
                case "BT.2020C":
                    colormatrix = "bt2020c";
                    break;
                case "BT.2020NC":
                    colormatrix = "bt2020nc";
                    break;
                case "SMPTE2085":
                    colormatrix = "smpte2085";
                    break;
                default:
                    break;
            }
            switch (ComboBoxColorTrans.Text)
            {
                case "BT.709":
                    colortransfer = "bt709";
                    break;
                case "BT.2020-10":
                    colortransfer = "bt2020-10";
                    break;
                case "BT.2020-12":
                    colortransfer = "bt2020-12";
                    break;
                case "SMPTE2084":
                    colortransfer = "smpte2084";
                    break;
                default:
                    break;
            }
            //----------------------------------------------------------------------------------------||
            //Process Priority -----------------------------------------------------------------------||
            switch (ComboBoxProcessPrio.Text)
            {
                case "Normal":
                    processPriority = "Normal";
                    break;
                case "Below Normal":
                    processPriority = "BelowNormal";
                    break;
                default:
                    break;
            }
        }

        public void SetAomencParameters()
        {
            SmallScripts.Logging("Landed in SetAomencParameters() function");
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
            }
            else if (RadioButtonBitrate.IsChecked == true)
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
            }
            else if (CheckBoxAdvancedSettings.IsChecked == true && CheckBoxCustomCommandLine.IsChecked == false)
            {
                if (colormatrix == "bt709") { aomColormatrix = "bt709"; }
                else if (colormatrix == "bt2020c") { aomColormatrix = "bt2020cl"; }
                else if (colormatrix == "bt2020nc") { aomColormatrix = "bt2020ncl"; }
                else if (colormatrix == "smpte2085") { aomColormatrix = "smpte2085"; }

                if (colortransfer == "bt709") {aomColortransfer = "bt709"; }
                else if (colortransfer == "bt2020-10") { aomColortransfer = "bt2020-10bit"; }
                else if (colortransfer == "bt2020-12") { aomColortransfer = "bt2020-12bit"; }
                else if (colortransfer == "smpte2084") { aomColortransfer = "smpte2084"; }

                if (yuvcolorspace == " yuv420p") { aomChromaSubsample = "--i420"; }
                else if (yuvcolorspace == " yuv422p") { aomChromaSubsample = "--i422"; }
                else if (yuvcolorspace == " yuv444p") { aomChromaSubsample = "--i444"; }

                string aqMode = "";
                if (ComboBoxAqMode.Text == "0")
                {
                    aqMode = "0";
                }
                else if (ComboBoxAqMode.Text == "1")
                {
                    aqMode = "1";
                }
                else if (ComboBoxAqMode.Text == "2")
                {
                    aqMode = "2";
                }
                else if (ComboBoxAqMode.Text == "3")
                {
                    aqMode = "3";
                }

                if (CheckBoxAomKeyframefiltering.IsChecked == false) { keyframefilt = "0"; }
                if (CheckBoxAutoAltRef.IsChecked == true) { autoaltref = "1"; }
                if (CheckBoxFrameBoost.IsChecked == true) { frameboost = "1"; }

                allSettingsAom = " --cpu-used=" + SliderPreset.Value + " " + aomChromaSubsample + " --transfer-characteristics=" + aomColortransfer + " --color-primaries=" + colorprimaries + " --matrix-coefficients=" + aomColormatrix + " --bit-depth=" + ComboBoxBitDepth.Text + " --fps=" + TextBoxFramerate.Text + " --threads=" + TextBoxThreads.Text + " --kf-max-dist=" + TextBoxKeyframeInterval.Text + " --tile-rows=" + TextBoxTileRows.Text + " --tile-columns=" + TextBoxTileColumns.Text + " --aq-mode=" + aqMode + aomencQualityMode + " --enable-keyframe-filtering=" + keyframefilt + " --auto-alt-ref=" + autoaltref + " --frame-boost=" + frameboost;
            }
            else if (CheckBoxAdvancedSettings.IsChecked == true && CheckBoxCustomCommandLine.IsChecked == true)
            {
                allSettingsAom = " " + TextBoxCustomCommand.Text;
            }
            SmallScripts.Logging("Set the following Command: " + allSettingsAom);
        }

        public void SetLibAomParameters()
        {
            SmallScripts.Logging("Landed in SetLibAomParameters() function");
            //Sets 2-Pass Mode -----------------------------------------------------------------------||
            if (CheckBoxTwoPass.IsChecked == true)
            {
                numberOfPasses = 2;
            }
            //----------------------------------------------------------------------------------------||
            //Sets Quality Mode ----------------------------------------------------------------------||
            if (RadioButtonConstantQuality.IsChecked == true)
            {
                aomencQualityMode = " -crf " + SliderQuality.Value + " -b:v 0";
            }
            else if (RadioButtonBitrate.IsChecked == true)
            {
                aomencQualityMode = " -b:v " + TextBoxBitrate.Text + "k";
            }
            //----------------------------------------------------------------------------------------||
            //Sets libaom arguments ------------------------------------------------------------------||
            if (ComboBoxBitDepth.Text == "10")
            {
                //pipeBitDepth = " yuv420p10le";
            }
            else if (ComboBoxBitDepth.Text == "12")
            {
                //pipeBitDepth = " yuv420p12le";
            }
            if (CheckBoxAdvancedSettings.IsChecked == false)
            {
                //Basic Settings
                allSettingsAom = aomencQualityMode + " -cpu-used " + SliderPreset.Value + " -r " + TextBoxFramerate.Text + " -threads 2 -g 240 -tile-columns 1 -tile-rows 1";
            }
            else if (CheckBoxAdvancedSettings.IsChecked == true && CheckBoxCustomCommandLine.IsChecked == false)
            {
                string aqMode = "";
                if (ComboBoxAqMode.Text == "0")
                {
                    aqMode = "0";
                }
                else if (ComboBoxAqMode.Text == "1")
                {
                    aqMode = "1";
                }
                else if (ComboBoxAqMode.Text == "2")
                {
                    aqMode = "2";
                }
                else if (ComboBoxAqMode.Text == "3")
                {
                    aqMode = "3";
                }
                allSettingsAom = aomencQualityMode + " -colorspace " + colormatrix + " -color_primaries " + colorprimaries + " -color_trc " + colortransfer + " -cpu-used " + SliderPreset.Value + " -r " + TextBoxFramerate.Text + " -threads " + TextBoxThreads.Text + " -g " + TextBoxKeyframeInterval.Text + " -tile-rows " + TextBoxTileRows.Text + " -tile-columns " + TextBoxTileColumns.Text + " -aq-mode " + aqMode;
            }
            else if (CheckBoxAdvancedSettings.IsChecked == true && CheckBoxCustomCommandLine.IsChecked == true)
            {
                allSettingsAom = " " + TextBoxCustomCommand.Text;
            }
            SmallScripts.Logging("Set the following Command: " + allSettingsAom);

        }

        public void SetRavieParameters()
        {
            SmallScripts.Logging("Landed in SetRavieParameters() function");
            //Sets 2-Pass Mode -----------------------------------------------------------------------||
            if (CheckBoxTwoPass.IsChecked == true)
            {
                numberOfPasses = 2;
            }
            //----------------------------------------------------------------------------------------||
            //Sets Quality Mode ----------------------------------------------------------------------||
            if (RadioButtonConstantQuality.IsChecked == true)
            {
                ravieQualityMode = " --quantizer " + SliderQuality.Value;
            }
            else if (RadioButtonBitrate.IsChecked == true)
            {
                ravieQualityMode = " --bitrate " + TextBoxBitrate.Text;
            }
            //----------------------------------------------------------------------------------------||
            //Sets All Encoding Settings--------------------------------------------------------------||
            if (CheckBoxAdvancedSettings.IsChecked == false)
            {
                //Basic Settings
                allSettingsRavie = " --speed " + SliderPreset.Value + " --keyint 240 --tile-rows 1 --tile-cols 4 --primaries BT709 --transfer BT709 --matrix BT709" + ravieQualityMode;
            }
            else if (CheckBoxAdvancedSettings.IsChecked == true && CheckBoxCustomCommandLine.IsChecked == false)
            {

                if (colortransfer == "bt709") { rav1eColortransfer = "BT709"; }
                else if (colortransfer == "bt2020-10") { rav1eColortransfer = "BT2020_10Bit"; }
                else if (colortransfer == "bt2020-12") { rav1eColortransfer = "BT2020_12Bit"; }
                else if (colortransfer == "smpte2084") { rav1eColortransfer = "SMPTE2084"; }

                if (colormatrix == "bt709") { rav1eColormatrix = "BT709"; }
                else if (colormatrix == "bt2020c") { rav1eColormatrix = "BT2020CL"; }
                else if (colormatrix == "bt2020nc") { rav1eColormatrix = "BT2020NCL"; }
                else if (colormatrix == "smpte2085") { rav1eColormatrix = "SMPTE2085"; }

                allSettingsRavie = " --primaries " + colorprimaries + " --transfer " + rav1eColortransfer + " --matrix " + rav1eColormatrix + " --speed " + SliderPreset.Value + " --keyint " + TextBoxKeyframeInterval.Text + " --tile-rows " + TextBoxTileRows.Text + " --tile-cols " + TextBoxTileColumns.Text + " --threads " + TextBoxThreads.Text + ravieQualityMode;
            }
            else if (CheckBoxAdvancedSettings.IsChecked == true && CheckBoxCustomCommandLine.IsChecked == true)
            {
                allSettingsRavie = " " + TextBoxCustomCommand.Text;
            }
            //----------------------------------------------------------------------------------------||
            //Sets Piping Bit-Depth Settings because rav1e can't convert it itself--------------------||
            if (ComboBoxBitDepth.Text == "10")
            {
                pipeBitDepth = " yuv420p10le -strict -1";
            }
            else if (ComboBoxBitDepth.Text == "12")
            {
                pipeBitDepth = " yuv420p12le -strict -1";
            }
            SmallScripts.Logging("Set the following Command: " + allSettingsRavie);
        }

        public void SetSVTAV1Parameters()
        {
            SmallScripts.Logging("Landed in SetSVTAV1Parameters() function");
            //Sets 2-Pass Mode -----------------------------------------------------------------------||
            if (CheckBoxTwoPass.IsChecked == true)
            {
                numberOfPasses = 2;
            }
            //----------------------------------------------------------------------------------------||
            //Sets Quality Mode ----------------------------------------------------------------------||
            if (RadioButtonConstantQuality.IsChecked == true)
            {
                svtav1QualityMode = "-rc 0 -q " + SliderQuality.Value;
            }
            else if (RadioButtonBitrate.IsChecked == true)
            {
                svtav1QualityMode = "-rc 1 -tbr " + TextBoxBitrate.Text;
            }
            //----------------------------------------------------------------------------------------||
            //Sets All Encoding Settings--------------------------------------------------------------||
            if (CheckBoxAdvancedSettings.IsChecked == false)
            {
                //Basic Settings
                allSettingsSvtav1 = "-enc-mode " + SliderPreset.Value + " -bit-depth " + ComboBoxBitDepth.Text + " " + svtav1QualityMode;
                if (numberOfPasses == 2)
                {
                    allSettingsSvtav1SecondPass = "-enc-mode-2p " + SliderPreset.Value + " -bit-depth " + ComboBoxBitDepth.Text + " " + svtav1QualityMode;
                }
            }
            else if (CheckBoxAdvancedSettings.IsChecked == true && CheckBoxCustomCommandLine.IsChecked == false)
            {
                allSettingsSvtav1 = "-enc-mode " + SliderPreset.Value + " -bit-depth " + ComboBoxBitDepth.Text + " -adaptive-quantization " + ComboBoxAqMode.Text + svtav1QualityMode;
                if (numberOfPasses == 2)
                {
                    allSettingsSvtav1SecondPass = "-enc-mode-2p " + SliderPreset.Value + " -bit-depth " + ComboBoxBitDepth.Text + " -adaptive-quantization " + ComboBoxAqMode.Text + svtav1QualityMode;
                }
            }
            else if (CheckBoxAdvancedSettings.IsChecked == true && CheckBoxCustomCommandLine.IsChecked == true)
            {
                allSettingsSvtav1 = " " + TextBoxCustomCommand.Text;
            }
            //----------------------------------------------------------------------------------------||
            //Sets Piping Bit-Depth Settings because svt can't convert it itself----------------------||
            if (ComboBoxBitDepth.Text == "10")
            {
                pipeBitDepth = " yuv420p10le -strict -1";
            }
            SmallScripts.Logging("Set the following Command: " + allSettingsSvtav1);
        }

        public void SetAudioParameters()
        {
            SmallScripts.Logging("Landed in SetAudioParameters() function");
            if (CheckBoxAudioEncoding.IsChecked == true)
            {
                if (CheckBoxAudioTrackOne.IsChecked == false && CheckBoxAudioTrackTwo.IsChecked == false && CheckBoxAudioTrackThree.IsChecked == false && CheckBoxAudioTrackFour.IsChecked == false)
                {
                    if (MessageBox.Show("You enabled Audio Encoding without selecting the Audio Track! Defaulting to Track Nr.1?", "Error", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        SmallScripts.Logging("Defaulting to Track Nr.1 due to user error.");
                        CheckBoxAudioTrackOne.IsChecked = true;
                    }
                    else
                    {
                        SmallScripts.Logging("Cancelling all due to User Error selecting the Audio Stream.");
                        SmallScripts.Cancel.CancelAll = true;
                    }
                }
                audioEncoding = true;
                audioCodec = ComboBoxAudioCodec.Text;
                audioCodecTrackTwo = ComboBoxAudioCodecTrackTwo.Text;
                audioCodecTrackThree = ComboBoxAudioCodecTrackThree.Text;
                audioCodecTrackFour = ComboBoxAudioCodecTrackFour.Text;
                audioBitrate = Int16.Parse(TextBoxAudioBitrate.Text);
                audioBitrateTrackTwo = Int16.Parse(TextBoxAudioBitrateTrackTwo.Text);
                audioBitrateTrackThree = Int16.Parse(TextBoxAudioBitrateTrackThree.Text);
                audioBitrateTrackFour = Int16.Parse(TextBoxAudioBitrateTrackFour.Text);
                trackOne = CheckBoxAudioTrackOne.IsChecked == true;
                trackTwo = CheckBoxAudioTrackTwo.IsChecked == true;
                trackThree = CheckBoxAudioTrackThree.IsChecked == true;
                trackFour = CheckBoxAudioTrackFour.IsChecked == true;

                SmallScripts.Logging("audioEncoding : " + audioEncoding.ToString());
                SmallScripts.Logging("audioCodec : " + audioCodec);
                SmallScripts.Logging("audioCodecTrackTwo : " + audioCodecTrackTwo);
                SmallScripts.Logging("audioCodecTrackThree : " + audioCodecTrackThree);
                SmallScripts.Logging("audioCodecTrackFour : " + audioCodecTrackFour);
                SmallScripts.Logging("audioBitrate : " + Int16.Parse(TextBoxAudioBitrate.Text).ToString());
                SmallScripts.Logging("audioBitrateTrackTwo : " + Int16.Parse(TextBoxAudioBitrateTrackTwo.Text).ToString());
                SmallScripts.Logging("audioBitrateTrackThree : " + Int16.Parse(TextBoxAudioBitrateTrackThree.Text).ToString());
                SmallScripts.Logging("audioBitrateTrackFour : " + Int16.Parse(TextBoxAudioBitrateTrackFour.Text).ToString());
                SmallScripts.Logging("trackOne : " + trackOne.ToString());
                SmallScripts.Logging("trackTwo : " + trackTwo.ToString());
                SmallScripts.Logging("trackThree : " + trackThree.ToString());
                SmallScripts.Logging("trackFour : " + trackFour.ToString());

                switch (ComboBoxTrackOneChannels.SelectedIndex.ToString())
                {
                    case "0":
                        audioChannelsTrackOne = 1;
                        break;
                    case "1":
                        audioChannelsTrackOne = 2;
                        break;
                    case "2":
                        audioChannelsTrackOne = 6;
                        break;
                    case "3":
                        audioChannelsTrackOne = 8;
                        break;
                    default:
                        break;
                }
                SmallScripts.Logging("audioChannelsTrackOne : " + audioChannelsTrackOne.ToString());

                switch (ComboBoxTrackTwoChannels.SelectedIndex.ToString())
                {
                    case "0":
                        audioChannelsTrackTwo = 1;
                        break;
                    case "1":
                        audioChannelsTrackTwo = 2;
                        break;
                    case "2":
                        audioChannelsTrackTwo = 6;
                        break;
                    case "3":
                        audioChannelsTrackTwo = 8;
                        break;
                    default:
                        break;
                }
                SmallScripts.Logging("audioChannelsTrackTwo : " + audioChannelsTrackTwo.ToString());

                switch (ComboBoxTrackThreeChannels.SelectedIndex.ToString())
                {
                    case "0":
                        audioChannelsTrackThree = 1;
                        break;
                    case "1":
                        audioChannelsTrackThree = 2;
                        break;
                    case "2":
                        audioChannelsTrackThree = 6;
                        break;
                    case "3":
                        audioChannelsTrackThree = 8;
                        break;
                    default:
                        break;
                }
                SmallScripts.Logging("audioChannelsTrackThree : " + audioChannelsTrackThree.ToString());

                switch (ComboBoxTrackFourChannels.SelectedIndex.ToString())
                {
                    case "0":
                        audioChannelsTrackFour = 1;
                        break;
                    case "1":
                        audioChannelsTrackFour = 2;
                        break;
                    case "2":
                        audioChannelsTrackFour = 6;
                        break;
                    case "3":
                        audioChannelsTrackFour = 8;
                        break;
                    default:
                        break;
                }
                SmallScripts.Logging("audioChannelsTrackFour : " + audioChannelsTrackFour.ToString());
            }
            
        }

        public void SetSubtitleParameters()
        {
            SubtitleChunks = ListBoxSubtitles.Items.OfType<string>().ToArray();
        }

        //-------------------------------------------------------------------------------------------------||

        //----------------------------------------- Encoders ----------------------------------------------||

        private void EncodeAomencOrRav1e()
        {
            SmallScripts.Logging("Landed in EncodeAomencOrRav1e() function.");
            MainProgressBar.Dispatcher.Invoke(() => MainProgressBar.Maximum = Int16.Parse(numberofvideoChunks), DispatcherPriority.Background);
            pLabel.Dispatcher.Invoke(() => pLabel.Content = "0 / " + MainProgressBar.Maximum, DispatcherPriority.Background);
            string labelstring = videoChunks.Count().ToString();
            //Sets the Time for later eta calculation
            DateTime starttime = DateTime.Now;
            starttimea = starttime;
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

                                    if (aomEncode == true)
                                    {
                                        startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + chunksDir + "\\" + items + '\u0022' + " " + videoResize + " -pix_fmt" + pipeBitDepth + " -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + aomenc + '\u0022' + " - --passes=1" + allSettingsAom + " --output=" + '\u0022' + chunksDir + "\\" + items + "-av1.ivf" + '\u0022';
                                    }
                                    else if (rav1eEncode == true)
                                    {
                                        startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + chunksDir + "\\" + items + '\u0022' + " " + videoResize + " -pix_fmt" + pipeBitDepth + " -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + ravie + '\u0022' + " - " + allSettingsRavie + " --output " + '\u0022' + chunksDir + "\\" + items + "-av1.ivf" + '\u0022';
                                    }
                                    else if (libaomEncode == true)
                                    {
                                        startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + chunksDir + "\\" + items + '\u0022' + " " + videoResize + " -pix_fmt" + pipeBitDepth + " -strict experimental -c:v libaom-av1 " + allSettingsAom + " " + '\u0022' + chunksDir + "\\" + items + "-av1.ivf" + '\u0022';
                                    }

                                    process.StartInfo = startInfo;
                                    //Console.WriteLine(startInfo.Arguments);
                                    process.Start();

                                    //Sets the Process Priority
                                    if (processPriority == "BelowNormal")
                                    {
                                        process.PriorityClass = ProcessPriorityClass.BelowNormal;
                                    }
                                    
                                    process.WaitForExit();

                                    //Progressbar +1
                                    MainProgressBar.Dispatcher.Invoke(() => MainProgressBar.Value += 1, DispatcherPriority.Background);
                                    //Label of Progressbar = Progressbar
                                    TimeSpan timespent = DateTime.Now - starttime;

                                    pLabel.Dispatcher.Invoke(() => pLabel.Content = MainProgressBar.Value + " / " + labelstring + " - " + Math.Round(Convert.ToDecimal(((((Int16.Parse(streamLength) * Int16.Parse(streamFrameRateLabel)) / Int16.Parse(labelstring)) * MainProgressBar.Value) / timespent.TotalSeconds)), 2).ToString() + "fps" + " - " + Math.Round((((timespent.TotalSeconds / MainProgressBar.Value) * (Int16.Parse(labelstring) - MainProgressBar.Value)) / 60), MidpointRounding.ToEven) + "min left", DispatcherPriority.Background);

                                    if (SmallScripts.Cancel.CancelAll == false)
                                    {
                                        //Write Item to file for later resume if something bad happens
                                        SmallScripts.WriteToFileThreadSafe(items, "encoded.log");
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

                                        if (aomEncode == true)
                                        {
                                            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + chunksDir + "\\" + items + '\u0022' + " " + videoResize + " -pix_fmt" + pipeBitDepth + " -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + aomenc + '\u0022' + " - --passes=2 --pass=1 --fpf=" + '\u0022' + chunksDir + "\\" + items + "_stats.log" + '\u0022' + allSettingsAom + " --output=NUL";
                                        }
                                        else if (rav1eEncode == true)
                                        {
                                            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + chunksDir + "\\" + items + '\u0022' + " " + videoResize + " -pix_fmt" + pipeBitDepth + " -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + ravie + '\u0022' + " - " + allSettingsRavie + " --first-pass " + '\u0022' + chunksDir + "\\" + items + "_stats.log" + '\u0022';
                                        }
                                        else if (libaomEncode == true)
                                        {
                                            startInfo.Arguments = "/C ffmpeg.exe -y -i " + '\u0022' + chunksDir + "\\" + items + '\u0022' + " " + videoResize + " -pix_fmt" + pipeBitDepth + " -strict experimental -c:v libaom-av1 " + allSettingsAom + " -pass 1 -passlogfile " + '\u0022' + chunksDir + "\\" + items + "_stats.log" + '\u0022' + " -f matroska NUL";
                                        }
                                        
                                        process.StartInfo = startInfo;
                                        //Console.WriteLine(startInfo.Arguments);
                                        process.Start();

                                        //Sets the Process Priority
                                        if (processPriority == "BelowNormal")
                                        {
                                            process.PriorityClass = ProcessPriorityClass.BelowNormal;
                                        }

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

                                    if (aomEncode == true)
                                    {
                                        startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + chunksDir + "\\" + items + '\u0022' + " " + videoResize + " -pix_fmt" + pipeBitDepth + " -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + aomenc + '\u0022' + " - --passes=2 --pass=2 --fpf=" + '\u0022' + chunksDir + "\\" + items + "_stats.log" + '\u0022' + allSettingsAom + " --output=" + '\u0022' + chunksDir + "\\" + items + "-av1.ivf" + '\u0022';
                                    }
                                    else if (rav1eEncode == true)
                                    {
                                        startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + chunksDir + "\\" + items + '\u0022' + " " + videoResize + " -pix_fmt" + pipeBitDepth + " -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + ravie + '\u0022' + " - " + allSettingsRavie + " --second-pass " + '\u0022' + chunksDir + "\\" + items + "_stats.log" + '\u0022' + " --output " + '\u0022' + chunksDir + "\\" + items + "-av1.ivf" + '\u0022';
                                    }
                                    else if (libaomEncode == true)
                                    {
                                        startInfo.Arguments = "/C ffmpeg.exe -y -i " + '\u0022' + chunksDir + "\\" + items + '\u0022' + " " + videoResize + " -pix_fmt" + pipeBitDepth + " -strict experimental -c:v libaom-av1 " + allSettingsAom + " -pass 2 -passlogfile " + '\u0022' + chunksDir + "\\" + items + "_stats.log" + '\u0022' + " " + '\u0022' + chunksDir + "\\" + items + " - av1.ivf" + '\u0022';
                                    }
                                    
                                    process.StartInfo = startInfo;
                                    //Console.WriteLine(startInfo.Arguments);
                                    process.Start();

                                    //Sets the Process Priority
                                    if (processPriority == "BelowNormal")
                                    {
                                        process.PriorityClass = ProcessPriorityClass.BelowNormal;
                                    }

                                    process.WaitForExit();

                                    MainProgressBar.Dispatcher.Invoke(() => MainProgressBar.Value += 1, DispatcherPriority.Background);
                                    TimeSpan timespent = DateTime.Now - starttime;
                                    pLabel.Dispatcher.Invoke(() => pLabel.Content = MainProgressBar.Value + " / " + labelstring + " - " + Math.Round(Convert.ToDecimal(((((Int16.Parse(streamLength) * Int16.Parse(streamFrameRateLabel)) / Int16.Parse(labelstring)) * MainProgressBar.Value) / timespent.TotalSeconds)), 2).ToString() + "fps" + " - " + Math.Round((((timespent.TotalSeconds / MainProgressBar.Value) * (Int16.Parse(labelstring) - MainProgressBar.Value)) / 60), MidpointRounding.ToEven) + "min left", DispatcherPriority.Background);

                                    if (SmallScripts.Cancel.CancelAll == false)
                                    {
                                        //Write Item to file for later resume if something bad happens
                                        SmallScripts.WriteToFileThreadSafe(items, "encoded.log");
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

        private void EncodeSVTAV1()
        {
            SmallScripts.Logging("Landed in EncodeSVTAV1() function.");
            //Sets the Timer
            DateTime starttime = DateTime.Now;
            starttimea = starttime;
            //Sets the Maximum of the Progressbar
            MainProgressBar.Dispatcher.Invoke(() => MainProgressBar.Maximum = Int16.Parse(numberofvideoChunks), DispatcherPriority.Background);
            //Sets the Label of the Progressbar
            pLabel.Dispatcher.Invoke(() => pLabel.Content = "0 / " + MainProgressBar.Maximum, DispatcherPriority.Background);
            string labelstring = videoChunks.Count().ToString();

            //-n set to 9.999.999s (~2777hours) so we don't need counting the frames... without -n the pipe would break

            foreach (var items in videoChunks)
            {
                if (SmallScripts.Cancel.CancelAll == false)
                {
                    Process process = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    if (numberOfPasses == 1)
                    {
                        //1 Pass
                        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        startInfo.UseShellExecute = true;
                        startInfo.FileName = "cmd.exe";
                        startInfo.WorkingDirectory = exeffmpegPath + "\\";
                        startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + chunksDir + "\\" + items + '\u0022' + " " + videoResize + " -pix_fmt" + pipeBitDepth + " -nostdin -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + svtav1 + '\u0022' + " -i stdin " + allSettingsSvtav1 + " -n 9999999 -b " + '\u0022' + chunksDir + "\\" + items + "-av1.ivf" + '\u0022';
                        process.StartInfo = startInfo;
                        //Console.WriteLine(startInfo.Arguments);
                        process.Start();

                        //Sets the Process Priority
                        if (processPriority == "BelowNormal")
                        {
                            process.PriorityClass = ProcessPriorityClass.BelowNormal;
                        }

                        process.WaitForExit();
                    }

                    if (numberOfPasses == 2)
                    {
                        //2 Pass

                        //1st Pass
                        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        startInfo.UseShellExecute = true;
                        startInfo.FileName = "cmd.exe";
                        startInfo.WorkingDirectory = exeffmpegPath + "\\";
                        startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + chunksDir + "\\" + items + '\u0022' + " " + videoResize + " -pix_fmt" + pipeBitDepth + " -nostdin -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + svtav1 + '\u0022' + " -i stdin " + allSettingsSvtav1 + " -n 9999999 -b NUL -output-stat-file " + '\u0022' + chunksDir + "\\" + items + "-av1pass.stats" + '\u0022';
                        process.StartInfo = startInfo;
                        //Console.WriteLine(startInfo.Arguments);
                        process.Start();

                        //Sets the Process Priority
                        if (processPriority == "BelowNormal")
                        {
                            process.PriorityClass = ProcessPriorityClass.BelowNormal;
                        }

                        process.WaitForExit();

                        //2nd Pass
                        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        startInfo.UseShellExecute = true;
                        startInfo.FileName = "cmd.exe";
                        startInfo.WorkingDirectory = exeffmpegPath + "\\";
                        startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + chunksDir + "\\" + items + '\u0022' + " " + videoResize + " -pix_fmt" + pipeBitDepth + " -nostdin -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + svtav1 + '\u0022' + " -i stdin " + allSettingsSvtav1SecondPass + " -n 9999999 -b " + '\u0022' + chunksDir + "\\" + items + "-av1.ivf" + '\u0022' + " -input-stat-file " + +'\u0022' + chunksDir + "\\" + items + "-av1pass.stats" + '\u0022';
                        process.StartInfo = startInfo;
                        //Console.WriteLine(startInfo.Arguments);
                        process.Start();

                        //Sets the Process Priority
                        if (processPriority == "BelowNormal")
                        {
                            process.PriorityClass = ProcessPriorityClass.BelowNormal;
                        }

                        process.WaitForExit();
                    }

                    //Progressbar +1
                    MainProgressBar.Dispatcher.Invoke(() => MainProgressBar.Value += 1, DispatcherPriority.Background);
                    //Label of Progressbar = Progressbar
                    TimeSpan timespent = DateTime.Now - starttime;

                    pLabel.Dispatcher.Invoke(() => pLabel.Content = MainProgressBar.Value + " / " + labelstring + " - " + Math.Round(Convert.ToDecimal(((((Int16.Parse(streamLength) * Int16.Parse(streamFrameRateLabel)) / Int16.Parse(labelstring)) * MainProgressBar.Value) / timespent.TotalSeconds)), 2).ToString() + "fps" + " - " + Math.Round((((timespent.TotalSeconds / MainProgressBar.Value) * (Int16.Parse(labelstring) - MainProgressBar.Value)) / 60), MidpointRounding.ToEven) + "min left", DispatcherPriority.Background);

                    if (SmallScripts.Cancel.CancelAll == false)
                    {
                        //Write Item to file for later resume if something bad happens
                        SmallScripts.WriteToFileThreadSafe(items, "encoded.log");
                    }
                    else
                    {
                        SmallScripts.KillInstances();
                    }
                }
            }
        }


        //-------------------------------------------------------------------------------------------------||

        //----------------------------------------- Settings ----------------------------------------------||
        public void LoadSettings(string profileName, bool saveJob, bool saveQueue, string saveQueueName)
        {
            SmallScripts.Logging("Landed in LoadSettings() function. Profilename: " + profileName);
            //Loads Settings from XML File -------------------------------------------------------------------------------------||
            XmlDocument doc = new XmlDocument();

            string directory = "";

            if (saveJob == true)
            {
                directory = "unfinishedjob.xml";
            }
            else if (saveQueue == true)
            {
                directory = "Queue\\" + saveQueueName;
            }
            else
            {
                directory = currentDir + "\\Profiles\\" + profileName;
            }

            doc.Load(directory);
            XmlNodeList node = doc.GetElementsByTagName("Settings");
            foreach (XmlNode n in node[0].ChildNodes)
            {
                switch (n.Name)
                {
                    case "Logging":
                        if (n.InnerText == "False") 
                        { 
                            logging = false;
                            CheckBoxLogging.IsChecked = false;
                            if (File.Exists("program.log")) { File.Delete("program.log"); }
                        } else if (n.InnerText == "True") 
                        { 
                            logging = true;
                            CheckBoxLogging.IsChecked = true;
                        }
                        break;
                    case "ChunkLength":
                        TextBoxChunkLength.Text = n.InnerText;
                        break;
                    case "Reencode":
                        if (n.InnerText == "True")
                        {
                            CheckBoxReencode.IsChecked = true;
                        }
                        else if (n.InnerText == "False")
                        {
                            CheckBoxReencode.IsChecked = false;
                        }
                        break;
                    case "Prereencode":
                        if (n.InnerText == "True")
                        {
                            CheckBoxPreReencode.IsChecked = true;
                        }
                        else if (n.InnerText == "False")
                        {
                            CheckBoxPreReencode.IsChecked = false;
                        }
                        break;
                    case "Reencodecodec":
                        if (n.InnerText == "utvideo") { ComboBoxReencodingMethod.SelectedIndex = 0; }
                        if (n.InnerText == "x264") { ComboBoxReencodingMethod.SelectedIndex = 1; }
                        break;
                    case "Prereencodecodec":
                        if (n.InnerText == "utvideo") { ComboBoxPreReencodingMethod.SelectedIndex = 0; }
                        if (n.InnerText == "x264") { ComboBoxPreReencodingMethod.SelectedIndex = 1; }
                        break;
                    case "Workers":
                        TextBoxNumberOfWorkers.Text = n.InnerText;
                        break;
                    case "Encoder":
                        if (n.InnerText == "aomenc") { ComboBoxEncoder.SelectedIndex = 0; }
                        if (n.InnerText == "RAV1E") { ComboBoxEncoder.SelectedIndex = 1; }
                        if (n.InnerText == "SVT-AV1") { ComboBoxEncoder.SelectedIndex = 2; }
                        break;
                    case "BitDepth":
                        if (n.InnerText == "8") { ComboBoxBitDepth.SelectedIndex = 0; }
                        if (n.InnerText == "10") { ComboBoxBitDepth.SelectedIndex = 1; }
                        if (n.InnerText == "12") { ComboBoxBitDepth.SelectedIndex = 2; }
                        break;
                    case "Preset":
                        SliderPreset.Value = Int16.Parse(n.InnerText);
                        break;
                    case "TwoPassEncoding":
                        if (n.InnerText == "True") { CheckBoxTwoPass.IsChecked = true; } else { CheckBoxTwoPass.IsChecked = false; }
                        break;
                    case "QualityMode":
                        if (n.InnerText == "True") { RadioButtonConstantQuality.IsChecked = true; } else { RadioButtonConstantQuality.IsChecked = false; }
                        break;
                    case "Quality":
                        SliderQuality.Value = Int16.Parse(n.InnerText);
                        break;
                    case "BitrateMode":
                        if (n.InnerText == "True") { RadioButtonBitrate.IsChecked = true; } else { RadioButtonBitrate.IsChecked = false; }
                        break;
                    case "Bitrate":
                        TextBoxBitrate.Text = n.InnerText;
                        break;
                    case "CBRActive":
                        if (n.InnerText == "True") { CheckBoxCBR.IsChecked = true; } else { CheckBoxCBR.IsChecked = false; }
                        break;
                    case "AdvancedSettingsActive":
                        if (n.InnerText == "True") { CheckBoxAdvancedSettings.IsChecked = true; } else { CheckBoxAdvancedSettings.IsChecked = false; }
                        break;
                    case "AdvancedSettingsThreads":
                        TextBoxThreads.Text = n.InnerText;
                        break;
                    case "AdvancedSettingsTileColumns":
                        TextBoxTileColumns.Text = n.InnerText;
                        break;
                    case "AdvancedSettingsTileRows":
                        TextBoxTileRows.Text = n.InnerText;
                        break;
                    case "AdvancedSettingsAQMode":
                        ComboBoxAqMode.Text = n.InnerText;
                        break;
                    case "AdvancedSettingsKeyFrameInterval":
                        TextBoxKeyframeInterval.Text = n.InnerText;
                        break;
                    case "AdvancedSettingsCustomCommandActive":
                        if (n.InnerText == "True") { CheckBoxCustomCommandLine.IsChecked = true; } else { CheckBoxCustomCommandLine.IsChecked = false; }
                        break;
                    case "AdvancedSettingsCustomCommand":
                        TextBoxCustomCommand.Text = n.InnerText;
                        break;
                    case "AdvancedSettingsAomKeyframeFiltering":
                        if (n.InnerText == "False") { CheckBoxAomKeyframefiltering.IsChecked = false; }
                        break;
                    case "AdvancedSettingsAomAutoAltRef":
                        if (n.InnerText == "True") { CheckBoxAutoAltRef.IsChecked = true; } else { CheckBoxAutoAltRef.IsChecked = false; }
                        break;
                    case "AdvancedSettingsAomFrameboost":
                        if (n.InnerText == "True") { CheckBoxFrameBoost.IsChecked = true; } else { CheckBoxFrameBoost.IsChecked = false; }
                        break;
                    case "ShutdownAfterEncode":
                        if (n.InnerText == "True") { CheckBoxShutdownAfterEncode.IsChecked = true; } else { CheckBoxShutdownAfterEncode.IsChecked = false; }
                        break;
                    case "DeleteTempFiles":
                        if (n.InnerText == "True") { CheckBoxDeleteTempFiles.IsChecked = true; } else { CheckBoxDeleteTempFiles.IsChecked = false; }
                        break;
                    case "CustomFfmpegPathActive":
                        if (n.InnerText == "True") { CheckBoxCustomFfmpegPath.IsChecked = true; } else { CheckBoxCustomFfmpegPath.IsChecked = false; }
                        break;
                    case "CustomFfmpegPath":
                        TextBoxCustomFfmpegPath.Text = n.InnerText;
                        break;
                    case "CustomFfprobePathActive":
                        if (n.InnerText == "True") {
                            CheckBoxCustomFfprobePath.IsChecked = true;
                            CheckFfprobe();
                        }
                        else
                        {
                            CheckBoxCustomFfprobePath.IsChecked = false;
                        }
                        break;
                    case "CustomFfprobePath":
                        TextBoxCustomFfprobePath.Text = n.InnerText;
                        break;
                    case "CustomAomencPathActive":
                        if (n.InnerText == "True") { CheckBoxCustomAomencPath.IsChecked = true; } else { CheckBoxCustomAomencPath.IsChecked = false; }
                        break;
                    case "CustomAomencPath":
                        TextBoxCustomAomencPath.Text = n.InnerText;
                        break;
                    case "CustomTempPathActive":
                        if (n.InnerText == "True") { CheckBoxCustomTempFolder.IsChecked = true; } else { CheckBoxCustomTempFolder.IsChecked = false; }
                        break;
                    case "CustomTempPath":
                        TextBoxCustomTempFolder.Text = n.InnerText;
                        break;
                    case "CustomRaviePathActive":
                        if (n.InnerText == "True") { CheckBoxCustomRaviePath.IsChecked = true; } else { CheckBoxCustomRaviePath.IsChecked = false; }
                        break;
                    case "CustomRaviePath":
                        TextBoxCustomRaviePath.Text = n.InnerText;
                        break;
                    case "CustomSvtaviPathActive":
                        if (n.InnerText == "True") { CheckBoxCustomSVTPath.IsChecked = true; } else { CheckBoxCustomSVTPath.IsChecked = false; }
                        break;
                    case "CustomSvtaviPath":
                        TextBoxCustomSVTPath.Text = n.InnerText;
                        break;
                    case "CustomBackground":
                        if (n.InnerText == "True") { customBackground = true; } else { customBackground = false; }
                        break;
                    case "CustomBackgroundPath":
                        if (customBackground == true)
                        {
                            Uri fileUri = new Uri(n.InnerText);
                            imgDynamic.Source = new BitmapImage(fileUri);
                            customBackground = true;
                            PathToBackground = n.InnerText;
                        }
                        break;
                    case "AudioEncoding":
                        if (n.InnerText == "True") { CheckBoxAudioEncoding.IsChecked = true; } else { CheckBoxAudioEncoding.IsChecked = false; }
                        break;
                    case "AudioCodec":
                        ComboBoxAudioCodec.Text = n.InnerText;
                        break;
                    case "AudioCodecTrackTwo":
                        ComboBoxAudioCodecTrackTwo.Text = n.InnerText;
                        break;
                    case "AudioCodecTrackThree":
                        ComboBoxAudioCodecTrackThree.Text = n.InnerText;
                        break;
                    case "AudioCodecTrackFour":
                        ComboBoxAudioCodecTrackFour.Text = n.InnerText;
                        break;
                    case "AudioBitrate":
                        TextBoxAudioBitrate.Text = n.InnerText;
                        break;
                    case "AudioBitrateTrackTwo":
                        TextBoxAudioBitrateTrackTwo.Text = n.InnerText;
                        break;
                    case "AudioBitrateTrackThree":
                        TextBoxAudioBitrateTrackThree.Text = n.InnerText;
                        break;
                    case "AudioBitrateTrackFour":
                        TextBoxAudioBitrateTrackFour.Text = n.InnerText;
                        break;
                    case "AudioTrackOne":
                        if (n.InnerText == "True") { CheckBoxAudioTrackOne.IsChecked = true; } else { CheckBoxAudioTrackOne.IsChecked = false; }
                        break;
                    case "AudioTrackTwo":
                        if (n.InnerText == "True") { CheckBoxAudioTrackTwo.IsChecked = true; } else { CheckBoxAudioTrackTwo.IsChecked = false; }
                        break;
                    case "AudioTrackThree":
                        if (n.InnerText == "True") { CheckBoxAudioTrackThree.IsChecked = true; } else { CheckBoxAudioTrackThree.IsChecked = false; }
                        break;
                    case "AudioTrackFour":
                        if (n.InnerText == "True") { CheckBoxAudioTrackFour.IsChecked = true; } else { CheckBoxAudioTrackFour.IsChecked = false; }
                        break;
                    case "AudioChannelTrackOne":
                        if (n.InnerText == "1 (Mono)") { ComboBoxTrackOneChannels.SelectedIndex = 0; }
                        if (n.InnerText == "2.0 (Stereo)") { ComboBoxTrackOneChannels.SelectedIndex = 1; }
                        if (n.InnerText == "5.1") { ComboBoxTrackOneChannels.SelectedIndex = 2; }
                        if (n.InnerText == "7.1") { ComboBoxTrackOneChannels.SelectedIndex = 3; }
                        break;
                    case "AudioChannelTrackTwo":
                        if (n.InnerText == "1 (Mono)") { ComboBoxTrackTwoChannels.SelectedIndex = 0; }
                        if (n.InnerText == "2.0 (Stereo)") { ComboBoxTrackTwoChannels.SelectedIndex = 1; }
                        if (n.InnerText == "5.1") { ComboBoxTrackTwoChannels.SelectedIndex = 2; }
                        if (n.InnerText == "7.1") { ComboBoxTrackTwoChannels.SelectedIndex = 3; }
                        break;
                    case "AudioChannelTrackThree":
                        if (n.InnerText == "1 (Mono)") { ComboBoxTrackThreeChannels.SelectedIndex = 0; }
                        if (n.InnerText == "2.0 (Stereo)") { ComboBoxTrackThreeChannels.SelectedIndex = 1; }
                        if (n.InnerText == "5.1") { ComboBoxTrackThreeChannels.SelectedIndex = 2; }
                        if (n.InnerText == "7.1") { ComboBoxTrackThreeChannels.SelectedIndex = 3; }
                        break;
                    case "AudioChannelTrackFour":
                        if (n.InnerText == "1 (Mono)") { ComboBoxTrackFourChannels.SelectedIndex = 0; }
                        if (n.InnerText == "2.0 (Stereo)") { ComboBoxTrackFourChannels.SelectedIndex = 1; }
                        if (n.InnerText == "5.1") { ComboBoxTrackFourChannels.SelectedIndex = 2; }
                        if (n.InnerText == "7.1") { ComboBoxTrackFourChannels.SelectedIndex = 3; }
                        break;
                    case "VideoResize":
                        if (n.InnerText == "True") { CheckBoxVideoResize.IsChecked = true; } else { CheckBoxVideoResize.IsChecked = false; }
                        break;
                    case "ResizeFrameHeight":
                        TextBoxFrameHeight.Text = n.InnerText;
                        break;
                    case "ResizeFrameWidth":
                        TextBoxFrameWidth.Text = n.InnerText;
                        break;
                    case "SubtitleEnabled":
                        if (n.InnerText == "True") { CheckBoxEnableSubtitles.IsChecked = true; } else { CheckBoxEnableSubtitles.IsChecked = false; }
                        break;
                    case "SubtitleEnabledStreamCopy":
                        if (n.InnerText == "True") { RadioButtonStreamCopySubtitle.IsChecked = true; } else { RadioButtonStreamCopySubtitle.IsChecked = false; }
                        break;
                    case "SubtitleEnabledCustom":
                        if (n.InnerText == "True") { RadioButtonCustomSubtitles.IsChecked = true; } else { RadioButtonCustomSubtitles.IsChecked = false; }
                        break;
                    case "CalculateChunkLengthAutomaticly":
                        if (n.InnerText == "True") { CheckBoxAutomaticChunkLength.IsChecked = true; } else { CheckBoxAutomaticChunkLength.IsChecked = false; }
                        break;
                    case "PlayFinishedSound":
                        if (n.InnerText == "True") { CheckBoxEnableFinishedSound.IsChecked = true; } else { CheckBoxEnableFinishedSound.IsChecked = false; }
                        break;
                    case "ProcessPriority":
                        if (n.InnerText == "Below Normal") { ComboBoxProcessPrio.SelectedIndex = 1; } else { ComboBoxProcessPrio.SelectedIndex = 0; }
                        break;
                    case "ChromaSubsampling":
                        if (n.InnerText == "4:2:0") { ComboBoxChroma.SelectedIndex = 0; }
                        if (n.InnerText == "4:2:2") { ComboBoxChroma.SelectedIndex = 1; }
                        if (n.InnerText == "4:4:4") { ComboBoxChroma.SelectedIndex = 2; }
                        break;
                    case "ColorTransfer":
                        if (n.InnerText == "BT.709") { ComboBoxColorTrans.SelectedIndex = 0; }
                        if (n.InnerText == "BT.2020-10") { ComboBoxColorTrans.SelectedIndex = 1; }
                        if (n.InnerText == "BT.2020-12") { ComboBoxColorTrans.SelectedIndex = 2; }
                        if (n.InnerText == "SMPTE2084") { ComboBoxColorTrans.SelectedIndex = 3; }
                        break;
                    case "ColorPrimaries":
                        if (n.InnerText == "BT.709") { ComboBoxColorPrim.SelectedIndex = 0; }
                        if (n.InnerText == "BT.2020") { ComboBoxColorPrim.SelectedIndex = 1; }
                        break;
                    case "ColorSpace":
                        if (n.InnerText == "BT.709") { ComboBoxColorSpace.SelectedIndex = 0; }
                        if (n.InnerText == "BT.2020NC") { ComboBoxColorSpace.SelectedIndex = 1; }
                        if (n.InnerText == "BT.2020C") { ComboBoxColorSpace.SelectedIndex = 2; }
                        if (n.InnerText == "SMPTE2085") { ComboBoxColorSpace.SelectedIndex = 3; }
                        break;
                    default:
                        break;
                }

                if (saveJob == true || saveQueue == true)
                {
                    if (n.Name == "VideoInput") { TextBoxVideoInput.Text = n.InnerText; }
                    if (n.Name == "VideoOutput") { TextBoxVideoOutput.Text = n.InnerText; }
                }
                //------------------------------------------------------------------------------------------------------------------||
            }
        }

        public void SaveSettings(string profileName, bool saveJob, bool saveQueue, string saveQueueName)
        {
            SmallScripts.Logging("Landed in SaveSettings() function.");
            string directory = "";
            //Saves Settings to XML File ---------------------------------------------------------------------------------------||
            if (saveJob == true)
            {
                directory = "unfinishedjob.xml";
            }
            else if (saveQueue == true)
            {
                directory = "Queue\\" + saveQueueName;
            }
            else
            {
                directory = currentDir + "\\Profiles\\" + profileName;
            }

            XmlWriter writer = XmlWriter.Create(directory);

            writer.WriteStartElement("Settings");
            writer.WriteElementString("Logging", CheckBoxLogging.IsChecked.ToString());
            writer.WriteElementString("ChunkLength", TextBoxChunkLength.Text);
            writer.WriteElementString("Reencode", CheckBoxReencode.IsChecked.ToString());
            writer.WriteElementString("Prereencode", CheckBoxPreReencode.IsChecked.ToString());
            writer.WriteElementString("Reencodecodec", ComboBoxReencodingMethod.Text);
            writer.WriteElementString("Prereencodecodec", ComboBoxPreReencodingMethod.Text);
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
            writer.WriteElementString("AdvancedSettingsAomKeyframeFiltering", CheckBoxAomKeyframefiltering.IsChecked.ToString());
            writer.WriteElementString("AdvancedSettingsAomAutoAltRef", CheckBoxAutoAltRef.IsChecked.ToString());
            writer.WriteElementString("AdvancedSettingsAomFrameboost", CheckBoxFrameBoost.IsChecked.ToString());
            writer.WriteElementString("ShutdownAfterEncode", CheckBoxShutdownAfterEncode.IsChecked.ToString());
            writer.WriteElementString("DeleteTempFiles", CheckBoxDeleteTempFiles.IsChecked.ToString());
            writer.WriteElementString("CustomFfmpegPathActive", CheckBoxCustomFfmpegPath.IsChecked.ToString());
            writer.WriteElementString("CustomFfmpegPath", TextBoxCustomFfmpegPath.Text);
            writer.WriteElementString("CustomFfprobePathActive", CheckBoxCustomFfprobePath.IsChecked.ToString());
            writer.WriteElementString("CustomFfprobePath", TextBoxCustomFfprobePath.Text);
            writer.WriteElementString("CustomRaviePathActive", CheckBoxCustomRaviePath.IsChecked.ToString());
            writer.WriteElementString("CustomRaviePath", TextBoxCustomRaviePath.Text);
            writer.WriteElementString("CustomSvtaviPathActive", CheckBoxCustomSVTPath.IsChecked.ToString());
            writer.WriteElementString("CustomSvtaviPath", TextBoxCustomSVTPath.Text);
            writer.WriteElementString("CustomAomencPathActive", CheckBoxCustomAomencPath.IsChecked.ToString());
            writer.WriteElementString("CustomAomencPath", TextBoxCustomAomencPath.Text);
            writer.WriteElementString("CustomTempPathActive", CheckBoxCustomTempFolder.IsChecked.ToString());
            writer.WriteElementString("CustomTempPath", TextBoxCustomTempFolder.Text);
            writer.WriteElementString("CustomBackground", customBackground.ToString());
            writer.WriteElementString("CustomBackgroundPath", PathToBackground);
            writer.WriteElementString("AudioEncoding", CheckBoxAudioEncoding.IsChecked.ToString());
            writer.WriteElementString("AudioCodec", ComboBoxAudioCodec.Text);
            writer.WriteElementString("AudioCodecTrackTwo", ComboBoxAudioCodecTrackTwo.Text);
            writer.WriteElementString("AudioCodecTrackThree", ComboBoxAudioCodecTrackThree.Text);
            writer.WriteElementString("AudioCodecTrackFour", ComboBoxAudioCodecTrackFour.Text);
            writer.WriteElementString("AudioBitrate", TextBoxAudioBitrate.Text);
            writer.WriteElementString("AudioBitrateTrackTwo", TextBoxAudioBitrateTrackTwo.Text);
            writer.WriteElementString("AudioBitrateTrackThree", TextBoxAudioBitrateTrackThree.Text);
            writer.WriteElementString("AudioBitrateTrackFour", TextBoxAudioBitrateTrackFour.Text);
            writer.WriteElementString("AudioTrackOne", CheckBoxAudioTrackOne.IsChecked.ToString());
            writer.WriteElementString("AudioTrackTwo", CheckBoxAudioTrackTwo.IsChecked.ToString());
            writer.WriteElementString("AudioTrackThree", CheckBoxAudioTrackThree.IsChecked.ToString());
            writer.WriteElementString("AudioTrackFour", CheckBoxAudioTrackFour.IsChecked.ToString());
            writer.WriteElementString("AudioChannelTrackOne", ComboBoxTrackOneChannels.Text);
            writer.WriteElementString("AudioChannelTrackTwo", ComboBoxTrackTwoChannels.Text);
            writer.WriteElementString("AudioChannelTrackThree", ComboBoxTrackThreeChannels.Text);
            writer.WriteElementString("AudioChannelTrackFour", ComboBoxTrackFourChannels.Text);
            writer.WriteElementString("VideoResize", CheckBoxVideoResize.IsChecked.ToString());
            writer.WriteElementString("ResizeFrameHeight", TextBoxFrameHeight.Text);
            writer.WriteElementString("ResizeFrameWidth", TextBoxFrameWidth.Text);
            writer.WriteElementString("SubtitleEnabled", CheckBoxEnableSubtitles.IsChecked.ToString());
            writer.WriteElementString("SubtitleEnabledStreamCopy", RadioButtonStreamCopySubtitle.IsChecked.ToString());
            writer.WriteElementString("SubtitleEnabledCustom", RadioButtonCustomSubtitles.IsChecked.ToString());
            writer.WriteElementString("CalculateChunkLengthAutomaticly", CheckBoxAutomaticChunkLength.IsChecked.ToString());
            writer.WriteElementString("PlayFinishedSound", CheckBoxEnableFinishedSound.IsChecked.ToString());
            writer.WriteElementString("ChromaSubsampling", ComboBoxChroma.Text);
            writer.WriteElementString("ColorTransfer", ComboBoxColorTrans.Text);
            writer.WriteElementString("ColorPrimaries", ComboBoxColorPrim.Text);
            writer.WriteElementString("ColorSpace", ComboBoxColorSpace.Text);
            writer.WriteElementString("ProcessPriority", ComboBoxProcessPrio.Text);

            if (saveJob == true || saveQueue == true)
            {
                writer.WriteElementString("VideoInput", TextBoxVideoInput.Text);
                writer.WriteElementString("VideoOutput", TextBoxVideoOutput.Text);
            }
            writer.WriteEndElement();
            writer.Close();
            //------------------------------------------------------------------------------------------------------------------||
        }

        private void LoadProfiles()
        {
            SmallScripts.Logging("Landed in LoadProfiles() function.");
            try
            {
                DirectoryInfo profiles = new DirectoryInfo(currentDir + "\\Profiles");
                FileInfo[] Files = profiles.GetFiles("*.xml"); //Getting XML
                ComboBoxProfiles.ItemsSource = Files;
                foreach (FileInfo item in Files){
                    SmallScripts.Logging("Found Profile: " + item.ToString());
                }
                
            }
            catch { }
        }

        private void LoadProfileStartup()
        {
            SmallScripts.Logging("Landed in LoadProfileStartup() function.");
            try
            {
                //This function loads the default Profile if it exists
                bool fileExist = File.Exists("Profiles\\Default\\default.xml");
                SmallScripts.Logging("Found default Profile: " + fileExist.ToString());
                if (fileExist)
                {
                    XmlDocument doc = new XmlDocument();

                    string directory = currentDir + "\\Profiles\\Default\\default.xml";

                    doc.Load(directory);
                    XmlNodeList node = doc.GetElementsByTagName("Settings");
                    foreach (XmlNode n in node[0].ChildNodes)
                    {
                        if (n.Name == "DefaultProfile")
                        {
                            LoadSettings(n.InnerText, false, false, "");
                            SmallScripts.Logging("Default Profile: " + n.InnerText);
                        }
                    }
                }
            }
            catch { }
        }

        //-------------------------------------------------------------------------------------------------||

        //----------------------------------------- Buttons -----------------------------------------------||
        //------------------------------------ Binaries Buttons -------------------------------------------||

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
                CheckFfprobe();
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

        private void ButtonCustomSVTPath_Click(object sender, RoutedEventArgs e)
        {
            //Sets the aomenc folder
            System.Windows.Forms.FolderBrowserDialog browseSVTFolder = new System.Windows.Forms.FolderBrowserDialog();

            if (browseSVTFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TextBoxCustomSVTPath.Text = browseSVTFolder.SelectedPath;

                bool SVTExist = File.Exists(TextBoxCustomSVTPath.Text + "\\SvtAv1EncApp.exe");

                if (SVTExist == false)
                {
                    MessageBox.Show("Couldn't find svt-av1 in that folder!", "Attention!", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void ButtonCustomRaviePath_Click(object sender, RoutedEventArgs e)
        {
            //Sets the ffprobe folder
            System.Windows.Forms.FolderBrowserDialog browseRavieFolder = new System.Windows.Forms.FolderBrowserDialog();

            if (browseRavieFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TextBoxCustomRaviePath.Text = browseRavieFolder.SelectedPath;

                bool FfprobeExist = File.Exists(TextBoxCustomRaviePath.Text + "\\rav1e.exe");

                if (FfprobeExist == false)
                {
                    MessageBox.Show("Couldn't find rav1e in that folder!", "Attention!", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        //------------------------------------ Profile Buttons -------------------------------------------||

        private void ButtonLoadProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadSettings(ComboBoxProfiles.Text, false, false, "");
            }
            catch { }
        }

        private void ButtonSaveProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SmallScripts.CreateDirectory(currentDir, "Profiles");
                SaveSettings(TextBoxProfiles.Text, false, false, "");
                LoadProfiles();
            }
            catch { }
        }

        private void ButtonProfilesRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadProfiles();
            }
            catch { }
        }

        //------------------------------------ Start Stop Buttons ----------------------------------------||

        private async void ButtonStartEncode_Click(object sender, RoutedEventArgs e)
        {
            ButtonStartEncode.BorderBrush = Brushes.Green;
            if (CheckBoxBatchEncoding.IsChecked == false)
            {
                if (CheckBoxQueueMode.IsChecked == true)
                {
                    SmallScripts.Logging("Selected Queue Encoding");
                    QueueEncode();
                } else
                {
                    SmallScripts.Logging("Selected Single File Encoding");
                    //Main entry Point
                    SmallScripts.Cancel.CancelAll = false;
                    ResetProgressBar();
                    CheckResume();
                    SetParametersBeforeEncode();
                    SetAudioParameters();
                    if (inputSet == false)
                    {
                        MessageBox.Show("Input Path not specified!");
                        SmallScripts.Logging("Input Path not specified!");
                    }
                    else if (outputSet == false)
                    {
                        MessageBox.Show("Output Path not specified!");
                        SmallScripts.Logging("Output Path not specified!");
                    }
                    else
                    {
                        if (ComboBoxEncoder.Text == "aomenc")
                        {
                            SmallScripts.Logging("Set Aomenc Parameters");
                            SetAomencParameters();
                        }
                        else if (ComboBoxEncoder.Text == "RAV1E")
                        {
                            SmallScripts.Logging("Set RAV1E Parameters");
                            SetRavieParameters();
                        }
                        else if (ComboBoxEncoder.Text == "SVT-AV1")
                        {
                            SmallScripts.Logging("Set SVT-AV1 Parameters");
                            SetSVTAV1Parameters();
                        }
                        else if (ComboBoxEncoder.Text == "libaom")
                        {
                            SmallScripts.Logging("Set libaom Parameters");
                            SetLibAomParameters();
                        }
                        if (SmallScripts.Cancel.CancelAll == false)
                        {
                            SmallScripts.Logging("Start AsyncClass()");
                            await AsyncClass();
                            ButtonStartEncode.BorderBrush = Brushes.White;
                        }
                    }
                }


            }
            else if (CheckBoxBatchEncoding.IsChecked == true)
            {
                SmallScripts.Logging("Selected Batch Encoding");
                BatchEncode();
            }
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            SmallScripts.Logging("Pressed Cancel Button!");
            SmallScripts.Cancel.CancelAll = true;
            SmallScripts.KillInstances();
            ButtonStartEncode.BorderBrush = Brushes.Red;
        }

        //---------------------------------- Input / Output Buttons --------------------------------------||

        private void ButtonSaveEncodeTo_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBoxBatchEncoding.IsChecked == false)
            {
                //Open the OpenFileDialog to set the Videooutput
                SaveFileDialog saveVideoFileDialog = new SaveFileDialog();
                saveVideoFileDialog.Filter = "Matroska|*.mkv|WebM|*.webm|MP4|*.mp4";

                Nullable<bool> result = saveVideoFileDialog.ShowDialog();

                if (result == true)
                {
                    TextBoxVideoOutput.Text = saveVideoFileDialog.FileName;
                    videoOutput = saveVideoFileDialog.FileName;
                    outputSet = true;
                }
            }
            else if (CheckBoxBatchEncoding.IsChecked == true)
            {
                System.Windows.Forms.FolderBrowserDialog browseOutputFolder = new System.Windows.Forms.FolderBrowserDialog();

                if (browseOutputFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    TextBoxVideoOutput.Text = browseOutputFolder.SelectedPath;
                    outputSet = true;
                }
            }
        }

        private void ButtonOpenSource_Click(object sender, RoutedEventArgs e)
        {

            detectedTrackOne = false;
            detectedTrackTwo = false;
            detectedTrackThree = false;
            detectedTrackFour = false;

            if (CheckBoxBatchEncoding.IsChecked == false)
            {
                CheckDependencies();
                //Open the OpenFileDialog to set the Videoinput
                OpenFileDialog openVideoFileDialog = new OpenFileDialog();

                Nullable<bool> result = openVideoFileDialog.ShowDialog();

                if (result == true)
                {
                    CheckFfprobe();
                    TextBoxVideoInput.Text = openVideoFileDialog.FileName;
                    GetStreamFps(TextBoxVideoInput.Text);
                    CheckAudioTracks(TextBoxVideoInput.Text);
                    SmallScripts.GetStreamLength(TextBoxVideoInput.Text);
                    inputSet = true;
                    if (CheckBoxAutomaticChunkLength.IsChecked == true)
                    {
                        //Sets the Chunk Length automaticly
                        TextBoxChunkLength.Text = (Int16.Parse(streamLength) / Int16.Parse(TextBoxNumberOfWorkers.Text)).ToString();
                    }
                }
            }
            else if (CheckBoxBatchEncoding.IsChecked == true)
            {
                System.Windows.Forms.FolderBrowserDialog browseInputFolder = new System.Windows.Forms.FolderBrowserDialog();

                if (browseInputFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    inputSet = true;
                    TextBoxVideoInput.Text = browseInputFolder.SelectedPath;
                }
            }
        }

        //-------------------------------------- Settings Buttons ----------------------------------------||

        private void ComboBoxEncoder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Sets the Maximum Quality Values, which are Encoder dependant
            string comboitem = (e.AddedItems[0] as ComboBoxItem).Content as string;

            if (comboitem == "aomenc")
            {
                if (SliderQuality != null)
                {
                    SliderQuality.Maximum = 63;
                    SliderQuality.Value = 30;
                    SliderPreset.Maximum = 8;
                    SliderPreset.Value = 3;
                    CheckBoxCBR.IsEnabled = true;
                    ComboBoxAqMode.IsEnabled = true;
                    CheckBoxTwoPass.IsEnabled = true;
                }
            }
            else if (comboitem == "RAV1E")
            {
                SliderQuality.Maximum = 255;
                SliderQuality.Value = 100;
                SliderPreset.Maximum = 10;
                SliderPreset.Value = 6;
                CheckBoxCBR.IsEnabled = false;
                ComboBoxAqMode.IsEnabled = false;
                CheckBoxTwoPass.IsEnabled = false; //2-Pass completly broken in rav1e
            }
            else if (comboitem == "SVT-AV1")
            {
                SliderQuality.Maximum = 63;
                SliderQuality.Value = 40;
                SliderPreset.Value = 5;
                SliderPreset.Maximum = 8;
                CheckBoxCBR.IsEnabled = true;
                ComboBoxAqMode.IsEnabled = true;
                CheckBoxTwoPass.IsEnabled = true;
            }
        }

        private void RadioButtonBitrate_Checked(object sender, RoutedEventArgs e)
        {
            RadioButtonConstantQuality.IsChecked = false;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //Changes the Background
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                if (openFileDialog.ShowDialog() == true)
                {
                    Uri fileUri = new Uri(openFileDialog.FileName);
                    imgDynamic.Source = new BitmapImage(fileUri);
                    customBackground = true;
                    PathToBackground = openFileDialog.FileName;
                }
            }
            catch { }
        }

        private void ButtonSetProfileDefault_Click(object sender, RoutedEventArgs e)
        {
            //Saves the default Profile to default.xml
            try
            {
                SmallScripts.CreateDirectory(currentDir, "Profiles\\Default");
                string directory = currentDir + "\\Profiles\\Default\\default.xml";
                XmlWriter writer = XmlWriter.Create(directory);
                writer.WriteStartElement("Settings");
                writer.WriteElementString("DefaultProfile", ComboBoxProfiles.Text);
                writer.WriteEndElement();
                writer.Close();
            }
            catch { }
        }

        private void ButtonAddCustomSubtitle_Click(object sender, RoutedEventArgs e)
        {
            //Open the OpenFileDialog to set the Subtitle Input
            OpenFileDialog openVideoFileDialog = new OpenFileDialog();
            Nullable<bool> result = openVideoFileDialog.ShowDialog();

            if (result == true)
            {
                ListBoxSubtitles.Items.Add(openVideoFileDialog.FileName);
                customsubtitleadded += 1;
            }
        }

        private void ButtonDeleteSubtitle_Click(object sender, RoutedEventArgs e)
        {
            try {
                ListBoxSubtitles.Items.RemoveAt(ListBoxSubtitles.SelectedIndex);
                customsubtitleadded -= 1;
            }
            catch { }
        }

        private void ButtonAddQueue_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SmallScripts.CreateDirectory(currentDir, "Queue");
                SaveSettings("", false, true, TextBoxQueueName.Text);
                ListBoxQueue.Items.Add(TextBoxQueueName.Text);
            }
            catch { }

        }

        private void ButtonRemoveFromQueue_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                File.Delete("Queue\\" + ListBoxQueue.SelectedItem);
                ListBoxQueue.Items.RemoveAt(ListBoxQueue.SelectedIndex);
            }
            catch { }

        }

        private void TextBoxNumberOfWorkers_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (CheckBoxAutomaticChunkLength != null)
                {
                    if (CheckBoxAutomaticChunkLength.IsChecked == true)
                    //Sets the Chunk Length automaticly
                    TextBoxChunkLength.Text = (Int16.Parse(streamLength) / Int16.Parse(TextBoxNumberOfWorkers.Text)).ToString();
                }
            }
            catch { }
        }

        private void CheckBoxCustomCommandLine_Checked(object sender, RoutedEventArgs e)
        {
            if (ComboBoxEncoder.Text == "aomenc")
            {
                if (RadioButtonConstantQuality.IsChecked == true)
                {
                    TextBoxCustomCommand.Text = "--threads=" + TextBoxThreads.Text + " --bit-depth=" + ComboBoxBitDepth.Text + " --end-usage=q --cq-level=" + SliderQuality.Value + " --kf-max-dist=" + TextBoxKeyframeInterval.Text + " --tile-columns=" + TextBoxTileColumns.Text + " --tile-rows=" + TextBoxTileRows.Text + " --aq-mode=" + ComboBoxAqMode.Text;
                }
                else
                {
                    if (CheckBoxCBR.IsChecked == true)
                    {
                        TextBoxCustomCommand.Text = "--threads=" + TextBoxThreads.Text + " --bit-depth=" + ComboBoxBitDepth.Text + " --end-usage=cbr --target-bitrate=" + TextBoxBitrate.Text + " --kf-max-dist=" + TextBoxKeyframeInterval.Text + " --tile-columns=" + TextBoxTileColumns.Text + " --tile-rows=" + TextBoxTileRows.Text + " --aq-mode=" + ComboBoxAqMode.Text;
                    }
                    else
                    {
                        TextBoxCustomCommand.Text = "--threads=" + TextBoxThreads.Text + " --bit-depth=" + ComboBoxBitDepth.Text + " --end-usage=vbr --target-bitrate=" + TextBoxBitrate.Text + " --kf-max-dist=" + TextBoxKeyframeInterval.Text + " --tile-columns=" + TextBoxTileColumns.Text + " --tile-rows=" + TextBoxTileRows.Text + " --aq-mode=" + ComboBoxAqMode.Text;
                    }

                }

            }
            else if (ComboBoxEncoder.Text == "RAV1E")
            {
                if (RadioButtonConstantQuality.IsChecked == true)
                {
                   

                    TextBoxCustomCommand.Text = " --threads " + TextBoxThreads.Text + " --quantizer " + SliderQuality.Value + " --keyint " + TextBoxKeyframeInterval.Text + " --tile-cols " + TextBoxTileColumns.Text + " --tile-rows " + TextBoxTileRows.Text;
                }
                else
                {

                    TextBoxCustomCommand.Text = " --threads " + TextBoxThreads.Text + " --bitrate " + TextBoxBitrate.Text + " --keyint " + TextBoxKeyframeInterval.Text + " --tile-cols " + TextBoxTileColumns.Text + " --tile-rows " + TextBoxTileRows.Text;


                }
            }


        }
        //-------------------------------------------------------------------------------------------------||
    }
}