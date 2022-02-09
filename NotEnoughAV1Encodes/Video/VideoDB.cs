using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NotEnoughAV1Encodes.Video
{
    public class VideoDB
    {
        public string InputPath { get; set; }
        public string OutputPath { get; set; }
        public string InputFileName { get; set; }
        public string OutputFileName { get; set; }
        public string MIDuration { get; set; }
        public string MIFramerate { get; set; }
        public string MIColorSpace { get; set; }
        public string MIChromaSubsampling { get; set; }
        public string MIBitDepth { get; set; }
        public string MIDisplayAspectRatio { get; set; }
        public string MIPixelAspectRatio { get; set; }
        public long MIFrameCount { get; set; }
        public bool MIIsVFR { get; set; }
        public int MIWidth { get; set; }
        public int MIHeight { get; set; }

        public List<Audio.AudioTracks> AudioTracks { get; set; }

        public List<Subtitle.SubtitleTracks> SubtitleTracks { get; set; }

        public void ParseMediaInfo(VideoSettings settings)
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
                try { MIFrameCount = long.Parse(mediaInfo.Get(StreamKind.Video, 0, "FrameCount")); } catch { MIFrameCount = 0; }
                try { MIIsVFR = mediaInfo.Get(StreamKind.Video, 0, "FrameRate_Mode") == "VFR"; } catch { MIIsVFR = false; }
                try { MIWidth = int.Parse(mediaInfo.Get(StreamKind.Video, 0, "Width")); } catch { }
                try { MIHeight = int.Parse(mediaInfo.Get(StreamKind.Video, 0, "Height")); } catch { }
                try { MIDisplayAspectRatio = mediaInfo.Get(StreamKind.Video, 0, "DisplayAspectRatio/String"); } catch { }
                try { MIPixelAspectRatio = mediaInfo.Get(StreamKind.Video, 0, "PixelAspectRatio"); } catch { }

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

                        int bitrate = 128;
                        int codec = 0;
                        switch (channels)
                        {
                            case 1:
                                channels = 0;
                                bitrate = settings.AudioBitrateMono;
                                codec = settings.AudioCodecMono;
                                break;
                            case 2:
                                channels = 1;
                                bitrate = settings.AudioBitrateStereo;
                                codec = settings.AudioCodecStereo;
                                break;
                            case 6:
                                channels = 2;
                                bitrate = settings.AudioBitrateSixChannel;
                                codec = settings.AudioCodecSixChannel;
                                break;
                            case 8:
                                channels = 3;
                                bitrate = settings.AudioBitrateEightChannel;
                                codec = settings.AudioCodecEightChannel;
                                break;
                            default:
                                break;
                        }

                        string lang = "und";
                        try
                        {
                            lang = mediaInfo.Get(StreamKind.Audio, i, "Language/String3");
                            if (!resources.MediaLanguages.Languages.ContainsValue(lang))
                            {
                                lang = "und";
                            }
                            lang = resources.MediaLanguages.Languages.FirstOrDefault(x => x.Value == lang).Key;
                        }
                        catch { }

                        AudioTracks.Add(new Audio.AudioTracks()
                        {
                            Active = true,
                            Index = i,
                            Codec = codec,
                            Bitrate = bitrate.ToString(),
                            Languages = resources.MediaLanguages.LanguageKeys,
                            Language = lang,
                            CustomName = name,
                            Channels = channels,
                            PCM = pcm
                        });
                    }
                }

                int subtitleCount = mediaInfo.Count_Get(StreamKind.Text);
                if (subtitleCount > 0)
                {
                    SubtitleTracks = new();

                    for (int i = 0; i < subtitleCount; i++)
                    {
                        
                        string name = "";
                        try { name = mediaInfo.Get(StreamKind.Text, i, "Title"); } catch { }

                        string lang = "und";
                        try
                        {
                            lang = mediaInfo.Get(StreamKind.Text, i, "Language/String3");
                            if (!resources.MediaLanguages.Languages.ContainsValue(lang))
                            {
                                lang = "und";
                            }
                            lang = resources.MediaLanguages.Languages.FirstOrDefault(x => x.Value == lang).Key;
                        }
                        catch { }

                        bool pictureBased = false;
                        try
                        {
                            string format = mediaInfo.Get(StreamKind.Text, i, "Format");
                            if (format == "PGS" || format == "VobSub") { pictureBased = true; }
                        }
                        catch { }

                        SubtitleTracks.Add(new Subtitle.SubtitleTracks()
                        {
                            Active = true,
                            Index = i,
                            Languages = resources.MediaLanguages.LanguageKeys,
                            Language = lang,
                            CustomName = name,
                            Default = false,
                            BurnIn = false,
                            PictureBased = pictureBased
                        });
                    }
                }

                mediaInfo.Close();

                InputFileName = Path.GetFileName(InputPath);
            }
        }
    }
}
