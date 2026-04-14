namespace reader.Models;

public class GpuStaticInfo
{
    public string Name { get; set; } = "Unknown";
    public string Manufacturer { get; set; } = "Unknown";
    public string DriverVersion { get; set; } = "Unknown";

    public ulong DedicatedMemoryMB { get; set; }
    public ulong SharedMemoryMB { get; set; }
}

public class GpuEngineUsageInfo
{
    public string EngineName { get; set; } = string.Empty;
    public float UsagePercent { get; set; }
}

public class GpuDynamicInfo
{
    public DateTime CurrentTime { get; set; }

    public float TotalGpuUsagePercent { get; set; }
    public List<GpuEngineUsageInfo> EngineUsages { get; set; } = new();

    public float DedicatedMemoryUsedMB { get; set; }
    public float SharedMemoryUsedMB { get; set; }

    public float? TemperatureCelsius { get; set; }
    public int? FanSpeedRpm { get; set; }
    public float? CoreClockMHz { get; set; }

    public float? EncoderUsagePercent { get; set; }
    public float? DecoderUsagePercent { get; set; }

    public double? PowerDrawWatts { get; set; }
    public float? VramBandwidthPercent { get; set; }
}