using System.Drawing.Imaging;

namespace AutoVibrance;

public class ScreenLuminanceAnalyzer : IDisposable
{
    private const int SampleRegionSize = 400;  // 400x400 center region (~4.3% of 1440p)
    private const int SampleStride = 8;        // Sample every 8th pixel (2500 samples)

    private Bitmap? _sampleBitmap;
    private float _lastLuminance = 128f;

    public float GetCenterLuminance()
    {
        try
        {
            var screenBounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
            int centerX = (screenBounds.Width - SampleRegionSize) / 2;
            int centerY = (screenBounds.Height - SampleRegionSize) / 2;

            // Reuse bitmap to avoid GC pressure
            _sampleBitmap ??= new Bitmap(SampleRegionSize, SampleRegionSize, PixelFormat.Format24bppRgb);

            using var g = Graphics.FromImage(_sampleBitmap);
            g.CopyFromScreen(centerX, centerY, 0, 0, new Size(SampleRegionSize, SampleRegionSize));

            float luminance = CalculateAverageLuminance(_sampleBitmap);
            _lastLuminance = luminance;
            return luminance;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Screen capture failed: {ex.Message}");
            return _lastLuminance;
        }
    }

    private float CalculateAverageLuminance(Bitmap bitmap)
    {
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

        try
        {
            double totalLuminance = 0;
            int sampleCount = 0;
            int stride = data.Stride;
            int bytesPerPixel = 3;

            unsafe
            {
                byte* ptr = (byte*)data.Scan0;

                for (int y = 0; y < bitmap.Height; y += SampleStride)
                {
                    for (int x = 0; x < bitmap.Width; x += SampleStride)
                    {
                        int offset = y * stride + x * bytesPerPixel;
                        byte b = ptr[offset];
                        byte g = ptr[offset + 1];
                        byte r = ptr[offset + 2];

                        // Standard luminance formula
                        double luminance = 0.299 * r + 0.587 * g + 0.114 * b;
                        totalLuminance += luminance;
                        sampleCount++;
                    }
                }
            }

            return sampleCount > 0 ? (float)(totalLuminance / sampleCount) : 128f;
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    public void Dispose()
    {
        _sampleBitmap?.Dispose();
        _sampleBitmap = null;
    }
}
