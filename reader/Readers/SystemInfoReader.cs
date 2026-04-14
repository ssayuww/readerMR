using System;
using System.Management;
using System.Globalization;
namespace reader.Readers;
public static class SystemInfoReader
{
    public static StaticSystemInfo ReadStatic()
    {
        var info = new StaticSystemInfo();

        info.MachineName = Environment.MachineName;
        info.UserName = Environment.UserName;
        info.OSVersion = Environment.OSVersion.ToString();
        info.OSName = GetOSName();
        info.Architecture = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";

        info.Hostname = Environment.MachineName;
        info.Domain = Environment.UserDomainName;

        info.TimeZone = TimeZoneInfo.Local.DisplayName;
        info.Locale = CultureInfo.CurrentCulture.Name;

        info.BootTime = DateTime.Now - TimeSpan.FromMilliseconds(Environment.TickCount64);

        info.Manufacturer = GetWMI("Win32_ComputerSystem", "Manufacturer");
        info.Model = GetWMI("Win32_ComputerSystem", "Model");
        info.BiosVersion = GetWMI("Win32_BIOS", "SMBIOSBIOSVersion");
        info.Motherboard = GetWMI("Win32_BaseBoard", "Product");
        info.Serial = GetWMI("Win32_BIOS", "SerialNumber");

        return info;
    }

    public static DynamicSystemInfo ReadDynamic(DateTime bootTime)
    {
        return new DynamicSystemInfo
        {
            CurrentTime = DateTime.Now,
            Uptime = DateTime.Now - bootTime
        };
    }

    private static string GetWMI(string className, string property)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {className}");
            foreach (ManagementObject obj in searcher.Get())
            {
                return obj[property]?.ToString() ?? "Unknown";
            }
        }
        catch { }

        return "Unknown";
    }

    private static string GetOSName()
    {
        return System.Runtime.InteropServices.RuntimeInformation.OSDescription;
    }
}