using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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
        public static string ffprobePath, ffmpegPath, aomencPath, rav1ePath, svtav1Path, mkvToolNixPath;
        public static string videoInput, videoOutput, encoder, fileName, videoResize, pipeBitDepth = "yuv420p", reencoder;
        public static string audioCodecTrackOne, audioCodecTrackTwo, audioCodecTrackThree, audioCodecTrackFour;
        public static string trackOneLanguage, trackTwoLanguage, trackThreeLanguage, trackFourLanguage;
        public static string allSettingsAom, allSettingsRav1e, allSettingsSVTAV1, allSettingsVP9;
        public static string tempPath = ""; //Temp Path for Splitting and Encoding
        public static string[] videoChunks, SubtitleChunks; //Temp Chunk List
        public static string PathToBackground, subtitleFfmpegCommand, deinterlaceCommand, cropCommand, trimCommand, saveSettingString, localFileName, ffmpegFramerateSplitting, frameRateTemp;
        public static string trimEndTemp, trimEndTempMax;
        public static string encoderMetadata;
        public static string subtitleTrackOnePath, subtitleTrackTwoPath, subtitleTrackThreePath, subtitleTrackFourPath;
        public static string subtitleMuxingInput, subtitleMuxingMapping;
        public static int videoChunksCount, frameCountSource, frameCountChunks;
        public static int coreCount, workerCount, chunkLength; //Variable to set the Worker Count
        public static int videoPasses, processPriority, videoLength, customsubtitleadded, counterQueue, frameRateIndex;
        public static int audioBitrateTrackOne, audioBitrateTrackTwo, audioBitrateTrackThree, audioBitrateTrackFour;
        public static int audioChannelsTrackOne, audioChannelsTrackTwo, audioChannelsTrackThree, audioChannelsTrackFour;
        public static bool trackOne, trackTwo, trackThree, trackFour, audioEncoding, pcmBluray;
        public static bool trackOneLang, trackTwoLang, trackThreeLang, trackFourLang;
        public static bool inputSet, outputSet, reencode, beforereencode, resumeMode, deleteTempFiles, deleteTempFilesDynamically;
        public static bool subtitleTrackOne, subtitleTrackTwo, subtitleTrackThree, subtitleTrackFour;
        public static bool subtitleCopy, subtitleCustom, subtitleHardcoding, subtitleEncoding;
        public static bool customBackground, programStartup = true, logging = true, buttonActive = true, saveSettings, found7z, startupTrim = false, trimButtons = false, encodeStarted;
        public static bool skipSplitting = false;
        public static bool showTerminalDuringEncode;
        public static double videoFrameRate;
        public DateTime starttimea;
        private CancellationTokenSource cancellationTokenSource;

        public MainWindow()
        {
            InitializeComponent();
            LoadSettingsTab();
            SmallFunctions.Logging("Program Version: " + TextBoxProgramVersion.Text);
            getCoreCount();            
            LoadPresetsIntoComboBox();
            LoadBackground();
            LoadDefaultProfile();
            setEncoderPath();
            SmallFunctions.Check7zExtractor();
            CheckForResumeFile();
            SmallFunctions.checkDependeciesStartup();
            programStartup = false;            
            LoadQueueStartup();
            FreeSpace();
        }

        //════════════════════════════════════ Main Functions ═════════════════════════════════════

        private void PreStart()
        {
            SmallFunctions.Logging("Button Start encode");
            SmallFunctions.Cancel.CancelAll = false;

            if (CheckBoxBatchEncoding.IsChecked == false && CheckBoxQueueEncoding.IsChecked == true)
            {
                SmallFunctions.Logging("QueueEncode()");
                QueueEncode();
            }else if(inputSet && outputSet)
            {
                ProgressBar.Value = 0;
                ProgressBar.Foreground = new SolidColorBrush(Color.FromRgb(3, 112, 200));
                resumeMode = CheckBoxResumeMode.IsChecked == true;
                ButtonStartEncode.BorderBrush = Brushes.Green;
                ButtonCancelEncode.BorderBrush = new SolidColorBrush(Color.FromRgb(228, 228, 228));
                buttonActive = false;
                if (CheckBoxBatchEncoding.IsChecked == false)
                {
                    MainEntry();
                }
                else
                {
                    SmallFunctions.Logging("BatchEncode()");
                    BatchEncode();
                }
            }
            else
            {
                if (inputSet == false) { MessageBoxes.MessageVideoInput(); SmallFunctions.Logging("Video Input not set"); }
                if (outputSet == false) { MessageBoxes.MessageVideoOutput(); SmallFunctions.Logging("Video Output not set"); }
            }
        }

        private async void MainEntry()
        {
            cancellationTokenSource = new CancellationTokenSource();
            encodeStarted = true;
            encoder = ComboBoxEncoder.Text;
            if (SmallFunctions.checkDependencies(encoder) && SmallFunctions.Cancel.CancelAll == false)
            {
                setParameters();
                setAudioParameters();
                setSubtitleParameters();
                if(CheckBoxCheckFrameCount.IsChecked == true && CheckBoxSplitting.IsChecked == true) {
                    setProgressBarLabel("Calculating Source Frame Count...");
                    await Task.Run(() => SmallFunctions.GetSourceFrameCount(videoInput)); }
                if (resumeMode == true) { await AsyncClass(cancellationTokenSource.Token); } 
                else
                {
                    saveResumeJob();
                    if (SmallFunctions.CheckFileFolder()) { await AsyncClass(cancellationTokenSource.Token); }
                    else
                    {
                        if (MessageBox.Show("Temp Chunks Folder not Empty! Overwrite existing Data?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            SmallFunctions.DeleteChunkFolderContent();
                            await AsyncClass(cancellationTokenSource.Token);
                        }
                        else { SmallFunctions.Cancel.CancelAll = true; CancelRoutine(); }
                    }
                }
            }
        }

        private async Task AsyncClass(CancellationToken token)
        {
            try
            {
                if (SmallFunctions.Cancel.CancelAll == false && resumeMode == false)
                {
                    Console.WriteLine(tempPath);
                    setProgressBarLabel("Started Audio Encoding");
                    await Task.Run(() => { token.ThrowIfCancellationRequested(); EncodeAudio.AudioEncode(); }, token);
                    if (SmallFunctions.CheckAudioOutput() == false) { MessageNoAudioOutput(); }

                    if (CheckBoxSplitting.IsChecked == true)
                    {
                        setProgressBarLabel("Started Video Splitting");
                        await Task.Run(() => { token.ThrowIfCancellationRequested(); VideoSplitting.SplitVideo(videoInput, chunkLength, reencoder, reencode, beforereencode); }, token);
                        await Task.Run(() => { token.ThrowIfCancellationRequested(); RenameChunks.Rename(); }, token);
                        if (CheckBoxCheckFrameCount.IsChecked == true)
                        {
                            setProgressBarLabel("Calculating Chunk Frame Count...");
                            await Task.Run(() => { token.ThrowIfCancellationRequested(); SmallFunctions.GetChunksFrameCount(tempPath); }, token);
                            CompareFrameCount();
                        }
                    }
                    else
                    {
                        SmallFunctions.checkCreateFolder(Path.Combine(tempPath, "Chunks"));
                    }
                }
                SmallFunctions.CountVideoChunks();
                setProgressBar(videoChunksCount);
                setProgressBarLabel("0 / " + videoChunksCount.ToString());
                setEncoderParameters(false);
                await Task.Run(() => { token.ThrowIfCancellationRequested(); Encode(); }, token);

                if (SmallFunctions.Cancel.CancelAll == false)
                {
                    await Task.Run(() => VideoMuxing.Concat());
                    if (SmallFunctions.CheckVideoOutput())
                    {
                        if (CheckBoxBatchEncoding.IsChecked == false)
                        {
                            LabelProgressbar.Dispatcher.Invoke(() => LabelProgressbar.Content = "Encoding completed! Elapsed Time: " + (DateTime.Now - starttimea).ToString("hh\\:mm\\:ss") + " - " + Math.Round(Convert.ToDecimal((((videoLength * videoFrameRate) / (DateTime.Now - starttimea).TotalSeconds))), 2).ToString() + "fps", DispatcherPriority.Background);
                            buttonActive = true;
                            ProgressBar.Foreground = Brushes.Green;
                            ButtonStartEncode.BorderBrush = new SolidColorBrush(Color.FromRgb(228, 228, 228));
                        }
                        if (deleteTempFiles) { SmallFunctions.DeleteTempFiles(); SmallFunctions.DeleteLogFile(); }
                        if (CheckBoxFinishedSound.IsChecked == true && CheckBoxBatchEncoding.IsChecked == false) { SmallFunctions.PlayFinishedSound(); }
                        if (CheckBoxShutdownAfterEncode.IsChecked == true && CheckBoxBatchEncoding.IsChecked == false) { Process.Start("shutdown.exe", "/s /t 0"); }
                    }
                }
                encodeStarted = false;
            }
            catch (Exception e){ SmallFunctions.Logging(e.Message); }
        }

        private async void BatchEncode()
        {
            videoInput = LabelVideoSource.Content.ToString();  // Both are set, to avoid problems when people retry the batch encoding in the same instance
            videoOutput = LabelVideoOutput.Content.ToString(); // -^
            encodeStarted = true;
            DirectoryInfo batchfiles = new DirectoryInfo(videoInput);
            foreach (var file in batchfiles.GetFiles())
            {
                if (CheckFileType(file.ToString()) == true && SmallFunctions.Cancel.CancelAll == false)
                {
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
                    setSubtitleParameters();

                    if (CheckBoxSubtitleEncoding.IsChecked == true)
                    {
                        //Don't want to burn in subtitles in Batch Encoding
                        CheckBoxSubOneBurn.IsChecked = false;
                        CheckBoxSubTwoBurn.IsChecked = false;
                        CheckBoxSubThreeBurn.IsChecked = false;
                        CheckBoxSubFourBurn.IsChecked = false;
                    }

                    try
                    {
                        if (SmallFunctions.CheckFileFolder())
                            SmallFunctions.DeleteChunkFolderContent(); // Avoids Temp file issues, as the majaroity of safeguards are not used during Batch encoding
                    } catch { }

                    if (CheckBoxCheckFrameCount.IsChecked == true && CheckBoxSplitting.IsChecked == true)
                    {
                        setProgressBarLabel("Calculating Source Frame Count...");
                        await Task.Run(() => SmallFunctions.GetSourceFrameCount(videoInput));
                    }

                    if (SmallFunctions.Cancel.CancelAll == false)
                    {
                        await AsyncClass(cancellationTokenSource.Token);
                    }
                }
            }
            buttonActive = true;
            ProgressBar.Foreground = Brushes.Green;
            ButtonStartEncode.BorderBrush = new SolidColorBrush(Color.FromRgb(228, 228, 228));
            if (CheckBoxFinishedSound.IsChecked == true) { SmallFunctions.PlayFinishedSound(); }
            encodeStarted = false;
            if (CheckBoxShutdownAfterEncode.IsChecked == true) { Process.Start("shutdown.exe", "/s /t 0"); }
        }

        private async void QueueEncode()
        {
            encodeStarted = true;
            List<object> queue = new List<object>();
            foreach (var item in ListBoxQueue.Items) { queue.Add(item); }
            foreach (var item in queue)
            {
                if(SmallFunctions.Cancel.CancelAll == false)
                {
                    LoadSettings(item.ToString(), false, false, true);
                    ProgressBar.Maximum = 100;
                    ProgressBar.Value = 0;
                    ProgressBar.Foreground = new SolidColorBrush(Color.FromRgb(3, 112, 200));
                    setChunkLength();
                    setParameters();
                    SmallFunctions.DeleteChunkFolderContent();
                    setAudioParameters();
                    setSubtitleParameters();
                    setFrameRate(ffprobe.GetFrameRate(videoInput));

                    if (SmallFunctions.Cancel.CancelAll == false)
                    {
                        await AsyncClass(cancellationTokenSource.Token);
                    }

                    File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "Queue", item.ToString() + ".xml"));
                    ListBoxQueue.Items.Clear();
                    LoadQueueStartup();
                }
            }
            buttonActive = true;
            ProgressBar.Foreground = new SolidColorBrush(Color.FromRgb(3, 112, 200));
            ButtonStartEncode.BorderBrush = new SolidColorBrush(Color.FromRgb(228, 228, 228));
            encodeStarted = false;
            if (CheckBoxFinishedSound.IsChecked == true) { SmallFunctions.PlayFinishedSound(); }
            if (CheckBoxShutdownAfterEncode.IsChecked == true && SmallFunctions.Cancel.CancelAll == false) { Process.Start("shutdown.exe", "/s /t 0"); }
        }

        //══════════════════════════════════ General Parameters ═══════════════════════════════════

        private void setParameters()
        {
            tempPath = "NEAV1E\\" + fileName + "\\";

            encoder = ComboBoxEncoder.Text; reencoder = ComboBoxReencodeCodec.Text;
            SmallFunctions.Logging("Encoder: " + encoder); SmallFunctions.Logging("ReEncoder: " + reencoder);

            processPriority = ComboBoxProcessPriority.SelectedIndex;
            showTerminalDuringEncode = CheckBoxEncodingTerminal.IsChecked == true;

            resumeMode = CheckBoxResumeMode.IsChecked == true;
            SmallFunctions.Logging("Resume Mode: " + resumeMode);
            deleteTempFiles = CheckBoxDeleteTempFiles.IsChecked == true;
            deleteTempFilesDynamically = CheckBoxDeleteTempFilesDynamically.IsChecked == true;
            reencode = CheckBoxReencodeDuringSplitting.IsChecked == true;
            SmallFunctions.Logging("Reencode: " + reencode);
            beforereencode = CheckBoxReencodeBeforeSplitting.IsChecked == true;
            SmallFunctions.Logging("PreReencode: " + beforereencode);

            videoPasses = int.Parse(ComboBoxPasses.Text);
            SmallFunctions.Logging("Encoding Passes: " + videoPasses);
            workerCount = int.Parse(ComboBoxWorkers.Text);
            SmallFunctions.Logging("Worker Count: " + workerCount);
            chunkLength = int.Parse(TextBoxChunkLength.Text);
            SmallFunctions.Logging("Chunk Length: " + chunkLength);
            videoLength = int.Parse(ffprobe.GetVideoLength(videoInput));
            SmallFunctions.Logging("Video Length: " + videoLength);

            if (CheckBoxTrimming.IsChecked == true)
            {
                trimCommand = " -ss " + TextBoxTrimStart.Text + " -to " + TextBoxTrimEnd.Text + " ";
                setVideoLengthTrimmed();
            } else { trimCommand = " "; }

            setFilters();

            if (CheckBoxCustomTempPath.IsChecked == true) 
            { 
                tempPath = Path.Combine(TextBoxCustomTempPath.Text, tempPath); 
            } 
            else 
            { 
                tempPath = Path.Combine(Path.GetTempPath(), tempPath); 
            }

            SmallFunctions.checkCreateFolder(tempPath);

            if (ComboBoxFrameRate.SelectedIndex != frameRateIndex)
            {
                ffmpegFramerateSplitting = " -r " + setChangeFramerate() + " ";
            }
            else { ffmpegFramerateSplitting = ""; }
        }

        private void setFilters()
        {
            string widthNew = (int.Parse(TextBoxCropRight.Text) + int.Parse(TextBoxCropLeft.Text)).ToString();
            string hieghtNew = (int.Parse(TextBoxCropTop.Text) + int.Parse(TextBoxCropBottom.Text)).ToString();
            if (CheckBoxResize.IsChecked == true) 
            { 
                videoResize = "-vf scale=" + TextBoxImageWidth.Text + ":" + TextBoxImageHeight.Text + " -sws_flags " + ComboBoxResizeFilters.Text; 
            } else { videoResize = ""; }

            if (CheckBoxDeinterlaceYadif.IsChecked == true) 
            { 
                deinterlaceCommand = " -vf " + ComboBoxDeinterlace.Text; 
            } else { deinterlaceCommand = ""; }

            if (CheckBoxCrop.IsChecked == true)
            {
                cropCommand = " -vf crop=iw-" + widthNew + ":ih-" + hieghtNew + ":" + TextBoxCropLeft.Text + ":" + TextBoxCropTop.Text;
            } else { cropCommand = ""; }

            if (CheckBoxDeinterlaceYadif.IsChecked == true && CheckBoxResize.IsChecked == true && CheckBoxCrop.IsChecked == false)
            {
                deinterlaceCommand = " -vf " + '\u0022' + "scale=" + TextBoxImageWidth.Text + ":" + TextBoxImageHeight.Text + "," + ComboBoxDeinterlace.Text + '\u0022' + " -sws_flags " + ComboBoxResizeFilters.Text;
                videoResize = ""; cropCommand = "";
            }

            if (CheckBoxDeinterlaceYadif.IsChecked == true && CheckBoxResize.IsChecked == true && CheckBoxCrop.IsChecked == true)
            {
                deinterlaceCommand = " -vf " + '\u0022' + "scale=" + TextBoxImageWidth.Text + ":" + TextBoxImageHeight.Text + "," + ComboBoxDeinterlace.Text + ",crop=iw-" + widthNew + ":ih-" + hieghtNew + ":" + TextBoxCropLeft.Text + ":" + TextBoxCropTop.Text + '\u0022' + " -sws_flags " + ComboBoxResizeFilters.Text;
                videoResize = ""; cropCommand = "";
            }

            if (CheckBoxDeinterlaceYadif.IsChecked == false && CheckBoxResize.IsChecked == true && CheckBoxCrop.IsChecked == true)
            {
                deinterlaceCommand = " -vf " + '\u0022' + "scale=" + TextBoxImageWidth.Text + ":" + TextBoxImageHeight.Text + ",crop=iw-" + widthNew + ":ih-" + hieghtNew + ":" + TextBoxCropLeft.Text + ":" + TextBoxCropTop.Text + '\u0022' + " -sws_flags " + ComboBoxResizeFilters.Text;
                videoResize = ""; cropCommand = "";
            }

            if (CheckBoxDeinterlaceYadif.IsChecked == true && CheckBoxResize.IsChecked == false && CheckBoxCrop.IsChecked == true)
            {
                deinterlaceCommand = " -vf " + '\u0022' + ComboBoxDeinterlace.Text + ",crop=iw-" + widthNew + ":ih-" + hieghtNew + ":" + TextBoxCropLeft.Text + ":" + TextBoxCropTop.Text + '\u0022';
                videoResize = ""; cropCommand = "";
            }
        }

        private void setVideoLengthTrimmed()
        {
            try
            {
                DateTime start = DateTime.ParseExact(TextBoxTrimStart.Text, "hh:mm:ss.fff", CultureInfo.InvariantCulture);
                DateTime end = DateTime.ParseExact(TextBoxTrimEnd.Text, "hh:mm:ss.fff", CultureInfo.InvariantCulture);
                if (start < end)
                {
                    TextBoxTrimEnd.BorderBrush = new SolidColorBrush(Color.FromRgb(171, 173, 179));
                    TextBoxTrimStart.BorderBrush = new SolidColorBrush(Color.FromRgb(171, 173, 179));
                    TimeSpan result = end - start;
                    videoLength = Convert.ToInt16(result.TotalSeconds);
                    if (CheckBoxCustomTempPath != null && inputSet){ setImagePreview(); }                    
                    if (CheckBoxChunkLengthAutoCalculation.IsChecked == true) { TextBoxChunkLength.Text = (videoLength / int.Parse(ComboBoxWorkers.Text)).ToString(); }
                }
                else
                {
                    TextBoxTrimEnd.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                    TextBoxTrimStart.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                }
            } catch { }
        }

        //══════════════════════════════════ Encoder Parameters ═══════════════════════════════════
        
        private void setEncoderParameters(bool tempSettings)
        {
            switch (encoder)
            {
                case "aomenc": SetAomencParameters(tempSettings); break;
                case "rav1e": SetRav1eParameters(tempSettings); break;
                case "libaom": SetLibaomParameters(tempSettings); break;
                case "svt-av1": SetSVTAV1Parameters(tempSettings); break;
                case "libvpx-vp9": SetVP9Parameters(tempSettings); break;
                default: break;
            }
        }

        private void SetAomencParameters(bool tempSettings)
        {
            switch (ComboBoxChromaSubsamplingAomenc.SelectedIndex)
            {
                case 0:
                    if (ComboBoxBitDepth.SelectedIndex == 0) { pipeBitDepth = "yuv420p"; }
                    if (ComboBoxBitDepth.SelectedIndex == 1) { pipeBitDepth = "yuv420p10le -strict -1"; }
                    if (ComboBoxBitDepth.SelectedIndex == 2) { pipeBitDepth = "yuv420p12le -strict -1"; }
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
            string aomencQualityMode;
            if (RadioButtonConstantQuality.IsChecked == true) { aomencQualityMode = "--end-usage=q --cq-level=" + SliderQuality.Value; }
            else { aomencQualityMode = "--end-usage=vbr --target-bitrate=" + TextBoxBitrate.Text; }
            //Basic Settings
            if (CheckBoxAdvancedSettings.IsChecked == false)
            {
                allSettingsAom = "--cpu-used=" + SliderPreset.Value + " --bit-depth=" + ComboBoxBitDepth.Text + " --threads=4 --tile-columns=2 --tile-rows=1 --kf-max-dist=240 " + aomencQualityMode;
            }
            else
            {
                if (CheckBoxCustomSettings.IsChecked == false || tempSettings)
                {
                    string rowmt = (CheckBoxRowmt.IsChecked == false) ? "0" : "1";
                    string keyfiltering = ComboBoxAomKeyframeFiltering.SelectedIndex.ToString();
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
            SmallFunctions.Logging("Parameters aomenc: " + allSettingsAom);
            if (CheckBoxCommentHeaderSettings.IsChecked == true && CheckBoxAdvancedSettings.IsChecked == true)
            {
                encoderMetadata = " -metadata description=" + '\u0022' + "NotEnoughAV1Encodes - Encoder: aomenc " + allSettingsAom + '\u0022';
            }
            else { encoderMetadata = ""; }
            if (CheckBoxRealtimeMode.IsChecked == true) { allSettingsAom += " --rt "; }
        }

        private void SetLibaomParameters(bool tempSettings)
        {
            switch (ComboBoxColorFormatLibaom.SelectedIndex)
            {
                case 0:
                    if (ComboBoxBitDepth.SelectedIndex == 0) { pipeBitDepth = "yuv420p"; }
                    if (ComboBoxBitDepth.SelectedIndex == 1) { pipeBitDepth = "yuv420p10le -strict -1"; }
                    if (ComboBoxBitDepth.SelectedIndex == 2) { pipeBitDepth = "yuv420p12le -strict -1"; }
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
            string aomencQualityMode;
            if (RadioButtonConstantQuality.IsChecked == true) { aomencQualityMode = " -crf " + SliderQuality.Value + " -b:v 0"; }
            else { aomencQualityMode = " -b:v " + TextBoxBitrate.Text + "k"; }
            //Basic Settings
            if (CheckBoxAdvancedSettings.IsChecked == false)
            {
                allSettingsAom = "-cpu-used " + SliderPreset.Value + " -threads 4 -g 240 -tile-columns 2 -tile-rows 1 " + aomencQualityMode;
            }
            else
            {
                if (CheckBoxCustomSettings.IsChecked == false || tempSettings)
                {
                    string altref = " -auto-alt-ref 1 ";
                    if (CheckBoxAltRefLibaom.IsChecked == false) { altref = " -auto-alt-ref 0 "; }
                    string aomencFrames = " -tile-columns " + ComboBoxTileColumns.Text + " -tile-rows " + ComboBoxTileRows.Text + " -g " + TextBoxMaxKeyframeinterval.Text + " -lag-in-frames " + TextBoxLagInFramesLibaom.Text + " -aq-mode " + ComboBoxAqModeLibaom.SelectedIndex + " -tune " + ComboBoxTunelibaom.Text;
                    allSettingsAom = "-cpu-used " + SliderPreset.Value + " -threads " + ComboBoxThreadsAomenc.Text + aomencFrames + aomencQualityMode + altref;
                }
                else
                {
                    allSettingsAom = TextBoxAdvancedSettings.Text;
                }
            }
            SmallFunctions.Logging("Parameters libaom: " + allSettingsAom);
            if (CheckBoxCommentHeaderSettings.IsChecked == true && CheckBoxAdvancedSettings.IsChecked == true)
            {
                encoderMetadata = " -metadata description=" + '\u0022' + "NotEnoughAV1Encodes - Encoder: libaom " + allSettingsAom + '\u0022';
            }
            else { encoderMetadata = ""; }    
            if (CheckBoxRealtimeMode.IsChecked == true) { allSettingsAom += " -usage realtime "; }
        }

        private void SetRav1eParameters(bool tempSettings)
        {
            string rav1eContentLight = "";
            string rav1eMasteringDisplay = "";
            switch (ComboBoxColorFormatRav1e.SelectedIndex)
            {
                case 0:
                    if (ComboBoxBitDepth.SelectedIndex == 1) { pipeBitDepth = "yuv420p"; }
                    if (ComboBoxBitDepth.SelectedIndex == 1) { pipeBitDepth = "yuv420p10le -strict -1"; }
                    if (ComboBoxBitDepth.SelectedIndex == 2) { pipeBitDepth = "yuv420p12le -strict -1"; }
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
            string rav1eQualityMode;
            if (RadioButtonConstantQuality.IsChecked == true) { rav1eQualityMode = "--quantizer " + SliderQuality.Value; }
            else { rav1eQualityMode = "--bitrate " + TextBoxBitrate.Text; }
            //Basic Settings
            if (CheckBoxAdvancedSettings.IsChecked == false)
            {
                allSettingsRav1e = "--speed " + SliderPreset.Value + " --keyint 240 --tile-rows 1 --tile-cols 4 " + rav1eQualityMode;
            }
            else if (CheckBoxCustomSettings.IsChecked == false || tempSettings)
            {
                string rav1eColor = " --primaries " + ComboBoxColorPrimariesRav1e.Text + " --transfer " + ComboBoxColorTransferRav1e.Text + " --matrix " + ComboBoxColorMatrixRav1e.Text + " --range " + ComboBoxPixelRangeRav1e.Text;
                if (CheckBoxContentLightRav1e.IsChecked == true) { rav1eContentLight = " --content-light " + TextBoxContentLightCllRav1e.Text + "," + TextBoxContentLightFallRav1e.Text; }
                if (CheckBoxMasteringDisplayRav1e.IsChecked == true) { rav1eMasteringDisplay = " --mastering-display G(" + TextBoxMasteringGxRav1e.Text + "," + TextBoxMasteringGyRav1e.Text + ")B(" + TextBoxMasteringBxRav1e.Text + "," + TextBoxMasteringByRav1e.Text + ")R(" + TextBoxMasteringRxRav1e.Text + "," + TextBoxMasteringRyRav1e.Text + ")WP(" + TextBoxMasteringWPxRav1e.Text + "," + TextBoxMasteringWPyRav1e.Text + ")L(" + TextBoxMasteringLmaxRav1e.Text + "," + TextBoxMasteringLminRav1e.Text + ")"; }
                allSettingsRav1e = "--speed " + SliderPreset.Value + " " + rav1eQualityMode + " --threads " + ComboBoxThreadsAomenc.Text + " --min-keyint " + TextBoxMinKeyframeinterval.Text + " --keyint " + TextBoxMaxKeyframeinterval.Text + " --tile-rows " + ComboBoxTileRows.Text + " --tile-cols " + ComboBoxTileColumns.Text + " --tune " + ComboBoxTuneRav1e.Text + " --rdo-lookahead-frames " + TextBoxRDOLookaheadRav1e.Text + rav1eColor + rav1eContentLight + rav1eMasteringDisplay;
            }
            else
            {
                allSettingsRav1e = TextBoxAdvancedSettings.Text;
            }
            SmallFunctions.Logging("Parameters rav1e: " + allSettingsRav1e);
            if (CheckBoxCommentHeaderSettings.IsChecked == true && CheckBoxAdvancedSettings.IsChecked == true)
            {
                encoderMetadata = " -metadata description=" + '\u0022' + "NotEnoughAV1Encodes - Encoder: rav1e " + allSettingsRav1e + '\u0022';
            }
            else { encoderMetadata = ""; }                
        }

        private void SetSVTAV1Parameters(bool tempSettings)
        {
            string svtav1QualityMode;
            switch (ComboBoxColorFormatSVT.SelectedIndex)
            {
                case 0: break; //yuv400p piping apparently does not work
                case 1:
                    if (ComboBoxBitDepth.SelectedIndex == 0) { pipeBitDepth = "yuv420p"; }
                    if (ComboBoxBitDepth.SelectedIndex == 1) { pipeBitDepth = "yuv420p10le -strict -1"; }
                    if (ComboBoxBitDepth.SelectedIndex == 2) { pipeBitDepth = "yuv420p12le -strict -1"; }
                    break;
                case 2:
                    if (ComboBoxBitDepth.SelectedIndex == 0) { pipeBitDepth = "yuv422p -strict -1"; }
                    if (ComboBoxBitDepth.SelectedIndex == 1) { pipeBitDepth = "yuv422p10le -strict -1"; }
                    if (ComboBoxBitDepth.SelectedIndex == 2) { pipeBitDepth = "yuv422p12le -strict -1"; } break;
                case 3:
                    if (ComboBoxBitDepth.SelectedIndex == 0) { pipeBitDepth = "yuv444p -strict -1"; }
                    if (ComboBoxBitDepth.SelectedIndex == 1) { pipeBitDepth = "yuv444p10le -strict -1"; }
                    if (ComboBoxBitDepth.SelectedIndex == 2) { pipeBitDepth = "yuv444p12le -strict -1"; } break;
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
                allSettingsSVTAV1 = "--preset " + SliderPreset.Value + " --input-depth " + ComboBoxBitDepth.Text + svtav1QualityMode + " --tile-rows " + ComboBoxTileRows.Text + " --tile-columns " + ComboBoxTileColumns.Text + " --color-format " + ComboBoxColorFormatSVT.SelectedIndex + hdrSVT + " --adaptive-quantization " + ComboBoxAQModeSVT.SelectedIndex + " --keyint " + TextBoxkeyframeIntervalSVT.Text;
            }
            else
            {
                allSettingsSVTAV1 = TextBoxAdvancedSettings.Text;
            }
            SmallFunctions.Logging("Parameters svt-av1: " + allSettingsSVTAV1);
            if (CheckBoxCommentHeaderSettings.IsChecked == true && CheckBoxAdvancedSettings.IsChecked == true)
            {
                encoderMetadata = " -metadata description=" + '\u0022' + "NotEnoughAV1Encodes - Encoder: SVT-AV1 " + allSettingsSVTAV1 + '\u0022';
            }
            else { encoderMetadata = ""; }              
        }

        private void SetVP9Parameters(bool tempSettings)
        {
            string vp9QualityMode;
            switch (ComboBoxColorFormatLibaom.SelectedIndex)
            {
                case 0:
                    if (ComboBoxBitDepth.SelectedIndex == 0) { pipeBitDepth = "yuv420p"; }
                    if (ComboBoxBitDepth.SelectedIndex == 1) { pipeBitDepth = "yuv420p10le -strict -1"; }
                    if (ComboBoxBitDepth.SelectedIndex == 2) { pipeBitDepth = "yuv420p12le -strict -1"; }
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
            if (RadioButtonConstantQuality.IsChecked == true) { vp9QualityMode = " -crf " + SliderQuality.Value + " -b:v 0 "; }
            else { vp9QualityMode = " -b:v " + TextBoxBitrate.Text + "k "; }
            //Basic Settings
            if (CheckBoxAdvancedSettings.IsChecked == false)
            {
                allSettingsVP9 = "-cpu-used " + SliderPreset.Value + " -g 240 -tile-columns 1 -tile-rows 1 " + vp9QualityMode;
            }
            else
            {
                if (CheckBoxCustomSettings.IsChecked == false || tempSettings)
                {
                    string altref = " -auto-alt-ref 0 ";
                    if (CheckBoxAutoAltRefVP9.IsChecked == true) { altref = " -auto-alt-ref 1 "; }
                    string vp9Frames = " -tile-columns " + ComboBoxTileColumns.Text + " -tile-rows " + ComboBoxTileRows.Text + " -g " + TextBoxMaxKeyframeinterval.Text + " -lag-in-frames " + TextBoxLagInFramesLibaom.Text + " -aq-mode " + ComboBoxAQModeVP9.SelectedIndex + " -tune " + ComboBoxTuneVP9.SelectedIndex;
                    allSettingsVP9 = "-cpu-used " + SliderPreset.Value + vp9QualityMode + vp9Frames + altref;
                }
                else
                {
                    allSettingsVP9 = TextBoxAdvancedSettings.Text;
                }
            }
            if (CheckBoxCommentHeaderSettings.IsChecked == true && CheckBoxAdvancedSettings.IsChecked == true)
            {
                encoderMetadata = " -metadata description=" + '\u0022' + "NotEnoughAV1Encodes - Encoder: VP9 " + allSettingsVP9 + '\u0022';
            }
            else { encoderMetadata = ""; }                
        }

        //═══════════════════════════════════ Audio Parameters ════════════════════════════════════
       
        private void setAudioParameters()
        {
            trackOne = CheckBoxAudioTrackOne.IsChecked == true;
            trackTwo = CheckBoxAudioTrackTwo.IsChecked == true;
            trackThree = CheckBoxAudioTrackThree.IsChecked == true;
            trackFour = CheckBoxAudioTrackFour.IsChecked == true;
            trackOneLang = ComboBoxTrackOneLanguage.SelectedIndex != 0;
            trackTwoLang = ComboBoxTrackTwoLanguage.SelectedIndex != 0;
            trackThreeLang = ComboBoxTrackThreeLanguage.SelectedIndex != 0;
            trackFourLang = ComboBoxTrackFourLanguage.SelectedIndex != 0;
            trackOneLanguage = ComboBoxTrackOneLanguage.Text;
            trackTwoLanguage = ComboBoxTrackTwoLanguage.Text;
            trackThreeLanguage = ComboBoxTrackThreeLanguage.Text;
            trackFourLanguage = ComboBoxTrackFourLanguage.Text;
            audioEncoding = CheckBoxAudioEncoding.IsChecked == true;
            audioCodecTrackOne = ComboBoxAudioCodec.Text;
            audioCodecTrackTwo = ComboBoxAudioCodecTrackTwo.Text;
            audioCodecTrackThree = ComboBoxAudioCodecTrackThree.Text;
            audioCodecTrackFour = ComboBoxAudioCodecTrackFour.Text;
            audioBitrateTrackOne = int.Parse(TextBoxAudioBitrate.Text);
            audioBitrateTrackTwo = int.Parse(TextBoxAudioBitrateTrackTwo.Text);
            audioBitrateTrackThree = int.Parse(TextBoxAudioBitrateTrackThree.Text);
            audioBitrateTrackFour = int.Parse(TextBoxAudioBitrateTrackFour.Text);
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

        //═════════════════════════════════ Subtitles Parameters ══════════════════════════════════

        private void setSubtitleParameters()
        {
            subtitleTrackOne = CheckBoxSubtitleActivatedOne.IsChecked == true;
            subtitleTrackTwo = CheckBoxSubtitleActivatedTwo.IsChecked == true;
            subtitleTrackThree = CheckBoxSubtitleActivatedThree.IsChecked == true;
            subtitleTrackFour = CheckBoxSubtitleActivatedFour.IsChecked == true;
            subtitleEncoding = false;

            //Required for later muxing - softsub
            if (subtitleTrackOne && CheckBoxSubOneBurn.IsChecked != true) 
            {
                string subDefault = "no";
                subtitleEncoding = true;
                if (CheckBoxSubOneDefault.IsChecked == true) { subDefault = "yes"; }
                subtitleMuxingInput += " --language 0:" + ComboBoxSubTrackOneLanguage.Text + " --track-name 0:" + '\u0022' + TextBoxSubOneName.Text + '\u0022' + " --default-track 0:" + subDefault + " " + '\u0022' + TextBoxSubtitleTrackOne.Text + '\u0022';
            }
            if (subtitleTrackTwo && CheckBoxSubTwoBurn.IsChecked != true) 
            {
                string subDefault = "no";
                subtitleEncoding = true;
                if (CheckBoxSubTwoDefault.IsChecked == true) { subDefault = "yes"; }
                subtitleMuxingInput += " --language 0:" + ComboBoxSubTrackTwoLanguage.Text + " --track-name 0:" + '\u0022' + TextBoxSubTwoName.Text + '\u0022' + " --default-track 0:" + subDefault + " " + '\u0022' + TextBoxSubtitleTrackTwo.Text + '\u0022';
            }
            if (subtitleTrackThree && CheckBoxSubThreeBurn.IsChecked != true) 
            {
                string subDefault = "no";
                subtitleEncoding = true;
                if (CheckBoxSubThreeDefault.IsChecked == true) { subDefault = "yes"; }
                subtitleMuxingInput += " --language 0:" + ComboBoxSubTrackThreeLanguage.Text + " --track-name 0:" + '\u0022' + TextBoxSubThreeName.Text + '\u0022' + " --default-track 0:" + subDefault + " " + '\u0022' + TextBoxSubtitleTrackThree.Text + '\u0022';
            }
            if (subtitleTrackFour && CheckBoxSubFourBurn.IsChecked != true) 
            {
                string subDefault = "no";
                subtitleEncoding = true;
                if (CheckBoxSubFourDefault.IsChecked == true) { subDefault = "yes"; }
                subtitleMuxingInput += " --language 0:" + ComboBoxSubTrackFourLanguage.Text + " --track-name 0:" + '\u0022' + TextBoxSubFourName.Text + '\u0022' + " --default-track 0:" + subDefault + " " + '\u0022' + TextBoxSubtitleTrackFour.Text + '\u0022';
            }

            //Required for reencoding - hardsub
            if (CheckBoxSubOneBurn.IsChecked == true && subtitleTrackOne)
            {
                string ext = Path.GetExtension(TextBoxSubtitleTrackOne.Text);
                if (ext == ".ass" || ext == ".ssa")
                {
                    subtitleFfmpegCommand = "-vf ass=" + '\u0022' + TextBoxSubtitleTrackOne.Text + '\u0022';
                    subtitleFfmpegCommand = subtitleFfmpegCommand.Replace("\u005c", "\u005c\u005c\u005c\u005c");
                    subtitleFfmpegCommand = subtitleFfmpegCommand.Replace(":", "\u005c\u005c\u005c:");
                    SmallFunctions.Logging("Subtitle Hardcoding Parameters: " + subtitleFfmpegCommand);
                }else if(ext == ".srt")
                {
                    subtitleFfmpegCommand = "-vf subtitles=" + '\u0022' + TextBoxSubtitleTrackOne.Text + '\u0022';
                    subtitleFfmpegCommand = subtitleFfmpegCommand.Replace("\u005c", "\u005c\u005c\u005c\u005c");
                    subtitleFfmpegCommand = subtitleFfmpegCommand.Replace(":", "\u005c\u005c\u005c:");
                }
                else{ MessageBoxes.MessageCustomSubtitleHardCodeNotSupported(); }
            }
            if (CheckBoxSubTwoBurn.IsChecked == true && subtitleTrackTwo)
            {
                string ext = Path.GetExtension(TextBoxSubtitleTrackTwo.Text);
                if (ext == ".ass" || ext == ".ssa")
                {
                    subtitleFfmpegCommand = "-vf ass=" + '\u0022' + TextBoxSubtitleTrackTwo.Text + '\u0022';
                    subtitleFfmpegCommand = subtitleFfmpegCommand.Replace("\u005c", "\u005c\u005c\u005c\u005c");
                    subtitleFfmpegCommand = subtitleFfmpegCommand.Replace(":", "\u005c\u005c\u005c:");
                    SmallFunctions.Logging("Subtitle Hardcoding Parameters: " + subtitleFfmpegCommand);
                }
                else if (ext == ".srt")
                {
                    subtitleFfmpegCommand = "-vf subtitles=" + '\u0022' + TextBoxSubtitleTrackTwo.Text + '\u0022';
                    subtitleFfmpegCommand = subtitleFfmpegCommand.Replace("\u005c", "\u005c\u005c\u005c\u005c");
                    subtitleFfmpegCommand = subtitleFfmpegCommand.Replace(":", "\u005c\u005c\u005c:");
                }
                else { MessageBoxes.MessageCustomSubtitleHardCodeNotSupported(); }
            }
            if (CheckBoxSubThreeBurn.IsChecked == true && subtitleTrackThree)
            {
                string ext = Path.GetExtension(TextBoxSubtitleTrackThree.Text);
                if (ext == ".ass" || ext == ".ssa")
                {
                    subtitleFfmpegCommand = "-vf ass=" + '\u0022' + TextBoxSubtitleTrackThree.Text + '\u0022';
                    subtitleFfmpegCommand = subtitleFfmpegCommand.Replace("\u005c", "\u005c\u005c\u005c\u005c");
                    subtitleFfmpegCommand = subtitleFfmpegCommand.Replace(":", "\u005c\u005c\u005c:");
                    SmallFunctions.Logging("Subtitle Hardcoding Parameters: " + subtitleFfmpegCommand);
                }
                else if (ext == ".srt")
                {
                    subtitleFfmpegCommand = "-vf subtitles=" + '\u0022' + TextBoxSubtitleTrackThree.Text + '\u0022';
                    subtitleFfmpegCommand = subtitleFfmpegCommand.Replace("\u005c", "\u005c\u005c\u005c\u005c");
                    subtitleFfmpegCommand = subtitleFfmpegCommand.Replace(":", "\u005c\u005c\u005c:");
                }
                else { MessageBoxes.MessageCustomSubtitleHardCodeNotSupported(); }
            }
            if (CheckBoxSubFourBurn.IsChecked == true && subtitleTrackFour)
            {
                string ext = Path.GetExtension(TextBoxSubtitleTrackThree.Text);
                if (ext == ".ass" || ext == ".ssa")
                {
                    subtitleFfmpegCommand = "-vf ass=" + '\u0022' + TextBoxSubtitleTrackFour.Text + '\u0022';
                    subtitleFfmpegCommand = subtitleFfmpegCommand.Replace("\u005c", "\u005c\u005c\u005c\u005c");
                    subtitleFfmpegCommand = subtitleFfmpegCommand.Replace(":", "\u005c\u005c\u005c:");
                    SmallFunctions.Logging("Subtitle Hardcoding Parameters: " + subtitleFfmpegCommand);
                }
                else if (ext == ".srt")
                {
                    subtitleFfmpegCommand = "-vf subtitles=" + '\u0022' + TextBoxSubtitleTrackFour.Text + '\u0022';
                    subtitleFfmpegCommand = subtitleFfmpegCommand.Replace("\u005c", "\u005c\u005c\u005c\u005c");
                    subtitleFfmpegCommand = subtitleFfmpegCommand.Replace(":", "\u005c\u005c\u005c:");
                }
                else { MessageBoxes.MessageCustomSubtitleHardCodeNotSupported(); }
            }

        }

        //═══════════════════════════════════════ Functions ═══════════════════════════════════════

        private string setChangeFramerate()
        {
            switch(ComboBoxFrameRate.SelectedIndex)
            {
                case 1: return "5"; case 2: return "10";
                case 3: return "12"; case 4: return "15";
                case 5: return "20"; case 6: return "24000/1001";
                case 7: return "24"; case 8: return "25";
                case 9: return "30000/1001"; case 10: return "30";
                case 11: return "48"; case 12: return "50";
                case 13: return "60000/1001"; case 14: return "60";
                case 15: return "120";
                default: return frameRateTemp.ToString();
            }
        }

        private void setChunkLength()
        {
            if (CheckBoxChunkLengthAutoCalculation.IsChecked == true) { TextBoxChunkLength.Text = (int.Parse(ffprobe.GetVideoLength(videoInput)) / int.Parse(ComboBoxWorkers.Text)).ToString(); }
            TextBoxTrimEnd.Text = ffprobe.GetVideoLengthAccurate(videoInput);
            trimEndTemp = TextBoxTrimEnd.Text;
            trimEndTempMax = TextBoxTrimEnd.Text;
        }

        private void getVideoInformation()
        {
            string frameRate = ffprobe.GetFrameRate(videoInput);
            string pixelFormat = ffprobe.GetPixelFormat(videoInput);
            fileName = SmallFunctions.getFilename(videoInput);
            SmallFunctions.Logging("Video Framerate: " + frameRate);
            setFrameRate(frameRate);
            SmallFunctions.Logging("Video Pixelformat: " + pixelFormat);
            setPixelFormat(pixelFormat);
            setChunkLength();
            if (CheckBoxBatchEncoding.IsChecked == false)
                LabelVideoSource.Content = fileName + "  |  " + frameRate + " FPS  |  " + pixelFormat;
            GetSubtitleTracks();
        }

        private void getCoreCount()
        {
            //Gets CoreCount of Hostmachine
            foreach (var item in new System.Management.ManagementObjectSearcher("Select * from Win32_Processor").Get()) { coreCount += int.Parse(item["NumberOfCores"].ToString()); }
            for (int i = 1; i <= coreCount; i++) { ComboBoxWorkers.Items.Add(i); }
            ComboBoxWorkers.SelectedItem = coreCount;
            SmallFunctions.Logging("System Core Count: " + coreCount);
        }

        private void getAudioInformation()
        {
            Process getAudioIndexes = new Process();
            getAudioIndexes.StartInfo = new ProcessStartInfo()
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                WorkingDirectory = ffprobePath,
                Arguments = "/C ffprobe.exe -i " + '\u0022' + videoInput + '\u0022' + " -loglevel error -select_streams a -show_streams -show_entries stream=index:tags=:disposition= -of csv",
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            getAudioIndexes.Start();
            //Reads the Console Output
            string audioIndexes = getAudioIndexes.StandardOutput.ReadToEnd();
            SmallFunctions.Logging("Audio Indexes ffprobe: " + audioIndexes);
            //Splits the Console Output
            string[] audioIndexesFixed = audioIndexes.Split(new string[] { " ", "stream," }, StringSplitOptions.RemoveEmptyEntries);
            int detectedTracks = 0;
            bool trackone = false, tracktwo = false, trackthree = false, trackfour = false;
            foreach (var item in audioIndexesFixed)
            {
                switch (detectedTracks)
                {
                    case 0: trackone = true; break;
                    case 1: tracktwo = true; break;
                    case 2: trackthree = true; break;
                    case 3: trackfour = true; break;
                    default: break;
                }
                detectedTracks += 1;
            }
            getAudioIndexes.WaitForExit();
            if (trackone == false) { CheckBoxAudioTrackOne.IsChecked = false; CheckBoxAudioTrackOne.IsEnabled = false; } else { CheckBoxAudioTrackOne.IsEnabled = true; CheckBoxAudioTrackOne.IsChecked = true; }
            if (tracktwo == false) { CheckBoxAudioTrackTwo.IsChecked = false; CheckBoxAudioTrackTwo.IsEnabled = false; } else { CheckBoxAudioTrackTwo.IsEnabled = true; CheckBoxAudioTrackTwo.IsChecked = true; }
            if (trackthree == false) { CheckBoxAudioTrackThree.IsChecked = false; CheckBoxAudioTrackThree.IsEnabled = false; } else { CheckBoxAudioTrackThree.IsEnabled = true; CheckBoxAudioTrackThree.IsChecked = true; }
            if (trackfour == false) { CheckBoxAudioTrackFour.IsChecked = false; CheckBoxAudioTrackFour.IsEnabled = false; } else { CheckBoxAudioTrackFour.IsEnabled = true; CheckBoxAudioTrackFour.IsChecked = true; }
            if (CheckBoxAudioTrackOne.IsEnabled == false && CheckBoxAudioTrackTwo.IsEnabled == false && CheckBoxAudioTrackThree.IsEnabled == false && CheckBoxAudioTrackFour.IsEnabled == false) { CheckBoxAudioEncoding.IsChecked = false; CheckBoxAudioEncoding.IsEnabled = false; }
            else { CheckBoxAudioEncoding.IsEnabled = true; CheckBoxAudioEncoding.IsChecked = true; }
            if (ffprobe.GetAudioInfo(videoInput) == "pcm_bluray") { MessageBoxes.MessagePCMBluray(); pcmBluray = true; } else { pcmBluray = false; }
            GetAudioLanguage(videoInput);
        }

        private void GetAudioLanguage(string videoInput)
        {
            //This function gets the Audio Languages from ffprobe and sets the ComboBoxes in the Audio Tab
            Process getAudioLang = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    WorkingDirectory = ffprobePath,
                    Arguments = "/C ffprobe.exe -i " + '\u0022' + videoInput + '\u0022' + " -v error -select_streams a -show_entries stream=index:stream_tags=language -of csv=p=0",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            getAudioLang.Start();
            string audio = getAudioLang.StandardOutput.ReadToEnd();
            string[] audioLanguages = audio.Split(new string[] { "1", "2", "3", "4", "," }, StringSplitOptions.RemoveEmptyEntries);
            audioLanguages = audioLanguages.Where(x => !string.IsNullOrEmpty(x)).ToArray();
            getAudioLang.WaitForExit();
            int index = 0;
            foreach (string line in audioLanguages)
            {
                string resultcropped;
                try { resultcropped = line.Replace(" ", "").Substring(0, 3); }
                catch { resultcropped = "und"; }
                
                int indexLang;
                switch (resultcropped)
                {
                    case "eng": indexLang = 1; break;
                    case "deu": indexLang = 2; break;
                    case "fre": indexLang = 3; break;
                    case "ita": indexLang = 4; break;
                    case "spa": indexLang = 5; break;
                    case "jpn": indexLang = 6; break;
                    case "chi": indexLang = 7; break;
                    case "kor": indexLang = 8; break;
                    default: indexLang = 0; break;
                }
                if (index == 0) { ComboBoxTrackOneLanguage.SelectedIndex = indexLang; }
                if (index == 1) { ComboBoxTrackTwoLanguage.SelectedIndex = indexLang; }
                if (index == 2) { ComboBoxTrackThreeLanguage.SelectedIndex = indexLang; }
                if (index == 3) { ComboBoxTrackFourLanguage.SelectedIndex = indexLang; }
                index += 1;
            }

        }

        public void GetSubtitleTracks()
        {
            //This function gets subtitle information
            Process getSubtitles = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    WorkingDirectory = MainWindow.ffprobePath,
                    Arguments = "/C ffprobe.exe -i " + '\u0022' + videoInput + '\u0022' + " -v error -select_streams s -show_entries stream=codec_name:stream_tags=language -of csv=p=0",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            getSubtitles.Start();
            string subs = getSubtitles.StandardOutput.ReadToEnd();
            getSubtitles.WaitForExit();

            //Splits the output from ffprobe
            var result = subs.Split('\n').Where(x => !string.IsNullOrEmpty(x)).ToArray();

            tempPath = "NEAV1E\\" + fileName + "\\Subitles\\";
            if (CheckBoxCustomTempPath.IsChecked == true) { tempPath = Path.Combine(TextBoxCustomTempPath.Text, tempPath); }
            else { tempPath = Path.Combine(Path.GetTempPath(), tempPath); }
            SmallFunctions.checkCreateFolder(tempPath);

            int a = 0;
            int b = 0;
            //Iterates over the lines from the splitted output
            foreach (var line in result)
            {
                if (line.Contains("hdmv_pgs_subtitle") || line.Contains("ass") || line.Contains("ssa") || line.Contains("subrip"))
                {
                    string tempName = "";
                    Process process = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo.UseShellExecute = true;
                    startInfo.FileName = "cmd.exe";
                    startInfo.WorkingDirectory = ffmpegPath + "\\";

                    if (line.Contains("hdmv_pgs_subtitle"))
                    {
                        startInfo.Arguments = "/C ffmpeg.exe -y -i " + '\u0022' + videoInput + '\u0022' + " -map 0:s:" + a + " -c:s copy " + '\u0022' + Path.Combine(tempPath, "pgs_" + b + ".sup") + '\u0022';
                        tempName = Path.Combine(tempPath, "pgs_" + b + ".sup");
                    }
                    else if (line.Contains("ass"))
                    {
                        startInfo.Arguments = "/C ffmpeg.exe -y -i " + '\u0022' + videoInput + '\u0022' + " -map 0:s:" + a + " -c:s copy " + '\u0022' + Path.Combine(tempPath, "ass_" + b + ".ass") + '\u0022';
                        tempName = Path.Combine(tempPath, "ass_" + b + ".ass");
                    }
                    else if (line.Contains("subrip"))
                    {
                        startInfo.Arguments = "/C ffmpeg.exe -y -i " + '\u0022' + videoInput + '\u0022' + " -map 0:s:" + a + " -c:s copy " + '\u0022' + Path.Combine(tempPath, "subrip_" + b + ".srt") + '\u0022';
                        tempName = Path.Combine(tempPath, "subrip_" + b + ".srt");
                    }
                    else if (line.Contains("ssa"))
                    {
                        startInfo.Arguments = "/C ffmpeg.exe -y -i " + '\u0022' + videoInput + '\u0022' + " -map 0:s:" + a + " -c:s copy " + '\u0022' + Path.Combine(tempPath, "ssa_" + b + ".ssa") + '\u0022';
                        tempName = Path.Combine(tempPath, "ssa_" + b + ".ssa");
                    }

                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();

                    string resultcropped = line.Substring(line.LastIndexOf(',') + 1).Substring(0, 3);
                    int indexLang;
                    switch (resultcropped)
                    {
                        case "eng": indexLang = 1; break;
                        case "deu": indexLang = 2; break;
                        case "ger": indexLang = 2; break;
                        case "fre": indexLang = 3; break;
                        case "ita": indexLang = 4; break;
                        case "spa": indexLang = 5; break;
                        case "jpn": indexLang = 6; break;
                        case "chi": indexLang = 7; break;
                        case "kor": indexLang = 8; break;
                        default: indexLang = 0; break;
                    }

                    //Sets the TextBoxes
                    if (b == 0) { TextBoxSubtitleTrackOne.Text = tempName; ComboBoxSubTrackOneLanguage.SelectedIndex = indexLang; CheckBoxSubtitleActivatedOne.IsChecked = true; }
                    if (b == 1) { TextBoxSubtitleTrackTwo.Text = tempName; ComboBoxSubTrackTwoLanguage.SelectedIndex = indexLang; CheckBoxSubtitleActivatedTwo.IsChecked = true; }
                    if (b == 2) { TextBoxSubtitleTrackThree.Text = tempName; ComboBoxSubTrackThreeLanguage.SelectedIndex = indexLang; CheckBoxSubtitleActivatedThree.IsChecked = true; }
                    if (b == 3) { TextBoxSubtitleTrackFour.Text = tempName; ComboBoxSubTrackFourLanguage.SelectedIndex = indexLang; CheckBoxSubtitleActivatedFour.IsChecked = true; }
                    b++;
                }
                else
                {
                    SmallFunctions.Logging("Unsupported Subtitle: " + line);
                }
                a++;
            }
            if (b >= 4)
            {
                MessageBoxes.MessageMoreSubtitles();
            }
            if (!Directory.EnumerateFiles(tempPath).Any()) { try { Directory.Delete(tempPath); }catch(Exception ex) { SmallFunctions.Logging(ex.Message); }}
        }

        private void setPixelFormat(string pixelFormat)
        {
            switch (pixelFormat)
            {
                case "yuv420p10le": ComboBoxBitDepth.SelectedIndex = 1; break;
                case "yuv420p12le": ComboBoxBitDepth.SelectedIndex = 2; break;
                case "yuv422p": ComboBoxChromaSubsamplingAomenc.SelectedIndex = 1; ComboBoxColorFormatRav1e.SelectedIndex = 1;
                    ComboBoxColorFormatSVT.SelectedIndex = 1; ComboBoxColorFormatLibaom.SelectedIndex = 1; break;
                case "yuv422p10le": ComboBoxChromaSubsamplingAomenc.SelectedIndex = 1; ComboBoxColorFormatRav1e.SelectedIndex = 1;
                    ComboBoxColorFormatSVT.SelectedIndex = 1; ComboBoxColorFormatLibaom.SelectedIndex = 1;
                    ComboBoxBitDepth.SelectedIndex = 1; break;
                case "yuv422p12le": ComboBoxChromaSubsamplingAomenc.SelectedIndex = 1; ComboBoxColorFormatRav1e.SelectedIndex = 1;
                    ComboBoxColorFormatSVT.SelectedIndex = 1; ComboBoxColorFormatLibaom.SelectedIndex = 1;
                    ComboBoxBitDepth.SelectedIndex = 2; break;
                case "yuv444p": ComboBoxChromaSubsamplingAomenc.SelectedIndex = 2; ComboBoxColorFormatRav1e.SelectedIndex = 2;
                    ComboBoxColorFormatSVT.SelectedIndex = 2; ComboBoxColorFormatLibaom.SelectedIndex = 2; break;
                case "yuv444p10le": ComboBoxChromaSubsamplingAomenc.SelectedIndex = 2; ComboBoxColorFormatRav1e.SelectedIndex = 2;
                    ComboBoxColorFormatSVT.SelectedIndex = 2; ComboBoxColorFormatLibaom.SelectedIndex = 2;
                    ComboBoxBitDepth.SelectedIndex = 1; break;
                case "yuv444p12le": ComboBoxChromaSubsamplingAomenc.SelectedIndex = 2; ComboBoxColorFormatRav1e.SelectedIndex = 2;
                    ComboBoxColorFormatSVT.SelectedIndex = 2; ComboBoxColorFormatLibaom.SelectedIndex = 2;
                    ComboBoxBitDepth.SelectedIndex = 2; break;
                default: break;
            }
        }

        private void setFrameRate(string frameRate)
        {
            //Sets the Combobox Framerate
            switch (frameRate)
            {
                case "5/1": frameRateIndex = 1; break;
                case "10/1": frameRateIndex = 2; break;
                case "12/1": frameRateIndex = 3; break;
                case "15/1": frameRateIndex = 4; break;
                case "20/1": frameRateIndex = 5; break;
                case "24000/1001": frameRateIndex = 6; break;
                case "24/1": frameRateIndex = 7; break;
                case "25/1": frameRateIndex = 8; break;
                case "30000/1001": frameRateIndex = 9; break;
                case "30/1": frameRateIndex = 10; break;
                case "48/1": frameRateIndex = 11; break;
                case "50/1": frameRateIndex = 12; break;
                case "60000/1001": frameRateIndex = 13; break;
                case "60/1": frameRateIndex = 14; break;
                case "120/1": frameRateIndex = 15; break;
                default: frameRateIndex = 0; break;
            }
            switch (frameRate)
            {
                case "5/1": ComboBoxFrameRate.SelectedIndex = 1; break;
                case "10/1": ComboBoxFrameRate.SelectedIndex = 2; break;
                case "12/1": ComboBoxFrameRate.SelectedIndex = 3; break;
                case "15/1": ComboBoxFrameRate.SelectedIndex = 4; break;
                case "20/1": ComboBoxFrameRate.SelectedIndex = 5; break;
                case "24000/1001": ComboBoxFrameRate.SelectedIndex = 6; break;
                case "24/1": ComboBoxFrameRate.SelectedIndex = 7; break;
                case "25/1": ComboBoxFrameRate.SelectedIndex = 8; break;
                case "30000/1001": ComboBoxFrameRate.SelectedIndex = 9; break;
                case "30/1": ComboBoxFrameRate.SelectedIndex = 10; break;
                case "48/1": ComboBoxFrameRate.SelectedIndex = 11; break;
                case "50/1": ComboBoxFrameRate.SelectedIndex = 12; break;
                case "60000/1001": ComboBoxFrameRate.SelectedIndex = 13; break;
                case "60/1": ComboBoxFrameRate.SelectedIndex = 14; break;
                case "120/1": ComboBoxFrameRate.SelectedIndex = 15; break;
                default: ComboBoxFrameRate.SelectedIndex = 0; break;
            }
            videoFrameRate = Convert.ToDouble(SmallFunctions.FractionToDouble(frameRate), CultureInfo.InvariantCulture); //for eta calculation
            frameRateTemp = frameRate; //For Splitting
            //frameRateIndex = ComboBoxFrameRate.SelectedIndex;
        }

        private void setProgressBarLabel(string Text)
        {
            LabelProgressbar.Content = Text;
        }

        private void setProgressBar(int Value)
        {
            ProgressBar.Maximum = Value;
        }

        private void CancelRoutine()
        {
            ButtonCancelEncode.BorderBrush = Brushes.Red;
            ButtonStartEncode.BorderBrush = new SolidColorBrush(Color.FromRgb(228, 228, 228));
            buttonActive = true;
            ProgressBar.Foreground = Brushes.Red;
            ProgressBar.Maximum = 100;
            ProgressBar.Value = 100;
            LabelProgressbar.Content = "Cancelled";
            SmallFunctions.Logging("CancelRoutine()");
        }

        private void SetBackgroundColorBlack()
        {
            //Sets the Background to dark and or dark transparent
            SolidColorBrush white = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            SolidColorBrush dark = new SolidColorBrush(Color.FromRgb(33, 33, 33));
            SolidColorBrush darker = new SolidColorBrush(Color.FromRgb(25, 25, 25));
            if (customBackground != true)
            {
                Window.Background = darker;
                TabControl.Background = dark;
                TabGrid.Background = dark;
                TextBoxChunkLength.Background = new SolidColorBrush(Color.FromRgb(44, 44, 44));
                ProgressBar.Background = new SolidColorBrush(Color.FromRgb(25, 25, 25));
            }
            LabelPresets.Foreground = white;
            CheckBoxResumeMode.Foreground = white;
            TextBlockOpenSource.Foreground = new SolidColorBrush(Color.FromRgb(240, 240, 240));
            GroupBox.BorderBrush = darker;
            GroupBox1.BorderBrush = darker;
            GroupBox2.BorderBrush = darker;
            GroupBox3.BorderBrush = darker;
        }

        private void SetBackgroundColorWhite()
        {
            //Sets the Background to white and or white transparent
            SolidColorBrush white = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            SolidColorBrush black = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            SolidColorBrush border = new SolidColorBrush(Color.FromRgb(213, 223, 229));
            if (customBackground != true)
            {
                Window.Background = white;
                TabControl.Background = white;
                TabGrid.Background = white;
                TextBoxChunkLength.Background = white;
                ProgressBar.Background = new SolidColorBrush(Color.FromRgb(230, 230, 230));
            }
            LabelPresets.Foreground = black;
            CheckBoxResumeMode.Foreground = black;
            TextBlockOpenSource.Foreground = new SolidColorBrush(Color.FromRgb(21, 65, 126));
            GroupBox.BorderBrush = border;
            GroupBox1.BorderBrush = border;
            GroupBox2.BorderBrush = border;
            GroupBox3.BorderBrush = border;
        }

        private void SetBackground()
        {
            if (CheckBoxDarkMode.IsChecked == true && customBackground)
            {
                SolidColorBrush transparentBlack = new SolidColorBrush(Color.FromArgb(65, 30, 30, 30));
                TabControl.Background = transparentBlack;
                TabGrid.Background = transparentBlack;
                TabGrid1.Background = transparentBlack;
                TabGrid2.Background = transparentBlack;
                TabGrid3.Background = transparentBlack;
                TabGrid4.Background = transparentBlack;
                TabGrid6.Background = transparentBlack;
                TextBoxChunkLength.Background = transparentBlack;
                ProgressBar.Background = transparentBlack;
            }
            else if (customBackground)
            {
                SolidColorBrush transparentWhite = new SolidColorBrush(Color.FromArgb(65, 100, 100, 100));
                TabControl.Background = transparentWhite;
                TabGrid.Background = transparentWhite;
                TabGrid1.Background = transparentWhite;
                TabGrid2.Background = transparentWhite;
                TabGrid3.Background = transparentWhite;
                TabGrid4.Background = transparentWhite;
                TabGrid6.Background = transparentWhite;
                TextBoxChunkLength.Background = transparentWhite;
                ProgressBar.Background = transparentWhite;
            }
        }

        private void AddToQueue()
        {
            SmallFunctions.checkCreateFolder(Path.Combine(Directory.GetCurrentDirectory(), "Queue"));
            if (ListBoxQueue.Items.Contains(localFileName) == false) { SaveSettings(localFileName, false, false, true); ListBoxQueue.Items.Add(localFileName); }
            else { localFileName += counterQueue; counterQueue += 1; AddToQueue(); }
        }

        private void FreeSpace()
        {
            if (GetTotalFreeSpace("C:\\") < 53687091200 && CheckBoxCustomTempPath.IsChecked == false) //50GB
            {
                MessageBoxes.MessageSpaceOnDrive();
            }
            else if(CheckBoxCustomTempPath.IsChecked == true)
            {
                FileInfo f = new FileInfo(TextBoxCustomTempPath.Text);
                if (GetTotalFreeSpace(Path.GetPathRoot(f.FullName)) < 53687091200)
                { MessageBoxes.MessageSpaceOnDrive(); }
            }
        }

        private long GetTotalFreeSpace(string driveName)
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.Name == driveName) { return drive.AvailableFreeSpace; }
            }
            return -1;
        }

        private void setImagePreview()
        {
            if (trimButtons == true)
            {
                tempPath = "NEAV1E\\" + fileName + "\\";
                if (CheckBoxCustomTempPath.IsChecked == true) { tempPath = Path.Combine(TextBoxCustomTempPath.Text, tempPath); }
                else { tempPath = Path.Combine(Path.GetTempPath(), tempPath); }
                SmallFunctions.checkCreateFolder(tempPath);

                Process getStartFrame = new Process
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = "cmd.exe",
                        WorkingDirectory = ffmpegPath,
                        Arguments = "/C ffmpeg.exe -y -ss " + TextBoxTrimStart.Text + " -loglevel error -i " + '\u0022' + videoInput + '\u0022' + " -vframes 1 -vf scale=\"min(240\\, iw):-1" + '\u0022' + " -sws_flags neighbor -threads 4 " + '\u0022' + Path.Combine(tempPath, "start.jpg") + '\u0022',
                        RedirectStandardError = true,
                        RedirectStandardOutput = true
                    }
                };
                getStartFrame.Start();
                getStartFrame.WaitForExit();

                var uriSource = new Uri(Path.Combine(tempPath, "start.jpg"));
                BitmapImage imgTemp = new BitmapImage();
                imgTemp.BeginInit();
                imgTemp.CacheOption = BitmapCacheOption.OnLoad;
                imgTemp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                imgTemp.UriSource = uriSource;
                imgTemp.EndInit();
                ImagePreviewTrimStart.Source = imgTemp;

                if (trimEndTemp != TextBoxTrimEnd.Text || startupTrim == false)
                {
                    Process getEndFrame = new Process
                    {
                        StartInfo = new ProcessStartInfo()
                        {
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                            FileName = "cmd.exe",
                            WorkingDirectory = ffmpegPath,
                            Arguments = "/C ffmpeg.exe -y -ss " + TextBoxTrimEnd.Text + " -loglevel error -i " + '\u0022' + videoInput + '\u0022' + " -vframes 1 -vf scale=\"min(240\\, iw):-1" + '\u0022' + " -sws_flags neighbor -threads 4 " + '\u0022' + Path.Combine(tempPath, "end.jpg") + '\u0022',
                            RedirectStandardError = true,
                            RedirectStandardOutput = true
                        }
                    };
                    getEndFrame.Start();
                    getEndFrame.WaitForExit();
                    var uriSourceEnd = new Uri(Path.Combine(tempPath, "end.jpg"));
                    BitmapImage imgTempEnd = new BitmapImage();
                    imgTempEnd.BeginInit();
                    imgTempEnd.CacheOption = BitmapCacheOption.OnLoad;
                    imgTempEnd.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    imgTempEnd.UriSource = uriSourceEnd;
                    imgTempEnd.EndInit();
                    ImagePreviewTrimEnd.Source = imgTempEnd;
                    trimEndTemp = TextBoxTrimEnd.Text;
                }
                startupTrim = true;
            }           
        }

        private bool CheckFileType(string fileName)
        {
            string ext = Path.GetExtension(fileName);
            string[] exts = { ".mp4", ".m4v", ".mkv", ".webm", ".m2ts", ".flv", ".avi", ".wmv", ".ts", ".yuv", ".mov" };
            return exts.Contains(ext.ToLower());
        }

        private void CompareFrameCount()
        {
            if (frameCountChunks != frameCountSource)
            {
                if (MessageBox.Show("The Framecount is different! \n\nSource Framecount: " + frameCountSource + "\n\nChunk Framecount: " + frameCountChunks + "\n\nProceed anyway?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    SmallFunctions.Cancel.CancelAll = true;
                    CancelRoutine();
                }
            }
        }

        private string SubtitleFiledialog()
        {
            //Opens OpenFileDialog for subtitletracktwo
            OpenFileDialog openSubtitleFileDialog = new OpenFileDialog();
            openSubtitleFileDialog.Filter = "Subtitle Files|*.pgs;*.srt;*.sup;*.ass;*.ssa;|All Files|*.*";
            Nullable<bool> result = openSubtitleFileDialog.ShowDialog();
            if (result == true)
            {
                return openSubtitleFileDialog.FileName;
            }
            return null;
        }

        //════════════════════════════════════════ Buttons ════════════════════════════════════════

        private void ButtonUpdateDependencies_Click(object sender, RoutedEventArgs e)
        {
            if (found7z) {
                DownloadDependencies egg = new DownloadDependencies(CheckBoxDarkMode.IsChecked == true);
                egg.ShowDialog();
            } else { MessageBoxes.Message7zNotFound(); }
        }

        private void ButtonRemoveFromQueue_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "Queue", (string)ListBoxQueue.SelectedItem));
                ListBoxQueue.Items.RemoveAt(ListBoxQueue.SelectedIndex);
            } catch { }
        }

        private void ButtonQueue_Click(object sender, RoutedEventArgs e)
        {
            if (TabItemQueue.Visibility == Visibility.Visible) { TabItemQueue.Visibility = Visibility.Collapsed; }
            else{ TabItemQueue.Visibility = Visibility.Visible; }           
        }

        private void ButtonAddToQueue_Click(object sender, RoutedEventArgs e)
        {
            localFileName = fileName;
            AddToQueue();
        }

        private void ButtonSubtitleTrackOne_Click(object sender, RoutedEventArgs e)
        {
            subtitleTrackOnePath = SubtitleFiledialog();
            TextBoxSubtitleTrackOne.Text = subtitleTrackOnePath;
        }

        private void ButtonSubtitleTrackTwo_Click(object sender, RoutedEventArgs e)
        {
            subtitleTrackTwoPath = SubtitleFiledialog();
            TextBoxSubtitleTrackTwo.Text = subtitleTrackTwoPath;
        }

        private void ButtonSubtitleTrackThree_Click(object sender, RoutedEventArgs e)
        {
            subtitleTrackThreePath = SubtitleFiledialog();
            TextBoxSubtitleTrackThree.Text = subtitleTrackThreePath;
        }

        private void ButtonSubtitleTrackFour_Click(object sender, RoutedEventArgs e)
        {
            subtitleTrackFourPath = SubtitleFiledialog();
            TextBoxSubtitleTrackFour.Text = subtitleTrackFourPath;
        }

        private void ButtonOpenProgramFolder_Click(object sender, RoutedEventArgs e)
        {
            try { Process.Start(Directory.GetCurrentDirectory()); } catch { }            
        }

        private void SliderPreset_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (CheckBoxRealtimeMode != null)
            {
                if (ComboBoxEncoder.SelectedIndex == 0) //libaom does not work with realtime mode, I don't know why
                {
                    if (SliderPreset.Value == 5 || SliderPreset.Value == 6 || SliderPreset.Value == 7 || SliderPreset.Value == 8 || SliderPreset.Value == 9)
                    {
                        CheckBoxRealtimeMode.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        CheckBoxRealtimeMode.Visibility = Visibility.Collapsed;
                        CheckBoxRealtimeMode.IsChecked = false;
                    }
                }
                else
                {
                    CheckBoxRealtimeMode.Visibility = Visibility.Collapsed;
                    CheckBoxRealtimeMode.IsChecked = false;
                }
            }
        }

        private void ButtonOpenTempFolder_Click(object sender, RoutedEventArgs e)
        {
            //Creates the temp directoy if not existent
            if (Directory.Exists(Path.Combine(Path.GetTempPath(), "NEAV1E")) == false) { Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "NEAV1E")); }
            if (CheckBoxCustomTempPath.IsChecked == false) { Process.Start(Path.Combine(Path.GetTempPath(), "NEAV1E")); }
            else { Process.Start(TextBoxCustomTempPath.Text); }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            encoder = ComboBoxEncoder.Text;
            setEncoderParameters(true);
            string inputSet = "Error";
            if (encoder == "aomenc" || encoder == "libaom") { inputSet = allSettingsAom; }
            if (encoder == "rav1e") { inputSet = allSettingsRav1e; }
            if (encoder == "svt-av1") { inputSet = allSettingsSVTAV1; }
            if (encoder == "libvpx-vp9") { inputSet = allSettingsVP9; }
            ShowSettings kappa = new ShowSettings(inputSet, CheckBoxDarkMode.IsChecked == true);
            kappa.Show();
        }

        private void ButtonSaveVideo_Click(object sender, RoutedEventArgs e)
        {
            if (buttonActive)
            {
                SmallFunctions.Logging("Button Save Video");
                if (CheckBoxBatchEncoding.IsChecked == false)
                {
                    SaveFileDialog saveVideoFileDialog = new SaveFileDialog();
                    saveVideoFileDialog.Filter = "Video|*.mkv;*.webm;*.mp4";
                    Nullable<bool> result = saveVideoFileDialog.ShowDialog();
                    if (result == true) { videoOutput = saveVideoFileDialog.FileName; outputSet = true; LabelVideoOutput.Content = videoOutput; SmallFunctions.Logging("Video Output: " + videoOutput); }
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
        }

        private void ButtonOpenSource_Click(object sender, RoutedEventArgs e)
        {
            if (buttonActive)
            {
                SmallFunctions.Logging("Button Open Video");
                if (CheckBoxBatchEncoding.IsChecked == false)
                {
                    //Opens OpenFileDialog for the Input Video
                    OpenFileDialog openVideoFileDialog = new OpenFileDialog();
                    openVideoFileDialog.Filter = "Video Files|*.mp4;*.m4v;*.mkv;*.webm;*.m2ts;*.flv;*.mov;*.avi;*.wmv;*.ts;*.yuv|All Files|*.*";
                    Nullable<bool> result = openVideoFileDialog.ShowDialog();
                    if (result == true)
                    {
                        videoInput = openVideoFileDialog.FileName;
                        
                        SmallFunctions.Logging("Video Input: " + videoInput);
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
        }

        private void ButtonSavePreset_Click(object sender, RoutedEventArgs e)
        {
            SavePreset kappa = new SavePreset(CheckBoxDarkMode.IsChecked == true);
            kappa.ShowDialog();
            if (saveSettings)
            {
                SaveSettings(saveSettingString, true, false, false);
                saveSettings = false;
                saveSettingString = null;
                LoadPresetsIntoComboBox();
            }
        }

        private void ButtonStartEncode_Click(object sender, RoutedEventArgs e)
        {
            if (encodeStarted == false)
            {
                PreStart();
            }
            else
            {
                if (MessageBox.Show("Encode already started. \n\nStart process anyway?", "Encode already started!", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    encodeStarted = false;
                    PreStart();
                }
            }
        }

        private void ButtonTrimMinus_Click(object sender, RoutedEventArgs e)
        {
            if (DateTime.ParseExact(TextBoxTrimStart.Text, "HH:mm:ss.fff", CultureInfo.InvariantCulture) > DateTime.ParseExact("00:00:00.999", "HH:mm:ss.fff", CultureInfo.InvariantCulture))
            {
                TextBoxTrimStart.Text = DateTime.ParseExact(TextBoxTrimStart.Text, "HH:mm:ss.fff", CultureInfo.InvariantCulture).AddSeconds(-1).ToString("HH:mm:ss.fff");
            }
            else { TextBoxTrimStart.Text = "00:00:00.000"; }      
        }

        private void ButtonTrimMinusEnd_Click(object sender, RoutedEventArgs e)
        {
            TextBoxTrimEnd.Text = DateTime.ParseExact(TextBoxTrimEnd.Text, "HH:mm:ss.fff", CultureInfo.InvariantCulture).AddSeconds(-1).ToString("HH:mm:ss.fff");
        }

        private void ButtonTrimPlusEnd_Click(object sender, RoutedEventArgs e)
        {
            if (DateTime.ParseExact(TextBoxTrimEnd.Text, "HH:mm:ss.fff", CultureInfo.InvariantCulture) < DateTime.ParseExact(trimEndTempMax, "HH:mm:ss.fff", CultureInfo.InvariantCulture))
            {
                TextBoxTrimEnd.Text = DateTime.ParseExact(TextBoxTrimEnd.Text, "HH:mm:ss.fff", CultureInfo.InvariantCulture).AddSeconds(1).ToString("HH:mm:ss.fff");
            }
            else { TextBoxTrimEnd.Text = trimEndTempMax; }                           
        }

        private void ButtonTrimPlus_Click(object sender, RoutedEventArgs e)
        {
            TextBoxTrimStart.Text = DateTime.ParseExact(TextBoxTrimStart.Text, "HH:mm:ss.fff", CultureInfo.InvariantCulture).AddSeconds(1).ToString("HH:mm:ss.fff");
            trimButtons = true;
        }

        private void ButtonCancelEncode_Click(object sender, RoutedEventArgs e)
        {
            SmallFunctions.Cancel.CancelAll = true;
            cancellationTokenSource.Cancel();
            SmallFunctions.KillInstances();
            CancelRoutine();
        }

        private void ButtonSetTempPath_Click(object sender, RoutedEventArgs e)
        {
            //Sets a custom Temp Folder
            System.Windows.Forms.FolderBrowserDialog browseTempFolder = new System.Windows.Forms.FolderBrowserDialog();
            if (browseTempFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK) { TextBoxCustomTempPath.Text = browseTempFolder.SelectedPath; SaveSettingsTab(); FreeSpace(); }
        }

        private void ButtonDeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                File.Delete("Profiles\\" + ComboBoxPresets.SelectedItem);
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
                    if (File.Exists("background.txt")) { File.Delete("background.txt"); }
                    SmallFunctions.WriteToFileThreadSafe(PathToBackground, "background.txt");
                }
            }
            catch { }
        }
        
        private void ButtonResetBackground_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists("background.txt")) { try { File.Delete("background.txt"); } catch { } }
            imgDynamic.Source = null;
            customBackground = false;
            if (CheckBoxDarkMode.IsChecked == true) { SetBackgroundColorBlack(); }
            else { SetBackgroundColorWhite(); }
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

        //═══════════════════════════════════════ CheckBoxes ══════════════════════════════════════

        private void CheckBoxSplitting_Checked(object sender, RoutedEventArgs e)
        {
            skipSplitting = false;
            SaveSettingsTab();
        }

        private void CheckBoxSplitting_Unchecked(object sender, RoutedEventArgs e)
        {
            skipSplitting = true;
            SaveSettingsTab();
        }

        private void CheckBoxSubOneBurn_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxSubTwoBurn.IsChecked = false;
            CheckBoxSubThreeBurn.IsChecked = false;
            CheckBoxSubFourBurn.IsChecked = false;
            CheckBoxSubOneDefault.IsChecked = false;
        }

        private void CheckBoxSubTwoBurn_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxSubOneBurn.IsChecked = false;
            CheckBoxSubThreeBurn.IsChecked = false;
            CheckBoxSubFourBurn.IsChecked = false;
            CheckBoxSubTwoDefault.IsChecked = false;
        }

        private void CheckBoxSubThreeBurn_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxSubOneBurn.IsChecked = false;
            CheckBoxSubTwoBurn.IsChecked = false;
            CheckBoxSubFourBurn.IsChecked = false;
            CheckBoxSubThreeDefault.IsChecked = false;
        }

        private void CheckBoxSubFourBurn_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxSubOneBurn.IsChecked = false;
            CheckBoxSubTwoBurn.IsChecked = false;
            CheckBoxSubThreeBurn.IsChecked = false;
            CheckBoxSubFourDefault.IsChecked = false;
        }

        private void CheckBoxSubOneDefault_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxSubTwoDefault.IsChecked = false;
            CheckBoxSubThreeDefault.IsChecked = false;
            CheckBoxSubFourDefault.IsChecked = false;
            CheckBoxSubOneBurn.IsChecked = false;
        }

        private void CheckBoxSubTwoDefault_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxSubOneDefault.IsChecked = false;
            CheckBoxSubThreeDefault.IsChecked = false;
            CheckBoxSubFourDefault.IsChecked = false;
            CheckBoxSubTwoBurn.IsChecked = false;
        }

        private void CheckBoxSubThreeDefault_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxSubOneDefault.IsChecked = false;
            CheckBoxSubTwoDefault.IsChecked = false;
            CheckBoxSubFourDefault.IsChecked = false;
            CheckBoxSubThreeBurn.IsChecked = false;
        }

        private void CheckBoxSubFourDefault_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxSubOneDefault.IsChecked = false;
            CheckBoxSubTwoDefault.IsChecked = false;
            CheckBoxSubThreeDefault.IsChecked = false;
            CheckBoxSubFourBurn.IsChecked = false;
        }

        private void CheckBoxCheckFrameCount_Checked(object sender, RoutedEventArgs e)
        {
            SaveSettingsTab();
            if (CheckBoxTrimming.IsChecked == true)
            {
                MessageBoxes.MessageCountNotAvailableWhenTrimming();
                CheckBoxCheckFrameCount.IsChecked = false;
            }
        }

        private void CheckBoxCheckFrameCount_Unchecked(object sender, RoutedEventArgs e)
        {
            SaveSettingsTab();
        }

        private void CheckBoxCustomTempPath_Checked(object sender, RoutedEventArgs e)
        {
            SaveSettingsTab();
        }

        private void CheckBoxCustomTempPath_Unchecked(object sender, RoutedEventArgs e)
        {
            SaveSettingsTab();
        }

        private void CheckBoxEncodingTerminal_Checked(object sender, RoutedEventArgs e)
        {
            SaveSettingsTab();
        }

        private void CheckBoxTrimming_Unchecked(object sender, RoutedEventArgs e)
        {
            trimButtons = false;
            ImagePreviewTrimStart.Source = null;
            ImagePreviewTrimEnd.Source = null;
        }

        private void CheckBoxTrimming_Checked(object sender, RoutedEventArgs e)
        {
            trimButtons = true;
            try { setVideoLengthTrimmed(); }
            catch { }
            if (CheckBoxCheckFrameCount.IsChecked == true) { CheckBoxCheckFrameCount.IsChecked = false; MessageBoxes.MessageCountNotAvailableWhenTrimming(); }
        }

        private void CheckBoxWorkerLimit_Checked(object sender, RoutedEventArgs e)
        {
            SaveSettingsTab();
        }

        private void CheckBoxFinishedSound_Checked(object sender, RoutedEventArgs e)
        {
            SaveSettingsTab();
        }

        private void CheckBoxDeleteTempFiles_Checked(object sender, RoutedEventArgs e)
        {
            SaveSettingsTab();
        }

        private void CheckBoxDeleteTempFiles_Unchecked(object sender, RoutedEventArgs e)
        {
            SaveSettingsTab();
        }

        private void CheckBoxDeleteTempFilesDynamically_Checked(object sender, RoutedEventArgs e)
        {
            SaveSettingsTab();
        }

        private void CheckBoxShutdownAfterEncode_Checked(object sender, RoutedEventArgs e)
        {
            SaveSettingsTab();
        }

        private void CheckBoxLogging_Unchecked(object sender, RoutedEventArgs e)
        {
            SaveSettingsTab();
            logging = false;
        }

        private void CheckBoxLogging_Checked(object sender, RoutedEventArgs e)
        {
            if (programStartup == false) { SaveSettingsTab(); }            
        }

        private void CheckBoxReencodeDuringSplitting_Unchecked(object sender, RoutedEventArgs e)
        {
            if (CheckBoxDeinterlaceYadif.IsChecked == true && CheckBoxReencodeBeforeSplitting.IsChecked == false)
            {
                MessageBoxes.MessageDeinterlacingWithoutReencoding();
            }
        }

        private void CheckBoxReencodeBeforeSplitting_Unchecked(object sender, RoutedEventArgs e)
        {
            if (CheckBoxDeinterlaceYadif.IsChecked == true && CheckBoxReencodeDuringSplitting.IsChecked == false)
            {
                MessageBoxes.MessageDeinterlacingWithoutReencoding();
            }
        }

        private void CheckBoxDeinterlaceYadif_Checked(object sender, RoutedEventArgs e)
        {
            if (CheckBoxReencodeBeforeSplitting.IsChecked == false && CheckBoxReencodeDuringSplitting.IsChecked == false)
            {
                CheckBoxReencodeDuringSplitting.IsChecked = true;
            }
        }

        private void CheckBoxBatchEncoding_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxSubOneBurn.IsChecked = false;
            CheckBoxSubTwoBurn.IsChecked = false;
            CheckBoxSubThreeBurn.IsChecked = false;
            CheckBoxSubFourBurn.IsChecked = false;
        }

        private void CheckBoxDarkMode_Checked(object sender, RoutedEventArgs e)
        {
            SetBackgroundColorBlack();
            SetBackground();
            SetBackgroundColorBlack();
            SaveSettingsTab();
        }

        private void CheckBoxDarkMode_UnChecked(object sender, RoutedEventArgs e)
        {
            SetBackgroundColorWhite();
            SetBackground();
            SaveSettingsTab();
        }

        private void CheckBoxCustomSettings_Checked(object sender, RoutedEventArgs e)
        {
            encoder = ComboBoxEncoder.Text;
            setEncoderParameters(true);
            string inputSet = "Error";
            if (encoder == "aomenc" || encoder == "libaom") { inputSet = allSettingsAom; }
            if (encoder == "rav1e") { inputSet = allSettingsRav1e; }
            if (encoder == "svt-av1") { inputSet = allSettingsSVTAV1; }
            if (encoder == "libvpx-vp9") { inputSet = allSettingsVP9; }
            TextBoxAdvancedSettings.Text = inputSet;
        }

        private void CheckBoxRealtimeMode_Checked(object sender, RoutedEventArgs e)
        {
            //Because aomenc does not support 2pass realtime mode
            if (ComboBoxPasses.SelectedIndex == 1) { ComboBoxPasses.SelectedIndex = 0; }
        }

        //═══════════════════════════════════════ ComboBoxes ══════════════════════════════════════

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
                        SliderPreset.Maximum = 9;
                        SliderPreset.Value = 4;
                    }
                    break;
                case "libaom":
                    if (SliderQuality != null)
                    {
                        SliderQuality.Maximum = 63;
                        SliderQuality.Value = 30;
                        SliderPreset.Maximum = 8; //Technically it should be 9, BUT it is still not implemented in ffmpeg
                        SliderPreset.Value = 4;
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
                        if (ComboBoxBitDepth.SelectedIndex == 2) { ComboBoxBitDepth.SelectedIndex = 1; }
                    }
                    break;
                case "libvpx-vp9":
                    if (SliderQuality != null)
                    {
                        SliderQuality.Maximum = 63;
                        SliderQuality.Value = 30;
                        SliderPreset.Value = 3;
                        SliderPreset.Maximum = 5;
                        if (CheckBoxWorkerLimit.IsChecked == false) { ComboBoxWorkers.SelectedIndex = 0; } //It's not necessary to have more than one Worker for SVT 
                        if (ComboBoxBitDepth.SelectedIndex == 2) { ComboBoxBitDepth.SelectedIndex = 1; }
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
                if (ComboBoxPresets.SelectedItem != null)
                {
                    LoadSettings(ComboBoxPresets.SelectedItem.ToString(), true, false, false);
                }else { }
            }
            catch { }
        }

        private void ComboBoxPasses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Due Rav1e Two Pass Still broken this will force one pass encoding
            if (ComboBoxEncoder.SelectedIndex == 2) { ComboBoxPasses.SelectedIndex = 0; }
            if (CheckBoxRealtimeMode != null)
            {
                if (CheckBoxRealtimeMode.IsChecked == true && ComboBoxPasses.SelectedIndex == 1) { ComboBoxPasses.SelectedIndex = 0; }
            }            
        }

        private void ComboBoxWorkers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBoxEncoder.SelectedIndex == 3 && ComboBoxWorkers.SelectedIndex != 0 && CheckBoxWorkerLimit.IsChecked == false) { ComboBoxWorkers.SelectedIndex = 0; MessageBoxes.MessageSVTWorkers(); }
        }

        private void ComboBoxFrameRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (inputSet == true && ComboBoxFrameRate.SelectedIndex != frameRateIndex)
            {
                if (CheckBoxReencodeBeforeSplitting.IsChecked == false && CheckBoxReencodeDuringSplitting.IsChecked == false && CheckBoxBatchEncoding.IsChecked == false && CheckBoxQueueEncoding.IsChecked == false)
                {
                    if (MessageBox.Show("Changing the Framerate requires reencoding! Activate Reencoding?", "Framerate", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    { CheckBoxReencodeDuringSplitting.IsChecked = true; }
                }
            }
        }

        //═══════════════════════════════════ Other UI Elements ═══════════════════════════════════

        private void TextBoxTrimEnd_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TextBoxTrimEnd != null) { setVideoLengthTrimmed(); }            
        }

        private void TextBoxChunkLength_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TextBoxChunkLength.Text == "0") { TextBoxChunkLength.Text = "1"; }
        }

        private void TextBoxAdvancedSettings_TextChanged(object sender, TextChangedEventArgs e)
        {
            string[] forbiddenWords = { "help", "cfg", "debug", "output", "passes", "pass", "fpf", "limit",
            "skip", "webm", "ivf", "obu", "q-hist", "rate-hist", "fullhelp", "benchmark", "first-pass", "second-pass",
            "reconstruction", "enc-mode-2p", "input-stat-file", "output-stat-file"};

            foreach (var words in forbiddenWords)
            {
                if (CheckBoxDarkMode.IsChecked == false) { TextBoxAdvancedSettings.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0)); }
                else { TextBoxAdvancedSettings.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255)); }

                if (TextBoxAdvancedSettings.Text.Contains(words)) { TextBoxAdvancedSettings.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0)); break; }
            }
        }

        private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double taskMax = ProgressBar.Maximum, taskVal = ProgressBar.Value;
            TaskbarItemInfo.ProgressValue = (1.0 / taskMax) * taskVal;
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.ToString());
        }

        private void MessageNoAudioOutput()
        {
            if (MessageBox.Show("No Audio Output detected! \nCancel?", "Error", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes) { SmallFunctions.Cancel.CancelAll = true; CancelRoutine(); }
            else { audioEncoding = false; }
        }

        //═════════════════════════════════ Save / Load Settings ══════════════════════════════════

        public void SaveSettings(string saveName, bool saveProfile, bool saveJob, bool saveQueue)
        {
            SmallFunctions.Logging("SaveSettings(): " + saveName + " Profile: " + saveProfile + " Job: " + saveJob);
            string directory = "";
            if (saveProfile) { directory = "Profiles\\" + saveName + ".xml"; }
            if (saveJob) { directory = "UnfinishedJobs\\" + saveName + ".xml"; }
            if (saveQueue) { directory = Path.Combine(Directory.GetCurrentDirectory(), "Queue", saveName + ".xml"); }
            XmlWriter writer = XmlWriter.Create(directory);
            writer.WriteStartElement("Settings");
            if (saveJob || saveQueue)
            {
                writer.WriteElementString("VideoInput",         videoInput);
                writer.WriteElementString("VideoInputFilename", fileName);
                writer.WriteElementString("VideoOutput",        videoOutput);
                writer.WriteElementString("Trimming",           CheckBoxTrimming.IsChecked.ToString());
                writer.WriteElementString("TrimStart",          TextBoxTrimStart.Text);
                writer.WriteElementString("TrimEnd",            TextBoxTrimEnd.Text);
                writer.WriteElementString("AudioLangOne",       ComboBoxTrackOneLanguage.SelectedIndex.ToString());
                writer.WriteElementString("AudioLangTwo",       ComboBoxTrackTwoLanguage.SelectedIndex.ToString());
                writer.WriteElementString("AudioLangThree",     ComboBoxTrackThreeLanguage.SelectedIndex.ToString());
                writer.WriteElementString("AudioLangFour",      ComboBoxTrackFourLanguage.SelectedIndex.ToString());
                writer.WriteElementString("Subtitles",          CheckBoxSubtitleEncoding.IsChecked.ToString());
                writer.WriteElementString("SubTrackOne",        CheckBoxSubtitleActivatedOne.IsChecked.ToString());
                writer.WriteElementString("SubTrackTwo",        CheckBoxSubtitleActivatedTwo.IsChecked.ToString());
                writer.WriteElementString("SubTrackThree",      CheckBoxSubtitleActivatedThree.IsChecked.ToString());
                writer.WriteElementString("SubTrackFour",       CheckBoxSubtitleActivatedFour.IsChecked.ToString());
                writer.WriteElementString("SubOnePath",         TextBoxSubtitleTrackOne.Text);
                writer.WriteElementString("SubTwoPath",         TextBoxSubtitleTrackTwo.Text);
                writer.WriteElementString("SubThreePath",       TextBoxSubtitleTrackThree.Text);
                writer.WriteElementString("SubFourPath",        TextBoxSubtitleTrackFour.Text);
                writer.WriteElementString("SubOneBurn",         CheckBoxSubOneBurn.IsChecked.ToString());
                writer.WriteElementString("SubTwoBurn",         CheckBoxSubOneBurn.IsChecked.ToString());
                writer.WriteElementString("SubThreeBurn",       CheckBoxSubThreeBurn.IsChecked.ToString());
                writer.WriteElementString("SubFourBurn",        CheckBoxSubFourBurn.IsChecked.ToString());
                writer.WriteElementString("SubOneDefault",      CheckBoxSubOneDefault.IsChecked.ToString());
                writer.WriteElementString("SubTwoDefault",      CheckBoxSubTwoDefault.IsChecked.ToString());
                writer.WriteElementString("SubThreeDefault",    CheckBoxSubThreeDefault.IsChecked.ToString());
                writer.WriteElementString("SubFourDefault",     CheckBoxSubFourDefault.IsChecked.ToString());
                writer.WriteElementString("SubOneLang",         ComboBoxSubTrackOneLanguage.SelectedIndex.ToString());
                writer.WriteElementString("SubTwoLang",         ComboBoxSubTrackTwoLanguage.SelectedIndex.ToString());
                writer.WriteElementString("SubThreeLang",       ComboBoxSubTrackThreeLanguage.SelectedIndex.ToString());
                writer.WriteElementString("SubFourLang",        ComboBoxSubTrackFourLanguage.SelectedIndex.ToString());
                writer.WriteElementString("SubOneTrackName",    TextBoxSubOneName.Text);
                writer.WriteElementString("SubTwoTrackName",    TextBoxSubTwoName.Text);
                writer.WriteElementString("SubThreeTrackName",  TextBoxSubThreeName.Text);
                writer.WriteElementString("SubFourTrackName",   TextBoxSubFourName.Text);
            }
            writer.WriteElementString("Encoder",            ComboBoxEncoder.SelectedIndex.ToString());
            writer.WriteElementString("Framerate",          ComboBoxFrameRate.SelectedIndex.ToString());
            writer.WriteElementString("BitDepth",           ComboBoxBitDepth.SelectedIndex.ToString());
            writer.WriteElementString("Preset",             SliderPreset.Value.ToString());
            writer.WriteElementString("QualityMode",        RadioButtonConstantQuality.IsChecked.ToString());
            writer.WriteElementString("Quality",            SliderQuality.Value.ToString());
            writer.WriteElementString("Bitrate",            TextBoxBitrate.Text);
            writer.WriteElementString("Passes",             ComboBoxPasses.SelectedIndex.ToString());
            writer.WriteElementString("RealTime",           CheckBoxRealtimeMode.IsChecked.ToString());
            writer.WriteElementString("Workers",            ComboBoxWorkers.SelectedIndex.ToString());
            writer.WriteElementString("Priority",           ComboBoxProcessPriority.SelectedIndex.ToString());
            writer.WriteElementString("ReencodeCodec",      ComboBoxReencodeCodec.SelectedIndex.ToString());
            writer.WriteElementString("Reencode",           CheckBoxReencodeDuringSplitting.IsChecked.ToString());
            writer.WriteElementString("PreReencode",        CheckBoxReencodeBeforeSplitting.IsChecked.ToString());
            writer.WriteElementString("ChunkCalc",          CheckBoxChunkLengthAutoCalculation.IsChecked.ToString());
            writer.WriteElementString("ChunkLength",        TextBoxChunkLength.Text);
            writer.WriteElementString("Crop",               CheckBoxCrop.IsChecked.ToString());
            writer.WriteElementString("CropTop",            TextBoxCropTop.Text);
            writer.WriteElementString("CropBottom",         TextBoxCropBottom.Text);
            writer.WriteElementString("CropLeft",           TextBoxCropLeft.Text);
            writer.WriteElementString("CropRight",          TextBoxCropRight.Text);
            writer.WriteElementString("Resize",             CheckBoxResize.IsChecked.ToString());
            writer.WriteElementString("ResizeWidth",        TextBoxImageWidth.Text);
            writer.WriteElementString("ResizeHeight",       TextBoxImageHeight.Text);
            writer.WriteElementString("ResizeFilter",       ComboBoxResizeFilters.SelectedIndex.ToString());
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
            writer.WriteElementString("Deinterlacing",      CheckBoxDeinterlaceYadif.IsChecked.ToString());
            writer.WriteElementString("Deinterlacer",       ComboBoxDeinterlace.SelectedIndex.ToString());
            writer.WriteElementString("AdvancedSettings",   CheckBoxAdvancedSettings.IsChecked.ToString());
            if (CheckBoxAdvancedSettings.IsChecked == true)
            {
                if (CheckBoxAdvancedSettings.IsChecked == true)
                {
                    writer.WriteElementString("CustomSettings",     CheckBoxCustomSettings.IsChecked.ToString());
                    writer.WriteElementString("CustomSettingsText", TextBoxAdvancedSettings.Text);
                }
                writer.WriteElementString("Threads",                ComboBoxThreadsAomenc.SelectedIndex.ToString());
                writer.WriteElementString("TileColumns",            ComboBoxTileColumns.SelectedIndex.ToString());
                writer.WriteElementString("TileRows",               ComboBoxTileRows.SelectedIndex.ToString());
                writer.WriteElementString("MinKeyframeInterval",    TextBoxMinKeyframeinterval.Text);
                writer.WriteElementString("MaxKeyframeInterval",    TextBoxMaxKeyframeinterval.Text);
                writer.WriteElementString("CommentHeader",          CheckBoxCommentHeaderSettings.IsChecked.ToString());

                if (ComboBoxEncoder.SelectedIndex == 0)
                {
                    writer.WriteElementString("LagInFrames",        TextBoxMaxLagInFrames.Text);
                    writer.WriteElementString("MaxRefFrames",       ComboBoxMaxReferenceFramesAomenc.SelectedIndex.ToString());
                    writer.WriteElementString("ColorPrimaries",     ComboBoxColorPrimariesAomenc.SelectedIndex.ToString());
                    writer.WriteElementString("ColorTransfer",      ComboBoxColorTransferAomenc.SelectedIndex.ToString());
                    writer.WriteElementString("ColorMatrix",        ComboBoxColorMatrixAomenc.SelectedIndex.ToString());
                    writer.WriteElementString("ChromaSubsampling",  ComboBoxChromaSubsamplingAomenc.SelectedIndex.ToString());
                    writer.WriteElementString("Tune",               ComboBoxTuneAomenc.SelectedIndex.ToString());
                    writer.WriteElementString("AQMode",             ComboBoxAQMode.SelectedIndex.ToString());
                    writer.WriteElementString("SharpnessLoopFilter", ComboBoxSharpnessFilterAomenc.SelectedIndex.ToString());
                    writer.WriteElementString("Rowmt",              CheckBoxRowmt.IsChecked.ToString());
                    writer.WriteElementString("KeyframeFiltering",  ComboBoxAomKeyframeFiltering.SelectedIndex.ToString());
                    writer.WriteElementString("AutoAltRef",         CheckBoxAutoAltRefAomenc.IsChecked.ToString());
                    writer.WriteElementString("FramePeriodicBoost", CheckBoxFrameBoostAomenc.IsChecked.ToString());
                }else if (ComboBoxEncoder.SelectedIndex == 2)
                {
                    writer.WriteElementString("RDOLookahead",       TextBoxRDOLookaheadRav1e.Text);
                    writer.WriteElementString("ColorPrimariesRav1e", ComboBoxColorPrimariesRav1e.SelectedIndex.ToString());
                    writer.WriteElementString("ColorTransferRav1e", ComboBoxColorTransferRav1e.SelectedIndex.ToString());
                    writer.WriteElementString("ColorMatrixRav1e",   ComboBoxColorMatrixRav1e.SelectedIndex.ToString());
                    writer.WriteElementString("PixelRangeRav1e",    ComboBoxPixelRangeRav1e.SelectedIndex.ToString());
                    writer.WriteElementString("TuneRav1e",          ComboBoxTuneRav1e.SelectedIndex.ToString());
                    writer.WriteElementString("ContentLightBool",   CheckBoxContentLightRav1e.IsChecked.ToString());
                    writer.WriteElementString("ContentLightCll",    TextBoxContentLightCllRav1e.Text);
                    writer.WriteElementString("ContentLightFall",   TextBoxContentLightFallRav1e.Text);
                    writer.WriteElementString("MasteringDisplay",   CheckBoxMasteringDisplayRav1e.IsChecked.ToString());
                    writer.WriteElementString("MasteringGx",        TextBoxMasteringGxRav1e.Text);
                    writer.WriteElementString("MasteringGy",        TextBoxMasteringGyRav1e.Text);
                    writer.WriteElementString("MasteringBx",        TextBoxMasteringBxRav1e.Text);
                    writer.WriteElementString("MasteringBy",        TextBoxMasteringByRav1e.Text);
                    writer.WriteElementString("MasteringRx",        TextBoxMasteringRxRav1e.Text);
                    writer.WriteElementString("MasteringRy",        TextBoxMasteringRyRav1e.Text);
                    writer.WriteElementString("MasteringWPx",       TextBoxMasteringWPxRav1e.Text);
                    writer.WriteElementString("MasteringWPy",       TextBoxMasteringWPyRav1e.Text);
                    writer.WriteElementString("MasteringLmin",      TextBoxMasteringLminRav1e.Text);
                    writer.WriteElementString("MasteringLmax",      TextBoxMasteringLmaxRav1e.Text);
                    writer.WriteElementString("ColorFormatRav1e",   ComboBoxColorFormatRav1e.SelectedIndex.ToString());
                }
                else if (ComboBoxEncoder.SelectedIndex == 3)
                {
                    writer.WriteElementString("ColorFormatSVT",     ComboBoxColorFormatSVT.SelectedIndex.ToString());
                    writer.WriteElementString("HDRSVT",             CheckBoxEnableHDRSVT.IsChecked.ToString());
                    writer.WriteElementString("AQModeSVT",          ComboBoxAQModeSVT.SelectedIndex.ToString());
                    writer.WriteElementString("KeyintSVT",          TextBoxkeyframeIntervalSVT.Text);
                }else if (ComboBoxEncoder.SelectedIndex == 1)
                {
                    writer.WriteElementString("ColorFormatLibaom",  ComboBoxColorFormatLibaom.SelectedIndex.ToString());
                    writer.WriteElementString("AQModeLibaom",       ComboBoxAqModeLibaom.SelectedIndex.ToString());
                    writer.WriteElementString("LagFramesLibaom",    TextBoxLagInFramesLibaom.Text);
                    writer.WriteElementString("AutoAltRefLibaom",   CheckBoxAltRefLibaom.IsChecked.ToString());
                    writer.WriteElementString("TuneLibaom",         ComboBoxTunelibaom.SelectedIndex.ToString());
                }else if(ComboBoxEncoder.SelectedIndex == 4)
                {
                    //It says Libaom, but both Encoders (ffmpeg) share the same arguments
                    writer.WriteElementString("ColorFormatLibaom",  ComboBoxColorFormatLibaom.SelectedIndex.ToString());
                    writer.WriteElementString("LagFramesLibaom",    TextBoxLagInFramesLibaom.Text);
                    writer.WriteElementString("AutoAltRefVP9",      CheckBoxAutoAltRefVP9.IsChecked.ToString());
                    writer.WriteElementString("TuneVP9",            ComboBoxTuneVP9.SelectedIndex.ToString());
                    writer.WriteElementString("AQModeVP9",          ComboBoxAQModeVP9.SelectedIndex.ToString());
                }
            }
            writer.WriteEndElement();
            writer.Close();
        }

        private void LoadSettings(string saveName, bool saveProfile, bool saveJob, bool saveQueue)
        {
            string directory = "";
            if (saveProfile) { directory = "Profiles\\" + saveName; }
            if (saveJob) { directory = "UnfinishedJobs\\" + saveName; }
            if (saveQueue) { directory = Path.Combine(Directory.GetCurrentDirectory(), "Queue", saveName + ".xml");  }
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

                    case "AutoAltRefVP9":       CheckBoxAutoAltRefVP9.IsChecked = n.InnerText == "True"; break;
                    case "TuneVP9":             ComboBoxTuneVP9.SelectedIndex = int.Parse(n.InnerText); break;
                    case "AQModeVP9":           ComboBoxAQModeVP9.SelectedIndex = int.Parse(n.InnerText); break;
                    case "Encoder":             ComboBoxEncoder.SelectedIndex = int.Parse(n.InnerText); break;
                    case "Framerate":           ComboBoxFrameRate.SelectedIndex = int.Parse(n.InnerText); break;
                    case "BitDepth":            ComboBoxBitDepth.SelectedIndex = int.Parse(n.InnerText); break;
                    case "Preset":              SliderPreset.Value = int.Parse(n.InnerText); break;
                    case "QualityMode":         if (n.InnerText == "True") { RadioButtonConstantQuality.IsChecked = true; RadioButtonBitrate.IsChecked = false; } else { RadioButtonConstantQuality.IsChecked = false; RadioButtonBitrate.IsChecked = true; } break;
                    case "Quality":             SliderQuality.Value = int.Parse(n.InnerText); break;
                    case "Bitrate":             TextBoxBitrate.Text = n.InnerText; break;
                    case "Passes":              ComboBoxPasses.SelectedIndex = int.Parse(n.InnerText); break;
                    case "RealTime":            CheckBoxRealtimeMode.IsChecked = n.InnerText == "True"; break;
                    case "Workers":             ComboBoxWorkers.SelectedIndex = int.Parse(n.InnerText); break;
                    case "Priority":            ComboBoxProcessPriority.SelectedIndex = int.Parse(n.InnerText); break;
                    case "ReencodeCodec":       ComboBoxReencodeCodec.SelectedIndex = int.Parse(n.InnerText); break;
                    case "Reencode":            CheckBoxReencodeDuringSplitting.IsChecked = n.InnerText == "True"; break;
                    case "PreReencode":         CheckBoxReencodeBeforeSplitting.IsChecked = n.InnerText == "True"; break;
                    case "CommentHeader":       CheckBoxCommentHeaderSettings.IsChecked = n.InnerText == "True"; break;
                    case "ChunkCalc":           CheckBoxChunkLengthAutoCalculation.IsChecked = n.InnerText == "True"; break;
                    case "ChunkLength":         TextBoxChunkLength.Text = n.InnerText; break;
                    case "Crop":                CheckBoxCrop.IsChecked = n.InnerText == "True"; break;
                    case "CropTop":             TextBoxCropTop.Text = n.InnerText; break;
                    case "CropBottom":          TextBoxCropBottom.Text = n.InnerText; break;
                    case "CropLeft":            TextBoxCropLeft.Text = n.InnerText; break;
                    case "CropRight":           TextBoxCropRight.Text = n.InnerText; break;
                    case "Resize":              CheckBoxResize.IsChecked = n.InnerText == "True"; break;
                    case "ResizeWidth":         TextBoxImageWidth.Text = n.InnerText; break;
                    case "ResizeHeight":        TextBoxImageHeight.Text = n.InnerText; break;
                    case "ResizeFilter":        ComboBoxResizeFilters.SelectedIndex = int.Parse(n.InnerText); break;
                    case "AudioEncoding":       CheckBoxAudioEncoding.IsChecked = n.InnerText == "True"; break;
                    case "AudioTrackOne":       CheckBoxAudioTrackOne.IsChecked = n.InnerText == "True"; break;
                    case "AudioTrackTwo":       CheckBoxAudioTrackTwo.IsChecked = n.InnerText == "True"; break;
                    case "AudioTrackThree":     CheckBoxAudioTrackThree.IsChecked = n.InnerText == "True"; break;
                    case "AudioTrackFour":      CheckBoxAudioTrackFour.IsChecked = n.InnerText == "True"; break;
                    case "AudioLangOne":        ComboBoxTrackOneLanguage.SelectedIndex = int.Parse(n.InnerText); break;
                    case "AudioLangTwo":        ComboBoxTrackTwoLanguage.SelectedIndex = int.Parse(n.InnerText); break;
                    case "AudioLangThree":      ComboBoxTrackThreeLanguage.SelectedIndex = int.Parse(n.InnerText); break;
                    case "AudioLangFour":       ComboBoxTrackFourLanguage.SelectedIndex = int.Parse(n.InnerText); break;
                    case "TrackOneCodec":       ComboBoxAudioCodec.SelectedIndex = int.Parse(n.InnerText); break;
                    case "TrackTwoCodec":       ComboBoxAudioCodecTrackTwo.SelectedIndex = int.Parse(n.InnerText); break;
                    case "TrackThreeCodec":     ComboBoxAudioCodecTrackThree.SelectedIndex = int.Parse(n.InnerText); break;
                    case "TrackFourCodec":      ComboBoxAudioCodecTrackFour.SelectedIndex = int.Parse(n.InnerText); break;
                    case "TrackOneBitrate":     TextBoxAudioBitrate.Text = n.InnerText; break;
                    case "TrackTwoBitrate":     TextBoxAudioBitrateTrackTwo.Text = n.InnerText; break;
                    case "TrackThreeBitrate":   TextBoxAudioBitrateTrackThree.Text = n.InnerText; break;
                    case "TrackFourBitrate":    TextBoxAudioBitrateTrackFour.Text = n.InnerText; break;
                    case "TrackOneChannels":    ComboBoxTrackOneChannels.SelectedIndex = int.Parse(n.InnerText); break;
                    case "TrackTwoChannels":    ComboBoxTrackTwoChannels.SelectedIndex = int.Parse(n.InnerText); break;
                    case "TrackThreeChannels":  ComboBoxTrackThreeChannels.SelectedIndex = int.Parse(n.InnerText); break;
                    case "TrackFourChannels":   ComboBoxTrackFourChannels.SelectedIndex = int.Parse(n.InnerText); break;
                    case "DarkMode":            CheckBoxDarkMode.IsChecked = n.InnerText == "True"; break;
                    case "AdvancedSettings":    CheckBoxAdvancedSettings.IsChecked = n.InnerText == "True"; break;
                    case "Threads":             ComboBoxThreadsAomenc.SelectedIndex = int.Parse(n.InnerText); break;
                    case "TileColumns":         ComboBoxTileColumns.SelectedIndex = int.Parse(n.InnerText); break;
                    case "TileRows":            ComboBoxTileRows.SelectedIndex = int.Parse(n.InnerText); break;
                    case "MinKeyframeInterval": TextBoxMinKeyframeinterval.Text = n.InnerText; break;
                    case "MaxKeyframeInterval": TextBoxMaxKeyframeinterval.Text = n.InnerText; break;
                    case "LagInFrames":         TextBoxMaxLagInFrames.Text = n.InnerText; break;
                    case "MaxRefFrames":        ComboBoxMaxReferenceFramesAomenc.SelectedIndex = int.Parse(n.InnerText); break;
                    case "ColorPrimaries":      ComboBoxColorPrimariesAomenc.SelectedIndex = int.Parse(n.InnerText); break;
                    case "ColorTransfer":       ComboBoxColorTransferAomenc.SelectedIndex = int.Parse(n.InnerText); break;
                    case "ColorMatrix":         ComboBoxColorMatrixAomenc.SelectedIndex = int.Parse(n.InnerText); break;
                    case "ChromaSubsampling":   ComboBoxChromaSubsamplingAomenc.SelectedIndex = int.Parse(n.InnerText); break;
                    case "Tune":                ComboBoxTuneAomenc.SelectedIndex = int.Parse(n.InnerText); break;
                    case "AQMode":              ComboBoxAQMode.SelectedIndex = int.Parse(n.InnerText); break;
                    case "AQModeLibaom":        ComboBoxAqModeLibaom.SelectedIndex = int.Parse(n.InnerText); break;
                    case "LagFramesLibaom":     TextBoxLagInFramesLibaom.Text = n.InnerText; break;
                    case "AutoAltRefLibaom":    CheckBoxAltRefLibaom.IsChecked = n.InnerText == "True"; break;
                    case "TuneLibaom":          ComboBoxTunelibaom.SelectedIndex = int.Parse(n.InnerText); break;
                    case "SharpnessLoopFilter": ComboBoxSharpnessFilterAomenc.SelectedIndex = int.Parse(n.InnerText); break;
                    case "Rowmt":               CheckBoxRowmt.IsChecked = n.InnerText == "True"; break;
                    case "KeyframeFiltering":   ComboBoxAomKeyframeFiltering.SelectedIndex = int.Parse(n.InnerText); break;
                    case "AutoAltRef":          CheckBoxAutoAltRefAomenc.IsChecked = n.InnerText == "True"; break;
                    case "FramePeriodicBoost":  CheckBoxFrameBoostAomenc.IsChecked = n.InnerText == "True"; break;
                    case "RDOLookahead":        TextBoxRDOLookaheadRav1e.Text = n.InnerText; break;
                    case "ColorPrimariesRav1e": ComboBoxColorPrimariesRav1e.SelectedIndex = int.Parse(n.InnerText); break;
                    case "ColorTransferRav1e":  ComboBoxColorTransferRav1e.SelectedIndex = int.Parse(n.InnerText); break;
                    case "ColorMatrixRav1e":    ComboBoxColorMatrixRav1e.SelectedIndex = int.Parse(n.InnerText); break;
                    case "PixelRangeRav1e":     ComboBoxPixelRangeRav1e.SelectedIndex = int.Parse(n.InnerText); break;
                    case "TuneRav1e":           ComboBoxTuneRav1e.SelectedIndex = int.Parse(n.InnerText); break;
                    case "ContentLightBool":    CheckBoxContentLightRav1e.IsChecked = n.InnerText == "True"; break;
                    case "ContentLightCll":     TextBoxContentLightCllRav1e.Text = n.InnerText; break;
                    case "ContentLightFall":    TextBoxContentLightFallRav1e.Text = n.InnerText; break;
                    case "ColorFormatRav1e":    ComboBoxColorFormatRav1e.SelectedIndex = int.Parse(n.InnerText); break;
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
                    case "ColorFormatSVT":      ComboBoxAQModeSVT.SelectedIndex = int.Parse(n.InnerText); break;
                    case "HDRSVT":              CheckBoxEnableHDRSVT.IsChecked = n.InnerText == "True"; break;
                    case "AQModeSVT":           ComboBoxAQModeSVT.SelectedIndex = int.Parse(n.InnerText); break;
                    case "KeyintSVT":           TextBoxkeyframeIntervalSVT.Text = n.InnerText; break;
                    case "ColorFormatLibaom":   ComboBoxColorFormatLibaom.SelectedIndex = int.Parse(n.InnerText); break;
                    case "Subtitles":           CheckBoxSubtitleEncoding.IsChecked = n.InnerText == "True"; break;
                    case "SubTrackOne":         CheckBoxSubtitleActivatedOne.IsChecked = n.InnerText == "True"; break;
                    case "SubTrackTwo":         CheckBoxSubtitleActivatedTwo.IsChecked = n.InnerText == "True"; break;
                    case "SubTrackThree":       CheckBoxSubtitleActivatedThree.IsChecked = n.InnerText == "True"; break;
                    case "SubTrackFour":        CheckBoxSubtitleActivatedFour.IsChecked = n.InnerText == "True"; break;
                    case "SubOnePath":          TextBoxSubtitleTrackOne.Text = n.InnerText; break;
                    case "SubTwoPath":          TextBoxSubtitleTrackTwo.Text = n.InnerText; break;
                    case "SubThreePath":        TextBoxSubtitleTrackThree.Text = n.InnerText; break;
                    case "SubFourPath":         TextBoxSubtitleTrackFour.Text = n.InnerText; break;
                    case "SubOneBurn":          CheckBoxSubOneBurn.IsChecked = n.InnerText == "True"; break;
                    case "SubTwoBurn":          CheckBoxSubTwoBurn.IsChecked = n.InnerText == "True"; break;
                    case "SubThreeBurn":        CheckBoxSubThreeBurn.IsChecked = n.InnerText == "True"; break;
                    case "SubFourBurn":         CheckBoxSubFourBurn.IsChecked = n.InnerText == "True"; break;
                    case "SubOneDefault":       CheckBoxSubOneDefault.IsChecked = n.InnerText == "True"; break;
                    case "SubTwoDefault":       CheckBoxSubTwoDefault.IsChecked = n.InnerText == "True"; break;
                    case "SubThreeDefault":     CheckBoxSubThreeDefault.IsChecked = n.InnerText == "True"; break;
                    case "SubFourDefault":      CheckBoxSubFourDefault.IsChecked = n.InnerText == "True"; break;
                    case "SubOneLang":          ComboBoxSubTrackOneLanguage.SelectedIndex = int.Parse(n.InnerText); break;
                    case "SubTwoLang":          ComboBoxSubTrackTwoLanguage.SelectedIndex = int.Parse(n.InnerText); break;
                    case "SubThreeLang":        ComboBoxSubTrackThreeLanguage.SelectedIndex = int.Parse(n.InnerText); break;
                    case "SubFourLang":         ComboBoxSubTrackFourLanguage.SelectedIndex = int.Parse(n.InnerText); break;
                    case "SubOneTrackName":     TextBoxSubOneName.Text = n.InnerText; break;
                    case "SubTwoTrackName":     TextBoxSubTwoName.Text = n.InnerText; break;
                    case "SubThreeTrackName":   TextBoxSubThreeName.Text = n.InnerText; break;
                    case "SubFourTrackName":    TextBoxSubFourName.Text = n.InnerText; break;
                    case "Deinterlacing":       CheckBoxDeinterlaceYadif.IsChecked = n.InnerText == "True"; break;
                    case "Deinterlacer":        ComboBoxDeinterlace.SelectedIndex = int.Parse(n.InnerText); break;
                    case "CustomSettings":      CheckBoxCustomSettings.IsChecked = n.InnerText == "True"; break;
                    case "CustomSettingsText":  TextBoxAdvancedSettings.Text = n.InnerText; break;
                    case "Trimming":            CheckBoxTrimming.IsChecked = n.InnerText == "True"; break;
                    case "TrimStart":           TextBoxTrimStart.Text = n.InnerText; break;
                    case "TrimEnd":             TextBoxTrimEnd.Text = n.InnerText; break;
                    default: break;
                }
            }
        }

        private void SaveSettingsTab()
        {
            //Saves the Settings of the SettingsTab
            if (programStartup == false)
            {
                XmlWriter writer = XmlWriter.Create(Path.Combine(Directory.GetCurrentDirectory(), "tabsettings.xml"));
                writer.WriteStartElement("Settings");
                writer.WriteElementString("CustomTemp",         CheckBoxCustomTempPath.IsChecked.ToString());
                writer.WriteElementString("CustomTempPath",     TextBoxCustomTempPath.Text);
                writer.WriteElementString("DeleteTempFiles",    CheckBoxDeleteTempFiles.IsChecked.ToString());
                writer.WriteElementString("DeleteTempFilesDyn", CheckBoxDeleteTempFilesDynamically.IsChecked.ToString());
                writer.WriteElementString("PlayFinishedSound",  CheckBoxFinishedSound.IsChecked.ToString());
                writer.WriteElementString("WorkerLimitSVT",     CheckBoxWorkerLimit.IsChecked.ToString());
                writer.WriteElementString("DarkMode",           CheckBoxDarkMode.IsChecked.ToString());
                writer.WriteElementString("Logging",            CheckBoxLogging.IsChecked.ToString());
                writer.WriteElementString("Shutdown",           CheckBoxShutdownAfterEncode.IsChecked.ToString());
                writer.WriteElementString("TempPathActive",     CheckBoxCustomTempPath.IsChecked.ToString());
                writer.WriteElementString("TempPath",           TextBoxCustomTempPath.Text);
                writer.WriteElementString("FrameCountActive",   CheckBoxCheckFrameCount.IsChecked.ToString());
                writer.WriteElementString("Splitting",          CheckBoxSplitting.IsChecked.ToString());
                writer.WriteElementString("Terminal",           CheckBoxEncodingTerminal.IsChecked.ToString());
                writer.WriteEndElement();
                writer.Close();
            }
        }

        private void LoadSettingsTab()
        {
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "tabsettings.xml")))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(Path.Combine(Directory.GetCurrentDirectory(), "tabsettings.xml"));
                XmlNodeList node = doc.GetElementsByTagName("Settings");
                foreach (XmlNode n in node[0].ChildNodes)
                {
                    switch (n.Name)
                    {
                        case "CustomTemp":          CheckBoxCustomTempPath.IsChecked = n.InnerText == "True"; break;
                        case "CustomTempPath":      TextBoxCustomTempPath.Text = n.InnerText; break;
                        case "DeleteTempFiles":     CheckBoxDeleteTempFiles.IsChecked = n.InnerText == "True"; break;
                        case "DeleteTempFilesDyn":  CheckBoxDeleteTempFilesDynamically.IsChecked = n.InnerText == "True"; break;
                        case "PlayFinishedSound":   CheckBoxFinishedSound.IsChecked = n.InnerText == "True"; break;
                        case "WorkerLimitSVT":      CheckBoxWorkerLimit.IsChecked = n.InnerText == "True"; break;
                        case "DarkMode":            CheckBoxDarkMode.IsChecked = n.InnerText == "True"; break;
                        case "Logging":             CheckBoxLogging.IsChecked = n.InnerText == "True"; break;
                        case "Shutdown":            CheckBoxShutdownAfterEncode.IsChecked = n.InnerText == "True"; break;
                        case "TempPathActive":      CheckBoxCustomTempPath.IsChecked = n.InnerText == "True"; break;
                        case "TempPath":            TextBoxCustomTempPath.Text = n.InnerText; break;
                        case "FrameCountActive":    CheckBoxCheckFrameCount.IsChecked = n.InnerText == "True"; break;
                        case "Splitting":           CheckBoxSplitting.IsChecked = n.InnerText == "True"; break;
                        case "Terminal":            CheckBoxEncodingTerminal.IsChecked = n.InnerText == "True"; break;
                        default: break;
                    }
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
                }
            }
            catch { }
        }

        private void LoadQueueStartup()
        {
            if (Directory.Exists("Queue") == true)
            {
                try
                {
                    DirectoryInfo queueFiles = new DirectoryInfo("Queue");
                    foreach (var file in queueFiles.GetFiles()) { ListBoxQueue.Items.Add(Path.GetFileNameWithoutExtension(file.ToString())); SmallFunctions.Logging("Found Queue file: " + file.ToString()); }
                }
                catch { }
            }
        }

        private void LoadBackground()
        {
            if (File.Exists("darkmode.txt"))
            {
                try
                {
                    if (File.ReadAllText("darkmode.txt").Contains("True"))
                    { CheckBoxDarkMode.IsChecked = true; }
                    else { CheckBoxDarkMode.IsChecked = false; }
                }
                catch { }
            }
            if (File.Exists("background.txt"))
            {
                try
                {
                    Uri fileUri = new Uri(File.ReadAllText("background.txt"));
                    imgDynamic.Source = new BitmapImage(fileUri);
                    PathToBackground = File.ReadAllText("background.txt");
                    customBackground = true;
                    SetBackground();
                }
                catch { }
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

        public static void setEncoderPath()
        {
            //aomenc
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "aomenc.exe"))) { aomencPath = Directory.GetCurrentDirectory(); }
            else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Apps", "Encoder", "aomenc.exe"))) { aomencPath = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "Encoder"); }
            else if (SmallFunctions.ExistsOnPath("aomenc.exe")) { aomencPath = SmallFunctions.GetFullPathWithOutName("aomenc.exe"); }
            SmallFunctions.Logging("Encoder aomenc Path: " + aomencPath);

            //rav1e
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "rav1e.exe"))) { rav1ePath = Directory.GetCurrentDirectory(); }
            else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Apps", "Encoder", "rav1e.exe"))) { rav1ePath = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "Encoder"); }
            else if (SmallFunctions.ExistsOnPath("rav1e.exe")) { rav1ePath = SmallFunctions.GetFullPathWithOutName("rav1e.exe"); }
            SmallFunctions.Logging("Encoder rav1e Path: " + rav1ePath);

            //svt-av1
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "SvtAv1EncApp.exe"))) { svtav1Path = Directory.GetCurrentDirectory(); }
            else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Apps", "Encoder", "SvtAv1EncApp.exe"))) { svtav1Path = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "Encoder"); }
            else if (SmallFunctions.ExistsOnPath("SvtAv1EncApp.exe")) { svtav1Path = SmallFunctions.GetFullPathWithOutName("SvtAv1EncApp.exe"); }
            SmallFunctions.Logging("Encoder svt-av1 Path: " + svtav1Path);

            //ffmpeg
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "ffmpeg.exe"))) { ffmpegPath = Directory.GetCurrentDirectory(); }
            else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Apps", "ffmpeg", "ffmpeg.exe"))) { ffmpegPath = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "ffmpeg"); }
            else if (SmallFunctions.ExistsOnPath("ffmpeg.exe")) { ffmpegPath = SmallFunctions.GetFullPathWithOutName("ffmpeg.exe"); }
            SmallFunctions.Logging("Encoder ffmpeg Path: " + ffmpegPath);

            //ffprobe
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "ffprobe.exe"))) { ffprobePath = Directory.GetCurrentDirectory(); }
            else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Apps", "ffmpeg", "ffprobe.exe"))) { ffprobePath = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "ffmpeg"); }
            else if (SmallFunctions.ExistsOnPath("ffprobe.exe")) { ffprobePath = SmallFunctions.GetFullPathWithOutName("ffprobe.exe"); }
            SmallFunctions.Logging("Encoder ffprobe Path: " + ffprobePath);

            //mkvmerge
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "mkvmerge.exe"))) { mkvToolNixPath = Directory.GetCurrentDirectory(); }
            else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Apps", "mkvtoolnix", "mkvmerge.exe"))) { mkvToolNixPath = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "mkvtoolnix"); }
            else if (SmallFunctions.ExistsOnPath("mkvmerge.exe")) { mkvToolNixPath = SmallFunctions.GetFullPathWithOutName("mkvmerge.exe"); }
            else if (File.Exists(Path.Combine("C:\\Program Files\\MKVToolNix", "mkvmerge.exe"))) { mkvToolNixPath = "C:\\Program Files\\MKVToolNix"; }
            SmallFunctions.Logging("MkvMerge Path: " + mkvToolNixPath);
        }

        private void saveResumeJob()
        {
            SmallFunctions.checkCreateFolder(Path.Combine(Directory.GetCurrentDirectory(), "UnfinishedJobs"));
            SaveSettings(fileName, false, true, false);
        }

        private void CheckForResumeFile()
        {
            if (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "UnfinishedJobs")))
            {
                DirectoryInfo resumeFiles = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "UnfinishedJobs"));
                
                foreach (var file in resumeFiles.GetFiles("*.xml"))
                {
                    SmallFunctions.Logging("Unfinished Job File found" + file.Name);
                    if (MessageBox.Show("Unfinished Job detected! Load unfinished Job: " + file.Name + "?", "Resume", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    { 
                        LoadSettings(file.Name, false, true, false); CheckBoxResumeMode.IsChecked = true; setFrameRate(ffprobe.GetFrameRate(videoInput));
                        break;
                    }
                    else 
                    { 
                        SmallFunctions.Logging("Unfinished Job File found but not loaded " + file.Name);
                        if (MessageBox.Show("Delete Unfinished Job File: " + file.Name + "? \nThis also deletes the temp files.", "Delete", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        { 
                            try 
                            {
                                File.Delete(file.FullName);
                                if (CheckBoxCustomTempPath.IsChecked == true) { tempPath = Path.Combine(TextBoxCustomTempPath.Text, "NEAV1E" ,Path.GetFileNameWithoutExtension(file.Name)); }
                                else { tempPath = Path.Combine(Path.GetTempPath(), "NEAV1E", Path.GetFileNameWithoutExtension(file.Name)); }
                                SmallFunctions.DeleteTempFiles();
                            } catch { } 
                        }
                    }
                }
            }
        }

        //═══════════════════════════════════════ Encoding ════════════════════════════════════════

        private void Encode()
        {

            DateTime starttime = DateTime.Now;
            starttimea = starttime;
            bool skipChunking = skipSplitting;
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
                                string inputPath;
                                if (skipChunking == false) { inputPath = Path.Combine(tempPath, "Chunks", items); }
                                else { inputPath = items; }

                                if (videoPasses == 1)
                                {
                                    Process process = new Process();
                                    ProcessStartInfo startInfo = new ProcessStartInfo();
                                    if (showTerminalDuringEncode == false)
                                        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                    startInfo.UseShellExecute = true;
                                    startInfo.FileName = "cmd.exe";
                                    startInfo.WorkingDirectory = ffmpegPath + "\\";


                                    string outputPath;
                                    if (skipChunking == false) { outputPath = Path.Combine(tempPath, "Chunks", items + "-av1.ivf"); }
                                    else { outputPath = Path.Combine(tempPath, "Chunks", "encode-av1.ivf"); }

                                    switch (encoder)
                                    {
                                        case "aomenc":
                                            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + inputPath + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + Path.Combine(aomencPath, "aomenc.exe") + '\u0022' + " - --passes=1 " + allSettingsAom + " --output=" + '\u0022' + outputPath + '\u0022';
                                            break;
                                        case "rav1e":
                                            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + inputPath + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + Path.Combine(rav1ePath, "rav1e.exe") + '\u0022' + " - " + allSettingsRav1e + " --output " + '\u0022' + outputPath + '\u0022';
                                            break;
                                        case "libaom":
                                            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + inputPath + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -strict experimental -c:v libaom-av1 " + allSettingsAom + " " + '\u0022' + outputPath + '\u0022';
                                            break;
                                        case "svt-av1":
                                            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + inputPath + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -nostdin -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + Path.Combine(svtav1Path, "SvtAv1EncApp.exe") + '\u0022' + " -i stdin " + allSettingsSVTAV1 + " --passes 1 -b " + '\u0022' + outputPath + '\u0022';
                                            break;
                                        case "libvpx-vp9":
                                            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + inputPath + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -c:v libvpx-vp9 " + allSettingsVP9 + " " + '\u0022' + outputPath + '\u0022';
                                            break;
                                        default:
                                            break;
                                    }
                                    SmallFunctions.Logging("Encode() Arguments: " + startInfo.Arguments);
                                    process.StartInfo = startInfo;
                                    process.Start();
                                    //Sets the Process Priority
                                    if (processPriority == 1) { process.PriorityClass = ProcessPriorityClass.BelowNormal; }

                                    process.WaitForExit();

                                    if (SmallFunctions.Cancel.CancelAll == false) 
                                    { 
                                        SmallFunctions.WriteToFileThreadSafe(items, Path.Combine(tempPath, "encoded.log"));
                                        if (deleteTempFilesDynamically && skipChunking == false) {
                                            try { File.Delete(Path.Combine(tempPath, "Chunks", items)); } catch { }
                                        }
                                    }
                                    else { SmallFunctions.KillInstances(); }
                                }
                                else if (videoPasses == 2)
                                {
                                    Process process = new Process();
                                    ProcessStartInfo startInfo = new ProcessStartInfo();

                                    string logPath;
                                    if (skipChunking == false) { logPath = Path.Combine(tempPath, "Chunks", items + "_1pass_successfull.log"); }
                                    else { logPath = Path.Combine(tempPath, "Chunks", "encode_1pass_successfull.log"); }

                                    bool FileExistFirstPass = File.Exists(Path.Combine(tempPath, logPath));

                                    string outputPathStats;
                                    if (skipChunking == false) { outputPathStats = Path.Combine(tempPath, "Chunks", items + "_stats.log"); }
                                    else { outputPathStats = Path.Combine(tempPath, "Chunks", "encode_stats.log"); }

                                    string outputPath;
                                    if (skipChunking == false) { outputPath = Path.Combine(tempPath, "Chunks", items + "-av1.ivf"); }
                                    else { outputPath = Path.Combine(tempPath, "Chunks", "encode-av1.ivf"); }



                                    if (FileExistFirstPass != true)
                                    {
                                        if (showTerminalDuringEncode == false)
                                            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                        startInfo.FileName = "cmd.exe";
                                        startInfo.WorkingDirectory = ffmpegPath + "\\";

                                        switch (encoder)
                                        {
                                            case "aomenc":
                                                startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + inputPath + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + Path.Combine(aomencPath, "aomenc.exe") + '\u0022' + " - --passes=2 --pass=1 --fpf=" + '\u0022' + outputPathStats + '\u0022' + " " + allSettingsAom + " --output=NUL";
                                                break;
                                            //case "rav1e": !!! RAV1E TWO PASS IS STILL BROKEN !!!
                                                //startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + tempPath + "\\Chunks\\" + items + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + rav1ePath + '\u0022' + " - " + allSettingsRav1e + " --first-pass " + '\u0022' + tempPath + "\\Chunks\\" + items + "_stats.log" + '\u0022';
                                                //break;
                                            case "libaom":
                                                startInfo.Arguments = "/C ffmpeg.exe -y -i " + '\u0022' + inputPath + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -strict experimental -c:v libaom-av1 " + allSettingsAom + " -pass 1 -passlogfile " + '\u0022' + outputPathStats + '\u0022' + " -f matroska NUL";
                                                break;
                                            case "svt-av1":
                                                startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + inputPath + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -nostdin -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + Path.Combine(svtav1Path, "SvtAv1EncApp.exe") + '\u0022' + " -i stdin " + allSettingsSVTAV1 + " -b NUL --pass 1 --irefresh-type 2 --stats " + '\u0022' + outputPathStats + '\u0022';
                                                break;
                                            case "libvpx-vp9":
                                                startInfo.Arguments = "/C ffmpeg.exe -y -i " + '\u0022' + inputPath + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -c:v libvpx-vp9 " + allSettingsVP9 + " -pass 1 -passlogfile " + '\u0022' + outputPathStats + '\u0022' + " -f matroska NUL";
                                                break;
                                            default:
                                                break;
                                        }
                                        process.StartInfo = startInfo;
                                        SmallFunctions.Logging("Encode() Arguments: " + startInfo.Arguments);
                                        process.Start();
                                        //Sets the Process Priority
                                        if (processPriority == 1) { process.PriorityClass = ProcessPriorityClass.BelowNormal; }

                                        process.WaitForExit();

                                        if (SmallFunctions.Cancel.CancelAll == false) { SmallFunctions.WriteToFileThreadSafe("", logPath); }
                                    }

                                    if (showTerminalDuringEncode == false)
                                        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                    startInfo.FileName = "cmd.exe";
                                    startInfo.WorkingDirectory = ffmpegPath + "\\";

                                    switch (encoder)
                                    {
                                        case "aomenc":
                                            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + inputPath + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + Path.Combine(aomencPath, "aomenc.exe") + '\u0022' + " - --passes=2 --pass=2 --fpf=" + '\u0022' + outputPathStats + '\u0022' + " " + allSettingsAom + " --output=" + '\u0022' + outputPath + '\u0022';
                                            break;
                                        //case "rav1e": !!! RAV1E TWO PASS IS STILL BROKEN !!!
                                        //startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + tempPath + "\\Chunks\\" + items + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + rav1ePath + '\u0022' + " - " + allSettingsRav1e + " --second-pass " + '\u0022' + tempPath + "\\Chunks\\" + items + "_stats.log" + '\u0022' + " --output " + '\u0022' + tempPath + "\\Chunks\\" + items + "-av1.ivf" + '\u0022';
                                        //break;
                                        case "libaom":
                                            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + inputPath + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -strict experimental -c:v libaom-av1 " + allSettingsAom + " -pass 2 -passlogfile " + '\u0022' + outputPathStats + '\u0022' + " " + '\u0022' + outputPath + '\u0022';
                                            break;
                                        case "svt-av1":
                                            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + inputPath + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -nostdin -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + Path.Combine(svtav1Path, "SvtAv1EncApp.exe") + '\u0022' + " -i stdin " + allSettingsSVTAV1 + " -b " + '\u0022' + outputPath + '\u0022'+ " --pass 2 --irefresh-type 2 --stats " + '\u0022' + outputPathStats + '\u0022';
                                            break;
                                        case "libvpx-vp9":
                                            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + inputPath + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -c:v libvpx-vp9 " + allSettingsVP9 + " -pass 2 -passlogfile " + '\u0022' + outputPathStats + '\u0022' + " " + '\u0022' + outputPath + '\u0022';
                                            break;
                                        default:
                                            break;
                                    }

                                    process.StartInfo = startInfo;
                                    SmallFunctions.Logging("Encode() Arguments: " + startInfo.Arguments);
                                    process.Start();
                                    if (processPriority == 1) { process.PriorityClass = ProcessPriorityClass.BelowNormal; }
                                    process.WaitForExit();

                                    if (SmallFunctions.Cancel.CancelAll == false) 
                                    {
                                        SmallFunctions.WriteToFileThreadSafe(items, Path.Combine(tempPath, "encoded.log"));

                                        if (deleteTempFilesDynamically && skipChunking == false)
                                        {
                                            try { File.Delete(Path.Combine(tempPath, "Chunks", items)); } catch { }
                                        }
                                    }
                                    else { SmallFunctions.KillInstances(); }
                                }
                            }


                        }
                        finally
                        {
                            concurrencySemaphore.Release();
                            if (SmallFunctions.Cancel.CancelAll == false)
                            {
                                ProgressBar.Dispatcher.Invoke(() => ProgressBar.Value += 1, DispatcherPriority.Background);
                                TimeSpan timespent = DateTime.Now - starttime;
                                LabelProgressbar.Dispatcher.Invoke(() => LabelProgressbar.Content = ProgressBar.Value + " / " + videoChunksCount.ToString() + " - " + Math.Round(Convert.ToDecimal(((((videoLength * videoFrameRate) / videoChunksCount) * ProgressBar.Value) / timespent.TotalSeconds)), 2).ToString() + "fps" + " - " + Math.Round((((timespent.TotalSeconds / ProgressBar.Value) * (videoChunksCount - ProgressBar.Value)) / 60), MidpointRounding.ToEven) + "min left", DispatcherPriority.Background);
                                LabelProgressbar.Dispatcher.Invoke(() => SmallFunctions.Logging("Progessbar: " + LabelProgressbar.Content), DispatcherPriority.Background);
                            }
                        }
                    });
                    tasks.Add(t);

                }
                Task.WaitAll(tasks.ToArray());
            }
            
        }
    }
}