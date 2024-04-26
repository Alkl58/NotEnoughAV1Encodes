using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace NotEnoughAV1Encodes.Controls
{
    public partial class ChunkingTab : UserControl
    {
        public ChunkingTab()
        {
            InitializeComponent();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Validates that the TextBox Input are only numbers
            Regex regex = new("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
