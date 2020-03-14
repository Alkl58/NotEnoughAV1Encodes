# NotEnoughAV1Encodes

NotEnoughAV1Encodes is a small GUI Handler for aomenc, rav1e and SVT-AV1 (AV1). 

NotEnoughAV1Encodes is a tool to make encoding easier and faster for AV1 encoders.

It splits the Source Video into multiple chunks and encode them parallel with the same given settings.
At the end it will [Concatenate](https://trac.ffmpeg.org/wiki/Concatenate) the chunks into a single video.

This tool is Windows only. For multiplatform and more features check out the CLI Tool [Av1an](https://github.com/master-of-zen/Av1an).

![alt text](https://i.imgur.com/f2Ofk81.png)
![alt text](https://i.imgur.com/phzfsxW.png)

---

[![Build status](https://ci.appveyor.com/api/projects/status/f3wd2kr5i8eofj88/branch/master?svg=true)](https://ci.appveyor.com/project/Alkl/notenoughav1encodes/branch/master)


### Installation:

1. Download [ffmpeg and ffprobe](https://www.ffmpeg.org/download.html), [aomenc](https://ci.appveyor.com/project/marcomsousa/build-aom/history) or [rav1e](https://github.com/xiph/rav1e) or [SVT-AV1](https://github.com/OpenVisualCloud/SVT-AV1) and [NotEnoughAV1Encodes](https://github.com/Alkl58/NotEnoughAV1Encodes/releases). 
2. Create a new Folder and put all .exe files in them (ffmpeg, ffprobe, aomenc/rav1e/SVT-AV1 and NotEnoughAV1Encodes.exe - you can also specify the location of these files under Settings!)

### System Requirements:
- [Microsoft .NET Framework 4.7.2](https://dotnet.microsoft.com/download/dotnet-framework/net472)
- [Microsoft Visual C++ Redistributable x64](https://support.microsoft.com/en-us/help/2977003/the-latest-supported-visual-c-downloads)

### Usage:
1. Open NotEnoughAV1Encodes.exe
2. Select your video file / Set video output file
3. Select the Chunk length in seconds. (The Chunks won't be exactly this long. ffmpeg will cut it at the nearest Key-Frame!)
4. Edit the Encoding settings. (You can save the settings by clicking on "Save Settings". Saving/Loading multiple Presets is possible!)
5. Click on "Start Encode".  

NEAV1E has a resume feature, with which you can resume cancled encodes. (unfinished chunks will be overwritten!)

If you press on cancel, the program will terminate ALL aomenc/rav1e/SVT-AV1 and ffmpeg processes. Don't press it if you have other encodes/instances running!

SVT-AV1 encoding is limited to one instance. NEAV1E is using a pipe to encode with SVT-AV1.

If you experience framelosses you may have a slightly corrupted video. Using the option "Reencode Lossless" might help with this issue. (Video will be encoded to utvideo - this may take alot of disk space)

---
### How does this program work?:
This programm will split the given video file into roughly equally chunks. This splitting process is not reencoding the video.
After splitting it will encode the chunks with x amount of workers. When finished, it will mux the encoded files together in one .mkv file.

### Disclaimer:
- This project is based on my first project [NEE](https://github.com/Alkl58/NotEnoughEncodes)

