using System.Runtime.InteropServices;

namespace AutoVibrance;

public class TrayApplication : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly System.Windows.Forms.Timer _processTimer;
    private readonly System.Windows.Forms.Timer _dynamicGammaTimer;
    private readonly ProcessMonitor _processMonitor;
    private readonly DisplaySettings _displaySettings;
    private readonly ScreenLuminanceAnalyzer _luminanceAnalyzer;
    private readonly GammaInterpolator _gammaInterpolator;
    private readonly OverlayForm _overlay;
    private readonly HotkeyWindow _hotkeyWindow;
    private bool _gameWasRunning;

    // Global enable/disable
    private bool _appEnabled = true;

    // Gamma mode
    private enum GammaMode { Off, Static, Dynamic }
    private GammaMode _gammaMode = GammaMode.Dynamic; // Default to dynamic now

    // Toggle states
    private bool _vibranceBoostEnabled = true;
    private bool _overlayEnabled = true;

    private const string TargetProcess = "PioneerGame";
    private const int ProcessPollIntervalMs = 5000;
    private const int DynamicGammaIntervalMs = 150;

    // Display settings
    private const float StaticGamma = 1.70f;
    private const int GameVibrance = 60;
    private const float DefaultGamma = 1.00f;
    private const int DefaultVibrance = 50;

    // Hotkeys
    private const int HOTKEY_TOGGLE_APP = 1;
    private const int HOTKEY_TOGGLE_OVERLAY = 2;
    private const int VK_HOME = 0x24;
    private const int VK_INSERT = 0x2D;

    // Track last values for overlay
    private float _lastLuminance = 0;
    private float _lastGamma = 1.0f;

    public TrayApplication()
    {
        _processMonitor = new ProcessMonitor();
        _displaySettings = new DisplaySettings();
        _luminanceAnalyzer = new ScreenLuminanceAnalyzer();
        _gammaInterpolator = new GammaInterpolator();
        _overlay = new OverlayForm();
        _gameWasRunning = false;

        // Create hotkey handler window
        _hotkeyWindow = new HotkeyWindow();
        _hotkeyWindow.HotkeyPressed += OnHotkeyPressed;
        _hotkeyWindow.RegisterHotkey(HOTKEY_TOGGLE_APP, VK_HOME);
        _hotkeyWindow.RegisterHotkey(HOTKEY_TOGGLE_OVERLAY, VK_INSERT);

        // Create tray icon
        _trayIcon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "Auto Vibrance - Idle",
            Visible = true,
            ContextMenuStrip = CreateContextMenu()
        };

        // Initialize display settings
        try
        {
            _displaySettings.Initialize();
            _displaySettings.ApplySettings(DefaultGamma, DefaultVibrance);
        }
        catch (Exception ex)
        {
            ShowError($"Failed to initialize NVIDIA API: {ex.Message}");
        }

        // Setup timer for process monitoring
        _processTimer = new System.Windows.Forms.Timer
        {
            Interval = ProcessPollIntervalMs
        };
        _processTimer.Tick += OnProcessTimerTick;
        _processTimer.Start();

        // Setup timer for dynamic gamma (starts/stops based on game state)
        _dynamicGammaTimer = new System.Windows.Forms.Timer
        {
            Interval = DynamicGammaIntervalMs
        };
        _dynamicGammaTimer.Tick += OnDynamicGammaTick;

        // Check immediately on startup
        CheckAndApplySettings();
    }

    private void OnHotkeyPressed(int hotkeyId)
    {
        if (hotkeyId == HOTKEY_TOGGLE_APP)
        {
            SetAppEnabled(!_appEnabled);
        }
        else if (hotkeyId == HOTKEY_TOGGLE_OVERLAY)
        {
            ToggleOverlay();
        }
    }

    private void ToggleOverlay()
    {
        _overlayEnabled = !_overlayEnabled;

        // Sync menu checkbox
        if (_trayIcon.ContextMenuStrip?.Items["overlayItem"] is ToolStripMenuItem overlayItem)
        {
            overlayItem.Checked = _overlayEnabled;
        }

        if (_overlayEnabled && _gameWasRunning && _appEnabled && _gammaMode == GammaMode.Dynamic)
        {
            _overlay.SetEnabled(true);
        }
        else
        {
            _overlay.SetEnabled(false);
        }
    }

    private void SetAppEnabled(bool enabled)
    {
        _appEnabled = enabled;

        // Sync menu checkbox (with recursion guard)
        _updatingEnableState = true;
        if (_trayIcon.ContextMenuStrip?.Items["enableItem"] is ToolStripMenuItem enableItem)
        {
            enableItem.Checked = _appEnabled;
        }
        _updatingEnableState = false;

        if (_appEnabled)
        {
            _trayIcon.ShowBalloonTip(1000, "Auto Vibrance", "Enabled", ToolTipIcon.Info);

            // Force re-apply settings and restart timers
            bool gameIsRunning = _processMonitor.IsProcessRunning(TargetProcess);
            _gameWasRunning = gameIsRunning;

            if (gameIsRunning && _gammaMode == GammaMode.Dynamic)
            {
                _gammaInterpolator.Reset();
                _dynamicGammaTimer.Start();
                if (_overlayEnabled) _overlay.SetEnabled(true);
            }

            ApplyCurrentSettings();
        }
        else
        {
            _trayIcon.ShowBalloonTip(1000, "Auto Vibrance", "Disabled", ToolTipIcon.Info);
            _dynamicGammaTimer.Stop();
            _overlay.SetEnabled(false);
            _displaySettings.ApplySettings(DefaultGamma, DefaultVibrance);
            UpdateStatus("Disabled (Home to enable)");
        }
    }

    private Icon LoadIcon()
    {
        // Try PNG first
        var pngPath = Path.Combine(AppContext.BaseDirectory, "Resources", "icon.png");
        if (File.Exists(pngPath))
        {
            using var bitmap = new Bitmap(pngPath);
            // Resize to standard icon size for better display
            using var resized = new Bitmap(bitmap, new Size(32, 32));
            IntPtr hIcon = resized.GetHicon();
            Icon icon = Icon.FromHandle(hIcon);
            // Clone to create an independent icon that won't be affected when handle is destroyed
            return (Icon)icon.Clone();
        }

        // Try ICO as fallback
        var icoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "icon.ico");
        if (File.Exists(icoPath))
        {
            return new Icon(icoPath);
        }

        // Use default application icon
        return SystemIcons.Application;
    }

    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();

        var statusItem = new ToolStripMenuItem("Status: Idle")
        {
            Enabled = false,
            Name = "statusItem"
        };
        menu.Items.Add(statusItem);

        menu.Items.Add(new ToolStripSeparator());

        // Enable/Disable toggle
        var enableItem = new ToolStripMenuItem("Enabled (Home)")
        {
            Name = "enableItem",
            CheckOnClick = true,
            Checked = _appEnabled
        };
        enableItem.CheckedChanged += OnEnableToggle;
        menu.Items.Add(enableItem);

        // Gamma Mode submenu
        var gammaModeMenu = new ToolStripMenuItem("Gamma Mode")
        {
            Name = "gammaModeMenu"
        };

        var gammaOffItem = new ToolStripMenuItem("Off")
        {
            Name = "gammaOffItem",
            Checked = _gammaMode == GammaMode.Off
        };
        gammaOffItem.Click += (s, e) => SetGammaMode(GammaMode.Off);
        gammaModeMenu.DropDownItems.Add(gammaOffItem);

        var gammaStaticItem = new ToolStripMenuItem($"Static Boost ({StaticGamma})")
        {
            Name = "gammaStaticItem",
            Checked = _gammaMode == GammaMode.Static
        };
        gammaStaticItem.Click += (s, e) => SetGammaMode(GammaMode.Static);
        gammaModeMenu.DropDownItems.Add(gammaStaticItem);

        var gammaDynamicItem = new ToolStripMenuItem("Dynamic (Auto)")
        {
            Name = "gammaDynamicItem",
            Checked = _gammaMode == GammaMode.Dynamic
        };
        gammaDynamicItem.Click += (s, e) => SetGammaMode(GammaMode.Dynamic);
        gammaModeMenu.DropDownItems.Add(gammaDynamicItem);

        menu.Items.Add(gammaModeMenu);

        // Vibrance boost checkbox
        var vibranceItem = new ToolStripMenuItem("Vibrance Boost")
        {
            Name = "vibranceItem",
            CheckOnClick = true,
            Checked = _vibranceBoostEnabled
        };
        vibranceItem.CheckedChanged += OnVibranceToggle;
        menu.Items.Add(vibranceItem);

        // Overlay toggle
        var overlayItem = new ToolStripMenuItem("Show Overlay (Insert)")
        {
            Name = "overlayItem",
            CheckOnClick = true,
            Checked = _overlayEnabled
        };
        overlayItem.CheckedChanged += OnOverlayToggle;
        menu.Items.Add(overlayItem);

        menu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("Exit", null, OnExit);
        menu.Items.Add(exitItem);

        return menu;
    }

    private bool _updatingEnableState;

    private void OnEnableToggle(object? sender, EventArgs e)
    {
        if (_updatingEnableState) return; // Prevent recursion

        if (sender is ToolStripMenuItem item)
        {
            _updatingEnableState = true;
            SetAppEnabled(item.Checked);
            _updatingEnableState = false;
        }
    }

    private void OnOverlayToggle(object? sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem item)
        {
            _overlayEnabled = item.Checked;
            if (_overlayEnabled && _gameWasRunning && _appEnabled && _gammaMode == GammaMode.Dynamic)
            {
                _overlay.SetEnabled(true);
            }
            else
            {
                _overlay.SetEnabled(false);
            }
        }
    }

    private void SetGammaMode(GammaMode mode)
    {
        _gammaMode = mode;
        UpdateGammaModeMenuChecks();

        // Handle timer and overlay state based on mode
        if (_gameWasRunning && _appEnabled)
        {
            if (mode == GammaMode.Dynamic)
            {
                _gammaInterpolator.Reset();
                _dynamicGammaTimer.Start();
                if (_overlayEnabled) _overlay.SetEnabled(true);
            }
            else
            {
                _dynamicGammaTimer.Stop();
                _overlay.SetEnabled(false);
                ApplyCurrentSettings();
            }
        }
        else
        {
            _dynamicGammaTimer.Stop();
            _overlay.SetEnabled(false);
        }

        if (mode != GammaMode.Dynamic)
        {
            ApplyCurrentSettings();
        }
    }

    private void UpdateGammaModeMenuChecks()
    {
        if (_trayIcon.ContextMenuStrip?.Items["gammaModeMenu"] is ToolStripMenuItem gammaModeMenu)
        {
            foreach (ToolStripItem item in gammaModeMenu.DropDownItems)
            {
                if (item is ToolStripMenuItem menuItem)
                {
                    menuItem.Checked = item.Name switch
                    {
                        "gammaOffItem" => _gammaMode == GammaMode.Off,
                        "gammaStaticItem" => _gammaMode == GammaMode.Static,
                        "gammaDynamicItem" => _gammaMode == GammaMode.Dynamic,
                        _ => false
                    };
                }
            }
        }
    }

    private void OnVibranceToggle(object? sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem item)
        {
            _vibranceBoostEnabled = item.Checked;
            ApplyCurrentSettings();
        }
    }

    private void OnProcessTimerTick(object? sender, EventArgs e)
    {
        CheckAndApplySettings();
    }

    private void OnDynamicGammaTick(object? sender, EventArgs e)
    {
        if (!_gameWasRunning || _gammaMode != GammaMode.Dynamic || !_appEnabled)
        {
            _dynamicGammaTimer.Stop();
            _overlay.SetEnabled(false);
            return;
        }

        try
        {
            float luminance = _luminanceAnalyzer.GetCenterLuminance();
            float targetGamma = _gammaInterpolator.LuminanceToGamma(luminance);
            float smoothedGamma = _gammaInterpolator.GetSmoothedGamma(targetGamma);

            _lastLuminance = luminance;
            _lastGamma = smoothedGamma;

            _displaySettings.ApplyGammaOnly(smoothedGamma);
            UpdateStatus($"Dynamic - G:{smoothedGamma:F2} L:{luminance:F0}");

            // Update overlay
            if (_overlayEnabled)
            {
                _overlay.UpdateValues(smoothedGamma, luminance);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dynamic gamma error: {ex.Message}");
        }
    }

    private void CheckAndApplySettings()
    {
        if (!_appEnabled) return;

        try
        {
            bool gameIsRunning = _processMonitor.IsProcessRunning(TargetProcess);

            // Update menu visibility when game state changes
            if (gameIsRunning != _gameWasRunning)
            {
                OnGameStateChanged(gameIsRunning);
                _gameWasRunning = gameIsRunning;
            }

            // Apply settings based on current state (skip if dynamic mode is handling gamma)
            if (_gammaMode != GammaMode.Dynamic || !_gameWasRunning)
            {
                ApplyCurrentSettings();
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error: {ex.Message}");
        }
    }

    private void OnGameStateChanged(bool gameIsRunning)
    {
        if (gameIsRunning && _gammaMode == GammaMode.Dynamic && _appEnabled)
        {
            _gammaInterpolator.Reset();
            _dynamicGammaTimer.Start();
            if (_overlayEnabled) _overlay.SetEnabled(true);
        }
        else
        {
            _dynamicGammaTimer.Stop();
            _gammaInterpolator.Reset();
            _overlay.SetEnabled(false);
        }
    }

    private void ApplyCurrentSettings()
    {
        if (!_appEnabled)
        {
            _displaySettings.ApplySettings(DefaultGamma, DefaultVibrance);
            return;
        }

        float gamma;
        int vibrance;

        if (_gameWasRunning)
        {
            gamma = _gammaMode switch
            {
                GammaMode.Off => DefaultGamma,
                GammaMode.Static => StaticGamma,
                GammaMode.Dynamic => _gammaInterpolator.CurrentGamma,
                _ => DefaultGamma
            };
            vibrance = _vibranceBoostEnabled ? GameVibrance : DefaultVibrance;

            if (_gammaMode == GammaMode.Dynamic)
            {
                // Status updated by dynamic timer
            }
            else
            {
                UpdateStatus($"Game Active - Gamma: {gamma}, Vibrance: {vibrance}%");
            }
        }
        else
        {
            gamma = DefaultGamma;
            vibrance = DefaultVibrance;
            UpdateStatus($"Idle - Gamma: {gamma}, Vibrance: {vibrance}%");
        }

        _displaySettings.ApplySettings(gamma, vibrance);
    }

    private void UpdateStatus(string status)
    {
        _trayIcon.Text = $"Auto Vibrance - {(status.Length > 50 ? status[..50] + "..." : status)}";

        if (_trayIcon.ContextMenuStrip?.Items["statusItem"] is ToolStripMenuItem statusItem)
        {
            statusItem.Text = $"Status: {status}";
        }
    }

    private void OnExit(object? sender, EventArgs e)
    {
        // Restore default settings before exiting
        try
        {
            _displaySettings.ApplySettings(DefaultGamma, DefaultVibrance);
        }
        catch { }

        _hotkeyWindow.UnregisterHotkey(HOTKEY_TOGGLE_APP);
        _hotkeyWindow.UnregisterHotkey(HOTKEY_TOGGLE_OVERLAY);
        _hotkeyWindow.Dispose();
        _processTimer.Stop();
        _processTimer.Dispose();
        _dynamicGammaTimer.Stop();
        _dynamicGammaTimer.Dispose();
        _luminanceAnalyzer.Dispose();
        _overlay.Close();
        _overlay.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _displaySettings.Dispose();
        Application.Exit();
    }

    private void ShowError(string message)
    {
        _trayIcon.ShowBalloonTip(3000, "Auto Vibrance Error", message, ToolTipIcon.Error);
    }
}

// Hidden window for handling global hotkeys
internal class HotkeyWindow : NativeWindow, IDisposable
{
    private const int WM_HOTKEY = 0x0312;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public event Action<int>? HotkeyPressed;

    public HotkeyWindow()
    {
        CreateHandle(new CreateParams());
    }

    public void RegisterHotkey(int id, int key)
    {
        RegisterHotKey(Handle, id, 0, (uint)key);
    }

    public void UnregisterHotkey(int id)
    {
        UnregisterHotKey(Handle, id);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY)
        {
            int hotkeyId = m.WParam.ToInt32();
            HotkeyPressed?.Invoke(hotkeyId);
        }
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        DestroyHandle();
    }
}
