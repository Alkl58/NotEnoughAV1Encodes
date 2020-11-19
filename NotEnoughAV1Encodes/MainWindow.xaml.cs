using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NotEnoughAV1Encodes
{
    public partial class MainWindow : Window
    {

        // Final Commands
        public static string FilterCommand = null;
        public static string PipeBitDepthCommand = null;
        public static string EncoderAomencCommand = null;
        public static string EncoderRav1eCommand = null;
        public static string EncoderSvtAV1Command = null;
        // Temp Settings
        public static int WorkerCount = 0;          // amount of workers
        public static int EncodeMethod = 0;         // 0 = aomenc, 1 = rav1e, 2 = svt-av1...
        public static int SplitMethod = 0;          // 0 = ffmpeg; 1 = pyscenedetect; 2 = chunking
        public static bool OnePass = true;          // true = Onepass, false = Twopass
        public static bool Priority = true;         // true = normal, false = below normal (process priority)
        public static string[] VideoChunks;         // Array of command/videochunks
        // IO Paths
        public static string TempPath = Path.Combine(Path.GetTempPath(), "NEAV1E");
        public static string TempPathFileName = null;
        public static string VideoInput = null;     // Video Input Path
        public static string VideoOutput = null;    // Video Output Path
        public static bool VideoInputSet = false;   // Video Input Set Boolean
        public static bool VideoOutputSet = false;  // Video Output Set Boolean
        // Dependencies Paths
        public static string FFmpegPath = null;     // Path to ffmpeg
        public static string AomencPath = null;     // Path to aomenc
        public static string Rav1ePath = null;      // Path to rav1e
        public static string SvtAV1Path = null;     // Path to svt-av1
        // Temp Variables
        public static int TotalFrames = 0;          // used for progressbar and frame check
        public DateTime StartTime;                  // used for eta calculation

        public MainWindow()
        {
            InitializeComponent();
            Startup();
        }

        // ═══════════════════════════════════════ UI Logic ═══════════════════════════════════════

        private void Startup()
        {
            CheckDependencies.Check();

            // Sets the workercount combobox
            int corecount = SmallFunctions.getCoreCount();
            for (int i = 1; i <= corecount; i++) { ComboBoxWorkerCount.Items.Add(i); }
            ComboBoxWorkerCount.SelectedItem = Convert.ToInt32(corecount * 75 / 100);

        }

        private void ComboBoxVideoEncoder_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (SliderVideoSpeed != null)
            {
                if (ComboBoxVideoEncoder.SelectedIndex == 0)
                {
                    // aomenc
                    SliderVideoSpeed.Maximum = 9;
                    SliderVideoSpeed.Value = 4;
                    SliderVideoQuality.Value = 28;
                    SliderVideoQuality.Maximum = 63;
                }
                else if (ComboBoxVideoEncoder.SelectedIndex == 1)
                {
                    // rav1e
                    SliderVideoSpeed.Maximum = 10;
                    SliderVideoSpeed.Value = 6;
                    SliderVideoQuality.Maximum = 255;
                    SliderVideoQuality.Value = 100;
                }
                else if (ComboBoxVideoEncoder.SelectedIndex == 2)
                {
                    // svt-av1
                    SliderVideoSpeed.Maximum = 8;
                    SliderVideoSpeed.Value = 8;
                    SliderVideoQuality.Value = 50;
                    SliderVideoQuality.Maximum = 63;
                }
            }

        }

        private void CheckBoxCustomVideoSettings_Checked(object sender, RoutedEventArgs e)
        {
            // When Checking the custom encoding settings checkbox it will write the current settings to it
            if (CheckBoxCustomVideoSettings.IsChecked == true)
            {
                // Sets the Encoder Settings
                if (ComboBoxVideoEncoder.SelectedIndex == 0) { TextBoxCustomVideoSettings.Text = SetAomencCommand(); }
                if (ComboBoxVideoEncoder.SelectedIndex == 1) { TextBoxCustomVideoSettings.Text = SetRav1eCommand(); }
                if (ComboBoxVideoEncoder.SelectedIndex == 2) { TextBoxCustomVideoSettings.Text = SetSvtAV1Command(); }
            }
        }

        // ══════════════════════════════════════ Main Logic ══════════════════════════════════════

        public async void MainEntry()
        {
            if (!Directory.Exists(Path.Combine(TempPath, TempPathFileName, "Chunks")))
                Directory.CreateDirectory(Path.Combine(TempPath, TempPathFileName, "Chunks"));
                Directory.CreateDirectory(Path.Combine(TempPath, TempPathFileName, "Progress"));
            SetEncoderSettings();
            SetVideoFilters();
            SplitVideo();
            SetTempSettings();
            await Task.Run(() => SmallFunctions.GetSourceFrameCount());
            ProgressBar.Dispatcher.Invoke(() => ProgressBar.Maximum = TotalFrames);
            await Task.Run(() => EncodeVideo());
            await Task.Run(() => VideoMuxing.Concat());
            SmallFunctions.CheckVideoOutput();
            SmallFunctions.PlayFinishedSound();
        }

        private void SplitVideo()
        {
            // Temp Arguments for Splitting / Scenedetection
            bool reencodesplit = CheckBoxSplittingReencode.IsChecked == true;
            int splitmethod = ComboBoxSplittingMethod.SelectedIndex;
            int reencodeMethod = ComboBoxSplittingReencodeMethod.SelectedIndex;
            string ffmpegThreshold = TextBoxSplittingThreshold.Text;
            string chunkLength = TextBoxSplittingChunkLength.Text;
            VideoSplittingWindow videoSplittingWindow = new VideoSplittingWindow(splitmethod, reencodesplit, reencodeMethod, ffmpegThreshold, chunkLength);
            videoSplittingWindow.ShowDialog();
        }

        // ════════════════════════════════════ Temp Settings ═════════════════════════════════════

        private void SetTempSettings()
        {
            WorkerCount = int.Parse(ComboBoxWorkerCount.Text);      // Sets the worker count
            OnePass = ComboBoxVideoPasses.SelectedIndex == 0;       // Sets the amount of passes (true = 1, false = 2)
            Priority = ComboBoxProcessPriority.SelectedIndex == 0;  // Sets the Process Priority
            SplitMethod = ComboBoxSplittingMethod.SelectedIndex;    // Sets the Splitmethod, used for VideoEncode() function
            EncodeMethod = ComboBoxVideoEncoder.SelectedIndex;      // Sets the encoder (0 aomenc; 1 rav1e; 2 svt-av1)
            SmallFunctions.setVideoChunks(SplitMethod);             // Sets the array of videochunks/commands
            SetPipeCommand();
        }

        private void SetPipeCommand()
        {
            // Potential breaking point: 422p 8bit / 444p 8bit not being "-strict -1"

            PipeBitDepthCommand = " -pix_fmt yuv";
            if (ComboBoxVideoEncoder.SelectedIndex == 0)
            {
                // aomenc
                if (ComboBoxAomencColorFormat.SelectedIndex == 0)
                {
                    PipeBitDepthCommand += "420p";
                }
                else if (ComboBoxAomencColorFormat.SelectedIndex == 1)
                {
                    PipeBitDepthCommand += "422p";
                }
                else if (ComboBoxAomencColorFormat.SelectedIndex == 2)
                {
                    PipeBitDepthCommand += "444p";
                }
            }

            if (ComboBoxVideoEncoder.SelectedIndex == 1)
            {
                // rav1e
                if (ComboBoxRav1eColorFormat.SelectedIndex == 0)
                {
                    PipeBitDepthCommand += "420p";
                }
                else if (ComboBoxRav1eColorFormat.SelectedIndex == 1)
                {
                    PipeBitDepthCommand += "422p";
                }
                else if (ComboBoxRav1eColorFormat.SelectedIndex == 2)
                {
                    PipeBitDepthCommand += "444p";
                }
            }

            if (ComboBoxVideoEncoder.SelectedIndex == 1)
            {
                // svt-av1
                if (ComboBoxSVTAV1ColorFormat.SelectedIndex == 0)
                {
                    PipeBitDepthCommand += "420p";
                }
                else if (ComboBoxSVTAV1ColorFormat.SelectedIndex == 1)
                {
                    PipeBitDepthCommand += "422p";
                }
                else if (ComboBoxSVTAV1ColorFormat.SelectedIndex == 2)
                {
                    PipeBitDepthCommand += "444p";
                }
            }

            if (ComboBoxVideoBitDepth.SelectedIndex == 1)
            {
                // 10bit
                PipeBitDepthCommand += "10le -strict -1";
            }
            else if (ComboBoxVideoBitDepth.SelectedIndex == 2)
            {
                // 12bit
                PipeBitDepthCommand += "12le -strict -1";
            }
            // To-Do:
            // 422 / 444
            // Will be implemented once Subsampling in GUI has been implemented
        }

        // ════════════════════════════════════ Video Filters ═════════════════════════════════════

        private void SetVideoFilters()
        {
            bool crop = CheckBoxFiltersCrop.IsChecked == true;
            bool rotate = CheckBoxFiltersRotate.IsChecked == true;
            bool resize = CheckBoxFiltersResize.IsChecked == true;
            bool deinterlace = CheckBoxFiltersDeinterlace.IsChecked == true;
            int tempCounter = 0;

            if (crop == true || rotate == true || resize == true || deinterlace == true)
            {
                FilterCommand = " -vf ";
                if (crop == true)
                {
                    FilterCommand += VideoFiltersCrop();
                    tempCounter += 1;
                }
                if (rotate == true)
                {
                    if (tempCounter != 0) { FilterCommand += ","; }
                    FilterCommand += VideoFiltersRotate();
                    tempCounter += 1;
                }
                if (deinterlace == true)
                {
                    if (tempCounter != 0) { FilterCommand += ","; }
                    FilterCommand = VideoFiltersDeinterlace();
                    tempCounter += 1;
                }
                if (resize == true)
                {
                    // Has to be last, due to scaling algorithm
                    if (tempCounter != 0) { FilterCommand += ","; }
                    FilterCommand += VideoFiltersResize();
                }
            }
            else
            {
                // If not set it would give issues when encoding another video in same ui instance
                FilterCommand = "";
            }
        }

        private string VideoFiltersCrop()
        {
            // Sets the values for cropping the video
            string widthNew = (int.Parse(TextBoxFiltersCropRight.Text) + int.Parse(TextBoxFiltersCropLeft.Text)).ToString();
            string heightNew = (int.Parse(TextBoxFiltersCropTop.Text) + int.Parse(TextBoxFiltersCropBottom.Text)).ToString();
            return "crop=iw-" + widthNew + ":ih-" + heightNew + ":" + TextBoxFiltersCropLeft.Text + ":" + TextBoxFiltersCropTop.Text;
        }

        private string VideoFiltersRotate()
        {
            // Sets the values for rotating the video
            if (ComboBoxFiltersRotate.SelectedIndex == 1) return "transpose=1";
            else if (ComboBoxFiltersRotate.SelectedIndex == 2) return "transpose=2,transpose=2";
            else if (ComboBoxFiltersRotate.SelectedIndex == 3) return "transpose=2";
            else return ""; // If user selected no ratation but still has it enabled
        }

        private string VideoFiltersDeinterlace()
        {
            // Sets the values for deinterlacing the video
            return "yadif=" + ComboBoxFiltersDeinterlace.Text;
        }

        private string VideoFiltersResize()
        {
            // Sets the values for scaling the video
            return "scale=" + TextBoxFiltersResizeWidth.Text + ":" + TextBoxFiltersResizeHeight.Text + " -sws_flags " + ComboBoxFiltersScaling.Text;
        }

        // ══════════════════════════════════ Encoder Settings ════════════════════════════════════

        private void SetEncoderSettings()
        {
            if (CheckBoxCustomVideoSettings.IsChecked == false)
            {
                // Sets the Encoder Settings
                if (ComboBoxVideoEncoder.SelectedIndex == 0) { EncoderAomencCommand = SetAomencCommand(); }
                if (ComboBoxVideoEncoder.SelectedIndex == 1) { EncoderRav1eCommand = SetRav1eCommand(); }
                if (ComboBoxVideoEncoder.SelectedIndex == 2) { EncoderSvtAV1Command = SetSvtAV1Command(); }
            }
            else
            {
                // Custom Encoding Settings
                if (ComboBoxVideoEncoder.SelectedIndex == 0) { EncoderAomencCommand = " " + TextBoxCustomVideoSettings.Text; }
                if (ComboBoxVideoEncoder.SelectedIndex == 1) { EncoderRav1eCommand = " " + TextBoxCustomVideoSettings.Text; }
                if (ComboBoxVideoEncoder.SelectedIndex == 2) { EncoderSvtAV1Command = " " + TextBoxCustomVideoSettings.Text; }
            }

        }

        private string SetAomencCommand()
        {
            // Aomenc Command
            string cmd = "";
            cmd += " --bit-depth=" + ComboBoxVideoBitDepth.Text;    // Bit-Depth
            cmd += " --cpu-used=" + SliderVideoSpeed.Value;         // Speed

            // Constant Quality or Target Bitrate
            if (RadioButtonVideoConstantQuality.IsChecked == true) { cmd += " --end-usage=q --cq-level=" + SliderVideoQuality.Value; }
            else if (RadioButtonVideoBitrate.IsChecked == true) { cmd += " --end-usage=vbr --target-bitrate=" + TextBoxVideoBitrate.Text; }

            if (CheckBoxVideoAdvancedSettings.IsChecked == false)
            {
                // Default params when User don't select advanced settings
                cmd += " --threads=4 --tile-columns=2 --tile-rows=1";
            }
            else
            {
                // Advanced Settings
                cmd += " --threads=" + ComboBoxAomencThreads.Text;                                      // Threads
                cmd += " --tile-columns=" + ComboBoxAomencTileColumns.Text;                             // Tile Columns
                cmd += " --tile-rows=" + ComboBoxAomencTileRows.Text;                                   // Tile Rows
                cmd += " --lag-in-frames=" + TextBoxAomencLagInFrames.Text;                             // Lag in Frames
                cmd += " --sharpness=" + ComboBoxAomencSharpness.Text;                                  // Sharpness (Filter)
                cmd += " --aq-mode=" + ComboBoxAomencAQMode.SelectedIndex;                              // AQ-Mode
                cmd += " --enable-keyframe-filtering=" + ComboBoxAomencKeyFiltering.SelectedIndex;      // Key Frame Filtering
                cmd += " --tune=" + ComboBoxAomencTune.Text;                                            // Tune
                if (TextBoxAomencMaxGOP.Text != "0")
                {
                    cmd += " --kf-max-dist=" + TextBoxAomencMaxGOP.Text;                                // Keyframe Interval
                }
                if (CheckBoxAomencRowMT.IsChecked == false)
                {
                    cmd += " --row-mt=0";                                                               // Row Based Multithreading
                }
                if (CheckBoxAomencCDEF.IsChecked == false)
                {
                    cmd += " --enable-cdef=0";                                                          // Constrained Directional Enhancement Filter
                }
                if (ComboBoxAomencColorPrimaries.SelectedIndex != 0)
                {
                    cmd += " --color-primaries=" + ComboBoxAomencColorPrimaries.Text;                   // Color Primaries
                }
                if (ComboBoxAomencColorTransfer.SelectedIndex != 0)
                {
                    cmd += " --transfer-characteristics=" + ComboBoxAomencColorTransfer.Text;           // Color Transfer
                }
                if (ComboBoxAomencColorMatrix.SelectedIndex != 0)
                {
                    cmd += " --matrix-coefficients=" + ComboBoxAomencColorMatrix.Text;                  // Color Matrix
                }
                if (ComboBoxAomencColorFormat.SelectedIndex != 0)
                {
                    cmd += " --" + ComboBoxAomencColorFormat.Text;                                      // Color Space
                }
                if (CheckBoxAomencARNRMax.IsChecked == true)
                {
                    cmd += " --arnr-maxframes=" + ComboBoxAomencARNRMax.Text;                           // ARNR Maxframes
                    cmd += " --arnr-strength=" + ComboBoxAomencARNRStrength.Text;                       // ARNR Strength
                }
            }

            return cmd;
        }

        private string SetRav1eCommand()
        {
            // Rav1e Command
            string cmd = "";
            cmd += " --speed " + SliderVideoSpeed.Value;    // Speed

            // Constant Quality or Target Bitrate
            if (RadioButtonVideoConstantQuality.IsChecked == true) { cmd += " --quantizer " + SliderVideoQuality.Value; }
            else if (RadioButtonVideoBitrate.IsChecked == true) { cmd += " --bitrate " + TextBoxVideoBitrate.Text; }

            if (CheckBoxVideoAdvancedSettings.IsChecked == false)
            {
                // Default params when User don't select advanced settings
                cmd += " --threads 4 --tile-cols 2 --tile-rows 1";
            }
            else
            {
                cmd += " --threads " + ComboBoxRav1eThreads.SelectedIndex;                              // Threads
                cmd += " --tile-cols " + ComboBoxRav1eTileColumns.SelectedIndex;                        // Tile Columns
                cmd += " --tile-rows " + ComboBoxRav1eTileRows.SelectedIndex;                           // Tile Rows
                cmd += " --rdo-lookahead-frames " + TextBoxRav1eLookahead.Text;                         // RDO Lookahead
                cmd += " --tune " + ComboBoxRav1eTune.Text;                                             // Tune
                if (TextBoxRav1eMaxGOP.Text != "0")
                {
                    cmd += " --keyint " + TextBoxRav1eMaxGOP.Text;                                      // Keyframe Interval
                }
                if (ComboBoxRav1eColorPrimaries.SelectedIndex != 0)
                {
                    cmd += " --primaries " + ComboBoxRav1eColorPrimaries.Text;                          // Color Primaries
                }
                if (ComboBoxRav1eColorTransfer.SelectedIndex != 0)
                {
                    cmd += " --transfer " + ComboBoxRav1eColorTransfer.Text;                            // Color Transfer
                }
                if (ComboBoxRav1eColorMatrix.SelectedIndex != 0)
                {
                    cmd += " --matrix " + ComboBoxRav1eColorMatrix.Text;                                // Color Matrix
                }
                if (CheckBoxRav1eMasteringDisplay.IsChecked == true)
                {
                    cmd += " --mastering-display G(" + TextBoxRav1eMasteringGx.Text + ",";              // Mastering Gx
                    cmd += TextBoxRav1eMasteringGy.Text + ")B(";                                        // Mastering Gy
                    cmd += TextBoxRav1eMasteringBx.Text + ",";                                          // Mastering Bx
                    cmd += TextBoxRav1eMasteringBy.Text + ")R(";                                        // Mastering By
                    cmd += TextBoxRav1eMasteringRx.Text + ",";                                          // Mastering Rx
                    cmd += TextBoxRav1eMasteringRy.Text + ")WP(";                                       // Mastering Ry
                    cmd += TextBoxRav1eMasteringWPx.Text + ",";                                         // Mastering WPx
                    cmd += TextBoxRav1eMasteringWPy.Text + ")L(";                                       // Mastering WPy
                    cmd += TextBoxRav1eMasteringLx.Text + ",";                                          // Mastering Lx
                    cmd += TextBoxRav1eMasteringLy.Text + ")";                                          // Mastering Ly
                }
                if (CheckBoxRav1eContentLight.IsChecked == true)
                {
                    cmd += " --content-light " + TextBoxRav1eContentLightCll.Text;                      // Content Light CLL
                    cmd += "," + TextBoxRav1eContentLightFall.Text;                                     // Content Light FALL
                }
            }

            return cmd;
        }

        private string SetSvtAV1Command()
        {
            string cmd = "";
            cmd += " --preset " + SliderVideoSpeed.Value;

            // Constant Quality or Target Bitrate
            if (RadioButtonVideoConstantQuality.IsChecked == true) { cmd += " --rc 0 -q " + SliderVideoQuality.Value; }
            else if (RadioButtonVideoBitrate.IsChecked == true) 
            {
                cmd += " --rc 1";
                cmd += " --tbr " + TextBoxVideoBitrate.Text; 
            }

            if (CheckBoxVideoAdvancedSettings.IsChecked == true)
            {
                cmd += " --tile-columns " + ComboBoxSVTAV1TileColumns.Text;                             // Tile Columns
                cmd += " --tile-rows " + ComboBoxSVTAV1TileRows.Text;                                   // Tile Rows
                cmd += " --keyint " + TextBoxSVTAV1MaxGOP.Text;                                         // Keyframe Interval
                cmd += " --lookahead " + TextBoxSVTAV1Lookahead.Text;                                   // Lookahead
                cmd += " --adaptive-quantization " + ComboBoxSVTAV1AQMode.SelectedIndex;                // AQ-Mode
                cmd += " --profile " + ComboBoxSVTAV1Profile.SelectedIndex;                             // Bitstream Profile
                if (ComboBoxSVTAV1AltRefLevel.SelectedIndex != 0)
                {
                    cmd += " --tf-level " + ComboBoxSVTAV1AltRefLevel.Text;                             // AltRef Level
                }
                if (ComboBoxSVTAV1AltRefStrength.SelectedIndex != 5)
                {
                    cmd += " --altref-strength " + ComboBoxSVTAV1AltRefStrength.SelectedIndex;          // AltRef Strength
                }
                if (ComboBoxSVTAV1AltRefFrames.SelectedIndex != 7)
                {
                    cmd += " --altref-nframes " + ComboBoxSVTAV1AltRefFrames.SelectedIndex;             // AltRef Frames
                }
                if (CheckBoxSVTAV1HDR.IsChecked == true)
                {
                    cmd += " --enable-hdr 1";                                                           // HDR
                }
            }

            return cmd;
        }

        // ══════════════════════════════════════ Buttons ═════════════════════════════════════════

        private void ButtonOpenSource_Click(object sender, RoutedEventArgs e)
        {
            // Creates a new object of the type "OpenVideoWindow"
            OpenVideoWindow WindowVideoSource = new OpenVideoWindow();
            // Hides the main user interface
            this.Hide();
            // Shows the just created window object and awaits its exit
            WindowVideoSource.ShowDialog();
            // Shows the main user interface
            this.Show();
            // Uses the public get method in OpenVideoSource window to get variable
            string result = WindowVideoSource.VideoPath;
            if (result != null)
                VideoInputSet = true;
            // Sets the label in the user interface
            // Note that this has to be edited once batch encoding is added as function
            TextBoxVideoSource.Text = result;
            VideoInput = result;
            TempPathFileName = Path.GetFileNameWithoutExtension(result);
        }

        private void ButtonOpenDestination_Click(object sender, RoutedEventArgs e)
        {
            // Note that this has to be edited once batch encoding is being implemented
            // Save File Dialog for single file saving
            SaveFileDialog saveVideoFileDialog = new SaveFileDialog();
            saveVideoFileDialog.Filter = "Video|*.mkv;*.webm;*.mp4";
            // Avoid NULL being returned resulting in crash
            Nullable<bool> result = saveVideoFileDialog.ShowDialog();
            if (result == true)
            {
                TextBoxVideoDestination.Text = saveVideoFileDialog.FileName;
                VideoOutput = saveVideoFileDialog.FileName;
                VideoOutputSet = true;
            }
        }

        private void ButtonStartEncode_Click(object sender, RoutedEventArgs e)
        {
            if (VideoInputSet == true && VideoOutputSet == true) { MainEntry(); }
            else { MessageBox.Show("Input or Output not set!", "IO", MessageBoxButton.OK); }
        }

        // ═══════════════════════════════════ Progress Bar ═══════════════════════════════════════

        private void ProgressBarUpdating()
        {
            // Gets all Progress Files of ffmpeg
            string[] filePaths = Directory.GetFiles(Path.Combine(TempPath, TempPathFileName, "Progress"), "*.log", SearchOption.AllDirectories);

            int totalencodedframes = 0;

            // Sets the total framecount
            int totalframes = TotalFrames;

            // The amount of frames doubles when in two pass mode
            if (OnePass != true)
                totalframes = totalframes * 2;

            foreach (string file in filePaths)
            {
                // Reads the progress file of ffmpeg without locking it up
                Stream stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                TextReader objstream = new StreamReader(stream);

                // Reads the content of the stream
                string text = objstream.ReadToEnd();

                // Closes the stream reader
                stream.Close();

                // Splits every line
                string[] lines = text.Split( new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None );

                string tempvalue = "";

                // Iterates over all lines
                foreach (var line in lines)
                {
                    // Checks if the line contains the word "frame="
                    if (line.Contains("frame=")) { tempvalue = line.Remove(0, 6); }
                }

                try
                {
                    // Adds the framecount to the total encoded frames
                    totalencodedframes += int.Parse(tempvalue);
                }
                catch { }
                objstream.Close();
            }

            // Gets the so far spent time
            TimeSpan timespent = DateTime.Now - StartTime;
            try
            {
                // Setting Label & Progressbar
                LabelProgressBar.Dispatcher.Invoke(() => LabelProgressBar.Content = totalencodedframes + " / " + totalframes + " Frames - " + Math.Round(totalencodedframes / timespent.TotalSeconds, 2) + "fps - " + Math.Round(((timespent.TotalSeconds / totalencodedframes) * (totalframes - totalencodedframes)) / 60, MidpointRounding.ToEven) + "min left");
                ProgressBar.Dispatcher.Invoke(() => ProgressBar.Value = totalencodedframes);
            }
            catch { }
        }

        // ══════════════════════════════════ Video Encoding ══════════════════════════════════════

        private void EncodeVideo()
        {
            // Starts "a timer" for eta / fps calculation
            DateTime starttime = DateTime.Now;
            StartTime = starttime;
            bool encodeStarted = true;
            Task taskProgressBar = new Task(() =>
            {
                while (encodeStarted)
                {
                    ProgressBarUpdating();
                    // Waits 1s before updating
                    Thread.Sleep(1000); 
                }
            });
            taskProgressBar.Start();
            // Main Encoding Function
            // Creates a new Thread Pool
            using (SemaphoreSlim concurrencySemaphore = new SemaphoreSlim(WorkerCount))
            {
                // Creates a tasks list
                List<Task> tasks = new List<Task>();
                // Iterates over all args in VideoChunks list
                foreach (var command in VideoChunks)
                {
                    concurrencySemaphore.Wait();
                    var task = Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            // We need the index of the command in the array
                            var index = Array.FindIndex(VideoChunks, row => row.Contains(command));

                            // Logic for resume mode - skips already encoded files
                            if (File.Exists(Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + ".ivf" + "_finished.log")) == false)
                            {
                                // One Pass Encoding
                                Process ffmpegProcess = new Process();
                                ProcessStartInfo startInfo = new ProcessStartInfo();
                                startInfo.UseShellExecute = true;
                                startInfo.FileName = "cmd.exe";
                                startInfo.WorkingDirectory = FFmpegPath;

                                string InputVideo = "";
                                // FFmpeg Scene Detect or PySceneDetect
                                if (SplitMethod == 0 || SplitMethod == 1) { InputVideo = " -i " + '\u0022' + VideoInput + '\u0022' + " " + command; }
                                else if (SplitMethod == 2) { InputVideo = " -i " + '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", command) + '\u0022'; } // Chunk based splitting

                                string FFmpegProgress = " -progress " + '\u0022' + Path.Combine(TempPath, TempPathFileName, "Progress", "split" + index.ToString("D5") + "_progress.log") + '\u0022';

                                // Logic to skip first pass encoding if "_finished" log file exists
                                if (File.Exists(Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + "_stats.log" + "_finished.log")) == false)
                                {
                                    if (EncodeMethod == 0) // aomenc
                                    {
                                        string aomencCMD = "";
                                        string output = "";
                                        string ffmpegPipe = InputVideo + " " + FilterCommand + PipeBitDepthCommand + " -color_range 0 -vsync 0 -f yuv4mpegpipe - | ";

                                        if (OnePass) // One Pass Encoding
                                        {
                                            aomencCMD = '\u0022' + Path.Combine(AomencPath, "aomenc.exe") + '\u0022' + " - --passes=1" + EncoderAomencCommand + " --output=";
                                            output = '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + ".ivf") + '\u0022';
                                        }
                                        else // Two Pass Encoding First Pass
                                        {
                                            aomencCMD = '\u0022' + Path.Combine(AomencPath, "aomenc.exe") + '\u0022' + " - --passes=2 --pass=1" + EncoderAomencCommand + " --fpf=";
                                            output = '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + "_stats.log") + '\u0022' + " --output=NUL";
                                        }
                                        Console.WriteLine("/C ffmpeg.exe" + FFmpegProgress + ffmpegPipe + aomencCMD + output);
                                        startInfo.Arguments = "/C ffmpeg.exe" + FFmpegProgress + ffmpegPipe + aomencCMD + output;
                                    }
                                    else if (EncodeMethod == 1) // rav1e
                                    {

                                        string ffmpegPipe = InputVideo + " " + FilterCommand + PipeBitDepthCommand + " -color_range 0 -vsync 0 -f yuv4mpegpipe - | ";
                                        string rav1eCMD = '\u0022' + Path.Combine(Rav1ePath, "rav1e.exe") + '\u0022' + " - " + EncoderRav1eCommand + " --output ";
                                        string output = '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + ".ivf") + '\u0022';
                                        Console.WriteLine("/C ffmpeg.exe" + FFmpegProgress + ffmpegPipe + rav1eCMD + output);
                                        startInfo.Arguments = "/C ffmpeg.exe" + FFmpegProgress + ffmpegPipe + rav1eCMD + output;
                                    }
                                    else if (EncodeMethod == 2) // svt-av1
                                    {
                                        string svtav1CMD = "";
                                        string output = "";
                                        string ffmpegPipe = InputVideo + " " + FilterCommand + PipeBitDepthCommand + " -color_range 0 -nostdin -vsync 0 -f yuv4mpegpipe - | ";
                                        if (OnePass)
                                        {
                                            // One Pass Encoding
                                            svtav1CMD = '\u0022' + Path.Combine(Rav1ePath, "SvtAv1EncApp.exe") + '\u0022' + " -i stdin " + EncoderSvtAV1Command + " --passes 1 -b ";
                                            output = '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + ".ivf") + '\u0022';
                                        }
                                        else
                                        {
                                            // Two Pass Encoding First Pass
                                            svtav1CMD = '\u0022' + Path.Combine(Rav1ePath, "SvtAv1EncApp.exe") + '\u0022' + " -i stdin " + EncoderSvtAV1Command + " --irefresh-type 2 --pass 1 -b NUL --stats ";
                                            output = '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + "_stats.log") + '\u0022';
                                        }
                                        
                                        Console.WriteLine("/C ffmpeg.exe" + FFmpegProgress + ffmpegPipe + svtav1CMD + output);
                                        startInfo.Arguments = "/C ffmpeg.exe" + FFmpegProgress + ffmpegPipe + svtav1CMD + output;
                                    }

                                    ffmpegProcess.StartInfo = startInfo;
                                    ffmpegProcess.Start();

                                    // Sets the process priority
                                    if (Priority == false)
                                        ffmpegProcess.PriorityClass = ProcessPriorityClass.BelowNormal;

                                    ffmpegProcess.WaitForExit();

                                    if (OnePass == false)
                                    {
                                        // Writes log file if first pass is finished, to be able to skip them later if in resume mode
                                        SmallFunctions.WriteToFileThreadSafe("", Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + "_stats.log" + "_finished.log"));
                                    }

                                }

                                if (OnePass != true)
                                {
                                    // Two Pass Encoding Second Pass
                                    if (EncodeMethod == 0) // aomenc
                                    {
                                        string ffmpegPipe = InputVideo + " " + FilterCommand + PipeBitDepthCommand + " -color_range 0 -vsync 0 -f yuv4mpegpipe - | ";
                                        string aomencCMD = '\u0022' + Path.Combine(AomencPath, "aomenc.exe") + '\u0022' + " - --passes=2 --pass=2" + EncoderAomencCommand + " --fpf=";
                                        string outputLog = '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + "_stats.log") + '\u0022';
                                        string outputVid = " --output=" + '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + ".ivf") + '\u0022';
                                        Console.WriteLine("/C ffmpeg.exe" + FFmpegProgress + ffmpegPipe + aomencCMD + outputLog + outputVid);
                                        startInfo.Arguments = "/C ffmpeg.exe" + FFmpegProgress + ffmpegPipe + aomencCMD + outputLog + outputVid;
                                    }
                                    else if (EncodeMethod == 1) // rav1e
                                    {
                                        // Rav1e 2 Pass still broken
                                    }
                                    else if (EncodeMethod == 2) // svt-av1
                                    {
                                        string ffmpegPipe = InputVideo + " " + FilterCommand + PipeBitDepthCommand + " -color_range 0 -nostdin -vsync 0 -f yuv4mpegpipe - | ";
                                        string svtav1CMD = '\u0022' + Path.Combine(Rav1ePath, "SvtAv1EncApp.exe") + '\u0022' + " -i stdin " + EncoderSvtAV1Command + " --irefresh-type 2 --pass 2 --stats ";
                                        string stats = '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + "_stats.log") + '\u0022';
                                        string outputVid = " -b " + '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + ".ivf") + '\u0022';
                                        Console.WriteLine("/C ffmpeg.exe" + FFmpegProgress + ffmpegPipe + svtav1CMD + stats + outputVid);
                                        startInfo.Arguments = "/C ffmpeg.exe" + FFmpegProgress + ffmpegPipe + svtav1CMD + stats + outputVid;
                                    }

                                    ffmpegProcess.StartInfo = startInfo;
                                    ffmpegProcess.Start();

                                    // Sets the process priority
                                    if (Priority == false)
                                        ffmpegProcess.PriorityClass = ProcessPriorityClass.BelowNormal;

                                    ffmpegProcess.WaitForExit();
                                }
                                // This function will write finished encodes to a log file, to be able to skip them if in resume mode
                                SmallFunctions.WriteToFileThreadSafe("", Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + ".ivf" + "_finished.log"));
                            }
                        }
                        finally { concurrencySemaphore.Release();}
                    });
                    tasks.Add(task);
                }
                Task.WaitAll(tasks.ToArray());
            }

            encodeStarted = false;
        }

    }
}
