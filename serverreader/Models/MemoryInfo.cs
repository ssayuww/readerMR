namespace serverreader.Models;

public class MemoryStaticInfo
{
    public ulong TotalPhysicalMemoryMB { get; set; }

    public int ModuleCount { get; set; }

    public List<ulong> ModuleSizesMB { get; set; } = new();
    public List<int> ModuleSpeedsMHz { get; set; } = new();
    public List<string> ModuleTypes { get; set; } = new();

    public int? TotalSlots { get; set; }
}


public class MemoryDynamicInfo
{
    public DateTime CurrentTime { get; set; }

    public float UsedMemoryMB { get; set; }
    public float FreeMemoryMB { get; set; }
    public float TotalMemoryMB { get; set; }

    public float MemoryUsagePercent { get; set; }

    public float CachedMemoryMB { get; set; }

    public float CommittedMemoryMB { get; set; }
    public float CommitLimitMB { get; set; }

    public float PageFileUsagePercent { get; set; }

    public float PageFaultsPerSec { get; set; }

    public float MemoryPressure { get; set; } // heuristic
}