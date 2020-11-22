using System.Windows;

namespace NotEnoughAV1Encodes
{
    public partial class SavePreset : Window
    {
        public SavePreset()
        {
            InitializeComponent();
        }
        private void ButtonCloseWindow_Click(object sender, RoutedEventArgs e)
        {
            // Closes the Window
            this.Close();
        }

        private void ButtonSavePreset_Click(object sender, RoutedEventArgs e)
        {
            // Saves the Preset
            SaveSettings saveSettings = new SaveSettings(true, TextBoxPresetName.Text);
            // Closes the Window
            this.Close();
        }
    }
}
