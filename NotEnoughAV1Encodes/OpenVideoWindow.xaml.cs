using Microsoft.Win32;
using System;
using System.Windows;

namespace NotEnoughAV1Encodes
{
    public partial class OpenVideoWindow : Window
    {
        public string VideoPath { get; set; }
        public OpenVideoWindow()
        {
            InitializeComponent();
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
                // Closes the Window
                this.Close();
            }
        }

        // ----------------------------------------------------------------
        // Other Functions to be Implemented
        // ----------------------------------------------------------------
    }
}
