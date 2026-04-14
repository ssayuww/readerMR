namespace reader.Models;

public class NetworkAdvancedInfo
{
    public double? PingLatencyMs { get; set; }
    public double? JitterMs { get; set; }
    public double? DnsLookupMs { get; set; }

    public string PublicIp { get; set; } = string.Empty;
    public bool? IsVpnActive { get; set; }
}