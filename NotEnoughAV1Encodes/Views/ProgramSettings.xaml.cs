using ControlzEx.Theming;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace NotEnoughAV1Encodes.Views
{
    public partial class ProgramSettings : MetroWindow
    {
        public bool DeleteTempFiles { get; set; }
        public bool ShutdownAfterEncode { get; set; }
        public bool OverrideWorkerCount { get; set; }
        public int BaseTheme { get; set; }
        public int AccentTheme { get; set; }
        public string Theme { get; set; }
        public string BGImage { get; set; }
        public ProgramSettings(SettingsDB settingsDB)
        {
            InitializeComponent();
            ToggleSwitchDeleteTempFiles.IsOn = settingsDB.DeleteTempFiles;
            ToggleSwitchShutdown.IsOn = settingsDB.ShutdownAfterEncode;
            ToggleSwitchOverrideWorkerCount.IsOn = settingsDB.OverrideWorkerCount;
            ComboBoxAccentTheme.SelectedIndex = settingsDB.AccentTheme;
            ComboBoxBaseTheme.SelectedIndex = settingsDB.BaseTheme;
            BGImage = settingsDB.BGImage;
            try
            {
                ThemeManager.Current.ChangeTheme(this, settingsDB.Theme);
            }
            catch { }
        }

        private void ButtonSelectBGImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
            if (openFileDialog.ShowDialog() == true)
            {
                BGImage = openFileDialog.FileName;
            }
        }

        private void ButtonUpdater_Click(object sender, RoutedEventArgs e)
        {
            Updater updater = new(ComboBoxBaseTheme.Text, ComboBoxAccentTheme.Text);
            updater.ShowDialog();
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DeleteTempFiles = ToggleSwitchDeleteTempFiles.IsOn;
            ShutdownAfterEncode = ToggleSwitchShutdown.IsOn;
            OverrideWorkerCount = ToggleSwitchOverrideWorkerCount.IsOn;
            BaseTheme = ComboBoxBaseTheme.SelectedIndex;
            AccentTheme = ComboBoxAccentTheme.SelectedIndex;
            Theme = ComboBoxBaseTheme.Text + "." + ComboBoxAccentTheme.Text;
        }

        private void ButtonResetBGImage_Click(object sender, RoutedEventArgs e)
        {
            BGImage = null;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            ProcessStartInfo psi = new()
            {
                FileName = "cmd",
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = $"/c start {e.Uri}"
            };
            Process.Start(psi);
        }

        private void ButtonExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
