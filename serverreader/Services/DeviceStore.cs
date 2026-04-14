using System.Collections.Concurrent;
using serverreader.Models;

namespace serverreader.Services;

public class DeviceStore
{
    private readonly ConcurrentDictionary<string, DeviceSnapshot> _devices = new();

    public void Save(DeviceSnapshot snapshot)
    {
        _devices[snapshot.DeviceId] = snapshot;
    }

    public IReadOnlyCollection<DeviceSnapshot> GetAll()
    {
        return _devices.Values.ToList();
    }

    public DeviceSnapshot? GetById(string deviceId)
    {
        _devices.TryGetValue(deviceId, out var snapshot);
        return snapshot;
    }
}