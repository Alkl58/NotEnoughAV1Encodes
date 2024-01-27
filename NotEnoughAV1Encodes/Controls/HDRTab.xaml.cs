using System.Windows;
using System.Windows.Controls;

namespace NotEnoughAV1Encodes.Controls
{
    public partial class HDRTab : UserControl
    {
        public HDRTab()
        {
            InitializeComponent();
        }

        public string GenerateMKVMergeHDRCommand()
        {
            string settings = " ";

            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (mainWindow.VideoTabVideoPartialControl.CheckBoxVideoHDR.IsChecked == true)
            {
                settings = "";
                if (CheckBoxMKVMergeMasteringDisplay.IsChecked == true)
                {
                    // --chromaticity-coordinates TID:red-x,red-y,green-x,green-y,blue-x,blue-y
                    settings += " --chromaticity-coordinates 0:" +
                        TextBoxMKVMergeMasteringRx.Text + "," +
                        TextBoxMKVMergeMasteringRy.Text + "," +
                        TextBoxMKVMergeMasteringGx.Text + "," +
                        TextBoxMKVMergeMasteringGy.Text + "," +
                        TextBoxMKVMergeMasteringBx.Text + "," +
                        TextBoxMKVMergeMasteringBy.Text;
                }
                if (CheckBoxMKVMergeWhiteMasteringDisplay.IsChecked == true)
                {
                    // --white-colour-coordinates TID:x,y
                    settings += " --white-colour-coordinates 0:" +
                        TextBoxMKVMergeMasteringWPx.Text + "," +
                        TextBoxMKVMergeMasteringWPy.Text;
                }
                if (CheckBoxMKVMergeLuminance.IsChecked == true)
                {
                    // --max-luminance TID:float
                    // --min-luminance TID:float
                    settings += " --max-luminance 0:" + TextBoxMKVMergeMasteringLMax.Text;
                    settings += " --min-luminance 0:" + TextBoxMKVMergeMasteringLMin.Text;
                }
                if (CheckBoxMKVMergeMaxContentLight.IsChecked == true)
                {
                    // --max-content-light TID:n
                    settings += " --max-content-light 0:" + TextBoxMKVMergeMaxContentLight.Text;
                }
                if (CheckBoxMKVMergeMaxFrameLight.IsChecked == true)
                {
                    // --max-frame-light TID:n
                    settings += " --max-frame-light 0:" + TextBoxMKVMergeMaxFrameLight.Text;
                }
                if (ComboBoxMKVMergeColorPrimaries.SelectedIndex != 2)
                {
                    // --colour-primaries TID:n
                    settings += " --colour-primaries 0:" + ComboBoxMKVMergeColorPrimaries.SelectedIndex.ToString();
                }
                if (ComboBoxMKVMergeColorTransfer.SelectedIndex != 2)
                {
                    // --colour-transfer-characteristics TID:n
                    settings += " --colour-transfer-characteristics 0:" + ComboBoxMKVMergeColorTransfer.SelectedIndex.ToString();
                }
                if (ComboBoxMKVMergeColorMatrix.SelectedIndex != 2)
                {
                    // --colour-matrix-coefficients TID:n
                    settings += " --colour-matrix-coefficients 0:" + ComboBoxMKVMergeColorMatrix.SelectedIndex.ToString();
                }
            }
            return settings;
        }
    }
}
