using ControlzEx.Theming;
using MahApps.Metro.Controls;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace NotEnoughAV1Encodes.Views
{
    public partial class SavePresetDialog : MetroWindow
    {
        public string PresetName { get; set; }
        public string PresetBatchName { get; set; }
        public int AudioCodecMono { get; set; }
        public int AudioBitrateMono { get; set; }
        public int AudioCodecStereo { get; set; }
        public int AudioBitrateStereo { get; set; } 
        public int AudioCodecSixChannel { get; set; }
        public int AudioBitrateSixChannel { get; set; }
        public int AudioCodecEightChannel { get; set; }
        public int AudioBitrateEightChannel { get; set; }
        public bool Quit { get; set; }
        public SavePresetDialog(string _theme)
        {
            InitializeComponent();
            try { ThemeManager.Current.ChangeTheme(this, _theme); } catch { }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Validates that the TextBox Input are only numbers
            Regex regex = new("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBoxPresetName.Text))
            {
                MessageBox.Show("Baka! You need to set a preset name!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            PresetName = TextBoxPresetName.Text;
            PresetBatchName = TextBoxBatchFileName.Text;
            // Mono Audio
            AudioCodecMono = ComboBoxAudioCodecMono.SelectedIndex;
            AudioBitrateMono = int.Parse(TextBoxAudioBitrateMono.Text);
            // Stereo Audio
            AudioCodecStereo = ComboBoxAudioCodecStereo.SelectedIndex;
            AudioBitrateMono = int.Parse(TextBoxAudioBitrateStereo.Text);
            // 5.1 Audio
            AudioCodecSixChannel = ComboBoxAudioCodecSixChannel.SelectedIndex;
            AudioBitrateSixChannel = int.Parse(TextBoxAudioBitrateSixChannel.Text);
            // 7.1 Audio
            AudioCodecEightChannel = ComboBoxAudioCodecEightChannel.SelectedIndex;
            AudioBitrateEightChannel = int.Parse(TextBoxAudioBitrateEightChannel.Text);
            Quit = true;
            Close();
        }
    }
}
