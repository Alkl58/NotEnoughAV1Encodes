﻿using Microsoft.Win32;
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
        public static string ffprobePath, ffmpegPath, aomencPath, rav1ePath, svtav1Path;
        public static string videoInput, videoOutput, encoder, fileName, videoResize, pipeBitDepth = "yuv420p", reencoder;
        public static string audioCodecTrackOne, audioCodecTrackTwo, audioCodecTrackThree, audioCodecTrackFour;
        public static string trackOneLanguage, trackTwoLanguage, trackThreeLanguage, trackFourLanguage;
        public static string allSettingsAom, allSettingsRav1e, allSettingsSVTAV1, allSettingsVP9;
        public static string tempPath = ""; //Temp Path for Splitting and Encoding
        public static string[] videoChunks, SubtitleChunks; //Temp Chunk List
        public static string PathToBackground, subtitleFfmpegCommand, deinterlaceCommand, cropCommand, trimCommand, saveSettingString, localFileName, ffmpegFramerateSplitting;
        public static string trimEndTemp, trimEndTempMax;
        public static string encoderMetadata;
        public static int videoChunksCount; //Number of Chunks, mainly only for Progressbar
        public static int coreCount, workerCount, chunkLength; //Variable to set the Worker Count
        public static int videoPasses, processPriority, videoLength, customsubtitleadded, counterQueue, frameRateIndex;
        public static int audioBitrateTrackOne, audioBitrateTrackTwo, audioBitrateTrackThree, audioBitrateTrackFour;
        public static int audioChannelsTrackOne, audioChannelsTrackTwo, audioChannelsTrackThree, audioChannelsTrackFour;
        public static bool trackOne, trackTwo, trackThree, trackFour, audioEncoding, pcmBluray;
        public static bool trackOneLang, trackTwoLang, trackThreeLang, trackFourLang;
        public static bool inputSet, outputSet, reencode, beforereencode, resumeMode, deleteTempFiles, deleteTempFilesDynamically;
        public static bool subtitleCopy, subtitleCustom, subtitleHardcoding, subtitleEncoding;
        public static bool customBackground, programStartup = true, logging = true, buttonActive = true, saveSettings, found7z, startupTrim = false, trimButtons = false, encodeStarted;
        public static double videoFrameRate;
        public DateTime starttimea;

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
            Check7zExtractor();
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
            if (inputSet && outputSet)
            {
                ProgressBar.Value = 0;
                ProgressBar.Foreground = new SolidColorBrush(Color.FromRgb(3, 112, 200));
                resumeMode = CheckBoxResumeMode.IsChecked == true;
                ButtonStartEncode.BorderBrush = Brushes.Green;
                ButtonCancelEncode.BorderBrush = new SolidColorBrush(Color.FromRgb(228, 228, 228));
                buttonActive = false;
                if (CheckBoxBatchEncoding.IsChecked == false && CheckBoxQueueEncoding.IsChecked == false)
                {
                    MainEntry();
                }
                else if (CheckBoxBatchEncoding.IsChecked == true && CheckBoxQueueEncoding.IsChecked == false)
                {
                    SmallFunctions.Logging("BatchEncode()");
                    BatchEncode();
                }
            }
            else if (CheckBoxQueueEncoding.IsChecked == false)
            {
                if (inputSet == false) { MessageBoxes.MessageVideoInput(); SmallFunctions.Logging("Video Input not set"); }
                if (outputSet == false) { MessageBoxes.MessageVideoOutput(); SmallFunctions.Logging("Video Output not set"); }
            }
            else if (CheckBoxBatchEncoding.IsChecked == false && CheckBoxQueueEncoding.IsChecked == true)
            {
                SmallFunctions.Logging("QueueEncode()");
                QueueEncode();
            }
        }

        private async void MainEntry()
        {
            encodeStarted = true;
            encoder = ComboBoxEncoder.Text;
            if (SmallFunctions.checkDependencies(encoder) && SmallFunctions.Cancel.CancelAll == false)
            {
                setParameters();
                setAudioParameters();
                setSubtitleParameters();
                if (resumeMode == true) { await AsyncClass(); } 
                else
                {
                    saveResumeJob();
                    if (SmallFunctions.CheckFileFolder()) { await AsyncClass(); }
                    else
                    {
                        if (MessageBox.Show("Temp Chunks Folder not Empty! Overwrite existing Data?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
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
                if (CheckBoxSubtitleEncoding.IsChecked == true && CheckBoxBatchEncoding.IsChecked == false)
                {
                    setProgressBarLabel("Started Subtitle Encoding / Demuxing");
                    await Task.Run(() => Subtitles.EncSubtitles());
                    if (SmallFunctions.CheckSubtitleOutput() == false) { MessageNoSubtitleOutput(); }                    
                }
                else if (RadioButtonStreamCopySubtitles.IsChecked == true)
                {
                    setProgressBarLabel("Started Subtitle Encoding / Demuxing");
                    await Task.Run(() => Subtitles.EncSubtitles());
                    if (SmallFunctions.CheckSubtitleOutput() == false) { MessageNoSubtitleOutput(); }
                }

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
                        if (RadioButtonStreamCopySubtitles.IsChecked == false)
                        {
                            subtitleEncoding = false;
                            subtitleHardcoding = false;
                        }
                    }

                    try
                    {
                        if (SmallFunctions.CheckFileFolder())
                            SmallFunctions.DeleteChunkFolderContent(); // Avoids Temp file issues, as the majaroity of safeguards are not used during Batch encoding
                    } catch { }


                    if (SmallFunctions.Cancel.CancelAll == false)
                    {
                        await AsyncClass();
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
                while(SmallFunctions.Cancel.CancelAll == false)
                {
                    LoadSettings(item.ToString(), false, false, true);
                    ProgressBar.Maximum = 100;
                    ProgressBar.Value = 0;
                    setChunkLength();
                    setParameters();
                    SmallFunctions.DeleteChunkFolderContent();
                    setAudioParameters();
                    setSubtitleParameters();
                    setFrameRate(SmallFunctions.getFrameRate(videoInput));

                    if (SmallFunctions.Cancel.CancelAll == false)
                    {
                        await AsyncClass();
                    }

                    File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "Queue", item.ToString()));
                    ListBoxQueue.Items.Clear();
                    LoadQueueStartup();
                }
            }
            buttonActive = true;
            ProgressBar.Foreground = Brushes.Green;
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

            resumeMode = CheckBoxResumeMode.IsChecked == true;
            SmallFunctions.Logging("Resume Mode: " + resumeMode);
            deleteTempFiles = CheckBoxDeleteTempFiles.IsChecked == true;
            deleteTempFilesDynamically = CheckBoxDeleteTempFilesDynamically.IsChecked == true;
            reencode = CheckBoxReencodeDuringSplitting.IsChecked == true;
            SmallFunctions.Logging("Reencode: " + reencode);
            beforereencode = CheckBoxReencodeBeforeSplitting.IsChecked == true;
            SmallFunctions.Logging("PreReencode: " + beforereencode);

            videoPasses = Int16.Parse(ComboBoxPasses.Text);
            SmallFunctions.Logging("Encoding Passes: " + videoPasses);
            workerCount = Int16.Parse(ComboBoxWorkers.Text);
            SmallFunctions.Logging("Worker Count: " + workerCount);
            chunkLength = Int16.Parse(TextBoxChunkLength.Text);
            SmallFunctions.Logging("Chunk Length: " + chunkLength);
            videoLength = Int16.Parse(SmallFunctions.getVideoLength(videoInput));
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
            string widthNew = (Int16.Parse(TextBoxCropRight.Text) + Int16.Parse(TextBoxCropLeft.Text)).ToString();
            string hieghtNew = (Int16.Parse(TextBoxCropTop.Text) + Int16.Parse(TextBoxCropBottom.Text)).ToString();
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
                    if (CheckBoxChunkLengthAutoCalculation.IsChecked == true) { TextBoxChunkLength.Text = (videoLength / Int16.Parse(ComboBoxWorkers.Text)).ToString(); }
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
                case "aomenc (ffmpeg)": SetLibaomParameters(tempSettings); break;
                case "svt-av1": SetSVTAV1Parameters(tempSettings); break;
                case "libvpx-vp9": SetVP9Parameters(tempSettings); break;
                default: break;
            }
        }

        private void SetAomencParameters(bool tempSettings)
        {
            string aomencQualityMode = "";
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
            SmallFunctions.Logging("Parameters aomenc: " + allSettingsAom);
            if (CheckBoxCommentHeaderSettings.IsChecked == true)
            {
                encoderMetadata = " -metadata description=" + '\u0022' + "NotEnoughAV1Encodes - Encoder: aomenc " + allSettingsAom + '\u0022' + " ";
            }
            else { encoderMetadata = ""; }
            
        }

        private void SetLibaomParameters(bool tempSettings)
        {
            string aomencQualityMode = "";
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
                    string altref = " -auto-alt-ref 0 ";
                    if (CheckBoxAltRefLibaom.IsChecked == true) { altref = " -auto-alt-ref 1 "; }
                    string aomencFrames = " -tile-columns " + ComboBoxTileColumns.Text + " -tile-rows " + ComboBoxTileRows.Text + " -g " + TextBoxMaxKeyframeinterval.Text + " -lag-in-frames " + TextBoxLagInFramesLibaom.Text + " -aq-mode " + ComboBoxAqModeLibaom.SelectedIndex + " -tune " + ComboBoxTunelibaom.Text;
                    allSettingsAom = "-cpu-used " + SliderPreset.Value + " -threads " + ComboBoxThreadsAomenc.Text + aomencFrames + aomencQualityMode + altref;
                }
                else
                {
                    allSettingsAom = TextBoxAdvancedSettings.Text;
                }
            }
            SmallFunctions.Logging("Parameters libaom: " + allSettingsAom);
            if (CheckBoxCommentHeaderSettings.IsChecked == true)
            {
                encoderMetadata = " -metadata description=" + '\u0022' + "NotEnoughAV1Encodes - Encoder: libaom " + allSettingsAom + '\u0022' + " ";
            }
            else { encoderMetadata = ""; }              
        }

        private void SetRav1eParameters(bool tempSettings)
        {
            string rav1eQualityMode = "";
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
            if (CheckBoxCommentHeaderSettings.IsChecked == true)
            {
                encoderMetadata = " -metadata description=" + '\u0022' + "NotEnoughAV1Encodes - Encoder: rav1e " + allSettingsRav1e + '\u0022' + " ";
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
            if (CheckBoxCommentHeaderSettings.IsChecked == true)
            {
                encoderMetadata = " -metadata description=" + '\u0022' + "NotEnoughAV1Encodes - Encoder: SVT-AV1 " + allSettingsSVTAV1 + '\u0022' + " ";
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
            if (CheckBoxCommentHeaderSettings.IsChecked == true)
            {
                encoderMetadata = " -metadata description=" + '\u0022' + "NotEnoughAV1Encodes - Encoder: VP9 " + allSettingsVP9 + '\u0022' + " ";
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

        //═════════════════════════════════ Subtitles Parameters ══════════════════════════════════

        private void setSubtitleParameters()
        {
            subtitleCopy = RadioButtonStreamCopySubtitles.IsChecked == true;
            subtitleCustom = RadioButtonCustomSubtitles.IsChecked == true;
            subtitleHardcoding = CheckBoxHardcodeSubtitle.IsChecked == true;
            subtitleEncoding = CheckBoxSubtitleEncoding.IsChecked == true;
            if (subtitleCustom) { SubtitleChunks = ListBoxSubtitles.Items.OfType<string>().ToArray(); }
            if (subtitleHardcoding) { setSubtitleHardcodingParameters(); }
            if (subtitleHardcoding == false) { subtitleFfmpegCommand = ""; } //If not set, it could create problems when a second job is running afterwards
        }

        private void setSubtitleHardcodingParameters()
        {
            if (subtitleCustom)
            {
                string ext = Path.GetExtension(SubtitleChunks[0]);
                if (ext == ".ass" || ext == ".ssa")
                {
                    subtitleFfmpegCommand = "-vf ass=" + '\u0022' + SubtitleChunks[0] + '\u0022';
                    subtitleFfmpegCommand = subtitleFfmpegCommand.Replace("\u005c", "\u005c\u005c\u005c\u005c");
                    subtitleFfmpegCommand = subtitleFfmpegCommand.Replace(":", "\u005c\u005c\u005c:");
                    SmallFunctions.Logging("Subtitle Hardcoding Parameters: " + subtitleFfmpegCommand);
                }
                else if (ext == ".srt")
                {
                    subtitleFfmpegCommand = "-vf subtitles=" + '\u0022' + SubtitleChunks[0] + '\u0022';
                    subtitleFfmpegCommand = subtitleFfmpegCommand.Replace("\u005c", "\u005c\u005c\u005c\u005c");
                    subtitleFfmpegCommand = subtitleFfmpegCommand.Replace(":", "\u005c\u005c\u005c:");
                    SmallFunctions.Logging("Subtitle Hardcoding Parameters: " + subtitleFfmpegCommand);
                }
                else { MessageBoxes.MessageCustomSubtitleHardCodeNotSupported(); }
            }
            if (subtitleCopy) { subtitleFfmpegCommand = "-vf subtitles=" + '\u0022' + videoInput + '\u0022'; subtitleFfmpegCommand = subtitleFfmpegCommand.Replace("\u005c", "\u005c\u005c\u005c\u005c"); subtitleFfmpegCommand = subtitleFfmpegCommand.Replace(":", "\u005c\u005c\u005c:"); }
        }

        //═══════════════════════════════════════ Functions ═══════════════════════════════════════

        private string setChangeFramerate()
        {
            switch(ComboBoxFrameRate.SelectedIndex)
            {
                case 0: return "5"; case 1: return "10";
                case 2: return "12"; case 3: return "15";
                case 4: return "20"; case 5: return "24000/1001";
                case 6: return "24"; case 7: return "25";
                case 8: return "30000/1001"; case 9: return "30";
                case 10: return "48"; case 11: return "50";
                case 12: return "60000/1001"; case 13: return "60";
                default: return "24";
            }
        }

        private void setChunkLength()
        {
            if (CheckBoxChunkLengthAutoCalculation.IsChecked == true) { TextBoxChunkLength.Text = (Int16.Parse(SmallFunctions.getVideoLength(videoInput)) / Int16.Parse(ComboBoxWorkers.Text)).ToString(); }
            TextBoxTrimEnd.Text = SmallFunctions.getVideoLengthAccurate(videoInput);
            trimEndTemp = TextBoxTrimEnd.Text;
            trimEndTempMax = TextBoxTrimEnd.Text;
        }

        private void getVideoInformation()
        {
            string frameRate = SmallFunctions.getFrameRate(videoInput);
            string pixelFormat = SmallFunctions.getPixelFormat(videoInput);
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
            if (trackone == false) { CheckBoxAudioTrackOne.IsChecked = false; CheckBoxAudioTrackOne.IsEnabled = false; }
            if (tracktwo == false) { CheckBoxAudioTrackTwo.IsChecked = false; CheckBoxAudioTrackTwo.IsEnabled = false; }
            if (trackthree == false) { CheckBoxAudioTrackThree.IsChecked = false; CheckBoxAudioTrackThree.IsEnabled = false; }
            if (trackfour == false) { CheckBoxAudioTrackFour.IsChecked = false; CheckBoxAudioTrackFour.IsEnabled = false; }
            if (CheckBoxAudioTrackOne.IsEnabled == false && CheckBoxAudioTrackTwo.IsEnabled == false && CheckBoxAudioTrackThree.IsEnabled == false && CheckBoxAudioTrackFour.IsEnabled == false) { CheckBoxAudioEncoding.IsChecked = false; CheckBoxAudioEncoding.IsEnabled = false; }
            if (SmallFunctions.getAudioInfo(videoInput) == "pcm_bluray") { MessageBoxes.MessagePCMBluray(); pcmBluray = true; } else { pcmBluray = false; }
        }

        public void GetSubtitleTracks()
        {
            //Gets the SubtitleIndexes of the Input Video, because people may enable subtitles, even they don't exist
            Process getSubtitleIndexes = new Process();
            getSubtitleIndexes.StartInfo = new ProcessStartInfo()
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                WorkingDirectory = ffprobePath,
                Arguments = "/C ffprobe.exe -i " + '\u0022' + videoInput + '\u0022' + " -loglevel error -select_streams s -show_streams -show_entries stream=index:tags=:disposition= -of csv",
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            getSubtitleIndexes.Start();
            //Reads the Console Output
            string subtitleIndexes = getSubtitleIndexes.StandardOutput.ReadToEnd();
            SmallFunctions.Logging("Subtitle Indexes ffprobe: " + subtitleIndexes);
            if (subtitleIndexes == "")
            {
                //If the Source video doesnt have Subtitles embedded, then Stream Copy Subtitles will be disabled.
                RadioButtonStreamCopySubtitles.IsChecked = false;
                RadioButtonStreamCopySubtitles.IsEnabled = false;
            }
            else if (subtitleIndexes != "") { RadioButtonStreamCopySubtitles.IsEnabled = true; }
            getSubtitleIndexes.WaitForExit();
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
            frameRateIndex = ComboBoxFrameRate.SelectedIndex;
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
            SolidColorBrush white = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            SolidColorBrush dark = new SolidColorBrush(Color.FromRgb(33, 33, 33));
            SolidColorBrush darker = new SolidColorBrush(Color.FromRgb(25, 25, 25));
            if (customBackground != true)
            {
                Window.Background = darker;
                TabControl.Background = dark;
                TabGrid.Background = dark;
                TabGrid1.Background = dark;
                TabGrid2.Background = dark;
                TabGrid3.Background = dark;
                TabGrid4.Background = dark;
                TabGrid6.Background = dark;
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
            SolidColorBrush white = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            SolidColorBrush black = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            if (customBackground != true)
            {
                Window.Background = white;
                TabControl.Background = white;
                TabGrid.Background = white;
                TabGrid1.Background = white;
                TabGrid2.Background = white;
                TabGrid3.Background = white;
                TabGrid4.Background = white;
                TabGrid6.Background = white;
                TextBoxChunkLength.Background = white;
                ProgressBar.Background = new SolidColorBrush(Color.FromRgb(230, 230, 230));
            }
            LabelPresets.Foreground = black;
            CheckBoxResumeMode.Foreground = black;
            TextBlockOpenSource.Foreground = new SolidColorBrush(Color.FromRgb(21, 65, 126));
            GroupBox.BorderBrush = new SolidColorBrush(Color.FromRgb(213, 223, 229));
            GroupBox1.BorderBrush = new SolidColorBrush(Color.FromRgb(213, 223, 229));
            GroupBox2.BorderBrush = new SolidColorBrush(Color.FromRgb(213, 223, 229));
            GroupBox3.BorderBrush = new SolidColorBrush(Color.FromRgb(213, 223, 229));
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
            if (trimButtons != true)
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

        private void ButtonAddCustomSubtitle_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBoxHardcodeSubtitle.IsChecked == false)
            {
                //Open the OpenFileDialog to set the Subtitle Input
                OpenFileDialog openVideoFileDialog = new OpenFileDialog();
                Nullable<bool> result = openVideoFileDialog.ShowDialog();
                if (result == true)  {  ListBoxSubtitles.Items.Add(openVideoFileDialog.FileName); customsubtitleadded += 1; }
            } else if (customsubtitleadded < 1)
            {
                //Open the OpenFileDialog to set the Subtitle Input
                OpenFileDialog openVideoFileDialog = new OpenFileDialog();
                Nullable<bool> result = openVideoFileDialog.ShowDialog();
                if (result == true) { 
                    ListBoxSubtitles.Items.Add(openVideoFileDialog.FileName); 
                    customsubtitleadded += 1;
                    try
                    {
                        string ext = Path.GetExtension(openVideoFileDialog.FileName);
                        if (ext != ".ass") { if (ext != ".ssa") { if (ext != ".srt") { MessageBoxes.MessageCustomSubtitleHardCodeNotSupported(); } } }
                    }
                    catch { }
                }
            }

        }

        private void ButtonDeleteSubtitle_Click(object sender, RoutedEventArgs e)
        {
            try { ListBoxSubtitles.Items.RemoveAt(ListBoxSubtitles.SelectedIndex); customsubtitleadded -= 1; }
            catch { MessageBoxes.MessageNoSubtitlesToDelete(); }
        }

        private void ButtonOpenProgramFolder_Click(object sender, RoutedEventArgs e)
        {
            try { Process.Start(Directory.GetCurrentDirectory()); } catch { }            
        }

        private void ButtonOpenTempFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CheckBoxCustomTempPath.IsChecked == false) { Process.Start(Path.Combine(Path.GetTempPath(), "NEAV1E")); }
                else { Process.Start(TextBoxCustomTempPath.Text); }
            } catch { }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            encoder = ComboBoxEncoder.Text;
            setEncoderParameters(true);
            string inputSet = "Error";
            if (encoder == "aomenc") { inputSet = allSettingsAom; }
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
            SmallFunctions.KillInstances();
            CancelRoutine();
        }

        private void ButtonSetTempPath_Click(object sender, RoutedEventArgs e)
        {
            //Sets a custom Temp Folder
            System.Windows.Forms.FolderBrowserDialog browseTempFolder = new System.Windows.Forms.FolderBrowserDialog();
            if (browseTempFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK) { TextBoxCustomTempPath.Text = browseTempFolder.SelectedPath; }
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

        private void CheckBoxTrimming_Unchecked(object sender, RoutedEventArgs e)
        {
            ImagePreviewTrimStart.Source = null;
            ImagePreviewTrimEnd.Source = null;
        }

        private void CheckBoxTrimming_Checked(object sender, RoutedEventArgs e)
        {
            try { setVideoLengthTrimmed(); }
            catch { }
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

        private void CheckBoxHardcodeSubtitle_Checked(object sender, RoutedEventArgs e)
        {
            if (customsubtitleadded > 1) { MessageBoxes.MessageHardcodeSubtitlesCheckBox(); }
            if (customsubtitleadded == 1) {
                try
                {
                    string ext = Path.GetExtension((string)ListBoxSubtitles.Items[0]);
                    if (ext != ".ass") { if (ext != ".ssa") { if (ext != ".srt") { MessageBoxes.MessageCustomSubtitleHardCodeNotSupported(); } } }
                }
                catch { }
            }
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

        private void RadioButtonCustomSubtitles_Checked(object sender, RoutedEventArgs e)
        {
            if (CheckBoxBatchEncoding.IsChecked == true) { RadioButtonCustomSubtitles.IsChecked = false; RadioButtonStreamCopySubtitles.IsChecked = true; MessageBoxes.MessageCustomSubtitleBatchMode(); }
        }

        private void CheckBoxBatchEncoding_Checked(object sender, RoutedEventArgs e)
        {
            RadioButtonCustomSubtitles.IsChecked = false; 
            RadioButtonCustomSubtitles.IsEnabled = false;
            RadioButtonStreamCopySubtitles.IsChecked = true;
        }

        private void CheckBoxBatchEncoding_Unchecked(object sender, RoutedEventArgs e)
        {
            RadioButtonCustomSubtitles.IsEnabled = true;
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
            if (encoder == "aomenc" || encoder == "aomenc (ffmpeg)") { inputSet = allSettingsAom; }
            if (encoder == "rav1e") { inputSet = allSettingsRav1e; }
            if (encoder == "svt-av1") { inputSet = allSettingsSVTAV1; }
            if (encoder == "libvpx-vp9") { inputSet = allSettingsVP9; }
            TextBoxAdvancedSettings.Text = inputSet;
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
                case "aomenc (ffmpeg)":
                    if (SliderQuality != null)
                    {
                        SliderQuality.Maximum = 63;
                        SliderQuality.Value = 30;
                        SliderPreset.Maximum = 9;
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
        }

        private void ComboBoxWorkers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBoxEncoder.SelectedIndex == 3 && ComboBoxWorkers.SelectedIndex != 0 && CheckBoxWorkerLimit.IsChecked == false) { ComboBoxWorkers.SelectedIndex = 0; MessageBoxes.MessageSVTWorkers(); }
        }

        private void ComboBoxFrameRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (inputSet == true && ComboBoxFrameRate.SelectedIndex != frameRateIndex)
            {
                if (CheckBoxReencodeBeforeSplitting.IsChecked == false && CheckBoxReencodeDuringSplitting.IsChecked == false && CheckBoxBatchEncoding.IsChecked == false)
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

        private void MessageNoSubtitleOutput()
        {
            if (MessageBox.Show("No Subtitle Output detected! \nCancel?", "Error", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes) { SmallFunctions.Cancel.CancelAll = true; CancelRoutine(); }
            else { subtitleEncoding = false; }
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
            writer.WriteElementString("Subtitles",          CheckBoxSubtitleEncoding.IsChecked.ToString());
            writer.WriteElementString("SubtitlesCopy",      RadioButtonStreamCopySubtitles.IsChecked.ToString());
            writer.WriteElementString("SubtitlesCustom",    RadioButtonCustomSubtitles.IsChecked.ToString());
            writer.WriteElementString("SubtitlesHardSub",   CheckBoxHardcodeSubtitle.IsChecked.ToString());
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
                    writer.WriteElementString("KeyframeFiltering",  CheckBoxKeyframeFilteringAomenc.IsChecked.ToString());
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
            if (saveQueue) { directory = Path.Combine(Directory.GetCurrentDirectory(), "Queue", saveName);  }
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
                    case "TuneVP9":             ComboBoxTuneVP9.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "AQModeVP9":           ComboBoxAQModeVP9.SelectedIndex = Int16.Parse(n.InnerText); break;
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
                    case "ResizeFilter":        ComboBoxResizeFilters.SelectedIndex = Int16.Parse(n.InnerText); break;
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
                    case "AQModeLibaom":        ComboBoxAqModeLibaom.SelectedIndex = Int16.Parse(n.InnerText); break;
                    case "LagFramesLibaom":     TextBoxLagInFramesLibaom.Text = n.InnerText; break;
                    case "AutoAltRefLibaom":    CheckBoxAltRefLibaom.IsChecked = n.InnerText == "True"; break;
                    case "TuneLibaom":          ComboBoxTunelibaom.SelectedIndex = Int16.Parse(n.InnerText); break;
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
                    case "Subtitles":           CheckBoxSubtitleEncoding.IsChecked = n.InnerText == "True"; break;
                    case "SubtitlesCopy":       RadioButtonStreamCopySubtitles.IsChecked = n.InnerText == "True"; break;
                    case "SubtitlesCustom":     RadioButtonCustomSubtitles.IsChecked = n.InnerText == "True"; break;
                    case "SubtitlesHardSub":    CheckBoxHardcodeSubtitle.IsChecked = n.InnerText == "True"; break;
                    case "Deinterlacing":       CheckBoxDeinterlaceYadif.IsChecked = n.InnerText == "True"; break;
                    case "Deinterlacer":        ComboBoxDeinterlace.SelectedIndex = Int16.Parse(n.InnerText); break;
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
            if (programStartup == false)
            {
                XmlWriter writer = XmlWriter.Create(Path.Combine(Directory.GetCurrentDirectory(), "tabsettings.xml"));
                writer.WriteStartElement("Settings");
                writer.WriteElementString("CustomTemp", CheckBoxCustomTempPath.IsChecked.ToString());
                writer.WriteElementString("CustomTempPath", TextBoxCustomTempPath.Text);
                writer.WriteElementString("DeleteTempFiles", CheckBoxDeleteTempFiles.IsChecked.ToString());
                writer.WriteElementString("DeleteTempFilesDyn", CheckBoxDeleteTempFilesDynamically.IsChecked.ToString());
                writer.WriteElementString("PlayFinishedSound", CheckBoxFinishedSound.IsChecked.ToString());
                writer.WriteElementString("WorkerLimitSVT", CheckBoxWorkerLimit.IsChecked.ToString());
                writer.WriteElementString("DarkMode", CheckBoxDarkMode.IsChecked.ToString());
                writer.WriteElementString("Logging", CheckBoxLogging.IsChecked.ToString());
                writer.WriteElementString("Shutdown", CheckBoxShutdownAfterEncode.IsChecked.ToString());
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
                        case "CustomTemp": CheckBoxCustomTempPath.IsChecked = n.InnerText == "True"; break;
                        case "CustomTempPath": TextBoxCustomTempPath.Text = n.InnerText; break;
                        case "DeleteTempFiles": CheckBoxDeleteTempFiles.IsChecked = n.InnerText == "True"; break;
                        case "DeleteTempFilesDyn": CheckBoxDeleteTempFilesDynamically.IsChecked = n.InnerText == "True"; break;
                        case "PlayFinishedSound": CheckBoxFinishedSound.IsChecked = n.InnerText == "True"; break;
                        case "WorkerLimitSVT": CheckBoxWorkerLimit.IsChecked = n.InnerText == "True"; break;
                        case "DarkMode": CheckBoxDarkMode.IsChecked = n.InnerText == "True"; break;
                        case "Logging": CheckBoxLogging.IsChecked = n.InnerText == "True"; break;
                        case "Shutdown": CheckBoxShutdownAfterEncode.IsChecked = n.InnerText == "True"; break;
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
                    foreach (var file in queueFiles.GetFiles()) { ListBoxQueue.Items.Add(file); SmallFunctions.Logging("Found Queue file: " + file.ToString()); }
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

        private void Check7zExtractor()
        {
            if (File.Exists(@"C:\Program Files\7-Zip\7zG.exe")) { found7z = true; }
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
                        LoadSettings(file.Name, false, true, false); CheckBoxResumeMode.IsChecked = true; setFrameRate(SmallFunctions.getFrameRate(videoInput));
                        break;
                    }
                    else 
                    { 
                        SmallFunctions.Logging("Unfinished Job File found but not loaded " + file.Name);
                        if (MessageBox.Show("Delete Unfinished Job File: " + file.Name + "?", "Delete", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        { try { File.Delete(file.FullName); } catch { } }
                    }
                }
            }
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
                                            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + Path.Combine(tempPath, "Chunks", items) + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + Path.Combine(aomencPath, "aomenc.exe") + '\u0022' + " - --passes=1 " + allSettingsAom + " --output=" + '\u0022' + Path.Combine(tempPath, "Chunks", items + "-av1.ivf") + '\u0022';
                                            break;
                                        case "rav1e":
                                            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + Path.Combine(tempPath, "Chunks", items) + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + Path.Combine(rav1ePath, "rav1e.exe") + '\u0022' + " - " + allSettingsRav1e + " --output " + '\u0022' + Path.Combine(tempPath, "Chunks", items + "-av1.ivf") + '\u0022';
                                            break;
                                        case "aomenc (ffmpeg)":
                                            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + Path.Combine(tempPath, "Chunks", items) + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -strict experimental -c:v libaom-av1 " + allSettingsAom + " " + '\u0022' + Path.Combine(tempPath, "Chunks", items + "-av1.ivf") + '\u0022';
                                            break;
                                        case "svt-av1":
                                            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + Path.Combine(tempPath, "Chunks", items) + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -nostdin -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + Path.Combine(svtav1Path, "SvtAv1EncApp.exe") + '\u0022' + " -i stdin " + allSettingsSVTAV1 + " -b " + '\u0022' + Path.Combine(tempPath, "Chunks", items + "-av1.ivf") + '\u0022';
                                            break;
                                        case "libvpx-vp9":
                                            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + Path.Combine(tempPath, "Chunks", items) + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -c:v libvpx-vp9 " + allSettingsVP9 + " " + '\u0022' + Path.Combine(tempPath, "Chunks", items + "-av1.ivf") + '\u0022';
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
                                        if (deleteTempFilesDynamically) {
                                            try { File.Delete(Path.Combine(tempPath, "Chunks", items)); } catch { }
                                        }
                                    }
                                    else { SmallFunctions.KillInstances(); }
                                }
                                else if (videoPasses == 2)
                                {
                                    Process process = new Process();
                                    ProcessStartInfo startInfo = new ProcessStartInfo();

                                    bool FileExistFirstPass = File.Exists(Path.Combine(tempPath, "Chunks", items + "_1pass_successfull.log"));

                                    if (FileExistFirstPass != true)
                                    {
                                        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                        startInfo.FileName = "cmd.exe";
                                        startInfo.WorkingDirectory = ffmpegPath + "\\";

                                        switch (encoder)
                                        {
                                            case "aomenc":
                                                startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + Path.Combine(tempPath, "Chunks", items) + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + Path.Combine(aomencPath, "aomenc.exe") + '\u0022' + " - --passes=2 --pass=1 --fpf=" + '\u0022' + Path.Combine(tempPath, "Chunks", items + "_stats.log") + '\u0022' + " " + allSettingsAom + " --output=NUL";
                                                break;
                                            //case "rav1e": !!! RAV1E TWO PASS IS STILL BROKEN !!!
                                                //startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + tempPath + "\\Chunks\\" + items + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + rav1ePath + '\u0022' + " - " + allSettingsRav1e + " --first-pass " + '\u0022' + tempPath + "\\Chunks\\" + items + "_stats.log" + '\u0022';
                                                //break;
                                            case "aomenc (ffmpeg)":
                                                startInfo.Arguments = "/C ffmpeg.exe -y -i " + '\u0022' + Path.Combine(tempPath, "Chunks", items) + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -strict experimental -c:v libaom-av1 " + allSettingsAom + " -pass 1 -passlogfile " + '\u0022' + Path.Combine(tempPath, "Chunks", items + "_stats.log") + '\u0022' + " -f matroska NUL";
                                                break;
                                            case "svt-av1":
                                                startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + Path.Combine(tempPath, "Chunks", items) + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -nostdin -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + Path.Combine(svtav1Path, "SvtAv1EncApp.exe") + '\u0022' + " -i stdin " + allSettingsSVTAV1 + " -b NUL -output-stat-file " + '\u0022' + Path.Combine(tempPath, "Chunks", items + "-av1pass.stats") + '\u0022';
                                                break;
                                            case "libvpx-vp9":
                                                startInfo.Arguments = "/C ffmpeg.exe -y -i " + '\u0022' + Path.Combine(tempPath, "Chunks", items) + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -c:v libvpx-vp9 " + allSettingsVP9 + " -pass 1 -passlogfile " + '\u0022' + Path.Combine(tempPath, "Chunks", items + "_stats.log") + '\u0022' + " -f matroska NUL";
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

                                        if (SmallFunctions.Cancel.CancelAll == false) { SmallFunctions.WriteToFileThreadSafe("", tempPath + "\\Chunks\\" + items + "_1pass_successfull.log"); }
                                    }

                                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                    startInfo.FileName = "cmd.exe";
                                    startInfo.WorkingDirectory = ffmpegPath + "\\";

                                    switch (encoder)
                                    {
                                        case "aomenc":
                                            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + Path.Combine(tempPath, "Chunks", items) + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + Path.Combine(aomencPath, "aomenc.exe") + '\u0022' + " - --passes=2 --pass=2 --fpf=" + '\u0022' + Path.Combine(tempPath, "Chunks", items + "_stats.log") + '\u0022' + " " + allSettingsAom + " --output=" + '\u0022' + Path.Combine(tempPath, "Chunks", items + "-av1.ivf") + '\u0022';
                                            break;
                                        //case "rav1e": !!! RAV1E TWO PASS IS STILL BROKEN !!!
                                        //startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + tempPath + "\\Chunks\\" + items + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + rav1ePath + '\u0022' + " - " + allSettingsRav1e + " --second-pass " + '\u0022' + tempPath + "\\Chunks\\" + items + "_stats.log" + '\u0022' + " --output " + '\u0022' + tempPath + "\\Chunks\\" + items + "-av1.ivf" + '\u0022';
                                        //break;
                                        case "aomenc (ffmpeg)":
                                            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + Path.Combine(tempPath, "Chunks", items) + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -strict experimental -c:v libaom-av1 " + allSettingsAom + " -pass 2 -passlogfile " + '\u0022' + Path.Combine(tempPath, "Chunks", items + "_stats.log") + '\u0022' + " " + '\u0022' + Path.Combine(tempPath, "Chunks", items + "-av1.ivf") + '\u0022';
                                            break;
                                        case "svt-av1":
                                            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + Path.Combine(tempPath, "Chunks", items) + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -nostdin -vsync 0 -f yuv4mpegpipe - | " + '\u0022' + Path.Combine(svtav1Path, "SvtAv1EncApp.exe") + '\u0022' + " -i stdin " + allSettingsSVTAV1 + " -b " + '\u0022' + Path.Combine(tempPath, "Chunks", items + "-av1.ivf") + '\u0022'+ " -input-stat-file " + '\u0022' + Path.Combine(tempPath, "Chunks", items + "-av1pass.stats") + '\u0022';
                                            break;
                                        case "libvpx-vp9":
                                            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + Path.Combine(tempPath, "Chunks", items) + '\u0022' + " " + videoResize + " -pix_fmt " + pipeBitDepth + " -c:v libvpx-vp9 " + allSettingsVP9 + " -pass 2 -passlogfile " + '\u0022' + Path.Combine(tempPath, "Chunks", items + "_stats.log") + '\u0022' + " " + '\u0022' + Path.Combine(tempPath, "Chunks", items + "-av1.ivf") + '\u0022';
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
                                        if (deleteTempFilesDynamically)
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