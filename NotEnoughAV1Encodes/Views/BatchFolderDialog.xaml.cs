using ControlzEx.Theming;
using MahApps.Metro.Controls;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NotEnoughAV1Encodes.Views
{
    public partial class BatchFolderDialog : MetroWindow
    {
        public bool Quit { get; set; }
        public string Preset { get; set; }
        public string Output { get; set; }
        public int Container { get; set; }

        public List<string> Files = new();

        public BatchFolderDialog(string theme, string folderPath)
        {
            InitializeComponent();
            try { ThemeManager.Current.ChangeTheme(this, theme); } catch { }

            // "Video Files|*.mp4;*.mkv;*.webm;*.flv;*.avi;*.mov;*.wmv;
            var files = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                        .Where(s => s.ToLower().EndsWith(".mp4")  || 
                                    s.ToLower().EndsWith(".mkv")  || 
                                    s.ToLower().EndsWith(".webm") || 
                                    s.ToLower().EndsWith(".flv")  ||
                                    s.ToLower().EndsWith(".avi")  ||
                                    s.ToLower().EndsWith(".mov")  ||
                                    s.ToLower().EndsWith(".wmv"));

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
        }

        private void ButtonSelectDestination_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog openFileDlg = new System.Windows.Forms.FolderBrowserDialog();
            var result = openFileDlg.ShowDialog();
            if (result.ToString() != string.Empty)
            {
                TextBoxDestination.Text = openFileDlg.SelectedPath;
            }
        }

        private void ButtonAddToQueue_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            foreach(string files in ListBoxVideoItems.Items) Files.Add(files);
            Container = ComboBoxContainer.SelectedIndex;
            Preset = ComboBoxPresets.SelectedItem.ToString();
            Output = TextBoxDestination.Text;
            Quit = true;
            Close();
        }

        private void ButtonCancelEncode_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }
    }
}
