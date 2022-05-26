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
            Global.Logger("DEBUG - EncodeAudio.Encode()", queueElement.Output + ".log");
            if (queueElement.AudioCommand != null && !File.Exists(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Audio", "exit.log")))
            {
                Directory.CreateDirectory(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Audio"));

                Global.Logger("INFO  - EncodeAudio.Encode() => Command: " + queueElement.AudioCommand, queueElement.Output + ".log");

                Process processAudio = new();
                ProcessStartInfo startInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "FFmpeg"),
                    Arguments = "/C ffmpeg.exe -i \"" + queueElement.VideoDB.InputPath + "\" -vn -sn -map_metadata -1 " + queueElement.AudioCommand + " \"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Audio", "audio.mkv") + "\"",
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
                    int processedFrames = Global.GetTotalTimeProcessed(sr.ReadLine(), queueElement);
                    if (processedFrames != 0)
                    {
                        queueElement.Progress = Convert.ToDouble(processedFrames);
                        queueElement.Status = "Encoding Audio - " + ((decimal)queueElement.Progress / queueElement.FrameCount).ToString("0.00%");
                    }
                }

                processAudio.WaitForExit();

                // Reset Progressbar
                queueElement.Progress = 0.00;

                if (processAudio.ExitCode == 0 && _token.IsCancellationRequested == false)
                {
                    File.Create(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Audio", "exit.log"));
                    Global.Logger("DEBUG - EncodeAudio.Encode() => ExitCode: " + processAudio.ExitCode, queueElement.Output + ".log");
                }
                else
                {
                    Global.Logger("FATAL - EncodeAudio.Encode() => ExitCode: " + processAudio.ExitCode, queueElement.Output + ".log");
                }
            }
            else
            {
                Global.Logger("WARN  - EncodeAudio.Encode() => File already exist - Resuming?", queueElement.Output + ".log");
            }
        }
    }
}
