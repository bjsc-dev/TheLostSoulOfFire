using System;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// Run-local presentation controls. They deliberately affect only drawing and cosmetic
/// emission: combat timing, damage and hitstop continue to use their gameplay values.
/// </summary>
public enum VisualQuality
{
    Baseline,
    High
}

public sealed class PresentationSettings
{
    public VisualQuality Quality { get; private set; } = VisualQuality.High;
    public bool ReducedEffects { get; private set; }

    public float CameraMotionScale => ReducedEffects ? 0f : 1f;
    public float FlashScale => ReducedEffects ? 0f : 1f;
    public float ParticleDensityScale => ReducedEffects ? 0.5f : 1f;
    public float GlowIntensityScale => ReducedEffects ? 0.78f : 1f;
    public float VignetteScale => ReducedEffects ? 0.82f : 1f;
    public bool UsesSoftEmission => Quality == VisualQuality.High && !ReducedEffects;

    public string Summary => $"{Quality.ToString().ToUpperInvariant()} {(ReducedEffects ? "REDUCED" : "FULL")}";

    public void SetQuality(VisualQuality quality) => Quality = quality;

    public void SetReducedEffects(bool reducedEffects) => ReducedEffects = reducedEffects;

    public void ToggleQuality() => Quality = Quality == VisualQuality.High
        ? VisualQuality.Baseline
        : VisualQuality.High;

    public void ToggleReducedEffects() => ReducedEffects = !ReducedEffects;

    public static bool TryParseQuality(string value, out VisualQuality quality)
    {
        if (string.Equals(value, "baseline", StringComparison.OrdinalIgnoreCase))
        {
            quality = VisualQuality.Baseline;
            return true;
        }

        if (string.Equals(value, "high", StringComparison.OrdinalIgnoreCase))
        {
            quality = VisualQuality.High;
            return true;
        }

        quality = VisualQuality.High;
        return false;
    }
}
