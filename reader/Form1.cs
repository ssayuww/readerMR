using reader.Services;

namespace reader;

using System;
using System.Drawing;
using System.Windows.Forms;


public partial class Form1 : Form
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _trayMenu;

    private bool _reallyClose;
    private bool _isActive = false;
    
    private MonitoringService _monitoringService;
    

    public Form1()
    {
        InitializeComponent();

        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        
        _monitoringService = new MonitoringService();

        _trayMenu = new ContextMenuStrip();
        _trayMenu.Items.Add("Activate", null, Activate_Click);
        _trayMenu.Items.Add("Deactivate", null, Deactivate_Click);
        _trayMenu.Items.Add("Exit", null, Exit_Click);

        _notifyIcon = new NotifyIcon
        {
            Text = "Reader",
            Icon = new Icon("icon.ico"),
            Visible = true,
            ContextMenuStrip = _trayMenu
        };

        UpdateTrayMenu();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Hide();
    }

    private void Activate_Click(object? sender, EventArgs e)
    {
        if (_isActive)
            return;

        _isActive = true;
        UpdateTrayMenu();


        _monitoringService.Start();
    }

    private void Deactivate_Click(object? sender, EventArgs e)
    {
        if (!_isActive)
            return;

        _isActive = false;
        UpdateTrayMenu();
        
        _monitoringService.Stop();
    }

    private void Exit_Click(object? sender, EventArgs e)
    {
        _reallyClose = true;
        _notifyIcon.Visible = false;
        Close();
    }

    private void UpdateTrayMenu()
    {
        _trayMenu.Items[0].Enabled = !_isActive;
        _trayMenu.Items[1].Enabled = _isActive;
    }

    private void HideToTray()
    {
        ShowInTaskbar = false;
        Hide();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (WindowState == FormWindowState.Minimized)
        {
            HideToTray();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_reallyClose)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _notifyIcon.Dispose();
        _trayMenu.Dispose();
        base.OnFormClosed(e);
    }
}