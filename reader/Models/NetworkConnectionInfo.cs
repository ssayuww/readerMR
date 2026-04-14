namespace reader.Models;

public class NetworkConnectionInfo
{
    public int OpenTcpConnections { get; set; }
    public int EstablishedConnections { get; set; }
    public int ListeningPorts { get; set; }

    public List<string> TopRemoteEndpoints { get; set; } = new();
    public List<ProcessBandwidthInfo> TopProcessesByBandwidth { get; set; } = new();
}

public class ProcessBandwidthInfo
{
    public string ProcessName { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public double BandwidthEstimateMbps { get; set; }
}