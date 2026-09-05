using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Entities;

namespace TheLostSoulOfFire.Rendering;

public sealed record SpriteClip(
    Texture2D Texture,
    int FrameWidth,
    int FrameHeight,
    int FrameCount,
    float FramesPerSecond,
    bool Loop)
{
    public float Duration => FrameCount / FramesPerSecond;

    public int GetFrameIndex(float elapsed)
    {
        int frame = (int)(MathF.Max(0f, elapsed) * FramesPerSecond);
        return Loop ? frame % FrameCount : Math.Min(frame, FrameCount - 1);
    }

    public Rectangle GetSourceRectangle(float elapsed)
    {
        int index = GetFrameIndex(elapsed);
        int columns = Texture.Width / FrameWidth;
        return new Rectangle(
            index % columns * FrameWidth,
            index / columns * FrameHeight,
            FrameWidth,
            FrameHeight);
    }
}

public sealed class SpritePlayback
{
    private string _clipKey = string.Empty;
    private float _startedAt;

    public float Elapsed(string clipKey, float globalTime)
    {
        if (!string.Equals(_clipKey, clipKey, StringComparison.Ordinal))
        {
            _clipKey = clipKey;
            _startedAt = globalTime;
        }

        return MathF.Max(0f, globalTime - _startedAt);
    }

    public bool IsComplete(SpriteClip clip, string clipKey, float globalTime) =>
        !clip.Loop && Elapsed(clipKey, globalTime) >= clip.Duration;
}

public sealed class ArtAssets : IDisposable
{
    private static readonly string[] Directions = ["n", "ne", "e", "se", "s", "sw", "w", "nw"];
    private readonly Dictionary<string, SpriteClip> _characterClips = [];
    private readonly Dictionary<string, SpriteClip> _effects = [];
    private readonly ConditionalWeakTable<object, SpritePlayback> _playbacks = new();
    private float _time;

    public Texture2D Arena { get; }
    private readonly Texture2D _floorSurface;
    public Texture2D PhysicalScythe { get; }
    public Texture2D SoulCannon { get; }
    public Texture2D LostSoul { get; }
    public Texture2D LifeFlame { get; }

    public ArtAssets(ContentManager content)
    {
        Arena = content.Load<Texture2D>("Textures/Environment/arena_base_1800x1000");
        _floorSurface = ArenaFloorSurface.Create(Arena);
        // Delivery filenames are inverted: visual inspection shows the curved
        // scythe in soul_cannon_256 and the straight barrel in scythe_physical_256.
        // Keep original assets/provenance intact and bind by actual silhouette.
        PhysicalScythe = content.Load<Texture2D>("Textures/Weapons/soul_cannon_256");
        SoulCannon = content.Load<Texture2D>("Textures/Weapons/scythe_physical_256");
        LostSoul = content.Load<Texture2D>("Textures/Pickups/lost_soul_64");
        LifeFlame = content.Load<Texture2D>("Textures/Ending/life_flame_128");

        LoadDirectional(content, "player", "Textures/Player/Animations", "idle", 128, 9, 9f, true);
        LoadDirectional(content, "player", "Textures/Player/Animations", "move", 128, 9, 12f, true);

        LoadDirectional(content, "hollow", "Textures/Enemies/Hollow/Animations", "idle", 128, 9, 8f, true);
        LoadDirectional(content, "hollow", "Textures/Enemies/Hollow/Animations", "move", 128, 9, 12f, true);
        LoadDirectional(content, "hollow", "Textures/Enemies/Hollow/Animations", "swipe", 128, 9, 18f, false);

        LoadDirectional(content, "burning", "Textures/Enemies/Burning/Animations", "idle", 128, 9, 9f, true);
        LoadDirectional(content, "burning", "Textures/Enemies/Burning/Animations", "move", 128, 9, 14f, true);
        LoadDirectional(content, "burning", "Textures/Enemies/Burning/Animations", "charge", 128, 9, 15f, false);

        LoadDirectional(content, "devourer", "Textures/Enemies/Devourer/Animations", "idle", 192, 9, 7f, true);
        LoadDirectional(content, "devourer", "Textures/Enemies/Devourer/Animations", "move", 192, 9, 9f, true);
        LoadDirectional(content, "devourer", "Textures/Enemies/Devourer/Animations", "slam", 192, 16, 16f, false);
        LoadDirectional(content, "devourer", "Textures/Enemies/Devourer/Animations", "devour", 192, 9, 9f, true);

        LoadEffect(content, "scythe_slash_01", "fx_scythe_slash_01", 256, 9, 24f, false);
        LoadEffect(content, "scythe_slash_02", "fx_scythe_slash_02", 256, 9, 24f, false);
        LoadEffect(content, "scythe_cleave", "fx_scythe_cleave", 256, 9, 22f, false);
        LoadEffect(content, "core_hit", "fx_core_hit", 128, 9, 30f, false);
        LoadEffect(content, "dash_ignition", "fx_dash_ignition", 128, 9, 45f, false);
        LoadEffect(content, "cannon_charge_loop", "fx_cannon_charge_loop", 128, 9, 12f, true);
        LoadEffect(content, "cannon_muzzle_full", "fx_cannon_muzzle_full", 256, 9, 24f, false);
        LoadEffect(content, "cannon_projectile_full", "fx_cannon_projectile_full", 128, 9, 18f, true);
        LoadEffect(content, "burning_detonation", "fx_burning_detonation", 256, 16, 24f, false);
        LoadEffect(content, "soul_release", "fx_soul_release", 128, 16, 12f, false);
        LoadEffect(content, "resonance_activate", "fx_resonance_activate", 256, 16, 24f, false);
        LoadEffect(content, "death_flame_loop", "fx_death_flame_loop", 128, 9, 12f, true);
    }

    public void Update(float deltaTime) => _time += MathF.Max(0f, deltaTime);

    public SpriteClip GetEffect(string key) => _effects[key];

    public void DrawArena(SpriteBatch batch)
    {
        batch.Draw(Arena, new Rectangle(0, 0, 1800, 1000), Color.White);
        batch.Draw(_floorSurface, new Rectangle(0, 0, 1800, 1000), Color.White);
    }

    public void Dispose() => _floorSurface.Dispose();

    public void DrawPlayer(SpriteBatch batch, Player player)
    {
        if (player.IsDead)
        {
            return;
        }

        string action = player.Velocity.LengthSquared() > 120f ? "move" : "idle";
        DrawDirectional(batch, player, "player", action, player.FacingDirection, player.Position, 100f, Color.White);
    }

    public void DrawEnemy(SpriteBatch batch, Enemy enemy)
    {
        string family;
        string action;
        Vector2 facing;
        float size;

        switch (enemy)
        {
            case Hollow hollow when hollow.State is not (HollowState.Dying or HollowState.Dead):
                family = "hollow";
                action = hollow.State switch
                {
                    HollowState.Approach => "move",
                    HollowState.Telegraph or HollowState.Swipe or HollowState.Recovery => "swipe",
                    _ => "idle"
                };
                facing = hollow.FacingDirection;
                size = 112f;
                break;

            case Burning burning when burning.State is not (BurningState.Dying or BurningState.Detonating or BurningState.Dead):
                family = "burning";
                action = burning.State switch
                {
                    BurningState.Approach => "move",
                    BurningState.Charge => "charge",
                    _ => "idle"
                };
                facing = burning.FacingDirection;
                size = 104f;
                break;

            case Devourer devourer when devourer.State is not (DevourerState.Dying or DevourerState.Dead):
                family = "devourer";
                action = devourer.State switch
                {
                    DevourerState.ApproachPlayer or DevourerState.ApproachSoul => "move",
                    DevourerState.SlamTelegraph or DevourerState.Slam => "slam",
                    DevourerState.Devour => "devour",
                    _ => "idle"
                };
                facing = devourer.FacingDirection;
                size = 174f * (1f + devourer.ConsumedSoulCount * 0.035f);
                break;

            default:
                return;
        }

        Color tint = enemy.HitFlashRemaining > 0f ? new Color(255, 235, 255) : Color.White;
        // Contact cells inspected in the delivered sheets. Timers remain owned
        // by combat; turning and Draw cadence cannot advance an attack early.
        float? frame = enemy switch
        {
            Hollow h when h.State == HollowState.Telegraph => h.TelegraphProgress * 2.99f,
            Hollow h when h.State == HollowState.Swipe => 3f + h.StrikeProgress * 3.99f,
            Hollow h when h.State == HollowState.Recovery => 7f + h.RecoveryProgress * 1.99f,
            Burning b when b.State == BurningState.Charge => b.ChargeProgress * 8.99f,
            Devourer d when d.State == DevourerState.SlamTelegraph => d.TelegraphProgress * 7.99f,
            Devourer d when d.State == DevourerState.Slam => 8f + d.StrikeProgress * 7.99f,
            _ => null
        };
        DrawDirectional(batch, enemy, family, action, facing, enemy.Position, size, tint, frame);
    }

    public void DrawLostSoul(SpriteBatch batch, Soul soul)
    {
        if (soul.State is SoulState.Released or SoulState.Consumed or SoulState.Residue)
        {
            return;
        }

        float pulse = 0.94f + MathF.Sin(_time * 5f) * 0.07f;
        float departure = MathHelper.Clamp((soul.ReleaseProgress - 0.35f) / 0.65f, 0f, 1f);
        batch.Draw(
            LostSoul,
            soul.Position,
            null,
            Color.White * (1f - departure * departure),
            0f,
            new Vector2(LostSoul.Width, LostSoul.Height) * 0.5f,
            0.7f * pulse,
            SpriteEffects.None,
            0f);
    }

    public void DrawLifeFlame(SpriteBatch batch, Vector2 position, float alpha, float scale)
    {
        batch.Draw(
            LifeFlame,
            position,
            null,
            Color.White * alpha,
            0f,
            new Vector2(LifeFlame.Width, LifeFlame.Height) * 0.5f,
            scale,
            SpriteEffects.None,
            0f);
    }

    public void DrawLoopingEffect(
        SpriteBatch batch,
        object owner,
        string effectKey,
        Vector2 position,
        float rotation,
        float scale,
        Color color)
    {
        SpriteClip clip = _effects[effectKey];
        SpritePlayback playback = _playbacks.GetValue(owner, _ => new SpritePlayback());
        float elapsed = playback.Elapsed($"effect/{effectKey}", _time);
        DrawClip(batch, clip, elapsed, position, rotation, scale, color);
    }

    public void DrawCannonProjectile(SpriteBatch batch, CannonShot shot)
    {
        float rotation = MathF.Atan2(shot.Direction.Y, shot.Direction.X);
        float scale = MathHelper.Lerp(0.34f, 0.72f, shot.Charge);
        Color color = shot.IsFullCharge ? Color.White : new Color(220, 190, 255);
        DrawLoopingEffect(batch, shot, "cannon_projectile_full", shot.Position, rotation, scale, color);
    }

    public void DrawDeathFlame(SpriteBatch batch, object owner, Vector2 position) =>
        DrawLoopingEffect(batch, owner, "death_flame_loop", position, 0f, 0.56f, Color.White);

    public static void DrawClip(
        SpriteBatch batch,
        SpriteClip clip,
        float elapsed,
        Vector2 position,
        float rotation,
        float scale,
        Color color)
    {
        batch.Draw(
            clip.Texture,
            position,
            clip.GetSourceRectangle(elapsed),
            color,
            rotation,
            new Vector2(clip.FrameWidth, clip.FrameHeight) * 0.5f,
            scale,
            SpriteEffects.None,
            0f);
    }

    private void DrawDirectional(
        SpriteBatch batch,
        object owner,
        string family,
        string action,
        Vector2 facing,
        Vector2 position,
        float displaySize,
        Color color,
        float? frame = null)
    {
        string direction = GetDirection(facing);
        string key = $"{family}/{action}/{direction}";
        SpriteClip clip = _characterClips[key];
        SpritePlayback playback = _playbacks.GetValue(owner, _ => new SpritePlayback());
        // Facing picks a sheet, not a new action. Turning must not reset gait.
        float elapsed = frame.HasValue ? frame.Value / clip.FramesPerSecond
            : playback.Elapsed($"{family}/{action}", _time);
        DrawClip(batch, clip, elapsed, position, 0f, displaySize / clip.FrameWidth, color);
    }

    private void LoadDirectional(
        ContentManager content,
        string family,
        string contentRoot,
        string action,
        int frameSize,
        int frameCount,
        float framesPerSecond,
        bool loop)
    {
        foreach (string direction in Directions)
        {
            Texture2D texture = content.Load<Texture2D>($"{contentRoot}/{action}/{direction}");
            _characterClips[$"{family}/{action}/{direction}"] = new SpriteClip(
                texture,
                frameSize,
                frameSize,
                frameCount,
                framesPerSecond,
                loop);
        }
    }

    private void LoadEffect(
        ContentManager content,
        string key,
        string filename,
        int frameSize,
        int frameCount,
        float framesPerSecond,
        bool loop)
    {
        Texture2D texture = content.Load<Texture2D>($"Textures/Effects/{filename}");
        _effects[key] = new SpriteClip(texture, frameSize, frameSize, frameCount, framesPerSecond, loop);
    }

    private static string GetDirection(Vector2 direction)
    {
        if (direction.LengthSquared() < 0.001f)
        {
            return "s";
        }

        float degrees = MathHelper.ToDegrees(MathF.Atan2(direction.Y, direction.X));
        if (degrees < 0f)
        {
            degrees += 360f;
        }

        int sector = (int)MathF.Floor((degrees + 22.5f) / 45f) % 8;
        return sector switch
        {
            0 => "e",
            1 => "se",
            2 => "s",
            3 => "sw",
            4 => "w",
            5 => "nw",
            6 => "n",
            _ => "ne"
        };
    }
}
