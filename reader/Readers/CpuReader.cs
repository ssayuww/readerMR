using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using reader.Models;

namespace reader.Readers;

public static class CpuReader
{
    private static PerformanceCounter? _totalCpuCounter;
    private static List<PerformanceCounter> _perCoreCounters = new();
    private static PerformanceCounter? _interruptsCounter;
    private static PerformanceCounter? _contextSwitchesCounter;
    private static PerformanceCounter? _queueLengthCounter;
    private static PerformanceCounter? _userTimeCounter;
    private static PerformanceCounter? _privilegedTimeCounter;

    private static bool _initialized = false;

    public static CpuStaticInfo ReadStatic()
    {
        var cpu = new CpuStaticInfo();

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, Manufacturer, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed, Architecture, L2CacheSize, L3CacheSize FROM Win32_Processor");

            foreach (ManagementObject obj in searcher.Get())
            {
                cpu.Name = obj["Name"]?.ToString()?.Trim() ?? "Unknown";
                cpu.Manufacturer = obj["Manufacturer"]?.ToString()?.Trim() ?? "Unknown";
                cpu.PhysicalCores = SafeToInt(obj["NumberOfCores"]);
                cpu.LogicalCores = SafeToInt(obj["NumberOfLogicalProcessors"]);

                var maxClockMHz = SafeToFloat(obj["MaxClockSpeed"]);
                cpu.MaxClockGHz = maxClockMHz > 0 ? maxClockMHz / 1000f : 0f;
                cpu.BaseClockGHz = cpu.MaxClockGHz;

                cpu.Architecture = GetArchitecture(SafeToInt(obj["Architecture"]));

                var l2 = SafeToInt(obj["L2CacheSize"]);
                var l3 = SafeToInt(obj["L3CacheSize"]);

                cpu.L2Cache = l2 > 0 ? $"{l2} KB" : "Unknown";
                cpu.L3Cache = l3 > 0 ? $"{l3} KB" : "Unknown";

                break;
            }
        }
        catch
        {
        }

        return cpu;
    }

    public static CpuDynamicInfo ReadDynamic()
    {
        EnsureInitialized();

        var info = new CpuDynamicInfo
        {
            CurrentTime = DateTime.Now
        };

        try
        {
            if (_totalCpuCounter != null)
                info.TotalCpuUsagePercent = _totalCpuCounter.NextValue();
        }
        catch
        {
        }

        try
        {
            foreach (var counter in _perCoreCounters)
                info.PerCoreUsagePercent.Add(counter.NextValue());
        }
        catch
        {
        }

        try
        {
            if (_userTimeCounter != null)
                info.UserTimePercent = _userTimeCounter.NextValue();

            if (_privilegedTimeCounter != null)
                info.PrivilegedTimePercent = _privilegedTimeCounter.NextValue();
        }
        catch
        {
        }

        info.CurrentClockGHz = ReadCurrentClockGHz();

        try
        {
            if (_interruptsCounter != null)
                info.InterruptsPerSec = _interruptsCounter.NextValue();
        }
        catch
        {
        }

        try
        {
            if (_contextSwitchesCounter != null)
                info.ContextSwitchesPerSec = _contextSwitchesCounter.NextValue();
        }
        catch
        {
        }

        try
        {
            if (_queueLengthCounter != null)
                info.ProcessorQueueLength = _queueLengthCounter.NextValue();
        }
        catch
        {
        }

        info.LoadAverageLikeScore = CalculateLoadLike(
            info.TotalCpuUsagePercent,
            info.ProcessorQueueLength,
            Environment.ProcessorCount);

        info.CpuTemperatureCelsius = TryReadTemperature();
        info.IsThrottling = TryReadThrottling();
        info.CurrentPowerState = TryReadPowerState();

        for (int i = 0; i < Environment.ProcessorCount; i++)
            info.PerCoreTemperatureCelsius.Add(null);

        return info;
    }

    public static void Prime()
    {
        EnsureInitialized();

        try
        {
            _totalCpuCounter?.NextValue();

            foreach (var counter in _perCoreCounters)
                counter.NextValue();

            _interruptsCounter?.NextValue();
            _contextSwitchesCounter?.NextValue();
            _queueLengthCounter?.NextValue();
            _userTimeCounter?.NextValue();
            _privilegedTimeCounter?.NextValue();
        }
        catch
        {
        }
    }

    private static void EnsureInitialized()
    {
        if (_initialized)
            return;

        try
        {
            _totalCpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _interruptsCounter = new PerformanceCounter("Processor", "Interrupts/sec", "_Total");
            _contextSwitchesCounter = new PerformanceCounter("System", "Context Switches/sec");
            _queueLengthCounter = new PerformanceCounter("System", "Processor Queue Length");
            _userTimeCounter = new PerformanceCounter("Processor", "% User Time", "_Total");
            _privilegedTimeCounter = new PerformanceCounter("Processor", "% Privileged Time", "_Total");

            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                _perCoreCounters.Add(new PerformanceCounter("Processor", "% Processor Time", i.ToString()));
            }
        }
        catch
        {
        }

        _initialized = true;
    }

    private static float ReadCurrentClockGHz()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT CurrentClockSpeed FROM Win32_Processor");

            foreach (ManagementObject obj in searcher.Get())
            {
                var mhz = SafeToFloat(obj["CurrentClockSpeed"]);
                if (mhz > 0)
                    return mhz / 1000f;
            }
        }
        catch
        {
        }

        return 0f;
    }

    private static float? TryReadTemperature()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"root\WMI",
                "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");

            foreach (ManagementObject obj in searcher.Get())
            {
                var raw = obj["CurrentTemperature"];
                if (raw != null)
                {
                    double kelvinTenths = Convert.ToDouble(raw);
                    double celsius = (kelvinTenths / 10.0) - 273.15;
                    return (float)Math.Round(celsius, 1);
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static bool? TryReadThrottling()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT CurrentClockSpeed, MaxClockSpeed FROM Win32_Processor");

            foreach (ManagementObject obj in searcher.Get())
            {
                var current = SafeToFloat(obj["CurrentClockSpeed"]);
                var max = SafeToFloat(obj["MaxClockSpeed"]);

                if (current > 0 && max > 0)
                    return current < max * 0.95f;
            }
        }
        catch
        {
        }

        return null;
    }

    private static uint? TryReadPowerState()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT CurrentPowerState FROM Win32_Processor");

            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["CurrentPowerState"] != null)
                    return Convert.ToUInt32(obj["CurrentPowerState"]);
            }
        }
        catch
        {
        }

        return null;
    }

    private static double CalculateLoadLike(float totalCpuPercent, float queueLength, int logicalCpuCount)
    {
        if (logicalCpuCount <= 0)
            logicalCpuCount = 1;

        double cpuPart = (totalCpuPercent / 100.0) * logicalCpuCount;
        return Math.Round(cpuPart + queueLength, 2);
    }

    private static string GetArchitecture(int arch)
    {
        return arch switch
        {
            0 => "x86",
            5 => "ARM",
            9 => "x64",
            12 => "ARM64",
            _ => "Unknown"
        };
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

    private static float SafeToFloat(object? value)
    {
        try
        {
            return value == null ? 0f : Convert.ToSingle(value);
        }
        catch
        {
            return 0f;
        }
    }
}