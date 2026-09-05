using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Input;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Combat;

public readonly record struct ScytheStrike(
    int Step,
    int Damage,
    float Range,
    float ArcRadians,
    float Knockback,
    Vector2 Direction);

public sealed class ScytheCombat
{
    private float _attackElapsed;
    private float _attackDuration;
    private float _strikeTime;
    private float _comboTimer;
    private bool _strikeCreated;
    private bool _strikePending;
    private bool _queuedAttack;
    private int _nextStep = 1;
    private Vector2 _attackDirection = Vector2.UnitX;
    private bool _resonanceActive;

    public int ActiveStep { get; private set; }
    public bool StartedThisFrame { get; private set; }
    public Vector2 AttackDirection => _attackDirection;
    public float NormalizedProgress => ActiveStep == 0 ? 0f : MathHelper.Clamp(_attackElapsed / _attackDuration, 0f, 1f);
    public string StateLabel => ActiveStep == 0 ? (_comboTimer > 0f ? $"CHAIN {_nextStep}" : "READY") : $"HIT {ActiveStep}";

    public void Reset()
    {
        _attackElapsed = 0f;
        _attackDuration = 0f;
        _comboTimer = 0f;
        _strikeCreated = false;
        _strikePending = false;
        _queuedAttack = false;
        _nextStep = 1;
        ActiveStep = 0;
    }

    public void Update(
        float deltaTime,
        InputState input,
        Vector2 facingDirection,
        Vector2 playerPosition,
        ParticleSystem particles,
        bool canStartAttack,
        bool resonanceActive)
    {
        StartedThisFrame = false;
        _resonanceActive = resonanceActive;

        if (ActiveStep == 0)
        {
            _comboTimer = MathF.Max(0f, _comboTimer - deltaTime);
            if (_comboTimer <= 0f)
            {
                _nextStep = 1;
            }

            if (canStartAttack && (input.WasLeftMousePressed || _queuedAttack))
            {
                _queuedAttack = false;
                StartAttack(facingDirection, playerPosition, particles);
            }

            return;
        }

        if (input.WasLeftMousePressed && _attackElapsed > 0.055f)
        {
            _queuedAttack = true;
        }

        _attackElapsed += deltaTime;
        if (!_strikeCreated && _attackElapsed >= _strikeTime)
        {
            _strikeCreated = true;
            _strikePending = true;
        }

        if (_attackElapsed < _attackDuration)
        {
            return;
        }

        ActiveStep = 0;
        _comboTimer = GameBalance.ComboResetTime;
        if (canStartAttack && _queuedAttack)
        {
            _queuedAttack = false;
            StartAttack(facingDirection, playerPosition, particles);
        }
    }

    public bool TryConsumeStrike(out ScytheStrike strike)
    {
        if (!_strikePending)
        {
            strike = default;
            return false;
        }

        _strikePending = false;
        strike = BuildStrike(ActiveStep, _attackDirection, _resonanceActive);
        return true;
    }

    public float GetForwardImpulse()
    {
        float impulse = ActiveStep switch
        {
            1 => 105f,
            2 => 132f,
            3 => 225f,
            _ => 0f
        };
        return impulse * (_resonanceActive ? 1.18f : 1f);
    }

    public void Draw(
        SpriteBatch batch,
        Texture2D pixel,
        Texture2D physicalScythe,
        Vector2 playerPosition,
        Vector2 facingDirection,
        bool debugVisible)
    {
        if (ActiveStep == 0)
        {
            DrawRestingScythe(batch, physicalScythe, playerPosition, facingDirection);
            return;
        }

        DrawAttackingScythe(batch, pixel, physicalScythe, playerPosition, debugVisible);
    }

    private void StartAttack(Vector2 facingDirection, Vector2 playerPosition, ParticleSystem particles)
    {
        ActiveStep = _nextStep;
        _nextStep = ActiveStep == 3 ? 1 : ActiveStep + 1;
        _attackDirection = facingDirection.LengthSquared() > 0.001f ? Vector2.Normalize(facingDirection) : Vector2.UnitX;
        _attackElapsed = 0f;
        _strikeCreated = false;
        StartedThisFrame = true;

        (_attackDuration, _strikeTime) = ActiveStep switch
        {
            1 => (0.205f, 0.062f),
            2 => (0.255f, 0.085f),
            _ => (0.42f, 0.155f)
        };

        Color flame = ActiveStep == 3 ? GameBalance.DeathFlameBright : GameBalance.DeathFlame;
        int ignitionParticles = ActiveStep switch { 1 => 2, 2 => 4, _ => 8 };
        particles.EmitBurst(
            playerPosition + _attackDirection * 42f,
            _attackDirection,
            ignitionParticles,
            flame,
            ActiveStep == 3 ? 115f : 60f,
            ActiveStep == 3 ? 6f : 3f);
    }

    private static ScytheStrike BuildStrike(int step, Vector2 direction, bool resonanceActive)
    {
        ScytheStrike strike = step switch
        {
            1 => new ScytheStrike(1, GameBalance.ScytheDamage1, GameBalance.ScytheRange1, MathHelper.ToRadians(120f), 170f, direction),
            2 => new ScytheStrike(2, GameBalance.ScytheDamage2, GameBalance.ScytheRange2, MathHelper.ToRadians(140f), 220f, direction),
            _ => new ScytheStrike(3, GameBalance.ScytheDamage3, GameBalance.ScytheRange3, MathHelper.ToRadians(198f), 410f, direction)
        };

        if (!resonanceActive)
        {
            return strike;
        }

        return strike with
        {
            Damage = (int)MathF.Round(strike.Damage * GameBalance.ResonanceScytheDamageMultiplier),
            Range = strike.Range * GameBalance.ResonanceScytheRangeMultiplier,
            Knockback = strike.Knockback * GameBalance.ResonanceScytheKnockbackMultiplier
        };
    }

    private static void DrawRestingScythe(
        SpriteBatch batch,
        Texture2D physicalScythe,
        Vector2 playerPosition,
        Vector2 facingDirection)
    {
        Vector2 right = new(-facingDirection.Y, facingDirection.X);
        float rotation = MathF.Atan2(facingDirection.Y, facingDirection.X) + 0.35f;
        batch.Draw(
            physicalScythe,
            playerPosition + facingDirection * 12f + right * 8f,
            null,
            Color.White,
            rotation,
            new Vector2(148f, 158f),
            0.46f,
            SpriteEffects.None,
            0f);
    }

    private void DrawAttackingScythe(
        SpriteBatch batch,
        Texture2D pixel,
        Texture2D physicalScythe,
        Vector2 playerPosition,
        bool debugVisible)
    {
        float aim = MathF.Atan2(_attackDirection.Y, _attackDirection.X);
        float attackProgress = NormalizedProgress;
        float swingProgress = ActiveStep == 3
            ? MathHelper.Clamp((attackProgress - 0.2f) / 0.58f, 0f, 1f)
            : attackProgress;
        float eased = 1f - MathF.Pow(1f - swingProgress, ActiveStep == 3 ? 2.35f : 3f);
        float totalArc = ActiveStep switch
        {
            1 => MathHelper.ToRadians(120f),
            2 => -MathHelper.ToRadians(140f),
            _ => MathHelper.ToRadians(198f)
        };
        float start = aim - totalArc * 0.5f;
        float current = start + totalArc * eased;
        float radius = ActiveStep switch { 1 => 88f, 2 => 99f, _ => 119f };
        float thickness = ActiveStep switch { 1 => 2f, 2 => 3.5f, _ => 7f };
        if (_resonanceActive)
        {
            radius *= GameBalance.ResonanceScytheRangeMultiplier;
            thickness *= 1.22f;
        }
        Color trail = ActiveStep switch
        {
            1 => GameBalance.DeathFlame * 0.58f,
            2 => GameBalance.DeathFlameBright * 0.78f,
            _ => GameBalance.DeathFlameBright * 0.94f
        };

        float fadeStart = ActiveStep == 3 ? 0.7f : 0.62f;
        float trailAlpha = 1f - MathHelper.Clamp((attackProgress - fadeStart) / (1f - fadeStart), 0f, 1f);
        float visibleSweep = totalArc * MathHelper.Clamp(eased, 0.08f, 1f);
        float outerThickness = thickness + (ActiveStep switch { 1 => 2f, 2 => 3f, _ => 5f });
        batch.DrawArc(pixel, playerPosition, radius, start, visibleSweep, GameBalance.DeepViolet * (0.62f * trailAlpha), outerThickness, ActiveStep == 3 ? 34 : 24);
        batch.DrawArc(pixel, playerPosition, radius, start, visibleSweep, trail * trailAlpha, thickness, ActiveStep == 3 ? 34 : 24);
        if (ActiveStep == 3)
        {
            batch.DrawArc(pixel, playerPosition, radius + 3f, start, visibleSweep, GameBalance.SoulWhite * (0.72f * trailAlpha), 2.5f, 34);
        }

        Vector2 bladeDirection = new(MathF.Cos(current), MathF.Sin(current));
        batch.Draw(
            physicalScythe,
            playerPosition + bladeDirection * 29f,
            null,
            Color.White,
            current + MathHelper.PiOver2,
            new Vector2(148f, 158f),
            ActiveStep switch { 1 => 0.55f, 2 => 0.6f, _ => 0.7f },
            SpriteEffects.None,
            0f);

        if (debugVisible)
        {
            ScytheStrike strike = BuildStrike(ActiveStep, _attackDirection, _resonanceActive);
            batch.DrawArc(pixel, playerPosition, strike.Range, aim - strike.ArcRadians * 0.5f, strike.ArcRadians, new Color(80, 220, 210) * 0.65f, 2f, 28);
        }
    }
}
