using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Input;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Combat;

public enum SoulCannonState
{
    Stored,
    Drawing,
    Charging,
    Returning
}

public readonly record struct CannonShotRequest(
    Vector2 Direction,
    float Charge,
    bool IsFullCharge,
    bool SoulSenseAtFire,
    int Damage,
    float Radius);

public sealed class SoulCannon
{
    private float _stateTimer;
    private float _chargeTime;
    private float _chargeParticleTimer;
    private float _visualTime;
    private bool _shotPending;
    private bool _fullCueCreated;
    private CannonShotRequest _pendingShot;
    private Vector2 _aimDirection = Vector2.UnitX;
    private bool _resonanceActive;

    public SoulCannonState State { get; private set; } = SoulCannonState.Stored;
    public float ChargeProgress => MathHelper.Clamp(_chargeTime / GetFullChargeTime(), 0f, 1f);
    public bool IsFullCharge => ChargeProgress >= 1f;
    public bool IsHandling => State != SoulCannonState.Stored;
    public bool CanUseScythe => State == SoulCannonState.Stored;
    public int ChargeStage => State is SoulCannonState.Stored or SoulCannonState.Returning
        ? 0
        : ChargeProgress < 0.25f
            ? 1
            : ChargeProgress < 0.67f
                ? 2
                : 3;
    public string StateLabel => State == SoulCannonState.Charging
        ? $"CHARGE {ChargeStage} {(int)(ChargeProgress * 100f)}%"
        : State.ToString().ToUpperInvariant();

    public void Reset()
    {
        State = SoulCannonState.Stored;
        _stateTimer = 0f;
        _chargeTime = 0f;
        _chargeParticleTimer = 0f;
        _visualTime = 0f;
        _shotPending = false;
        _fullCueCreated = false;
        _aimDirection = Vector2.UnitX;
        _resonanceActive = false;
    }

    public void Update(
        float deltaTime,
        InputState input,
        Vector2 playerPosition,
        Vector2 facingDirection,
        bool canStart,
        bool soulSenseActive,
        ParticleSystem particles,
        bool resonanceActive)
    {
        _visualTime += deltaTime;
        _resonanceActive = resonanceActive;
        _aimDirection = facingDirection.LengthSquared() > 0.001f ? Vector2.Normalize(facingDirection) : Vector2.UnitX;

        switch (State)
        {
            case SoulCannonState.Stored:
                if (canStart && input.WasRightMousePressed)
                {
                    State = SoulCannonState.Drawing;
                    _stateTimer = GameBalance.CannonDrawDuration;
                    _chargeTime = 0f;
                    _fullCueCreated = false;
                }
                break;

            case SoulCannonState.Drawing:
                _stateTimer = MathF.Max(0f, _stateTimer - deltaTime);
                if (input.WasRightMouseReleased)
                {
                    Fire(soulSenseActive);
                }
                else if (_stateTimer <= 0f)
                {
                    State = SoulCannonState.Charging;
                    _chargeParticleTimer = 0f;
                }
                break;

            case SoulCannonState.Charging:
                _chargeTime = MathF.Min(GetFullChargeTime(), _chargeTime + deltaTime);
                EmitChargeParticles(deltaTime, playerPosition, particles);
                if (IsFullCharge && !_fullCueCreated)
                {
                    _fullCueCreated = true;
                    Vector2 muzzle = playerPosition + _aimDirection * 68f;
                    particles.EmitConvergence(muzzle, 18, 82f, GameBalance.SoulWhite, 0.2f, 5.5f);
                    particles.EmitBurst(muzzle, -_aimDirection, 7, GameBalance.SoulWhite, 105f, 5f);
                }

                if (input.WasRightMouseReleased || !input.IsRightMouseDown)
                {
                    Fire(soulSenseActive);
                }
                break;

            case SoulCannonState.Returning:
                _stateTimer = MathF.Max(0f, _stateTimer - deltaTime);
                if (_stateTimer <= 0f)
                {
                    State = SoulCannonState.Stored;
                    _chargeTime = 0f;
                }
                break;
        }
    }

    public bool TryConsumeShot(out CannonShotRequest shot)
    {
        if (!_shotPending)
        {
            shot = default;
            return false;
        }

        _shotPending = false;
        shot = _pendingShot;
        return true;
    }

    public float GetMovementMultiplier() => State switch
    {
        SoulCannonState.Charging => GameBalance.CannonChargeMovementMultiplier,
        SoulCannonState.Drawing or SoulCannonState.Returning => GameBalance.CannonHandlingMovementMultiplier,
        _ => 1f
    };

    public void DrawBack(
        SpriteBatch batch,
        Texture2D pixel,
        Texture2D weaponTexture,
        Vector2 playerPosition,
        Vector2 facingDirection)
    {
        if (State != SoulCannonState.Stored)
        {
            return;
        }

        Vector2 right = new(-facingDirection.Y, facingDirection.X);
        Vector2 stock = playerPosition - facingDirection * 24f - right * 22f;
        Vector2 barrel = playerPosition + facingDirection * 34f + right * 20f;
        DrawWeapon(batch, pixel, weaponTexture, stock, barrel, 0f, false, 0f);
    }

    public void DrawActive(
        SpriteBatch batch,
        Texture2D pixel,
        Texture2D weaponTexture,
        Vector2 playerPosition,
        Vector2 facingDirection)
    {
        if (State == SoulCannonState.Stored)
        {
            return;
        }

        Vector2 right = new(-facingDirection.Y, facingDirection.X);
        float transition = State switch
        {
            SoulCannonState.Drawing => 1f - _stateTimer / GameBalance.CannonDrawDuration,
            SoulCannonState.Returning => _stateTimer / GameBalance.CannonReturnDuration,
            _ => 1f
        };
        Vector2 storedStock = playerPosition - facingDirection * 24f - right * 22f;
        Vector2 storedBarrel = playerPosition + facingDirection * 34f + right * 20f;
        Vector2 activeStock = playerPosition - facingDirection * 19f + right * 6f;
        Vector2 activeBarrel = playerPosition + facingDirection * 72f + right * 6f;
        Vector2 stock = Vector2.Lerp(storedStock, activeStock, transition);
        Vector2 barrel = Vector2.Lerp(storedBarrel, activeBarrel, transition);
        float pulse = 0.5f + 0.5f * MathF.Sin(_visualTime * (IsFullCharge ? 28f : 17f));
        float vibrationStrength = State == SoulCannonState.Charging && ChargeStage >= 3
            ? MathHelper.Lerp(0.65f, 1.8f, MathHelper.Clamp((ChargeProgress - 0.67f) / 0.33f, 0f, 1f))
            : 0f;
        Vector2 vibration = right * MathF.Sin(_visualTime * 53f) * vibrationStrength;
        stock += vibration * 0.35f;
        barrel += vibration;
        DrawWeapon(batch, pixel, weaponTexture, stock, barrel, ChargeProgress, IsFullCharge, pulse);

        if (State == SoulCannonState.Charging)
        {
            Vector2 core = playerPosition + facingDirection * 2f;
            batch.DrawLine(pixel, core, playerPosition + facingDirection * 22f + right * 6f, GameBalance.DeathFlame * (0.35f + ChargeProgress * 0.45f), 4f + ChargeProgress * 3f);
        }
    }

    private void Fire(bool soulSenseActive)
    {
        float charge = ChargeProgress;
        bool full = IsFullCharge;
        int damage = (int)MathF.Round(MathHelper.Lerp(GameBalance.CannonWeakDamage, GameBalance.CannonFullDamage, charge));
        float radius = MathHelper.Lerp(11f, 25f, charge);
        if (_resonanceActive)
        {
            damage = (int)MathF.Round(damage * GameBalance.ResonanceCannonDamageMultiplier);
            radius *= GameBalance.ResonanceCannonSizeMultiplier;
        }
        _pendingShot = new CannonShotRequest(
            _aimDirection,
            charge,
            full,
            soulSenseActive,
            damage,
            radius);
        _shotPending = true;
        State = SoulCannonState.Returning;
        _stateTimer = GameBalance.CannonReturnDuration;
    }

    private void EmitChargeParticles(float deltaTime, Vector2 playerPosition, ParticleSystem particles)
    {
        _chargeParticleTimer -= deltaTime;
        if (_chargeParticleTimer > 0f)
        {
            return;
        }

        _chargeParticleTimer = ChargeStage switch
        {
            1 => 0.12f,
            2 => 0.075f,
            _ => IsFullCharge ? 0.045f : 0.055f
        };
        Vector2 muzzle = playerPosition + _aimDirection * 68f;
        int particleCount = ChargeStage switch { 1 => 1, 2 => 2, _ => 3 };
        float convergenceRadius = ChargeStage switch { 1 => 38f, 2 => 56f, _ => 72f };
        Color color = IsFullCharge
            ? GameBalance.SoulWhite
            : ChargeStage >= 3
                ? GameBalance.DeathFlameBright
                : GameBalance.DeathFlame;
        particles.EmitConvergence(
            muzzle,
            particleCount,
            convergenceRadius,
            color,
            MathHelper.Lerp(0.16f, 0.11f, ChargeProgress),
            2.8f + ChargeProgress * 2.1f);
    }

    private float GetFullChargeTime() => _resonanceActive
        ? GameBalance.CannonFullChargeTime / GameBalance.ResonanceCannonChargeSpeedMultiplier
        : GameBalance.CannonFullChargeTime;

    private static void DrawWeapon(
        SpriteBatch batch,
        Texture2D pixel,
        Texture2D weaponTexture,
        Vector2 stock,
        Vector2 barrel,
        float charge,
        bool full,
        float pulse)
    {
        Vector2 direction = Vector2.Normalize(barrel - stock);
        float rotation = MathF.Atan2(direction.Y, direction.X);
        float displayLength = Vector2.Distance(stock, barrel) + 36f;
        batch.Draw(
            weaponTexture,
            Vector2.Lerp(stock, barrel, 0.52f),
            null,
            Color.White,
            rotation,
            new Vector2(weaponTexture.Width, weaponTexture.Height) * 0.5f,
            displayLength / weaponTexture.Width,
            SpriteEffects.None,
            0f);

        if (charge <= 0f)
        {
            return;
        }

        int stage = charge < 0.25f ? 1 : charge < 0.67f ? 2 : 3;
        Color energy = full
            ? GameBalance.SoulWhite
            : Color.Lerp(GameBalance.DeepViolet, GameBalance.DeathFlameBright, charge);
        float chamberRadius = stage switch { 1 => 5f, 2 => 8f, _ => 10f };
        float barrelRadius = stage switch { 1 => 10f, 2 => 16f, _ => 21f };
        batch.FillCircle(pixel, stock + direction * 28f, chamberRadius + pulse * 1.5f, energy * (0.58f + charge * 0.38f));
        batch.DrawCircle(pixel, barrel, barrelRadius + pulse * 2f, energy * (0.48f + charge * 0.42f), 2.5f + stage, 24);
        if (stage >= 3)
        {
            batch.FillCircle(pixel, barrel, full ? 10f + pulse * 1.5f : 6f + pulse, full ? GameBalance.SoulWhite : GameBalance.DeathFlameBright * 0.9f);
        }
        if (full)
        {
            batch.DrawCircle(pixel, barrel, 29f + pulse * 5f, GameBalance.SoulWhite * (0.68f + pulse * 0.18f), 3f, 28);
        }
    }
}
