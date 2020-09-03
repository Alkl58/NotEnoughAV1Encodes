using System;
using System.IO;

namespace NotEnoughAV1Encodes
{
    class VideoSplitting
    {
        public static string ffmpegCommand = "";
        public static void SplitVideo(string videoInput, int chunkLength, string reencodeCodec, bool reencode, bool beforereencode)
        {
            if (reencodeCodec == "x264") { reencodeCodec = "libx264 -crf 0 -preset ultrafast -g 9 -sc_threshold 0 -force_key_frames " + '\u0022' + "expr:gte(t, n_forced * 9)" + '\u0022'; }
            if (reencodeCodec == "utvideo") { reencodeCodec = "utvideo"; }
            if (reencodeCodec == "ffv1") { reencodeCodec = "ffv1 -level 3 -threads 4 -coder 1 -context 1 -g 1 -slicecrc 0 -slices 4"; }

            SmallFunctions.checkCreateFolder(Path.Combine(MainWindow.tempPath, "Chunks"));

            if (reencode && beforereencode)
            {
                //Encodes the Video temporary, then splits it while reencoding additionally
                //Found interesting problem: When encoding in x264 it leads to not the selected chunksize, when encoding ut it works flawless.
                ffmpegCommand = "/C ffmpeg.exe -i " + '\u0022' + videoInput + '\u0022' + MainWindow.ffmpegFramerateSplitting + " -map_metadata -1 " + MainWindow.subtitleFfmpegCommand + MainWindow.trimCommand + MainWindow.deinterlaceCommand + MainWindow.cropCommand +  " -c:v " + reencodeCodec + " -an " + '\u0022' + MainWindow.tempPath + "\\temp_prereencode.mkv" + '\u0022';
                SmallFunctions.Logging("VideoSplitting() BeforeReencodeCommand: " + ffmpegCommand);
                SmallFunctions.ExecuteFfmpegTask(ffmpegCommand);
                ffmpegCommand = "/C ffmpeg.exe -i " + '\u0022' + MainWindow.tempPath + "\\temp_prereencode.mkv" + '\u0022' + " -map_metadata -1 -c:v " + reencodeCodec + " -f segment -segment_time " + chunkLength + " -an " + '\u0022' + MainWindow.tempPath + "\\Chunks\\out%0d.mkv" + '\u0022';
                SmallFunctions.Logging("VideoSplitting() ReencodeCommand: " + ffmpegCommand);
                SmallFunctions.ExecuteFfmpegTask(ffmpegCommand);
            }
            else if(reencode && beforereencode == false)
            {
                //Only reencodes during Splitting
                ffmpegCommand = "/C ffmpeg.exe -i " + '\u0022' + videoInput + '\u0022' + MainWindow.ffmpegFramerateSplitting + " -map_metadata -1 " + MainWindow.subtitleFfmpegCommand + MainWindow.trimCommand + MainWindow.deinterlaceCommand + MainWindow.cropCommand + " -c:v " + reencodeCodec + " -f segment -segment_time " + chunkLength + " -an " + '\u0022' + MainWindow.tempPath + "\\Chunks\\out%0d.mkv" + '\u0022';
                SmallFunctions.Logging("VideoSplitting() ReencodeCommand: " + ffmpegCommand);
                SmallFunctions.ExecuteFfmpegTask(ffmpegCommand);
            }
            else if(reencode == false && beforereencode)
            {
                //Reencodes before splitting
                ffmpegCommand = "/C ffmpeg.exe -i " + '\u0022' + videoInput + '\u0022' + MainWindow.ffmpegFramerateSplitting + " -map_metadata -1 " + MainWindow.subtitleFfmpegCommand + MainWindow.trimCommand + MainWindow.deinterlaceCommand + MainWindow.cropCommand + " -c:v " + reencodeCodec + " -an " + '\u0022' + MainWindow.tempPath + "\\temp_prereencode.mkv" + '\u0022';
                SmallFunctions.Logging("VideoSplitting() BeforeReencodeCommand: " + ffmpegCommand);
                SmallFunctions.ExecuteFfmpegTask(ffmpegCommand);
                ffmpegCommand = "/C ffmpeg.exe -i " + '\u0022' + MainWindow.tempPath + "\\temp_prereencode.mkv" + '\u0022' + " -map_metadata -1 -vcodec copy -f segment -segment_time " + chunkLength + " -an " + '\u0022' + MainWindow.tempPath + "\\Chunks\\out%0d.mkv" + '\u0022';
                SmallFunctions.Logging("VideoSplitting() ReencodeCommand: " + ffmpegCommand);
                SmallFunctions.ExecuteFfmpegTask(ffmpegCommand);
            }
            else if(reencode == false && beforereencode == false)
            {
                //Splits the Video without Reencoding
                ffmpegCommand = "/C ffmpeg.exe -i " + '\u0022' + videoInput + '\u0022' + " -map_metadata -1" + MainWindow.trimCommand + "-vcodec copy -f segment -segment_time " + chunkLength + " -an " + '\u0022' + MainWindow.tempPath + "\\Chunks\\out%0d.mkv" + '\u0022';
                SmallFunctions.Logging("VideoSplitting() Command: " + ffmpegCommand);
                SmallFunctions.ExecuteFfmpegTask(ffmpegCommand);
            }
        }
    }
}
