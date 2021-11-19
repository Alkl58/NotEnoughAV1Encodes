using System.Collections.Generic;

namespace NotEnoughAV1Encodes.Subtitle
{
    public class SubtitleTracks
    {
        public bool Active { get; set; }
        public int Index { get; set; }
        public string Language { get; set; }
        public List<string> Languages { get; set; }
        public string CustomName { get; set; }
        public bool Default { get; set; }
        public bool BurnIn { get; set; }
        public bool PictureBased { get; set; }
    }
}
