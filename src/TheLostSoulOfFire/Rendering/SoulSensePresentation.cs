using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// Owns the visual transition into the hidden soul layer. Gameplay continues to use
/// Player.SoulSenseActive; these curves exist only to stage world recession and soul emergence.
/// </summary>
public sealed class SoulSensePresentation
{
    private const float ActivationDuration = 0.25f;
    private const float DeactivationDuration = 0.15f;

    private static readonly Vector2[][] TracePaths =
    [
        [
            new(178f, 758f), new(276f, 732f), new(382f, 686f), new(492f, 664f),
            new(612f, 690f), new(735f, 704f), new(842f, 683f), new(955f, 648f)
        ],
        [
            new(948f, 232f), new(1068f, 248f), new(1175f, 296f), new(1278f, 346f),
            new(1390f, 358f), new(1508f, 321f), new(1627f, 334f)
        ],
        [
            new(356f, 268f), new(454f, 292f), new(548f, 342f), new(650f, 375f),
            new(760f, 361f), new(858f, 390f)
        ]
    ];

    private float _transition;

    /// <summary>Early curve: the physical scene becomes quieter in roughly the first 100 ms.</summary>
    public float WorldSuppression => SmoothStep(0f, 0.42f, _transition);

    /// <summary>Delayed curve: supernatural information resolves after the world starts receding.</summary>
    public float SoulEmergence => SmoothStep(0.36f, 1f, _transition);

    public void Update(float deltaTime, bool active)
    {
        float duration = active ? ActivationDuration : DeactivationDuration;
        float target = active ? 1f : 0f;
        _transition = MoveTowards(_transition, target, MathF.Max(0f, deltaTime) / duration);
    }

    public void Reset() => _transition = 0f;

    public void DrawSoulLayer(
        SpriteBatch batch,
        Texture2D pixel,
        Matrix worldTransform,
        Player player,
        IReadOnlyList<Enemy> enemies,
        IReadOnlyList<Soul> souls,
        float presentationTime)
    {
        float amount = SoulEmergence;
        if (amount <= 0.001f)
        {
            return;
        }

        batch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            transformMatrix: worldTransform);

        bool imminentThreat = false;
        foreach (Enemy enemy in enemies)
            imminentThreat |= enemy is Hollow { State: HollowState.Telegraph or HollowState.Swipe }
                or Burning { State: BurningState.Telegraph or BurningState.Charge }
                or Devourer { State: DevourerState.SlamTelegraph or DevourerState.Slam };
        DrawTraces(batch, pixel, presentationTime, amount * (imminentThreat ? 0.22f : 0.55f));
        DrawSouls(batch, pixel, souls, presentationTime, amount);
        DrawEnemySouls(batch, pixel, enemies, presentationTime, amount);
        DrawPlayerResponse(batch, pixel, player, presentationTime, amount);

        batch.End();
    }

    private static void DrawTraces(SpriteBatch batch, Texture2D pixel, float time, float amount)
    {
        float breathe = 0.82f + MathF.Sin(time * 2.1f) * 0.18f;
        for (int pathIndex = 0; pathIndex < TracePaths.Length; pathIndex++)
        {
            Vector2[] path = TracePaths[pathIndex];
            for (int segment = 0; segment < path.Length - 1; segment++)
            {
                Vector2 start = path[segment];
                Vector2 end = path[segment + 1];
                Vector2 delta = end - start;
                int pieces = Math.Max(2, (int)MathF.Ceiling(delta.Length() / 22f));

                for (int piece = 0; piece < pieces; piece++)
                {
                    // Uneven two-on/one-off cadence reads as residue instead of navigation markup.
                    if ((piece + segment * 2 + pathIndex) % 3 == 2)
                    {
                        continue;
                    }

                    float from = (piece + 0.12f) / pieces;
                    float to = MathF.Min(1f, (piece + 0.78f) / pieces);
                    Vector2 a = Vector2.Lerp(start, end, from);
                    Vector2 b = Vector2.Lerp(start, end, to);
                    batch.DrawLine(pixel, a, b, GameBalance.DeepViolet * (0.25f * amount), 5f);
                    batch.DrawLine(pixel, a, b, GameBalance.SoulSenseTrace * (0.22f * breathe * amount), 1.5f);
                }
            }

            for (int node = 1; node < path.Length - 1; node += 2)
            {
                float nodePulse = 0.7f + 0.3f * MathF.Sin(time * 2.7f + pathIndex * 1.9f + node);
                batch.DrawLine(pixel, path[node] - new Vector2(2f, 0f), path[node] + new Vector2(2f, -3f),
                    GameBalance.SoulSenseTrace * (0.22f * nodePulse * amount), 1f);
            }

            float travel = (time * 0.055f + pathIndex * 0.31f) % 1f;
            Vector2 mote = PointAlongPath(path, travel);
            batch.FillCircle(pixel, mote, 2f, GameBalance.SoulWhite * (0.58f * amount));
        }
    }

    private static void DrawSouls(
        SpriteBatch batch,
        Texture2D pixel,
        IReadOnlyList<Soul> souls,
        float time,
        float amount)
    {
        float pulse = 0.5f + 0.5f * MathF.Sin(time * 4.4f);
        foreach (Soul soul in souls)
        {
            if (soul.State is SoulState.Released or SoulState.Consumed)
            {
                continue;
            }

            float scale = soul.State == SoulState.Residue ? 0.55f : 1f;
            float urgency = soul.State == SoulState.BeingDevoured ? 1.2f : 1f;
            batch.DrawCircle(
                pixel,
                soul.Position,
                (20f + pulse * 4f) * scale * urgency,
                GameBalance.DeathFlameBright * (0.48f * amount),
                2f,
                20);
            batch.FillCircle(pixel, soul.Position, 7f * scale, GameBalance.DeathFlame * (0.44f * amount));
            batch.FillCircle(pixel, soul.Position, 3f * scale, GameBalance.SoulWhite * (0.94f * amount));
        }
    }

    private static void DrawEnemySouls(
        SpriteBatch batch,
        Texture2D pixel,
        IReadOnlyList<Enemy> enemies,
        float time,
        float amount)
    {
        float pulse = 0.5f + 0.5f * MathF.Sin(time * 5.2f);
        foreach (Enemy enemy in enemies)
        {
            if (!enemy.IsAlive)
            {
                continue;
            }

            switch (enemy)
            {
                case Hollow hollow:
                    DrawCriticalCore(batch, pixel, hollow.CorePosition, 13f, pulse, amount);
                    break;

                case Burning burning:
                    Vector2[] fractures = burning.GetFracturePositions();
                    foreach (Vector2 fracture in fractures)
                    {
                        batch.DrawLine(pixel, fracture - new Vector2(5f, 7f), fracture + new Vector2(4f, 6f),
                            new Color(7, 6, 12) * amount, 6f);
                        batch.DrawLine(pixel, fracture - new Vector2(4f, 6f), fracture + new Vector2(3f, 5f),
                            GameBalance.DeathFlameBright * (0.82f * amount), 2f);
                        batch.FillCircle(pixel, fracture, 2.5f, GameBalance.SoulWhite * amount);
                    }
                    break;

                case Devourer devourer:
                    DrawDevourerSoul(batch, pixel, devourer, time, pulse, amount);
                    break;
            }
        }
    }

    private static void DrawCriticalCore(
        SpriteBatch batch,
        Texture2D pixel,
        Vector2 position,
        float radius,
        float pulse,
        float amount)
    {
        batch.FillCircle(pixel, position, 9f, new Color(7, 6, 12) * (0.88f * amount));
        float extent = 10f + pulse;
        for (int i = 0; i < 4; i++)
        {
            float angle = i * MathHelper.PiOver2;
            Vector2 axis = new(MathF.Cos(angle), MathF.Sin(angle));
            batch.DrawLine(pixel, position + axis * extent, position + axis * (extent + 3f),
                GameBalance.DeathFlameBright * (0.64f * amount), 1.5f);
        }
        batch.FillCircle(pixel, position, 5f, GameBalance.DeathFlameBright * (0.9f * amount));
        batch.FillCircle(pixel, position, 3f, GameBalance.SoulWhite * amount);
    }

    private static void DrawDevourerSoul(
        SpriteBatch batch,
        Texture2D pixel,
        Devourer devourer,
        float time,
        float pulse,
        float amount)
    {
        Vector2 torso = devourer.TorsoPosition;
        batch.DrawArc(pixel, torso, 18f + pulse, 0.3f, 2.2f, GameBalance.DeathFlame * (0.38f * amount), 2f, 18);
        batch.FillCircle(pixel, torso, 3f, GameBalance.DeathFlameBright * (0.86f * amount));

        for (int index = 0; index < devourer.ConsumedSoulCount; index++)
        {
            float angle = time * (1.05f + index * 0.12f) + index * MathHelper.TwoPi / devourer.ConsumedSoulCount;
            Vector2 trapped = torso + new Vector2(MathF.Cos(angle) * 16f, MathF.Sin(angle) * 11f);
            batch.FillCircle(pixel, trapped, 5f, GameBalance.DeathFlameBright * (0.78f * amount));
            batch.FillCircle(pixel, trapped, 2f, GameBalance.SoulWhite * amount);
        }
    }

    private static void DrawPlayerResponse(
        SpriteBatch batch,
        Texture2D pixel,
        Player player,
        float time,
        float amount)
    {
        if (player.IsDead)
        {
            return;
        }

        float pulse = 0.5f + 0.5f * MathF.Sin(time * 4.8f);
        Vector2 core = player.Position + player.FacingDirection * 2f;
        Vector2 eye = player.Position + player.FacingDirection * 26f;
        batch.DrawCircle(pixel, core, 11f + pulse * 2f, GameBalance.DeathFlameBright * (0.28f * amount), 1.5f, 16);
        batch.FillCircle(pixel, eye, 2f, GameBalance.SoulWhite * (0.9f * amount));
    }

    private static Vector2 PointAlongPath(Vector2[] path, float amount)
    {
        float totalLength = 0f;
        for (int i = 0; i < path.Length - 1; i++)
        {
            totalLength += Vector2.Distance(path[i], path[i + 1]);
        }

        float target = totalLength * MathHelper.Clamp(amount, 0f, 1f);
        for (int i = 0; i < path.Length - 1; i++)
        {
            float length = Vector2.Distance(path[i], path[i + 1]);
            if (target <= length)
            {
                return Vector2.Lerp(path[i], path[i + 1], length <= 0f ? 0f : target / length);
            }
            target -= length;
        }

        return path[^1];
    }

    private static float SmoothStep(float minimum, float maximum, float value)
    {
        float amount = MathHelper.Clamp((value - minimum) / (maximum - minimum), 0f, 1f);
        return amount * amount * (3f - 2f * amount);
    }

    private static float MoveTowards(float current, float target, float maximumDelta)
    {
        if (MathF.Abs(target - current) <= maximumDelta)
        {
            return target;
        }

        return current + MathF.Sign(target - current) * maximumDelta;
    }
}
