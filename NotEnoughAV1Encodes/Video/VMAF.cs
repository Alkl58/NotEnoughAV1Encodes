using MathNet.Numerics;
using NotEnoughAV1Encodes.Queue;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace NotEnoughAV1Encodes.Video
{
    public class VMAF
    {
        private double CalculateVMAFScore(string source, string distorted, string chunk, QueueElement queueElement, CancellationToken token)
        {
            string vmafModel = "../vmaf/vmaf_v0.6.1.json";
            string logPath = distorted.Replace("\\", "/").Replace(":", "\\\\:") + ".xml";

            string ffmpegCommand = "/C ffmpeg.exe";

            // Input
            ffmpegCommand += source;
            ffmpegCommand += " -i \"" + distorted + "\"";

            // VMAF Filter
            ffmpegCommand += " -lavfi \"[0:v]setpts=PTS-STARTPTS[reference];" +
                                       "[1:v]scale=1920:1080:flags=bicubic,setpts=PTS-STARTPTS[distorted];" +
                                       "[distorted][reference]libvmaf=log_fmt=xml:log_path=" + logPath + ":model_path=" + vmafModel + ":n_threads=4\"" +
                                       " -f null -";

            Process processVideo = new();
            ProcessStartInfo startInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "FFmpeg"),
                Arguments = ffmpegCommand,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true
            };

            processVideo.StartInfo = startInfo;

            token.Register(() => { try { processVideo.StandardInput.Write("q"); } catch { } });

            processVideo.Start();

            // Get launched Process ID
            int _pid = processVideo.Id;

            // Add Process ID to Array, inorder to keep track / kill the instances
            Global.LaunchedPIDs.Add(_pid);

            //StreamReader sr = processVideo.StandardError;

            double vmafScore = 0.0;

            string lastLine = null;
            while (!processVideo.StandardError.EndOfStream)
            {
                lastLine = processVideo.StandardError.ReadLine();
                //Global.Logger(lastLine, queueElement.Output + ".log");
                if (lastLine.Contains("VMAF score:"))
                {
                    string split = "VMAF score:";
                    vmafScore = double.Parse(lastLine[(lastLine.IndexOf(split) + split.Length)..], System.Globalization.CultureInfo.InvariantCulture);
                }
            }


            processVideo.WaitForExit();

            // Remove PID from Array after Exit
            Global.LaunchedPIDs.RemoveAll(i => i == _pid);

            if (processVideo.ExitCode == 0 && token.IsCancellationRequested == false)
            {
                Global.Logger("INFO  - VMAF.CalculateVMAFScore() => Exit Code: 0  Chunk: " + chunk, queueElement.Output + ".log");
            }
            else
            {
                queueElement.Error = true;
                queueElement.ErrorCount += 1;
                Global.Logger("FATAL - VMAF.CalculateVMAFScore() => Exit Code: " + processVideo.ExitCode + "  Chunk: " + chunk, queueElement.Output + ".log");
            }

            return vmafScore;
        }

        private double EncodeAndCalculate(QueueElement queueElement, string chunk, int index, string ffmpegFilter, string quality, Settings settings, CancellationToken token)
        {
            // Create Folder where VMAF Probe Encodes are saved
            Directory.CreateDirectory(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "VMAF"));

            string ChunkOutput = "\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "VMAF", index.ToString("D6") + "_vmaf_probe_q" + quality + ".mp4") + "\"";
            string videoCommand = " -c:v libaom-av1 -crf " + quality + " -b:v 0 -cpu-used 6 -threads 4 -tile-columns 2 -tile-rows 1 ";

            string ChunkInput = " -i \"" + chunk + "_vmaf_reference.mp4\"";

            if (queueElement.Preset.TargetVMAFUserEncoderSettings)
            {
                videoCommand = queueElement.VideoCommand;
                videoCommand = videoCommand.Replace("{q_vmaf}", quality);
            }

            Global.Logger("INFO  - VMAF.EncodeAndCalculate() => Chunk: " + chunk + " -  Q: " + quality + " - Command: " + "/C ffmpeg.exe -y " + ChunkInput + " " + ffmpegFilter + " -an -sn -map_metadata -1 " + videoCommand + " " + ChunkOutput, queueElement.Output + ".log");

            Process processVideo = new();
            ProcessStartInfo startInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "FFmpeg"),
                Arguments = "/C ffmpeg.exe -y " + ChunkInput + " " + ffmpegFilter + " -an -sn -map_metadata -1 " + videoCommand + " " + ChunkOutput,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true
            };

            processVideo.StartInfo = startInfo;

            token.Register(() => { try { processVideo.StandardInput.Write("q"); } catch { } });

            processVideo.Start();

            // Get launched Process ID
            int _pid = processVideo.Id;

            // Add Process ID to Array, inorder to keep track / kill the instances
            Global.LaunchedPIDs.Add(_pid);

            string lastLine = null;
            while (! processVideo.StandardError.EndOfStream)
            {
                lastLine = processVideo.StandardError.ReadLine();
            }

            processVideo.WaitForExit();

            // Remove PID from Array after Exit
            Global.LaunchedPIDs.RemoveAll(i => i == _pid);

            if (processVideo.ExitCode == 0 && token.IsCancellationRequested == false)
            {
                Global.Logger("INFO  - VMAF.EncodeAndCalculate() => Exit Code: 0  Chunk: " + chunk, queueElement.Output + ".log");
            }
            else
            {
                queueElement.Error = true;
                queueElement.ErrorCount += 1;
                Global.Logger("FATAL - VMAF.EncodeAndCalculate() => Exit Code: " + processVideo.ExitCode + "  Chunk: " + chunk, queueElement.Output + ".log");
            }

            // Calculate VMAF Score
            double vmafScore = CalculateVMAFScore(ChunkInput, ChunkOutput, chunk, queueElement, token);
            Global.Logger("INFO  - VMAF.EncodeAndCalculate() => Chunk: " + chunk + " -  Q: " + quality + " => VAMF Score: " + vmafScore, queueElement.Output + ".log");
            return vmafScore;
        }

        private static double InterpolateVMAF(double[] qValues, double[] vmafValues, double targetVmaf)
        {
            return Interpolate.Linear(vmafValues, qValues).Interpolate(targetVmaf);
        }

        public string Probe(QueueElement queueElement, string chunk, int index, string ffmpegFilter, Settings settings, CancellationToken token)
        {
            Global.Logger("INFO  - VMAF.Probe() => Chunk: " + chunk, queueElement.Output + ".log");
            // Check if already Probed
            List<ChunkVMAF> tempList = queueElement.ChunkVMAF.ToList();
            ChunkVMAF result = tempList.FirstOrDefault(n => n.ChunkName == chunk);
            if (result != null)
            {
                Global.Logger("INFO  - VMAF.Probe() => Chunk: " + chunk + " - Already Probed - Return Q: " + result.CalculatedQuantizer, queueElement.Output + ".log");
                return result.CalculatedQuantizer;
            }

            // Create VMAF Object
            ChunkVMAF chunkVMAF = new();
            chunkVMAF.ChunkName = chunk;

            // Base Encodes
            List<double> qValues = new();
            List<double> vmafValues = new();

            // Floor / Ceiling
            qValues.Add(0);
            vmafValues.Add(100);

            qValues.Add(queueElement.Preset.TargetVMAFMinQ);
            double vmaf = EncodeAndCalculate(queueElement, chunk, index, ffmpegFilter, queueElement.Preset.TargetVMAFMinQ.ToString(), settings, token);
            vmafValues.Add(vmaf);
            Global.Logger("INFO  - VMAF.Probe() => Chunk: " + chunk + " - Probe Q: " + queueElement.Preset.TargetVMAFMinQ.ToString() + " => VMAF: " + vmaf.ToString(), queueElement.Output + ".log");

            double diff = Math.Abs(queueElement.Preset.TargetVMAFScore - vmaf);
            if (vmaf < queueElement.Preset.TargetVMAFScore || diff < 1.0)
            {
                // Skip everything, the calculated vmaf score is less than the target vmaf score, can't go below the TargetVMAFMinQ (Highest Quality)
                // or diff is already less than 1
                chunkVMAF.CalculatedQuantizer = queueElement.Preset.TargetVMAFMinQ.ToString();
                chunkVMAF.VMAFValues = vmafValues;
                chunkVMAF.QValues = qValues;

                queueElement.ChunkVMAF.Add(chunkVMAF);

                return chunkVMAF.CalculatedQuantizer;
            }

            qValues.Add(queueElement.Preset.TargetVMAFMaxQ);
            vmaf = EncodeAndCalculate(queueElement, chunk, index, ffmpegFilter, queueElement.Preset.TargetVMAFMaxQ.ToString(), settings, token);
            vmafValues.Add(vmaf);
            Global.Logger("INFO  - VMAF.Probe() => Chunk: " + chunk + " - Probe Q: " + queueElement.Preset.TargetVMAFMaxQ.ToString() + " => VMAF: " + vmaf.ToString(), queueElement.Output + ".log");


            // First Calculation
            double interpolatedQ = InterpolateVMAF(qValues.ToArray(), vmafValues.ToArray(), queueElement.Preset.TargetVMAFScore);
            Global.Logger("INFO  - VMAF.Probe() => Chunk: " + chunk + " - Interpolated Q: " + interpolatedQ.ToString() + " for VMAF: " + queueElement.Preset.TargetVMAFScore.ToString(), queueElement.Output + ".log");

            // already outside of the given min max q values
            if (interpolatedQ < queueElement.Preset.TargetVMAFMinQ)
            {
                chunkVMAF.CalculatedQuantizer = queueElement.Preset.TargetVMAFMinQ.ToString();
                chunkVMAF.VMAFValues = vmafValues;
                chunkVMAF.QValues = qValues;

                queueElement.ChunkVMAF.Add(chunkVMAF);

                return chunkVMAF.CalculatedQuantizer;
            }

            if (interpolatedQ > queueElement.Preset.TargetVMAFMaxQ)
            {
                chunkVMAF.CalculatedQuantizer = queueElement.Preset.TargetVMAFMaxQ.ToString();
                chunkVMAF.VMAFValues = vmafValues;
                chunkVMAF.QValues = qValues;

                queueElement.ChunkVMAF.Add(chunkVMAF);

                return chunkVMAF.CalculatedQuantizer;
            }

            // Do the encodes / calculations as often as specified in the settings
            for (int i = 2; i < queueElement.Preset.TargetVMAFProbes; i++)
            {
                qValues.Add(interpolatedQ);
                double vmafTmp = EncodeAndCalculate(queueElement, chunk, index, ffmpegFilter, Convert.ToInt32(Math.Round(interpolatedQ)).ToString(), settings, token);
                vmafValues.Add(vmafTmp);

                interpolatedQ = InterpolateVMAF(qValues.ToArray(), vmafValues.ToArray(), queueElement.Preset.TargetVMAFScore);
                Global.Logger("INFO  - VMAF.Probe() => Chunk: " + chunk + " - Interpolated Q: " + interpolatedQ.ToString() + " for VMAF: " + queueElement.Preset.TargetVMAFScore.ToString(), queueElement.Output + ".log");

                // Skip remaining probes if the target vmaf value is less than 1 apart from current vmaf value
                diff = Math.Abs(queueElement.Preset.TargetVMAFScore - vmafTmp);
                if (diff < 1.0 || interpolatedQ < queueElement.Preset.TargetVMAFMinQ)
                {
                    Global.Logger("INFO  - VMAF.Probe() => Chunk: " + chunk + " - SKIP - Interpolated Q: " + interpolatedQ.ToString() + " VMAF Diff: " + diff.ToString(), queueElement.Output + ".log");
                    break;
                }
            }

            chunkVMAF.CalculatedQuantizer = Convert.ToInt32(Math.Round(interpolatedQ)).ToString();
            chunkVMAF.VMAFValues = vmafValues;
            chunkVMAF.QValues = qValues;

            queueElement.ChunkVMAF.Add(chunkVMAF);

            return chunkVMAF.CalculatedQuantizer;
        }
    }
}
