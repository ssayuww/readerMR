namespace serverreader.Models;

public class StorageStaticInfo
{
    public List<DriveStaticInfo> Drives { get; set; } = new();
}

public class StorageDynamicInfo
{
    public List<DriveDynamicInfo> Drives { get; set; } = new();
}