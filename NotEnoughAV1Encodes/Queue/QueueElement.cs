using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace NotEnoughAV1Encodes.Queue
{
    public class QueueElement : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private double _progress;
        private string _status;
        /// <summary>Full Video Input Path.</summary>
        public string Input { get; set; }
        /// <summary>Full Video Output Path.</summary>
        public string Output { get; set; }
        /// <summary>Video Input Filename without extension.</summary>
        public string InputFileName { get; set; }
        /// <summary>Video Output Filename without extension.</summary>
        public string OutputFileName { get; set; }
        /// <summary>Current Status displayed in the Queue.</summary>
        public string Status
        {
            get => _status;
            set { _status = value; NotifyPropertyChanged("Status"); }
        }
        /// <summary>Video Encoding parameters.</summary>
        public string VideoCommand { get; set; }
        /// <summary>Audio Encoding parameters.</summary>
        public string AudioCommand { get; set; }
        /// <summary>Unique Identifier to avoid Filesystem conflicts.</summary>
        public string UniqueIdentifier { get; set; }
        /// <summary>Encoding Method; 0=aom ffmpeg, 1=rav1e ffmpeg, 2=svt-av1 ffmpeg ...</summary>
        public int EncodingMethod { get; set; }
        /// <summary>Chunking Method; 0=Equal Chunking, 1=FFmpeg Scenedetect, 2=PySceneDetect.</summary>
        public int ChunkingMethod { get; set; }
        /// <summary>Re-Encoding Method (only for Equal Chunking).</summary>
        public int ReencodeMethod { get; set; }
        /// <summary>Chunk Length (only for Equal Chunking).</summary>
        public int ChunkLength { get; set; }
        /// <summary>Amount of Encoding Passes.</summary>
        public int Passes { get; set; }
        /// <summary>PySceneDetect Threshold (after Decimal).</summary>
        public float PySceneDetectThreshold { get; set; }
        /// <summary>Framecount of Source Video.</summary>
        public long FrameCount { get; set; }
        /// <summary>List of Progress of each Chunk.</summary>
        public List<ChunkProgress> ChunkProgress { get; set; } = new();
        public double Progress
        {
            get => _progress;
            set { _progress = value; NotifyPropertyChanged("Progress"); }
        }

        private void NotifyPropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
                PropertyChanged(this, new PropertyChangedEventArgs("DisplayMember"));
            }
        }

        public void GetFrameCount()
        {
            // Only do manual Framecount, if MediaInfo did not detect it
            if (FrameCount == 0)
            {
                try
                {
                    // This function calculates the total number of frames
                    Process process = new()
                    {
                        StartInfo = new ProcessStartInfo()
                        {
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                            FileName = "cmd.exe",
                            WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "FFmpeg"),
                            Arguments = "/C ffmpeg.exe -i \"" + Input + "\" -hide_banner -loglevel 32 -map 0:v:0 -f null -",
                            RedirectStandardError = true,
                            RedirectStandardOutput = true
                        }
                    };
                    process.Start();
                    string stream = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    string tempStream = stream[stream.LastIndexOf("frame=")..];
                    string data = GetBetween(tempStream, "frame=", "fps=");
                    FrameCount = long.Parse(data);
                }
                catch { }
            }
        }

        private static string GetBetween(string strSource, string strStart, string strEnd)
        {
            // This function parses data between two points
            if (strSource.Contains(strStart) && strSource.Contains(strEnd))
            {
                int Start, End;
                Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                End = strSource.IndexOf(strEnd, Start);
                return strSource[Start..End];
            }
            return "0";
        }
    }
}
