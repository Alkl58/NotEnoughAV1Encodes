using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace NotEnoughAV1Encodes.Audio
{
    class EncodeAudio
    {
        public void Encode(Queue.QueueElement queueElement, CancellationToken _token)
        {
            if (queueElement.AudioCommand != null && !File.Exists(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Audio", "exit.log")))
            {
                if (!Directory.Exists(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Audio")))
                {
                    Directory.CreateDirectory(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Audio"));
                }

                Process processAudio = new();
                ProcessStartInfo startInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "FFmpeg"),
                    Arguments = "/C ffmpeg.exe -i \"" + queueElement.Input + "\" -vn -sn -map_metadata -1 " + queueElement.AudioCommand + " \"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Audio", "audio.mkv") + "\"",
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true
                };
                processAudio.StartInfo = startInfo;

                _token.Register(() => { try { processAudio.StandardInput.Write("q"); } catch { } });

                processAudio.Start();

                StreamReader sr = processAudio.StandardError;
                while (!sr.EndOfStream)
                {
                    queueElement.Progress = Convert.ToDouble(getTotalFramesProcessed(sr.ReadLine()));
                    queueElement.Status = "Encoding Audio - " + ((decimal)queueElement.Progress / queueElement.FrameCount).ToString("0.00%");
                }

                processAudio.WaitForExit();

                // Reset Progressbar
                queueElement.Progress = 0.00;

                if (processAudio.ExitCode == 0 && _token.IsCancellationRequested == false)
                {
                    File.Create(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Audio", "exit.log"));
                }
            }
        }

        private int getTotalFramesProcessed(string stderr)
        {
            try
            {
                int Start, End;
                Start = stderr.IndexOf("frame=", 0) + "frame=".Length;
                End = stderr.IndexOf("fps=", Start);
                return int.Parse(stderr.Substring(Start, End - Start));
            }
            catch
            {
                return 0;
            }
        }
    }
}
