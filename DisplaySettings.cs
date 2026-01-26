using System.Runtime.InteropServices;
using NvAPIWrapper;
using NvAPIWrapper.Display;

namespace AutoVibrance;

public class DisplaySettings : IDisposable
{
    private bool _initialized;
    private Display[] _nvidiaDisplays = [];

    // Windows GDI for gamma control
    [DllImport("gdi32.dll")]
    private static extern bool SetDeviceGammaRamp(IntPtr hDC, ref GammaRamp lpRamp);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr CreateDC(string lpszDriver, string lpszDevice, string? lpszOutput, IntPtr lpInitData);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct GammaRamp
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public ushort[] Red;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public ushort[] Green;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public ushort[] Blue;
    }

    public void Initialize()
    {
        try
        {
            NVIDIA.Initialize();
            _nvidiaDisplays = Display.GetDisplays();
            _initialized = true;
        }
        catch (Exception ex)
        {
            // NVAPI might fail if no NVIDIA GPU, but gamma will still work
            System.Diagnostics.Debug.WriteLine($"NVIDIA API init failed (gamma still works): {ex.Message}");
            _initialized = false;
        }
    }

    public void ApplySettings(float gamma, int vibrancePercent)
    {
        // Apply gamma to ALL monitors (works regardless of GPU)
        SetGammaAllMonitors(gamma);

        // Apply vibrance to ALL NVIDIA displays
        if (_initialized && _nvidiaDisplays.Length > 0)
        {
            SetDigitalVibranceAllDisplays(vibrancePercent);
        }
    }

    private void SetDigitalVibranceAllDisplays(int level)
    {
        int targetLevel = Math.Clamp(level, 0, 100);

        foreach (var display in _nvidiaDisplays)
        {
            try
            {
                var dvc = display.DigitalVibranceControl;
                dvc.CurrentLevel = targetLevel;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to set vibrance on {display.Name}: {ex.Message}");
            }
        }
    }

    private GammaRamp _currentRamp;

    private void SetGammaAllMonitors(float gamma)
    {
        // Build gamma ramp
        _currentRamp = new GammaRamp
        {
            Red = new ushort[256],
            Green = new ushort[256],
            Blue = new ushort[256]
        };

        for (int i = 0; i < 256; i++)
        {
            double value = Math.Pow(i / 255.0, 1.0 / gamma) * 65535.0;
            value = Math.Clamp(value, 0, 65535);
            ushort rampValue = (ushort)value;
            _currentRamp.Red[i] = rampValue;
            _currentRamp.Green[i] = rampValue;
            _currentRamp.Blue[i] = rampValue;
        }

        // Apply to all monitors
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumCallback, IntPtr.Zero);
    }

    private bool MonitorEnumCallback(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
    {
        var monitorInfo = new MONITORINFOEX();
        monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));

        if (GetMonitorInfo(hMonitor, ref monitorInfo))
        {
            IntPtr hdc = CreateDC("DISPLAY", monitorInfo.szDevice, null, IntPtr.Zero);
            if (hdc != IntPtr.Zero)
            {
                SetDeviceGammaRamp(hdc, ref _currentRamp);
                DeleteDC(hdc);
            }
        }
        return true; // Continue enumeration
    }

    public void Dispose()
    {
        if (_initialized)
        {
            try
            {
                NVIDIA.Unload();
            }
            catch { }
            _initialized = false;
        }
    }
}
