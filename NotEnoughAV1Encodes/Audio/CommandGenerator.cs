namespace NotEnoughAV1Encodes.Audio
{
    class CommandGenerator
    {
        public string Generate(System.Windows.Controls.ItemCollection tracks)
        {
            string audio_command = "";
            int end_index = 0;
            bool copyaudio = false;
            bool noaudio = true;

            foreach (AudioTracks track in tracks)
            {
                audio_command += MultipleTrackCommandGenerator(int.Parse(track.Bitrate), track.Index, end_index, track.Codec, track.Channels, track.Language, track.CustomName, track.PCM);
                end_index += 1;

                if (track.Codec == 5)
                {
                    copyaudio = true;
                }

                noaudio = false;
            }

            if (!copyaudio)
            {
                audio_command += " -af aformat=channel_layouts=" + '\u0022' + "7.1|5.1|stereo|mono" + '\u0022' + " ";
            }

            return noaudio ? null : audio_command;
        }

        private string SwitchCodec(int audio_codec, bool pcm_bluray)
        {
            string audio_codec_switch = "";
            switch (audio_codec)
            {
                case 0: audio_codec_switch = "libopus"; break;
                case 1: audio_codec_switch = "ac3"; break;
                case 2: audio_codec_switch = "eac3"; break;
                case 3: audio_codec_switch = "aac"; break;
                case 4: audio_codec_switch = "libmp3lame"; break;
                case 5:
                    if (pcm_bluray) { audio_codec_switch = "pcm_s16le"; }
                    else { audio_codec_switch = "copy"; }
                    break;

                default: break;
            }
            return audio_codec_switch;
        }

        private string audioCodecCommand = "";

        private string MultipleTrackCommandGenerator(int activetrackbitrate, int map_index, int end_index, int activtrackcodec, int channellayout, string lang, string track_name, bool pcm_bluray)
        {
            // Command Builder for Audio
            // Audio Mapping
            audioCodecCommand = " -map 0:a:" + map_index + " -c:a:" + end_index + " ";
            // Codec
            audioCodecCommand += SwitchCodec(activtrackcodec, pcm_bluray);
            // Channel Layout / Bitrate
            if (activtrackcodec != 5) { audioCodecCommand += " -b:a:" + end_index + " " + activetrackbitrate + "k"; }
            audioCodecCommand += " -ac:a:" + end_index + " " + SetChannelLayout(channellayout) + " ";
            // Metadata
            audioCodecCommand += " -metadata:s:a:" + end_index + " language=" + lang;
            if (activtrackcodec != 5)
            {
                audioCodecCommand += " -metadata:s:a:" + end_index + " title=" + '\u0022' + track_name + '\u0022' + " ";
            }
            return audioCodecCommand;
        }

        private static string SetChannelLayout(int layout)
        {
            string returnLayout;
            switch (layout)
            {
                case 0: returnLayout = "1"; break;
                case 1: returnLayout = "2"; break;
                case 2: returnLayout = "6"; break;
                case 3: returnLayout = "8"; break;
                default: returnLayout = "2"; break;
            }
            return returnLayout;
        }
    }
}
