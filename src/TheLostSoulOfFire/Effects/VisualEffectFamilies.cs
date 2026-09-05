using System;
using System.Collections.Generic;

namespace TheLostSoulOfFire.Effects;

public enum VisualEffectPriority
{
    Decorative,
    Combat,
    Critical
}

public enum VisualEffectBlend
{
    Alpha,
    Additive
}

/// <summary>
/// The ownership and presentation contract for every existing sprite-VFX sheet.
/// Gameplay code chooses an event; this table chooses the cosmetic family details.
/// </summary>
public sealed record VisualEffectFamily(
    string Key,
    VisualEffectBlend Blend,
    VisualEffectPriority Priority,
    float GlowRadius,
    float GlowIntensity);

public static class VisualEffectFamilies
{
    private static readonly Dictionary<string, VisualEffectFamily> ByKey =
        new Dictionary<string, VisualEffectFamily>(StringComparer.Ordinal)
        {
            ["burning_detonation"] = new("burning_detonation", VisualEffectBlend.Additive, VisualEffectPriority.Critical, 96f, 0.35f),
            ["cannon_charge_loop"] = new("cannon_charge_loop", VisualEffectBlend.Additive, VisualEffectPriority.Combat, 62f, 0.24f),
            ["cannon_muzzle_full"] = new("cannon_muzzle_full", VisualEffectBlend.Additive, VisualEffectPriority.Critical, 88f, 0.42f),
            ["cannon_projectile_full"] = new("cannon_projectile_full", VisualEffectBlend.Additive, VisualEffectPriority.Critical, 70f, 0.34f),
            ["core_hit"] = new("core_hit", VisualEffectBlend.Additive, VisualEffectPriority.Critical, 64f, 0.38f),
            ["dash_ignition"] = new("dash_ignition", VisualEffectBlend.Additive, VisualEffectPriority.Combat, 42f, 0.16f),
            ["death_flame_loop"] = new("death_flame_loop", VisualEffectBlend.Alpha, VisualEffectPriority.Decorative, 38f, 0.1f),
            ["resonance_activate"] = new("resonance_activate", VisualEffectBlend.Additive, VisualEffectPriority.Critical, 128f, 0.38f),
            ["scythe_cleave"] = new("scythe_cleave", VisualEffectBlend.Additive, VisualEffectPriority.Critical, 72f, 0.3f),
            ["scythe_slash_01"] = new("scythe_slash_01", VisualEffectBlend.Alpha, VisualEffectPriority.Combat, 38f, 0.12f),
            ["scythe_slash_02"] = new("scythe_slash_02", VisualEffectBlend.Additive, VisualEffectPriority.Combat, 48f, 0.18f),
            ["soul_release"] = new("soul_release", VisualEffectBlend.Additive, VisualEffectPriority.Critical, 72f, 0.28f)
        };

    public static IReadOnlyCollection<VisualEffectFamily> All => ByKey.Values;

    public static VisualEffectFamily Get(string key) => ByKey.TryGetValue(key, out VisualEffectFamily family)
        ? family
        : throw new ArgumentOutOfRangeException(nameof(key), key, "No visual-effect family is registered for this sprite sheet.");
}
