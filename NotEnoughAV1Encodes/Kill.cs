using System;
using System.Diagnostics;
using System.Linq;

namespace NotEnoughAV1Encodes
{
    class Kill
    {
        public static void Kill_PID()
        {
            var temp_PID = Global.Launched_PIDs;
            // Iterates over all PIDs to kill them
            foreach (int pid in temp_PID)
            {
                try
                {
                    // Get the Process by ID
                    Process proc_to_kill = Process.GetProcessById(pid);
                    // Kills the Process
                    proc_to_kill.Kill();
                    // Remove PID from Array
                    Global.Launched_PIDs.RemoveAll(i => i == pid);
                }
                catch (Exception e)
                {
                    Helpers.Logging("Kill_PID(): " + e.Message);
                }
            }
        }
    }
}
