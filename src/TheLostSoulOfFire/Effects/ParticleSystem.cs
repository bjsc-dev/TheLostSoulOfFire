using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Effects;

public sealed class ParticleSystem
{
    public const int Capacity = 512;

    private enum ParticleShape
    {
        Orb,
        Shard
    }

    private enum ParticleMotion
    {
        Free,
        Converge
    }

    private sealed class Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Lifetime;
        public float Remaining;
        public float StartSize;
        public float EndSize;
        public Color Color;
        public ParticleShape Shape;
        public float Rotation;
        public float AngularVelocity;
        public ParticleMotion Motion;
        public Vector2 StartPosition;
        public Vector2 TargetPosition;
        public VisualEffectPriority Priority;
        public long Sequence;
    }

    private readonly List<Particle> _particles = [];
    private readonly Random _random = new(1987);
    private readonly PresentationSettings _settings;
    private long _nextSequence;

    public int ActiveCount => _particles.Count;
    public int DroppedCount { get; private set; }

    public ParticleSystem(PresentationSettings settings)
    {
        _settings = settings;
    }

    public void EmitDeathFlame(
        Vector2 position,
        int count,
        float intensity = 1f,
        VisualEffectPriority priority = VisualEffectPriority.Decorative)
    {
        int adjustedCount = GetAdjustedCount(count, priority);
        for (int i = 0; i < adjustedCount; i++)
        {
            float angle = RandomRange(-MathHelper.Pi, MathHelper.Pi);
            float speed = RandomRange(15f, 48f) * intensity;
            Vector2 velocity = new(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed);

            // Death Flame deliberately drifts sideways or downward instead of behaving like normal fire.
            velocity.Y += RandomRange(-2f, 22f) * intensity;
            Color color = _random.NextDouble() > 0.25d ? GameBalance.DeathFlame : GameBalance.DeathFlameBright;
            Add(
                position + RandomVector(7f),
                velocity,
                RandomRange(0.22f, 0.55f),
                RandomRange(2f, 5f) * intensity,
                0.5f,
                color,
                i % 3 == 0 ? ParticleShape.Shard : ParticleShape.Orb,
                priority: priority);
        }
    }

    public void EmitBurst(
        Vector2 position,
        Vector2 direction,
        int count,
        Color color,
        float force,
        float size,
        VisualEffectPriority priority = VisualEffectPriority.Combat)
    {
        Vector2 baseDirection = direction.LengthSquared() > 0.001f ? Vector2.Normalize(direction) : Vector2.UnitX;
        float baseAngle = MathF.Atan2(baseDirection.Y, baseDirection.X);

        int adjustedCount = GetAdjustedCount(count, priority);
        for (int i = 0; i < adjustedCount; i++)
        {
            float angle = baseAngle + RandomRange(-0.9f, 0.9f);
            float speed = RandomRange(force * 0.35f, force);
            Add(
                position + RandomVector(5f),
                new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed,
                RandomRange(0.16f, 0.38f),
                RandomRange(size * 0.45f, size),
                0.5f,
                color,
                i % 4 == 0 ? ParticleShape.Shard : ParticleShape.Orb,
                priority: priority);
        }
    }

    public void EmitConvergence(
        Vector2 target,
        int count,
        float radius,
        Color color,
        float lifetime = 0.24f,
        float size = 4f,
        VisualEffectPriority priority = VisualEffectPriority.Critical)
    {
        int adjustedCount = GetAdjustedCount(count, priority);
        for (int i = 0; i < adjustedCount; i++)
        {
            float angle = RandomRange(-MathHelper.Pi, MathHelper.Pi);
            float distance = RandomRange(radius * 0.62f, radius);
            Vector2 start = target + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
            Add(
                start,
                Vector2.Zero,
                lifetime * RandomRange(0.82f, 1.12f),
                size * RandomRange(0.65f, 1.08f),
                0.45f,
                color,
                i % 3 == 0 ? ParticleShape.Shard : ParticleShape.Orb,
                ParticleMotion.Converge,
                target + RandomVector(2.5f),
                priority);
        }
    }

    public void EmitSoulRelease(Vector2 position, VisualEffectPriority priority = VisualEffectPriority.Critical)
    {
        int adjustedCount = GetAdjustedCount(18, priority);
        for (int i = 0; i < adjustedCount; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            Vector2 velocity = new(side * RandomRange(8f, 34f), RandomRange(-78f, -28f));
            Add(
                position + RandomVector(5f),
                velocity,
                RandomRange(0.45f, 0.85f),
                RandomRange(2f, 5f),
                0.4f,
                GameBalance.SoulWhite,
                ParticleShape.Orb,
                priority: priority);
        }
    }

    public void Update(float deltaTime)
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            Particle particle = _particles[i];
            particle.Remaining -= deltaTime;
            if (particle.Remaining <= 0f)
            {
                _particles.RemoveAt(i);
                continue;
            }

            if (particle.Motion == ParticleMotion.Converge)
            {
                float progress = 1f - particle.Remaining / particle.Lifetime;
                float eased = 1f - MathF.Pow(1f - MathHelper.Clamp(progress, 0f, 1f), 2.4f);
                particle.Position = Vector2.Lerp(particle.StartPosition, particle.TargetPosition, eased);
            }
            else
            {
                particle.Position += particle.Velocity * deltaTime;
                particle.Velocity *= MathF.Pow(0.08f, deltaTime);
            }
            particle.Rotation += particle.AngularVelocity * deltaTime;
        }
    }

    public void Draw(SpriteBatch batch, Texture2D pixel)
    {
        foreach (Particle particle in _particles)
        {
            float normalized = particle.Remaining / particle.Lifetime;
            float size = MathHelper.Lerp(particle.EndSize, particle.StartSize, normalized);
            if (particle.Shape == ParticleShape.Shard)
            {
                Vector2 direction = new(MathF.Cos(particle.Rotation), MathF.Sin(particle.Rotation));
                batch.DrawLine(pixel, particle.Position - direction * size, particle.Position + direction * size * 1.6f, particle.Color * normalized, MathF.Max(1.5f, size * 0.45f));
            }
            else
            {
                batch.FillCircle(pixel, particle.Position, size, particle.Color * normalized);
            }
        }
    }

    public void DrawLighting(SpriteBatch batch, SoulfireRenderer renderer)
    {
        foreach (Particle particle in _particles)
        {
            float normalized = particle.Remaining / particle.Lifetime;
            float size = MathHelper.Lerp(particle.EndSize, particle.StartSize, normalized);
            float radius = MathF.Max(10f, size * SoulfireRenderSettings.ParticleGlowRadiusMultiplier);
            renderer.DrawGlow(
                batch,
                particle.Position,
                radius,
                particle.Color,
                normalized * SoulfireRenderSettings.ParticleGlowIntensity);
        }
    }

    public void Clear()
    {
        _particles.Clear();
        _nextSequence = 0;
        DroppedCount = 0;
    }

    private void Add(
        Vector2 position,
        Vector2 velocity,
        float lifetime,
        float startSize,
        float endSize,
        Color color,
        ParticleShape shape,
        ParticleMotion motion = ParticleMotion.Free,
        Vector2 targetPosition = default,
        VisualEffectPriority priority = VisualEffectPriority.Decorative)
    {
        if (!ReserveSlot(priority))
        {
            return;
        }

        _particles.Add(new Particle
        {
            Position = position,
            StartPosition = position,
            TargetPosition = targetPosition,
            Velocity = velocity,
            Lifetime = lifetime,
            Remaining = lifetime,
            StartSize = startSize,
            EndSize = endSize,
            Color = color,
            Shape = shape,
            Motion = motion,
            Priority = priority,
            Sequence = _nextSequence++,
            Rotation = RandomRange(-MathHelper.Pi, MathHelper.Pi),
            AngularVelocity = RandomRange(-8f, 8f)
        });
    }

    private Vector2 RandomVector(float radius) =>
        new(RandomRange(-radius, radius), RandomRange(-radius, radius));

    private float RandomRange(float minimum, float maximum) =>
        minimum + (float)_random.NextDouble() * (maximum - minimum);

    private int GetAdjustedCount(int count, VisualEffectPriority priority)
    {
        if (count <= 0 || _settings.ReducedEffects && priority == VisualEffectPriority.Decorative)
        {
            if (count > 0)
            {
                DroppedCount += count;
            }
            return 0;
        }

        if (!_settings.ReducedEffects || priority == VisualEffectPriority.Critical)
        {
            return count;
        }

        int adjusted = Math.Max(1, (int)MathF.Ceiling(count * _settings.ParticleDensityScale));
        DroppedCount += count - adjusted;
        return adjusted;
    }

    private bool ReserveSlot(VisualEffectPriority incomingPriority)
    {
        if (_particles.Count < Capacity)
        {
            return true;
        }

        int candidateIndex = -1;
        for (int index = 0; index < _particles.Count; index++)
        {
            Particle candidate = _particles[index];
            if (candidate.Priority >= incomingPriority)
            {
                continue;
            }

            if (candidateIndex < 0 || candidate.Priority < _particles[candidateIndex].Priority ||
                candidate.Priority == _particles[candidateIndex].Priority && candidate.Sequence < _particles[candidateIndex].Sequence)
            {
                candidateIndex = index;
            }
        }

        if (candidateIndex < 0)
        {
            DroppedCount++;
            return false;
        }

        _particles.RemoveAt(candidateIndex);
        DroppedCount++;
        return true;
    }
}
