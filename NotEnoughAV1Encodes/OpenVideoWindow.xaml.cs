using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using MahApps.Metro.Controls;
using ControlzEx.Theming;

namespace NotEnoughAV1Encodes
{
    public partial class OpenVideoWindow : MetroWindow
    {
        public string VideoPath { get; set; }
        public bool ProjectFile { get; set; }
        public bool QuitCorrectly { get; set; }
        public OpenVideoWindow(string baseTheme, string accentTheme)
        {
            InitializeComponent();
            ThemeManager.Current.ChangeTheme(this, baseTheme + "." + accentTheme);
        }
        private void ButtonOpenSingleSource_Click(object sender, RoutedEventArgs e)
        {
            // OpenFileDialog for a Single Video File
            OpenFileDialog openVideoFileDialog = new OpenFileDialog();
            openVideoFileDialog.Filter = "Video Files|*.mp4;*.mkv;*.webm;*.flv;*.avi;*.mov;*.wmv;|All Files|*.*";
            // Avoid NULL being returned resulting in crash
            Nullable<bool> result = openVideoFileDialog.ShowDialog();
            if (result == true)
            {
                // Sets the Video Path which the main window gets
                // with the function at the beginning
                VideoPath = openVideoFileDialog.FileName;
                ProjectFile = false;
                QuitCorrectly = true;
                // Closes the Window
                this.Close();
            }
        }

        private void ButtonBatchFile_Click(object sender, RoutedEventArgs e)
        {
            // To be implemented
        }

        private void ButtonProjectFile_Click(object sender, RoutedEventArgs e)
        {
            // OpenFileDialog for a Project File
            OpenFileDialog openVideoFileDialog = new OpenFileDialog();
            openVideoFileDialog.Filter = "Project File|*.xml;";
            openVideoFileDialog.InitialDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Jobs");
            // Avoid NULL being returned resulting in crash
            Nullable<bool> result = openVideoFileDialog.ShowDialog();
            if (result == true)
            {
                // Sets the Video Path which the main window gets
                // with the function at the beginning
                VideoPath = openVideoFileDialog.FileName;
                ProjectFile = true;
                QuitCorrectly = true;
                // Closes the Window
                this.Close();
            }
        }

        // ----------------------------------------------------------------
        // Other Functions to be Implemented
        // ----------------------------------------------------------------
    }
}
