using System.Windows;
using System.Windows.Media;

namespace NotEnoughAV1Encodes
{
    /// <summary>
    /// Interaktionslogik für ShowSettings.xaml
    /// </summary>
    public partial class ShowSettings : Window
    {
        public ShowSettings(string textInput, bool uiTheme)
        {
            InitializeComponent();
            if(uiTheme) { 
            WindowSettings.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(25, 25, 25));
                TextBoxSettings.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 33, 33));
                TextBoxSettings.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                ButtonCloseWindow.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 33, 33));
                ButtonCloseWindow.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            }
            TextBoxSettings.Text = textInput;
            TextBoxSettings.Focus();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
