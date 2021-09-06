using System;
using System.Diagnostics;
using System.Windows;
using MahApps.Metro.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Threading;
using System.Timers;
using System.Linq;

namespace NotEnoughAV1Encodes
{
    public partial class MainWindow : MetroWindow
    {
        public Views.ProgramSettings programSettings = new();
        private readonly Video.VideoDB videoDB = new();
        private int ProgramState;

        public ObservableCollection<Queue.QueueElement> QueueList { get; set; } = new();

        public MainWindow()
        {
            InitializeComponent();
            Initialize();
        }

        #region Startup
        private void Initialize()
        {
            resources.MediaLanguages.FillDictionary();

            // Load Queue
            if (Directory.Exists(Path.Combine(Global.AppData, "NEAV1E", "Queue")))
            {
                string[] filePaths = Directory.GetFiles(Path.Combine(Global.AppData, "NEAV1E", "Queue"), "*.json", SearchOption.TopDirectoryOnly);

                foreach (string file in filePaths)
                {
                    ListBoxQueue.Items.Add(JsonConvert.DeserializeObject<Queue.QueueElement>(File.ReadAllText(file)));
                }
            }
        }
        #endregion

        #region Buttons
        private void ButtonProgramSettings_Click(object sender, RoutedEventArgs e)
        {
            programSettings.ShowDialog();
        }

        private void ButtonOpenSource_Click(object sender, RoutedEventArgs e)
        {
            Views.OpenSource openSource = new();
            openSource.ShowDialog();
            if (openSource.Quit)
            {
                videoDB.InputPath = openSource.Path;
                videoDB.ParseMediaInfo();
                ListBoxAudioTracks.Items.Clear();
                //ListBoxAudioTracks.Items.Clear()
                ListBoxAudioTracks.ItemsSource = videoDB.AudioTracks;
                TextBoxVideoSource.Content = videoDB.InputPath;
                LabelVideoLength.Content = videoDB.MIDuration;
                LabelVideoResolution.Content = videoDB.MIWidth + "x" + videoDB.MIHeight;
                LabelVideoColorFomat.Content = videoDB.MIChromaSubsampling;
                string vfr = "";
                if (videoDB.MIIsVFR)
                {
                    vfr = " (VFR)";
                }
                LabelVideoFramerate.Content = videoDB.MIFramerate + vfr;
            }
        }

        private void ButtonSetDestination_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveVideoFileDialog = new()
            {
                Filter = "MKV Video|*.mkv|WebM Video|*.webm|MP4 Video|*.mp4"
            };

            if (saveVideoFileDialog.ShowDialog() == true)
            {
                videoDB.OutputPath = saveVideoFileDialog.FileName;
                LabelVideoDestination.Content = videoDB.OutputPath;
            }
        }

        private async void ButtonStartStop_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(videoDB.InputPath) && !string.IsNullOrEmpty(videoDB.OutputPath))
            {
                if (ProgramState is 0 or 2)
                {
                    ImageStartStop.Source = new BitmapImage(new Uri(@"/NotEnoughAV1Encodes;component/resources/img/pause.png", UriKind.Relative));

                    // Main Start
                    if (ProgramState is 0)
                    {
                        //await PreStart();
                        await Task.Run(() => PreStart());
                    }

                    // Resume all PIDs
                    if (ProgramState is 2)
                    {

                    }

                    ProgramState = 1;
                }
                else if (ProgramState is 1)
                {
                    ProgramState = 2;
                    ImageStartStop.Source = new BitmapImage(new Uri(@"/NotEnoughAV1Encodes;component/resources/img/resume.png", UriKind.Relative));
                }
            }
            else
            {
                // To-Do: Error Meldung
            }
        }

        private void ButtonAddToQueue_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(videoDB.InputPath))
            {
                // Throw Error
                return;
            }

            if (string.IsNullOrEmpty(videoDB.OutputPath))
            {
                // Throw Error
                return;
            }

            Queue.QueueElement queueElement = new();
            Audio.CommandGenerator commandgenerator = new();

            queueElement.Input = videoDB.InputPath;
            queueElement.Output = videoDB.OutputPath;
            queueElement.InputFileName = videoDB.FileName;
            queueElement.AudioCommand = commandgenerator.Generate(ListBoxAudioTracks.Items);

            // Generate a random identifier to avoid filesystem conflicts
            const string src = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            StringBuilder identifier = new();
            Random RNG = new();
            for (int i = 0; i < 15; i++)
            {
                identifier.Append(src[RNG.Next(0, src.Length)]);
            }

            queueElement.UniqueIdentifier = identifier.ToString();

            // Add to Queue
            ListBoxQueue.Items.Add(queueElement);

            if(!Directory.Exists(Path.Combine(Global.AppData, "NEAV1E", "Queue")))
            {
                Directory.CreateDirectory(Path.Combine(Global.AppData, "NEAV1E", "Queue"));
            }

            // Save as JSON
            File.WriteAllText(Path.Combine(Global.AppData, "NEAV1E", "Queue", videoDB.FileName + "_" + identifier + ".json"), JsonConvert.SerializeObject(queueElement, Formatting.Indented));
        }
        #endregion

        #region UI Functions
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Validates that the TextBox Input are only numbers
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            programSettings.Close();
        }
        #endregion


        private int getTotalFramesProcessed(string stderr)
        {
            try
            {
                int Start, End;
                Start = stderr.IndexOf("frame=", 0) + "frame=".Length;
                End = stderr.IndexOf("fps=", Start);
                return int.Parse(stderr.Substring(Start, End - Start));
            }
            catch {
                return 10;
            }
        }

        #region Main Entry
        private void PreStart()
        {
            // To-Do: Set WorkerCount either by QueueElement or Queue Parallel
            int WorkerCountQueue = 1;
            int WorkerCountElement = 1;

            using SemaphoreSlim concurrencySemaphore = new(WorkerCountQueue);
            // Creates a tasks list
            List<Task> tasks = new();

            foreach (Queue.QueueElement queueElement in ListBoxQueue.Items)
            {
                concurrencySemaphore.Wait();
                var task = Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        // Inner concurrency
                        //using SemaphoreSlim concurrencySemaphoreInner = new(WorkerCountElement);

                        //string[] VideoChunks = Directory.GetFiles(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Chunks"), "*mkv", SearchOption.AllDirectories).Select(x => Path.GetFileName(x)).ToArray();


                        Process process = new Process();
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            WindowStyle = ProcessWindowStyle.Hidden,
                            FileName = "cmd.exe",
                            WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "FFmpeg"),
                            Arguments = "/C ffmpeg.exe -i \"" + queueElement.Input + "\" -c:v libx265 -crf 10 \"" + queueElement.Output,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };
                        process.StartInfo = startInfo;
                        process.Start();

                        StreamReader sr = process.StandardError;

                        while (!sr.EndOfStream)
                        {
                            queueElement.Progress = Convert.ToDouble(getTotalFramesProcessed(sr.ReadLine()));
                        }

                        process.WaitForExit();

                    }
                    finally
                    {
                        concurrencySemaphore.Release();
                    }
                });

                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());
        }
        #endregion
    }
}
