using System.Windows;

namespace NotEnoughAV1Encodes
{
    public partial class SavePreset : Window
    {
        public string SaveName { get; set; }
        public bool cancel { get; set; }
        public SavePreset()
        {
            InitializeComponent();
        }
        private void ButtonCloseWindow_Click(object sender, RoutedEventArgs e)
        {
            cancel = true;
            // Closes the Window
            this.Close();
        }

        private void ButtonSavePreset_Click(object sender, RoutedEventArgs e)
        {
            // Saves the Preset
            SaveName = TextBoxPresetName.Text;
            cancel = false;

            // Closes the Window
            this.Close();
        }
    }
}
