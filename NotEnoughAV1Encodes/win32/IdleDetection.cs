using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace NotEnoughAV1Encodes.win32
{
    internal class IdleDetection
    {
        public static TimeSpan GetInputIdleTime()
        {
            var plii = new NativeMethods.LastInputInfo();
            plii.cbSize = (UInt32)Marshal.SizeOf(plii);

            if (NativeMethods.GetLastInputInfo(ref plii))
            {
                return TimeSpan.FromMilliseconds(Environment.TickCount64 - plii.dwTime);
            }
            else
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        private static class NativeMethods
        {
            public struct LastInputInfo
            {
                public UInt32 cbSize;
                public UInt32 dwTime;
            }

            [DllImport("user32.dll")]
            public static extern bool GetLastInputInfo(ref LastInputInfo plii);
        }
    }
}
