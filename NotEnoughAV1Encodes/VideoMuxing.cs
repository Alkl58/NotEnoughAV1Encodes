using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace NotEnoughAV1Encodes
{
    class VideoMuxing
    {
        public static async Task Concat()
        {
            if (SmallFunctions.Cancel.CancelAll == false)
            {
                //Gets all .ivf files from the temp Path
                string[] filePaths = Directory.GetFiles(Path.Combine(MainWindow.tempPath, "Chunks"), "*.ivf", SearchOption.TopDirectoryOnly);

                //Logic for appending the files
                string appendedFiles = "";
                string appendTo = "";
                int counter = 0;
                int x = 2;
                int y = 1;
                foreach (var file in filePaths)
                {
                    if (counter == 0) 
                    { 
                        appendedFiles += '\u0022' + file + '\u0022';
                        appendTo += "1:0:0:0";
                    }
                    else
                    { 
                        appendedFiles += " + " + '\u0022' + file + '\u0022';
                        if (counter != 1)
                        {
                            appendTo += "," + x + ":0:" + y + ":0";
                            x++; y++;
                        }
                    }
                    counter++; 
                }

                //-----------------------------------------------------

                if (MainWindow.audioEncoding == false)
                {
                    if(MainWindow.subtitleEncoding == false)
                    {
                        //No Audio && No Softsubs
                        //Run mkvmerge command
                        Process mkvmerge = new Process();
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            WindowStyle = ProcessWindowStyle.Hidden,
                            FileName = "cmd.exe",
                            WorkingDirectory = MainWindow.mkvToolNixPath,
                            Arguments = "/C mkvmerge.exe --output " + '\u0022' + MainWindow.videoOutput + '\u0022' + MainWindow.encoderMetadata + " --language 0:und " + appendedFiles + " --append-to " + appendTo
                        };
                        mkvmerge.StartInfo = startInfo;
                        SmallFunctions.Logging("VideoMuxing() Command: mkvmerge.exe --output " + '\u0022' + MainWindow.videoOutput + '\u0022' + MainWindow.encoderMetadata + " --language 0:und " + appendedFiles + " --append-to " + appendTo);
                        mkvmerge.Start();
                        mkvmerge.WaitForExit();
                    }
                    else
                    {
                        //No Audio && With Softsubs
                        //Run mkvmerge command
                        Process mkvmerge = new Process();
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            WindowStyle = ProcessWindowStyle.Hidden,
                            FileName = "cmd.exe",
                            WorkingDirectory = MainWindow.mkvToolNixPath,
                            Arguments = "/C mkvmerge.exe --output " + '\u0022' + Path.Combine(MainWindow.tempPath, "temp.mkv") + '\u0022' + " --language 0:und " + appendedFiles + " --append-to " + appendTo
                        };
                        mkvmerge.StartInfo = startInfo;
                        SmallFunctions.Logging("VideoMuxing() Command: mkvmerge.exe --output " + '\u0022' + Path.Combine(MainWindow.tempPath, "temp.mkv") + '\u0022' + " --language 0:und " + appendedFiles + " --append-to " + appendTo);
                        mkvmerge.Start();
                        mkvmerge.WaitForExit();

                        //Run mkvmerge command
                        Process mkvToolNix = new Process();
                        ProcessStartInfo startInfoa = new ProcessStartInfo
                        {
                            WindowStyle = ProcessWindowStyle.Hidden,
                            FileName = "cmd.exe",
                            WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Apps", "mkvtoolnix"),
                            Arguments = "/C mkvmerge.exe --output " + '\u0022' + MainWindow.videoOutput + '\u0022' + MainWindow.encoderMetadata + " --no-track-tags --no-global-tags --language 0:und --default-track 0:yes " + '\u0022' + Path.Combine(MainWindow.tempPath, "temp.mkv") + '\u0022' + " " + MainWindow.subtitleMuxingInput
                        };
                        mkvToolNix.StartInfo = startInfoa;
                        SmallFunctions.Logging("VideoMuxing() Command: mkvmerge.exe --output " + '\u0022' + MainWindow.videoOutput + '\u0022' + MainWindow.encoderMetadata + " --no-track-tags --no-global-tags --language 0:und --default-track 0:yes " + '\u0022' + Path.Combine(MainWindow.tempPath, "temp.mkv") + '\u0022' + " " + MainWindow.subtitleMuxingInput);
                        mkvToolNix.Start();
                        mkvToolNix.WaitForExit();                       
                    }
                }
                else
                {
                    //Temp File
                    Process mkvmerge = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = "cmd.exe",
                        WorkingDirectory = MainWindow.mkvToolNixPath,
                        Arguments = "/C mkvmerge.exe --output " + '\u0022' + Path.Combine(MainWindow.tempPath, "temp.mkv") + '\u0022' + " --language 0:und " + appendedFiles + " --append-to " + appendTo
                    };
                    mkvmerge.StartInfo = startInfo;
                    SmallFunctions.Logging("VideoMuxing() Command: mkvmerge.exe --output " + '\u0022' + Path.Combine(MainWindow.tempPath, "temp.mkv") + '\u0022' + MainWindow.encoderMetadata + " --language 0:und " + appendedFiles + " --append-to " + appendTo);
                    mkvmerge.Start();
                    mkvmerge.WaitForExit();


                    if (MainWindow.subtitleEncoding == false)
                    {
                        //With Audio && No Softsubs
                        Process mkvmergea = new Process();
                        ProcessStartInfo startInfob = new ProcessStartInfo
                        {
                            WindowStyle = ProcessWindowStyle.Hidden,
                            FileName = "cmd.exe",
                            WorkingDirectory = MainWindow.mkvToolNixPath,
                            Arguments = "/C mkvmerge.exe --output " + '\u0022' + MainWindow.videoOutput + '\u0022' + MainWindow.encoderMetadata + " --no-track-tags --no-global-tags --language 0:und --default-track 0:yes " + '\u0022' + Path.Combine(MainWindow.tempPath, "temp.mkv") + '\u0022' + " --default-track 0:yes " + '\u0022' + Path.Combine(MainWindow.tempPath, "AudioEncoded", "audio.mkv") + '\u0022'
                        };
                        mkvmergea.StartInfo = startInfob;
                        SmallFunctions.Logging("VideoMuxing() Command: mkvmerge.exe --output " + '\u0022' + MainWindow.videoOutput + '\u0022' + MainWindow.encoderMetadata + " --no-track-tags --no-global-tags --language 0:und --default-track 0:yes " + '\u0022' + Path.Combine(MainWindow.tempPath, "temp.mkv") + '\u0022' + " --default-track 0:yes " + '\u0022' + Path.Combine(MainWindow.tempPath, "AudioEncoded", "audio.mkv") + '\u0022');
                        mkvmergea.Start();
                        mkvmergea.WaitForExit();
                    }
                    else
                    {
                        //With Audio && With Softsubs
                        //Run mkvmerge command
                        Process mkvToolNix = new Process();
                        ProcessStartInfo startInfoc = new ProcessStartInfo
                        {
                            WindowStyle = ProcessWindowStyle.Hidden,
                            FileName = "cmd.exe",
                            WorkingDirectory = MainWindow.mkvToolNixPath,
                            Arguments = "/C mkvmerge.exe --output " + '\u0022' + MainWindow.videoOutput + '\u0022' + MainWindow.encoderMetadata + " --no-track-tags --no-global-tags --language 0:und --default-track 0:yes " + '\u0022' + Path.Combine(MainWindow.tempPath, "temp.mkv") + '\u0022' + " --default-track 0:yes " + '\u0022' + Path.Combine(MainWindow.tempPath, "AudioEncoded", "audio.mkv") + '\u0022' + " " + MainWindow.subtitleMuxingInput
                        };
                        mkvToolNix.StartInfo = startInfoc;
                        SmallFunctions.Logging("VideoMuxing() Command: mkvmerge.exe --output " + '\u0022' + MainWindow.videoOutput + '\u0022' + MainWindow.encoderMetadata + " --no-track-tags --no-global-tags --language 0:und --default-track 0:yes " + '\u0022' + Path.Combine(MainWindow.tempPath, "temp.mkv") + '\u0022' + " --default-track 0:yes " + '\u0022' + Path.Combine(MainWindow.tempPath, "AudioEncoded", "audio.mkv") + '\u0022' + " " + MainWindow.subtitleMuxingInput);
                        mkvToolNix.Start();
                        mkvToolNix.WaitForExit();
                    }
                }
            }
        }
    }
}
