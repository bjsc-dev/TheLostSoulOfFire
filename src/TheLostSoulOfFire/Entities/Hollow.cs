using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Entities;

public enum HollowState
{
    Approach,
    Pause,
    Telegraph,
    Swipe,
    Recovery,
    Staggered,
    Dying,
    Dead
}

public sealed class Hollow : Enemy
{
    private readonly int _movementSeed;
    private float _stateTimer;
    private float _brokenStepTimer;
    private float _deathTimer;
    private bool _swipeDamagePending;
    private bool _soulSpawnPending;
    private Vector2 _facing = -Vector2.UnitY;

    public HollowState State { get; private set; } = HollowState.Approach;
    public override string StateLabel => State.ToString().ToUpperInvariant();
    public Vector2 FacingDirection => _facing;
    public Vector2 CorePosition => Position + new Vector2(0f, -5f);
    public float TelegraphProgress => MathHelper.Clamp(1f - _stateTimer / GameBalance.HollowSwipeTelegraph, 0f, 1f);
    public float StrikeProgress => MathHelper.Clamp(1f - _stateTimer / GameBalance.HollowSwipeDuration, 0f, 1f);
    public float RecoveryProgress => MathHelper.Clamp(1f - _stateTimer / GameBalance.HollowRecoveryDuration, 0f, 1f);

    public Hollow(Vector2 position, int movementSeed)
        : base(position, GameBalance.HollowMaxHealth, GameBalance.HollowRadius)
    {
        _movementSeed = movementSeed;
        _brokenStepTimer = 0.55f + movementSeed * 0.11f;
    }

    public override void Update(
        float deltaTime,
        Player player,
        IReadOnlyList<Soul> souls,
        Rectangle movementBounds,
        ParticleSystem particles,
        ScreenEffects screenEffects)
    {
        UpdateCommon(deltaTime, movementBounds);

        if (State == HollowState.Dead)
        {
            return;
        }

        if (State == HollowState.Dying)
        {
            UpdateDying(deltaTime, particles);
            return;
        }

        Vector2 toPlayer = player.Position - Position;
        float distance = toPlayer.Length();
        if (distance > 0.001f)
        {
            _facing = toPlayer / distance;
        }

        _stateTimer = MathF.Max(0f, _stateTimer - deltaTime);
        switch (State)
        {
            case HollowState.Approach:
                UpdateApproach(deltaTime, distance);
                break;
            case HollowState.Pause:
                if (_stateTimer <= 0f)
                {
                    State = HollowState.Approach;
                }
                break;
            case HollowState.Telegraph:
                if (_stateTimer <= 0f)
                {
                    State = HollowState.Swipe;
                    _stateTimer = GameBalance.HollowSwipeDuration;
                    _swipeDamagePending = true;
                }
                break;
            case HollowState.Swipe:
                ResolveSwipe(player, screenEffects);
                if (_stateTimer <= 0f)
                {
                    State = HollowState.Recovery;
                    _stateTimer = GameBalance.HollowRecoveryDuration;
                }
                break;
            case HollowState.Recovery:
            case HollowState.Staggered:
                if (_stateTimer <= 0f)
                {
                    State = HollowState.Approach;
                }
                break;
        }
    }

    public override void ApplyDamage(DamageInfo damage)
    {
        base.ApplyDamage(damage);
        if (!IsAlive)
        {
            return;
        }

        if (damage.IsFullCannon && damage.IsSoulCoreHit)
        {
            State = HollowState.Staggered;
            _stateTimer = GameBalance.HollowFullCannonStagger;
        }
    }

    public override bool TryConsumeSoulSpawn(out Vector2 position)
    {
        if (!_soulSpawnPending)
        {
            position = default;
            return false;
        }

        _soulSpawnPending = false;
        position = Position;
        return true;
    }

    public override void Draw(
        SpriteBatch batch,
        Texture2D pixel,
        bool debugVisible,
        bool soulSenseActive,
        bool useSpriteArt)
    {
        if (State == HollowState.Dead)
        {
            return;
        }

        if (State == HollowState.Dying)
        {
            DrawDying(batch, pixel);
            return;
        }

        Vector2 right = new(-_facing.Y, _facing.X);
        float telegraph = State == HollowState.Telegraph
            ? 1f - _stateTimer / GameBalance.HollowSwipeTelegraph
            : 0f;
        Color body = HitFlashRemaining > 0f ? GameBalance.SoulWhite : new Color(35, 34, 42);

        if (!useSpriteArt)
        {
            batch.FillCircle(pixel, Position + new Vector2(5f, 25f), 31f, new Color(3, 3, 7) * 0.6f);
            batch.DrawLine(pixel, Position + new Vector2(0f, -47f), Position + new Vector2(0f, 31f), body, 18f);
            batch.DrawLine(pixel, Position + new Vector2(-11f, 12f), Position + new Vector2(-20f, 42f), new Color(26, 25, 33), 11f);
            batch.DrawLine(pixel, Position + new Vector2(11f, 12f), Position + new Vector2(17f, 45f), new Color(26, 25, 33), 10f);

            Vector2 shoulder = Position + new Vector2(0f, -24f);
            Vector2 armBack = shoulder - _facing * (20f + telegraph * 24f) + right * 18f;
            Vector2 armFront = shoulder + _facing * (State == HollowState.Swipe ? 55f : 15f) - right * 17f;
            batch.DrawLine(pixel, shoulder + right * 7f, armBack, body, 10f);
            batch.DrawLine(pixel, shoulder - right * 7f, armFront, body, 10f);

            Vector2 mask = Position + new Vector2(0f, -48f) + _facing * 2f;
            batch.FillCircle(pixel, mask, 13f, new Color(216, 211, 203));
            batch.DrawLine(pixel, mask - right * 5f, mask + right * 5f, new Color(130, 124, 128), 1.5f);
        }

        if (soulSenseActive)
        {
            DrawSoulCore(batch, pixel);
        }

        if (debugVisible)
        {
            batch.DrawCircle(pixel, Position, Radius, new Color(80, 220, 210), 2f);
            batch.DrawCircle(pixel, CorePosition, GameBalance.HollowCoreRadius, new Color(255, 210, 80), 2f);
        }
    }

    public void DrawSoulCore(SpriteBatch batch, Texture2D pixel)
    {
        batch.FillCircle(pixel, CorePosition, 13f, GameBalance.DeepViolet * 0.8f);
        batch.FillCircle(pixel, CorePosition, 8f, GameBalance.DeathFlameBright);
        batch.FillCircle(pixel, CorePosition, 4f, GameBalance.SoulWhite);
    }

    protected override void OnDeath()
    {
        State = HollowState.Dying;
        _deathTimer = GameBalance.HollowDeathDuration;
    }

    private void UpdateApproach(float deltaTime, float distance)
    {
        if (distance <= GameBalance.HollowAttackStartRange)
        {
            State = HollowState.Telegraph;
            _stateTimer = GameBalance.HollowSwipeTelegraph;
            return;
        }

        _brokenStepTimer -= deltaTime;
        float speedMultiplier = 1f;
        if (_brokenStepTimer <= 0f)
        {
            _brokenStepTimer = 0.82f + ((_movementSeed * 37) % 5) * 0.09f;
            if ((_movementSeed + (int)(Position.X + Position.Y)) % 3 == 0)
            {
                State = HollowState.Pause;
                _stateTimer = 0.12f;
                return;
            }

            speedMultiplier = 2.25f;
        }

        Position += _facing * GameBalance.HollowMoveSpeed * speedMultiplier * deltaTime;
    }

    private void ResolveSwipe(Player player, ScreenEffects screenEffects)
    {
        if (!_swipeDamagePending)
        {
            return;
        }

        _swipeDamagePending = false;
        Vector2 toPlayer = player.Position - Position;
        if (toPlayer.LengthSquared() > MathF.Pow(GameBalance.HollowSwipeRange + player.Radius, 2f))
        {
            return;
        }

        Vector2 direction = toPlayer.LengthSquared() > 0.001f ? Vector2.Normalize(toPlayer) : _facing;
        player.ApplyDamage(GameBalance.HollowSwipeDamage, direction * GameBalance.HollowSwipeKnockback, screenEffects);
    }

    private void UpdateDying(float deltaTime, ParticleSystem particles)
    {
        float previous = _deathTimer;
        _deathTimer = MathF.Max(0f, _deathTimer - deltaTime);

        if (previous > 0.3f && _deathTimer <= 0.3f)
        {
            particles.EmitBurst(Position, -_facing, 18, new Color(47, 43, 54), 170f, 7f);
            particles.EmitDeathFlame(Position, 7, 0.9f);
            _soulSpawnPending = true;
        }

        if (_deathTimer <= 0f)
        {
            State = HollowState.Dead;
            IsFinished = true;
        }
    }

    private void DrawDying(SpriteBatch batch, Texture2D pixel)
    {
        float normalized = _deathTimer / GameBalance.HollowDeathDuration;
        Color body = new Color(43, 39, 50) * normalized;
        batch.DrawLine(pixel, Position + new Vector2(-13f, -29f), Position + new Vector2(-27f, 29f), body, 14f);
        batch.DrawLine(pixel, Position + new Vector2(13f, -27f), Position + new Vector2(25f, 33f), body, 13f);
        Vector2 mask = Position + new Vector2(0f, -48f);
        batch.FillCircle(pixel, mask, 13f, new Color(216, 211, 203) * normalized);
        batch.DrawLine(pixel, mask - new Vector2(8f, 7f), mask + new Vector2(7f, 8f), GameBalance.DeepViolet * normalized, 3f);
    }
}
