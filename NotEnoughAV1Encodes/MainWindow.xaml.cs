using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Xml;

namespace NotEnoughAV1Encodes
{
    public partial class MainWindow : Window
    {
        public static string ffprobePath, ffmpegPath, aomencPath, rav1ePath, svtav1Path;
        public static string videoInput, videoOutput, encoder, fileName, videoResize, pipeBitDepth = "yuv420p", reencoder;
        public static string audioCodecTrackOne, audioCodecTrackTwo, audioCodecTrackThree, audioCodecTrackFour;
        public static string allSettingsAom, allSettingsRav1e, allSettingsSVTAV1;
        public static string tempPath = ""; //Temp Path for Splitting and Encoding
        public static string[] videoChunks; //Temp Chunk List
        public static string PathToBackground;
        public static int videoChunksCount; //Number of Chunks, mainly only for Progressbar
        public static int coreCount, workerCount, chunkLength; //Variable to set the Worker Count
        public static int videoPasses, processPriority, videoLength;
        public static int audioBitrateTrackOne, audioBitrateTrackTwo, audioBitrateTrackThree, audioBitrateTrackFour;
        public static int audioChannelsTrackOne, audioChannelsTrackTwo, audioChannelsTrackThree, audioChannelsTrackFour;
        public static bool trackOne, trackTwo, trackThree, trackFour, audioEncoding;
        public static bool inputSet, outputSet, reencode, beforereencode, resumeMode, deleteTempFiles;
        public static bool customBackground;
        public static double videoFrameRate;
        public DateTime starttimea;

        public MainWindow()
        {
            InitializeComponent();
            getCoreCount();
            LoadPresetsIntoComboBox();
            LoadDefaultProfile();
            CheckForResumeFile();
            setEncoderPath();
            SmallFunctions.checkDependeciesStartup();
        }

        //════════════════════════════════════ Main Functions ═════════════════════════════════════

        private async void MainEntry()
        {
            encoder = ComboBoxEncoder.Text;
            if (SmallFunctions.checkDependencies(encoder) && SmallFunctions.Cancel.CancelAll == false)
            {
                setParameters();
                setAudioParameters();
                if (resumeMode == false) { saveResumeJob(); }
                if (resumeMode == true) { await AsyncClass(); } 
                else
                {
                    if (SmallFunctions.CheckFileFolder()) { await AsyncClass(); }
                    else
                    {
                        if (MessageBox.Show("Temp Chunks Folder not Empty! Overwrite existing Data?", "Error", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            SmallFunctions.DeleteChunkFolderContent();
                            await AsyncClass();
                        }
                        else { SmallFunctions.Cancel.CancelAll = true; CancelRoutine(); }
                    }
                }
            }
        }

        private async Task AsyncClass()
        {
            if (SmallFunctions.Cancel.CancelAll == false && resumeMode == false)
            {
                setProgressBarLabel("Started Audio Encoding");
                await Task.Run(() => EncodeAudio.AudioEncode());
                if (SmallFunctions.CheckAudioOutput() == false) { MessageNoAudioOutput(); }
                setProgressBarLabel("Started Video Splitting");
                await Task.Run(() => VideoSplitting.SplitVideo(videoInput, chunkLength, reencoder, reencode, beforereencode));
                await Task.Run(() => RenameChunks.Rename());
            }

            SmallFunctions.CountVideoChunks();
            setProgressBar(videoChunksCount);
            setProgressBarLabel("0 / " + videoChunksCount.ToString());
            setEncoderParameters(false);

            await Task.Run(() => Encode());

            if (SmallFunctions.Cancel.CancelAll == false)
            {
                await Task.Run(() => VideoMuxing.Concat());
                if (SmallFunctions.CheckVideoOutput())
                {
                    if (CheckBoxBatchEncoding.IsChecked == false)
                    {
                        LabelProgressbar.Dispatcher.Invoke(() => LabelProgressbar.Content = "Encoding completed! Elapsed Time: " + (DateTime.Now - starttimea).ToString("hh\\:mm\\:ss") + " - " + Math.Round(Convert.ToDecimal((((videoLength * videoFrameRate) / (DateTime.Now - starttimea).TotalSeconds))), 2).ToString() + "fps", DispatcherPriority.Background);
                        ButtonSaveVideo.IsEnabled = true;
                        ButtonOpenSource.IsEnabled = true;
                        ProgressBar.Foreground = System.Windows.Media.Brushes.Green;
                        ButtonStartEncode.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(228, 228, 228));
                    }
                    if (deleteTempFiles) { SmallFunctions.DeleteTempFiles(); }
                    if (CheckBoxFinishedSound.IsChecked == true && CheckBoxBatchEncoding.IsChecked == false) { SmallFunctions.PlayFinishedSound(); }
                    if (CheckBoxShutdownAfterEncode.IsChecked == true && CheckBoxBatchEncoding.IsChecked == false) { Process.Start("shutdown.exe", "/s /t 0"); }
                }
            }
        }

        private async void BatchEncode()
        {
            DirectoryInfo batchfiles = new DirectoryInfo(videoInput);
            foreach (var file in batchfiles.GetFiles())
            {
                SmallFunctions.Cancel.CancelAll = false;

                ProgressBar.Maximum = 100;
                ProgressBar.Value = 0;
                setProgressBarLabel("Encoding: " + file);

                videoInput = LabelVideoSource.Content + "\\" + file;
                videoOutput = LabelVideoOutput.Content + "\\" + file + "_av1.mkv";

                getVideoInformation();
                getAudioInformation();

                setChunkLength();
                setParameters();
                setAudioParameters();

                if (SmallFunctions.Cancel.CancelAll == false)
                {
                    await AsyncClass();
                }

            }
            ButtonOpenSource.IsEnabled = true;
            ProgressBar.Foreground = System.Windows.Media.Brushes.Green;
            ButtonStartEncode.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(228, 228, 228));
            if (CheckBoxFinishedSound.IsChecked == true) { SmallFunctions.PlayFinishedSound(); }
            if (CheckBoxShutdownAfterEncode.IsChecked == true) { Process.Start("shutdown.exe", "/s /t 0"); }
        }

        //═══════════════════════════════════════ Functions ═══════════════════════════════════════
   
        private void MessageNoAudioOutput()
        {
            if (MessageBox.Show("No Audio Output detected! \nCancel?", "Error", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes) { SmallFunctions.Cancel.CancelAll = true; CancelRoutine(); }
            else { audioEncoding = false; }
        }

        private void getCoreCount()
        {
            //Gets CoreCount of Hostmachine
            foreach (var item in new System.Management.ManagementObjectSearcher("Select * from Win32_Processor").Get()) { coreCount += int.Parse(item["NumberOfCores"].ToString()); }
            for (int i = 1; i <= coreCount; i++) { ComboBoxWorkers.Items.Add(i); }
            ComboBoxWorkers.SelectedItem = coreCount;
        }

        private void saveResumeJob()
        {
            SaveSettings(fileName, false, true, false);
        }

        private void setEncoderPath()
        {
            if (SmallFunctions.ExistsOnPath("aomenc.exe") && File.Exists("Apps\\Encoder\\aomenc.exe") == false) { aomencPath = SmallFunctions.GetFullPathWithOutName("aomenc.exe"); }
            else{ aomencPath = Path.Combine(Directory.GetCurrentDirectory(), "Apps\\Encoder\\aomenc.exe"); }
            if (SmallFunctions.ExistsOnPath("rav1e.exe") && File.Exists("Apps\\Encoder\\rav1e.exe") == false) { rav1ePath = SmallFunctions.GetFullPathWithOutName("rav1e.exe"); }
            else { rav1ePath = Path.Combine(Directory.GetCurrentDirectory(), "Apps\\Encoder\\rav1e.exe"); }
            if (SmallFunctions.ExistsOnPath("SvtAv1EncApp.exe") && File.Exists("Apps\\Encoder\\SvtAv1EncApp.exe") == false) { svtav1Path = SmallFunctions.GetFullPathWithOutName("SvtAv1EncApp.exe"); } 
            else { svtav1Path = Path.Combine(Directory.GetCurrentDirectory(), "Apps\\Encoder\\SvtAv1EncApp.exe"); }
            if (SmallFunctions.ExistsOnPath("ffmpeg.exe") && File.Exists("Apps\\ffmpeg\\ffmpeg.exe") == false) { ffmpegPath = SmallFunctions.GetFullPathWithOutName("ffmpeg.exe"); }
            else { ffmpegPath = Path.Combine(Directory.GetCurrentDirectory(), "Apps\\ffmpeg\\ffmpeg.exe"); }
            if (SmallFunctions.ExistsOnPath("ffprobe.exe") && File.Exists("Apps\\ffmpeg\\ffprobe.exe") == false) { ffprobePath = SmallFunctions.GetFullPathWithOutName("ffprobe.exe"); }
            else { ffprobePath = Path.Combine(Directory.GetCurrentDirectory(), "Apps\\ffmpeg\\ffprobe.exe"); }
        }

        private void setParameters()
        {
            tempPath = "Temp\\" + fileName + "\\";

            encoder = ComboBoxEncoder.Text;
            reencoder = ComboBoxReencodeCodec.Text;
            processPriority = ComboBoxProcessPriority.SelectedIndex;

            resumeMode = CheckBoxResumeMode.IsChecked == true ? true : false;
            deleteTempFiles = CheckBoxDeleteTempFiles.IsChecked == true ? true : false;
            reencode = CheckBoxReencodeDuringSplitting.IsChecked == true ? true : false;
            beforereencode = CheckBoxReencodeBeforeSplitting.IsChecked == true ? true : false;

            videoPasses = Int16.Parse(ComboBoxPasses.Text);
            workerCount = Int16.Parse(ComboBoxWorkers.Text);
            chunkLength = Int16.Parse(TextBoxChunkLength.Text);
            videoLength = Int16.Parse(SmallFunctions.getVideoLength(videoInput));
            
            if (CheckBoxResize.IsChecked == true) { videoResize = "-vf scale=" + TextBoxImageWidth.Text + ":" + TextBoxImageHeight.Text; } else { videoResize = ""; }
            if (CheckBoxCustomTempPath.IsChecked == true) { tempPath = Path.Combine(TextBoxCustomTempPath.Text, tempPath); } else { tempPath = Path.Combine(Directory.GetCurrentDirectory(), tempPath); }
            
            SmallFunctions.checkCreateFolder(tempPath);
        }

        private void setChunkLength()
        {
            if (CheckBoxChunkLengthAutoCalculation.IsChecked == true) { TextBoxChunkLength.Text = (Int16.Parse(SmallFunctions.getVideoLength(videoInput)) / Int16.Parse(ComboBoxWorkers.Text)).ToString(); }
        }

        private void setAudioParameters()
        {
            trackOne = CheckBoxAudioTrackOne.IsChecked == true;
            trackTwo = CheckBoxAudioTrackTwo.IsChecked == true;
            trackThree = CheckBoxAudioTrackThree.IsChecked == true;
            trackFour = CheckBoxAudioTrackFour.IsChecked == true;
            audioEncoding = CheckBoxAudioEncoding.IsChecked == true;
            audioCodecTrackOne = ComboBoxAudioCodec.Text;
            audioCodecTrackTwo = ComboBoxAudioCodecTrackTwo.Text;
            audioCodecTrackThree = ComboBoxAudioCodecTrackThree.Text;
            audioCodecTrackFour = ComboBoxAudioCodecTrackFour.Text;
            audioBitrateTrackOne = Int16.Parse(TextBoxAudioBitrate.Text);
            audioBitrateTrackTwo = Int16.Parse(TextBoxAudioBitrateTrackTwo.Text);
            audioBitrateTrackThree = Int16.Parse(TextBoxAudioBitrateTrackThree.Text);
            audioBitrateTrackFour = Int16.Parse(TextBoxAudioBitrateTrackFour.Text);
            switch (ComboBoxTrackOneChannels.SelectedIndex)
            {
                case 0: audioChannelsTrackOne = 1; break;
                case 1: audioChannelsTrackOne = 2; break;
                case 2: audioChannelsTrackOne = 6; break;
                case 3: audioChannelsTrackOne = 8; break;
                default: break;
            }
            switch (ComboBoxTrackTwoChannels.SelectedIndex)
            {
                case 0: audioChannelsTrackTwo = 1; break;
                case 1: audioChannelsTrackTwo = 2; break;
                case 2: audioChannelsTrackTwo = 6; break;
                case 3: audioChannelsTrackTwo = 8; break;
                default: break;
            }
            switch (ComboBoxTrackThreeChannels.SelectedIndex)
            {
                case 0: audioChannelsTrackThree = 1; break;
                case 1: audioChannelsTrackThree = 2; break;
                case 2: audioChannelsTrackThree = 6; break;
                case 3: audioChannelsTrackThree = 8; break;
                default: break;
            }
            switch (ComboBoxTrackFourChannels.SelectedIndex)
            {
                case 0: audioChannelsTrackFour = 1; break;
                case 1: audioChannelsTrackFour = 2; break;
                case 2: audioChannelsTrackFour = 6; break;
                case 3: audioChannelsTrackFour = 8; break;
                default: break;
            }
        }

        private void setEncoderParameters(bool tempSettings)
        {
            switch (encoder)
            {
                case "aomenc": SetAomencParameters(tempSettings); break;
                case "rav1e": SetRav1eParameters(tempSettings); break;
                case "aomenc (ffmpeg)": SetLibaomParameters(tempSettings); break;
                case "svt-av1": SetSVTAV1Parameters(tempSettings); break;
                default: break;
            }
        }

        private void SetAomencParameters(bool tempSettings)
        {
            string aomencQualityMode = "";
            switch (ComboBoxChromaSubsamplingAomenc.SelectedIndex)
            {
                case 0:
                    if (ComboBoxBitDepth.SelectedIndex == 1) { pipeBitDepth = "yuv420p10le"; }
                    if (ComboBoxBitDepth.SelectedIndex == 2) { pipeBitDepth = "yuv420p12le"; }
                    break;
                case 1:
                    if (ComboBoxBitDepth.SelectedIndex == 0) { pipeBitDepth = "yuv422p -strict -1"; }
                    if (ComboBoxBitDepth.SelectedIndex == 1) { pipeBitDepth = "yuv422p10le -strict -1"; }
                    if (ComboBoxBitDepth.SelectedIndex == 2) { pipeBitDepth = "yuv422p12le -strict -1"; }
                    break;
                case 2:
                    if (ComboBoxBitDepth.SelectedIndex == 0) { pipeBitDepth = "yuv444p -strict -1"; }
                    if (ComboBoxBitDepth.SelectedIndex == 1) { pipeBitDepth = "yuv444p10le -strict -1"; }
                    if (ComboBoxBitDepth.SelectedIndex == 2) { pipeBitDepth = "yuv444p12le -strict -1"; }
                    break;
                default: break;
            }
            if (RadioButtonConstantQuality.IsChecked == true) { aomencQualityMode = "--end-usage=q --cq-level=" + SliderQuality.Value; }
            else { aomencQualityMode = "--end-usage=vbr --target-bitrate=" + TextBoxBitrate.Text; }
            //Basic Settings
            if (CheckBoxAdvancedSettings.IsChecked == false)
            {
                allSettingsAom = "--cpu-used=" + SliderPreset.Value + " --bit-depth=" + ComboBoxBitDepth.Text + " --threads=2 --kf-max-dist=240 " + aomencQualityMode;
            }
            else
            {
                if (CheckBoxCustomSettings.IsChecked == false || tempSettings)
                {
                    string rowmt = (CheckBoxRowmt.IsChecked == false) ? "0" : "1";
                    string keyfiltering = (CheckBoxKeyframeFilteringAomenc.IsChecked == false) ? "0" : "1";
                    string autoAltRef = (CheckBoxAutoAltRefAomenc.IsChecked == false) ? "0" : "1";
                    string frameBoost = (CheckBoxFrameBoostAomenc.IsChecked == false) ? "0" : "1";
                    string aomencFrames = " --tile-columns=" + ComboBoxTileColumns.Text + " --tile-rows=" + ComboBoxTileRows.Text + " --kf-min-dist=" + TextBoxMinKeyframeinterval.Text + " --kf-max-dist=" + TextBoxMaxKeyframeinterval.Text + " --lag-in-frames=" + TextBoxMaxLagInFrames.Text + " --max-reference-frames=" + ComboBoxMaxReferenceFramesAomenc.Text;
                    string aomencColor = " --color-primaries=" + ComboBoxColorPrimariesAomenc.Text + " --transfer-characteristics=" + ComboBoxColorTransferAomenc.Text + " --matrix-coefficients=" + ComboBoxColorMatrixAomenc.Text + " --" + ComboBoxChromaSubsamplingAomenc.Text + " ";
                    string aomencOther = " --tune=" + ComboBoxTuneAomenc.Text + " --sharpness=" + ComboBoxSharpnessFilterAomenc.Text + " --row-mt=" + rowmt + " --enable-keyframe-filtering=" + keyfiltering + " --aq-mode=" + ComboBoxAQMode.SelectedIndex + " --auto-alt-ref=" + autoAltRef + " --frame-boost=" + frameBoost + " ";
                    allSettingsAom = "--cpu-used=" + SliderPreset.Value + " --bit-depth=" + ComboBoxBitDepth.Text + " --threads=" + ComboBoxThreadsAomenc.Text + aomencFrames + aomencColor + aomencOther + aomencQualityMode;
                }
                else
                {
                    allSettingsAom = TextBoxAdvancedSettings.Text;
                }

            }
        }

        private void SetLibaomParameters(bool tempSettings)
        {
            string aomencQualityMode = "";
            switch (ComboBoxColorFormatLibaom.SelectedIndex)
            {
                case 0:
                    if (ComboBoxBitDepth.SelectedIndex == 1) { pipeBitDepth = "yuv420p10le"; }
                    if (ComboBoxBitDepth.SelectedIndex == 2) { pipeBitDepth = "yuv420p12le"; }
                    break;
                case 1:
                    if (ComboBoxBitDepth.SelectedIndex == 0) { pipeBitDepth = "yuv422p -strict -1"; }
                    if (ComboBoxBitDepth.SelectedIndex == 1) { pipeBitDepth = "yuv422p10le -strict -1"; }
                    if (ComboBoxBitDepth.SelectedIndex == 2) { pipeBitDepth = "yuv422p12le -strict -1"; }
                    break;
                case 2:
                    if (ComboBoxBitDepth.SelectedIndex == 0) { pipeBitDepth = "yuv444p -strict -1"; }
                    if (ComboBoxBitDepth.SelectedIndex == 1) { pipeBitDepth = "yuv444p10le -strict -1"; }
                    if (ComboBoxBitDepth.SelectedIndex == 2) { pipeBitDepth = "yuv444p12le -strict -1"; }
                    break;
                default: break;
            }
            if (ComboBoxBitDepth.SelectedIndex == 1) { pipeBitDepth = "yuv420p10le"; } else if (ComboBoxBitDepth.SelectedIndex == 2) { pipeBitDepth = "yuv420p12le"; }
            if (RadioButtonConstantQuality.IsChecked == true) { aomencQualityMode = " -crf " + SliderQuality.Value + " -b:v 0"; }
            else { aomencQualityMode = " -b:v " + TextBoxBitrate.Text + "k"; }
            //Basic Settings
            if (CheckBoxAdvancedSettings.IsChecked == false)
            {
                allSettingsAom = "-cpu-used " + SliderPreset.Value + " -threads 2 -g 240 -tile-columns 1 -tile-rows 1 " + aomencQualityMode;
            }
            else
            {
                if (CheckBoxCustomSettings.IsChecked == false || tempSettings)
                {
                    string aomencFrames = " -tile-columns " + ComboBoxTileColumns.Text + " -tile-rows " + ComboBoxTileRows.Text + " -g " + TextBoxMaxKeyframeinterval.Text;
                    allSettingsAom = "-cpu-used " + SliderPreset.Value + " -threads " + ComboBoxThreadsAomenc.Text + aomencFrames + aomencQualityMode;
                }
                else
                {
                    allSettingsAom = TextBoxAdvancedSettings.Text;
                }

            }
            
        }

        private void SetRav1eParameters(bool tempSettings)
        {
            string rav1eQualityMode = "";
            string rav1eContentLight = "";
            string rav1eMasteringDisplay = "";
            switch (ComboBoxColorFormatRav1e.SelectedIndex)
            {
                case 0:
                    if (ComboBoxBitDepth.SelectedIndex == 1) { pipeBitDepth = "yuv420p10le"; }
                    if (ComboBoxBitDepth.SelectedIndex == 2) { pipeBitDepth = "yuv420p12le"; }
                    break;
                case 1:
                    if (ComboBoxBitDepth.SelectedIndex == 0) { pipeBitDepth = "yuv422p -strict -1"; }
                    if (ComboBoxBitDepth.SelectedIndex == 1) { pipeBitDepth = "yuv422p10le -strict -1"; }
                    if (ComboBoxBitDepth.SelectedIndex == 2) { pipeBitDepth = "yuv422p12le -strict -1"; }
                    break;
                case 2:
                    if (ComboBoxBitDepth.SelectedIndex == 0) { pipeBitDepth = "yuv444p -strict -1"; }
                    if (ComboBoxBitDepth.SelectedIndex == 1) { pipeBitDepth = "yuv444p10le -strict -1"; }
                    if (ComboBoxBitDepth.SelectedIndex == 2) { pipeBitDepth = "yuv444p12le -strict -1"; }
                    break;
                default: break;
            }
            if (RadioButtonConstantQuality.IsChecked == true) { rav1eQualityMode = "--quantizer " + SliderQuality.Value; }
            else { rav1eQualityMode = "--bitrate " + TextBoxBitrate.Text; }
            //Basic Settings
            if (CheckBoxAdvancedSettings.IsChecked == false)
            {
                allSettingsRav1e = "--speed " + SliderPreset.Value + " --keyint 240 --tile-rows 1 --tile-cols 4 " + rav1eQualityMode;
            }
            else if(CheckBoxCustomSettings.IsChecked == false || tempSettings)
            {
                string rav1eColor = " --primaries " + ComboBoxColorPrimariesRav1e.Text + " --transfer " + ComboBoxColorTransferRav1e.Text + " --matrix " + ComboBoxColorMatrixRav1e.Text + " --range " + ComboBoxPixelRangeRav1e.Text;
                if (CheckBoxContentLightRav1e.IsChecked == true) { rav1eContentLight = " --content-light " + TextBoxContentLightCllRav1e.Text + "," + TextBoxContentLightFallRav1e.Text; }
                if (CheckBoxMasteringDisplayRav1e.IsChecked == true) { rav1eMasteringDisplay = " --mastering-display G(" + TextBoxMasteringGxRav1e.Text + "," + TextBoxMasteringGyRav1e.Text + ")B("+TextBoxMasteringBxRav1e.Text + "," + TextBoxMasteringByRav1e.Text+")R("+TextBoxMasteringRxRav1e.Text + "," + TextBoxMasteringRyRav1e.Text + ")WP(" + TextBoxMasteringWPxRav1e.Text + "," + TextBoxMasteringWPyRav1e.Text + ")L(" + TextBoxMasteringLmaxRav1e.Text + "," + TextBoxMasteringLminRav1e.Text + ")"; }
                allSettingsRav1e = "--speed " + SliderPreset.Value + " " + rav1eQualityMode + " --threads " + ComboBoxThreadsAomenc.Text + " --min-keyint " + TextBoxMinKeyframeinterval.Text + " --keyint " + TextBoxMaxKeyframeinterval.Text + " --tile-rows " + ComboBoxTileRows.Text + " --tile-cols " + ComboBoxTileColumns.Text + " --tune " + ComboBoxTuneRav1e.Text + " --rdo-lookahead-frames " + TextBoxRDOLookaheadRav1e.Text + rav1eColor + rav1eContentLight + rav1eMasteringDisplay;
            }
            else
            {
                allSettingsRav1e = TextBoxCustomTempPath.Text;
            }
            
        }

        private void SetSVTAV1Parameters(bool tempSettings)
        {
            string svtav1QualityMode;
            switch (ComboBoxColorFormatSVT.SelectedIndex)
            {
                case 0:
                    if (ComboBoxBitDepth.SelectedIndex == 1) { pipeBitDepth = "yuv420p10le"; }
                    if (ComboBoxBitDepth.SelectedIndex == 2) { pipeBitDepth = "yuv420p12le"; }
                    break;
                case 1:
                    if (ComboBoxBitDepth.SelectedIndex == 0) { pipeBitDepth = "yuv422p -strict -1"; }
                    if (ComboBoxBitDepth.SelectedIndex == 1) { pipeBitDepth = "yuv422p10le -strict -1"; }
                    if (ComboBoxBitDepth.SelectedIndex == 2) { pipeBitDepth = "yuv422p12le -strict -1"; }
                    break;
                case 2:
                    if (ComboBoxBitDepth.SelectedIndex == 0) { pipeBitDepth = "yuv444p -strict -1"; }
                    if (ComboBoxBitDepth.SelectedIndex == 1) { pipeBitDepth = "yuv444p10le -strict -1"; }
                    if (ComboBoxBitDepth.SelectedIndex == 2) { pipeBitDepth = "yuv444p12le -strict -1"; }
                    break;
                default: break;
            }
            if (RadioButtonConstantQuality.IsChecked == true) { svtav1QualityMode = " --rc 0 -q " + SliderQuality.Value; }
            else { svtav1QualityMode = " --rc 1 --tbr " + TextBoxBitrate.Text; }
            //Basic Settings
            if (CheckBoxAdvancedSettings.IsChecked == false)
            {
                allSettingsSVTAV1 = "--preset " + SliderPreset.Value + " --input-depth " + ComboBoxBitDepth.Text + svtav1QualityMode;
            }
            else if (CheckBoxCustomSettings.IsChecked == false || tempSettings)
            {
                string hdrSVT = "";
                if (CheckBoxEnableHDRSVT.IsChecked == true) { hdrSVT = " --enable-hdr"; }
                allSettingsSVTAV1 = "--preset " + SliderPreset.Value + " --input-depth " + ComboBoxBitDepth.Text + svtav1QualityMode + " --tile-rows " + ComboBoxTileRows.Text + " --tile-columns " + ComboBoxTileColumns.Text + " --color-format " + ComboBoxColorFormatSVT.Text + hdrSVT + " --adaptive-quantization " + ComboBoxAQModeSVT.SelectedIndex + " --keyint " + TextBoxkeyframeIntervalSVT.Text;
            }
            else
            {
                allSettingsSVTAV1 = TextBoxAdvancedSettings.Text;
            }
            
        }

        private void LoadDefaultProfile()
        {
            try
            {
                bool fileExist = File.Exists("Profiles\\Default\\default.xml");
                if (fileExist)
                {
                    XmlDocument doc = new XmlDocument();
                    string directory = "Profiles\\Default\\default.xml";
                    doc.Load(directory);
                    XmlNodeList node = doc.GetElementsByTagName("Settings");
                    foreach (XmlNode n in node[0].ChildNodes) { if (n.Name == "DefaultProfile") { ComboBoxPresets.Text = n.InnerText; } }  //ComboBox automaticly loads Settings on change
                }
            }
            catch { }
        }

        private void getVideoInformation()
        {
            string frameRate = SmallFunctions.getFrameRate(videoInput);
            setFrameRate(frameRate);
            string pixelFormat = SmallFunctions.getPixelFormat(videoInput);
            setPixelFormat(pixelFormat);
            setChunkLength();
            fileName = SmallFunctions.getFilename(videoInput);
        }

        private void setPixelFormat(string pixelFormat)
        {
            switch (pixelFormat)
            {
                case "yuv420p10le":
                    ComboBoxBitDepth.SelectedIndex = 1;
                    break;
                case "yuv420p12le":
                    ComboBoxBitDepth.SelectedIndex = 2;
                    break;
                case "yuv422p":
                    ComboBoxChromaSubsamplingAomenc.SelectedIndex = 1;
                    ComboBoxColorFormatRav1e.SelectedIndex = 1;
                    ComboBoxColorFormatSVT.SelectedIndex = 1;
                    ComboBoxColorFormatLibaom.SelectedIndex = 1;
                    break;
                case "yuv422p10le":
                    ComboBoxChromaSubsamplingAomenc.SelectedIndex = 1;
                    ComboBoxColorFormatRav1e.SelectedIndex = 1;
                    ComboBoxColorFormatSVT.SelectedIndex = 1;
                    ComboBoxColorFormatLibaom.SelectedIndex = 1;
                    ComboBoxBitDepth.SelectedIndex = 1;
                    break;
                case "yuv422p12le":
                    ComboBoxChromaSubsamplingAomenc.SelectedIndex = 1;
                    ComboBoxColorFormatRav1e.SelectedIndex = 1;
                    ComboBoxColorFormatSVT.SelectedIndex = 1;
                    ComboBoxColorFormatLibaom.SelectedIndex = 1;
                    ComboBoxBitDepth.SelectedIndex = 2;
                    break;
                case "yuv444p":
                    ComboBoxChromaSubsamplingAomenc.SelectedIndex = 2;
                    ComboBoxColorFormatRav1e.SelectedIndex = 2;
                    ComboBoxColorFormatSVT.SelectedIndex = 2;
                    ComboBoxColorFormatLibaom.SelectedIndex = 2;
                    break;
                case "yuv444p10le":
                    ComboBoxChromaSubsamplingAomenc.SelectedIndex = 2;
                    ComboBoxColorFormatRav1e.SelectedIndex = 2;
                    ComboBoxColorFormatSVT.SelectedIndex = 2;
                    ComboBoxColorFormatLibaom.SelectedIndex = 2;
                    ComboBoxBitDepth.SelectedIndex = 1;
                    break;
                case "yuv444p12le":
                    ComboBoxChromaSubsamplingAomenc.SelectedIndex = 2;
                    ComboBoxColorFormatRav1e.SelectedIndex = 2;
                    ComboBoxColorFormatSVT.SelectedIndex = 2;
                    ComboBoxColorFormatLibaom.SelectedIndex = 2;
                    ComboBoxBitDepth.SelectedIndex = 2;
                    break;
                default: break;
            }
        }

        private void setFrameRate(string frameRate)
        {
            //Sets the Combobox Framerate
            switch (frameRate)
            {
                case "5/1": ComboBoxFrameRate.SelectedIndex = 0; break;
                case "10/1": ComboBoxFrameRate.SelectedIndex = 1; break;
                case "12/1": ComboBoxFrameRate.SelectedIndex = 2; break;
                case "15/1": ComboBoxFrameRate.SelectedIndex = 3; break;
                case "20/1": ComboBoxFrameRate.SelectedIndex = 4; break;
                case "24000/1001": ComboBoxFrameRate.SelectedIndex = 5; break;
                case "24/1": ComboBoxFrameRate.SelectedIndex = 6; break;
                case "25/1": ComboBoxFrameRate.SelectedIndex = 7; break;
                case "30000/1001": ComboBoxFrameRate.SelectedIndex = 8; break;
                case "30/1": ComboBoxFrameRate.SelectedIndex = 9; break;
                case "48/1": ComboBoxFrameRate.SelectedIndex = 10; break;
                case "50/1": ComboBoxFrameRate.SelectedIndex = 11; break;
                case "60000/1001": ComboBoxFrameRate.SelectedIndex = 12; break;
                case "60/1": ComboBoxFrameRate.SelectedIndex = 13; break;
                default: MessageBoxes.MessageVideoBadFramerate(); break;
            }
            videoFrameRate = Convert.ToDouble(ComboBoxFrameRate.Text, CultureInfo.InvariantCulture);
        }

        private void getAudioInformation()
        {
            string input = '\u0022' + MainWindow.videoInput + '\u0022';
            Process getAudioIndexes = new Process();
            getAudioIndexes.StartInfo = new ProcessStartInfo()
            {
                UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, FileName = "cmd.exe", WorkingDirectory = MainWindow.ffprobePath,
                Arguments = "/C ffprobe.exe -i " + input + " -loglevel error -select_streams a -show_streams -show_entries stream=index:tags=:disposition= -of csv",
                RedirectStandardError = true, RedirectStandardOutput = true
            };
            getAudioIndexes.Start();
            //Reads the Console Output
            string audioIndexes = getAudioIndexes.StandardOutput.ReadToEnd();
            //Splits the Console Output
            string[] audioIndexesFixed = audioIndexes.Split(new string[] { " ", "stream," }, StringSplitOptions.RemoveEmptyEntries);
            int detectedTracks = 0;
            bool trackone = false, tracktwo = false, trackthree = false, trackfour = false;
            foreach (var item in audioIndexesFixed)
            {
                switch(detectedTracks)
                {
                    case 0: trackone = true; break;
                    case 1: tracktwo = true; break;
                    case 3: trackthree = true; break;
                    case 4: trackfour = true; break;
                    default: break;
                }
                detectedTracks += 1;
            }
            getAudioIndexes.WaitForExit();
            if (trackone == false) { CheckBoxAudioTrackOne.IsChecked = false; CheckBoxAudioTrackOne.IsEnabled = false; }
            if (tracktwo == false) { CheckBoxAudioTrackTwo.IsChecked = false; CheckBoxAudioTrackTwo.IsEnabled = false; }
            if (trackthree == false) { CheckBoxAudioTrackThree.IsChecked = false; CheckBoxAudioTrackThree.IsEnabled = false; }
            if (trackfour == false) { CheckBoxAudioTrackFour.IsChecked = false; CheckBoxAudioTrackFour.IsEnabled = false; }
            if (CheckBoxAudioTrackOne.IsEnabled == false && CheckBoxAudioTrackTwo.IsEnabled == false && CheckBoxAudioTrackThree.IsEnabled == false && CheckBoxAudioTrackFour.IsEnabled == false) { CheckBoxAudioEncoding.IsChecked = false; CheckBoxAudioEncoding.IsEnabled = false; }
        }

        private void setProgressBarLabel(string Text)
        {
            LabelProgressbar.Content = Text;
        }


        private void setProgressBar(int Value)
        {
            ProgressBar.Maximum = Value;
        }

        private void CheckForResumeFile()
        {
            if (File.Exists("unfinishedjob.xml")) { if (MessageBox.Show("Unfinished Job detected! Load unfinished Job?", "Resume", MessageBoxButton.YesNo) == MessageBoxResult.Yes) { LoadSettings("", false, true, false); CheckBoxResumeMode.IsChecked = true; } }
        }

        private void CancelRoutine()
        {
            ButtonCancelEncode.BorderBrush = System.Windows.Media.Brushes.Red;
            ButtonStartEncode.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(228, 228, 228));
            ButtonOpenSource.IsEnabled = true;
            ButtonSaveVideo.IsEnabled = true;
            ProgressBar.Foreground = System.Windows.Media.Brushes.Red;
            ProgressBar.Value = 100;
            LabelProgressbar.Content = "Cancelled";
        }

        private void SetBackground()
        {
            if (CheckBoxDarkMode.IsChecked == true)
            {
                SolidColorBrush transparentBlack = new SolidColorBrush(System.Windows.Media.Color.FromArgb(65, 30, 30, 30));
                TabControl.Background = transparentBlack;
                TabGrid.Background = transparentBlack;
                TabGrid1.Background = transparentBlack;
                TabGrid2.Background = transparentBlack;
                TabGrid3.Background = transparentBlack;
                TabGrid4.Background = transparentBlack;
                TextBoxPresetName.Background = transparentBlack;
                ProgressBar.Background = transparentBlack;
            }
            else
            {
                SolidColorBrush transparentWhite = new SolidColorBrush(System.Windows.Media.Color.FromArgb(65, 100, 100, 100));
                TabControl.Background = transparentWhite;
                TabGrid.Background = transparentWhite;
                TabGrid1.Background = transparentWhite;
                TabGrid2.Background = transparentWhite;
                TabGrid3.Background = transparentWhite;
                TabGrid4.Background = transparentWhite;
                TextBoxPresetName.Background = transparentWhite;
                ProgressBar.Background = transparentWhite;
            }
        }

        //════════════════════════════════════════ Buttons ════════════════════════════════════════

        private void ButtonOpenProgramFolder_Click(object sender, RoutedEventArgs e)
        {
            try { Process.Start(Directory.GetCurrentDirectory()); }
            catch { }            
        }

        private void ButtonOpenTempFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CheckBoxCustomTempPath.IsChecked == false) { Process.Start(Directory.GetCurrentDirectory() + "\\Temp"); }
                else { Process.Start(TextBoxCustomTempPath.Text); }
            }
            catch { }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            encoder = ComboBoxEncoder.Text;
            setEncoderParameters(true);
            string inputSet = "Error";
            if (encoder == "aomenc") { inputSet = allSettingsAom; }
            if (encoder == "rav1e") { inputSet = allSettingsRav1e; }
            if (encoder == "svt-av1") { inputSet = allSettingsSVTAV1; }
            ShowSettings kappa = new ShowSettings(inputSet, CheckBoxDarkMode.IsChecked == true);
            kappa.Show();
        }

        private void ButtonSaveVideo_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBoxBatchEncoding.IsChecked == false)
            {
                SaveFileDialog saveVideoFileDialog = new SaveFileDialog();
                saveVideoFileDialog.Filter = "Video|*.mkv;*.webm;*.mp4";
                Nullable<bool> result = saveVideoFileDialog.ShowDialog();
                if (result == true) { videoOutput = saveVideoFileDialog.FileName; outputSet = true; LabelVideoOutput.Content = videoOutput; }
            }
            else
            {               
                //Sets the Batch Encoding Output Folder
                System.Windows.Forms.FolderBrowserDialog browseOutputFolder = new System.Windows.Forms.FolderBrowserDialog();
                if (browseOutputFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    videoOutput = browseOutputFolder.SelectedPath;
                    LabelVideoOutput.Content = videoOutput;
                    outputSet = true;
                }

            }

        }

        private void ButtonOpenSource_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBoxBatchEncoding.IsChecked == false)
            {
                //Opens OpenFileDialog for the Input Video
                OpenFileDialog openVideoFileDialog = new OpenFileDialog();
                openVideoFileDialog.Filter = "Video Files|*.mp4;*.m4v;*.mkv;*.webm;*.m2ts;*.flv;*.avi;*.wmv;*.ts;*.yuv|All Files|*.*";
                Nullable<bool> result = openVideoFileDialog.ShowDialog();
                if (result == true)
                {
                    videoInput = openVideoFileDialog.FileName;
                    LabelVideoSource.Content = videoInput;
                    getVideoInformation();
                    getAudioInformation();
                    inputSet = true;
                }
            }
            else
            {
                //Sets the Batch Encoding Source Folder
                System.Windows.Forms.FolderBrowserDialog browseSourceFolder = new System.Windows.Forms.FolderBrowserDialog();
                if (browseSourceFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    videoInput = browseSourceFolder.SelectedPath;
                    LabelVideoSource.Content = videoInput;
                    inputSet = true;
                }
            }

        }

        private void ButtonSavePreset_Click(object sender, RoutedEventArgs e)
        {
            SmallFunctions.checkCreateFolder("Profiles");
            SaveSettings(TextBoxPresetName.Text, true, false, false);
            LoadPresetsIntoComboBox();
        }

        private void ButtonStartEncode_Click(object sender, RoutedEventArgs e)
        {
            SmallFunctions.Cancel.CancelAll = false;
            if (inputSet && outputSet)
            {
                ProgressBar.Value = 0;
                ProgressBar.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(3, 112, 200));
                resumeMode = CheckBoxResumeMode.IsChecked == true ? true : false;
                ButtonStartEncode.BorderBrush = System.Windows.Media.Brushes.Green;
                ButtonCancelEncode.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(228, 228, 228));
                ButtonOpenSource.IsEnabled = false;
                ButtonSaveVideo.IsEnabled = false;
                if (CheckBoxBatchEncoding.IsChecked == false)
                {
                    MainEntry();
                }
                else
                {
                    BatchEncode();
                }
            }
            else
            {
                if (inputSet == false) { MessageBoxes.MessageVideoInput(); }
                if (outputSet == false) { MessageBoxes.MessageVideoOutput(); }
            }
        }

        private void ButtonCancelEncode_Click(object sender, RoutedEventArgs e)
        {
            SmallFunctions.Cancel.CancelAll = true;
            SmallFunctions.KillInstances();
            CancelRoutine();
        }

        private void ButtonSetTempPath_Click(object sender, RoutedEventArgs e)
        {
            //Sets a custom Temp Folder
            System.Windows.Forms.FolderBrowserDialog browseTempFolder = new System.Windows.Forms.FolderBrowserDialog();
            if (browseTempFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TextBoxCustomTempPath.Text = browseTempFolder.SelectedPath;
            }
        }

        private void ButtonDeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                File.Delete("Profiles\\" + ComboBoxPresetSettings.SelectedItem);
                LoadPresetsIntoComboBox(); //Reloads ComboBox
            }
            catch { }
        }

        private void ButtonSetDefaultPreset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SmallFunctions.checkCreateFolder("Profiles\\Default");
                string directory = "Profiles\\Default\\default.xml";
                XmlWriter writer = XmlWriter.Create(directory);
                writer.WriteStartElement("Settings");
                writer.WriteElementString("DefaultProfile", ComboBoxPresets.Text);
                writer.WriteEndElement();
                writer.Close();
            }
            catch { }
        }

        private void ButtonBackgroundImage_Click(object sender, RoutedEventArgs e)
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
                    SetBackground();
                }
            }
            catch { }
        }

        //═══════════════════════════════════ Other UI Elements ═══════════════════════════════════

        private void TextBoxAdvancedSettings_TextChanged(object sender, TextChangedEventArgs e)
        {
            string[] forbiddenWords = { "help", "cfg", "debug", "output", "passes", "pass", "fpf", "limit",
            "skip", "webm", "ivf", "obu", "q-hist", "rate-hist", "fullhelp", "benchmark", "first-pass", "second-pass",
            "reconstruction", "enc-mode-2p", "input-stat-file", "output-stat-file"};

            foreach (var words in forbiddenWords)
            {
                TextBoxAdvancedSettings.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
                if (TextBoxAdvancedSettings.Text.Contains(words))
                {
                    TextBoxAdvancedSettings.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                    break;
                }
            }

        }

        private void CheckBoxCustomSettings_Checked(object sender, RoutedEventArgs e)
        {
            encoder = ComboBoxEncoder.Text;
            setEncoderParameters(true);
            string inputSet = "Error";
            if (encoder == "aomenc" || encoder == "aomenc (ffmpeg)") { inputSet = allSettingsAom; }
            if (encoder == "rav1e") { inputSet = allSettingsRav1e; }
            if (encoder == "svt-av1") { inputSet = allSettingsSVTAV1; }
            TextBoxAdvancedSettings.Text = inputSet;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.ToString());
        }

        private void ButtonSupportMePayPal_Click(object sender, RoutedEventArgs e)
        {
            //If people wan't to support me
            Process.Start("https://paypal.me/alkl58");
        }

        private void ButtonDiscord_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://discord.gg/HSBxne3");
        }

        private void ButtonGithub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Alkl58/NotEnoughAV1Encodes");
        }

        private void ButtonReddit_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.reddit.com/user/Al_kl");
        }

        private void ComboBoxEncoder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string comboitem = (e.AddedItems[0] as ComboBoxItem).Content as string;
            switch(comboitem)
            {
                case "aomenc":
                    if (SliderQuality != null)
                    {
                        SliderQuality.Maximum = 63;
                        SliderQuality.Value = 30;
                        SliderPreset.Maximum = 8;
                        SliderPreset.Value = 3;
                    }
                    break;
                case "aomenc (ffmpeg)":
                    if (SliderQuality != null)
                    {
                        SliderQuality.Maximum = 63;
                        SliderQuality.Value = 30;
                        SliderPreset.Maximum = 8;
                        SliderPreset.Value = 3;
                    }
                    break;
                case "rav1e":
                    if (SliderQuality != null)
                    {
                        SliderQuality.Maximum = 255;
                        SliderQuality.Value = 100;
                        SliderPreset.Maximum = 10;
                        SliderPreset.Value = 6;
                        ComboBoxPasses.SelectedIndex = 0;
                    }
                    break;
                case "svt-av1":
                    if (SliderQuality != null)
                    {
                        SliderQuality.Maximum = 63;
                        SliderQuality.Value = 40;
                        SliderPreset.Value = 5;
                        SliderPreset.Maximum = 8;
                        if (CheckBoxWorkerLimit.IsChecked == false) { ComboBoxWorkers.SelectedIndex = 0; } //It's not necessary to have more than one Worker for SVT 
                    }
                    break;
                default:
                    break;
            }
            if (CheckBoxCustomSettings != null) { if (CheckBoxCustomSettings.IsChecked == true) { CheckBoxCustomSettings.IsChecked = false; }}
        }

        private void ComboBoxPresets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ComboBoxPresets.SelectedItem.ToString() != null)
                {
                    LoadSettings(ComboBoxPresets.SelectedItem.ToString(), true, false, false);
                }else { }
            }
            catch { }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void ComboBoxPasses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Due Rav1e Two Pass Still broken this will force one pass encoding
            if (ComboBoxEncoder.SelectedIndex == 2) { ComboBoxPasses.SelectedIndex = 0; }
        }

        private void ComboBoxWorkers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBoxEncoder.SelectedIndex == 3 && ComboBoxWorkers.SelectedIndex != 0 && CheckBoxWorkerLimit.IsChecked == false) { ComboBoxWorkers.SelectedIndex = 0; MessageBoxes.MessageSVTWorkers(); }
        }

        private void TextBoxChunkLength_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TextBoxChunkLength.Text == "0") { TextBoxChunkLength.Text = "1"; }
        }

        private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double taskMax = ProgressBar.Maximum, taskVal = ProgressBar.Value, barVal;
            TaskbarItemInfo.ProgressValue = (1.0 / taskMax) * taskVal;
        }

        private void CheckBoxDarkMode_Checked(object sender, RoutedEventArgs e)
        {
            SolidColorBrush white = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            SolidColorBrush dark = new SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 33, 33));
            SolidColorBrush darker = new SolidColorBrush(System.Windows.Media.Color.FromRgb(25, 25, 25));
            if (customBackground != true)
            {
                Window.Background = darker;
                TabControl.Background = dark;
                TabGrid.Background = dark;
                TabGrid1.Background = dark;
                TabGrid2.Background = dark;
                TabGrid3.Background = dark;
                TabGrid4.Background = dark;
                TextBoxPresetName.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(44, 44, 44));
                ProgressBar.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(25, 25, 25));
            }

            LabelPresets.Foreground = white;
            CheckBoxResumeMode.Foreground = white;
            TextBlockOpenSource.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240));
            GroupBox.BorderBrush = darker;
            GroupBox1.BorderBrush = darker;
            GroupBox2.BorderBrush = darker;
            GroupBox3.BorderBrush = darker;

        }

        private void CheckBoxDarkMode_UnChecked(object sender, RoutedEventArgs e)
        {
            SolidColorBrush white = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            SolidColorBrush black = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
            if (customBackground != true)
            {
                Window.Background = white;
                TabControl.Background = white;
                TabGrid.Background = white;
                TabGrid1.Background = white;
                TabGrid2.Background = white;
                TabGrid3.Background = white;
                TabGrid4.Background = white;
                TextBoxPresetName.Background = white;
                ProgressBar.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 230, 230));
            }
            LabelPresets.Foreground = black;
            CheckBoxResumeMode.Foreground = black;
            TextBlockOpenSource.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(21, 65, 126));
            GroupBox.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(213, 223, 229));
            GroupBox1.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(213, 223, 229));
            GroupBox2.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(213, 223, 229));
            GroupBox3.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(213, 223, 229));
        }

        //═════════════════════════════════ Save / Load Settings ══════════════════════════════════

        private void SaveSettings(string saveName, bool saveProfile, bool saveJob, bool saveQueue)
        {
            string directory = "";
            if (saveProfile) { directory = "Profiles\\" + saveName + ".xml"; }
            if (saveJob) { directory = "unfinishedjob.xml"; }
            XmlWriter writer = XmlWriter.Create(directory);
            writer.WriteStartElement("Settings");
            if (saveJob)
            {
                writer.WriteElementString("VideoInput",         videoInput);
                writer.WriteElementString("VideoInputFilename", fileName);
                writer.WriteElementString("VideoOutput",        videoOutput);
            }
            writer.WriteElementString("Encoder",            ComboBoxEncoder.SelectedIndex.ToString());
            writer.WriteElementString("Framerate",          ComboBoxFrameRate.SelectedIndex.ToString());
            writer.WriteElementString("BitDepth",           ComboBoxBitDepth.SelectedIndex.ToString());
            writer.WriteElementString("Preset",             SliderPreset.Value.ToString());
            writer.WriteElementString("QualityMode",        RadioButtonConstantQuality.IsChecked.ToString());
            writer.WriteElementString("Quality",            SliderQuality.Value.ToString());
            writer.WriteElementString("Bitrate",            TextBoxBitrate.Text);
            writer.WriteElementString("Passes",             ComboBoxPasses.SelectedIndex.ToString());
            writer.WriteElementString("Workers",            ComboBoxWorkers.SelectedIndex.ToString());
            writer.WriteElementString("Priority",           ComboBoxProcessPriority.SelectedIndex.ToString());
            writer.WriteElementString("ReencodeCodec",      ComboBoxReencodeCodec.SelectedIndex.ToString());
            writer.WriteElementString("Reencode",           CheckBoxReencodeDuringSplitting.IsChecked.ToString());
            writer.WriteElementString("PreReencode",        CheckBoxReencodeBeforeSplitting.IsChecked.ToString());
            writer.WriteElementString("ChunkCalc",          CheckBoxChunkLengthAutoCalculation.IsChecked.ToString());
            writer.WriteElementString("ChunkLength",        TextBoxChunkLength.Text);
            writer.WriteElementString("Resize",             CheckBoxResize.IsChecked.ToString());
            writer.WriteElementString("ResizeWidth",        TextBoxImageWidth.Text);
            writer.WriteElementString("ResizeHeight",       TextBoxImageHeight.Text);
            writer.WriteElementString("CustomTemp",         CheckBoxCustomTempPath.IsChecked.ToString());
            writer.WriteElementString("CustomTempPath",     TextBoxCustomTempPath.Text);
            writer.WriteElementString("DeleteTempFiles",    CheckBoxDeleteTempFiles.IsChecked.ToString());
            writer.WriteElementString("PlayFinishedSound",  CheckBoxFinishedSound.IsChecked.ToString());
            writer.WriteElementString("WorkerLimitSVT",     CheckBoxWorkerLimit.IsChecked.ToString());
            writer.WriteElementString("AudioEncoding",      CheckBoxAudioEncoding.IsChecked.ToString());
            writer.WriteElementString("AudioTrackOne",      CheckBoxAudioTrackOne.IsChecked.ToString());
            writer.WriteElementString("AudioTrackTwo",      CheckBoxAudioTrackTwo.IsChecked.ToString());
            writer.WriteElementString("AudioTrackThree",    CheckBoxAudioTrackThree.IsChecked.ToString());
            writer.WriteElementString("AudioTrackFour",     CheckBoxAudioTrackFour.IsChecked.ToString());
            writer.WriteElementString("TrackOneCodec",      ComboBoxAudioCodec.SelectedIndex.ToString());
            writer.WriteElementString("TrackTwoCodec",      ComboBoxAudioCodecTrackTwo.SelectedIndex.ToString());
            writer.WriteElementString("TrackThreeCodec",    ComboBoxAudioCodecTrackThree.SelectedIndex.ToString());
            writer.WriteElementString("TrackFourCodec",     ComboBoxAudioCodecTrackFour.SelectedIndex.ToString());
            writer.WriteElementString("TrackOneBitrate",    TextBoxAudioBitrate.Text);
            writer.WriteElementString("TrackTwoBitrate",    TextBoxAudioBitrateTrackTwo.Text);
            writer.WriteElementString("TrackThreeBitrate",  TextBoxAudioBitrateTrackThree.Text);
            writer.WriteElementString("TrackFourBitrate",   TextBoxAudioBitrateTrackFour.Text);
            writer.WriteElementString("TrackOneChannels",   ComboBoxTrackOneChannels.SelectedIndex.ToString());
            writer.WriteElementString("TrackTwoChannels",   ComboBoxTrackTwoChannels.SelectedIndex.ToString());
            writer.WriteElementString("TrackThreeChannels", ComboBoxTrackThreeChannels.SelectedIndex.ToString());
            writer.WriteElementString("TrackFourChannels",  ComboBoxTrackFourChannels.SelectedIndex.ToString());
            writer.WriteElementString("DarkMode",           CheckBoxDarkMode.IsChecked.ToString());
            writer.WriteElementString("CustomBackground",   customBackground.ToString());
            writer.WriteElementString("BackgroundPath",     PathToBackground);
            writer.WriteElementString("AdvancedSettings",   CheckBoxAdvancedSettings.IsChecked.ToString());
            if (CheckBoxAdvancedSettings.IsChecked == true)
            {
                writer.WriteElementString("Threads", ComboBoxThreadsAomenc.SelectedIndex.ToString());
                if  (ComboBoxEncoder.SelectedIndex == 0 || ComboBoxEncoder.SelectedIndex == 1 || ComboBoxEncoder.SelectedIndex == 2)
                {
                    writer.WriteElementString("Threads", CheckBoxAdvancedSettings.IsChecked.ToString());
                }
                writer.WriteElementString("TileColumns", ComboBoxTileColumns.SelectedIndex.ToString());
                writer.WriteElementString("TileRows", ComboBoxTileRows.SelectedIndex.ToString());
                writer.WriteElementString("MinKeyframeInterval", TextBoxMinKeyframeinterval.Text);
                writer.WriteElementString("MaxKeyframeInterval", TextBoxMaxKeyframeinterval.Text);
                writer.WriteElementString("ColorFormatLibaom", ComboBoxColorFormatLibaom.SelectedIndex.ToString());
                if (ComboBoxEncoder.SelectedIndex == 0)
                {
                    writer.WriteElementString("LagInFrames", TextBoxMaxLagInFrames.Text);
                    writer.WriteElementString("MaxRefFrames", ComboBoxMaxReferenceFramesAomenc.SelectedIndex.ToString());
                    writer.WriteElementString("ColorPrimaries", ComboBoxColorPrimariesAomenc.SelectedIndex.ToString());
                    writer.WriteElementString("ColorTransfer", ComboBoxColorTransferAomenc.SelectedIndex.ToString());
                    writer.WriteElementString("ColorMatrix", ComboBoxColorMatrixAomenc.SelectedIndex.ToString());
                    writer.WriteElementString("ChromaSubsampling", ComboBoxChromaSubsamplingAomenc.SelectedIndex.ToString());
                    writer.WriteElementString("Tune", ComboBoxTuneAomenc.SelectedIndex.ToString());
                    writer.WriteElementString("AQMode", ComboBoxAQMode.SelectedIndex.ToString());
                    writer.WriteElementString("SharpnessLoopFilter", ComboBoxSharpnessFilterAomenc.SelectedIndex.ToString());
                    writer.WriteElementString("Rowmt", CheckBoxRowmt.IsChecked.ToString());
                    writer.WriteElementString("KeyframeFiltering", CheckBoxKeyframeFilteringAomenc.IsChecked.ToString());
                    writer.WriteElementString("AutoAltRef", CheckBoxAutoAltRefAomenc.IsChecked.ToString());
                    writer.WriteElementString("FramePeriodicBoost", CheckBoxFrameBoostAomenc.IsChecked.ToString());
                }else if (ComboBoxEncoder.SelectedIndex == 2)
                {
                    writer.WriteElementString("RDOLookahead", TextBoxRDOLookaheadRav1e.Text);
                    writer.WriteElementString("ColorPrimariesRav1e", ComboBoxColorPrimariesRav1e.SelectedIndex.ToString());
                    writer.WriteElementString("ColorTransferRav1e", ComboBoxColorTransferRav1e.SelectedIndex.ToString());
                    writer.WriteElementString("ColorMatrixRav1e", ComboBoxColorMatrixRav1e.SelectedIndex.ToString());
                    writer.WriteElementString("PixelRangeRav1e", ComboBoxPixelRangeRav1e.SelectedIndex.ToString());
                    writer.WriteElementString("TuneRav1e", ComboBoxTuneRav1e.SelectedIndex.ToString());
                    writer.WriteElementString("ContentLightBool", CheckBoxContentLightRav1e.IsChecked.ToString());
                    writer.WriteElementString("ContentLightCll", TextBoxContentLightCllRav1e.Text);
                    writer.WriteElementString("ContentLightFall", TextBoxContentLightFallRav1e.Text);
                    writer.WriteElementString("MasteringDisplay", CheckBoxMasteringDisplayRav1e.IsChecked.ToString());
                    writer.WriteElementString("MasteringGx", TextBoxMasteringGxRav1e.Text);
                    writer.WriteElementString("MasteringGy", TextBoxMasteringGyRav1e.Text);
                    writer.WriteElementString("MasteringBx", TextBoxMasteringBxRav1e.Text);
                    writer.WriteElementString("MasteringBy", TextBoxMasteringByRav1e.Text);
                    writer.WriteElementString("MasteringRx", TextBoxMasteringRxRav1e.Text);
                    writer.WriteElementString("MasteringRy", TextBoxMasteringRyRav1e.Text);
                    writer.WriteElementString("MasteringWPx", TextBoxMasteringWPxRav1e.Text);
                    writer.WriteElementString("MasteringWPy", TextBoxMasteringWPyRav1e.Text);
                    writer.WriteElementString("MasteringLmin", TextBoxMasteringLminRav1e.Text);
                    writer.WriteElementString("MasteringLmax", TextBoxMasteringLmaxRav1e.Text);
                    writer.WriteElementString("ColorFormatRav1e", ComboBoxColorFormatRav1e.SelectedIndex.ToString());
                }
                else if (ComboBoxEncoder.SelectedIndex == 3)
                {
                    writer.WriteElementString("ColorFormatSVT", ComboBoxColorFormatSVT.SelectedIndex.ToString());
                    writer.WriteElementString("HDRSVT", CheckBoxEnableHDRSVT.IsChecked.ToString());
                    writer.WriteElementString("AQModeSVT", ComboBoxAQModeSVT.SelectedIndex.ToString());
                    writer.WriteElementString("KeyintSVT", TextBoxkeyframeIntervalSVT.Text);
                }
            }
            writer.WriteEndElement();
            writer.Close();
        }

        private void LoadSettings(string saveName, bool saveProfile, bool saveJob, bool saveQueue)
        {
            string directory = "";
            if (saveProfile) { directory = "Profiles\\" + saveName; }
            if (saveJob) { directory = "unfinishedjob.xml"; }
            XmlDocument doc = new XmlDocument();
            doc.Load(directory);
            XmlNodeList node = doc.GetElementsByTagName("Settings");
            foreach (XmlNode n in node[0].ChildNodes)
            {
                switch(n.Name)
                {
                    case "VideoInput" :         videoInput = n.InnerText; inputSet = true; LabelVideoSource.Content = n.InnerText; break;
                    case "VideoOutput":         videoOutput = n.InnerText; outputSet = true; LabelVideoOutput.Content = n.InnerText; break;
                    case "VideoInputFilename":  fileName = n.InnerText; break;

                    case "Encoder":             ComboBoxEncoder.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "Framerate":           ComboBoxFrameRate.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "BitDepth":            ComboBoxBitDepth.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "Preset":              SliderPreset.Value = Int16.Parse(n.InnerText); break;
                    case "QualityMode":         if (n.InnerText == "True") { RadioButtonConstantQuality.IsChecked = true; RadioButtonBitrate.IsChecked = false; } else { RadioButtonConstantQuality.IsChecked = false; RadioButtonBitrate.IsChecked = true; } break;
                    case "Quality":             SliderQuality.Value = Int16.Parse(n.InnerText); break;
                    case "Bitrate":             TextBoxBitrate.Text = n.InnerText; break;
                    case "Passes":              ComboBoxPasses.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "Workers":             ComboBoxWorkers.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "Priority":            ComboBoxProcessPriority.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "ReencodeCodec":       ComboBoxReencodeCodec.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "Reencode":            CheckBoxReencodeDuringSplitting.IsChecked = n.InnerText == "True"; break;
                    case "PreReencode":         CheckBoxReencodeBeforeSplitting.IsChecked = n.InnerText == "True"; break;
                    case "ChunkCalc":           CheckBoxChunkLengthAutoCalculation.IsChecked = n.InnerText == "True"; break;
                    case "ChunkLength":         TextBoxChunkLength.Text = n.InnerText; break;
                    case "Resize":              CheckBoxResize.IsChecked = n.InnerText == "True"; break;
                    case "ResizeWidth":         TextBoxImageWidth.Text = n.InnerText; break;
                    case "ResizeHeight":        TextBoxImageHeight.Text = n.InnerText; break;
                    case "CustomTemp":          CheckBoxCustomTempPath.IsChecked = n.InnerText == "True"; break;
                    case "CustomTempPath":      TextBoxCustomTempPath.Text = n.InnerText; break;
                    case "DeleteTempFiles":     CheckBoxDeleteTempFiles.IsChecked = n.InnerText == "True"; break;
                    case "PlayFinishedSound":   CheckBoxFinishedSound.IsChecked = n.InnerText == "True"; break;
                    case "WorkerLimitSVT":      CheckBoxWorkerLimit.IsChecked = n.InnerText == "True"; break;
                    case "AudioEncoding":       CheckBoxAudioEncoding.IsChecked = n.InnerText == "True"; break;
                    case "AudioTrackOne":       CheckBoxAudioTrackOne.IsChecked = n.InnerText == "True"; break;
                    case "AudioTrackTwo":       CheckBoxAudioTrackTwo.IsChecked = n.InnerText == "True"; break;
                    case "AudioTrackThree":     CheckBoxAudioTrackThree.IsChecked = n.InnerText == "True"; break;
                    case "AudioTrackFour":      CheckBoxAudioTrackFour.IsChecked = n.InnerText == "True"; break;
                    case "TrackOneCodec":       ComboBoxAudioCodec.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "TrackTwoCodec":       ComboBoxAudioCodecTrackTwo.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "TrackThreeCodec":     ComboBoxAudioCodecTrackThree.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "TrackFourCodec":      ComboBoxAudioCodecTrackFour.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "TrackOneBitrate":     TextBoxAudioBitrate.Text = n.InnerText; break;
                    case "TrackTwoBitrate":     TextBoxAudioBitrateTrackTwo.Text = n.InnerText; break;
                    case "TrackThreeBitrate":   TextBoxAudioBitrateTrackThree.Text = n.InnerText; break;
                    case "TrackFourBitrate":    TextBoxAudioBitrateTrackFour.Text = n.InnerText; break;
                    case "TrackOneChannels":    ComboBoxTrackOneChannels.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "TrackTwoChannels":    ComboBoxTrackTwoChannels.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "TrackThreeChannels":  ComboBoxTrackThreeChannels.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "TrackFourChannels":   ComboBoxTrackFourChannels.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "DarkMode":            CheckBoxDarkMode.IsChecked = n.InnerText == "True"; break;
                    case "CustomBackground":    customBackground = n.InnerText == "True"; break;
                    case "BackgroundPath":      if (customBackground) { Uri fileUri = new Uri(n.InnerText); imgDynamic.Source = new BitmapImage(fileUri); PathToBackground = n.InnerText; SetBackground(); } break;
                    case "AdvancedSettings":    CheckBoxAdvancedSettings.IsChecked = n.InnerText == "True"; break;
                    case "Threads":             ComboBoxThreadsAomenc.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "TileColumns":         ComboBoxTileColumns.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "TileRows":            ComboBoxTileRows.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "MinKeyframeInterval": TextBoxMinKeyframeinterval.Text = n.InnerText; break;
                    case "MaxKeyframeInterval": TextBoxMaxKeyframeinterval.Text = n.InnerText; break;
                    case "LagInFrames":         TextBoxMaxLagInFrames.Text = n.InnerText; break;
                    case "MaxRefFrames":        ComboBoxMaxReferenceFramesAomenc.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "ColorPrimaries":      ComboBoxColorPrimariesAomenc.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "ColorTransfer":       ComboBoxColorTransferAomenc.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "ColorMatrix":         ComboBoxColorMatrixAomenc.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "ChromaSubsampling":   ComboBoxChromaSubsamplingAomenc.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "Tune":                ComboBoxTuneAomenc.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "AQMode":              ComboBoxAQMode.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "SharpnessLoopFilter": ComboBoxSharpnessFilterAomenc.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "Rowmt":               CheckBoxRowmt.IsChecked = n.InnerText == "True"; break;
                    case "KeyframeFiltering":   CheckBoxKeyframeFilteringAomenc.IsChecked = n.InnerText == "True"; break;
                    case "AutoAltRef":          CheckBoxAutoAltRefAomenc.IsChecked = n.InnerText == "True"; break;
                    case "FramePeriodicBoost":  CheckBoxFrameBoostAomenc.IsChecked = n.InnerText == "True"; break;
                    case "RDOLookahead":        TextBoxRDOLookaheadRav1e.Text = n.InnerText; break;
                    case "ColorPrimariesRav1e": ComboBoxColorPrimariesRav1e.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "ColorTransferRav1e":  ComboBoxColorTransferRav1e.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "ColorMatrixRav1e":    ComboBoxColorMatrixRav1e.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "PixelRangeRav1e":     ComboBoxPixelRangeRav1e.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "TuneRav1e":           ComboBoxTuneRav1e.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "ContentLightBool":    CheckBoxContentLightRav1e.IsChecked = n.InnerText == "True"; break;
                    case "ContentLightCll":     TextBoxContentLightCllRav1e.Text = n.InnerText; break;
                    case "ContentLightFall":    TextBoxContentLightFallRav1e.Text = n.InnerText; break;
                    case "ColorFormatRav1e":    ComboBoxColorFormatRav1e.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "MasteringDisplay":    CheckBoxMasteringDisplayRav1e.IsChecked = n.InnerText == "True"; break;
                    case "MasteringGx":         TextBoxMasteringGxRav1e.Text = n.InnerText; break;
                    case "MasteringGy":         TextBoxMasteringGyRav1e.Text = n.InnerText; break;
                    case "MasteringBx":         TextBoxMasteringBxRav1e.Text = n.InnerText; break;
                    case "MasteringBy":         TextBoxMasteringByRav1e.Text = n.InnerText; break;
                    case "MasteringRx":         TextBoxMasteringRxRav1e.Text = n.InnerText; break;
                    case "MasteringRy":         TextBoxMasteringRyRav1e.Text = n.InnerText; break;
                    case "MasteringWPx":        TextBoxMasteringWPxRav1e.Text = n.InnerText; break;
                    case "MasteringWPy":        TextBoxMasteringWPyRav1e.Text = n.InnerText; break;
                    case "MasteringLmin":       TextBoxMasteringLminRav1e.Text = n.InnerText; break;
                    case "MasteringLmax":       TextBoxMasteringLmaxRav1e.Text = n.InnerText; break;
                    case "ColorFormatSVT":      ComboBoxAQModeSVT.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "HDRSVT":              CheckBoxEnableHDRSVT.IsChecked = n.InnerText == "True"; break;
                    case "AQModeSVT":           ComboBoxAQModeSVT.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "KeyintSVT":           TextBoxkeyframeIntervalSVT.Text = n.InnerText; break;
                    case "ColorFormatLibaom":   ComboBoxColorFormatLibaom.SelectedIndex = Int16.Parse(n.InnerText); break;

                    default: break;
                }
            }
        }

        private void LoadPresetsIntoComboBox()
        {
            try
            {
                if (Directory.Exists("Profiles"))
                {
                    DirectoryInfo profiles = new DirectoryInfo("Profiles");
                    FileInfo[] Files = profiles.GetFiles("*.xml");
                    ComboBoxPresets.ItemsSource = Files;
                    ComboBoxPresetSettings.ItemsSource = Files;
                }
            }
            catch { }
        }

        //═══════════════════════════════════════ Encoding ════════════════════════════════════════

        private void Encode()
        {

            DateTime starttime = DateTime.Now;
            starttimea = starttime;

            using (SemaphoreSlim concurrencySemaphore = new SemaphoreSlim(workerCount))
            {
                List<Task> tasks = new List<Task>();    
                foreach (var items in videoChunks)
                {
                    concurrencySemaphore.Wait();
                    var t = Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            if (SmallFunctions.Cancel.CancelAll == false)
                            {
                                if (videoPasses == 1)
                                {
                                    Process process = new Process();
                                    ProcessStartInfo startInfo = new ProcessStartInfo();
                                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                    startInfo.UseShellExecute = true;
                                    startInfo.FileName = "cmd.exe";
                                    startInfo.WorkingDirectory = ffmpegPath + "\\";

                                    switch (encoder)
                                    {
                                        case "aomenc":
                                            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + tempPath + "\\Chunks\\" + items + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + aomencPath + "\\aomenc.exe" + '\u0022' + " - --passes=1 " + allSettingsAom + " --output=" + '\u0022' + tempPath + "\\Chunks\\" + items + "-av1.ivf" + '\u0022';
                                            break;
                                        case "rav1e":
                                            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + tempPath + "\\Chunks\\" + items + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + rav1ePath + "\\rav1e.exe" + '\u0022' + " - " + allSettingsRav1e + " --output " + '\u0022' + tempPath + "\\Chunks\\" + items + "-av1.ivf" + '\u0022';
                                            break;
                                        case "aomenc (ffmpeg)":
                                            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + tempPath + "\\Chunks\\" + items + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -strict experimental -c:v libaom-av1 " + allSettingsAom + " " + '\u0022' + tempPath + "\\Chunks\\" + items + "-av1.ivf" + '\u0022';
                                            break;
                                        case "svt-av1":
                                            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + tempPath + "\\Chunks\\" + items + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -nostdin -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + svtav1Path + "\\SvtAv1EncApp.exe" + '\u0022' + " -i stdin " + allSettingsSVTAV1 + " -b " + '\u0022' + tempPath + "\\Chunks\\" + items + "-av1.ivf" + '\u0022';
                                            break;
                                        default:
                                            break;
                                    }

                                    process.StartInfo = startInfo;
                                    process.Start();
                                    //Sets the Process Priority
                                    if (processPriority == 1) { process.PriorityClass = ProcessPriorityClass.BelowNormal; }

                                    process.WaitForExit();

                                    if (SmallFunctions.Cancel.CancelAll == false) { SmallFunctions.WriteToFileThreadSafe(items, Path.Combine(tempPath, "encoded.log")); }
                                    else { SmallFunctions.KillInstances(); }
                                }
                                else if (videoPasses == 2)
                                {
                                    Process process = new Process();
                                    ProcessStartInfo startInfo = new ProcessStartInfo();

                                    bool FileExistFirstPass = File.Exists(tempPath + "\\Chunks\\" + items + "_1pass_successfull.log");

                                    if (FileExistFirstPass != true)
                                    {
                                        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                        startInfo.FileName = "cmd.exe";
                                        startInfo.WorkingDirectory = ffmpegPath + "\\";

                                        switch (encoder)
                                        {
                                            case "aomenc":
                                                startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + tempPath + "\\Chunks\\" + items + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + aomencPath + "\\aomenc.exe" + '\u0022' + " - --passes=2 --pass=1 --fpf=" + '\u0022' + tempPath + "\\Chunks\\" + items + "_stats.log" + '\u0022' + " " + allSettingsAom + " --output=NUL";
                                                break;
                                            //case "rav1e": !!! RAV1E TWO PASS IS STILL BROKEN !!!
                                                //startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + tempPath + "\\Chunks\\" + items + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + rav1ePath + '\u0022' + " - " + allSettingsRav1e + " --first-pass " + '\u0022' + tempPath + "\\Chunks\\" + items + "_stats.log" + '\u0022';
                                                //break;
                                            case "aomenc (ffmpeg)":
                                                startInfo.Arguments = "/C ffmpeg.exe -y -i " + '\u0022' + tempPath + "\\Chunks\\" + items + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -strict experimental -c:v libaom-av1 " + allSettingsAom + " -pass 1 -passlogfile " + '\u0022' + tempPath + "\\Chunks\\" + items + "_stats.log" + '\u0022' + " -f matroska NUL";
                                                break;
                                            case "svt-av1":
                                                startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + tempPath + "\\Chunks\\" + items + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -nostdin -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + svtav1Path + "\\SvtAv1EncApp.exe" + '\u0022' + " -i stdin " + allSettingsSVTAV1 + " -b NUL -output-stat-file " + '\u0022' + tempPath + "\\Chunks\\" + items + "-av1pass.stats" + '\u0022';
                                                break;
                                            default:
                                                break;
                                        }
                                        process.StartInfo = startInfo;
                                        
                                        process.Start();
                                        //Sets the Process Priority
                                        if (processPriority == 1) { process.PriorityClass = ProcessPriorityClass.BelowNormal; }

                                        process.WaitForExit();

                                        if (SmallFunctions.Cancel.CancelAll == false) { SmallFunctions.WriteToFileThreadSafe("", tempPath + "\\Chunks\\" + items + "_1pass_successfull.log"); }
                                    }

                                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                    startInfo.FileName = "cmd.exe";
                                    startInfo.WorkingDirectory = ffmpegPath + "\\";

                                    switch (encoder)
                                    {
                                        case "aomenc":
                                            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + tempPath + "\\Chunks\\" + items + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + aomencPath + "\\aomenc.exe" + '\u0022' + " - --passes=2 --pass=2 --fpf=" + '\u0022' + tempPath + "\\Chunks\\" + items + "_stats.log" + '\u0022' + " " + allSettingsAom + " --output=" + '\u0022' + tempPath + "\\Chunks\\" + items + "-av1.ivf" + '\u0022';
                                            break;
                                        //case "rav1e": !!! RAV1E TWO PASS IS STILL BROKEN !!!
                                        //startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + tempPath + "\\Chunks\\" + items + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + rav1ePath + '\u0022' + " - " + allSettingsRav1e + " --second-pass " + '\u0022' + tempPath + "\\Chunks\\" + items + "_stats.log" + '\u0022' + " --output " + '\u0022' + tempPath + "\\Chunks\\" + items + "-av1.ivf" + '\u0022';
                                        //break;
                                        case "aomenc (ffmpeg)":
                                            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + tempPath + "\\Chunks\\" + items + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -strict experimental -c:v libaom-av1 " + allSettingsAom + " -pass 2 -passlogfile " + '\u0022' + tempPath + "\\Chunks\\" + items + "_stats.log" + '\u0022' + " " + '\u0022' + tempPath + "\\Chunks\\" + items + "-av1.ivf" + '\u0022';
                                            break;
                                        case "svt-av1":
                                            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + tempPath + "\\Chunks\\" + items + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -nostdin -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + svtav1Path + '\u0022' + " -i stdin " + allSettingsSVTAV1 + " -b " + '\u0022' + tempPath + "\\Chunks\\" + items + "-av1.ivf" + '\u0022'+ " -input-stat-file " + '\u0022' + tempPath + "\\Chunks\\" + items + "-av1pass.stats" + '\u0022';
                                            break;
                                        default:
                                            break;
                                    }

                                    process.StartInfo = startInfo;
                                    
                                    process.Start();
                                    if (processPriority == 1) { process.PriorityClass = ProcessPriorityClass.BelowNormal; }
                                    process.WaitForExit();

                                    if (SmallFunctions.Cancel.CancelAll == false) { SmallFunctions.WriteToFileThreadSafe(items, Path.Combine(tempPath, "encoded.log")); }
                                    else { SmallFunctions.KillInstances(); }
                                }
                            }


                        }
                        finally
                        {
                            concurrencySemaphore.Release();
                            ProgressBar.Dispatcher.Invoke(() => ProgressBar.Value += 1, DispatcherPriority.Background);
                            TimeSpan timespent = DateTime.Now - starttime;
                            LabelProgressbar.Dispatcher.Invoke(() => LabelProgressbar.Content = ProgressBar.Value + " / " + videoChunksCount.ToString() + " - " + Math.Round(Convert.ToDecimal(((((videoLength * videoFrameRate) / videoChunksCount) * ProgressBar.Value) / timespent.TotalSeconds)), 2).ToString() + "fps" + " - " + Math.Round((((timespent.TotalSeconds / ProgressBar.Value) * (videoChunksCount - ProgressBar.Value)) / 60), MidpointRounding.ToEven) + "min left", DispatcherPriority.Background);
                        }
                    });
                    tasks.Add(t);

                }
                Task.WaitAll(tasks.ToArray());
            }
            
        }
    }
}