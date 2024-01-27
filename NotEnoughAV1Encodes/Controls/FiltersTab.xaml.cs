using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace NotEnoughAV1Encodes.Controls
{
    public partial class FiltersTab : UserControl
    {
        public FiltersTab()
        {
            InitializeComponent();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Validates that the TextBox Input are only numbers
            Regex regex = new("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void ToggleSwitchFilterCrop_Toggled(object sender, EventArgs e)
        {
            if (MainWindow.startupLock) return;

            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            CreateCropPreviewsOnLoad(mainWindow);
        }

        private void ButtonCropAutoDetect_Click(object sender, EventArgs e)
        {
            if (MainWindow.startupLock) return;

            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            AutoCropDetect(mainWindow);
        }

        public string GenerateVideoFilters()
        {
            if (MainWindow.startupLock) return "";

            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            bool crop = ToggleSwitchFilterCrop.IsOn;
            bool rotate = ToggleSwitchFilterRotate.IsOn;
            bool resize = ToggleSwitchFilterResize.IsOn;
            bool deinterlace = ToggleSwitchFilterDeinterlace.IsOn;
            bool fps = mainWindow.VideoTabVideoPartialControl.ComboBoxVideoFrameRate.SelectedIndex != 0;
            bool oneFilter = false;

            string FilterCommand = "";

            if (crop || rotate || resize || deinterlace || fps)
            {
                FilterCommand = " -vf ";
                if (crop)
                {
                    FilterCommand += VideoFiltersCrop();
                    oneFilter = true;
                }
                if (resize)
                {
                    if (oneFilter) { FilterCommand += ","; }
                    FilterCommand += VideoFiltersResize();
                    oneFilter = true;
                }
                if (rotate)
                {
                    if (oneFilter) { FilterCommand += ","; }
                    FilterCommand += VideoFiltersRotate();
                    oneFilter = true;
                }
                if (deinterlace)
                {
                    if (oneFilter) { FilterCommand += ","; }
                    FilterCommand += VideoFiltersDeinterlace();
                    oneFilter = true;
                }
                if (fps)
                {
                    if (oneFilter) { FilterCommand += ","; }
                    FilterCommand += mainWindow.VideoTabVideoPartialControl.GenerateFFmpegFramerate();
                }
            }


            return FilterCommand;
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
            int filterIndex = ComboBoxFiltersDeinterlace.SelectedIndex;
            string filter = "";

            if (filterIndex == 0)
            {
                filter = "bwdif=mode=0";
            }
            else if (filterIndex == 1)
            {
                filter = "estdif=mode=0";
            }
            else if (filterIndex == 2)
            {
                string bin = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "nnedi", "nnedi3_weights.bin");
                bin = bin.Replace("\u005c", "\u005c\u005c").Replace(":", "\u005c:");
                filter = "nnedi=weights='" + bin + "'";
            }
            else if (filterIndex == 3)
            {
                filter = "yadif=mode=0";
            }

            return filter;
        }

        private string VideoFiltersResize()
        {
            // Auto Set Width
            if (TextBoxFiltersResizeWidth.Text == "0")
            {
                return "scale=trunc(oh*a/2)*2:" + TextBoxFiltersResizeHeight.Text + ":flags=" + ComboBoxResizeAlgorithm.Text;
            }

            // Auto Set Height
            if (TextBoxFiltersResizeHeight.Text == "0")
            {
                return "scale=" + TextBoxFiltersResizeWidth.Text + ":trunc(ow/a/2)*2:flags=" + ComboBoxResizeAlgorithm.Text;
            }

            return "scale=" + TextBoxFiltersResizeWidth.Text + ":" + TextBoxFiltersResizeHeight.Text + ":flags=" + ComboBoxResizeAlgorithm.Text;

        }

        private void ButtonCropPreviewForward_Click(object sender, EventArgs e)
        {
            if (MainWindow.startupLock) return;

            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (mainWindow.videoDB.InputPath == null) return;
            int index = int.Parse(LabelCropPreview.Content.ToString().Split("/")[0]) + 1;
            if (index > 4)
                index = 1;
            LabelCropPreview.Content = index.ToString() + "/4";

            LoadCropPreview(index);
        }
        private void ButtonCropPreviewBackward_Click(object sender, EventArgs e)
        {
            if (MainWindow.startupLock) return;

            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (mainWindow.videoDB.InputPath == null) return;
            int index = int.Parse(LabelCropPreview.Content.ToString().Split("/")[0]) - 1;
            if (index < 1)
                index = 4;
            LabelCropPreview.Content = index.ToString() + "/4";

            LoadCropPreview(index);
        }

        public async void CreateCropPreviewsOnLoad(MainWindow mainWindow)
        {
            if (MainWindow.startupLock) return;

            if (!IsLoaded) return;

            if (mainWindow.videoDB.InputPath == null) return;

            if (!ToggleSwitchFilterCrop.IsOn)
            {
                ImageCropPreview.Source = new BitmapImage(new Uri("pack://application:,,,/NotEnoughAV1Encodes;component/resources/img/videoplaceholder.jpg")); ;
                return;
            }

            string crop = "-vf " + VideoFiltersCrop();

            await Task.Run(() => CreateCropPreviews(mainWindow, crop));

            try
            {
                int index = int.Parse(LabelCropPreview.Content.ToString().Split("/")[0]);

                MemoryStream memStream = new(File.ReadAllBytes(Path.Combine(Global.Temp, "NEAV1E", "crop_preview_" + index.ToString() + ".bmp")));
                BitmapImage bmi = new();
                bmi.BeginInit();
                bmi.StreamSource = memStream;
                bmi.EndInit();
                ImageCropPreview.Source = bmi;
            }
            catch { }
        }

        public void DeleteCropPreviews()
        {
            for (int i = 1; i < 5; i++)
            {
                string image = Path.Combine(Global.Temp, "NEAV1E", "crop_preview_" + i.ToString() + ".bmp");
                if (File.Exists(image))
                {
                    try
                    {
                        File.Delete(image);
                    }
                    catch { }
                }
            }
        }

        private async void AutoCropDetect(MainWindow mainWindow)
        {
            if (MainWindow.startupLock) return;

            if (mainWindow.videoDB.InputPath == null) return;

            List<string> cropList = new();

            string time = mainWindow.videoDB.MIDuration;

            int seconds = Convert.ToInt32(Math.Floor(TimeSpan.Parse(time).TotalSeconds / 4));

            // Use the current frame as start point of detection
            int index = int.Parse(LabelCropPreview.Content.ToString().Split("/")[0]);

            string command = "/C ffmpeg.exe -ss " + (index * seconds).ToString() + " -i \"" + mainWindow.videoDB.InputPath + "\" -vf cropdetect=24:2:0 -t 5  -f null -";

            Process ffmpegProcess = new();
            ProcessStartInfo startInfo = new()
            {
                FileName = "cmd.exe",
                WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "FFmpeg"),
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = command,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true
            };

            ffmpegProcess.StartInfo = startInfo;
            ffmpegProcess.Start();

            string lastLine;
            while (!ffmpegProcess.StandardError.EndOfStream)
            {
                lastLine = ffmpegProcess.StandardError.ReadLine();
                if (lastLine.Contains("crop="))
                {
                    cropList.Add(lastLine.Split("crop=")[1]);
                }
            }

            ffmpegProcess.WaitForExit();

            // Get most occuring value
            string crop = cropList.Where(c => !string.IsNullOrEmpty(c)).GroupBy(a => a).OrderByDescending(b => b.Key[1].ToString()).First().Key;

            try
            {
                // Translate Output to crop values
                int cropTop = int.Parse(crop.Split(":")[3]);
                TextBoxFiltersCropTop.Text = cropTop.ToString();
                mainWindow.PresetSettings.FilterCropTop = cropTop.ToString();

                int cropLeft = int.Parse(crop.Split(":")[2]);
                TextBoxFiltersCropLeft.Text = cropLeft.ToString();
                mainWindow.PresetSettings.FilterCropLeft = cropLeft.ToString();

                int cropBottom = mainWindow.videoDB.MIHeight - cropTop - int.Parse(crop.Split(":")[1]);
                TextBoxFiltersCropBottom.Text = cropBottom.ToString();
                mainWindow.PresetSettings.FilterCropBottom = cropBottom.ToString();

                int cropRight = mainWindow.videoDB.MIWidth - cropLeft - int.Parse(crop.Split(":")[0]);
                TextBoxFiltersCropRight.Text = cropRight.ToString();
                mainWindow.PresetSettings.FilterCropRight = cropRight.ToString();


                string cropNew = "-vf " + VideoFiltersCrop();
                await Task.Run(() => CreateCropPreviews(mainWindow,cropNew));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void CreateCropPreviews(MainWindow mainWindow, string crop)
        {
            if (MainWindow.startupLock) return;

            Directory.CreateDirectory(Path.Combine(Global.Temp, "NEAV1E"));

            string time = mainWindow.videoDB.MIDuration;
            int seconds = Convert.ToInt32(Math.Floor(TimeSpan.Parse(time).TotalSeconds / 4));

            for (int i = 1; i < 5; i++)
            {
                // Extract Frames
                string command = "/C ffmpeg.exe -y -ss " + (i * seconds).ToString() + " -i \"" + mainWindow.videoDB.InputPath + "\" -vframes 1 " + crop + " \"" + Path.Combine(Global.Temp, "NEAV1E", "crop_preview_" + i.ToString() + ".bmp") + "\"";

                Process ffmpegProcess = new();
                ProcessStartInfo startInfo = new()
                {
                    UseShellExecute = true,
                    FileName = "cmd.exe",
                    WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "FFmpeg"),
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = command
                };

                ffmpegProcess.StartInfo = startInfo;
                ffmpegProcess.Start();
                ffmpegProcess.WaitForExit();
            }
        }

        private void LoadCropPreview(int index)
        {
            string input = Path.Combine(Global.Temp, "NEAV1E", "crop_preview_" + index.ToString() + ".bmp");
            if (!File.Exists(input)) return;

            try
            {
                MemoryStream memStream = new(File.ReadAllBytes(input));
                BitmapImage bmi = new();
                bmi.BeginInit();
                bmi.StreamSource = memStream;
                bmi.EndInit();
                ImageCropPreview.Source = bmi;
            }
            catch { }
        }

        public string VideoFiltersCrop()
        {
            // Sets the values for cropping the video
            string widthNew;
            string heightNew;
            try
            {
                widthNew = (int.Parse(TextBoxFiltersCropRight.Text) + int.Parse(TextBoxFiltersCropLeft.Text)).ToString();
                heightNew = (int.Parse(TextBoxFiltersCropTop.Text) + int.Parse(TextBoxFiltersCropBottom.Text)).ToString();
            }
            catch
            {
                widthNew = "0";
                heightNew = "0";
            }

            return "crop=iw-" + widthNew + ":ih-" + heightNew + ":" + TextBoxFiltersCropLeft.Text + ":" + TextBoxFiltersCropTop.Text;
        }
    }
}
