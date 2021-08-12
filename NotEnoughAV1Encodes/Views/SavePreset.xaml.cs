using ControlzEx.Theming;
using MahApps.Metro.Controls;
using System.Windows;

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