using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace NotEnoughAV1Encodes.Subtitle
{
    internal class ExtractSubtitles
    {
        public void Extract(Queue.QueueElement queueElement, CancellationToken _token)
        {
            Global.Logger("DEBUG - ExtractSubtitles.Extract()", queueElement.Output + ".log");
            if (queueElement.SubtitleCommand != null && !File.Exists(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Subtitles", "exit.log")))
            {
                Directory.CreateDirectory(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Subtitles"));
                Global.Logger("INFO  - ExtractSubtitles.Extract() => Command: " + queueElement.SubtitleCommand, queueElement.Output + ".log");

                Process processSubtitles = new();
                ProcessStartInfo startInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "FFmpeg"),
                    Arguments = "/C ffmpeg.exe -i \"" + queueElement.VideoDB.InputPath + "\" -vn -an -dn -map_metadata -1 -map 0:s? -c:s copy \"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Subtitles", "subs.mkv") + "\"",
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true
                };
                processSubtitles.StartInfo = startInfo;

                _token.Register(() => { try { processSubtitles.StandardInput.Write("q"); } catch { } });

                // Read stderr to get progress
                processSubtitles.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        int processedFrames = Global.GetTotalFramesProcessed(e.Data);
                        if (processedFrames != 0)
                        {
                            queueElement.Progress = Convert.ToDouble(processedFrames);
                            queueElement.Status = "Extracting Subtitles - " + ((decimal)queueElement.Progress / queueElement.FrameCount).ToString("0.00%");
                        }
                    }
                };

                processSubtitles.Start();

                processSubtitles.BeginErrorReadLine();

                processSubtitles.WaitForExit();

                // Reset Progressbar
                queueElement.Progress = 0.00;

                if (processSubtitles.ExitCode == 0 && _token.IsCancellationRequested == false)
                {
                    File.Create(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Subtitles", "exit.log"));
                    Global.Logger("DEBUG - ExtractSubtitles.Extract() => ExitCode: " + processSubtitles.ExitCode, queueElement.Output + ".log");
                }
                else
                {
                    Global.Logger("FATAL - ExtractSubtitles.Extract() => ExitCode: " + processSubtitles.ExitCode, queueElement.Output + ".log");
                }
            }
            else
            {
                Global.Logger("WARN  - ExtractSubtitles.Extract() => File already exist - Resuming?", queueElement.Output + ".log");
            }
        }
    }
}
