# NotEnoughAV1Encodes (NEAV1E)

NotEnoughAV1Encodes is a small GUI Handler for aomenc, rav1e and SVT-AV1 (AV1).

NEAV1E is a tool to make encoding easier and faster for AV1 encoders.

It splits the Source Video into multiple chunks and encodes them parallel with the same given settings.
At the end it will [Concatenate](https://trac.ffmpeg.org/wiki/Concatenate) the chunks into a single video.

This tool is Windows only. For multiplatform and more features check out the CLI Tool [Av1an](https://github.com/master-of-zen/Av1an).

![alt text](https://i.imgur.com/AbVetfn.png)
![alt text](https://i.imgur.com/SJsHBBc.png)

---

[![Build status](https://ci.appveyor.com/api/projects/status/f3wd2kr5i8eofj88/branch/master?svg=true)](https://ci.appveyor.com/project/Alkl/notenoughav1encodes/branch/master)

AppVeyor Builds: [Click](https://ci.appveyor.com/project/Alkl/notenoughav1encodes/branch/master/artifacts)

### Installation:

1. Download [NotEnoughAV1Encodes](https://github.com/Alkl58/NotEnoughAV1Encodes/releases).
2. Extract the archive to your desired location.

### System Requirements:
- [Microsoft .NET Framework 4.7.2](https://dotnet.microsoft.com/download/dotnet-framework/net472)
- [Microsoft Visual C++ Redistributable x64](https://support.microsoft.com/en-us/help/2977003/the-latest-supported-visual-c-downloads)

### Usage:
1. Open NotEnoughAV1Encodes.exe
2. Select your video file / Set video output file
3. Select the Chunk length in seconds.
4. Edit the encoding settings. (You can save the settings by clicking on "Save Settings". Saving/Loading multiple presets is possible!)
5. Click on "Start Encode".

NEAV1E has a resume feature, with which you can resume cancelled encodes. (unfinished chunks will be overwritten!)

If you press on cancel, the program will terminate ALL aomenc/rav1e/SVT-AV1 and ffmpeg processes. Don't press it if you have other encodes/instances running!

SVT-AV1 encoding is limited to one instance. This limitation can be overwritten in the program settings.

If you experience framelosses you may have a slightly corrupted video. Using the option "Reencode Lossless" might help with this issue. (Video will be reencoded - this may take a lot of disk space)

---
### How does this program work?:
This program will split the given video file into roughly equally chunks. This splitting process is not reencoding the video, except specified in the settings.
After splitting, it will encode the chunks with x amount of workers. When finished, it will mux the encoded files together in one output file.

### Submit a Bugreport:
I need lots of information in order to recreate Bugs.
Please put the following information in your bug report:
- OS (e.g. Win 10 1909)
- GUI Build Version (can be found under Program Settings)
- Encoder you are using (e.g. aomenc)
- Encoding Settings
- Audio Settings if used
- General Video Information (Framerate / Resolution - [MediaInfo](https://mediaarea.net/de/MediaInfo) Screenshot would be nice)
- The exact steps you made which caused the problem!

##### Contacting me:
You can find me on the inofficial [AV1 Discord](https://discord.gg/HSBxne3)
