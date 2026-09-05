using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Effects;

/// <summary>
/// Small, intentionally narrow tuning surface for the authored arena atmosphere.
/// Values describe presentation density, not gameplay behavior.
/// </summary>
public static class ArenaAtmosphereTuning
{
    public const float AmbientIntensity = 0.72f;
    public const float AshDensity = 0.65f;
    public const float EmberDensity = 0.42f;
    public const float FurnacePulseStrength = 0.14f;
    public const float HazeOpacity = 0.07f;
    public const float CompletionCalm = 0.22f;
}

/// <summary>
/// Owns restrained, arena-specific environmental motion and secondary reactions.
/// Particle storage is fixed and all source locations correspond to machinery in
/// the authored Abandoned Soul Furnace background.
/// </summary>
public sealed class ArenaAtmosphere
{
    private enum AmbientParticleKind : byte
    {
        Ash,
        Smoke,
        Ember,
        SoulMote
    }

    private struct AmbientParticle
    {
        public bool Active;
        public AmbientParticleKind Kind;
        public Vector2 Position;
        public Vector2 Velocity;
        public float Lifetime;
        public float Remaining;
        public float StartSize;
        public float EndSize;
        public float Opacity;
        public float Phase;
        public float Depth;
    }

    private readonly record struct FurnaceSource(
        Vector2 Position,
        float Radius,
        float Phase,
        float Strength,
        bool Circular);

    private readonly record struct HazeBand(
        Vector2 Start,
        Vector2 End,
        float Width,
        float Phase,
        float Speed);

    private const int ParticleCapacity = 40;
    private const int MaximumAsh = 14;
    private const int MaximumSmoke = 6;
    private const int MaximumEmbers = 5;

    // Dark furnace mouths and damaged exhausts visible in arena_base_1800x1000.
    private static readonly FurnaceSource[] FurnaceSources =
    [
        new(new Vector2(919f, 187f), 54f, 0.1f, 1f, false),
        new(new Vector2(1167f, 158f), 44f, 2.15f, 0.72f, false),
        new(new Vector2(263f, 867f), 34f, 4.2f, 0.46f, true)
    ];

    // These remain outside the central combat basin and move at different rates.
    private static readonly HazeBand[] HazeBands =
    [
        new(new Vector2(122f, 286f), new Vector2(470f, 246f), 42f, 0.4f, 0.11f),
        new(new Vector2(1365f, 287f), new Vector2(1732f, 340f), 48f, 2.1f, 0.075f),
        new(new Vector2(112f, 764f), new Vector2(425f, 805f), 58f, 4.5f, 0.055f),
        new(new Vector2(1395f, 782f), new Vector2(1728f, 727f), 52f, 5.8f, 0.085f)
    ];

    private static readonly Vector2[] SmokeSources =
    [
        new(491f, 174f),
        new(1168f, 169f),
        new(273f, 843f),
        new(1370f, 858f)
    ];

    private static readonly Vector2[] SoulConduits =
    [
        new(785f, 132f),
        new(1012f, 134f),
        new(1370f, 54f)
    ];

    private readonly AmbientParticle[] _particles = new AmbientParticle[ParticleCapacity];
    private readonly Random _random = new(4129);
    private float _time;
    private float _activity = 1f;
    private float _ashSpawnTimer;
    private float _smokeSpawnTimer;
    private float _emberSpawnTimer;
    private float _soulMoteTimer;
    private float _machineFaultTimer;
    private float _machineFaultRemaining;
    private float _forcePressure;
    private float _resonancePressure;
    private int _nextParticleSlot;

    public ArenaAtmosphere()
    {
        Reset();
    }

    public void Reset()
    {
        Array.Clear(_particles);
        _time = 0f;
        _activity = 1f;
        _ashSpawnTimer = 0.3f;
        _smokeSpawnTimer = 0.55f;
        _emberSpawnTimer = 0.9f;
        _soulMoteTimer = RandomRange(7.5f, 11.5f);
        _machineFaultTimer = RandomRange(5.5f, 8.5f);
        _machineFaultRemaining = 0f;
        _forcePressure = 0f;
        _resonancePressure = 0f;
        _nextParticleSlot = 0;

        // Start with a lived-in frame instead of waiting several seconds for the first layer.
        for (int i = 0; i < 9; i++)
        {
            SpawnAsh(true);
        }
        for (int i = 0; i < 3; i++)
        {
            SpawnSmoke(true);
        }
        for (int i = 0; i < 2; i++)
        {
            SpawnEmber(true);
        }
    }

    public void Update(float deltaTime, bool calming)
    {
        deltaTime = MathF.Max(0f, deltaTime);
        _time += deltaTime;

        float targetActivity = calming ? ArenaAtmosphereTuning.CompletionCalm : 1f;
        float calmSpeed = calming ? 0.72f : 1.8f;
        _activity = MathHelper.Lerp(
            _activity,
            targetActivity,
            1f - MathF.Exp(-deltaTime * calmSpeed));
        _forcePressure = MathF.Max(0f, _forcePressure - deltaTime * 2.2f);
        _resonancePressure = MathF.Max(0f, _resonancePressure - deltaTime * 0.7f);

        UpdateMachineFault(deltaTime, calming);
        UpdateSpawning(deltaTime, calming);

        for (int i = 0; i < _particles.Length; i++)
        {
            ref AmbientParticle particle = ref _particles[i];
            if (!particle.Active)
            {
                continue;
            }

            particle.Remaining -= deltaTime;
            if (particle.Remaining <= 0f || IsOutsideArena(particle.Position))
            {
                particle.Active = false;
                continue;
            }

            float calmMotion = MathHelper.Lerp(0.72f, 1f, _activity);
            float pressureSpeed = calmMotion + _forcePressure * 0.24f + _resonancePressure * 0.1f;
            switch (particle.Kind)
            {
                case AmbientParticleKind.Ash:
                    particle.Position += particle.Velocity * (deltaTime * particle.Depth * pressureSpeed);
                    particle.Position.X += MathF.Sin(_time * 0.52f + particle.Phase) * deltaTime * 3.2f;
                    break;

                case AmbientParticleKind.Smoke:
                    particle.Position += particle.Velocity * (deltaTime * pressureSpeed);
                    particle.Position.X += MathF.Sin(_time * 0.38f + particle.Phase) * deltaTime * 5f;
                    break;

                case AmbientParticleKind.Ember:
                    particle.Position += particle.Velocity * (deltaTime * pressureSpeed);
                    particle.Velocity.Y -= deltaTime * 5f;
                    break;

                case AmbientParticleKind.SoulMote:
                    // Residual Soul movement is subtly wrong: sideways and occasionally downward.
                    particle.Position += particle.Velocity * deltaTime;
                    particle.Position.X += MathF.Sin(_time * 1.35f + particle.Phase) * deltaTime * 8f;
                    particle.Position.Y += MathF.Cos(_time * 0.83f + particle.Phase) * deltaTime * 3f;
                    break;
            }
        }
    }

    public void ReactToResonance()
    {
        _resonancePressure = 1f;
        _machineFaultTimer = MathF.Max(_machineFaultTimer, 1.2f);
    }

    public void ReactToForce(Vector2 origin, float radius, float strength)
    {
        if (radius <= 0f || strength <= 0f)
        {
            return;
        }

        float radiusSquared = radius * radius;
        for (int i = 0; i < _particles.Length; i++)
        {
            ref AmbientParticle particle = ref _particles[i];
            if (!particle.Active)
            {
                continue;
            }

            Vector2 away = particle.Position - origin;
            float distanceSquared = away.LengthSquared();
            if (distanceSquared >= radiusSquared)
            {
                continue;
            }

            float distance = MathF.Sqrt(distanceSquared);
            Vector2 direction = distance > 0.01f
                ? away / distance
                : new Vector2(MathF.Cos(particle.Phase), MathF.Sin(particle.Phase));
            float falloff = 1f - distance / radius;
            particle.Velocity += direction * (strength * falloff * (0.55f + particle.Depth * 0.45f));
        }

        _forcePressure = MathF.Max(_forcePressure, MathHelper.Clamp(strength / 190f, 0f, 1f));
    }

    public void DrawBackground(
        SpriteBatch batch,
        Texture2D pixel,
        float soulSenseAmount)
    {
        float sense = MathHelper.Clamp(soulSenseAmount, 0f, 1f);
        float physicalVisibility = MathHelper.Lerp(1f, 0.32f, sense);

        DrawHaze(batch, pixel, physicalVisibility);
        DrawFurnaceFaces(batch, pixel, physicalVisibility);

        for (int i = 0; i < _particles.Length; i++)
        {
            ref readonly AmbientParticle particle = ref _particles[i];
            if (!particle.Active)
            {
                continue;
            }

            DrawParticle(batch, pixel, particle, physicalVisibility, sense);
        }
    }

    public void DrawLighting(
        SpriteBatch batch,
        SoulfireRenderer renderer,
        float soulSenseAmount)
    {
        float sense = MathHelper.Clamp(soulSenseAmount, 0f, 1f);
        float physicalVisibility = MathHelper.Lerp(1f, 0.2f, sense);
        float resonanceSuppression = 1f - _resonancePressure * 0.14f;

        for (int i = 0; i < FurnaceSources.Length; i++)
        {
            FurnaceSource source = FurnaceSources[i];
            float pulse = GetFurnacePulse(i);
            float intensity = (0.035f + pulse * 0.028f) *
                source.Strength *
                ArenaAtmosphereTuning.AmbientIntensity *
                physicalVisibility *
                resonanceSuppression *
                MathHelper.Lerp(0.45f, 1f, _activity);
            renderer.DrawGlow(
                batch,
                source.Position,
                source.Radius,
                GameBalance.DeathFlame,
                intensity);
        }

        for (int i = 0; i < _particles.Length; i++)
        {
            ref readonly AmbientParticle particle = ref _particles[i];
            if (!particle.Active || particle.Kind != AmbientParticleKind.SoulMote)
            {
                continue;
            }

            float lifeAlpha = GetLifeAlpha(particle);
            renderer.DrawGlow(
                batch,
                particle.Position,
                22f * particle.Depth,
                GameBalance.DeathFlame,
                lifeAlpha * MathHelper.Lerp(0.035f, 0.1f, sense));
        }
    }

    private void UpdateMachineFault(float deltaTime, bool calming)
    {
        _machineFaultTimer -= deltaTime;
        _machineFaultRemaining = MathF.Max(0f, _machineFaultRemaining - deltaTime);
        if (_machineFaultTimer > 0f)
        {
            return;
        }

        if (!calming)
        {
            _machineFaultRemaining = 0.24f;
        }
        _machineFaultTimer = RandomRange(calming ? 10f : 6.5f, calming ? 15f : 10.5f);
    }

    private void UpdateSpawning(float deltaTime, bool calming)
    {
        _ashSpawnTimer -= deltaTime;
        _smokeSpawnTimer -= deltaTime;
        _emberSpawnTimer -= deltaTime;
        _soulMoteTimer -= deltaTime;

        float spawnActivity = MathF.Max(0.12f, _activity);
        if (_ashSpawnTimer <= 0f)
        {
            if (Count(AmbientParticleKind.Ash) < MaximumAsh && RandomUnit() <= spawnActivity)
            {
                SpawnAsh(false);
            }
            _ashSpawnTimer = RandomRange(0.7f, 1.15f) /
                MathF.Max(0.2f, ArenaAtmosphereTuning.AshDensity * spawnActivity);
        }

        if (_smokeSpawnTimer <= 0f)
        {
            if (Count(AmbientParticleKind.Smoke) < MaximumSmoke && RandomUnit() <= spawnActivity)
            {
                SpawnSmoke(false);
            }
            _smokeSpawnTimer = RandomRange(1.2f, 1.9f) / spawnActivity;
        }

        if (_emberSpawnTimer <= 0f)
        {
            if (Count(AmbientParticleKind.Ember) < MaximumEmbers && RandomUnit() <= spawnActivity)
            {
                SpawnEmber(false);
            }
            _emberSpawnTimer = RandomRange(1.1f, 1.8f) /
                MathF.Max(0.18f, ArenaAtmosphereTuning.EmberDensity * spawnActivity);
        }

        if (_soulMoteTimer <= 0f)
        {
            if (!calming && Count(AmbientParticleKind.SoulMote) == 0)
            {
                SpawnSoulMote();
            }
            _soulMoteTimer = RandomRange(calming ? 15f : 8f, calming ? 22f : 13f);
        }
    }

    private void DrawHaze(SpriteBatch batch, Texture2D pixel, float physicalVisibility)
    {
        float calmOpacity = MathHelper.Lerp(0.32f, 1f, _activity);
        float pressure = 1f + _forcePressure * 0.12f;
        Color outer = new(47, 45, 55);
        Color inner = new(69, 64, 75);

        foreach (HazeBand band in HazeBands)
        {
            Vector2 drift = new(
                MathF.Sin(_time * band.Speed + band.Phase) * 13f,
                MathF.Cos(_time * band.Speed * 0.72f + band.Phase) * 7f);
            float breathe = 0.82f + MathF.Sin(_time * 0.19f + band.Phase) * 0.18f;
            float opacity = ArenaAtmosphereTuning.HazeOpacity *
                ArenaAtmosphereTuning.AmbientIntensity *
                physicalVisibility *
                calmOpacity *
                breathe;
            batch.DrawLine(pixel, band.Start + drift, band.End + drift, outer * (opacity * 0.55f), band.Width * pressure);
            batch.DrawLine(pixel, band.Start + drift, band.End + drift, inner * (opacity * 0.35f), band.Width * 0.42f * pressure);
        }
    }

    private void DrawFurnaceFaces(SpriteBatch batch, Texture2D pixel, float physicalVisibility)
    {
        Color deepHeat = GameBalance.DeepViolet;
        Color ironHeat = GameBalance.DeathFlame;
        float calm = MathHelper.Lerp(0.45f, 1f, _activity);

        for (int i = 0; i < FurnaceSources.Length; i++)
        {
            FurnaceSource source = FurnaceSources[i];
            float pulse = GetFurnacePulse(i);
            float alpha = (0.065f + pulse * 0.075f) *
                source.Strength *
                ArenaAtmosphereTuning.AmbientIntensity *
                physicalVisibility *
                calm;
            if (source.Circular)
            {
                batch.FillCircle(pixel, source.Position, 10f + pulse * 2f, deepHeat * alpha);
                batch.FillCircle(pixel, source.Position, 3f, ironHeat * (alpha * 1.7f));
            }
            else
            {
                Vector2 halfMouth = Vector2.UnitX * (source.Radius * 0.28f);
                batch.DrawLine(pixel, source.Position - halfMouth, source.Position + halfMouth, deepHeat * alpha, 13f);
                batch.DrawLine(pixel, source.Position - halfMouth * 0.72f, source.Position + halfMouth * 0.72f, ironHeat * (alpha * 1.25f), 3f);
            }
        }
    }

    private void DrawParticle(
        SpriteBatch batch,
        Texture2D pixel,
        in AmbientParticle particle,
        float physicalVisibility,
        float soulSenseAmount)
    {
        float lifeAlpha = GetLifeAlpha(particle);
        float age = 1f - particle.Remaining / particle.Lifetime;
        float size = MathHelper.Lerp(particle.StartSize, particle.EndSize, age);
        float commonAlpha = lifeAlpha * particle.Opacity * ArenaAtmosphereTuning.AmbientIntensity;

        switch (particle.Kind)
        {
            case AmbientParticleKind.Ash:
                Vector2 direction = particle.Velocity.LengthSquared() > 0.01f
                    ? Vector2.Normalize(particle.Velocity)
                    : Vector2.UnitY;
                batch.DrawLine(
                    pixel,
                    particle.Position - direction * size * 0.35f,
                    particle.Position + direction * size,
                    new Color(139, 134, 145) * (commonAlpha * physicalVisibility),
                    MathF.Max(1f, particle.Depth * 1.35f));
                break;

            case AmbientParticleKind.Smoke:
                batch.FillCircle(
                    pixel,
                    particle.Position,
                    size,
                    new Color(62, 59, 69) * (commonAlpha * physicalVisibility * 0.46f));
                batch.FillCircle(
                    pixel,
                    particle.Position + new Vector2(size * 0.28f, -size * 0.12f),
                    size * 0.56f,
                    new Color(78, 72, 82) * (commonAlpha * physicalVisibility * 0.24f));
                break;

            case AmbientParticleKind.Ember:
                batch.DrawLine(
                    pixel,
                    particle.Position,
                    particle.Position - particle.Velocity * 0.035f,
                    GameBalance.DeathFlame * (commonAlpha * physicalVisibility),
                    MathF.Max(1f, size));
                break;

            case AmbientParticleKind.SoulMote:
                float soulVisibility = MathHelper.Lerp(0.32f, 0.82f, soulSenseAmount);
                batch.FillCircle(
                    pixel,
                    particle.Position,
                    MathF.Max(1f, size),
                    GameBalance.DeathFlame * (commonAlpha * soulVisibility));
                batch.DrawLine(
                    pixel,
                    particle.Position,
                    particle.Position + new Vector2(-5f, 8f),
                    GameBalance.DeathFlameBright * (commonAlpha * soulVisibility * 0.45f),
                    1f);
                break;
        }
    }

    private float GetFurnacePulse(int index)
    {
        FurnaceSource source = FurnaceSources[index];
        float slowPulse = 0.5f + 0.5f * MathF.Sin(_time * (0.72f + index * 0.08f) + source.Phase);
        float machineBreath = 0.5f + 0.5f * MathF.Sin(_time * 0.19f + source.Phase * 1.7f);
        float pulse = 0.72f + slowPulse * ArenaAtmosphereTuning.FurnacePulseStrength + machineBreath * 0.05f;

        // Only the damaged right-hand furnace faults, in a short authored cadence.
        if (index == 1 && _machineFaultRemaining > 0f)
        {
            float elapsed = 0.24f - _machineFaultRemaining;
            float faultMultiplier = elapsed < 0.055f
                ? 0.42f
                : elapsed < 0.11f
                    ? 1.08f
                    : elapsed < 0.165f
                        ? 0.62f
                        : 0.92f;
            pulse *= faultMultiplier;
        }

        return pulse;
    }

    private void SpawnAsh(bool priming)
    {
        int lane = _random.Next(4);
        Vector2 position;
        Vector2 velocity;
        switch (lane)
        {
            case 0:
                position = new Vector2(RandomRange(110f, 485f), RandomRange(105f, 325f));
                velocity = new Vector2(RandomRange(5f, 14f), RandomRange(2f, 7f));
                break;
            case 1:
                position = new Vector2(RandomRange(1325f, 1705f), RandomRange(110f, 350f));
                velocity = new Vector2(RandomRange(-14f, -5f), RandomRange(2f, 8f));
                break;
            case 2:
                position = new Vector2(RandomRange(105f, 455f), RandomRange(700f, 890f));
                velocity = new Vector2(RandomRange(5f, 14f), RandomRange(-5f, 2f));
                break;
            default:
                position = new Vector2(RandomRange(1360f, 1700f), RandomRange(690f, 890f));
                velocity = new Vector2(RandomRange(-14f, -5f), RandomRange(-5f, 2f));
                break;
        }

        float lifetime = RandomRange(7f, 12f);
        ref AmbientParticle particle = ref AllocateParticle();
        particle = new AmbientParticle
        {
            Active = true,
            Kind = AmbientParticleKind.Ash,
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Remaining = priming ? lifetime * RandomRange(0.28f, 0.95f) : lifetime,
            StartSize = RandomRange(1.4f, 3.2f),
            EndSize = RandomRange(0.8f, 1.6f),
            Opacity = RandomRange(0.12f, 0.24f),
            Phase = RandomRange(0f, MathHelper.TwoPi),
            Depth = RandomRange(0.48f, 1f)
        };
    }

    private void SpawnSmoke(bool priming)
    {
        Vector2 source = SmokeSources[_random.Next(SmokeSources.Length)];
        float lifetime = RandomRange(4.5f, 7f);
        ref AmbientParticle particle = ref AllocateParticle();
        particle = new AmbientParticle
        {
            Active = true,
            Kind = AmbientParticleKind.Smoke,
            Position = source + new Vector2(RandomRange(-10f, 10f), RandomRange(-2f, 5f)),
            Velocity = new Vector2(RandomRange(-5f, 5f), RandomRange(-13f, -7f)),
            Lifetime = lifetime,
            Remaining = priming ? lifetime * RandomRange(0.35f, 0.92f) : lifetime,
            StartSize = RandomRange(8f, 13f),
            EndSize = RandomRange(22f, 34f),
            Opacity = RandomRange(0.12f, 0.2f),
            Phase = RandomRange(0f, MathHelper.TwoPi),
            Depth = RandomRange(0.55f, 0.86f)
        };
    }

    private void SpawnEmber(bool priming)
    {
        FurnaceSource source = FurnaceSources[_random.Next(FurnaceSources.Length)];
        float lifetime = RandomRange(1.5f, 2.8f);
        ref AmbientParticle particle = ref AllocateParticle();
        particle = new AmbientParticle
        {
            Active = true,
            Kind = AmbientParticleKind.Ember,
            Position = source.Position + new Vector2(RandomRange(-12f, 12f), RandomRange(-5f, 7f)),
            Velocity = new Vector2(RandomRange(-7f, 7f), RandomRange(-25f, -13f)),
            Lifetime = lifetime,
            Remaining = priming ? lifetime * RandomRange(0.3f, 0.9f) : lifetime,
            StartSize = RandomRange(1.1f, 1.8f),
            EndSize = 0.6f,
            Opacity = RandomRange(0.36f, 0.58f),
            Phase = RandomRange(0f, MathHelper.TwoPi),
            Depth = RandomRange(0.7f, 1f)
        };
    }

    private void SpawnSoulMote()
    {
        Vector2 source = SoulConduits[_random.Next(SoulConduits.Length)];
        float lifetime = RandomRange(1.7f, 2.5f);
        ref AmbientParticle particle = ref AllocateParticle();
        particle = new AmbientParticle
        {
            Active = true,
            Kind = AmbientParticleKind.SoulMote,
            Position = source,
            Velocity = new Vector2(RandomRange(-10f, 10f), RandomRange(2f, 9f)),
            Lifetime = lifetime,
            Remaining = lifetime,
            StartSize = RandomRange(1.2f, 2f),
            EndSize = 0.6f,
            Opacity = RandomRange(0.28f, 0.42f),
            Phase = RandomRange(0f, MathHelper.TwoPi),
            Depth = RandomRange(0.82f, 1f)
        };
    }

    private ref AmbientParticle AllocateParticle()
    {
        for (int offset = 0; offset < _particles.Length; offset++)
        {
            int index = (_nextParticleSlot + offset) % _particles.Length;
            if (_particles[index].Active)
            {
                continue;
            }

            _nextParticleSlot = (index + 1) % _particles.Length;
            return ref _particles[index];
        }

        // Capacity is intentionally strict. Replacing the oldest ambient particle is
        // preferable to growing storage or dropping a burst of allocations in combat.
        int oldestIndex = 0;
        float lowestRemaining = _particles[0].Remaining;
        for (int i = 1; i < _particles.Length; i++)
        {
            if (_particles[i].Remaining < lowestRemaining)
            {
                oldestIndex = i;
                lowestRemaining = _particles[i].Remaining;
            }
        }
        _nextParticleSlot = (oldestIndex + 1) % _particles.Length;
        return ref _particles[oldestIndex];
    }

    private int Count(AmbientParticleKind kind)
    {
        int count = 0;
        for (int i = 0; i < _particles.Length; i++)
        {
            if (_particles[i].Active && _particles[i].Kind == kind)
            {
                count++;
            }
        }
        return count;
    }

    private static float GetLifeAlpha(in AmbientParticle particle)
    {
        float age = 1f - particle.Remaining / particle.Lifetime;
        float fadeIn = MathHelper.Clamp(age / 0.18f, 0f, 1f);
        float fadeOut = MathHelper.Clamp(particle.Remaining / MathF.Min(0.8f, particle.Lifetime * 0.3f), 0f, 1f);
        return fadeIn * fadeOut;
    }

    private static bool IsOutsideArena(Vector2 position) =>
        position.X < -80f || position.X > GameBalance.ArenaBounds.Right + 80f ||
        position.Y < -80f || position.Y > GameBalance.ArenaBounds.Bottom + 80f;

    private float RandomUnit() => (float)_random.NextDouble();

    private float RandomRange(float minimum, float maximum) =>
        minimum + RandomUnit() * (maximum - minimum);
}
