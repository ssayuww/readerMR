using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using reader.Models;

namespace reader.Readers;

public static class GpuReader
{
    private static readonly List<PerformanceCounter> _gpuEngineCounters = new();
    private static readonly List<string> _selectedGpuLuidFragments = new();

    private static bool _initialized;

    public static GpuStaticInfo ReadStatic()
    {
        var info = new GpuStaticInfo();

        var candidates = ReadGpuCandidates();

        var selected = candidates
            .Where(c => !IsVirtualGpu(c))
            .OrderByDescending(IsPreferredVendor)
            .ThenByDescending(c => c.AdapterRamBytes)
            .ThenByDescending(c => c.Name?.Length ?? 0)
            .FirstOrDefault();

        if (selected == null)
            return info;

        info.Name = selected.Name ?? "Unknown";
        info.Manufacturer = selected.Manufacturer ?? "Unknown";
        info.DriverVersion = selected.DriverVersion ?? "Unknown";
        info.DedicatedMemoryMB = selected.AdapterRamBytes / (1024 * 1024);
        info.SharedMemoryMB = 0;

        CacheSelectedGpuLuidCandidates(selected);

        return info;
    }

    public static GpuDynamicInfo ReadDynamic()
    {
        EnsureInitialized();

        var info = new GpuDynamicInfo
        {
            CurrentTime = DateTime.Now
        };

        try
        {
            var engineValues = new List<GpuEngineUsageInfo>();

            foreach (var counter in _gpuEngineCounters)
            {
                float value;
                try
                {
                    value = counter.NextValue();
                }
                catch
                {
                    continue;
                }

                if (value <= 0.01f)
                    continue;

                var simplified = SimplifyEngineName(counter.InstanceName);

                engineValues.Add(new GpuEngineUsageInfo
                {
                    EngineName = simplified,
                    UsagePercent = value
                });
            }

            info.EngineUsages = engineValues
                .GroupBy(e => e.EngineName)
                .Select(g => new GpuEngineUsageInfo
                {
                    EngineName = g.Key,
                    UsagePercent = g.Sum(x => x.UsagePercent)
                })
                .OrderByDescending(e => e.UsagePercent)
                .ToList();

            info.TotalGpuUsagePercent = info.EngineUsages.Sum(e => e.UsagePercent);

            var encoder = info.EngineUsages
                .Where(e => e.EngineName.Contains("Encode", StringComparison.OrdinalIgnoreCase))
                .Sum(e => e.UsagePercent);

            var decoder = info.EngineUsages
                .Where(e => e.EngineName.Contains("Decode", StringComparison.OrdinalIgnoreCase))
                .Sum(e => e.UsagePercent);

            info.EncoderUsagePercent = encoder > 0 ? encoder : null;
            info.DecoderUsagePercent = decoder > 0 ? decoder : null;
        }
        catch
        {
        }

        info.DedicatedMemoryUsedMB = 0;
        info.SharedMemoryUsedMB = 0;
        info.TemperatureCelsius = null;
        info.FanSpeedRpm = null;
        info.CoreClockMHz = null;
        info.PowerDrawWatts = null;
        info.VramBandwidthPercent = null;

        return info;
    }

    public static void Prime()
    {
        EnsureInitialized();

        foreach (var counter in _gpuEngineCounters)
        {
            try
            {
                counter.NextValue();
            }
            catch
            {
            }
        }
    }

    private static void EnsureInitialized()
    {
        if (_initialized)
            return;

        try
        {
            if (_selectedGpuLuidFragments.Count == 0)
            {
                var candidates = ReadGpuCandidates();
                var selected = candidates
                    .Where(c => !IsVirtualGpu(c))
                    .OrderByDescending(IsPreferredVendor)
                    .ThenByDescending(c => c.AdapterRamBytes)
                    .ThenByDescending(c => c.Name?.Length ?? 0)
                    .FirstOrDefault();

                if (selected != null)
                    CacheSelectedGpuLuidCandidates(selected);
            }

            var category = new PerformanceCounterCategory("GPU Engine");

            foreach (var instance in category.GetInstanceNames())
            {
                if (!LooksLikeUsefulGpuEngine(instance))
                    continue;

                if (_selectedGpuLuidFragments.Count > 0 &&
                    !_selectedGpuLuidFragments.Any(fragment =>
                        instance.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                try
                {
                    _gpuEngineCounters.Add(new PerformanceCounter(
                        "GPU Engine",
                        "Utilization Percentage",
                        instance));
                }
                catch
                {
                }
            }
        }
        catch
        {
        }

        _initialized = true;
    }

    private static List<GpuCandidate> ReadGpuCandidates()
    {
        var results = new List<GpuCandidate>();

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, AdapterCompatibility, DriverVersion, AdapterRAM, PNPDeviceID, VideoProcessor FROM Win32_VideoController");

            foreach (ManagementObject obj in searcher.Get())
            {
                var candidate = new GpuCandidate
                {
                    Name = obj["Name"]?.ToString()?.Trim(),
                    Manufacturer = obj["AdapterCompatibility"]?.ToString()?.Trim(),
                    DriverVersion = obj["DriverVersion"]?.ToString()?.Trim(),
                    AdapterRamBytes = SafeToULong(obj["AdapterRAM"]),
                    PnpDeviceId = obj["PNPDeviceID"]?.ToString()?.Trim(),
                    VideoProcessor = obj["VideoProcessor"]?.ToString()?.Trim()
                };

                if (!string.IsNullOrWhiteSpace(candidate.Name))
                    results.Add(candidate);
            }
        }
        catch
        {
        }

        return results;
    }

    private static bool IsVirtualGpu(GpuCandidate gpu)
    {
        string text = $"{gpu.Name} {gpu.Manufacturer} {gpu.VideoProcessor} {gpu.PnpDeviceId}".ToLowerInvariant();

        string[] blocked =
        {
            "meta",
            "virtual",
            "remote",
            "mirror",
            "basic display",
            "indirect display",
            "render only",
            "hyper-v",
            "vmware",
            "virtualbox",
            "parallels",
            "rdp",
            "citrix"
        };

        if (blocked.Any(k => text.Contains(k)))
            return true;

        if (gpu.AdapterRamBytes == 0 &&
            (text.Contains("monitor") || text.Contains("display")))
            return true;

        return false;
    }

    private static int IsPreferredVendor(GpuCandidate gpu)
    {
        string text = $"{gpu.Name} {gpu.Manufacturer} {gpu.VideoProcessor}".ToLowerInvariant();

        if (text.Contains("nvidia"))
            return 3;

        if (text.Contains("amd") || text.Contains("radeon") || text.Contains("advanced micro devices"))
            return 3;

        if (text.Contains("intel"))
            return 2;

        return 0;
    }

    private static void CacheSelectedGpuLuidCandidates(GpuCandidate selected)
    {
        _selectedGpuLuidFragments.Clear();

        string text = $"{selected.Name} {selected.Manufacturer} {selected.VideoProcessor}".ToLowerInvariant();

        if (text.Contains("nvidia"))
        {
            _selectedGpuLuidFragments.Add("phys_0");
            _selectedGpuLuidFragments.Add("engtype_");
            return;
        }

        if (text.Contains("amd") || text.Contains("radeon"))
        {
            _selectedGpuLuidFragments.Add("phys_0");
            _selectedGpuLuidFragments.Add("engtype_");
            return;
        }

        if (text.Contains("intel"))
        {
            _selectedGpuLuidFragments.Add("phys_0");
            _selectedGpuLuidFragments.Add("engtype_");
            return;
        }

        _selectedGpuLuidFragments.Add("engtype_");
    }

    private static bool LooksLikeUsefulGpuEngine(string instance)
    {
        if (string.IsNullOrWhiteSpace(instance))
            return false;

        string text = instance.ToLowerInvariant();

        if (!text.Contains("engtype_"))
            return false;

        string[] blocked =
        {
            "meta",
            "virtual",
            "vmware",
            "hyper-v",
            "basicrender"
        };

        if (blocked.Any(k => text.Contains(k)))
            return false;

        return true;
    }

    private static string SimplifyEngineName(string rawInstanceName)
    {
        if (string.IsNullOrWhiteSpace(rawInstanceName))
            return "Unknown";

        string text = rawInstanceName;

        int idx = text.IndexOf("engtype_", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            string type = text[(idx + "engtype_".Length)..];

            if (type.Contains("3D", StringComparison.OrdinalIgnoreCase))
                return "3D";

            if (type.Contains("Copy", StringComparison.OrdinalIgnoreCase))
                return "Copy";

            if (type.Contains("Video Codec", StringComparison.OrdinalIgnoreCase))
                return "Video Codec";

            if (type.Contains("Video Decode", StringComparison.OrdinalIgnoreCase))
                return "Video Decode";

            if (type.Contains("Video Encode", StringComparison.OrdinalIgnoreCase))
                return "Video Encode";

            if (type.Contains("Compute", StringComparison.OrdinalIgnoreCase))
                return "Compute";

            return type;
        }

        return rawInstanceName;
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

    private sealed class GpuCandidate
    {
        public string? Name { get; set; }
        public string? Manufacturer { get; set; }
        public string? DriverVersion { get; set; }
        public ulong AdapterRamBytes { get; set; }
        public string? PnpDeviceId { get; set; }
        public string? VideoProcessor { get; set; }
    }
}