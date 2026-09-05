using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Entities;

public enum BurningState
{
    Approach,
    Telegraph,
    Charge,
    Recovery,
    Dying,
    Detonating,
    Dead
}

public sealed class Burning : Enemy
{
    private readonly int _movementSeed;
    private float _stateTimer;
    private float _visualTime;
    private bool _chargeDamagePending;
    private bool _soulSpawnPending;
    private bool _detonationPending;
    private bool _detonationReleased;
    private bool _hasAggressionSlot;
    private Vector2 _facing = Vector2.UnitX;
    private Vector2 _chargeDirection = Vector2.UnitX;

    public BurningState State { get; private set; } = BurningState.Approach;
    public override string StateLabel => State.ToString().ToUpperInvariant();
    public bool IsCharging => State == BurningState.Charge;
    public bool IsAggressionCommitted => State is BurningState.Telegraph or BurningState.Charge;
    public Vector2 FacingDirection => _facing;
    public Vector2 ChargeDirection => _chargeDirection;
    public float TelegraphProgress => MathHelper.Clamp(1f - _stateTimer / GameBalance.BurningChargeTelegraph, 0f, 1f);
    public float ChargeProgress => MathHelper.Clamp(1f - _stateTimer / GameBalance.BurningChargeDuration, 0f, 1f);

    public Burning(Vector2 position, int movementSeed)
        : base(position, GameBalance.BurningMaxHealth, GameBalance.BurningRadius)
    {
        _movementSeed = movementSeed;
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
        _visualTime += deltaTime;

        if (State == BurningState.Dead)
        {
            return;
        }

        if (State is BurningState.Dying or BurningState.Detonating)
        {
            UpdateDeath(deltaTime, particles);
            return;
        }

        Vector2 toPlayer = player.Position - Position;
        float distance = toPlayer.Length();
        if (distance > 0.001f && State != BurningState.Charge)
        {
            _facing = toPlayer / distance;
        }

        _stateTimer = MathF.Max(0f, _stateTimer - deltaTime);
        switch (State)
        {
            case BurningState.Approach:
                UpdateApproach(deltaTime, distance, particles);
                break;

            case BurningState.Telegraph:
                _chargeDirection = distance > 0.001f ? toPlayer / distance : _facing;
                if (_stateTimer <= 0f)
                {
                    State = BurningState.Charge;
                    _stateTimer = GameBalance.BurningChargeDuration;
                    _facing = _chargeDirection;
                    _chargeDamagePending = true;
                    particles.EmitBurst(Position, -_chargeDirection, 16, GameBalance.DeathFlameBright, 210f, 7f);
                    screenEffects.AddShake(0.1f, 4f);
                }
                break;

            case BurningState.Charge:
                Position += _chargeDirection * GameBalance.BurningChargeSpeed * deltaTime;
                particles.EmitDeathFlame(Position - _chargeDirection * 16f, 2, 0.72f);
                ResolveChargeHit(player, screenEffects);
                if (_stateTimer <= 0f)
                {
                    EnterRecovery();
                }
                break;

            case BurningState.Recovery:
                if (_stateTimer <= 0f)
                {
                    State = BurningState.Approach;
                }
                break;
        }
    }

    public void SetAggressionSlot(bool hasSlot)
    {
        _hasAggressionSlot = hasSlot || IsAggressionCommitted;
    }

    public override void ApplyDamage(DamageInfo damage)
    {
        base.ApplyDamage(damage);
        if (IsAlive && State == BurningState.Telegraph && damage.IsFullCannon)
        {
            State = BurningState.Recovery;
            _stateTimer = GameBalance.BurningRecoveryDuration;
        }
    }

    public void Detonate()
    {
        if (!IsAlive || State != BurningState.Charge)
        {
            return;
        }

        Health = 0;
        State = BurningState.Detonating;
        _stateTimer = GameBalance.BurningDeathDuration;
        _detonationPending = false;
        _detonationReleased = false;
        _soulSpawnPending = false;
    }

    public bool TryConsumeDetonation(out Vector2 position)
    {
        if (!_detonationPending)
        {
            position = default;
            return false;
        }

        _detonationPending = false;
        position = Position;
        return true;
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

    public Vector2[] GetFracturePositions() =>
    [
        Position + new Vector2(-10f, -20f),
        Position + new Vector2(11f, -3f),
        Position + new Vector2(-7f, 16f)
    ];

    public override void Draw(
        SpriteBatch batch,
        Texture2D pixel,
        bool debugVisible,
        bool soulSenseActive,
        bool useSpriteArt)
    {
        if (State == BurningState.Dead)
        {
            return;
        }

        float pulse = 0.5f + 0.5f * MathF.Sin(_visualTime * (Health <= MaxHealth / 4 ? 13f : 8f));
        float telegraph = State == BurningState.Telegraph
            ? 1f - _stateTimer / GameBalance.BurningChargeTelegraph
            : 0f;
        Color body = HitFlashRemaining > 0f ? GameBalance.SoulWhite : new Color(29, 24, 31);
        Vector2 right = new(-_facing.Y, _facing.X);

        if (State == BurningState.Detonating)
        {
            float releaseRemaining = GameBalance.BurningDeathDuration - CombatFeedbackTuning.BurningCompressionDuration;
            if (!_detonationReleased)
            {
                float compression = MathHelper.Clamp(
                    (GameBalance.BurningDeathDuration - _stateTimer) / CombatFeedbackTuning.BurningCompressionDuration,
                    0f,
                    1f);
                float instability = 0.5f + 0.5f * MathF.Sin(_visualTime * 42f);
                float outerRadius = MathHelper.Lerp(62f, 18f, compression);
                batch.FillCircle(pixel, Position, outerRadius, GameBalance.DeepViolet * (0.18f + compression * 0.35f));
                batch.DrawCircle(pixel, Position, outerRadius + instability * 5f, GameBalance.DeathFlameBright * (0.58f + compression * 0.36f), 4f + compression * 5f, 30);
                batch.FillCircle(pixel, Position, 6f + compression * 8f, GameBalance.SoulWhite * (0.62f + compression * 0.38f));
                foreach (Vector2 fracture in GetFracturePositions())
                {
                    batch.DrawLine(pixel, fracture, Vector2.Lerp(fracture, Position, compression), GameBalance.DeathFlameBright * 0.82f, 3f + compression * 2f);
                }
                return;
            }

            float progress = 1f - MathHelper.Clamp(_stateTimer / releaseRemaining, 0f, 1f);
            batch.FillCircle(pixel, Position, 28f + progress * 118f, GameBalance.DeepViolet * (0.62f * (1f - progress)));
            batch.DrawCircle(pixel, Position, 40f + progress * 132f, GameBalance.DeathFlameBright * (1f - progress), 8f, 30);
            return;
        }

        if (!useSpriteArt)
        {
            batch.FillCircle(pixel, Position + new Vector2(4f, 21f), 29f, new Color(3, 3, 6) * 0.62f);
            batch.DrawLine(pixel, Position + new Vector2(0f, -31f), Position + new Vector2(0f, 27f), body, 28f);
            batch.DrawLine(pixel, Position - right * 10f + new Vector2(0f, 6f), Position - right * 22f + _facing * 24f, body, 12f);
            batch.DrawLine(pixel, Position + right * 10f + new Vector2(0f, 6f), Position + right * 23f + _facing * 21f, body, 12f);
            batch.FillCircle(pixel, Position + new Vector2(0f, -37f), 12f, new Color(24, 20, 27));
        }

        foreach (Vector2 fracture in GetFracturePositions())
        {
            batch.DrawLine(pixel, fracture - right * 7f, fracture + right * 7f + _facing * 5f, GameBalance.DeathFlame * (0.42f + pulse * 0.38f), 3f);
        }

        if (State == BurningState.Charge)
        {
            batch.DrawLine(pixel, Position - _chargeDirection * 78f, Position, GameBalance.DeepViolet * 0.82f, 28f);
            batch.DrawLine(pixel, Position - _chargeDirection * 58f, Position, GameBalance.DeathFlameBright * 0.72f, 8f);
        }

        if (soulSenseActive)
        {
            foreach (Vector2 fracture in GetFracturePositions())
            {
                batch.FillCircle(pixel, fracture, 10f, GameBalance.DeepViolet * 0.78f);
                batch.FillCircle(pixel, fracture, 5f, GameBalance.SoulWhite);
            }
        }

        if (debugVisible)
        {
            batch.DrawCircle(pixel, Position, Radius, new Color(80, 220, 210), 2f);
            if (State == BurningState.Charge)
            {
                batch.DrawCircle(pixel, Position, GameBalance.BurningDetonationRadius, new Color(255, 190, 70) * 0.45f, 2f, 32);
            }
        }
    }

    protected override void OnDeath()
    {
        State = BurningState.Dying;
        _stateTimer = GameBalance.BurningDeathDuration;
    }

    private void ResolveChargeHit(Player player, ScreenEffects screenEffects)
    {
        if (!_chargeDamagePending || Vector2.DistanceSquared(Position, player.Position) > MathF.Pow(Radius + player.Radius, 2f))
        {
            return;
        }

        _chargeDamagePending = false;
        player.ApplyDamage(GameBalance.BurningChargeDamage, _chargeDirection * GameBalance.BurningChargeKnockback, screenEffects);
        EnterRecovery();
    }

    private void UpdateApproach(float deltaTime, float distance, ParticleSystem particles)
    {
        if (_hasAggressionSlot && distance <= GameBalance.BurningChargeStartRange)
        {
            State = BurningState.Telegraph;
            _stateTimer = GameBalance.BurningChargeTelegraph;
            _chargeDirection = _facing;
            particles.EmitDeathFlame(Position, 8, 0.9f);
            return;
        }

        if (_hasAggressionSlot || distance > GameBalance.BurningStalkOuterRange)
        {
            float approachSpeed = _hasAggressionSlot ? 1f : 0.68f;
            Position += _facing * GameBalance.BurningMoveSpeed * approachSpeed * deltaTime;
            return;
        }

        float strafeSign = _movementSeed % 2 == 0 ? 1f : -1f;
        Vector2 right = new(-_facing.Y, _facing.X);
        Vector2 stalkMovement = right * strafeSign * 0.58f;
        if (distance < GameBalance.BurningStalkInnerRange)
        {
            stalkMovement -= _facing * 0.7f;
        }
        else
        {
            stalkMovement += _facing * 0.18f;
        }

        Position += Vector2.Normalize(stalkMovement) * GameBalance.BurningMoveSpeed * 0.72f * deltaTime;
    }

    private void EnterRecovery()
    {
        State = BurningState.Recovery;
        _stateTimer = GameBalance.BurningRecoveryDuration;
        _chargeDamagePending = false;
    }

    private void UpdateDeath(float deltaTime, ParticleSystem particles)
    {
        float previous = _stateTimer;
        _stateTimer = MathF.Max(0f, _stateTimer - deltaTime);
        float detonationReleaseRemaining = GameBalance.BurningDeathDuration - CombatFeedbackTuning.BurningCompressionDuration;
        if (State == BurningState.Detonating &&
            !_detonationReleased &&
            previous > detonationReleaseRemaining &&
            _stateTimer <= detonationReleaseRemaining)
        {
            _detonationReleased = true;
            _detonationPending = true;
            _soulSpawnPending = true;
        }

        if (State == BurningState.Dying && previous > 0.28f && _stateTimer <= 0.28f)
        {
            particles.EmitBurst(Position, -_facing, 22, new Color(45, 36, 46), 190f, 7f);
            particles.EmitDeathFlame(Position, 12, 1.05f);
            _soulSpawnPending = true;
        }

        if (_stateTimer <= 0f)
        {
            State = BurningState.Dead;
            IsFinished = true;
        }
    }
}
