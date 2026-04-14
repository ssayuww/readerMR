using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using reader.Models;

namespace reader.Readers;

public static class StorageReader
{
    private static readonly Dictionary<string, PerformanceCounter> _readCounters = new();
    private static readonly Dictionary<string, PerformanceCounter> _writeCounters = new();
    private static readonly Dictionary<string, PerformanceCounter> _readIopsCounters = new();
    private static readonly Dictionary<string, PerformanceCounter> _writeIopsCounters = new();
    private static readonly Dictionary<string, PerformanceCounter> _activeTimeCounters = new();
    private static readonly Dictionary<string, PerformanceCounter> _queueCounters = new();
    private static readonly Dictionary<string, PerformanceCounter> _latencyCounters = new();

    private static bool _initialized = false;

    public static StorageStaticInfo ReadStatic()
    {
        var result = new StorageStaticInfo();

        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                var info = new DriveStaticInfo
                {
                    DriveLetter = drive.Name.TrimEnd('\\'),
                    MountPoint = drive.Name,
                    VolumeName = SafeVolumeName(drive),
                    DriveType = GetReadableDriveType(drive),
                    FileSystem = drive.IsReady ? drive.DriveFormat : "Unavailable"
                };

                if (drive.IsReady)
                {
                    double totalGB = BytesToGB(drive.TotalSize);
                    double freeGB = BytesToGB(drive.TotalFreeSpace);
                    double usedGB = totalGB - freeGB;

                    info.TotalSizeGB = totalGB;
                    info.FreeSpaceGB = freeGB;
                    info.UsedSpaceGB = usedGB;
                    info.UsedPercent = totalGB > 0 ? (usedGB / totalGB) * 100.0 : 0;
                }

                if (info.DriveType == "Fixed")
                {
                    info.DriveType = TryGetPhysicalMediaType(info.DriveLetter) ?? "Fixed";
                }

                result.Drives.Add(info);
            }
            catch
            {
            }
        }

        return result;
    }

    public static StorageDynamicInfo ReadDynamic()
    {
        EnsureInitialized();

        var result = new StorageDynamicInfo();

        foreach (var driveLetter in _readCounters.Keys.ToList())
        {
            var info = new DriveDynamicInfo
            {
                DriveLetter = driveLetter,
                CurrentTime = DateTime.Now
            };

            try
            {
                info.ReadMBPerSec = _readCounters[driveLetter].NextValue() / (1024f * 1024f);
            }
            catch
            {
            }

            try
            {
                info.WriteMBPerSec = _writeCounters[driveLetter].NextValue() / (1024f * 1024f);
            }
            catch
            {
            }

            try
            {
                info.ReadIOPS = _readIopsCounters[driveLetter].NextValue();
            }
            catch
            {
            }

            try
            {
                info.WriteIOPS = _writeIopsCounters[driveLetter].NextValue();
            }
            catch
            {
            }

            info.TotalIOPS = info.ReadIOPS + info.WriteIOPS;

            try
            {
                info.ActiveTimePercent = _activeTimeCounters[driveLetter].NextValue();
            }
            catch
            {
            }

            try
            {
                info.QueueLength = _queueCounters[driveLetter].NextValue();
            }
            catch
            {
            }

            try
            {
                info.AvgResponseTimeMs = _latencyCounters[driveLetter].NextValue() * 1000f;
            }
            catch
            {
            }

            result.Drives.Add(info);
        }

        return result;
    }

    public static void Prime()
    {
        EnsureInitialized();

        foreach (var counter in _readCounters.Values)
        {
            try { counter.NextValue(); } catch { }
        }

        foreach (var counter in _writeCounters.Values)
        {
            try { counter.NextValue(); } catch { }
        }

        foreach (var counter in _readIopsCounters.Values)
        {
            try { counter.NextValue(); } catch { }
        }

        foreach (var counter in _writeIopsCounters.Values)
        {
            try { counter.NextValue(); } catch { }
        }

        foreach (var counter in _activeTimeCounters.Values)
        {
            try { counter.NextValue(); } catch { }
        }

        foreach (var counter in _queueCounters.Values)
        {
            try { counter.NextValue(); } catch { }
        }

        foreach (var counter in _latencyCounters.Values)
        {
            try { counter.NextValue(); } catch { }
        }
    }

    private static void EnsureInitialized()
    {
        if (_initialized)
            return;

        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (!drive.IsReady)
                    continue;

                string driveLetter = drive.Name.Substring(0, 2); // "C:"
                string instanceName = driveLetter;

                _readCounters[driveLetter] = new PerformanceCounter("LogicalDisk", "Disk Read Bytes/sec", instanceName);
                _writeCounters[driveLetter] = new PerformanceCounter("LogicalDisk", "Disk Write Bytes/sec", instanceName);
                _readIopsCounters[driveLetter] = new PerformanceCounter("LogicalDisk", "Disk Reads/sec", instanceName);
                _writeIopsCounters[driveLetter] = new PerformanceCounter("LogicalDisk", "Disk Writes/sec", instanceName);
                _activeTimeCounters[driveLetter] = new PerformanceCounter("LogicalDisk", "% Disk Time", instanceName);
                _queueCounters[driveLetter] = new PerformanceCounter("LogicalDisk", "Current Disk Queue Length", instanceName);
                _latencyCounters[driveLetter] = new PerformanceCounter("LogicalDisk", "Avg. Disk sec/Transfer", instanceName);
            }
            catch
            {
            }
        }

        _initialized = true;
    }

    private static string SafeVolumeName(DriveInfo drive)
    {
        try
        {
            return drive.IsReady ? drive.VolumeLabel : "Unavailable";
        }
        catch
        {
            return "Unavailable";
        }
    }

    private static string GetReadableDriveType(DriveInfo drive)
    {
        return drive.DriveType switch
        {
            DriveType.CDRom => "CD/DVD",
            DriveType.Fixed => "Fixed",
            DriveType.Network => "Network",
            DriveType.NoRootDirectory => "NoRoot",
            DriveType.Ram => "RAM",
            DriveType.Removable => "Removable",
            _ => "Unknown"
        };
    }

    private static string? TryGetPhysicalMediaType(string driveLetter)
    {
        try
        {
            string query = $@"
ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{driveLetter}'}}
WHERE AssocClass=Win32_LogicalDiskToPartition";

            using var partitionSearcher = new ManagementObjectSearcher(query);
            foreach (ManagementObject partition in partitionSearcher.Get())
            {
                string partitionId = partition["DeviceID"]?.ToString() ?? string.Empty;

                string diskQuery = $@"
ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partitionId}'}}
WHERE AssocClass=Win32_DiskDriveToDiskPartition";

                using var diskSearcher = new ManagementObjectSearcher(diskQuery);
                foreach (ManagementObject disk in diskSearcher.Get())
                {
                    string model = disk["Model"]?.ToString()?.ToLowerInvariant() ?? "";
                    string mediaType = disk["MediaType"]?.ToString()?.ToLowerInvariant() ?? "";

                    if (model.Contains("ssd") || mediaType.Contains("ssd"))
                        return "SSD";

                    if (model.Contains("nvme"))
                        return "SSD";

                    if (model.Contains("hdd") || mediaType.Contains("hard"))
                        return "HDD";

                    return "Fixed";
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static double BytesToGB(long bytes)
    {
        return Math.Round(bytes / 1024d / 1024d / 1024d, 2);
    }
}