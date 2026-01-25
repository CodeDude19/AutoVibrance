using System.Runtime.InteropServices;
using NvAPIWrapper;
using NvAPIWrapper.Display;

namespace AutoVibrance;

public class DisplaySettings : IDisposable
{
    private bool _initialized;
    private Display? _primaryDisplay;

    // Windows GDI for gamma control
    [DllImport("gdi32.dll")]
    private static extern bool SetDeviceGammaRamp(IntPtr hDC, ref GammaRamp lpRamp);

    [DllImport("gdi32.dll")]
    private static extern bool GetDeviceGammaRamp(IntPtr hDC, ref GammaRamp lpRamp);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

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
            var displays = Display.GetDisplays();
            _primaryDisplay = displays.FirstOrDefault();
            _initialized = true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to initialize NVIDIA API: {ex.Message}", ex);
        }
    }

    public void ApplySettings(float gamma, int vibrancePercent)
    {
        SetGamma(gamma);

        if (_initialized && _primaryDisplay != null)
        {
            SetDigitalVibrance(vibrancePercent);
        }
    }

    private void SetDigitalVibrance(int level)
    {
        if (_primaryDisplay == null) return;

        try
        {
            // NvAPIWrapper digital vibrance control
            // Level is typically 0-100 where 50 is default
            var dvc = _primaryDisplay.DigitalVibranceControl;

            // Clamp to valid range
            int targetLevel = Math.Clamp(level, 0, 100);
            dvc.CurrentLevel = targetLevel;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to set digital vibrance: {ex.Message}");
        }
    }

    private void SetGamma(float gamma)
    {
        IntPtr hDC = GetDC(IntPtr.Zero);
        if (hDC == IntPtr.Zero) return;

        try
        {
            var ramp = new GammaRamp
            {
                Red = new ushort[256],
                Green = new ushort[256],
                Blue = new ushort[256]
            };

            // Build gamma ramp
            for (int i = 0; i < 256; i++)
            {
                // Apply gamma correction formula
                double value = Math.Pow(i / 255.0, 1.0 / gamma) * 65535.0;
                value = Math.Clamp(value, 0, 65535);

                ushort rampValue = (ushort)value;
                ramp.Red[i] = rampValue;
                ramp.Green[i] = rampValue;
                ramp.Blue[i] = rampValue;
            }

            SetDeviceGammaRamp(hDC, ref ramp);
        }
        finally
        {
            ReleaseDC(IntPtr.Zero, hDC);
        }
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
