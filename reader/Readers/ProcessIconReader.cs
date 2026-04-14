using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using reader.Models;

namespace reader.Readers;

public static class ProcessIconReader
{
    public static List<ProcessIconInfo> Read(int maxCount = 30)
    {
        var results = new List<ProcessIconInfo>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Process[] processes;
        try
        {
            processes = Process.GetProcesses();
        }
        catch
        {
            return results;
        }

        foreach (var process in processes)
        {
            try
            {
                string path = TryGetExecutablePath(process);

                if (string.IsNullOrWhiteSpace(path))
                    continue;

                if (!File.Exists(path))
                    continue;

                if (!seenPaths.Add(path))
                    continue;

                string? iconBase64 = TryGetIconBase64(path);
                if (string.IsNullOrWhiteSpace(iconBase64))
                    continue;

                results.Add(new ProcessIconInfo
                {
                    ProcessName = Safe(() => process.ProcessName, string.Empty),
                    ExecutablePath = path,
                    IconBase64 = iconBase64
                });

                if (results.Count >= maxCount)
                    break;
            }
            catch
            {
            }
            finally
            {
                try { process.Dispose(); } catch { }
            }
        }

        return results
            .OrderBy(x => x.ProcessName)
            .ToList();
    }

    private static string TryGetExecutablePath(Process process)
    {
        try
        {
            return process.MainModule?.FileName ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string? TryGetIconBase64(string path)
    {
        try
        {
            using Icon? icon = Icon.ExtractAssociatedIcon(path);
            if (icon == null)
                return null;

            using Bitmap bmp = icon.ToBitmap();
            using MemoryStream ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return Convert.ToBase64String(ms.ToArray());
        }
        catch
        {
            return null;
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