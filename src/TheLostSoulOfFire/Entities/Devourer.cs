using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Entities;

public enum DevourerState
{
    ApproachPlayer,
    ApproachSoul,
    SlamTelegraph,
    Slam,
    Devour,
    Recovery,
    Staggered,
    Dying,
    Dead
}

public sealed class Devourer : Enemy
{
    private readonly List<Soul> _consumedSouls = [];
    private float _stateTimer;
    private float _visualTime;
    private bool _slamDamagePending;
    private bool _soulSpawnPending;
    private bool _extractionEffectPending;
    private Soul _targetSoul;
    private Vector2 _facing = -Vector2.UnitY;

    public DevourerState State { get; private set; } = DevourerState.ApproachPlayer;
    public override string StateLabel => State.ToString().ToUpperInvariant();
    public int ConsumedSoulCount => _consumedSouls.Count;
    public Vector2 FacingDirection => _facing;
    public Vector2 TorsoPosition => Position + new Vector2(0f, -8f);
    public float TelegraphProgress => MathHelper.Clamp(1f - _stateTimer / GameBalance.DevourerSlamTelegraph, 0f, 1f);
    public float StrikeProgress => MathHelper.Clamp(1f - _stateTimer / GameBalance.DevourerSlamDuration, 0f, 1f);

    public Devourer(Vector2 position)
        : base(position, GameBalance.DevourerMaxHealth, GameBalance.DevourerRadius)
    {
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

        if (State == DevourerState.Dead)
        {
            return;
        }

        if (State == DevourerState.Dying)
        {
            UpdateDying(deltaTime, particles);
            return;
        }

        _stateTimer = MathF.Max(0f, _stateTimer - deltaTime);
        if (State is not (DevourerState.SlamTelegraph or DevourerState.Slam or DevourerState.Devour or DevourerState.Recovery or DevourerState.Staggered))
        {
            Soul availableSoul = FindClosestSoul(souls);
            if (availableSoul is not null)
            {
                if (_targetSoul != availableSoul)
                {
                    particles.EmitBurst(TorsoPosition, Vector2.UnitY, 10, GameBalance.DeathFlame, 90f, 5f);
                }

                _targetSoul = availableSoul;
                State = DevourerState.ApproachSoul;
            }
            else
            {
                _targetSoul = null;
                State = DevourerState.ApproachPlayer;
            }
        }

        switch (State)
        {
            case DevourerState.ApproachPlayer:
                UpdateApproachPlayer(deltaTime, player);
                break;

            case DevourerState.ApproachSoul:
                UpdateApproachSoul(deltaTime);
                break;

            case DevourerState.SlamTelegraph:
                Face(player.Position);
                if (_stateTimer <= 0f)
                {
                    State = DevourerState.Slam;
                    _stateTimer = GameBalance.DevourerSlamDuration;
                    _slamDamagePending = true;
                    screenEffects.AddShake(0.15f, 7f);
                }
                break;

            case DevourerState.Slam:
                ResolveSlam(player, screenEffects);
                if (_stateTimer <= 0f)
                {
                    State = DevourerState.Recovery;
                    _stateTimer = GameBalance.DevourerRecoveryDuration;
                }
                break;

            case DevourerState.Devour:
                UpdateDevour(deltaTime, particles);
                break;

            case DevourerState.Recovery:
            case DevourerState.Staggered:
                if (_stateTimer <= 0f)
                {
                    State = DevourerState.ApproachPlayer;
                }
                break;
        }
    }

    public override void ApplyDamage(DamageInfo damage)
    {
        DevourerState previousState = State;
        Soul interruptedSoul = _targetSoul;
        base.ApplyDamage(damage);
        if (!IsAlive)
        {
            return;
        }

        if (previousState == DevourerState.Devour && interruptedSoul is not null)
        {
            interruptedSoul.CancelDevour();
            _targetSoul = null;
            State = DevourerState.Staggered;
            _stateTimer = 0.38f;
        }

        if (damage.IsFullCannon)
        {
            if (_targetSoul?.State == SoulState.BeingDevoured)
            {
                _targetSoul.CancelDevour();
                _targetSoul = null;
            }

            State = DevourerState.Staggered;
            _stateTimer = GameBalance.DevourerFullCannonStagger;
            ExpelOneSoul();
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

    public bool TryConsumeExtractionEffect(out Vector2 position)
    {
        if (!_extractionEffectPending)
        {
            position = default;
            return false;
        }

        _extractionEffectPending = false;
        position = TorsoPosition;
        return true;
    }

    public override void Draw(
        SpriteBatch batch,
        Texture2D pixel,
        bool debugVisible,
        bool soulSenseActive,
        bool useSpriteArt)
    {
        if (State == DevourerState.Dead)
        {
            return;
        }

        float pulse = 0.5f + 0.5f * MathF.Sin(_visualTime * 4.2f);
        float stackScale = 1f + ConsumedSoulCount * 0.035f;
        Color body = HitFlashRemaining > 0f ? GameBalance.SoulWhite : new Color(27, 25, 33);
        Vector2 right = new(-_facing.Y, _facing.X);

        if (State == DevourerState.Dying)
        {
            float progress = 1f - _stateTimer / GameBalance.DevourerDeathDuration;
            batch.FillCircle(pixel, Position, (55f + progress * 18f) * stackScale, body * (1f - progress * 0.8f));
            for (int i = 0; i < 7; i++)
            {
                float angle = i * MathHelper.TwoPi / 7f;
                Vector2 crack = new(MathF.Cos(angle), MathF.Sin(angle));
                batch.DrawLine(pixel, TorsoPosition, TorsoPosition + crack * (22f + progress * 45f), GameBalance.DeathFlameBright * (1f - progress), 4f);
            }
            return;
        }

        if (!useSpriteArt)
        {
            batch.FillCircle(pixel, Position + new Vector2(7f, 30f), 54f * stackScale, new Color(3, 3, 7) * 0.66f);
            batch.FillCircle(pixel, Position, 48f * stackScale, body);
            batch.DrawLine(pixel, Position - right * 32f, Position - right * 56f + _facing * 25f, body, 25f);
            batch.DrawLine(pixel, Position + right * 32f, Position + right * 57f + _facing * 22f, body, 25f);
            batch.DrawLine(pixel, Position - right * 21f + new Vector2(0f, 30f), Position - right * 26f + new Vector2(0f, 59f), new Color(20, 19, 25), 25f);
            batch.DrawLine(pixel, Position + right * 21f + new Vector2(0f, 30f), Position + right * 27f + new Vector2(0f, 57f), new Color(20, 19, 25), 25f);
            batch.FillCircle(pixel, Position + new Vector2(0f, -52f), 18f, new Color(20, 19, 26));
        }

        if (!useSpriteArt)
            batch.FillCircle(pixel, TorsoPosition, 28f, new Color(7, 5, 10));
        // Keep the painted torso cavity visible; a solid primitive disk erased it.
        batch.DrawArc(pixel, TorsoPosition, 22f + pulse, 0.25f, 1.7f,
            GameBalance.DeepViolet * (0.38f + ConsumedSoulCount * 0.07f), 2f, 16);

        if (State == DevourerState.ApproachSoul && _targetSoul is not null)
        {
            batch.DrawLine(pixel, TorsoPosition, _targetSoul.Position, GameBalance.DeathFlame * 0.48f, 4f);
            batch.DrawCircle(pixel, _targetSoul.Position, 31f + pulse * 8f, GameBalance.DeathFlameBright * 0.72f, 4f, 24);
        }
        else if (State == DevourerState.Devour && _targetSoul is not null)
        {
            batch.DrawLine(pixel, TorsoPosition, _targetSoul.Position, GameBalance.DeepViolet * 0.9f, 15f);
            batch.DrawLine(pixel, TorsoPosition, _targetSoul.Position, GameBalance.DeathFlameBright * 0.8f, 4f);
        }

        if (soulSenseActive)
        {
            batch.FillCircle(pixel, TorsoPosition, GameBalance.DevourerTorsoRadius, GameBalance.DeepViolet * 0.72f);
            int visibleSouls = Math.Max(1, ConsumedSoulCount);
            for (int i = 0; i < visibleSouls; i++)
            {
                float angle = _visualTime * (1.2f + i * 0.16f) + i * MathHelper.TwoPi / visibleSouls;
                Vector2 trappedPosition = TorsoPosition + new Vector2(MathF.Cos(angle) * 13f, MathF.Sin(angle) * 10f);
                batch.FillCircle(pixel, trappedPosition, ConsumedSoulCount > 0 ? 5f : 3f, ConsumedSoulCount > 0 ? GameBalance.SoulWhite : GameBalance.DeathFlame * 0.45f);
            }
        }

        if (debugVisible)
        {
            batch.DrawCircle(pixel, Position, Radius, new Color(80, 220, 210), 2f);
            batch.DrawCircle(pixel, TorsoPosition, GameBalance.DevourerTorsoRadius, new Color(255, 210, 80), 2f);
        }
    }

    protected override void OnDeath()
    {
        _targetSoul?.CancelDevour();
        _targetSoul = null;
        State = DevourerState.Dying;
        _stateTimer = GameBalance.DevourerDeathDuration;

        for (int i = _consumedSouls.Count - 1; i >= 0; i--)
        {
            float angle = i * MathHelper.TwoPi / Math.Max(1, _consumedSouls.Count);
            _consumedSouls[i].Expel(Position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 58f);
        }

        _consumedSouls.Clear();
    }

    private Soul FindClosestSoul(IReadOnlyList<Soul> souls)
    {
        float maxDistanceSquared = GameBalance.DevourerSoulTargetRange * GameBalance.DevourerSoulTargetRange;
        Soul closest = null;
        foreach (Soul soul in souls.Where(soul => soul.CanBeDevoured))
        {
            float distanceSquared = Vector2.DistanceSquared(Position, soul.Position);
            if (distanceSquared < maxDistanceSquared)
            {
                maxDistanceSquared = distanceSquared;
                closest = soul;
            }
        }

        return closest;
    }

    private void UpdateApproachPlayer(float deltaTime, Player player)
    {
        Face(player.Position);
        float distance = Vector2.Distance(Position, player.Position);
        if (distance <= GameBalance.DevourerSlamStartRange)
        {
            State = DevourerState.SlamTelegraph;
            _stateTimer = GameBalance.DevourerSlamTelegraph;
            return;
        }

        Position += _facing * GameBalance.DevourerMoveSpeed * deltaTime;
    }

    private void UpdateApproachSoul(float deltaTime)
    {
        if (_targetSoul is null || !_targetSoul.CanBeDevoured)
        {
            _targetSoul = null;
            State = DevourerState.ApproachPlayer;
            return;
        }

        Face(_targetSoul.Position);
        if (Vector2.DistanceSquared(Position, _targetSoul.Position) <= GameBalance.DevourerDevourStartRange * GameBalance.DevourerDevourStartRange)
        {
            State = DevourerState.Devour;
            _stateTimer = GameBalance.DevourerDevourDuration;
            _targetSoul.BeginDevour();
            return;
        }

        Position += _facing * GameBalance.DevourerMoveSpeed * deltaTime;
    }

    private void UpdateDevour(float deltaTime, ParticleSystem particles)
    {
        if (_targetSoul is null || _targetSoul.State != SoulState.BeingDevoured)
        {
            _targetSoul = null;
            State = DevourerState.ApproachPlayer;
            return;
        }

        _targetSoul.PullToward(TorsoPosition, deltaTime);
        if (_stateTimer > 0f)
        {
            return;
        }

        _targetSoul.Consume();
        _consumedSouls.Add(_targetSoul);
        _targetSoul = null;
        Health = Math.Min(MaxHealth, Health + GameBalance.DevourerHealPerSoul);
        particles.EmitDeathFlame(TorsoPosition, 18, 1.25f);
        State = DevourerState.Recovery;
        _stateTimer = GameBalance.DevourerRecoveryDuration;
    }

    private void ResolveSlam(Player player, ScreenEffects screenEffects)
    {
        if (!_slamDamagePending)
        {
            return;
        }

        _slamDamagePending = false;
        Vector2 toPlayer = player.Position - Position;
        if (toPlayer.LengthSquared() > MathF.Pow(GameBalance.DevourerSlamRange + player.Radius, 2f))
        {
            return;
        }

        Vector2 direction = toPlayer.LengthSquared() > 0.001f ? Vector2.Normalize(toPlayer) : _facing;
        int damage = GameBalance.DevourerSlamDamage + Math.Min(ConsumedSoulCount, GameBalance.DevourerMaxSoulStacks) * GameBalance.DevourerDamagePerSoul;
        player.ApplyDamage(damage, direction * GameBalance.DevourerSlamKnockback, screenEffects);
    }

    private void ExpelOneSoul()
    {
        if (_consumedSouls.Count == 0)
        {
            return;
        }

        Soul soul = _consumedSouls[^1];
        _consumedSouls.RemoveAt(_consumedSouls.Count - 1);
        soul.Expel(TorsoPosition + _facing * 74f);
        _extractionEffectPending = true;
    }

    private void UpdateDying(float deltaTime, ParticleSystem particles)
    {
        float previous = _stateTimer;
        _stateTimer = MathF.Max(0f, _stateTimer - deltaTime);
        if (previous > 0.38f && _stateTimer <= 0.38f)
        {
            particles.EmitBurst(Position, -_facing, 44, new Color(44, 38, 50), 250f, 12f);
            particles.EmitDeathFlame(Position, 24, 1.5f);
            _soulSpawnPending = true;
        }

        if (_stateTimer <= 0f)
        {
            State = DevourerState.Dead;
            IsFinished = true;
        }
    }

    private void Face(Vector2 target)
    {
        Vector2 direction = target - Position;
        if (direction.LengthSquared() > 0.001f)
        {
            _facing = Vector2.Normalize(direction);
        }
    }
}
