using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NotEnoughAV1Encodes.Controls
{
    public partial class AdvancedTab : UserControl
    {
        public AdvancedTab()
        {
            InitializeComponent();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Validates that the TextBox Input are only numbers
            Regex regex = new("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void CheckBoxCustomVideoSettings_Toggled(object sender, RoutedEventArgs e)
        {
            if (MainWindow.startupLock) return;

            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (CheckBoxCustomVideoSettings.IsOn && mainWindow.SummaryTabControl.presetLoadLock == false && IsLoaded)
            {
                TextBoxCustomVideoSettings.Text = mainWindow.GenerateEncoderCommand();
            }
        }

        private void TextBoxCustomVideoSettings_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (MainWindow.startupLock) return;

            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            // Verifies the arguments the user inputs into the encoding settings textbox
            // If the users writes a "forbidden" argument, it will display the text red
            string[] forbiddenWords = { "help", "cfg", "debug", "output", "passes", "pass", "fpf", "limit",
            "skip", "webm", "ivf", "obu", "q-hist", "rate-hist", "fullhelp", "benchmark", "first-pass", "second-pass",
            "reconstruction", "enc-mode-2p", "input-stat-file", "output-stat-file" };

            foreach (string word in forbiddenWords)
            {
                if (mainWindow.settingsDB.BaseTheme == 0)
                {
                    // Lightmode
                    TextBoxCustomVideoSettings.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                }
                else
                {
                    // Darkmode
                    TextBoxCustomVideoSettings.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                }

                if (TextBoxCustomVideoSettings.Text.Contains(word))
                {
                    TextBoxCustomVideoSettings.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                    break;
                }
            }
        }

        private void ButtonTestSettings_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.startupLock) return;

            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            Views.TestCustomSettings testCustomSettings = new(mainWindow.settingsDB.Theme, mainWindow.VideoTabVideoPartialControl.ComboBoxVideoEncoder.SelectedIndex, mainWindow.AdvancedTabControl.CheckBoxCustomVideoSettings.IsOn ? mainWindow.AdvancedTabControl.TextBoxCustomVideoSettings.Text : mainWindow.GenerateEncoderCommand());
            testCustomSettings.ShowDialog();
        }
    }
}
