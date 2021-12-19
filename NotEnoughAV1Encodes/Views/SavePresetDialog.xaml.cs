using ControlzEx.Theming;
using MahApps.Metro.Controls;
using System.Windows;

namespace NotEnoughAV1Encodes.Views
{
    public partial class SavePresetDialog : MetroWindow
    {
        public string PresetName { get; set; }
        public string PresetBatchName { get; set; }
        public bool Quit { get; set; }
        public SavePresetDialog(string _theme)
        {
            InitializeComponent();
            try { ThemeManager.Current.ChangeTheme(this, _theme); } catch { }
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
            Quit = true;
            Close();
        }
    }
}
