using Newtonsoft.Json;
using System.IO;
using System;
using System.Windows.Controls;
using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Input;
using NotEnoughAV1Encodes.Video;
using System.Collections.Generic;

namespace NotEnoughAV1Encodes.Controls
{
    public partial class SummaryTab : UserControl
    {
        public string Encoder { get; set; }

        public SummaryTab()
        {
            InitializeComponent();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Validates that the TextBox Input are only numbers
            Regex regex = new("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void ButtonDeletePreset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                File.Delete(Path.Combine(Global.AppData, "NEAV1E", "Presets", ComboBoxPresets.Text + ".json"));
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }

            try
            {
                ComboBoxPresets.Items.Clear();
                LoadPresets();
            }
            catch { }

        }

        public void LoadPresets()
        {
            // Load Presets
            if (Directory.Exists(Path.Combine(Global.AppData, "NEAV1E", "Presets")))
            {
                string[] filePaths = Directory.GetFiles(Path.Combine(Global.AppData, "NEAV1E", "Presets"), "*.json", SearchOption.TopDirectoryOnly);

                foreach (string file in filePaths)
                {
                    ComboBoxPresets.Items.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
        }

        private void ButtonSetPresetDefault_Click(object sender, RoutedEventArgs e)
        {
            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow.settingsDB.DefaultPreset = ComboBoxPresets.Text;

            SaveSettings();
        }

        private void SaveSettings()
        {
            try
            {
                // Get MainWindow instance to access UI elements
                MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

                Directory.CreateDirectory(Path.Combine(Global.AppData, "NEAV1E"));
                File.WriteAllText(Path.Combine(Global.AppData, "NEAV1E", "settings.json"), JsonConvert.SerializeObject(mainWindow.settingsDB, Formatting.Indented));
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void ButtonSavePreset_Click(object sender, RoutedEventArgs e)
        {
            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            Views.SavePresetDialog savePresetDialog = new(mainWindow.settingsDB.Theme);
            savePresetDialog.ShowDialog();
            if (savePresetDialog.Quit)
            {
                Directory.CreateDirectory(Path.Combine(Global.AppData, "NEAV1E", "Presets"));
                mainWindow.PresetSettings.PresetBatchName = savePresetDialog.PresetBatchName;
                mainWindow.PresetSettings.AudioCodecMono = savePresetDialog.AudioCodecMono;
                mainWindow.PresetSettings.AudioCodecStereo = savePresetDialog.AudioCodecStereo;
                mainWindow.PresetSettings.AudioCodecSixChannel = savePresetDialog.AudioCodecSixChannel;
                mainWindow.PresetSettings.AudioCodecEightChannel = savePresetDialog.AudioCodecEightChannel;
                mainWindow.PresetSettings.AudioBitrateMono = savePresetDialog.AudioBitrateMono;
                mainWindow.PresetSettings.AudioBitrateStereo = savePresetDialog.AudioBitrateStereo;
                mainWindow.PresetSettings.AudioBitrateSixChannel = savePresetDialog.AudioBitrateSixChannel;
                mainWindow.PresetSettings.AudioBitrateEightChannel = savePresetDialog.AudioBitrateEightChannel;
                File.WriteAllText(Path.Combine(Global.AppData, "NEAV1E", "Presets", savePresetDialog.PresetName + ".json"), JsonConvert.SerializeObject(mainWindow.PresetSettings, Formatting.Indented));
                ComboBoxPresets.Items.Clear();
                LoadPresets();
            }
        }

        public bool presetLoadLock = false;
        private void ComboBoxPresets_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ComboBoxPresets.SelectedItem == null) return;
            try
            {
                // Get MainWindow instance to access UI elements
                MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

                presetLoadLock = true;
                mainWindow.PresetSettings = JsonConvert.DeserializeObject<VideoSettings>(File.ReadAllText(Path.Combine(Global.AppData, "NEAV1E", "Presets", ComboBoxPresets.SelectedItem.ToString() + ".json")));
                mainWindow.DataContext = mainWindow.PresetSettings;
                presetLoadLock = false;

                ApplyPresetAudioToCurrentVideo();
            }
            catch { }
        }

        private void ApplyPresetAudioToCurrentVideo()
        {
            try
            {
                // Get MainWindow instance to access UI elements
                MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

                if (mainWindow.AudioTabControl.ListBoxAudioTracks.ItemsSource == null) return;
                mainWindow.videoDB.AudioTracks = (List<Audio.AudioTracks>) mainWindow.AudioTabControl.ListBoxAudioTracks.ItemsSource;
                try { mainWindow.AudioTabControl.ListBoxAudioTracks.Items.Clear(); } catch { }
                try { mainWindow.AudioTabControl.ListBoxAudioTracks.ItemsSource = null; } catch { }

                foreach (Audio.AudioTracks audioTrack in mainWindow.videoDB.AudioTracks)
                {
                    switch (audioTrack.Channels)
                    {
                        case 0:
                            audioTrack.Bitrate = mainWindow.PresetSettings.AudioBitrateMono.ToString();
                            audioTrack.Codec = mainWindow.PresetSettings.AudioCodecMono;
                            break;
                        case 1:
                            audioTrack.Bitrate = mainWindow.PresetSettings.AudioBitrateStereo.ToString();
                            audioTrack.Codec = mainWindow.PresetSettings.AudioCodecStereo;
                            break;
                        case 2:
                            audioTrack.Bitrate = mainWindow.PresetSettings.AudioBitrateSixChannel.ToString();
                            audioTrack.Codec = mainWindow.PresetSettings.AudioCodecSixChannel;
                            break;
                        case 3:
                            audioTrack.Bitrate = mainWindow.PresetSettings.AudioBitrateEightChannel.ToString();
                            audioTrack.Codec = mainWindow.PresetSettings.AudioCodecEightChannel;
                            break;
                        default:
                            break;
                    }
                }

                mainWindow.AudioTabControl.ListBoxAudioTracks.ItemsSource = mainWindow.videoDB.AudioTracks;
            }
            catch { }
        }
    }
}
