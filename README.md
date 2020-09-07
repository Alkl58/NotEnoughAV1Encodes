# NotEnoughAV1Encodes (NEAV1E)

NEAV1E is a GUI for aomenc, rav1e, SVT-AV1 and VP9. 

It is a tool to make encoding easier and faster for AV1 encoders.

### How does this program work?:
This program will split the given video file into roughly equal chunks. After splitting, it will encode the chunks with n-amount of workers. 
When finished, it will [Concatenate](https://trac.ffmpeg.org/wiki/Concatenate) the encoded files together in one output file.

This tool is Windows only. For multiplatform and more features check out the CLI Tool [Av1an](https://github.com/master-of-zen/Av1an) or the GUI Tool [qencoder](https://github.com/natis1/qencoder).

![alt text](https://i.imgur.com/AbVetfn.png)
![alt text](https://i.imgur.com/SJsHBBc.png)

---

[![Build status](https://ci.appveyor.com/api/projects/status/f3wd2kr5i8eofj88/branch/master?svg=true)](https://ci.appveyor.com/project/Alkl/notenoughav1encodes/branch/master)

AppVeyor Builds: [Click](https://ci.appveyor.com/project/Alkl/notenoughav1encodes/branch/master/artifacts)

### Installation / Usage / Wiki: https://github.com/Alkl58/NotEnoughAV1Encodes/wiki

---

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
You can find me on the unofficial [AV1 Discord](https://discord.gg/HSBxne3)
