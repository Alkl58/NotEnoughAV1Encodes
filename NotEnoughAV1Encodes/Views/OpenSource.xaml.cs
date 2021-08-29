using System;
using System.Windows;
using MahApps.Metro.Controls;
using Microsoft.Win32;

namespace NotEnoughAV1Encodes.Views
{
    public partial class OpenSource : MetroWindow
    {
        public string Path { get; set; }
        public bool Quit { get; set; }
        public OpenSource()
        {
            InitializeComponent();
        }

        private void ButtonOpenVideoFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openVideoFileDialog = new();
            openVideoFileDialog.Filter = "Video Files|*.mp4;*.mkv;*.webm;*.flv;*.avi;*.mov;*.wmv;|All Files|*.*";
            bool? result = openVideoFileDialog.ShowDialog();
            if (result == true)
            {
                Path = openVideoFileDialog.FileName;
                Quit = true;
                Close();
            }
        }
    }
}
