namespace NotEnoughAV1Encodes.Audio
{
    internal class CommandGenerator
    {
        public string Generate(System.Windows.Controls.ItemCollection tracks)
        {
            string audioCommand = "";
            int endIndex = 0;
            int externalIndex = 0;
            bool copyaudio = false;
            bool noaudio = true;

            foreach (AudioTracks track in tracks)
            {
                // Skip Audio Track if not active
                if(track.Active == false) continue;

                if (track.External)
                {
                    externalIndex += 1;
                }

                audioCommand += MultipleTrackCommandGenerator(int.Parse(track.Bitrate), track.Index, endIndex, track.Codec, track.Channels, track.Language, track.CustomName, track.PCM, track.External, externalIndex);
                endIndex += 1;

                if (track.Codec == 5)
                {
                    copyaudio = true;
                }

                noaudio = false;
            }

            if (!copyaudio)
            {
                audioCommand += " -af aformat=channel_layouts=" + '\u0022' + "7.1|5.1|stereo|mono" + '\u0022';
            }

            return noaudio ? null : audioCommand;
        }

        private static string SwitchCodec(int _audioCodec, bool _pcmBluray)
        {
            string audioCodeSwitch = _audioCodec switch
            {
                0 => " libopus",
                1 => " ac3",
                2 => " eac3",
                3 => " aac",
                4 => " libmp3lame",
                5 => _pcmBluray ? " pcm_s16le" : " copy",
                _ => " ",
            };
            return audioCodeSwitch;
        }

        private string MultipleTrackCommandGenerator(int activeTrackBitrate, int mapIndex, int endIndex, int activTrackCodec, int channelLayout, string language, string trackName, bool pcmBluray, bool external, int externalIndex)
        {
            string audioCodecCommand;

            // Audio Mapping
            audioCodecCommand = " -map 0:a:" + mapIndex + " -c:a:" + endIndex;

            if (external)
            {
                audioCodecCommand = " -map " + externalIndex + ":a:" + mapIndex + " -c:a:" + endIndex;
            }

            // Codec
            audioCodecCommand += SwitchCodec(activTrackCodec, pcmBluray);
            // Bitrate
            if (activTrackCodec != 5)
            {
                audioCodecCommand += " -b:a:" + endIndex + " " + activeTrackBitrate + "k";
            }
            // Channel Layout
            audioCodecCommand += " -ac:a:" + endIndex + " " + SetChannelLayout(channelLayout);
            // Metadata
            audioCodecCommand += " -metadata:s:a:" + endIndex + " language=" + resources.MediaLanguages.Languages[language];
            // Title
            if (activTrackCodec != 5)
            {
                audioCodecCommand += " -metadata:s:a:" + endIndex + " title=" + '\u0022' + trackName + '\u0022';
            }
            return audioCodecCommand;
        }

        private static string SetChannelLayout(int _layout)
        {
            string _returnLayout = _layout switch
            {
                0 => "1",
                1 => "2",
                2 => "6",
                3 => "8",
                _ => "2",
            };
            return _returnLayout;
        }
    }
}
