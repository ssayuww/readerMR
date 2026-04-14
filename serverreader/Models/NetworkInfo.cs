namespace serverreader.Models;

public class NetworkStaticInfo
{
    public bool IsInternetConnected { get; set; }

    public string InterfaceName { get; set; } = string.Empty;
    public string InterfaceType { get; set; } = string.Empty;

    public string MacAddress { get; set; } = string.Empty;
    public string LocalIPv4 { get; set; } = string.Empty;
    public string IPv6 { get; set; } = string.Empty;
    public string SubnetMask { get; set; } = string.Empty;
    public string Gateway { get; set; } = string.Empty;

    public List<string> DnsServers { get; set; } = new();

    public long LinkSpeedMbps { get; set; }

    public string Ssid { get; set; } = string.Empty;
    public int? WifiSignalStrengthPercent { get; set; }
}

public class NetworkDynamicInfo
{
    public DateTime CurrentTime { get; set; }

    public long BytesSentTotal { get; set; }
    public long BytesReceivedTotal { get; set; }

    public double UploadSpeedMbps { get; set; }
    public double DownloadSpeedMbps { get; set; }

    public long PacketsSent { get; set; }
    public long PacketsReceived { get; set; }

    public long IncomingPacketsDiscarded { get; set; }
    public long OutgoingPacketsDiscarded { get; set; }

    public long IncomingPacketsErrors { get; set; }
    public long OutgoingPacketsErrors { get; set; }
}