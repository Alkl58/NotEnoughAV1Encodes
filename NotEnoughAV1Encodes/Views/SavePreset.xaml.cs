using ControlzEx.Theming;
using MahApps.Metro.Controls;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace NotEnoughAV1Encodes
{
    public partial class SavePreset : MetroWindow
    {
        public string SaveName { get; set; }
        public bool Cancel { get; set; }
        public string AudioCodec { get; set; }
        public string AudioBitrate { get; set; }

        public SavePreset(string baseTheme, string accentTheme)
        {
            InitializeComponent();
            ThemeManager.Current.ChangeTheme(this, baseTheme + "." + accentTheme);
            Cancel = true;
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Validates that the TextBox Input are only numbers
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void ButtonCloseWindow_Click(object sender, RoutedEventArgs e)
        {
            Cancel = true;
            // Closes the Window
            Close();
        }

        private void ButtonSavePreset_Click(object sender, RoutedEventArgs e)
        {
            // Saves the Preset
            SaveName = TextBoxPresetName.Text;
            AudioCodec = ComboBoxAudioCodec.Text;
            AudioBitrate = TextBoxBitrate.Text;
            Cancel = false;

            // Closes the Window
            Close();
        }

        private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if(LabelBitrate != null)
                LabelBitrate.IsEnabled = TextBoxBitrate.IsEnabled = ComboBoxAudioCodec.SelectedIndex != 5;
        }
    }
}