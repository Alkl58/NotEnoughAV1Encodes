using System.Collections.Generic;
using System.ComponentModel;

namespace NotEnoughAV1Encodes.Subtitle
{
    public class SubtitleTracks : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private bool _active = true;
        private bool _enabled = true;
        public bool Active { 
            get => _active;
            set { _active = value; NotifyPropertyChanged("Active"); }
        }
        public bool Enabled
        {
            get => _enabled;
            set { _enabled = value; NotifyPropertyChanged("Enabled"); }
        }
        public int Index { get; set; }
        public string Language { get; set; }
        public List<string> Languages { get; set; }
        public string CustomName { get; set; }
        public bool Default { get; set; }
        public bool BurnIn { get; set; }
        public bool PictureBased { get; set; }

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
