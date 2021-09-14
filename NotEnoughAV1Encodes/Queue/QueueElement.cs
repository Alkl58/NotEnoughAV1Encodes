using System.Collections.Generic;
using System.ComponentModel;

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
        /// <summary>Chunking Method; 0=Equal Chunking, 1=FFmpeg Scenedetect, 2=PySceneDetect.</summary>
        public int ChunkingMethod { get; set; }
        /// <summary>Re-Encoding Method (only for Equal Chunking).</summary>
        public int ReencodeMethod { get; set; }
        /// <summary>Chunk Length (only for Equal Chunking).</summary>
        public int ChunkLength { get; set; }
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
    }
}
