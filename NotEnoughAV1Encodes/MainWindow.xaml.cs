using System;
using System.Diagnostics;
using System.Windows;
using MahApps.Metro.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace NotEnoughAV1Encodes
{
    public partial class MainWindow : MetroWindow
    {
        public Views.ProgramSettings programSettings = new();
        private readonly Video.VideoDB videoDB = new();
        private int ProgramState;

        public MainWindow()
        {
            InitializeComponent();
            resources.MediaLanguages.FillDictionary();
        }

        #region Buttons
        private void ButtonProgramSettings_Click(object sender, RoutedEventArgs e)
        {
            programSettings.ShowDialog();
        }

        private void ButtonOpenSource_Click(object sender, RoutedEventArgs e)
        {
            Views.OpenSource openSource = new();
            openSource.ShowDialog();
            if (openSource.Quit)
            {
                videoDB.InputPath = openSource.Path;
                videoDB.ParseMediaInfo();
                ListBoxAudioTracks.Items.Clear();
                ListBoxAudioTracks.ItemsSource = videoDB.AudioTracks;
                TextBoxVideoSource.Content = videoDB.InputPath;
                LabelVideoLength.Content = videoDB.MIDuration;
                LabelVideoResolution.Content = videoDB.MIWidth + "x" + videoDB.MIHeight;
                LabelVideoColorFomat.Content = videoDB.MIChromaSubsampling;
                string vfr = "";
                if (videoDB.MIIsVFR)
                {
                    vfr = " (VFR)";
                }
                LabelVideoFramerate.Content = videoDB.MIFramerate + vfr;
            }
        }

        private void ButtonSetDestination_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveVideoFileDialog = new()
            {
                Filter = "MKV Video|*.mkv|WebM Video|*.webm|MP4 Video|*.mp4"
            };

            if (saveVideoFileDialog.ShowDialog() == true)
            {
                videoDB.OutputPath = saveVideoFileDialog.FileName;
                TextBoxVideoSource.Content = videoDB.OutputPath;
            }
        }

        private void ButtonStartStop_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(videoDB.InputPath) && !string.IsNullOrEmpty(videoDB.OutputPath))
            {
                if (ProgramState is 0 or 2)
                {
                    ProgramState = 1;
                    ImageStartStop.Source = new BitmapImage(new Uri(@"/NotEnoughAV1Encodes;component/resources/img/pause.png", UriKind.Relative));

                    // Main Start
                    if (ProgramState is 0)
                    {

                    }

                    // Resume all PIDs
                    if (ProgramState is 2)
                    {

                    }
                }
                else if (ProgramState is 1)
                {
                    ProgramState = 2;
                    ImageStartStop.Source = new BitmapImage(new Uri(@"/NotEnoughAV1Encodes;component/resources/img/resume.png", UriKind.Relative));
                }
            }
            else
            {
                // To-Do: Error Meldung
            }
        }
        #endregion

        #region UI Functions
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Validates that the TextBox Input are only numbers
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            programSettings.Close();
        }
        #endregion
    }
}
