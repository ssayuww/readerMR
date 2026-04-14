using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Threading.Tasks;
using reader.Models;

namespace reader.Readers;

public static class NetworkReader
{
    private static long _lastBytesSent;
    private static long _lastBytesReceived;
    private static DateTime _lastSampleTime;
    private static bool _primed;

    public static NetworkStaticInfo ReadStatic()
    {
        var info = new NetworkStaticInfo();

        var nic = GetPrimaryNetworkInterface();
        if (nic == null)
            return info;

        info.IsInternetConnected = NetworkInterface.GetIsNetworkAvailable();
        info.InterfaceName = nic.Name;
        info.InterfaceType = GetInterfaceType(nic);
        info.MacAddress = FormatMac(nic.GetPhysicalAddress());
        info.LinkSpeedMbps = nic.Speed > 0 ? nic.Speed / 1_000_000 : 0;

        var ipProps = nic.GetIPProperties();

        foreach (var ua in ipProps.UnicastAddresses)
        {
            if (ua.Address.AddressFamily == AddressFamily.InterNetwork &&
                !IPAddress.IsLoopback(ua.Address) &&
                string.IsNullOrWhiteSpace(info.LocalIPv4))
            {
                info.LocalIPv4 = ua.Address.ToString();
                info.SubnetMask = ua.IPv4Mask?.ToString() ?? string.Empty;
            }

            if (ua.Address.AddressFamily == AddressFamily.InterNetworkV6 &&
                string.IsNullOrWhiteSpace(info.IPv6))
            {
                info.IPv6 = ua.Address.ToString();
            }
        }

        var gateway = ipProps.GatewayAddresses
            .FirstOrDefault(g => g.Address.AddressFamily == AddressFamily.InterNetwork);

        if (gateway != null)
            info.Gateway = gateway.Address.ToString();

        info.DnsServers = ipProps.DnsAddresses.Select(d => d.ToString()).ToList();

        return info;
    }

    public static NetworkDynamicInfo ReadDynamic()
    {
        var info = new NetworkDynamicInfo
        {
            CurrentTime = DateTime.Now
        };

        var nic = GetPrimaryNetworkInterface();
        if (nic == null)
            return info;

        var stats = nic.GetIPv4Statistics();

        info.BytesSentTotal = stats.BytesSent;
        info.BytesReceivedTotal = stats.BytesReceived;

        info.PacketsSent = stats.UnicastPacketsSent + stats.NonUnicastPacketsSent;
        info.PacketsReceived = stats.UnicastPacketsReceived + stats.NonUnicastPacketsReceived;

        info.IncomingPacketsDiscarded = stats.IncomingPacketsDiscarded;
        info.OutgoingPacketsDiscarded = stats.OutgoingPacketsDiscarded;
        info.IncomingPacketsErrors = stats.IncomingPacketsWithErrors;
        info.OutgoingPacketsErrors = stats.OutgoingPacketsWithErrors;

        var now = DateTime.Now;

        if (_primed)
        {
            var seconds = (now - _lastSampleTime).TotalSeconds;
            if (seconds > 0)
            {
                var sentDelta = info.BytesSentTotal - _lastBytesSent;
                var recvDelta = info.BytesReceivedTotal - _lastBytesReceived;

                info.UploadSpeedMbps = (sentDelta * 8d) / seconds / 1_000_000d;
                info.DownloadSpeedMbps = (recvDelta * 8d) / seconds / 1_000_000d;
            }
        }

        _lastBytesSent = info.BytesSentTotal;
        _lastBytesReceived = info.BytesReceivedTotal;
        _lastSampleTime = now;
        _primed = true;

        return info;
    }

    public static NetworkConnectionInfo ReadConnections()
    {
        var info = new NetworkConnectionInfo();

        try
        {
            var props = IPGlobalProperties.GetIPGlobalProperties();

            var tcpConnections = props.GetActiveTcpConnections();
            var tcpListeners = props.GetActiveTcpListeners();

            info.OpenTcpConnections = tcpConnections.Length;
            info.EstablishedConnections = tcpConnections.Count(c => c.State == TcpState.Established);
            info.ListeningPorts = tcpListeners.Length;

            info.TopRemoteEndpoints = tcpConnections
                .Where(c =>
                    c.State == TcpState.Established &&
                    c.RemoteEndPoint != null &&
                    !IsLocalEndpoint(c.RemoteEndPoint))
                .Select(c => c.RemoteEndPoint.ToString())
                .Distinct()
                .Take(5)
                .ToList();
        }
        catch
        {
        }

        return info;
    }

    public static async Task<NetworkAdvancedInfo> ReadAdvancedAsync(string pingHost = "8.8.8.8")
    {
        var info = new NetworkAdvancedInfo();

        info.PingLatencyMs = await TryMeasurePingAsync(pingHost);
        info.JitterMs = await TryMeasureJitterAsync(pingHost);
        info.DnsLookupMs = await TryMeasureDnsLookupAsync("google.com");

        return info;
    }

    public static void Prime()
    {
        var nic = GetPrimaryNetworkInterface();
        if (nic == null)
            return;

        var stats = nic.GetIPv4Statistics();

        _lastBytesSent = stats.BytesSent;
        _lastBytesReceived = stats.BytesReceived;
        _lastSampleTime = DateTime.Now;
        _primed = true;
    }

    // =========================
    // FIXED PRIMARY ADAPTER
    // =========================

    private static NetworkInterface? GetPrimaryNetworkInterface()
    {
        var candidates = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up)
            .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
            .Where(n => !IsVirtualOrVpnAdapter(n))
            .Where(HasUsableIpv4Address)
            .ToList();

        if (candidates.Count == 0)
            return null;

        return candidates
            .OrderByDescending(HasIpv4Gateway)
            .ThenByDescending(HasPrivateIpv4Address)
            .ThenByDescending(IsPreferredInterfaceType)
            .ThenByDescending(n => n.Speed)
            .FirstOrDefault();
    }

    private static bool IsVirtualOrVpnAdapter(NetworkInterface nic)
    {
        string text = $"{nic.Name} {nic.Description}".ToLowerInvariant();

        string[] blocked =
        {
            "hamachi", "virtual", "vmware", "hyper-v",
            "vpn", "tap", "tun", "loopback",
            "npcap", "wireguard", "tailscale", "zerotier"
        };

        return blocked.Any(k => text.Contains(k));
    }

    private static bool HasUsableIpv4Address(NetworkInterface nic)
    {
        try
        {
            return nic.GetIPProperties().UnicastAddresses.Any(a =>
                a.Address.AddressFamily == AddressFamily.InterNetwork &&
                !IPAddress.IsLoopback(a.Address) &&
                !a.Address.ToString().StartsWith("169.254."));
        }
        catch
        {
            return false;
        }
    }

    private static bool HasIpv4Gateway(NetworkInterface nic)
    {
        try
        {
            return nic.GetIPProperties().GatewayAddresses.Any(g =>
                g.Address.AddressFamily == AddressFamily.InterNetwork &&
                g.Address.ToString() != "0.0.0.0");
        }
        catch
        {
            return false;
        }
    }

    private static bool HasPrivateIpv4Address(NetworkInterface nic)
    {
        try
        {
            foreach (var ua in nic.GetIPProperties().UnicastAddresses)
            {
                if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
                    continue;

                var b = ua.Address.GetAddressBytes();

                if (b[0] == 10) return true;
                if (b[0] == 192 && b[1] == 168) return true;
                if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsPreferredInterfaceType(NetworkInterface nic)
    {
        return nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
               nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
               nic.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet;
    }

    // =========================

    private static string GetInterfaceType(NetworkInterface nic)
    {
        return nic.NetworkInterfaceType switch
        {
            NetworkInterfaceType.Wireless80211 => "Wi-Fi",
            NetworkInterfaceType.Ethernet => "Ethernet",
            _ => nic.NetworkInterfaceType.ToString()
        };
    }

    private static string FormatMac(PhysicalAddress address)
    {
        return string.Join(":", address.GetAddressBytes().Select(b => b.ToString("X2")));
    }

    private static bool IsLocalEndpoint(IPEndPoint ep)
    {
        return IPAddress.IsLoopback(ep.Address) ||
               ep.Address.ToString().StartsWith("127.") ||
               ep.Address.ToString() == "::1";
    }

    // =========================
    // ADVANCED
    // =========================

    private static async Task<double?> TryMeasurePingAsync(string host)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, 2000);
            if (reply.Status == IPStatus.Success)
                return reply.RoundtripTime;
        }
        catch { }

        return null;
    }

    private static async Task<double?> TryMeasureJitterAsync(string host)
    {
        try
        {
            var values = new List<long>();
            using var ping = new Ping();

            for (int i = 0; i < 4; i++)
            {
                var reply = await ping.SendPingAsync(host, 2000);
                if (reply.Status == IPStatus.Success)
                    values.Add(reply.RoundtripTime);
            }

            if (values.Count < 2)
                return null;

            double avg = values.Average();
            double variance = values.Average(v => Math.Pow(v - avg, 2));
            return Math.Sqrt(variance);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<double?> TryMeasureDnsLookupAsync(string host)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            await Dns.GetHostAddressesAsync(host);
            sw.Stop();
            return sw.Elapsed.TotalMilliseconds;
        }
        catch
        {
            return null;
        }
    }
}