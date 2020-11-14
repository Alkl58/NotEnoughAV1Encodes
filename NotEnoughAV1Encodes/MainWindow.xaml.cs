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
        public static int WorkerCount = 0;  // amount of workers
        public static int EncodeMethod = 0; // 0 = aomenc, 1 = rav1e, 2 = svt-av1...
        public static int SplitMethod = 0;  // 0 = ffmpeg; 1 = pyscenedetect; 2 = chunking
        public static bool OnePass = true;  // true = Onepass, false = Twopass
        public static bool Priority = true; // true = normal, false = below normal (process priority)
        public static string[] VideoChunks; // Array of command/videochunks
        // IO Paths
        public static string TempPath = Path.Combine(Path.GetTempPath(), "NEAV1E");
        public static string TempPathFileName = null;
        public static string VideoInput = null;     // Video Input Path
        public static string VideoOutput = null;    // Video Output Path
        public static bool VideoInputSet = false;
        public static bool VideoOutputSet = false;
        // Dependencies Paths
        public static string FFmpegPath = null; // Path to ffmpeg
        public static string AomencPath = null; // Path to aomenc
        public static string Rav1ePath = null;  // Path to rav1e
        public static string SvtAV1Path = null; // Path to svt-av1

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

        // ══════════════════════════════════════ Main Logic ══════════════════════════════════════

        public async void MainEntry()
        {
            if (!Directory.Exists(Path.Combine(TempPath, TempPathFileName, "Chunks")))
                Directory.CreateDirectory(Path.Combine(TempPath, TempPathFileName, "Chunks"));
            SetEncoderSettings();
            SetVideoFilters();
            SplitVideo();
            SetTempSettings();
            await Task.Run(() => EncodeVideo());
            await Task.Run(() => VideoMuxing.Concat());
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
            PipeBitDepthCommand = " -pix_fmt yuv";
            PipeBitDepthCommand += "420p";
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
            // Sets the Encoder Settings
            if (ComboBoxVideoEncoder.SelectedIndex == 0) { EncoderAomencCommand = SetAomencCommand(); }
            if (ComboBoxVideoEncoder.SelectedIndex == 1) { EncoderRav1eCommand = SetRav1eCommand(); }
            if (ComboBoxVideoEncoder.SelectedIndex == 2) { EncoderSvtAV1Command = SetSvtAV1Command(); }
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
            LabelVideoSource.Content = result;
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
                LabelVideoDestination.Content = saveVideoFileDialog.FileName;
                VideoOutput = saveVideoFileDialog.FileName;
                VideoOutputSet = true;
            }
        }

        private void ButtonStartEncode_Click(object sender, RoutedEventArgs e)
        {
            if (VideoInputSet == true && VideoOutputSet == true) { MainEntry(); }
            else { MessageBox.Show("Input or Output not set!", "IO", MessageBoxButton.OK); }
        }

        // ══════════════════════════════════ Video Encoding ══════════════════════════════════════

        private void EncodeVideo()
        {
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
                                        Console.WriteLine("/C ffmpeg.exe" + ffmpegPipe + aomencCMD + output);
                                        startInfo.Arguments = "/C ffmpeg.exe" + ffmpegPipe + aomencCMD + output;
                                    }
                                    else if (EncodeMethod == 1) // rav1e
                                    {

                                        string ffmpegPipe = InputVideo + " " + FilterCommand + PipeBitDepthCommand + " -color_range 0 -vsync 0 -f yuv4mpegpipe - | ";
                                        string rav1eCMD = '\u0022' + Path.Combine(Rav1ePath, "rav1e.exe") + '\u0022' + " - " + EncoderRav1eCommand + " --output ";
                                        string output = '\u0022' + Path.Combine(TempPath, TempPathFileName, "Chunks", "split" + index.ToString("D5") + ".ivf") + '\u0022';
                                        Console.WriteLine("/C ffmpeg.exe" + ffmpegPipe + rav1eCMD + output);
                                        startInfo.Arguments = "/C ffmpeg.exe" + ffmpegPipe + rav1eCMD + output;
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
                                        
                                        Console.WriteLine("/C ffmpeg.exe" + ffmpegPipe + svtav1CMD + output);
                                        startInfo.Arguments = "/C ffmpeg.exe" + ffmpegPipe + svtav1CMD + output;
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
                                        Console.WriteLine("/C ffmpeg.exe" + ffmpegPipe + aomencCMD + outputLog + outputVid);
                                        startInfo.Arguments = "/C ffmpeg.exe" + ffmpegPipe + aomencCMD + outputLog + outputVid;
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
                                        Console.WriteLine("/C ffmpeg.exe" + ffmpegPipe + svtav1CMD + stats + outputVid);
                                        startInfo.Arguments = "/C ffmpeg.exe" + ffmpegPipe + svtav1CMD + stats + outputVid;
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
        }

    }
}
