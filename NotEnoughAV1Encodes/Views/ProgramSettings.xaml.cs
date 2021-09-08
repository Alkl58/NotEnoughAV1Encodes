using MahApps.Metro.Controls;
using System.Windows;

namespace NotEnoughAV1Encodes.Views
{
    public partial class ProgramSettings : MetroWindow
    {
        public ProgramSettings()
        {
            InitializeComponent();
        }

        private void ButtonUpdater_Click(object sender, RoutedEventArgs e)
        {
            Updater updater = new("","");
            updater.ShowDialog();
        }
    }
}
