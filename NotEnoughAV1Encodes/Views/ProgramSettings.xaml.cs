﻿using MahApps.Metro.Controls;
using System.Windows;

namespace NotEnoughAV1Encodes.Views
{
    public partial class ProgramSettings : MetroWindow
    {
        public bool DeleteTempFiles { get; set; }
        public bool ShutdownAfterEncode { get; set; }
        public ProgramSettings(SettingsDB settingsDB)
        {
            InitializeComponent();
            ToggleSwitchDeleteTempFiles.IsOn = settingsDB.DeleteTempFiles;
            ToggleSwitchShutdown.IsOn = settingsDB.ShutdownAfterEncode;
        }

        private void ButtonUpdater_Click(object sender, RoutedEventArgs e)
        {
            Updater updater = new("","");
            updater.ShowDialog();
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DeleteTempFiles = ToggleSwitchDeleteTempFiles.IsOn;
            ShutdownAfterEncode = ToggleSwitchShutdown.IsOn;
        }
    }
}