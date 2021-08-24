using ControlzEx.Theming;
using MahApps.Metro.Controls;
using System.Windows;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Reflection;
using Microsoft.Win32;
using System;
using System.Windows.Navigation;
using System.Threading;

namespace NotEnoughAV1Encodes.Views
{
    public partial class Settings : MetroWindow
    {
        public Settings(string baseTheme, string accentTheme)
        {
            InitializeComponent();
            ThemeManager.Current.ChangeTheme(this, baseTheme + "." + accentTheme);
            // Sets the GUI Version
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            GroubBoxVersion.Header = "Version: " + version.Remove(version.Length - 2);
            LoadPresetsIntoComboBox();
            LoadSettingsTab();

            // Should only happen once
            if (MainWindow.language == null)
            {
                switch (Thread.CurrentThread.CurrentCulture.ToString())
                {
                    case "de-CH":
                    case "de-LU":
                    case "de-LI":
                    case "de-AT":
                    case "de-DE":
                        ComboBoxUILanguage.SelectedIndex = 1;
                        break;
                    case "fr-BE":
                    case "fr-CA":
                    case "fr-LU":
                    case "fr-MC":
                    case "fr-CH":
                    case "fr-FR":
                        ComboBoxUILanguage.SelectedIndex = 2;
                        break;
                    case "en-US":
                    default:
                        ComboBoxUILanguage.SelectedIndex = 0;
                        break;
                }
            }
        }

        private void ButtonUpdater_Click(object sender, RoutedEventArgs e)
        {
            // Opens the program Updater
            if (MainWindow.encode_state == 0)
            {
                Updater updater = new Updater(ComboBoxBaseTheme.Text, ComboBoxAccentTheme.Text);
                updater.ShowDialog();
                CheckDependencies.Check();
            }
        }

        private void ButtonOpenTempFolder_Click(object sender, RoutedEventArgs e)
        {
            // Opens the Temp Folder
            if (ToggleSwitchTempFolder.IsOn == false)
            {
                //Creates the temp directoy if not existent
                if (Directory.Exists(Path.Combine(Path.GetTempPath(), "NEAV1E")) == false) { Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "NEAV1E")); }
                Process.Start(Path.Combine(Path.GetTempPath(), "NEAV1E"));
            }
            else
            {
                Process.Start(TextBoxCustomTempPath.Text);
            }
        }

        private void ButtonSetTempPath_Click(object sender, RoutedEventArgs e)
        {
            // Custom Temp Path
            System.Windows.Forms.FolderBrowserDialog browseOutputFolder = new System.Windows.Forms.FolderBrowserDialog();
            if (browseOutputFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TextBoxCustomTempPath.Text = browseOutputFolder.SelectedPath;
            }
        }

        private void CheckBoxBatchWithDifferentPresets_Checked(object sender, RoutedEventArgs e)
        {
            if (CheckBoxBatchWithDifferentPresets.IsChecked == true)
            {
                ToggleSwitchDeleteTempFiles.IsOn = true;
            }
        }

        private void ButtonSaveUILanguage_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.language = ComboBoxUILanguage.Text;
        }

        private void ButtonSetTheme_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Current.ChangeTheme(this, ComboBoxBaseTheme.Text + "." + ComboBoxAccentTheme.Text);
        }

        private void ButtonSetBGImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                if (openFileDialog.ShowDialog() == true)
                {
                    Uri fileUri = new Uri(openFileDialog.FileName);
                    if (File.Exists("background.txt")) { File.Delete("background.txt"); }
                    Helpers.WriteToFileThreadSafe(openFileDialog.FileName, "background.txt");
                }
                else
                {
                    // Reset BG Image
                    if (File.Exists("background.txt")) { try { File.Delete("background.txt"); } catch { } }
                }
            }
            catch { }
        }

        private void ButtonGithub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Alkl58/NotEnoughAV1Encodes");
        }

        private void ButtonPayPal_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://paypal.me/alkl58");
        }

        private void ButtonDiscord_Click(object sender, RoutedEventArgs e)
        {
            // NEAV1E Discord
            Process.Start("https://discord.gg/yG27ArHBFe");
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // Opens a Hyperlink in the browser
            Process.Start(e.Uri.ToString());
        }

        private void ButtonDeleteTempFiles_Click(object sender, RoutedEventArgs e)
        {
            SmallFunctions.DeleteTempFilesButton();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SaveSettingsTab();
            MainWindow.BatchWithDifferentPresets = CheckBoxBatchWithDifferentPresets.IsChecked == true;
            MainWindow.BatchPresets = ComboBoxBatchSettings.Items;
            Close();
        }

        private void LoadPresetsIntoComboBox()
        {
            // Loads all Presets into ComboBox
            try
            {
                if (Directory.Exists("Profiles"))
                {
                    // DirectoryInfo of Profiles Folder
                    DirectoryInfo profiles = new DirectoryInfo("Profiles");
                    // Gets all .xml file -> add to FileInfo Array
                    FileInfo[] Files = profiles.GetFiles("*.xml");
                    // Fills the ComobBox with checkable items for batch encoding
                    foreach (FileInfo file in Files)
                    {
                        System.Windows.Controls.CheckBox comboBoxItem = new System.Windows.Controls.CheckBox
                        {
                            Content = file,
                            IsChecked = false
                        };
                        ComboBoxBatchSettings.Items.Add(comboBoxItem);
                    }
                }
            }
            catch { }
        }

        private void LoadSettingsTab()
        {
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "settings.xml")))
            {
                string language = "English";
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(Path.Combine(Directory.GetCurrentDirectory(), "settings.xml"));
                    XmlNodeList node = doc.GetElementsByTagName("Settings");
                    foreach (XmlNode n in node[0].ChildNodes)
                    {
                        switch (n.Name)
                        {
                            case "DeleteTempFiles":
                                ToggleSwitchDeleteTempFiles.IsOn = n.InnerText == "True";
                                break;
                            case "PlaySound":
                                ToggleSwitchUISounds.IsOn = n.InnerText == "True";
                                break;
                            case "Logging":
                                ToggleSwitchLogging.IsOn = n.InnerText == "True";
                                break;
                            case "ShowDialog":
                                ToggleSwitchShowWindow.IsOn = n.InnerText == "True";
                                break;
                            case "Shutdown":
                                ToggleSwitchShutdownAfterEncode.IsOn = n.InnerText == "True";
                                break;
                            case "TempPathActive":
                                ToggleSwitchTempFolder.IsOn = n.InnerText == "True";
                                break;
                            case "TempPath":
                                TextBoxCustomTempPath.Text = n.InnerText;
                                break;
                            case "Terminal":
                                ToggleSwitchHideTerminal.IsOn = n.InnerText == "True";
                                break;
                            case "ThemeAccent":
                                ComboBoxAccentTheme.SelectedIndex = int.Parse(n.InnerText);
                                break;
                            case "ThemeBase":
                                ComboBoxBaseTheme.SelectedIndex = int.Parse(n.InnerText);
                                break;
                            case "BatchContainer":
                                ComboBoxContainerBatchEncoding.SelectedIndex = int.Parse(n.InnerText);
                                break;
                            case "SkipSubtitles":
                                ToggleSkipSubtitleExtraction.IsOn = n.InnerText == "True";
                                break;
                            case "Language":
                                language = n.InnerText;
                                break;
                            case "OverrideWorkerCount":
                                ToggleOverrideWorkerCount.IsOn = n.InnerText == "True";
                                break;
                            default: break;
                        }
                    }
                }
                catch { }

                ThemeManager.Current.ChangeTheme(this, ComboBoxBaseTheme.Text + "." + ComboBoxAccentTheme.Text);

                switch (language)
                {
                    case "Deutsch":
                        ComboBoxUILanguage.SelectedIndex = 1;
                        break;
                    case "Français":
                        ComboBoxUILanguage.SelectedIndex = 2;
                        break;
                    default:
                        ComboBoxUILanguage.SelectedIndex = 0;
                        break;
                }
            }
        }


        public void SaveSettingsTab()
        {
            try
            {
                if (!MainWindow.StartUp)
                {
                    XmlWriter writer = XmlWriter.Create(Path.Combine(Directory.GetCurrentDirectory(), "settings.xml"));
                    writer.WriteStartElement("Settings");
                    writer.WriteElementString("DeleteTempFiles", ToggleSwitchDeleteTempFiles.IsOn.ToString());
                    writer.WriteElementString("PlaySound", ToggleSwitchUISounds.IsOn.ToString());
                    writer.WriteElementString("Logging", ToggleSwitchLogging.IsOn.ToString());
                    writer.WriteElementString("ShowDialog", ToggleSwitchShowWindow.IsOn.ToString());
                    writer.WriteElementString("Shutdown", ToggleSwitchShutdownAfterEncode.IsOn.ToString());
                    writer.WriteElementString("TempPathActive", ToggleSwitchTempFolder.IsOn.ToString());
                    writer.WriteElementString("TempPath", TextBoxCustomTempPath.Text);
                    writer.WriteElementString("Terminal", ToggleSwitchHideTerminal.IsOn.ToString());
                    writer.WriteElementString("ThemeAccent", ComboBoxAccentTheme.SelectedIndex.ToString());
                    writer.WriteElementString("ThemeBase", ComboBoxBaseTheme.SelectedIndex.ToString());
                    writer.WriteElementString("BatchContainer", ComboBoxContainerBatchEncoding.SelectedIndex.ToString());
                    writer.WriteElementString("ReencodeMessage", MainWindow.reencodeMessage.ToString());
                    writer.WriteElementString("Language", ComboBoxUILanguage.Text ?? "English");
                    writer.WriteElementString("SkipSubtitles", ToggleSkipSubtitleExtraction.IsOn.ToString());
                    writer.WriteElementString("OverrideWorkerCount", ToggleOverrideWorkerCount.IsOn.ToString());
                    writer.WriteEndElement();
                    writer.Close();
                }
            }
            catch { }
        }
    }
}
