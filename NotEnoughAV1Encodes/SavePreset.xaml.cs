using System.Windows;
using System.Windows.Media;

namespace NotEnoughAV1Encodes
{
    /// <summary>
    /// Interaktionslogik für SavePreset.xaml
    /// </summary>
    public partial class SavePreset : Window
    {
        public SavePreset(bool uiTheme)
        {
            InitializeComponent();
            if (uiTheme)
            {
                WindowSavePreset.Background = new SolidColorBrush(Color.FromRgb(25, 25, 25));
                TextBoxPresetName.Background = new SolidColorBrush(Color.FromRgb(33, 33, 33));
                TextBoxPresetName.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                ButtonCloseWindow.Background = new SolidColorBrush(Color.FromRgb(33, 33, 33));
                ButtonCloseWindow.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                ButtonSavePreset.Background = new SolidColorBrush(Color.FromRgb(33, 33, 33));
                ButtonSavePreset.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            }
        }

        private void ButtonCloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ButtonSavePreset_Click(object sender, RoutedEventArgs e)
        {
            SmallFunctions.checkCreateFolder("Profiles");
            MainWindow.saveSettings = true;
            MainWindow.saveSettingString = TextBoxPresetName.Text;
            this.Close();
        }
    }
}
