using System.Collections.Generic;

namespace NotEnoughAV1Encodes.Audio
{
    public class AudioTracks
    {
        public bool Active { get; set; }
        public int Index { get; set; }
        public int Codec { get; set; }
        public int Channels { get; set; }
        public string Bitrate { get; set; }
        public string Language { get; set; }
        public List<string> Languages { get; set; }
        public string CustomName { get; set; }
        public bool PCM { get; set; }
        public bool External { get; set; }
        public string ExternalPath { get; set; }
    }
}
