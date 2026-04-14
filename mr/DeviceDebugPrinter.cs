
namespace reader
{
    
    using UnityEngine;

    public static class DeviceDebugPrinter
    {
        public static void Print(DeviceSnapshot d)
        {
            if (d == null)
            {
                Debug.Log("Device is NULL");
                return;
            }

            //Debug.Log("========== DEVICE ==========");
            //Debug.Log($"ID: {d.deviceId}");
            //Debug.Log($"Name: {d.displayName}");

            //PrintSystem(d);
            //PrintCPU(d);
            //PrintMemory(d);
            //PrintGPU(d);
            //PrintStorage(d);
            //PrintNetwork(d);
            //PrintProcesses(d);

            //Debug.Log("========== END ==========\n");
        }

        private static void PrintSystem(DeviceSnapshot d)
        {
            var s = d.staticSystemInfo;
            var dyn = d.dynamicSystemInfo;

           // Debug.Log("---- SYSTEM ----");
            //Debug.Log($"Machine: {s.machineName}");
           // Debug.Log($"User: {s.userName}");
           // Debug.Log($"OS: {s.osName}");
           // Debug.Log($"Uptime: {dyn.uptime}");
        }

        private static void PrintCPU(DeviceSnapshot d)
        {
            var s = d.cpuStaticInfo;
            var dyn = d.cpuDynamicInfo;

            //Debug.Log("---- CPU ----");
            //Debug.Log($"{s.name} ({s.logicalCores} threads)");
           // Debug.Log($"Usage: {dyn.totalCpuUsagePercent:F1}%");
            //Debug.Log($"Clock: {dyn.currentClockGHz:F2} GHz");
        }

        private static void PrintMemory(DeviceSnapshot d)
        {
            var dyn = d.memoryDynamicInfo;

            //Debug.Log("---- MEMORY ----");
           // Debug.Log($"Usage: {dyn.memoryUsagePercent:F1}%");
            //Debug.Log($"Used: {dyn.usedMemoryMB:F0} MB");
            //Debug.Log($"Free: {dyn.freeMemoryMB:F0} MB");
        }

        private static void PrintGPU(DeviceSnapshot d)
        {
            var s = d.gpuStaticInfo;
            var dyn = d.gpuDynamicInfo;

            //Debug.Log("---- GPU ----");
           // Debug.Log($"{s.name}");
            //Debug.Log($"Usage: {dyn.totalGpuUsagePercent:F1}%");

            if (dyn.engineUsages != null)
            {
                foreach (var e in dyn.engineUsages)
                {
                   // Debug.Log($"  {e.engineName}: {e.usagePercent:F1}%");
                }
            }
        }

        private static void PrintStorage(DeviceSnapshot d)
        {
            //Debug.Log("---- STORAGE ----");

            foreach (var drive in d.storageStaticInfo.drives)
            {
                //Debug.Log(
                    //$"{drive.driveLetter} | {drive.usedPercent:F1}% used | " +
                    //$"{drive.usedSpaceGB:F0}/{drive.totalSizeGB:F0} GB"
                //);
            }
        }

        private static void PrintNetwork(DeviceSnapshot d)
        {
            var dyn = d.networkDynamicInfo;

           // Debug.Log("---- NETWORK ----");
            //Debug.Log($"Download: {dyn.downloadSpeedMbps:F2} Mbps");
            //Debug.Log($"Upload: {dyn.uploadSpeedMbps:F2} Mbps");
        }

        private static void PrintProcesses(DeviceSnapshot d)
        {
           // Debug.Log("---- PROCESSES (TOP 5) ----");

            int count = Mathf.Min(5, d.processes.Count);

            for (int i = 0; i < count; i++)
            {
                var p = d.processes[i];

               // Debug.Log(
//$"{p.processName} | CPU {p.cpuUsagePercent:F1}% | RAM {p.memoryUsageMB:F0}MB"
                //);
            }
        }
    }
}