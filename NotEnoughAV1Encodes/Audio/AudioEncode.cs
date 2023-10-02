using NotEnoughAV1Encodes.Queue;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace NotEnoughAV1Encodes.Audio
{
    class AudioEncode
    {
        public void Encode(QueueElement queueElement, CancellationToken _token)
        {
            Global.Logger("DEBUG - EncodeAudio.Encode()", queueElement.Output + ".log");

            if (queueElement.AudioCommand == null || File.Exists(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Audio", "exit.log")))
            {
                Global.Logger("WARN  - EncodeAudio.Encode() => File already exist - Resuming?", queueElement.Output + ".log");
                return;
            }

            Directory.CreateDirectory(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Audio"));

            Global.Logger("INFO  - EncodeAudio.Encode() => Command: " + queueElement.AudioCommand, queueElement.Output + ".log");

            string externalInput = "";
            foreach (AudioTracks audioTrack in queueElement.VideoDB.AudioTracks)
            {
                if (!audioTrack.External) continue;
                externalInput += " -i \"" + audioTrack.ExternalPath + "\"";
            }

            Process processAudio = new();
            ProcessStartInfo startInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "FFmpeg"),
                Arguments = "/C ffmpeg.exe -y -analyzeduration 100M -probesize 100M -i \"" + queueElement.VideoDB.InputPath + "\" " + externalInput + " -vn -sn -map_metadata -1 " + queueElement.AudioCommand + " \"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Audio", "audio.mkv") + "\"",
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true
            };
            processAudio.StartInfo = startInfo;

            _token.Register(() => { try { processAudio.StandardInput.Write("q"); } catch { } });

            string stderr = "\n";
            // Read stderr to get progress
            processAudio.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    string line = e.Data;
                    if (!line.Contains("time="))
                        stderr += line + "\n";
                    int processedFrames = Global.GetTotalTimeProcessed(line, queueElement);
                    if (processedFrames != 0)
                    {
                        queueElement.Progress = Convert.ToDouble(processedFrames);
                        queueElement.Status = "Encoding Audio - " + ((decimal)queueElement.Progress / queueElement.FrameCount).ToString("0.00%");
                    }
                }
            };

            processAudio.Start();

            processAudio.BeginErrorReadLine();

            processAudio.WaitForExit();

            // Reset Progressbar
            queueElement.Progress = 0.00;

            if (processAudio.ExitCode != 0 || _token.IsCancellationRequested == true)
            {
                queueElement.Error = true;
                queueElement.ErrorCount += 1;
                Global.Logger("FATAL - EncodeAudio.Encode() => ExitCode: " + processAudio.ExitCode, queueElement.Output + ".log");
                Global.Logger("==========================================================" + stderr, queueElement.Output + ".log");
                Global.Logger("==========================================================", queueElement.Output + ".log");
                return;
            }

            File.Create(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Audio", "exit.log"));
            Global.Logger("DEBUG - EncodeAudio.Encode() => ExitCode: " + processAudio.ExitCode, queueElement.Output + ".log");
        }
    }
}
