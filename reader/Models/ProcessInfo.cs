namespace reader.Models;

public class ProcessInfo
{
    public int Pid { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;

    public DateTime? StartTime { get; set; }
    public TimeSpan? RunningDuration { get; set; }

    public float CpuUsagePercent { get; set; }
    public float MemoryUsageMB { get; set; }

    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }

    public string Priority { get; set; } = string.Empty;
    public int SessionId { get; set; }

    public int? ParentPid { get; set; }
    public string ParentProcessName { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public bool IsResponding { get; set; }
    public string Status { get; set; } = string.Empty;
}