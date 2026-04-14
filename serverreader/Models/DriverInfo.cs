namespace serverreader.Models;

public class DriveStaticInfo
{
    public string DriveLetter { get; set; } = string.Empty;
    public string MountPoint { get; set; } = string.Empty;
    public string FileSystem { get; set; } = string.Empty;

    public double TotalSizeGB { get; set; }
    public double FreeSpaceGB { get; set; }
    public double UsedSpaceGB { get; set; }
    public double UsedPercent { get; set; }

    public string DriveType { get; set; } = string.Empty;
    public string VolumeName { get; set; } = string.Empty;
}


public class DriveDynamicInfo
{
    public string DriveLetter { get; set; } = string.Empty;
    public DateTime CurrentTime { get; set; }

    public float ReadMBPerSec { get; set; }
    public float WriteMBPerSec { get; set; }

    public float ReadIOPS { get; set; }
    public float WriteIOPS { get; set; }
    public float TotalIOPS { get; set; }

    public float ActiveTimePercent { get; set; }
    public float QueueLength { get; set; }
    public float AvgResponseTimeMs { get; set; }
}