using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NotEnoughAV1Encodes.Video
{
    class VideoDB
    {
        public string InputPath { get; set; }
        public string OutputPath { get; set; }
        public string FileName { get; set; }
        public string MIDuration { get; set; }
        public string MIFramerate { get; set; }
        public string MIColorSpace { get; set; }
        public string MIChromaSubsampling { get; set; }
        public string MIBitDepth { get; set; }
        public long MIFrameCount { get; set; }
        public bool MIIsVFR { get; set; }
        public int MIWidth { get; set; }
        public int MIHeight { get; set; }

        public List<Audio.AudioTracks> AudioTracks { get; set; }

        public void ParseMediaInfo()
        {
            if (!string.IsNullOrEmpty(InputPath))
            {
                MediaInfo mediaInfo = new();
                mediaInfo.Open(InputPath);

                try { MIDuration = mediaInfo.Get(StreamKind.Video, 0, "Duration/String3"); } catch { }
                try { MIFramerate = mediaInfo.Get(StreamKind.Video, 0, "FrameRate"); } catch { }
                try { MIColorSpace = mediaInfo.Get(StreamKind.Video, 0, "ColorSpace"); } catch { }
                try { MIChromaSubsampling = mediaInfo.Get(StreamKind.Video, 0, "ChromaSubsampling"); } catch { }
                try { MIBitDepth = mediaInfo.Get(StreamKind.Video, 0, "BitDepth"); } catch { }
                try { MIFrameCount = long.Parse(mediaInfo.Get(StreamKind.Video, 0, "FrameCount")); } catch { }
                try { MIIsVFR = mediaInfo.Get(StreamKind.Video, 0, "FrameRate_Mode") == "VFR"; } catch { MIIsVFR = false; }
                try { MIWidth = int.Parse(mediaInfo.Get(StreamKind.Video, 0, "Width")); } catch { }
                try { MIHeight = int.Parse(mediaInfo.Get(StreamKind.Video, 0, "Height")); } catch { }

                int audioCount = mediaInfo.Count_Get(StreamKind.Audio);
                if (audioCount > 0)
                {
                    AudioTracks = new();

                    for (int i = 0; i < audioCount; i++)
                    {
                        bool pcm = false;
                        try { pcm = mediaInfo.Get(StreamKind.Audio, i, "Format") == "PCM" && mediaInfo.Get(StreamKind.Audio, i, "MuxingMode") == "Blu-ray"; } catch { }

                        string name = "";
                        try { name = mediaInfo.Get(StreamKind.Audio, i, "Title"); } catch { }

                        int channels = 1;
                        try { channels = int.Parse(mediaInfo.Get(StreamKind.Audio, i, "Channels")); } catch { }

                        switch (channels)
                        {
                            case 1:
                                channels = 0;
                                break;
                            case 2:
                                channels = 1;
                                break;
                            case 6:
                                channels = 2;
                                break;
                            case 8:
                                channels = 3;
                                break;
                            default:
                                break;
                        }

                        string lang = "und";
                        try
                        {
                            lang = mediaInfo.Get(StreamKind.Audio, i, "Language/String3");
                            lang = resources.MediaLanguages.Languages.FirstOrDefault(x => x.Value == lang).Key;
                        }
                        catch { }

                        AudioTracks.Add(new Audio.AudioTracks()
                        {
                            Active = true,
                            Index = i,
                            Codec = 0,
                            Bitrate = "128",
                            Languages = resources.MediaLanguages.LanguageKeys,
                            Language = lang,
                            CustomName = name,
                            Channels = channels,
                            PCM = pcm
                        });
                    }
                }

                mediaInfo.Close();

                FileName = Path.GetFileNameWithoutExtension(InputPath);
            }
        }
    }
}
