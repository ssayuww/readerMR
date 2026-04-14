namespace reader
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    [Serializable]
    public class DeviceSnapshot
    {
        public string deviceId;
        public string displayName;

        public StaticSystemInfo staticSystemInfo;
        public DynamicSystemInfo dynamicSystemInfo;

        public CpuStaticInfo cpuStaticInfo;
        public CpuDynamicInfo cpuDynamicInfo;

        public MemoryStaticInfo memoryStaticInfo;
        public MemoryDynamicInfo memoryDynamicInfo;

        public StorageStaticInfo storageStaticInfo;
        public StorageDynamicInfo storageDynamicInfo;

        public NetworkStaticInfo networkStaticInfo;
        public NetworkDynamicInfo networkDynamicInfo;
        public NetworkConnectionInfo networkConnectionInfo;
        public NetworkAdvancedInfo networkAdvancedInfo;

        public GpuStaticInfo gpuStaticInfo;
        public GpuDynamicInfo gpuDynamicInfo;

        public List<ProcessInfo> processes;
        public List<ProcessIconInfo> processIcons;
    }

    [Serializable]
    public class StaticSystemInfo
    {
        public string machineName;
        public string userName;
        public string osName;
        public string osVersion;
        public string architecture;
        public string manufacturer;
        public string model;
        public string biosVersion;
        public string motherboard;
        public string serial;
        public string bootTime;
        public string timeZone;
        public string locale;
        public string hostname;
        public string domain;
    }

    [Serializable]
    public class DynamicSystemInfo
    {
        public string currentTime;
        public string uptime;
    }

    [Serializable]
    public class CpuStaticInfo
    {
        public string name;
        public string manufacturer;
        public int physicalCores;
        public int logicalCores;
        public float baseClockGHz;
        public float maxClockGHz;
        public string architecture;
        public string l2Cache;
        public string l3Cache;
    }

    [Serializable]
    public class CpuDynamicInfo
    {
        public string currentTime;
        public float totalCpuUsagePercent;
        public List<float> perCoreUsagePercent;
        public float currentClockGHz;
        public float userTimePercent;
        public float privilegedTimePercent;
        public float interruptsPerSec;
        public float contextSwitchesPerSec;
        public float processorQueueLength;
        public float loadAverageLikeScore;
        public float? cpuTemperatureCelsius;
        public bool? isThrottling;
        public uint? currentPowerState;
        public List<float?> perCoreTemperatureCelsius;
    }

    [Serializable]
    public class MemoryStaticInfo
    {
        public int totalPhysicalMemoryMB;
        public int moduleCount;
        public List<int> moduleSizesMB;
        public List<int> moduleSpeedsMHz;
        public List<string> moduleTypes;
        public int totalSlots;
    }

    [Serializable]
    public class MemoryDynamicInfo
    {
        public string currentTime;
        public float usedMemoryMB;
        public float freeMemoryMB;
        public float totalMemoryMB;
        public float memoryUsagePercent;
        public float cachedMemoryMB;
        public float committedMemoryMB;
        public float commitLimitMB;
        public float pageFileUsagePercent;
        public float pageFaultsPerSec;
        public float memoryPressure;
    }

    [Serializable]
    public class StorageStaticInfo
    {
        public List<DriveStaticInfo> drives;
    }

    [Serializable]
    public class DriveStaticInfo
    {
        public string driveLetter;
        public string mountPoint;
        public string fileSystem;
        public double totalSizeGB;
        public double freeSpaceGB;
        public double usedSpaceGB;
        public double usedPercent;
        public string driveType;
        public string volumeName;
    }

    [Serializable]
    public class StorageDynamicInfo
    {
        public List<DriveDynamicInfo> drives;
    }

    [Serializable]
    public class DriveDynamicInfo
    {
        public string driveLetter;
        public string currentTime;
        public float readMBPerSec;
        public float writeMBPerSec;
        public float readIOPS;
        public float writeIOPS;
        public float totalIOPS;
        public float activeTimePercent;
        public float queueLength;
        public float avgResponseTimeMs;
    }

    [Serializable]
    public class NetworkStaticInfo
    {
        public bool isInternetConnected;
        public string interfaceName;
        public string interfaceType;
        public string macAddress;
        public string localIPv4;

        [JsonProperty("iPv6")] public string ipv6;

        public string subnetMask;
        public string gateway;
        public List<string> dnsServers;
        public long linkSpeedMbps;
        public string ssid;
        public int? wifiSignalStrengthPercent;
    }

    [Serializable]
    public class NetworkDynamicInfo
    {
        public string currentTime;
        public long bytesSentTotal;
        public long bytesReceivedTotal;
        public double uploadSpeedMbps;
        public double downloadSpeedMbps;
        public long packetsSent;
        public long packetsReceived;
        public long incomingPacketsDiscarded;
        public long outgoingPacketsDiscarded;
        public long incomingPacketsErrors;
        public long outgoingPacketsErrors;
    }

    [Serializable]
    public class NetworkConnectionInfo
    {
        public int openTcpConnections;
        public int establishedConnections;
        public int listeningPorts;
        public List<string> topRemoteEndpoints;
        public List<object> topProcessesByBandwidth;
    }

    [Serializable]
    public class NetworkAdvancedInfo
    {
        public double? pingLatencyMs;
        public double? jitterMs;
        public double? dnsLookupMs;
        public string publicIp;
        public bool? isVpnActive;
    }

    [Serializable]
    public class GpuStaticInfo
    {
        public string name;
        public string manufacturer;
        public string driverVersion;
        public int dedicatedMemoryMB;
        public int sharedMemoryMB;
    }

    [Serializable]
    public class GpuDynamicInfo
    {
        public string currentTime;
        public float totalGpuUsagePercent;
        public List<GpuEngineUsage> engineUsages;
        public float dedicatedMemoryUsedMB;
        public float sharedMemoryUsedMB;
        public float? temperatureCelsius;
        public int? fanSpeedRpm;
        public float? coreClockMHz;
        public float? encoderUsagePercent;
        public float? decoderUsagePercent;
        public double? powerDrawWatts;
        public float? vramBandwidthPercent;
    }

    [Serializable]
    public class GpuEngineUsage
    {
        public string engineName;
        public float usagePercent;
    }

    [Serializable]
    public class ProcessInfo
    {
        public int pid;
        public string processName;
        public string executablePath;
        public string startTime;
        public string runningDuration;
        public float cpuUsagePercent;
        public float memoryUsageMB;
        public int threadCount;
        public int handleCount;
        public string priority;
        public int sessionId;
        public int? parentPid;
        public string parentProcessName;
        public string userName;
        public bool isResponding;
        public string status;
    }

    [Serializable]
    public class ProcessIconInfo
    {
        public string processName;
        public string executablePath;
        public string iconBase64;
    }
}