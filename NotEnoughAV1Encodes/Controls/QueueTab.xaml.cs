using Microsoft.Win32;
using Newtonsoft.Json;
using NotEnoughAV1Encodes.Video;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NotEnoughAV1Encodes.Controls
{
    public partial class QueueTab : UserControl
    {
        public QueueTab()
        {
            InitializeComponent();
        }

        private void ListBoxQueue_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                DeleteQueueItems();
            }
        }

        private void ButtonRemoveSelectedQueueItem_Click(object sender, RoutedEventArgs e)
        {
            DeleteQueueItems();
        }

        private void DeleteQueueItems()
        {
            if (ListBoxQueue.SelectedItem == null) return;
            if (MainWindow.ProgramState != 0) return;
            if (ListBoxQueue.SelectedItems.Count > 1)
            {
                List<Queue.QueueElement> items = ListBoxQueue.SelectedItems.OfType<Queue.QueueElement>().ToList();
                foreach (var item in items)
                {
                    ListBoxQueue.Items.Remove(item);
                    try
                    {
                        File.Delete(Path.Combine(Global.AppData, "NEAV1E", "Queue", item.VideoDB.InputFileName + "_" + item.UniqueIdentifier + ".json"));
                    }
                    catch { }
                }
            }
            else
            {
                Queue.QueueElement tmp = (Queue.QueueElement)ListBoxQueue.SelectedItem;
                ListBoxQueue.Items.Remove(ListBoxQueue.SelectedItem);
                try
                {
                    File.Delete(Path.Combine(Global.AppData, "NEAV1E", "Queue", tmp.VideoDB.InputFileName + "_" + tmp.UniqueIdentifier + ".json"));
                }
                catch { }
            }
        }

        private void QueueMenuItemSave_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxQueue.SelectedItem != null)
            {
                try
                {
                    Queue.QueueElement tmp = (Queue.QueueElement)ListBoxQueue.SelectedItem;
                    SaveFileDialog saveVideoFileDialog = new()
                    {
                        AddExtension = true,
                        Filter = "JSON File|*.json"
                    };
                    if (saveVideoFileDialog.ShowDialog() == true)
                    {
                        File.WriteAllText(saveVideoFileDialog.FileName, JsonConvert.SerializeObject(tmp, Formatting.Indented));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void QueueMenuItemOpenOutputDir_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxQueue.SelectedItem == null) return;
            try
            {
                Queue.QueueElement tmp = (Queue.QueueElement)ListBoxQueue.SelectedItem;
                string outPath = Path.GetDirectoryName(tmp.Output);
                ProcessStartInfo startInfo = new()
                {
                    Arguments = outPath,
                    FileName = "explorer.exe"
                };

                Process.Start(startInfo);
            }
            catch { }
        }

        private void ButtonClearQueue_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.ProgramState != 0) return;
            List<Queue.QueueElement> items = ListBoxQueue.Items.OfType<Queue.QueueElement>().ToList();
            foreach (var item in items)
            {
                ListBoxQueue.Items.Remove(item);
                try
                {
                    File.Delete(Path.Combine(Global.AppData, "NEAV1E", "Queue", item.VideoDB.InputFileName + "_" + item.UniqueIdentifier + ".json"));
                }
                catch { }
            }
        }

        private void ComboBoxSortQueueBy_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (MainWindow.startupLock) return;
            if (MainWindow.lockQueue) return;
            if (MainWindow.ProgramState != 0) return;

            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            mainWindow.settingsDB.SortQueueBy = ComboBoxSortQueueBy.SelectedIndex;
            try
            {
                Directory.CreateDirectory(Path.Combine(Global.AppData, "NEAV1E"));
                File.WriteAllText(Path.Combine(Global.AppData, "NEAV1E", "settings.json"), JsonConvert.SerializeObject(mainWindow.settingsDB, Formatting.Indented));
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }

            SortQueue();
        }

        public void SortQueue()
        {
            try
            {
                // Get MainWindow instance to access UI elements
                MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

                // Sort Queue
                List<Queue.QueueElement> queueElements = ListBoxQueue.Items.OfType<Queue.QueueElement>().ToList();

                queueElements = mainWindow.settingsDB.SortQueueBy switch
                {
                    0 => queueElements.OrderBy(queueElements => queueElements.DateAdded).ToList(),
                    1 => queueElements.OrderByDescending(queueElements => queueElements.DateAdded).ToList(),
                    2 => queueElements.OrderBy(queueElements => queueElements.VideoDB.MIFrameCount).ToList(),
                    3 => queueElements.OrderByDescending(queueElements => queueElements.VideoDB.MIFrameCount).ToList(),
                    4 => queueElements.OrderBy(queueElements => queueElements.VideoDB.OutputFileName).ToList(),
                    5 => queueElements.OrderByDescending(queueElements => queueElements.VideoDB.OutputFileName).ToList(),
                    _ => queueElements.OrderByDescending(queueElements => queueElements.DateAdded).ToList(),
                };
                ListBoxQueue.Items.Clear();
                foreach (var queueElement in queueElements)
                {
                    ListBoxQueue.Items.Add(queueElement);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ToggleSwitchQueueParallel_Toggled(object sender, RoutedEventArgs e)
        {
            if (MainWindow.startupLock) return;

            // Get MainWindow instance to access UI elements
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            mainWindow.settingsDB.QueueParallel = ToggleSwitchQueueParallel.IsOn;
            try
            {
                Directory.CreateDirectory(Path.Combine(Global.AppData, "NEAV1E"));
                File.WriteAllText(Path.Combine(Global.AppData, "NEAV1E", "settings.json"), JsonConvert.SerializeObject(mainWindow.settingsDB, Formatting.Indented));
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void ButtonEditSelectedItem_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.ProgramState != 0) return;

            if (ListBoxQueue.SelectedItem != null)
            {
                if (ListBoxQueue.SelectedItems.Count == 1)
                {
                    // Get MainWindow instance to access UI elements
                    MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

                    // Editing one entry
                    Queue.QueueElement tmp = (Queue.QueueElement)ListBoxQueue.SelectedItem;
                    mainWindow.PresetSettings = tmp.Preset;
                    mainWindow.DataContext = mainWindow.PresetSettings;
                    mainWindow.videoDB = tmp.VideoDB;
                    mainWindow.uid = tmp.UniqueIdentifier;

                    try { mainWindow.AudioTabControl.ListBoxAudioTracks.Items.Clear(); } catch { }
                    try { mainWindow.AudioTabControl.ListBoxAudioTracks.ItemsSource = null; } catch { }
                    try { mainWindow.SubtitlesTabControl.ListBoxSubtitleTracks.Items.Clear(); } catch { }
                    try { mainWindow.SubtitlesTabControl.ListBoxSubtitleTracks.ItemsSource = null; } catch { }

                    mainWindow.AudioTabControl.ListBoxAudioTracks.ItemsSource = mainWindow.videoDB.AudioTracks;
                    mainWindow.SubtitlesTabControl.ListBoxSubtitleTracks.ItemsSource = mainWindow.videoDB.SubtitleTracks;
                    mainWindow.SummaryTabControl.LabelVideoSource.Text = mainWindow.videoDB.InputPath;
                    mainWindow.SummaryTabControl.LabelVideoDestination.Text = mainWindow.videoDB.OutputPath;
                    mainWindow.SummaryTabControl.LabelVideoLength.Content = mainWindow.videoDB.MIDuration;
                    mainWindow.SummaryTabControl.LabelVideoResolution.Content = mainWindow.videoDB.MIWidth + "x" + mainWindow.videoDB.MIHeight;
                    mainWindow.SummaryTabControl.LabelVideoColorFomat.Content = mainWindow.videoDB.MIChromaSubsampling;

                    mainWindow.SummaryTabControl.ComboBoxChunkingMethod.SelectedIndex = tmp.ChunkingMethod;
                    mainWindow.SummaryTabControl.ComboBoxReencodeMethod.SelectedIndex = tmp.ReencodeMethod;
                    mainWindow.CheckBoxTwoPassEncoding.IsOn = tmp.Passes == 2;
                    mainWindow.SummaryTabControl.TextBoxChunkLength.Text = tmp.ChunkLength.ToString();
                    mainWindow.SummaryTabControl.TextBoxPySceneDetectThreshold.Text = tmp.PySceneDetectThreshold.ToString();

                    try
                    {
                        File.Delete(Path.Combine(Global.AppData, "NEAV1E", "Queue", tmp.VideoDB.InputFileName + "_" + tmp.UniqueIdentifier + ".json"));
                    }
                    catch { }

                    ListBoxQueue.Items.Remove(ListBoxQueue.SelectedItem);

                    Dispatcher.BeginInvoke((Action)(() => mainWindow.TabControl.SelectedIndex = 0));
                }
            }
        }
    }
}
