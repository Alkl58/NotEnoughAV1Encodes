using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace NotEnoughAV1Encodes.Video
{
    class VideoMuxer
    {
        public void Concat(Queue.QueueElement queueElement)
        {
            Debug.WriteLine("Landed in Concat()");
            queueElement.Progress = 0.0;
            queueElement.Status = "Muxing files. Please wait.";

            // Getting all Chunks
            IOrderedEnumerable<string> sortedChunks = Directory.GetFiles(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video"), "*.webm").OrderBy(f => f);
            if (queueElement.EncodingMethod > 4)
            {
                sortedChunks = Directory.GetFiles(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video"), "*.ivf").OrderBy(f => f);
            }

            Debug.WriteLine("Chunks read: " + sortedChunks.ToString());

            using (StreamWriter outputFile = new StreamWriter(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "chunks.txt")))
            {
                foreach (string fileTemp in sortedChunks)
                {
                    string tempName = fileTemp.Replace("'", "'\\''");
                    outputFile.WriteLine("file '" + tempName + "'");
                }
            }

            // Setting Output for FFmpeg
            string FFmpegOutput = Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "temp_mux.mkv");
            if (queueElement.AudioCommand == null && queueElement.VFR == false)
            {
                FFmpegOutput = queueElement.Output;
            }

            // Muxing Chunks
            Process processVideo = new();
            ProcessStartInfo startInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "FFmpeg"),
                RedirectStandardError = true,
                Arguments = "/C ffmpeg.exe -y -f concat -safe 0 -i \"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "chunks.txt") + "\" -c copy \"" + FFmpegOutput + "\"",
                CreateNoWindow = true
            };

            processVideo.StartInfo = startInfo;
            processVideo.Start();

            StreamReader sr = processVideo.StandardError;
            while (!sr.EndOfStream)
            {
                int processedFrames = Global.GetTotalFramesProcessed(sr.ReadLine());
                if (processedFrames != 0)
                {
                    queueElement.Progress = Convert.ToDouble(processedFrames);
                    queueElement.Status = "Muxing Chunks - " + ((decimal)queueElement.Progress / queueElement.FrameCount).ToString("0.00%");
                }
            }
            processVideo.WaitForExit();
            

            bool MuxWithMKVMerge = false;

            string audioMuxCommand = "";
            if (queueElement.AudioCommand != null)
            {
                audioMuxCommand = "--default-track 0:yes \"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Audio", "audio.mkv") + "\"";
                MuxWithMKVMerge = true;
            }

            string vfrMuxCommand = "";
            if (queueElement.VFR)
            {
                vfrMuxCommand = "--timestamps 0:\"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "vsync.txt") + "\"";
            }

            Debug.WriteLine("MuxWithMKVMerge " + MuxWithMKVMerge.ToString());
            Debug.WriteLine("AudioCommand " + audioMuxCommand);
            Debug.WriteLine("VFRCommand " + vfrMuxCommand);

            if (MuxWithMKVMerge)
            {
                string _webmcmd = "";
                if (Path.GetExtension(queueElement.Output).ToLower() == ".webm")
                {
                    _webmcmd = " --webm ";
                }

                //Run mkvmerge command
                Process processMKVMerge = new();
                ProcessStartInfo startInfoMKVMerge = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "MKVToolNix"),
                    Arguments = "/C mkvmerge.exe " + _webmcmd + " --output \"" + queueElement.Output + "\" --language 0:und --default-track 0:yes \"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "temp_mux.mkv") + "\" " + audioMuxCommand + " " + vfrMuxCommand
                };
                processMKVMerge.StartInfo = startInfoMKVMerge;

                processMKVMerge.Start();
                string _output = processMKVMerge.StandardOutput.ReadToEnd();
                processMKVMerge.WaitForExit();

                if (processMKVMerge.ExitCode != 0)
                {
                    MessageBox.Show(_output, "mkvmerge", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }
}
