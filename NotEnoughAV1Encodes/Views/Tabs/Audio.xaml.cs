using ControlzEx.Theming;
using Microsoft.Win32;
using NotEnoughAV1Encodes.Audio;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NotEnoughAV1Encodes.Views.Tabs
{
    public partial class Audio : Page
    {
        public List<AudioTracks> AudioTracks {  get; set; }
        public Audio()
        {
            InitializeComponent();
        }

        public void ThemeUpdate(string _theme)
        {
            try { ThemeManager.Current.ChangeTheme(this, _theme); } catch { }
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
                List<AudioTracks> AudioTracks = new();
                if (ListBoxAudioTracks.ItemsSource != null)
                {
                    AudioTracks = (List<AudioTracks>)ListBoxAudioTracks.ItemsSource;
                }
                foreach (string file in openAudioFilesDialog.FileNames)
                {
                    Debug.WriteLine(file);
                    AudioTracks.Add(MainWindow.videoDB.ParseMediaInfoAudio(file, MainWindow.PresetSettings));
                }

                try { ListBoxAudioTracks.Items.Clear(); } catch { }
                try { ListBoxAudioTracks.ItemsSource = null; } catch { }

                MainWindow.videoDB.AudioTracks = AudioTracks;
                ListBoxAudioTracks.ItemsSource = AudioTracks;
            }
        }
    }
}
