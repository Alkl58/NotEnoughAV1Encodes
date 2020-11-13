using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;

namespace NotEnoughAV1Encodes
{
    public partial class MainWindow : Window
    {

        // Final Commands
        public static string FilterCommand = null;
        // Temp Paths
        public static string TempPath = Path.Combine(Path.GetTempPath(), "NEAV1E");
        public static string TempPathFileName = null;
        public static string VideoInput = null;
        public static string FFmpegPath = null;

        public MainWindow()
        {
            InitializeComponent();
            CheckDependencies.Check();
        }

        // ════════════════════════════════════ Video Filters ═════════════════════════════════════

        private void VideoFilters()
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
                FilterCommand = null;
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
            return "scale=" + TextBoxFiltersResizeWidth.Text + ":" + TextBoxFiltersResizeHeight.Text + " -sws_flag " + ComboBoxFiltersScaling.Text;
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
            }
        }

        private void ButtonStartEncode_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(Path.Combine(TempPath, TempPathFileName, "Chunks")))
                Directory.CreateDirectory(Path.Combine(TempPath, TempPathFileName, "Chunks"));

            bool reencodesplit = CheckBoxSplittingReencode.IsChecked == true;
            int splitmethod = ComboBoxSplittingMethod.SelectedIndex;
            int reencodeMethod = ComboBoxSplittingReencodeMethod.SelectedIndex;
            string ffmpegThreshold = TextBoxSplittingThreshold.Text;

            VideoSplittingWindow videoSplittingWindow = new VideoSplittingWindow(splitmethod, reencodesplit, reencodeMethod, ffmpegThreshold);
            videoSplittingWindow.ShowDialog();

        }
    }
}
