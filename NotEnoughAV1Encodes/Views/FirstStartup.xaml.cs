using System;
using System.IO;
using System.Windows;
using MahApps.Metro.Controls;
using Newtonsoft.Json;

namespace NotEnoughAV1Encodes.Views
{
    public partial class FirstStartup : MetroWindow
    {
        SettingsDB _settingsDB;
        public FirstStartup(SettingsDB settingsDB)
        {
            InitializeComponent();
            _settingsDB = settingsDB;
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Directory.CreateDirectory(Path.Combine(Global.AppData, "NEAV1E"));
                File.WriteAllText(Path.Combine(Global.AppData, "NEAV1E", "settings.json"), JsonConvert.SerializeObject(_settingsDB, Formatting.Indented));
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
            Close();
        }
    }
}
