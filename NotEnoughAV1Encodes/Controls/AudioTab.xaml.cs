using Microsoft.Win32;
using NotEnoughAV1Encodes.Video;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NotEnoughAV1Encodes.Controls
{
    public partial class AudioTab : UserControl
    {
        public AudioTab()
        {
            InitializeComponent();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Validates that the TextBox Input are only numbers
            Regex regex = new("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void AudioTracksImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openAudioFilesDialog = new()
            {
                Filter = "Audio Files|*.mp3;*.aac;*.flac;*.m4a;*.ogg;*.opus;*.wav;*.wma|All Files|*.*",
                Multiselect = true
            };

            bool? result = openAudioFilesDialog.ShowDialog();
            if (result == true)
            {
                List<Audio.AudioTracks> AudioTracks = new();

                // Get MainWindow instance to access UI elements
                MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

                if (ListBoxAudioTracks.ItemsSource != null)
                {
                    AudioTracks = (List<Audio.AudioTracks>) ListBoxAudioTracks.ItemsSource;
                }
                foreach (string file in openAudioFilesDialog.FileNames)
                {
                    Debug.WriteLine(file);
                    AudioTracks.Add(mainWindow.videoDB.ParseMediaInfoAudio(file, mainWindow.PresetSettings));
                }

                try { ListBoxAudioTracks.Items.Clear(); } catch { }
                try { ListBoxAudioTracks.ItemsSource = null; } catch { }
                try { mainWindow.SubtitlesTabControl.ListBoxSubtitleTracks.Items.Clear(); } catch { }
                try { mainWindow.SubtitlesTabControl.ListBoxSubtitleTracks.ItemsSource = null; } catch { }

                mainWindow.videoDB.AudioTracks = AudioTracks;
                ListBoxAudioTracks.ItemsSource = AudioTracks;
            }
        }
    }
}
