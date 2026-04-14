namespace serverreader.Models;

public class StaticSystemInfo
{
    public string MachineName { get; set; }
    public string UserName { get; set; }
    public string OSName { get; set; }
    public string OSVersion { get; set; }
    public string Architecture { get; set; }

    public string Manufacturer { get; set; }
    public string Model { get; set; }
    public string BiosVersion { get; set; }
    public string Motherboard { get; set; }
    public string Serial { get; set; }

    public DateTime BootTime { get; set; }

    public string TimeZone { get; set; }
    public string Locale { get; set; }
    public string Hostname { get; set; }
    public string Domain { get; set; }
}

public class DynamicSystemInfo
{
    public DateTime CurrentTime { get; set; }
    public TimeSpan Uptime { get; set; }
}