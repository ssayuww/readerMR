namespace reader.Models;

public class CpuStaticInfo
{
    public string Name { get; set; } = "Unknown";
    public string Manufacturer { get; set; } = "Unknown";

    public int PhysicalCores { get; set; }
    public int LogicalCores { get; set; }

    public float BaseClockGHz { get; set; }
    public float MaxClockGHz { get; set; }

    public string Architecture { get; set; } = "Unknown";

    public string L2Cache { get; set; } = "Unknown";
    public string L3Cache { get; set; } = "Unknown";
}


public class CpuDynamicInfo
{
    public DateTime CurrentTime { get; set; }

    public float TotalCpuUsagePercent { get; set; }
    public List<float> PerCoreUsagePercent { get; set; } = new();

    public float CurrentClockGHz { get; set; }

    public float UserTimePercent { get; set; }
    public float PrivilegedTimePercent { get; set; }

    public float InterruptsPerSec { get; set; }
    public float ContextSwitchesPerSec { get; set; }
    public float ProcessorQueueLength { get; set; }

    public double LoadAverageLikeScore { get; set; }

    public float? CpuTemperatureCelsius { get; set; }
    public bool? IsThrottling { get; set; }
    public uint? CurrentPowerState { get; set; }

    public List<float?> PerCoreTemperatureCelsius { get; set; } = new();
}