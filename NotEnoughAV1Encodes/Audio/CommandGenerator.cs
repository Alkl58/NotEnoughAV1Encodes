namespace NotEnoughAV1Encodes.Audio
{
    internal class CommandGenerator
    {
        public string Generate(System.Windows.Controls.ItemCollection tracks)
        {
            string audioCommand = "";
            int endIndex = 0;
            bool copyaudio = false;
            bool noaudio = true;

            foreach (AudioTracks track in tracks)
            {
                audioCommand += MultipleTrackCommandGenerator(int.Parse(track.Bitrate), track.Index, endIndex, track.Codec, track.Channels, track.Language, track.CustomName, track.PCM);
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

        private string MultipleTrackCommandGenerator(int _activeTrackBitrate, int _mapIndex, int _endIndex, int _activTrackCodec, int _channelLayout, string _language, string _trackName, bool _pcmBluray)
        {
            string audioCodecCommand;
            // Audio Mapping
            audioCodecCommand = " -map 0:a:" + _mapIndex + " -c:a:" + _endIndex;
            // Codec
            audioCodecCommand += SwitchCodec(_activTrackCodec, _pcmBluray);
            // Bitrate
            if (_activTrackCodec != 5)
            {
                audioCodecCommand += " -b:a:" + _endIndex + " " + _activeTrackBitrate + "k";
            }
            // Channel Layout
            audioCodecCommand += " -ac:a:" + _endIndex + " " + SetChannelLayout(_channelLayout);
            // Metadata
            audioCodecCommand += " -metadata:s:a:" + _endIndex + " language=" + _language;
            // Title
            if (_activTrackCodec != 5)
            {
                audioCodecCommand += " -metadata:s:a:" + _endIndex + " title=" + '\u0022' + _trackName + '\u0022';
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
