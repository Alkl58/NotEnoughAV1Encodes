using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NotEnoughAV1Encodes.Video
{
    class VideoMuxer
    {
        public static void Concat(Queue.QueueElement queueElement)
        {
            Debug.WriteLine("Landed in Concat()");
            queueElement.Progress = queueElement.FrameCount;
            queueElement.Status = "Muxing files. Please wait.";
            IOrderedEnumerable<string> sortedChunks = null;
            sortedChunks = Directory.GetFiles(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Video"), "*.webm").OrderBy(f => f);
            Debug.WriteLine("Chunks read: " + sortedChunks.ToString());
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "chunks.txt")))
            {
                foreach (string fileTemp in sortedChunks)
                {
                    string tempName = fileTemp.Replace("'", "'\\''");
                    outputFile.WriteLine("file '" + tempName + "'");
                }
            }

            string FFmpegOutput;
            if (queueElement.AudioCommand == null)
            {
                FFmpegOutput = queueElement.Output;
            }
            else
            {
                FFmpegOutput = Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "temp_mux.mkv");
            }

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
            string _output = processVideo.StandardError.ReadToEnd();
            processVideo.WaitForExit();
            Debug.WriteLine(_output);

            bool MuxWithMKVMerge = false;

            string audioMuxCommand = "";
            if (queueElement.AudioCommand != null)
            {
                audioMuxCommand = "--default-track 0:yes \"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "Audio", "audio.mkv") + "\"";
                MuxWithMKVMerge = true;
            }

            Debug.WriteLine("MuxWithMKVMerge " + MuxWithMKVMerge.ToString());
            Debug.WriteLine("AudioCommand " + audioMuxCommand);

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
                    Arguments = "/C mkvmerge.exe " + _webmcmd + " --output \"" + queueElement.Output + "\" --language 0:und --default-track 0:yes \"" + Path.Combine(Global.Temp, "NEAV1E", queueElement.UniqueIdentifier, "temp_mux.mkv") + "\" " + audioMuxCommand
                };
                processMKVMerge.StartInfo = startInfoMKVMerge;

                processMKVMerge.Start();
                _output = processMKVMerge.StandardOutput.ReadToEnd();
                processMKVMerge.WaitForExit();

                if (processMKVMerge.ExitCode != 0)
                {
                    //Helpers.Logging(_output);
                    MessageBox.Show(_output, "mkvmerge", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }
}
