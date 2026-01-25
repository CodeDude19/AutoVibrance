namespace AutoVibrance;

public class TrayApplication : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly ProcessMonitor _processMonitor;
    private readonly DisplaySettings _displaySettings;
    private bool _gameWasRunning;

    // Toggle states
    private bool _gammaBoostEnabled = true;
    private bool _vibranceBoostEnabled = true;

    private const string TargetProcess = "PioneerGame";
    private const int PollIntervalMs = 5000;

    // Display settings
    private const float GameGamma = 1.70f;
    private const int GameVibrance = 60;
    private const float DefaultGamma = 1.00f;
    private const int DefaultVibrance = 50;

    public TrayApplication()
    {
        _processMonitor = new ProcessMonitor();
        _displaySettings = new DisplaySettings();
        _gameWasRunning = false;

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
        _timer = new System.Windows.Forms.Timer
        {
            Interval = PollIntervalMs
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();

        // Check immediately on startup
        CheckAndApplySettings();
    }

    private Icon LoadIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "icon.ico");
        if (File.Exists(iconPath))
        {
            return new Icon(iconPath);
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

        // Gamma boost checkbox
        var gammaItem = new ToolStripMenuItem("Gamma Boost")
        {
            Name = "gammaItem",
            CheckOnClick = true,
            Checked = _gammaBoostEnabled,
            Visible = false
        };
        gammaItem.CheckedChanged += OnGammaToggle;
        menu.Items.Add(gammaItem);

        // Vibrance boost checkbox
        var vibranceItem = new ToolStripMenuItem("Vibrance Boost")
        {
            Name = "vibranceItem",
            CheckOnClick = true,
            Checked = _vibranceBoostEnabled,
            Visible = false
        };
        vibranceItem.CheckedChanged += OnVibranceToggle;
        menu.Items.Add(vibranceItem);

        // Separator before exit (only shown when game is running)
        var boostSeparator = new ToolStripSeparator
        {
            Name = "boostSeparator",
            Visible = false
        };
        menu.Items.Add(boostSeparator);

        var exitItem = new ToolStripMenuItem("Exit", null, OnExit);
        menu.Items.Add(exitItem);

        return menu;
    }

    private void OnGammaToggle(object? sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem item)
        {
            _gammaBoostEnabled = item.Checked;
            ApplyCurrentSettings();
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

    private void OnTimerTick(object? sender, EventArgs e)
    {
        CheckAndApplySettings();
    }

    private void CheckAndApplySettings()
    {
        try
        {
            bool gameIsRunning = _processMonitor.IsProcessRunning(TargetProcess);

            // Update menu visibility when game state changes
            if (gameIsRunning != _gameWasRunning)
            {
                UpdateMenuVisibility(gameIsRunning);
                _gameWasRunning = gameIsRunning;
            }

            // Always apply settings based on current state
            ApplyCurrentSettings();
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error: {ex.Message}");
        }
    }

    private void ApplyCurrentSettings()
    {
        float gamma;
        int vibrance;

        if (_gameWasRunning)
        {
            gamma = _gammaBoostEnabled ? GameGamma : DefaultGamma;
            vibrance = _vibranceBoostEnabled ? GameVibrance : DefaultVibrance;
            UpdateStatus($"Game Active - Gamma: {gamma}, Vibrance: {vibrance}%");
        }
        else
        {
            gamma = DefaultGamma;
            vibrance = DefaultVibrance;
            UpdateStatus($"Idle - Gamma: {gamma}, Vibrance: {vibrance}%");
        }

        _displaySettings.ApplySettings(gamma, vibrance);
    }

    private void UpdateMenuVisibility(bool gameIsRunning)
    {
        if (_trayIcon.ContextMenuStrip == null) return;

        if (_trayIcon.ContextMenuStrip.Items["gammaItem"] is ToolStripMenuItem gammaItem)
        {
            gammaItem.Visible = gameIsRunning;
        }

        if (_trayIcon.ContextMenuStrip.Items["vibranceItem"] is ToolStripMenuItem vibranceItem)
        {
            vibranceItem.Visible = gameIsRunning;
        }

        if (_trayIcon.ContextMenuStrip.Items["boostSeparator"] is ToolStripSeparator separator)
        {
            separator.Visible = gameIsRunning;
        }
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

        _timer.Stop();
        _timer.Dispose();
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
