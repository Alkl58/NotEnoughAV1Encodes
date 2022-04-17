using System;
using System.Diagnostics;
using System.Windows;
using ControlzEx.Theming;
using MahApps.Metro.Controls;
using Microsoft.Win32;

namespace NotEnoughAV1Encodes.Views
{
    public partial class OpenSource : MetroWindow
    {
        public string Path { get; set; }
        public bool Quit { get; set; }
        public bool BatchFolder { get; set; }
        public bool ProjectFile { get; set; }

        public OpenSource(string theme)
        {
            InitializeComponent();
            try { ThemeManager.Current.ChangeTheme(this, theme); } catch { }
        }

        private void ButtonOpenVideoFile_Click(object sender, RoutedEventArgs e)
        {
            // Single File Input
            OpenFileDialog openVideoFileDialog = new();
            openVideoFileDialog.Filter = "Video Files|*.mp4;*.mkv;*.webm;*.flv;*.avi;*.mov;*.wmv;*.ts|All Files|*.*";
            bool? result = openVideoFileDialog.ShowDialog();
            if (result == true)
            {
                Path = openVideoFileDialog.FileName;
                BatchFolder = false;
                ProjectFile = false;
                Quit = true;
                Close();
            }
        }

        private void ButtonOpenBatchFolder_Click(object sender, RoutedEventArgs e)
        {
            // Folder Input
            System.Windows.Forms.FolderBrowserDialog openFolderDialog = new();
            var result = openFolderDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                Path = openFolderDialog.SelectedPath;
                BatchFolder = true;
                ProjectFile = false;
                Quit = true;
                Close();
            }
        }

        private void ButtonOpenProjectFile_Click(object sender, RoutedEventArgs e)
        {
            // Project File Input
            OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = "JSON File|*.json;";
            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                Path = openFileDialog.FileName;
                BatchFolder = false;
                ProjectFile = true;
                Quit = true;
                Close();
            }
        }
    }
}
