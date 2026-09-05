using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Input;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Entities;

public sealed class Player
{
    private sealed class Afterimage
    {
        public Vector2 Position;
        public Vector2 Facing;
        public float Remaining;
        public float Lifetime;
    }

    private readonly List<Afterimage> _afterimages = [];
    private float _idleParticleTimer;
    private float _visualTime;
    private float _dashTimer;
    private float _dashCooldownTimer;
    private float _dashTrailTimer;
    private float _afterimageTimer;
    private float _resonanceTimer;
    private float _resonanceActivationTimer;
    private float _resonanceAfterimageTimer;
    private float _activeDashDistance = GameBalance.DashDistance;
    private Vector2 _dashDirection = Vector2.UnitX;
    private Vector2 _attackImpulse;
    private Vector2 _damageKnockback;

    public Vector2 Position { get; private set; }
    public Vector2 Velocity { get; private set; }
    public Vector2 FacingDirection { get; private set; } = Vector2.UnitX;
    public Vector2 DashDirection => _dashDirection;
    public int Health { get; private set; } = GameBalance.PlayerMaxHealth;
    public float Radius => GameBalance.PlayerRadius;
    public float InvulnerabilityRemaining { get; private set; }
    public float HitFlashRemaining { get; private set; }
    public float DashCooldownRemaining => _dashCooldownTimer;
    public bool IsDashing => _dashTimer > 0f;
    public bool IsInvulnerable => InvulnerabilityRemaining > 0f;
    public bool IsDead => Health <= 0;
    public float Resonance { get; private set; }
    public bool IsResonanceReady => !ResonanceActive && Resonance >= GameBalance.ResonanceRequired;
    public bool ResonanceActive { get; private set; }
    public float ResonanceRemaining => _resonanceTimer;
    public float ResonanceActivationRemaining => _resonanceActivationTimer;
    public bool SoulSenseActive { get; private set; }
    public ScytheCombat Scythe { get; } = new();
    public SoulCannon Cannon { get; } = new();

    public Player(Vector2 position)
    {
        Position = position;
    }

    public void Reset(Vector2 position)
    {
        Position = position;
        Velocity = Vector2.Zero;
        FacingDirection = Vector2.UnitX;
        Health = GameBalance.PlayerMaxHealth;
        _idleParticleTimer = 0f;
        _dashTimer = 0f;
        _dashCooldownTimer = 0f;
        InvulnerabilityRemaining = 0f;
        HitFlashRemaining = 0f;
        _attackImpulse = Vector2.Zero;
        _damageKnockback = Vector2.Zero;
        Resonance = 0f;
        ResonanceActive = false;
        _resonanceTimer = 0f;
        _resonanceActivationTimer = 0f;
        _resonanceAfterimageTimer = 0f;
        _activeDashDistance = GameBalance.DashDistance;
        SoulSenseActive = false;
        _afterimages.Clear();
        Scythe.Reset();
        Cannon.Reset();
    }

    public void SettleForCompletion()
    {
        Velocity = Vector2.Zero;
        _dashTimer = 0f;
        _attackImpulse = Vector2.Zero;
        _damageKnockback = Vector2.Zero;
        Resonance = 0f;
        ResonanceActive = false;
        _resonanceTimer = 0f;
        _resonanceActivationTimer = 0f;
        _resonanceAfterimageTimer = 0f;
        SoulSenseActive = false;
        _afterimages.Clear();
        Scythe.Reset();
        Cannon.Reset();
    }

    public void Update(
        float deltaTime,
        InputState input,
        Vector2 mouseWorld,
        Rectangle movementBounds,
        ParticleSystem particles,
        ScreenEffects screenEffects,
        bool forceSoulSense = false)
    {
        _visualTime += deltaTime;
        _resonanceActivationTimer = MathF.Max(0f, _resonanceActivationTimer - deltaTime);
        HitFlashRemaining = MathF.Max(0f, HitFlashRemaining - deltaTime);
        if (ResonanceActive)
        {
            _resonanceTimer = MathF.Max(0f, _resonanceTimer - deltaTime);
            if (_resonanceTimer <= 0f)
            {
                ResonanceActive = false;
                particles.EmitDeathFlame(Position, 10, 0.72f);
            }
        }

        if (!IsDead && IsResonanceReady && input.WasKeyPressed(Keys.R))
        {
            StartResonance();
        }

        SoulSenseActive = !IsDead && (ResonanceActive || forceSoulSense || input.IsKeyDown(Keys.Q));
        _dashCooldownTimer = MathF.Max(0f, _dashCooldownTimer - deltaTime);
        InvulnerabilityRemaining = MathF.Max(0f, InvulnerabilityRemaining - deltaTime);
        UpdateAfterimages(deltaTime);

        if (IsDead)
        {
            ResonanceActive = false;
            _resonanceTimer = 0f;
            Velocity = Vector2.Zero;
            return;
        }

        Vector2 toMouse = mouseWorld - Position;
        if (toMouse.LengthSquared() > 4f)
        {
            FacingDirection = Vector2.Normalize(toMouse);
        }

        Vector2 movement = ReadMovement(input);

        Cannon.Update(
            deltaTime,
            input,
            Position,
            FacingDirection,
            !IsDashing && Scythe.ActiveStep == 0,
            SoulSenseActive,
            particles,
            ResonanceActive);

        Scythe.Update(deltaTime, input, FacingDirection, Position, particles, !IsDashing && Cannon.CanUseScythe, ResonanceActive);
        if (Scythe.StartedThisFrame)
        {
            _attackImpulse = Scythe.AttackDirection * Scythe.GetForwardImpulse();
        }

        if (input.WasKeyPressed(Keys.Space) && _dashCooldownTimer <= 0f && Scythe.ActiveStep == 0)
        {
            StartDash(movement, particles, screenEffects);
        }

        if (_dashTimer > 0f)
        {
            UpdateDash(deltaTime, particles);
        }
        else
        {
            float movementMultiplier = SoulSenseActive && !ResonanceActive ? GameBalance.SoulSenseMovementMultiplier : 1f;
            movementMultiplier *= ResonanceActive ? GameBalance.ResonanceMovementMultiplier : 1f;
            movementMultiplier *= Cannon.GetMovementMultiplier();
            Velocity = movement * GameBalance.PlayerMoveSpeed * movementMultiplier + _attackImpulse + _damageKnockback;
            _attackImpulse *= MathF.Pow(0.002f, deltaTime);
            _damageKnockback *= MathF.Pow(0.012f, deltaTime);
        }

        Position += Velocity * deltaTime;
        ClampTo(movementBounds);

        if (ResonanceActive && movement.LengthSquared() > 0.001f && !IsDashing)
        {
            _resonanceAfterimageTimer -= deltaTime;
            if (_resonanceAfterimageTimer <= 0f)
            {
                _resonanceAfterimageTimer = 0.16f;
                AddAfterimage();
            }
        }

        _idleParticleTimer -= deltaTime;
        if (_idleParticleTimer <= 0f && !IsDashing)
        {
            _idleParticleTimer = 0.16f;
            particles.EmitDeathFlame(Position - FacingDirection * 2f, 1, 0.55f);
        }
    }

    public void DrawAfterimages(SpriteBatch batch, Texture2D pixel)
    {
        foreach (Afterimage afterimage in _afterimages)
        {
            float alpha = afterimage.Remaining / afterimage.Lifetime;
            Vector2 right = new(-afterimage.Facing.Y, afterimage.Facing.X);
            Color silhouette = new Color(69, 28, 112) * (alpha * 0.48f);
            batch.DrawLine(pixel, afterimage.Position - afterimage.Facing * 15f, afterimage.Position + afterimage.Facing * 14f, silhouette, 28f);
            batch.DrawLine(pixel, afterimage.Position - afterimage.Facing * 13f, afterimage.Position - afterimage.Facing * 35f + right * 9f, silhouette, 11f);
            batch.FillCircle(pixel, afterimage.Position + afterimage.Facing * 18f, 10f, silhouette);
            batch.FillCircle(pixel, afterimage.Position, 4f, GameBalance.DeathFlameBright * (alpha * 0.35f));
        }
    }

    public void Draw(SpriteBatch batch, Texture2D pixel, ArtAssets art, bool debugVisible, float soulSenseAmount = 0f)
    {
        if (IsDead)
        {
            float deathPulse = 0.5f + 0.5f * MathF.Sin(_visualTime * 5f);
            batch.FillCircle(pixel, Position, 13f + deathPulse * 3f, GameBalance.DeepViolet * 0.8f);
            batch.FillCircle(pixel, Position, 6f + deathPulse, GameBalance.SoulWhite * 0.8f);
            return;
        }

        Vector2 right = new(-FacingDirection.Y, FacingDirection.X);
        float pulse = 0.5f + 0.5f * MathF.Sin(_visualTime * 4f);

        if (ResonanceActive)
        {
            float flare = 0.5f + 0.5f * MathF.Sin(_visualTime * 3.8f);
            // Broken, quiet crown leaves the coat and current attack legible.
            for (int i = 0; i < 3; i++)
                batch.DrawArc(pixel, Position, 30f + flare * 2f,
                    i * MathHelper.TwoPi / 3f + 0.2f, 0.75f,
                    GameBalance.DeathFlame * 0.42f, 2f, 8);
        }

        // The directional body sheet already includes the stored cannon.
        Scythe.Draw(batch, pixel, art.PhysicalScythe, Position, FacingDirection, debugVisible);

        Vector2 head = Position + FacingDirection * 18f;

        Vector2 eye = head + FacingDirection * 8f;
        float sense = MathHelper.Clamp(soulSenseAmount, 0f, 1f);
        Color eyeColor = Color.Lerp(new Color(174, 166, 183), GameBalance.SoulWhite, sense);
        if (sense > 0.001f)
        {
            batch.FillCircle(pixel, eye, 8f, GameBalance.DeepViolet * (0.68f * sense));
            batch.DrawLine(pixel, Position + FacingDirection * 4f, head, GameBalance.DeathFlame * (0.5f * sense), 4f);
        }
        batch.DrawLine(pixel, eye - right * 4f, eye + right * 4f, eyeColor, MathHelper.Lerp(2f, 3f, sense));

        bool coreReady = IsResonanceReady;
        float coreRadius = coreReady ? 9f + pulse * 2.4f : 7f + pulse * 1.3f;
        batch.FillCircle(pixel, Position + FacingDirection * 2f, coreRadius, GameBalance.DeepViolet * 0.75f);
        float coreAlpha = ResonanceActive || coreReady || SoulSenseActive ? 1f : 0.88f;
        batch.FillCircle(pixel, Position + FacingDirection * 2f, 3.2f + pulse * (coreReady ? 1.8f : 0.6f), GameBalance.SoulWhite * coreAlpha);
        if (coreReady)
        {
            batch.DrawCircle(pixel, Position + FacingDirection * 2f, 14f + pulse * 5f, GameBalance.DeathFlameBright * 0.78f, 3f, 20);
        }

        if (ResonanceActive)
        {
            batch.DrawLine(pixel, Position + FacingDirection * 2f, Position - right * 14f - Vector2.UnitY * 15f, GameBalance.DeathFlameBright * 0.72f, 3f);
            batch.DrawLine(pixel, Position + FacingDirection * 2f, Position + right * 13f + Vector2.UnitY * 13f, GameBalance.DeathFlame * 0.72f, 3f);
        }

        Cannon.DrawActive(batch, pixel, art.SoulCannon, Position, FacingDirection);

        if (HitFlashRemaining > 0f)
        {
            float flash = MathHelper.Clamp(HitFlashRemaining / 0.14f, 0f, 1f);
            batch.DrawCircle(pixel, Position, 29f, GameBalance.SoulWhite * (0.72f * flash), 4f, 24);
            batch.FillCircle(pixel, Position + FacingDirection * 2f, 7f, GameBalance.SoulWhite * (0.88f * flash));
        }

        if (IsDashing)
        {
            Vector2 ignitionOrigin = Position - _dashDirection * 15f;
            batch.DrawLine(pixel, ignitionOrigin - right * 8f, ignitionOrigin - _dashDirection * 23f - right * 11f, GameBalance.DeathFlame, 7f);
            batch.DrawLine(pixel, ignitionOrigin + right * 8f, ignitionOrigin - _dashDirection * 27f + right * 12f, GameBalance.DeathFlameBright, 5f);
        }

        if (debugVisible)
        {
            batch.DrawCircle(pixel, Position, Radius, new Color(80, 220, 210), 2f);
            batch.DrawLine(pixel, Position, Position + FacingDirection * 70f, new Color(80, 220, 210) * 0.8f, 2f);
        }
    }

    private void StartDash(Vector2 movement, ParticleSystem particles, ScreenEffects screenEffects)
    {
        _dashDirection = movement.LengthSquared() > 0.001f ? Vector2.Normalize(movement) : FacingDirection;
        _dashTimer = GameBalance.DashDuration;
        _activeDashDistance = GameBalance.DashDistance * (ResonanceActive ? GameBalance.ResonanceDashDistanceMultiplier : 1f);
        _dashCooldownTimer = GameBalance.DashCooldown * (ResonanceActive ? GameBalance.ResonanceDashCooldownMultiplier : 1f);
        InvulnerabilityRemaining = GameBalance.DashInvulnerability;
        _dashTrailTimer = 0f;
        _afterimageTimer = 0f;
        Velocity = _dashDirection * (_activeDashDistance / GameBalance.DashDuration);

        AddAfterimage();
        particles.EmitBurst(Position - _dashDirection * 12f, -_dashDirection, 12, GameBalance.DeathFlameBright, 145f, 6f);
        particles.EmitDeathFlame(Position, 8, 1.35f);
        screenEffects.AddShake(0.1f, 3f);
    }

    public void ApplyDamage(int damage, Vector2 knockback, ScreenEffects screenEffects)
    {
        if (IsDead || IsInvulnerable)
        {
            return;
        }

        Health = Math.Max(0, Health - damage);
        HitFlashRemaining = Health == 0 ? 0.24f : 0.14f;
        _damageKnockback += knockback;
        InvulnerabilityRemaining = 0.5f;
        screenEffects.BeginHitstop(Health == 0 ? 0.12f : 0.045f);
        screenEffects.AddShake(Health == 0 ? 0.28f : 0.12f, Health == 0 ? 9f : 5f);
        screenEffects.Flash(0.09f, Health == 0 ? 0.34f : 0.2f);
    }

    public void AddResonance(float amount)
    {
        Resonance = MathHelper.Clamp(Resonance + amount, 0f, GameBalance.ResonanceRequired);
    }

    public void FillResonance()
    {
        if (!ResonanceActive)
        {
            Resonance = GameBalance.ResonanceRequired;
        }
    }

    public void ApplyCannonRecoil(Vector2 shotDirection, float charge)
    {
        _damageKnockback -= shotDirection * MathHelper.Lerp(180f, 520f, charge);
    }

    private void UpdateDash(float deltaTime, ParticleSystem particles)
    {
        _dashTimer = MathF.Max(0f, _dashTimer - deltaTime);
        Velocity = _dashDirection * (_activeDashDistance / GameBalance.DashDuration);

        _dashTrailTimer -= deltaTime;
        if (_dashTrailTimer <= 0f)
        {
            _dashTrailTimer = 0.02f;
            particles.EmitDeathFlame(Position - _dashDirection * 10f, 2, 1.05f);
        }

        _afterimageTimer -= deltaTime;
        if (_afterimageTimer <= 0f && _afterimages.Count < (ResonanceActive ? 5 : 3))
        {
            _afterimageTimer = 0.045f;
            AddAfterimage();
        }

        if (_dashTimer <= 0f)
        {
            Velocity *= 0.18f;
        }
    }

    private void AddAfterimage()
    {
        _afterimages.Add(new Afterimage
        {
            Position = Position,
            Facing = FacingDirection,
            Remaining = 0.19f,
            Lifetime = 0.19f
        });
    }

    private void StartResonance()
    {
        Resonance = 0f;
        ResonanceActive = true;
        _resonanceTimer = GameBalance.ResonanceDuration;
        _resonanceActivationTimer = 0.5f;
        SoulSenseActive = true;
        AddAfterimage();
    }

    private void UpdateAfterimages(float deltaTime)
    {
        for (int i = _afterimages.Count - 1; i >= 0; i--)
        {
            _afterimages[i].Remaining -= deltaTime;
            if (_afterimages[i].Remaining <= 0f)
            {
                _afterimages.RemoveAt(i);
            }
        }
    }

    public static Vector2 ReadMovement(InputState input)
    {
        Vector2 movement = Vector2.Zero;
        if (input.IsKeyDown(Keys.W)) movement.Y -= 1f;
        if (input.IsKeyDown(Keys.S)) movement.Y += 1f;
        if (input.IsKeyDown(Keys.A)) movement.X -= 1f;
        if (input.IsKeyDown(Keys.D)) movement.X += 1f;

        return movement.LengthSquared() > 1f ? Vector2.Normalize(movement) : movement;
    }

    private void ClampTo(Rectangle bounds)
    {
        Position = new Vector2(
            MathHelper.Clamp(Position.X, bounds.Left + Radius, bounds.Right - Radius),
            MathHelper.Clamp(Position.Y, bounds.Top + Radius, bounds.Bottom - Radius));
    }
}
