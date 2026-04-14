namespace serverreader.Models;

public class DeviceSnapshot
{
    public string DeviceId { get; set; }
    public string DisplayName { get; set; }
    public StaticSystemInfo StaticSystemInfo { get; set; }
    public DynamicSystemInfo DynamicSystemInfo { get; set; }
    
    public CpuStaticInfo CpuStaticInfo { get; set; } = new();
    public CpuDynamicInfo CpuDynamicInfo { get; set; } = new();
    
    public MemoryStaticInfo MemoryStaticInfo { get; set; } = new();
    public MemoryDynamicInfo MemoryDynamicInfo { get; set; } = new();
    
    public StorageStaticInfo StorageStaticInfo { get; set; } = new();
    public StorageDynamicInfo StorageDynamicInfo { get; set; } = new();
    
    public NetworkStaticInfo NetworkStaticInfo { get; set; } = new();
    public NetworkDynamicInfo NetworkDynamicInfo { get; set; } = new();
    public NetworkConnectionInfo NetworkConnectionInfo { get; set; } = new();
    public NetworkAdvancedInfo NetworkAdvancedInfo { get; set; } = new();
    
    public GpuStaticInfo GpuStaticInfo { get; set; } = new();
    public GpuDynamicInfo GpuDynamicInfo { get; set; } = new();
    
    public List<ProcessInfo> Processes { get; set; } = new();
    public List<ProcessIconInfo> ProcessIcons { get; set; } = new();
    

}