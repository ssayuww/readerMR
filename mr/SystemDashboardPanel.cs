using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using reader;

public class SystemDashboardPanel : MonoBehaviour
{
    public enum Section
    {
        General,
        CPU,
        Memory,
        GPU,
        Storage,
        Network,
        Apps
    }

    [SerializeField] private DeviceJsonReader deviceReader;
    [SerializeField] private string deviceId;
    [SerializeField] private float panelWidth = 1500f;
    [SerializeField] private float panelHeight = 950f;
    [SerializeField] private float panelScale = 0.0015f;
    [SerializeField] private int maxAppsShown = 25;
    [SerializeField] private float minPanelWidth = 900f;
    [SerializeField] private float minPanelHeight = 600f;
    [SerializeField] private float maxPanelWidth = 2400f;
    [SerializeField] private float maxPanelHeight = 1600f;
    [SerializeField] private float colliderDepth = 0.02f;
    [SerializeField] private bool billboardToCamera = true;
    [SerializeField] private float billboardRotationSpeed = 8f;

    private Canvas _canvas;
    private RectTransform _root;
    private Text _headerText;
    private Text _sectionTitleText;
    private Text _detailsText;
    private ScrollRect _detailsScrollRect;
    private RectTransform _detailsContent;
    private BoxCollider _panelCollider;
    private PanelColliderSync _colliderSync;
    private Camera _mainCamera;

    private readonly Dictionary<Section, Button> _tabButtons = new Dictionary<Section, Button>();
    private Section _currentSection = Section.General;
    private DeviceSnapshot _currentDevice;

    public string DeviceId => deviceId;

    public void Initialize(DeviceJsonReader reader, string targetDeviceId)
    {
        deviceReader = reader;
        deviceId = targetDeviceId;
        _mainCamera = Camera.main;
        BuildUI();

        if (deviceReader == null)
        {
            _detailsText.text = "DeviceJsonReader is null.";
            return;
        }

        deviceReader.OnDevicesUpdated += HandleDevicesUpdated;
        deviceReader.OnCurrentDeviceUpdated += HandleCurrentDeviceUpdated;

        TryBindFromReader();
    }

    private void LateUpdate()
    {
        if (!billboardToCamera)
            return;

        if (_mainCamera == null)
            _mainCamera = Camera.main;

        if (_mainCamera == null)
            return;

        Vector3 toCamera = _mainCamera.transform.position - transform.position;
        toCamera.y = 0f;

        if (toCamera.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * billboardRotationSpeed);
    }

    private void OnDestroy()
    {
        if (deviceReader != null)
        {
            deviceReader.OnDevicesUpdated -= HandleDevicesUpdated;
            deviceReader.OnCurrentDeviceUpdated -= HandleCurrentDeviceUpdated;
        }
    }

    private void TryBindFromReader()
    {
        if (deviceReader == null)
            return;

        if (deviceReader.Devices != null && deviceReader.Devices.Count > 0)
        {
            var match = FindMatchingDevice(deviceReader.Devices);
            if (match != null)
            {
                _currentDevice = match;
                RefreshCurrentSection();
                return;
            }
        }

        if (deviceReader.CurrentDevice != null && IsMatch(deviceReader.CurrentDevice))
        {
            _currentDevice = deviceReader.CurrentDevice;
            RefreshCurrentSection();
            return;
        }

        RefreshCurrentSection();
    }

    private void HandleDevicesUpdated(List<DeviceSnapshot> devices)
    {
        _currentDevice = FindMatchingDevice(devices);
        RefreshCurrentSection();
    }

    private void HandleCurrentDeviceUpdated(DeviceSnapshot device)
    {
        if (IsMatch(device))
        {
            _currentDevice = device;
            RefreshCurrentSection();
        }
    }

    private DeviceSnapshot FindMatchingDevice(List<DeviceSnapshot> devices)
    {
        if (devices == null || devices.Count == 0)
            return null;

        if (string.IsNullOrWhiteSpace(deviceId))
            return devices[0];

        return devices.FirstOrDefault(d => string.Equals(d.deviceId, deviceId, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsMatch(DeviceSnapshot device)
    {
        if (device == null)
            return false;

        if (string.IsNullOrWhiteSpace(deviceId))
            return true;

        return string.Equals(device.deviceId, deviceId, StringComparison.OrdinalIgnoreCase);
    }

    private void BuildUI()
    {
        EnsureEventSystem();

        gameObject.name = string.IsNullOrWhiteSpace(deviceId) ? "SystemDashboardPanel" : "SystemDashboardPanel_" + deviceId;

        _canvas = GetComponent<Canvas>();
        if (_canvas == null)
            _canvas = gameObject.AddComponent<Canvas>();

        _canvas.renderMode = RenderMode.WorldSpace;

        if (GetComponent<CanvasScaler>() == null)
        {
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;
        }

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        TryAddTrackedDeviceGraphicRaycaster(gameObject);

        _root = GetComponent<RectTransform>();
        if (_root == null)
            _root = gameObject.AddComponent<RectTransform>();

        _root.anchorMin = new Vector2(0.5f, 0.5f);
        _root.anchorMax = new Vector2(0.5f, 0.5f);
        _root.pivot = new Vector2(0.5f, 0.5f);
        _root.sizeDelta = new Vector2(panelWidth, panelHeight);
        _root.localScale = Vector3.one * panelScale;

        var bg = GetOrAdd<Image>(gameObject);
        bg.color = new Color(0.05f, 0.08f, 0.14f, 0.74f);
        bg.raycastTarget = true;

        _panelCollider = GetOrAdd<BoxCollider>(gameObject);
        _colliderSync = GetOrAdd<PanelColliderSync>(gameObject);
        _colliderSync.Target = _root;
        _colliderSync.Collider = _panelCollider;
        _colliderSync.Depth = colliderDepth;
        _colliderSync.Refresh();

        RectTransform glowFrame = CreatePanel("GlowFrame", transform, new Color(0.20f, 0.55f, 0.95f, 0.22f));
        SetRect(glowFrame, 8f, -8f, 8f, -8f);

        RectTransform innerFrame = CreatePanel("InnerFrame", transform, new Color(0.10f, 0.13f, 0.20f, 0.88f));
        SetRect(innerFrame, 14f, -14f, 14f, -14f);

        RectTransform tabsPanel = CreatePanel("TabsPanel", innerFrame, new Color(0.09f, 0.12f, 0.18f, 0.88f));
        StretchLeft(tabsPanel, 240f);

        RectTransform mainPanel = CreatePanel("MainPanel", innerFrame, new Color(0.08f, 0.11f, 0.17f, 0.82f));
        StretchRemaining(mainPanel, 252f);

        RectTransform topAccent = CreatePanel("TopAccent", mainPanel, new Color(0.22f, 0.64f, 1f, 0.24f));
        SetRect(topAccent, 0f, 0f, 0f, panelHeight - 58f);

        _headerText = CreateText("Header", mainPanel, 30, FontStyle.Bold, TextAnchor.UpperLeft);
        SetRect(_headerText.rectTransform, 20f, -64f, 18f, 56f);
        _headerText.text = "System Dashboard";

        _sectionTitleText = CreateText("SectionTitle", mainPanel, 24, FontStyle.Bold, TextAnchor.UpperLeft);
        SetRect(_sectionTitleText.rectTransform, 20f, -16f, 76f, 112f);
        _sectionTitleText.text = "General";
        _sectionTitleText.color = new Color(0.75f, 0.90f, 1f, 1f);

        RectTransform detailsPanel = CreatePanel("DetailsPanel", mainPanel, new Color(0.12f, 0.16f, 0.24f, 0.62f));
        SetRect(detailsPanel, 20f, -20f, 132f, -94f);

        RectTransform detailsInner = CreatePanel("DetailsInner", detailsPanel, new Color(0.03f, 0.05f, 0.10f, 0.28f));
        SetRect(detailsInner, 10f, -10f, 10f, -10f);

        RectTransform viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask)).GetComponent<RectTransform>();
        viewport.SetParent(detailsInner, false);
        SetRect(viewport, 8f, -28f, 8f, -8f);
        var viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        viewportImage.raycastTarget = true;
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        RectTransform scrollbarObj = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar)).GetComponent<RectTransform>();
        scrollbarObj.SetParent(detailsInner, false);
        scrollbarObj.anchorMin = new Vector2(1f, 0f);
        scrollbarObj.anchorMax = new Vector2(1f, 1f);
        scrollbarObj.pivot = new Vector2(1f, 1f);
        scrollbarObj.sizeDelta = new Vector2(18f, 0f);
        scrollbarObj.anchoredPosition = new Vector2(-4f, 0f);
        var scrollbarBg = scrollbarObj.GetComponent<Image>();
        scrollbarBg.color = new Color(0.08f, 0.12f, 0.18f, 0.85f);
        scrollbarBg.raycastTarget = true;

        RectTransform slidingArea = new GameObject("SlidingArea", typeof(RectTransform)).GetComponent<RectTransform>();
        slidingArea.SetParent(scrollbarObj, false);
        slidingArea.anchorMin = Vector2.zero;
        slidingArea.anchorMax = Vector2.one;
        slidingArea.offsetMin = new Vector2(2f, 2f);
        slidingArea.offsetMax = new Vector2(-2f, -2f);

        RectTransform handle = new GameObject("Handle", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        handle.SetParent(slidingArea, false);
        handle.anchorMin = new Vector2(0f, 1f);
        handle.anchorMax = new Vector2(1f, 1f);
        handle.pivot = new Vector2(0.5f, 1f);
        handle.sizeDelta = new Vector2(0f, 60f);
        var handleImage = handle.GetComponent<Image>();
        handleImage.color = new Color(0.22f, 0.56f, 1f, 0.95f);
        handleImage.raycastTarget = true;

        Scrollbar scrollbar = scrollbarObj.GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = handleImage;

        RectTransform content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 1200f);
        _detailsContent = content;

        _detailsText = CreateText("Details", content, 20, FontStyle.Normal, TextAnchor.UpperLeft);
        _detailsText.rectTransform.anchorMin = new Vector2(0f, 1f);
        _detailsText.rectTransform.anchorMax = new Vector2(1f, 1f);
        _detailsText.rectTransform.pivot = new Vector2(0.5f, 1f);
        _detailsText.rectTransform.anchoredPosition = Vector2.zero;
        _detailsText.rectTransform.sizeDelta = new Vector2(-28f, 1200f);
        _detailsText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _detailsText.verticalOverflow = VerticalWrapMode.Overflow;
        _detailsText.text = "Waiting for data...";
        _detailsText.color = new Color(0.92f, 0.96f, 1f, 1f);

        _detailsScrollRect = detailsInner.gameObject.AddComponent<ScrollRect>();
        _detailsScrollRect.horizontal = false;
        _detailsScrollRect.vertical = true;
        _detailsScrollRect.movementType = ScrollRect.MovementType.Clamped;
        _detailsScrollRect.scrollSensitivity = 25f;
        _detailsScrollRect.viewport = viewport;
        _detailsScrollRect.content = content;
        _detailsScrollRect.verticalScrollbar = scrollbar;
        _detailsScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        RectTransform bottomBar = CreatePanel("BottomBar", mainPanel, new Color(0.10f, 0.13f, 0.18f, 0.92f));
        SetRect(bottomBar, 20f, -20f, panelHeight - 84f, -20f);

        RectTransform moveHandle = CreatePanel("MoveHandle", bottomBar, new Color(0.18f, 0.46f, 0.92f, 0.95f));
        moveHandle.anchorMin = new Vector2(0.5f, 0.5f);
        moveHandle.anchorMax = new Vector2(0.5f, 0.5f);
        moveHandle.pivot = new Vector2(0.5f, 0.5f);
        moveHandle.sizeDelta = new Vector2(260f, 46f);
        moveHandle.anchoredPosition = Vector2.zero;

        var moveText = CreateText("MoveLabel", moveHandle, 18, FontStyle.Bold, TextAnchor.MiddleCenter);
        moveText.rectTransform.anchorMin = Vector2.zero;
        moveText.rectTransform.anchorMax = Vector2.one;
        moveText.rectTransform.offsetMin = Vector2.zero;
        moveText.rectTransform.offsetMax = Vector2.zero;
        moveText.text = "DRAG PANEL";
        moveText.color = Color.white;

        var moveScript = moveHandle.gameObject.AddComponent<PanelMoveHandle>();
        moveScript.Target = transform;

        RectTransform resizeHandle = CreatePanel("ResizeHandle", innerFrame, new Color(0.22f, 0.56f, 1f, 0.95f));
        resizeHandle.anchorMin = new Vector2(1f, 0f);
        resizeHandle.anchorMax = new Vector2(1f, 0f);
        resizeHandle.pivot = new Vector2(1f, 0f);
        resizeHandle.sizeDelta = new Vector2(56f, 56f);
        resizeHandle.anchoredPosition = new Vector2(-12f, 12f);

        Text resizeText = CreateText("ResizeLabel", resizeHandle, 22, FontStyle.Bold, TextAnchor.MiddleCenter);
        resizeText.rectTransform.anchorMin = Vector2.zero;
        resizeText.rectTransform.anchorMax = Vector2.one;
        resizeText.rectTransform.offsetMin = Vector2.zero;
        resizeText.rectTransform.offsetMax = Vector2.zero;
        resizeText.text = "◢";
        resizeText.color = Color.white;

        var resizeScript = resizeHandle.gameObject.AddComponent<PanelResizeHandle>();
        resizeScript.Target = _root;
        resizeScript.MinWidth = minPanelWidth;
        resizeScript.MinHeight = minPanelHeight;
        resizeScript.MaxWidth = maxPanelWidth;
        resizeScript.MaxHeight = maxPanelHeight;
        resizeScript.OnResized = () =>
        {
            if (_colliderSync != null)
                _colliderSync.Refresh();
            RebuildScrollContent();
        };

        BuildTabs(tabsPanel);
        RebuildScrollContent();
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
            return;

        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(go);
        TryReplaceWithXRInputModule(go);
    }

    private void TryReplaceWithXRInputModule(GameObject eventSystemObject)
    {
        var typeNames = new[]
        {
            "UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule, Unity.XR.Interaction.Toolkit",
            "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem"
        };

        foreach (var typeName in typeNames)
        {
            var type = Type.GetType(typeName);
            if (type == null)
                continue;

            if (eventSystemObject.GetComponent(type) == null)
            {
                var standalone = eventSystemObject.GetComponent<StandaloneInputModule>();
                if (standalone != null)
                    Destroy(standalone);

                eventSystemObject.AddComponent(type);
            }

            return;
        }
    }

    private void TryAddTrackedDeviceGraphicRaycaster(GameObject target)
    {
        var typeNames = new[]
        {
            "UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit",
            "UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit.UI"
        };

        foreach (var typeName in typeNames)
        {
            var type = Type.GetType(typeName);
            if (type == null)
                continue;

            if (target.GetComponent(type) == null)
                target.AddComponent(type);

            return;
        }
    }

    private void BuildTabs(RectTransform tabsPanel)
    {
        float top = 18f;
        float height = 58f;
        float gap = 10f;

        CreateTabButton(tabsPanel, Section.General, "General", top, height);
        top += height + gap;
        CreateTabButton(tabsPanel, Section.CPU, "CPU", top, height);
        top += height + gap;
        CreateTabButton(tabsPanel, Section.Memory, "Memory", top, height);
        top += height + gap;
        CreateTabButton(tabsPanel, Section.GPU, "GPU", top, height);
        top += height + gap;
        CreateTabButton(tabsPanel, Section.Storage, "Storage", top, height);
        top += height + gap;
        CreateTabButton(tabsPanel, Section.Network, "Network", top, height);
        top += height + gap;
        CreateTabButton(tabsPanel, Section.Apps, "Apps", top, height);

        UpdateTabColors();
    }

    private void CreateTabButton(RectTransform parent, Section section, string label, float top, float height)
    {
        GameObject buttonObj = new GameObject(label + "Tab", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(parent, false);

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(12f, -(top + height));
        rect.offsetMax = new Vector2(-12f, -top);

        var image = buttonObj.GetComponent<Image>();
        image.raycastTarget = true;

        Button button = buttonObj.GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            _currentSection = section;
            RefreshCurrentSection();
            UpdateTabColors();
        });

        Text labelText = CreateText("Label", rect, 22, FontStyle.Bold, TextAnchor.MiddleCenter);
        labelText.rectTransform.anchorMin = Vector2.zero;
        labelText.rectTransform.anchorMax = Vector2.one;
        labelText.rectTransform.offsetMin = Vector2.zero;
        labelText.rectTransform.offsetMax = Vector2.zero;
        labelText.text = label;
        labelText.color = Color.white;

        _tabButtons[section] = button;
    }

    private void UpdateTabColors()
    {
        foreach (var pair in _tabButtons)
        {
            Image img = pair.Value.GetComponent<Image>();
            img.color = pair.Key == _currentSection
                ? new Color(0.20f, 0.56f, 1f, 0.96f)
                : new Color(0.12f, 0.18f, 0.28f, 0.78f);
        }
    }

    private void RefreshCurrentSection()
    {
        if (_headerText == null || _sectionTitleText == null || _detailsText == null)
            return;

        if (_currentDevice == null)
        {
            _headerText.text = "System Dashboard";
            _sectionTitleText.text = _currentSection.ToString();
            _detailsText.text = "No device data for " + Safe(deviceId, "device") + ".";
            RebuildScrollContent();
            return;
        }

        _headerText.text = Safe(_currentDevice.displayName, "Unknown device") + " • " +
                           Safe(_currentDevice.staticSystemInfo?.machineName, "Unknown machine");

        _sectionTitleText.text = _currentSection.ToString();

        try
        {
            switch (_currentSection)
            {
                case Section.General:
                    _detailsText.text = BuildGeneralText();
                    break;
                case Section.CPU:
                    _detailsText.text = BuildCpuText();
                    break;
                case Section.Memory:
                    _detailsText.text = BuildMemoryText();
                    break;
                case Section.GPU:
                    _detailsText.text = BuildGpuText();
                    break;
                case Section.Storage:
                    _detailsText.text = BuildStorageText();
                    break;
                case Section.Network:
                    _detailsText.text = BuildNetworkText();
                    break;
                case Section.Apps:
                    _detailsText.text = BuildAppsText();
                    break;
            }
        }
        catch (Exception ex)
        {
            _detailsText.text = "Failed to render section:\n" + ex;
        }

        RebuildScrollContent();

        if (_detailsScrollRect != null)
            _detailsScrollRect.verticalNormalizedPosition = 1f;
    }

    private void RebuildScrollContent()
    {
        if (_detailsText == null || _detailsContent == null)
            return;

        Canvas.ForceUpdateCanvases();

        float minHeight = Mathf.Max(700f, _root.rect.height - 320f);
        float neededHeight = Mathf.Max(minHeight, _detailsText.preferredHeight + 40f);

        _detailsText.rectTransform.sizeDelta = new Vector2(_detailsText.rectTransform.sizeDelta.x, neededHeight);
        _detailsContent.sizeDelta = new Vector2(_detailsContent.sizeDelta.x, neededHeight);
    }

    private string BuildGeneralText()
    {
        var s = _currentDevice.staticSystemInfo;
        var d = _currentDevice.dynamicSystemInfo;

        return
            "Display Name: " + Safe(_currentDevice.displayName) + "\n" +
            "Device ID: " + Safe(_currentDevice.deviceId) + "\n" +
            "Machine Name: " + Safe(s?.machineName) + "\n" +
            "User: " + Safe(s?.userName) + "\n" +
            "OS: " + Safe(s?.osName) + "\n" +
            "OS Version: " + Safe(s?.osVersion) + "\n" +
            "Architecture: " + Safe(s?.architecture) + "\n" +
            "Manufacturer: " + Safe(s?.manufacturer) + "\n" +
            "Model: " + Safe(s?.model) + "\n" +
            "BIOS: " + Safe(s?.biosVersion) + "\n" +
            "Motherboard: " + Safe(s?.motherboard) + "\n" +
            "Serial: " + Safe(s?.serial) + "\n" +
            "Boot Time: " + Safe(s?.bootTime) + "\n" +
            "Current Time: " + Safe(d?.currentTime) + "\n" +
            "Uptime: " + Safe(d?.uptime) + "\n" +
            "Timezone: " + Safe(s?.timeZone) + "\n" +
            "Locale: " + Safe(s?.locale) + "\n" +
            "Hostname: " + Safe(s?.hostname) + "\n" +
            "Domain: " + Safe(s?.domain);
    }

    private string BuildCpuText()
    {
        var s = _currentDevice.cpuStaticInfo;
        var d = _currentDevice.cpuDynamicInfo;

        string cores = d?.perCoreUsagePercent != null
            ? string.Join("\n", d.perCoreUsagePercent.Select((v, i) => "Core " + i + ": " + v.ToString("F1") + "%"))
            : "No per-core data";

        return
            "Name: " + Safe(s?.name) + "\n" +
            "Manufacturer: " + Safe(s?.manufacturer) + "\n" +
            "Physical Cores: " + (s?.physicalCores.ToString() ?? "-") + "\n" +
            "Logical Cores: " + (s?.logicalCores.ToString() ?? "-") + "\n" +
            "Base Clock: " + (s != null ? s.baseClockGHz.ToString("F2") : "-") + " GHz\n" +
            "Max Clock: " + (s != null ? s.maxClockGHz.ToString("F2") : "-") + " GHz\n" +
            "Architecture: " + Safe(s?.architecture) + "\n" +
            "L2 Cache: " + Safe(s?.l2Cache) + "\n" +
            "L3 Cache: " + Safe(s?.l3Cache) + "\n\n" +
            "Current Time: " + Safe(d?.currentTime) + "\n" +
            "Total Usage: " + (d != null ? d.totalCpuUsagePercent.ToString("F1") : "-") + "%\n" +
            "Current Clock: " + (d != null ? d.currentClockGHz.ToString("F2") : "-") + " GHz\n" +
            "User Time: " + (d != null ? d.userTimePercent.ToString("F1") : "-") + "%\n" +
            "Kernel Time: " + (d != null ? d.privilegedTimePercent.ToString("F1") : "-") + "%\n" +
            "Interrupts/sec: " + (d != null ? d.interruptsPerSec.ToString("F0") : "-") + "\n" +
            "Context Switches/sec: " + (d != null ? d.contextSwitchesPerSec.ToString("F0") : "-") + "\n" +
            "Queue Length: " + (d != null ? d.processorQueueLength.ToString("F1") : "-") + "\n" +
            "Load-Like Score: " + (d != null ? d.loadAverageLikeScore.ToString("F2") : "-") + "\n" +
            "Temperature: " + (d?.cpuTemperatureCelsius.HasValue == true ? d.cpuTemperatureCelsius.Value.ToString("F1") : "-") + "\n" +
            "Throttling: " + (d?.isThrottling.HasValue == true ? d.isThrottling.Value.ToString() : "-") + "\n" +
            "Power State: " + (d?.currentPowerState.HasValue == true ? d.currentPowerState.Value.ToString() : "-") + "\n\n" +
            cores;
    }

    private string BuildMemoryText()
    {
        var s = _currentDevice.memoryStaticInfo;
        var d = _currentDevice.memoryDynamicInfo;

        string modules = s?.moduleSizesMB != null ? string.Join(", ", s.moduleSizesMB.Select(x => x + " MB")) : "-";
        string speeds = s?.moduleSpeedsMHz != null ? string.Join(", ", s.moduleSpeedsMHz.Select(x => x + " MHz")) : "-";
        string types = s?.moduleTypes != null ? string.Join(", ", s.moduleTypes) : "-";

        return
            "Installed RAM: " + (s?.totalPhysicalMemoryMB.ToString() ?? "-") + " MB\n" +
            "Module Count: " + (s?.moduleCount.ToString() ?? "-") + "\n" +
            "Module Sizes: " + modules + "\n" +
            "Module Speeds: " + speeds + "\n" +
            "Module Types: " + types + "\n" +
            "Total Slots: " + (s?.totalSlots.ToString() ?? "-") + "\n\n" +
            "Current Time: " + Safe(d?.currentTime) + "\n" +
            "Used: " + (d != null ? d.usedMemoryMB.ToString("F0") : "-") + " MB\n" +
            "Free: " + (d != null ? d.freeMemoryMB.ToString("F0") : "-") + " MB\n" +
            "Total: " + (d != null ? d.totalMemoryMB.ToString("F0") : "-") + " MB\n" +
            "Usage: " + (d != null ? d.memoryUsagePercent.ToString("F1") : "-") + "%\n" +
            "Cached: " + (d != null ? d.cachedMemoryMB.ToString("F0") : "-") + " MB\n" +
            "Committed: " + (d != null ? d.committedMemoryMB.ToString("F0") : "-") + " MB\n" +
            "Commit Limit: " + (d != null ? d.commitLimitMB.ToString("F0") : "-") + " MB\n" +
            "Page File Usage: " + (d != null ? d.pageFileUsagePercent.ToString("F1") : "-") + "%\n" +
            "Page Faults/sec: " + (d != null ? d.pageFaultsPerSec.ToString("F0") : "-") + "\n" +
            "Memory Pressure: " + (d != null ? d.memoryPressure.ToString("F1") : "-");
    }

    private string BuildGpuText()
    {
        var s = _currentDevice.gpuStaticInfo;
        var d = _currentDevice.gpuDynamicInfo;

        string engines = d?.engineUsages != null
            ? string.Join("\n", d.engineUsages.Select(e => Safe(e.engineName) + ": " + e.usagePercent.ToString("F1") + "%"))
            : "No engine data";

        return
            "Name: " + Safe(s?.name) + "\n" +
            "Manufacturer: " + Safe(s?.manufacturer) + "\n" +
            "Driver Version: " + Safe(s?.driverVersion) + "\n" +
            "Dedicated Memory: " + (s?.dedicatedMemoryMB.ToString() ?? "-") + " MB\n" +
            "Shared Memory: " + (s?.sharedMemoryMB.ToString() ?? "-") + " MB\n\n" +
            "Current Time: " + Safe(d?.currentTime) + "\n" +
            "Total GPU Usage: " + (d != null ? d.totalGpuUsagePercent.ToString("F1") : "-") + "%\n" +
            "Dedicated Memory Used: " + (d != null ? d.dedicatedMemoryUsedMB.ToString("F0") : "-") + " MB\n" +
            "Shared Memory Used: " + (d != null ? d.sharedMemoryUsedMB.ToString("F0") : "-") + " MB\n" +
            "Temperature: " + (d?.temperatureCelsius.HasValue == true ? d.temperatureCelsius.Value.ToString("F1") : "-") + "\n" +
            "Fan Speed: " + (d?.fanSpeedRpm.HasValue == true ? d.fanSpeedRpm.Value.ToString() : "-") + "\n" +
            "Core Clock: " + (d?.coreClockMHz.HasValue == true ? d.coreClockMHz.Value.ToString("F1") : "-") + "\n" +
            "Encoder Usage: " + (d?.encoderUsagePercent.HasValue == true ? d.encoderUsagePercent.Value.ToString("F1") : "-") + "\n" +
            "Decoder Usage: " + (d?.decoderUsagePercent.HasValue == true ? d.decoderUsagePercent.Value.ToString("F1") : "-") + "\n" +
            "Power Draw: " + (d?.powerDrawWatts.HasValue == true ? d.powerDrawWatts.Value.ToString("F1") : "-") + "\n" +
            "VRAM Bandwidth: " + (d?.vramBandwidthPercent.HasValue == true ? d.vramBandwidthPercent.Value.ToString("F1") : "-") + "\n\n" +
            engines;
    }

    private string BuildStorageText()
    {
        var s = _currentDevice.storageStaticInfo;
        var d = _currentDevice.storageDynamicInfo;

        if (s?.drives == null || s.drives.Count == 0)
            return "No drive data.";

        var lines = new List<string>();

        foreach (var drive in s.drives)
        {
            var dyn = d?.drives != null ? d.drives.FirstOrDefault(x => x.driveLetter == drive.driveLetter) : null;

            lines.Add(
                "Drive: " + Safe(drive.driveLetter) + "\n" +
                "Mount Point: " + Safe(drive.mountPoint) + "\n" +
                "File System: " + Safe(drive.fileSystem) + "\n" +
                "Total Size: " + drive.totalSizeGB.ToString("F2") + " GB\n" +
                "Free Space: " + drive.freeSpaceGB.ToString("F2") + " GB\n" +
                "Used Space: " + drive.usedSpaceGB.ToString("F2") + " GB\n" +
                "Used %: " + drive.usedPercent.ToString("F1") + "%\n" +
                "Drive Type: " + Safe(drive.driveType) + "\n" +
                "Volume Name: " + Safe(drive.volumeName) + "\n" +
                (dyn != null
                    ? "\nRead: " + dyn.readMBPerSec.ToString("F2") + " MB/s\n" +
                      "Write: " + dyn.writeMBPerSec.ToString("F2") + " MB/s\n" +
                      "Read IOPS: " + dyn.readIOPS.ToString("F0") + "\n" +
                      "Write IOPS: " + dyn.writeIOPS.ToString("F0") + "\n" +
                      "Total IOPS: " + dyn.totalIOPS.ToString("F0") + "\n" +
                      "Active Time: " + dyn.activeTimePercent.ToString("F1") + "%\n" +
                      "Queue Length: " + dyn.queueLength.ToString("F1") + "\n" +
                      "Latency: " + dyn.avgResponseTimeMs.ToString("F2") + " ms"
                    : "\nNo dynamic data")
            );
        }

        return string.Join("\n\n", lines);
    }

    private string BuildNetworkText()
    {
        var s = _currentDevice.networkStaticInfo;
        var d = _currentDevice.networkDynamicInfo;
        var c = _currentDevice.networkConnectionInfo;
        var a = _currentDevice.networkAdvancedInfo;

        string dns = s?.dnsServers != null ? string.Join(", ", s.dnsServers) : "-";
        string endpoints = c?.topRemoteEndpoints != null ? string.Join("\n", c.topRemoteEndpoints) : "-";

        return
            "Connected: " + (s != null ? s.isInternetConnected.ToString() : "-") + "\n" +
            "Interface Name: " + Safe(s?.interfaceName) + "\n" +
            "Interface Type: " + Safe(s?.interfaceType) + "\n" +
            "MAC Address: " + Safe(s?.macAddress) + "\n" +
            "Local IPv4: " + Safe(s?.localIPv4) + "\n" +
            "IPv6: " + Safe(s?.ipv6) + "\n" +
            "Subnet Mask: " + Safe(s?.subnetMask) + "\n" +
            "Gateway: " + Safe(s?.gateway) + "\n" +
            "DNS Servers: " + dns + "\n" +
            "Link Speed: " + (s?.linkSpeedMbps.ToString() ?? "-") + " Mbps\n" +
            "SSID: " + Safe(s?.ssid) + "\n" +
            "Wi-Fi Signal: " + (s?.wifiSignalStrengthPercent.HasValue == true ? s.wifiSignalStrengthPercent.Value.ToString() : "-") + "\n\n" +
            "Bytes Sent: " + (d?.bytesSentTotal.ToString() ?? "-") + "\n" +
            "Bytes Received: " + (d?.bytesReceivedTotal.ToString() ?? "-") + "\n" +
            "Upload Speed: " + (d != null ? d.uploadSpeedMbps.ToString("F2") : "-") + " Mbps\n" +
            "Download Speed: " + (d != null ? d.downloadSpeedMbps.ToString("F2") : "-") + " Mbps\n" +
            "Packets Sent: " + (d?.packetsSent.ToString() ?? "-") + "\n" +
            "Packets Received: " + (d?.packetsReceived.ToString() ?? "-") + "\n" +
            "Incoming Discarded: " + (d?.incomingPacketsDiscarded.ToString() ?? "-") + "\n" +
            "Outgoing Discarded: " + (d?.outgoingPacketsDiscarded.ToString() ?? "-") + "\n" +
            "Incoming Errors: " + (d?.incomingPacketsErrors.ToString() ?? "-") + "\n" +
            "Outgoing Errors: " + (d?.outgoingPacketsErrors.ToString() ?? "-") + "\n\n" +
            "Open TCP Connections: " + (c?.openTcpConnections.ToString() ?? "-") + "\n" +
            "Established Connections: " + (c?.establishedConnections.ToString() ?? "-") + "\n" +
            "Listening Ports: " + (c?.listeningPorts.ToString() ?? "-") + "\n\n" +
            "Ping: " + (a?.pingLatencyMs.HasValue == true ? a.pingLatencyMs.Value.ToString("F2") : "-") + " ms\n" +
            "Jitter: " + (a?.jitterMs.HasValue == true ? a.jitterMs.Value.ToString("F2") : "-") + " ms\n" +
            "DNS Lookup: " + (a?.dnsLookupMs.HasValue == true ? a.dnsLookupMs.Value.ToString("F2") : "-") + " ms\n" +
            "Public IP: " + Safe(a?.publicIp) + "\n" +
            "VPN Active: " + (a?.isVpnActive.HasValue == true ? a.isVpnActive.Value.ToString() : "-") + "\n\n" +
            "Top Remote Endpoints:\n" + endpoints;
    }

    private string BuildAppsText()
    {
        if (_currentDevice.processes == null || _currentDevice.processes.Count == 0)
            return "No process data.";

        var lines = new List<string>();

        foreach (var p in _currentDevice.processes.Take(maxAppsShown))
        {
            lines.Add(
                Safe(p.processName) + "\n" +
                "PID " + p.pid +
                " | CPU " + p.cpuUsagePercent.ToString("F1") + "%" +
                " | RAM " + p.memoryUsageMB.ToString("F0") + " MB" +
                " | " + Safe(p.status) + "\n" +
                "Threads " + p.threadCount +
                " | Handles " + p.handleCount +
                " | Priority " + Safe(p.priority) +
                " | Session " + p.sessionId +
                "\nStart: " + Safe(p.startTime) +
                "\nDuration: " + Safe(p.runningDuration) +
                "\nExecutable: " + Safe(p.executablePath)
            );
        }

        return string.Join("\n\n", lines);
    }

    private RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return go.GetComponent<RectTransform>();
    }

    private void StretchLeft(RectTransform rect, float width)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.offsetMin = new Vector2(0f, 0f);
        rect.offsetMax = new Vector2(width, 0f);
    }

    private void StretchRemaining(RectTransform rect, float left)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(left, 0f);
        rect.offsetMax = Vector2.zero;
    }

    private void SetRect(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, -top);
    }

    private Text CreateText(string name, Transform parent, int fontSize, FontStyle fontStyle, TextAnchor anchor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = anchor;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = false;
        text.raycastTarget = false;
        return text;
    }

    private string Safe(string value, string fallback = "-")
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private T GetOrAdd<T>(GameObject go) where T : Component
    {
        T existing = go.GetComponent<T>();
        return existing != null ? existing : go.AddComponent<T>();
    }

    private class PanelMoveHandle : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Transform Target { get; set; }

        private Plane _dragPlane;
        private Vector3 _offset;
        private Camera _dragCamera;
        private bool _dragging;

        public void OnPointerDown(PointerEventData eventData)
        {
            BeginDragInternal(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            BeginDragInternal(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging || Target == null || _dragCamera == null)
                return;

            Ray ray = _dragCamera.ScreenPointToRay(eventData.position);

            if (_dragPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPoint = ray.GetPoint(distance);
                Target.position = worldPoint + _offset;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragging = false;
        }

        private void BeginDragInternal(PointerEventData eventData)
        {
            if (Target == null)
                return;

            _dragCamera = eventData.pressEventCamera != null ? eventData.pressEventCamera : eventData.enterEventCamera;
            if (_dragCamera == null)
                return;

            Vector3 planeNormal = Target.forward;
            _dragPlane = new Plane(planeNormal, Target.position);

            Ray ray = _dragCamera.ScreenPointToRay(eventData.position);
            if (_dragPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPoint = ray.GetPoint(distance);
                _offset = Target.position - worldPoint;
                _dragging = true;
            }
        }
    }

    private class PanelResizeHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        public RectTransform Target { get; set; }
        public float MinWidth { get; set; } = 900f;
        public float MinHeight { get; set; } = 600f;
        public float MaxWidth { get; set; } = 2400f;
        public float MaxHeight { get; set; } = 1600f;
        public Action OnResized { get; set; }

        private Vector2 _startSize;
        private Vector2 _startLocalPoint;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Target == null)
                return;

            _startSize = Target.sizeDelta;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Target, eventData.position, eventData.pressEventCamera, out _startLocalPoint);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Target == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Target, eventData.position, eventData.pressEventCamera, out var localPoint))
                return;

            Vector2 delta = localPoint - _startLocalPoint;

            float newWidth = Mathf.Clamp(_startSize.x + delta.x, MinWidth, MaxWidth);
            float newHeight = Mathf.Clamp(_startSize.y - delta.y, MinHeight, MaxHeight);

            Target.sizeDelta = new Vector2(newWidth, newHeight);
            OnResized?.Invoke();
        }
    }

    private class PanelColliderSync : MonoBehaviour
    {
        public RectTransform Target;
        public BoxCollider Collider;
        public float Depth = 0.02f;

        public void Refresh()
        {
            if (Target == null || Collider == null)
                return;

            Vector2 size = Target.rect.size;
            Collider.size = new Vector3(size.x, size.y, Depth);
            Collider.center = Vector3.zero;
        }

        private void LateUpdate()
        {
            Refresh();
        }
    }
}