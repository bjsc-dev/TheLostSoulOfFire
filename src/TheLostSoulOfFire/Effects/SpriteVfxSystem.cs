using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Effects;

public sealed class SpriteVfxSystem
{
    public const int Capacity = 64;

    private sealed class Instance
    {
        public required SpriteClip Clip;
        public required VisualEffectFamily Family;
        public required Vector2 Position;
        public float Rotation;
        public float Scale;
        public Color Color = Color.White;
        public float Elapsed;
        public long Sequence;
    }

    private readonly ArtAssets _art;
    private readonly PresentationSettings _settings;
    private readonly List<Instance> _instances = [];
    private long _nextSequence;

    public int ActiveCount => _instances.Count;
    public int DroppedCount { get; private set; }
    public int RejectedLoopCount { get; private set; }

    public SpriteVfxSystem(ArtAssets art, PresentationSettings settings)
    {
        _art = art;
        _settings = settings;
    }

    public bool Spawn(
        string effectKey,
        Vector2 position,
        float rotation = 0f,
        float scale = 1f,
        Color? color = null)
    {
        VisualEffectFamily family = VisualEffectFamilies.Get(effectKey);
        SpriteClip clip = _art.GetEffect(effectKey);
        if (clip.Loop)
        {
            // Loops need an explicit owner/cancellation path. One-shot presentation never owns them.
            RejectedLoopCount++;
            return false;
        }

        if (_settings.ReducedEffects && family.Priority == VisualEffectPriority.Decorative)
        {
            DroppedCount++;
            return false;
        }

        ReserveSlot(family.Priority);
        if (_instances.Count >= Capacity)
        {
            DroppedCount++;
            return false;
        }

        _instances.Add(new Instance
        {
            Clip = clip,
            Family = family,
            Position = position,
            Rotation = rotation,
            Scale = scale,
            Color = color ?? Color.White,
            Sequence = _nextSequence++
        });
        return true;
    }

    public void Update(float deltaTime)
    {
        for (int index = _instances.Count - 1; index >= 0; index--)
        {
            Instance instance = _instances[index];
            instance.Elapsed += deltaTime;
            if (!instance.Clip.Loop && instance.Elapsed >= instance.Clip.Duration)
            {
                _instances.RemoveAt(index);
            }
        }
    }

    public void DrawAlpha(SpriteBatch batch)
    {
        foreach (Instance instance in _instances)
        {
            if (instance.Family.Blend != VisualEffectBlend.Alpha)
            {
                continue;
            }

            ArtAssets.DrawClip(
                batch,
                instance.Clip,
                instance.Elapsed,
                instance.Position,
                instance.Rotation,
                instance.Scale,
                instance.Color);
        }
    }

    public void DrawAdditive(SpriteBatch batch, Matrix worldTransform)
    {
        if (!_instances.Exists(static instance => instance.Family.Blend == VisualEffectBlend.Additive))
        {
            return;
        }

        batch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, transformMatrix: worldTransform);
        foreach (Instance instance in _instances)
        {
            if (instance.Family.Blend != VisualEffectBlend.Additive)
            {
                continue;
            }

            ArtAssets.DrawClip(
                batch,
                instance.Clip,
                instance.Elapsed,
                instance.Position,
                instance.Rotation,
                instance.Scale,
                instance.Color);
        }
        batch.End();
    }

    public void DrawLighting(SpriteBatch batch, SoulfireRenderer renderer)
    {
        foreach (Instance instance in _instances)
        {
            if (instance.Family.GlowIntensity <= 0f)
            {
                continue;
            }

            float normalizedLifetime = instance.Clip.Duration <= 0f
                ? 0f
                : MathHelper.Clamp(1f - instance.Elapsed / instance.Clip.Duration, 0f, 1f);
            renderer.DrawGlow(
                batch,
                instance.Position,
                instance.Family.GlowRadius * instance.Scale,
                instance.Color,
                instance.Family.GlowIntensity * normalizedLifetime);
        }
    }

    public void Clear()
    {
        _instances.Clear();
        _nextSequence = 0;
        DroppedCount = 0;
        RejectedLoopCount = 0;
    }

    private void ReserveSlot(VisualEffectPriority incomingPriority)
    {
        if (_instances.Count < Capacity)
        {
            return;
        }

        int candidateIndex = -1;
        for (int index = 0; index < _instances.Count; index++)
        {
            Instance candidate = _instances[index];
            if (candidate.Family.Priority >= incomingPriority)
            {
                continue;
            }

            if (candidateIndex < 0 || candidate.Family.Priority < _instances[candidateIndex].Family.Priority ||
                candidate.Family.Priority == _instances[candidateIndex].Family.Priority && candidate.Sequence < _instances[candidateIndex].Sequence)
            {
                candidateIndex = index;
            }
        }

        if (candidateIndex >= 0)
        {
            _instances.RemoveAt(candidateIndex);
            DroppedCount++;
        }
    }
}
