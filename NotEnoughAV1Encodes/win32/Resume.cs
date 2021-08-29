using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

namespace NotEnoughAV1Encodes
{
    internal class Suspend
    {
        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = 0x0001,
            SUSPEND_RESUME = 0x0002,
            GET_CONTEXT = 0x0008,
            SET_CONTEXT = 0x0010,
            SET_INFORMATION = 0x0020,
            QUERY_INFORMATION = 0x0040,
            SET_THREAD_TOKEN = 0x0080,
            IMPERSONATE = 0x0100,
            DIRECT_IMPERSONATION = 0x0200
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll")]
        private static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        public static List<int> GetChildProcesses(int process_id)
        {
            List<int> children = new();

            ManagementObjectSearcher mos = new(string.Format("Select * From Win32_Process Where ParentProcessID={0}", process_id));

            foreach (ManagementObject mo in mos.Get())
            {
                children.Add(Convert.ToInt32(mo["ProcessID"]));
            }

            return children;
        }

        public static void SuspendProcessTree(int pid)
        {
            List<int> children = GetChildProcesses(pid);

            // Pause cmd
            SuspendProcess(pid);

            // Pause subprocess
            foreach (int pid_children in children)
            {
                SuspendProcess(pid_children);
            }
        }

        public static void SuspendProcess(int pid)
        {
            var process = Process.GetProcessById(pid); // throws exception if process does not exist

            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                SuspendThread(pOpenThread);

                CloseHandle(pOpenThread);
            }
        }
    }
}