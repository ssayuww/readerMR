using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using reader.Models;

namespace reader.Readers;

public static class ProcessReader
{
    private static readonly Dictionary<int, TimeSpan> _lastCpuTimes = new();
    private static DateTime _lastSampleTime = DateTime.MinValue;

    public static List<ProcessInfo> Read(int maxCount = 40)
    {
        var result = new List<ProcessInfo>();

        Process[] processes;
        try
        {
            processes = Process.GetProcesses();
        }
        catch
        {
            return result;
        }

        var now = DateTime.Now;
        double seconds = _lastSampleTime == DateTime.MinValue
            ? 0
            : (now - _lastSampleTime).TotalSeconds;

        foreach (var process in processes)
        {
            try
            {
                var info = new ProcessInfo
                {
                    Pid = process.Id,
                    ProcessName = Safe(() => process.ProcessName, string.Empty),
                    StartTime = TryGetStartTime(process),
                    MemoryUsageMB = Safe(() => process.WorkingSet64 / 1024f / 1024f, 0f),
                    ThreadCount = Safe(() => process.Threads.Count, 0),
                    HandleCount = Safe(() => process.HandleCount, 0),
                    Priority = TryGetPriority(process),
                    SessionId = Safe(() => process.SessionId, 0),

                    // temporarily disabled risky fields
                    ExecutablePath = string.Empty,
                    ParentPid = null,
                    ParentProcessName = string.Empty,
                    UserName = string.Empty,
                    IsResponding = true,
                    Status = "Running"
                };

                if (info.StartTime.HasValue)
                    info.RunningDuration = now - info.StartTime.Value;

                info.CpuUsagePercent = CalculateCpu(process, seconds);

                result.Add(info);
            }
            catch
            {
            }
            finally
            {
                try { process.Dispose(); } catch { }
            }
        }

        _lastSampleTime = now;

        return result
            .OrderByDescending(p => p.CpuUsagePercent)
            .ThenByDescending(p => p.MemoryUsageMB)
            .Take(maxCount)
            .ToList();
    }

    private static float CalculateCpu(Process process, double seconds)
    {
        try
        {
            var current = process.TotalProcessorTime;

            if (!_lastCpuTimes.TryGetValue(process.Id, out var previous))
            {
                _lastCpuTimes[process.Id] = current;
                return 0f;
            }

            _lastCpuTimes[process.Id] = current;

            if (seconds <= 0)
                return 0f;

            double cpuMs = (current - previous).TotalMilliseconds;
            double totalMs = seconds * Environment.ProcessorCount * 1000.0;

            if (totalMs <= 0)
                return 0f;

            return (float)Math.Max(0, Math.Min(100, (cpuMs / totalMs) * 100.0));
        }
        catch
        {
            return 0f;
        }
    }

    private static DateTime? TryGetStartTime(Process process)
    {
        try
        {
            return process.StartTime;
        }
        catch
        {
            return null;
        }
    }

    private static string TryGetPriority(Process process)
    {
        try
        {
            return process.PriorityClass.ToString();
        }
        catch
        {
            return string.Empty;
        }
    }

    private static T Safe<T>(Func<T> getter, T fallback)
    {
        try
        {
            return getter();
        }
        catch
        {
            return fallback;
        }
    }
}