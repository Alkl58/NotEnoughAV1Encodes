namespace NotEnoughAV1Encodes
{
    class SmallFunctions
    {
        public static int getCoreCount()
        {
            // Gets Core Count
            int coreCount = 0;
            foreach (var item in new System.Management.ManagementObjectSearcher("Select * from Win32_Processor").Get())
            { 
                coreCount += int.Parse(item["NumberOfCores"].ToString()); 
            }
            return coreCount;
        }
    }
}
