using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using Microsoft.VisualBasic.Devices;
using reader.Models;

namespace reader.Readers;

public static class MemoryReader
{
    private static PerformanceCounter? _availableMemory;
    private static PerformanceCounter? _cacheBytes;
    private static PerformanceCounter? _commitBytes;
    private static PerformanceCounter? _commitLimit;
    private static PerformanceCounter? _pageFaults;

    private static bool _initialized = false;

    public static MemoryStaticInfo ReadStatic()
    {
        var info = new MemoryStaticInfo();

        try
        {
            var computer = new ComputerInfo();
            info.TotalPhysicalMemoryMB = computer.TotalPhysicalMemory / (1024 * 1024);
        }
        catch
        {
        }

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Capacity, Speed, MemoryType FROM Win32_PhysicalMemory");

            foreach (ManagementObject obj in searcher.Get())
            {
                ulong capacity = SafeToULong(obj["Capacity"]) / (1024 * 1024);
                int speed = SafeToInt(obj["Speed"]);
                string type = GetMemoryType(SafeToInt(obj["MemoryType"]));

                info.ModuleSizesMB.Add(capacity);
                info.ModuleSpeedsMHz.Add(speed);
                info.ModuleTypes.Add(type);
            }

            info.ModuleCount = info.ModuleSizesMB.Count;
        }
        catch
        {
        }

        // RAM slots (often not reliable)
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT MemoryDevices FROM Win32_PhysicalMemoryArray");

            foreach (ManagementObject obj in searcher.Get())
            {
                info.TotalSlots = SafeToInt(obj["MemoryDevices"]);
                break;
            }
        }
        catch
        {
        }

        return info;
    }

    public static MemoryDynamicInfo ReadDynamic()
    {
        EnsureInitialized();

        var info = new MemoryDynamicInfo
        {
            CurrentTime = DateTime.Now
        };

        try
        {
            var computer = new ComputerInfo();

            float totalMB = computer.TotalPhysicalMemory / (1024f * 1024f);
            float freeMB = computer.AvailablePhysicalMemory / (1024f * 1024f);
            float usedMB = totalMB - freeMB;

            info.TotalMemoryMB = totalMB;
            info.FreeMemoryMB = freeMB;
            info.UsedMemoryMB = usedMB;

            info.MemoryUsagePercent = (usedMB / totalMB) * 100f;
        }
        catch
        {
        }

        try
        {
            if (_cacheBytes != null)
                info.CachedMemoryMB = _cacheBytes.NextValue() / (1024 * 1024);
        }
        catch
        {
        }

        try
        {
            if (_commitBytes != null)
                info.CommittedMemoryMB = _commitBytes.NextValue() / (1024 * 1024);

            if (_commitLimit != null)
                info.CommitLimitMB = _commitLimit.NextValue() / (1024 * 1024);

            if (info.CommitLimitMB > 0)
                info.PageFileUsagePercent = (info.CommittedMemoryMB / info.CommitLimitMB) * 100f;
        }
        catch
        {
        }

        try
        {
            if (_pageFaults != null)
                info.PageFaultsPerSec = _pageFaults.NextValue();
        }
        catch
        {
        }

        // simple pressure heuristic
        info.MemoryPressure = MathF.Min(100f, info.MemoryUsagePercent + (info.PageFileUsagePercent * 0.5f));

        return info;
    }

    public static void Prime()
    {
        EnsureInitialized();

        _availableMemory?.NextValue();
        _cacheBytes?.NextValue();
        _commitBytes?.NextValue();
        _commitLimit?.NextValue();
        _pageFaults?.NextValue();
    }

    private static void EnsureInitialized()
    {
        if (_initialized)
            return;

        try
        {
            _availableMemory = new PerformanceCounter("Memory", "Available MBytes");
            _cacheBytes = new PerformanceCounter("Memory", "Cache Bytes");
            _commitBytes = new PerformanceCounter("Memory", "Committed Bytes");
            _commitLimit = new PerformanceCounter("Memory", "Commit Limit");
            _pageFaults = new PerformanceCounter("Memory", "Page Faults/sec");
        }
        catch
        {
        }

        _initialized = true;
    }

    private static ulong SafeToULong(object? value)
    {
        try
        {
            return value == null ? 0 : Convert.ToUInt64(value);
        }
        catch
        {
            return 0;
        }
    }

    private static int SafeToInt(object? value)
    {
        try
        {
            return value == null ? 0 : Convert.ToInt32(value);
        }
        catch
        {
            return 0;
        }
    }

    private static string GetMemoryType(int type)
    {
        return type switch
        {
            20 => "DDR",
            21 => "DDR2",
            24 => "DDR3",
            26 => "DDR4",
            34 => "DDR5",
            _ => "Unknown"
        };
    }
}