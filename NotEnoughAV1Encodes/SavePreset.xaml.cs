using System.Windows;
using ControlzEx.Theming;
using MahApps.Metro.Controls;

namespace NotEnoughAV1Encodes
{
    public partial class SavePreset : MetroWindow
    {
        public string SaveName { get; set; }
        public bool Cancel { get; set; }
        public SavePreset(string baseTheme, string accentTheme)
        {
            InitializeComponent();
            ThemeManager.Current.ChangeTheme(this, baseTheme + "." + accentTheme);
        }
        private void ButtonCloseWindow_Click(object sender, RoutedEventArgs e)
        {
            Cancel = true;
            // Closes the Window
            this.Close();
        }

        private void ButtonSavePreset_Click(object sender, RoutedEventArgs e)
        {
            // Saves the Preset
            SaveName = TextBoxPresetName.Text;
            Cancel = false;

            // Closes the Window
            this.Close();
        }
    }
}
