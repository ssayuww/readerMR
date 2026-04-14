namespace reader{



using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;


public class DeviceJsonReader : MonoBehaviour
{
    [SerializeField] private string serverUrl = "http://192.168.0.30:5000/api/devices";
    [SerializeField] private float refreshSeconds = 3f;

    public List<DeviceSnapshot> Devices { get; private set; } = new();
    public DeviceSnapshot? CurrentDevice { get; private set; }

    public event Action<List<DeviceSnapshot>>? OnDevicesUpdated;
    public event Action<DeviceSnapshot>? OnCurrentDeviceUpdated;

    private readonly JsonSerializerSettings _jsonSettings = new()
    {
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Include
    };

    private void Start()
    {
        StartCoroutine(PollLoop());
    }

    private IEnumerator PollLoop()
    {
        while (true)
        {
            yield return StartCoroutine(ReadFromServer());
            yield return new WaitForSeconds(refreshSeconds);
        }
    }

    public IEnumerator ReadFromServer()
    {
        using UnityWebRequest request = UnityWebRequest.Get(serverUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to read JSON: " + request.error);
            yield break;
        }

        string json = request.downloadHandler.text;

        List<DeviceSnapshot>? parsed;
        try
        {
            parsed = JsonConvert.DeserializeObject<List<DeviceSnapshot>>(json, _jsonSettings);
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to parse JSON into classes:\n" + ex);
            yield break;
        }

        if (parsed == null)
        {
            Debug.LogError("Parsed device list is null.");
            yield break;
        }

        Devices = parsed;
        CurrentDevice = Devices.Count > 0 ? Devices[0] : null;

        Debug.Log($"Loaded devices: {Devices.Count}");

        for (int i = 0; i < Devices.Count; i++)
        {
            DeviceDebugPrinter.Print(Devices[i]);
        }

        OnDevicesUpdated?.Invoke(Devices);

        if (CurrentDevice != null)
            OnCurrentDeviceUpdated?.Invoke(CurrentDevice);
    }

    public bool SelectDeviceById(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return false;

        var found = Devices.Find(d =>
            string.Equals(d.deviceId, deviceId, StringComparison.OrdinalIgnoreCase));

        if (found == null)
            return false;

        CurrentDevice = found;
        OnCurrentDeviceUpdated?.Invoke(found);
        return true;
    }

    public bool SelectDeviceByMachineName(string machineName)
    {
        if (string.IsNullOrWhiteSpace(machineName))
            return false;

        var found = Devices.Find(d =>
            string.Equals(d.staticSystemInfo?.machineName, machineName, StringComparison.OrdinalIgnoreCase));

        if (found == null)
            return false;

        CurrentDevice = found;
        OnCurrentDeviceUpdated?.Invoke(found);
        return true;
    }
}
}