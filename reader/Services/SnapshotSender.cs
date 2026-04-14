using System.Net.Http.Json;
using reader.Models;

namespace reader.Services;

public class SnapshotSender
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public SnapshotSender(string baseUrl)
    {
        _httpClient = new HttpClient();
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task SendAsync(DeviceSnapshot snapshot)
    {
        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/devices/update", snapshot);
        response.EnsureSuccessStatusCode();
    }
}