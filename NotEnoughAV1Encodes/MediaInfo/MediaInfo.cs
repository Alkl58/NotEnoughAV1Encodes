using System;
using System.Runtime.InteropServices;

namespace NotEnoughAV1Encodes
{
    public enum StreamKind
    {
        General,
        Video,
        Audio,
        Text,
        Chapters,
        Image
    }

    public enum InfoKind
    {
        Name,
        Text,
        Measure,
        Options,
        NameText,
        MeasureText,
        Info,
        HowTo
    }

    public enum InfoOptions
    {
        ShowInInform,
        Support,
        ShowInSupported,
        TypeOfValue
    }

    public enum InfoFileOptions
    {
        FileOption_Nothing = 0x00,
        FileOption_NoRecursive = 0x01,
        FileOption_CloseAll = 0x02,
        FileOption_Max = 0x04
    };

    public class MediaInfo
    {
        //Import of DLL functions. DO NOT USE until you know what you do (MediaInfo DLL do NOT use CoTaskMemAlloc to allocate memory)
        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern IntPtr MediaInfo_New();

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern void MediaInfo_Delete(IntPtr Handle);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern IntPtr MediaInfo_Open(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string FileName);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern IntPtr MediaInfoA_Open(IntPtr Handle, IntPtr FileName);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern IntPtr MediaInfo_Open_Buffer_Init(IntPtr Handle, Int64 File_Size, Int64 File_Offset);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern IntPtr MediaInfoA_Open(IntPtr Handle, Int64 File_Size, Int64 File_Offset);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern IntPtr MediaInfo_Open_Buffer_Continue(IntPtr Handle, IntPtr Buffer, IntPtr Buffer_Size);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern IntPtr MediaInfoA_Open_Buffer_Continue(IntPtr Handle, Int64 File_Size, byte[] Buffer, IntPtr Buffer_Size);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern Int64 MediaInfo_Open_Buffer_Continue_GoTo_Get(IntPtr Handle);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern Int64 MediaInfoA_Open_Buffer_Continue_GoTo_Get(IntPtr Handle);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern IntPtr MediaInfo_Open_Buffer_Finalize(IntPtr Handle);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern IntPtr MediaInfoA_Open_Buffer_Finalize(IntPtr Handle);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern void MediaInfo_Close(IntPtr Handle);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern IntPtr MediaInfo_Inform(IntPtr Handle, IntPtr Reserved);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern IntPtr MediaInfoA_Inform(IntPtr Handle, IntPtr Reserved);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern IntPtr MediaInfo_GetI(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber, IntPtr Parameter, IntPtr KindOfInfo);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern IntPtr MediaInfoA_GetI(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber, IntPtr Parameter, IntPtr KindOfInfo);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern IntPtr MediaInfo_Get(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber, [MarshalAs(UnmanagedType.LPWStr)] string Parameter, IntPtr KindOfInfo, IntPtr KindOfSearch);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern IntPtr MediaInfoA_Get(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber, IntPtr Parameter, IntPtr KindOfInfo, IntPtr KindOfSearch);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern IntPtr MediaInfo_Option(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string Option, [MarshalAs(UnmanagedType.LPWStr)] string Value);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern IntPtr MediaInfoA_Option(IntPtr Handle, IntPtr Option, IntPtr Value);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern IntPtr MediaInfo_State_Get(IntPtr Handle);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern IntPtr MediaInfo_Count_Get(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber);

        //MediaInfo class
        public MediaInfo()
        {
            Handle = MediaInfo_New();
            MustUseAnsi = !Environment.OSVersion.ToString().Contains("Windows", StringComparison.CurrentCulture);
        }

        ~MediaInfo()
        {
            MediaInfo_Delete(Handle);
        }

        public int Open(string FileName)
        {
            if (MustUseAnsi)
            {
                IntPtr FileName_Ptr = Marshal.StringToHGlobalAnsi(FileName);
                int ToReturn = (int)MediaInfoA_Open(Handle, FileName_Ptr);
                Marshal.FreeHGlobal(FileName_Ptr);
                return ToReturn;
            }
            else
                return (int)MediaInfo_Open(Handle, FileName);
        }

        public int Open_Buffer_Init(long File_Size, long File_Offset)
        {
            return (int)MediaInfo_Open_Buffer_Init(Handle, File_Size, File_Offset);
        }

        public int Open_Buffer_Continue(IntPtr Buffer, IntPtr Buffer_Size)
        {
            return (int)MediaInfo_Open_Buffer_Continue(Handle, Buffer, Buffer_Size);
        }

        public long Open_Buffer_Continue_GoTo_Get()
        {
            return (int)MediaInfo_Open_Buffer_Continue_GoTo_Get(Handle);
        }

        public int Open_Buffer_Finalize()
        {
            return (int)MediaInfo_Open_Buffer_Finalize(Handle);
        }

        public void Close()
        {
            MediaInfo_Close(Handle);
        }

        public string Inform()
        {
            return MustUseAnsi
                ? Marshal.PtrToStringAnsi(MediaInfoA_Inform(Handle, (IntPtr)0))
                : Marshal.PtrToStringUni(MediaInfo_Inform(Handle, (IntPtr)0));
        }

        public string Get(StreamKind StreamKind, int StreamNumber, String Parameter, InfoKind KindOfInfo, InfoKind KindOfSearch)
        {
            if (MustUseAnsi)
            {
                IntPtr Parameter_Ptr = Marshal.StringToHGlobalAnsi(Parameter);
                string ToReturn = Marshal.PtrToStringAnsi(MediaInfoA_Get(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber, Parameter_Ptr, (IntPtr)KindOfInfo, (IntPtr)KindOfSearch));
                Marshal.FreeHGlobal(Parameter_Ptr);
                return ToReturn;
            }
            else
            {
                return Marshal.PtrToStringUni(MediaInfo_Get(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber, Parameter, (IntPtr)KindOfInfo, (IntPtr)KindOfSearch));
            }
        }

        public string Get(StreamKind StreamKind, int StreamNumber, int Parameter, InfoKind KindOfInfo)
        {
            return MustUseAnsi
                ? Marshal.PtrToStringAnsi(MediaInfoA_GetI(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber, (IntPtr)Parameter, (IntPtr)KindOfInfo))
                : Marshal.PtrToStringUni(MediaInfo_GetI(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber, (IntPtr)Parameter, (IntPtr)KindOfInfo));
        }

        public string Option(string Option, string Value)
        {
            if (MustUseAnsi)
            {
                IntPtr Option_Ptr = Marshal.StringToHGlobalAnsi(Option);
                IntPtr Value_Ptr = Marshal.StringToHGlobalAnsi(Value);
                string ToReturn = Marshal.PtrToStringAnsi(MediaInfoA_Option(Handle, Option_Ptr, Value_Ptr));
                Marshal.FreeHGlobal(Option_Ptr);
                Marshal.FreeHGlobal(Value_Ptr);
                return ToReturn;
            }
            else
            {
                return Marshal.PtrToStringUni(MediaInfo_Option(Handle, Option, Value));
            }
        }

        public int State_Get()
        {
            return (int)MediaInfo_State_Get(Handle);
        }

        public int Count_Get(StreamKind StreamKind, int StreamNumber)
        {
            return (int)MediaInfo_Count_Get(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber);
        }

        private readonly IntPtr Handle;
        private readonly bool MustUseAnsi;

        //Default values, if you know how to set default values in C#, say me
        public string Get(StreamKind StreamKind, int StreamNumber, string Parameter, InfoKind KindOfInfo) { return Get(StreamKind, StreamNumber, Parameter, KindOfInfo, InfoKind.Name); }

        public string Get(StreamKind StreamKind, int StreamNumber, string Parameter)
        {
            return Get(StreamKind, StreamNumber, Parameter, InfoKind.Text, InfoKind.Name);
        }

        public string Get(StreamKind StreamKind, int StreamNumber, int Parameter)
        {
            return Get(StreamKind, StreamNumber, Parameter, InfoKind.Text);
        }

        public string Option(string Option_)
        {
            return Option(Option_, "");
        }

        public int Count_Get(StreamKind StreamKind)
        {
            return Count_Get(StreamKind, -1);
        }
    }

    public class MediaInfoList
    {
        //Import of DLL functions. DO NOT USE until you know what you do (MediaInfo DLL do NOT use CoTaskMemAlloc to allocate memory)
        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern IntPtr MediaInfoList_New();

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern void MediaInfoList_Delete(IntPtr Handle);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern IntPtr MediaInfoList_Open(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string FileName, IntPtr Options);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern void MediaInfoList_Close(IntPtr Handle, IntPtr FilePos);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern IntPtr MediaInfoList_Inform(IntPtr Handle, IntPtr FilePos, IntPtr Reserved);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern IntPtr MediaInfoList_GetI(IntPtr Handle, IntPtr FilePos, IntPtr StreamKind, IntPtr StreamNumber, IntPtr Parameter, IntPtr KindOfInfo);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern IntPtr MediaInfoList_Get(IntPtr Handle, IntPtr FilePos, IntPtr StreamKind, IntPtr StreamNumber, [MarshalAs(UnmanagedType.LPWStr)] string Parameter, IntPtr KindOfInfo, IntPtr KindOfSearch);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern IntPtr MediaInfoList_Option(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string Option, [MarshalAs(UnmanagedType.LPWStr)] string Value);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern IntPtr MediaInfoList_State_Get(IntPtr Handle);

        [DllImport("MediaInfo/MediaInfo.dll")]
        private static extern IntPtr MediaInfoList_Count_Get(IntPtr Handle, IntPtr FilePos, IntPtr StreamKind, IntPtr StreamNumber);

        //MediaInfo class
        public MediaInfoList() { Handle = MediaInfoList_New(); }

        ~MediaInfoList()
        {
            MediaInfoList_Delete(Handle);
        }

        public int Open(string FileName, InfoFileOptions Options)
        {
            return (int)MediaInfoList_Open(Handle, FileName, (IntPtr)Options);
        }

        public void Close(int FilePos)
        {
            MediaInfoList_Close(Handle, (IntPtr)FilePos);
        }

        public string Inform(int FilePos)
        {
            return Marshal.PtrToStringUni(MediaInfoList_Inform(Handle, (IntPtr)FilePos, (IntPtr)0));
        }

        public string Get(int FilePos, StreamKind StreamKind, int StreamNumber, string Parameter, InfoKind KindOfInfo, InfoKind KindOfSearch)
        {
            return Marshal.PtrToStringUni(MediaInfoList_Get(Handle, (IntPtr)FilePos, (IntPtr)StreamKind, (IntPtr)StreamNumber, Parameter, (IntPtr)KindOfInfo, (IntPtr)KindOfSearch));
        }

        public string Get(int FilePos, StreamKind StreamKind, int StreamNumber, int Parameter, InfoKind KindOfInfo)
        {
            return Marshal.PtrToStringUni(MediaInfoList_GetI(Handle, (IntPtr)FilePos, (IntPtr)StreamKind, (IntPtr)StreamNumber, (IntPtr)Parameter, (IntPtr)KindOfInfo));
        }

        public string Option(string Option, string Value)
        {
            return Marshal.PtrToStringUni(MediaInfoList_Option(Handle, Option, Value));
        }

        public int State_Get()
        {
            return (int)MediaInfoList_State_Get(Handle);
        }

        public int Count_Get(int FilePos, StreamKind StreamKind, int StreamNumber)
        {
            return (int)MediaInfoList_Count_Get(Handle, (IntPtr)FilePos, (IntPtr)StreamKind, (IntPtr)StreamNumber);
        }

        private readonly IntPtr Handle;

        //Default values, if you know how to set default values in C#, say me
        public void Open(string FileName) { Open(FileName, 0); }

        public void Close()
        {
            Close(-1);
        }

        public string Get(int FilePos, StreamKind StreamKind, int StreamNumber, string Parameter, InfoKind KindOfInfo)
        {
            return Get(FilePos, StreamKind, StreamNumber, Parameter, KindOfInfo, InfoKind.Name);
        }

        public string Get(int FilePos, StreamKind StreamKind, int StreamNumber, string Parameter)
        {
            return Get(FilePos, StreamKind, StreamNumber, Parameter, InfoKind.Text, InfoKind.Name);
        }

        public string Get(int FilePos, StreamKind StreamKind, int StreamNumber, int Parameter)
        {
            return Get(FilePos, StreamKind, StreamNumber, Parameter, InfoKind.Text);
        }

        public string Option(string Option_)
        {
            return Option(Option_, "");
        }

        public int Count_Get(int FilePos, StreamKind StreamKind)
        {
            return Count_Get(FilePos, StreamKind, -1);
        }
    }
}