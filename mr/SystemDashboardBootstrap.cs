using System.Collections.Generic;
using UnityEngine;
using reader;

public class SystemDashboardBootstrap : MonoBehaviour
{
    [SerializeField] private DeviceJsonReader deviceReader;
    [SerializeField] private float distanceFromCamera = 1.5f;
    [SerializeField] private float horizontalSpacing = 1.0f;

    private readonly Dictionary<string, SystemDashboardPanel> _panels = new Dictionary<string, SystemDashboardPanel>();

    private void Start()
    {
        if (deviceReader == null)
            deviceReader = FindObjectOfType<DeviceJsonReader>();

        if (deviceReader == null)
        {
            Debug.LogError("DeviceJsonReader not found in scene.");
            return;
        }

        deviceReader.OnDevicesUpdated += HandleDevicesUpdated;

        if (deviceReader.Devices != null && deviceReader.Devices.Count > 0)
            HandleDevicesUpdated(deviceReader.Devices);
    }

    private void OnDestroy()
    {
        if (deviceReader != null)
            deviceReader.OnDevicesUpdated -= HandleDevicesUpdated;
    }

    private void HandleDevicesUpdated(List<DeviceSnapshot> devices)
    {
        if (devices == null || devices.Count == 0)
            return;

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("Main Camera not found.");
            return;
        }

        Vector3 forward = cam.transform.forward;
        Vector3 right = cam.transform.right;
        Vector3 center = cam.transform.position + forward * distanceFromCamera;

        float totalWidth = (devices.Count - 1) * horizontalSpacing;
        float startOffset = -totalWidth * 0.5f;

        for (int i = 0; i < devices.Count; i++)
        {
            var device = devices[i];
            if (device == null || string.IsNullOrWhiteSpace(device.deviceId))
                continue;

            if (_panels.ContainsKey(device.deviceId))
                continue;

            var panelGO = new GameObject("RuntimeSystemDashboard_" + device.deviceId);
            panelGO.transform.SetParent(null);

            Vector3 position = center + right * (startOffset + i * horizontalSpacing);
            panelGO.transform.position = position;
            panelGO.transform.forward = cam.transform.forward;

            var panel = panelGO.AddComponent<SystemDashboardPanel>();
            panel.Initialize(deviceReader, device.deviceId);

            _panels[device.deviceId] = panel;
        }
    }
}