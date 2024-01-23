using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NotEnoughAV1Encodes.Controls
{
    public partial class FiltersTab : UserControl
    {
        // Event Handlers
        public event EventHandler CropToggled;
        public event EventHandler PreviewForward;
        public event EventHandler PreviewBackward;
        public event EventHandler AutoCropDetect;

        public FiltersTab()
        {
            InitializeComponent();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Validates that the TextBox Input are only numbers
            Regex regex = new("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void ToggleSwitchFilterCrop_Toggled(object sender, RoutedEventArgs e)
        {
            CropToggled?.Invoke(this, e);
        }

        private void ButtonCropPreviewForward_Click(object sender, RoutedEventArgs e)
        {
            PreviewForward?.Invoke(this, e);
        }

        private void ButtonCropPreviewBackward_Click(object sender, RoutedEventArgs e)
        {
            PreviewBackward?.Invoke(this, e);
        }

        private void ButtonCropAutoDetect_Click(object sender, RoutedEventArgs e)
        {
            AutoCropDetect?.Invoke(this, e);
        }
    }
}
