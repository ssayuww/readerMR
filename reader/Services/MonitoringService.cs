using Microsoft.Extensions.Configuration;
using reader.Models;
using reader.Readers;

namespace reader.Services;

public class MonitoringService
{
    private readonly System.Windows.Forms.Timer _timer;
    private readonly SnapshotSender _snapshotSender;

    private readonly string _deviceId;
    private readonly string _displayName;

    private StaticSystemInfo _staticInfo;
    private CpuStaticInfo _cpuStaticInfo = new();
    private MemoryStaticInfo _memoryStaticInfo = new();
    private StorageStaticInfo _storageStaticInfo = new();
    private NetworkStaticInfo _networkStaticInfo = new();
    private GpuStaticInfo _gpuStaticInfo = new();
    
    public List<ProcessInfo> Processes { get; set; } = new();
    public List<ProcessIconInfo> _processIcons { get; set; } = new();
    
    
    

    public bool IsRunning { get; private set; }

    public MonitoringService()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var serverSettings = configuration.GetSection("Server").Get<ServerSettings>()
                             ?? throw new InvalidOperationException("Server settings are missing.");

        var deviceSettings = configuration.GetSection("Device").Get<DeviceSettings>()
                             ?? throw new InvalidOperationException("Device settings are missing.");

        if (string.IsNullOrWhiteSpace(deviceSettings.DeviceId))
            throw new InvalidOperationException("Device:DeviceId is missing.");

        if (string.IsNullOrWhiteSpace(deviceSettings.DisplayName))
            throw new InvalidOperationException("Device:DisplayName is missing.");

        _deviceId = deviceSettings.DeviceId;
        _displayName = deviceSettings.DisplayName;

        _snapshotSender = new SnapshotSender(serverSettings.BaseUrl);

        _timer = new System.Windows.Forms.Timer();
        _timer.Interval = 3000;
        _timer.Tick += OnTick;
    }

    public void Start()
    {
        if (IsRunning)
            return;

        _staticInfo = SystemInfoReader.ReadStatic();
        _cpuStaticInfo = CpuReader.ReadStatic();
        CpuReader.Prime();
        _memoryStaticInfo = MemoryReader.ReadStatic();
        MemoryReader.Prime();
        _storageStaticInfo = StorageReader.ReadStatic();
        StorageReader.Prime();
        _networkStaticInfo = NetworkReader.ReadStatic();
        NetworkReader.Prime();
        _gpuStaticInfo = GpuReader.ReadStatic();
        GpuReader.Prime();
        _processIcons = ProcessIconReader.Read(30);

        Console.WriteLine("=== READER STARTED ===");
        Console.WriteLine($"DeviceId: {_deviceId}");
        Console.WriteLine($"DisplayName: {_displayName}");
        Console.WriteLine();

        PrintStatic(_staticInfo);

        _timer.Start();
        IsRunning = true;
    }

    public void Stop()
    {
        if (!IsRunning)
            return;

        _timer.Stop();
        IsRunning = false;
    }

    private async void OnTick(object? sender, EventArgs e)
{
    try
    {
        Console.WriteLine("Tick started");

        var dynamicInfo = SystemInfoReader.ReadDynamic(_staticInfo.BootTime);
        var cpuDynamicInfo = CpuReader.ReadDynamic();
        var memoryDynamic = MemoryReader.ReadDynamic();
        var storageDynamicInfo = StorageReader.ReadDynamic();
        var networkDynamicInfo = NetworkReader.ReadDynamic();
        var networkConnectionInfo = NetworkReader.ReadConnections();
        var networkAdvancedInfo = await NetworkReader.ReadAdvancedAsync();
        var gpuDynamicInfo = GpuReader.ReadDynamic();

        Console.WriteLine("Before ProcessReader");
        var processes = ProcessReader.Read(40);
        Console.WriteLine($"Processes read: {processes.Count}");

        PrintDynamic(dynamicInfo);

        Console.WriteLine("Before snapshot creation");
        var snapshot = new DeviceSnapshot
        {
            DeviceId = _deviceId,
            DisplayName = _displayName,
            StaticSystemInfo = _staticInfo,
            DynamicSystemInfo = dynamicInfo,
            CpuStaticInfo = _cpuStaticInfo,
            CpuDynamicInfo = cpuDynamicInfo,
            MemoryStaticInfo = _memoryStaticInfo,
            MemoryDynamicInfo = memoryDynamic,
            StorageStaticInfo = _storageStaticInfo,
            StorageDynamicInfo = storageDynamicInfo,
            NetworkStaticInfo = _networkStaticInfo,
            NetworkDynamicInfo = networkDynamicInfo,
            NetworkConnectionInfo = networkConnectionInfo,
            NetworkAdvancedInfo = networkAdvancedInfo,
            GpuStaticInfo = _gpuStaticInfo,
            GpuDynamicInfo = gpuDynamicInfo,
            Processes = processes,
            ProcessIcons = _processIcons
        };

        Console.WriteLine("Before SendAsync");
        await _snapshotSender.SendAsync(snapshot);
        Console.WriteLine("Snapshot sent successfully.");
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine("OnTick failed:");
        Console.WriteLine(ex.ToString());
        Console.WriteLine();
    }
}

    private void PrintStatic(StaticSystemInfo info)
    {
        Console.WriteLine("=== STATIC SYSTEM INFO ===");
        Console.WriteLine($"Machine: {info.MachineName}");
        Console.WriteLine($"User: {info.UserName}");
        Console.WriteLine($"OS: {info.OSName}");
        Console.WriteLine($"Version: {info.OSVersion}");
        Console.WriteLine($"Architecture: {info.Architecture}");
        Console.WriteLine($"Manufacturer: {info.Manufacturer}");
        Console.WriteLine($"Model: {info.Model}");
        Console.WriteLine($"BIOS: {info.BiosVersion}");
        Console.WriteLine($"Motherboard: {info.Motherboard}");
        Console.WriteLine($"Serial: {info.Serial}");
        Console.WriteLine($"Boot Time: {info.BootTime}");
        Console.WriteLine($"Timezone: {info.TimeZone}");
        Console.WriteLine($"Locale: {info.Locale}");
        Console.WriteLine($"Hostname: {info.Hostname}");
        Console.WriteLine($"Domain: {info.Domain}");
        Console.WriteLine();
    }

    private void PrintDynamic(DynamicSystemInfo info)
    {
        Console.WriteLine("=== DYNAMIC SYSTEM INFO ===");
        Console.WriteLine($"Current Time: {info.CurrentTime}");
        Console.WriteLine($"Uptime: {info.Uptime}");
    }
}