using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NotEnoughAV1Encodes
{
    internal class Kill
    {
        public static void Kill_PID()
        {
            List<int> temp_pids = Global.Launched_PIDs;

            // Nuke all PIDs
            foreach (int pid in temp_pids.ToList())
            {
                try
                {
                    List<int> children = Suspend.GetChildProcesses(pid);

                    Process proc_to_kill = Process.GetProcessById(pid);
                    proc_to_kill.Kill();

                    if (children != null)
                    {
                        foreach (int pid_children in children)
                        {
                            Process child_proc_to_kill = Process.GetProcessById(pid_children);
                            child_proc_to_kill.Kill();
                        }
                    }
                }
                catch { }
            }
        }
    }
}