using ControlzEx.Theming;
using MahApps.Metro.Controls;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace NotEnoughAV1Encodes.Views
{
    public partial class BatchFolderDialog : MetroWindow
    {
        public bool Quit { get; set; }
        public bool PresetBitdepth { get; set; }
        public bool ActivateSubtitles { get; set; } = true;
        public bool MirrorFolderStructure { get; set; }
        public string Input { get; set; }
        public string Preset { get; set; }
        public string Output { get; set; }
        public int Container { get; set; }

        public List<string> Files = new();

        private bool OutputSelected = false;

        public BatchFolderDialog(string theme, string folderPath, bool subfolders)
        {
            InitializeComponent();
            try { ThemeManager.Current.ChangeTheme(this, theme); } catch { }

            Input = folderPath;

            SearchOption searchOption = SearchOption.TopDirectoryOnly;
            if (subfolders)
            {
                searchOption = SearchOption.AllDirectories;
            }

            // "Video Files|*.mp4;*.mkv;*.webm;*.flv;*.avi;*.mov;*.wmv;
            var files = Directory.EnumerateFiles(folderPath, "*.*", searchOption)
                        .Where(s => s.ToLower().EndsWith(".mp4")  || 
                                    s.ToLower().EndsWith(".mkv")  || 
                                    s.ToLower().EndsWith(".webm") || 
                                    s.ToLower().EndsWith(".flv")  ||
                                    s.ToLower().EndsWith(".avi")  ||
                                    s.ToLower().EndsWith(".mov")  ||
                                    s.ToLower().EndsWith(".wmv")  ||
                                    s.ToLower().EndsWith(".mpg")  ||
                                    s.ToLower().EndsWith(".ts"));

            foreach (var file in files)
            {
                ListBoxVideoItems.Items.Add(file);
            }

            // Load Presets
            if (Directory.Exists(Path.Combine(Global.AppData, "NEAV1E", "Presets")))
            {
                string[] filePaths = Directory.GetFiles(Path.Combine(Global.AppData, "NEAV1E", "Presets"), "*.json", SearchOption.TopDirectoryOnly);

                foreach (string file in filePaths)
                {
                    ComboBoxPresets.Items.Add(Path.GetFileNameWithoutExtension(file));
                }

                try { ComboBoxPresets.SelectedIndex = 0; } catch { }
            }

            // Set Default Values
            ToggleSwitchUsePresetBitDepth.IsOn = true;
        }

        private void ButtonSelectDestination_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog openFileDlg = new System.Windows.Forms.FolderBrowserDialog();
            var result = openFileDlg.ShowDialog();
            if (result.ToString() != string.Empty)
            {
                TextBoxDestination.Text = openFileDlg.SelectedPath;
                OutputSelected = true;
            }
        }

        private void ListBoxVideoItems_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (ListBoxVideoItems.SelectedItem == null) return;
            if (e.Key == System.Windows.Input.Key.Delete)
            {
                ListBoxVideoItems.Items.Remove(ListBoxVideoItems.SelectedItem);
            }
        }

        private void QueueMenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxVideoItems.SelectedItem == null) return;
            ListBoxVideoItems.Items.Remove(ListBoxVideoItems.SelectedItem);
        }

        private void ButtonAddToQueue_Click(object sender, RoutedEventArgs e)
        {
            // Return if Output is not selected
            if (!OutputSelected)
            {
                
                MessageBoxResult result = MessageBox.Show("Please select a Destination!", "Error", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    ButtonSelectDestination_Click(sender, e);
                }
                return;
            }
            else
            {
                foreach (string files in ListBoxVideoItems.Items) Files.Add(files);
                Container = ComboBoxContainer.SelectedIndex;
                Preset = ComboBoxPresets.SelectedItem.ToString();
                Output = TextBoxDestination.Text;
                PresetBitdepth = ToggleSwitchUsePresetBitDepth.IsOn;
                ActivateSubtitles = ToggleSwitchActivateSubtitles.IsOn;
                MirrorFolderStructure = ToggleSwitchMirrorFolderStructure.IsOn;
                Quit = true;
                Close();
            }
        }

        private void ButtonCancelEncode_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }
    }
}
