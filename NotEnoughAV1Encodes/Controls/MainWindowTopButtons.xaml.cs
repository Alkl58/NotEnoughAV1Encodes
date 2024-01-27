using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NotEnoughAV1Encodes.Controls
{
    public partial class MainWindowTopButtons : UserControl
    {
        // Button Event Handlers
        public event EventHandler OpenSource;
        public event EventHandler SetDestination;
        public event EventHandler AddToQueue;
        public event EventHandler OpenSettings;
        public event EventHandler Start;
        public event EventHandler Cancel;

        public MainWindowTopButtons()
        {
            InitializeComponent();
        }

        private void ButtonOpenSource_Click(object sender, RoutedEventArgs e)
        {
            OpenSource?.Invoke(this, e);
        }

        private void ButtonSetDestination_Click(object sender, RoutedEventArgs e)
        {
            SetDestination?.Invoke(this, e);
        }

        private void ButtonAddToQueue_Click(object sender, RoutedEventArgs e)
        {
            AddToQueue?.Invoke(this, e);
        }

        private void ButtonProgramSettings_Click(object sender, RoutedEventArgs e)
        {
            OpenSettings?.Invoke(this, e);
        }

        private void ButtonStartStop_Click(object sender, RoutedEventArgs e)
        {
            Start?.Invoke(this, e);
        }

        private void ButtonCancelEncode_Click(object sender, RoutedEventArgs e)
        {
            Cancel?.Invoke(this, e);
        }
    }
}