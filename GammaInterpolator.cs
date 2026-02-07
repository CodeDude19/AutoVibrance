namespace AutoVibrance;

public class GammaInterpolator
{
    // Gamma range for dynamic adjustment
    public float MinGamma { get; set; } = 1.0f;   // For bright scenes
    public float MaxGamma { get; set; } = 2.0f;   // For dark scenes

    // Smoothing factor (0.0 = no change, 1.0 = instant)
    public float SmoothFactor { get; set; } = 0.1f;

    // Luminance thresholds
    public float DarkThreshold { get; set; } = 60f;    // Below this = max gamma
    public float BrightThreshold { get; set; } = 180f; // Above this = min gamma

    private float _currentGamma = 1.0f;

    /// <summary>
    /// Maps luminance to target gamma using inverse relationship.
    /// Dark scenes get higher gamma, bright scenes get lower gamma.
    /// </summary>
    public float LuminanceToGamma(float luminance)
    {
        if (luminance <= DarkThreshold)
            return MaxGamma;

        if (luminance >= BrightThreshold)
            return MinGamma;

        // Linear interpolation between thresholds
        float t = (luminance - DarkThreshold) / (BrightThreshold - DarkThreshold);
        return MaxGamma - t * (MaxGamma - MinGamma);
    }

    /// <summary>
    /// Applies exponential smoothing for gradual camera-like transitions.
    /// </summary>
    public float GetSmoothedGamma(float targetGamma)
    {
        _currentGamma += (targetGamma - _currentGamma) * SmoothFactor;
        return _currentGamma;
    }

    /// <summary>
    /// Resets the current gamma to default (1.0).
    /// Call when switching modes or game exits.
    /// </summary>
    public void Reset()
    {
        _currentGamma = 1.0f;
    }

    /// <summary>
    /// Gets the current smoothed gamma value without updating it.
    /// </summary>
    public float CurrentGamma => _currentGamma;
}
