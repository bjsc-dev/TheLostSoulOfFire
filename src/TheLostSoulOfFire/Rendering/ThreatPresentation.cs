using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// Draws critical enemy warnings above the scene and Soul Sense pass. Geometry follows
/// the combat state machines so reduced effects and lighting cannot hide an attack cue.
/// </summary>
public static class ThreatPresentation
{
    private static readonly Color Separator = new(5, 4, 9);

    public static void Draw(SpriteBatch batch, Texture2D pixel, IReadOnlyList<Enemy> enemies)
    {
        foreach (Enemy enemy in enemies)
        {
            if (!enemy.IsAlive)
            {
                continue;
            }

            switch (enemy)
            {
                case Hollow hollow:
                    DrawHollow(batch, pixel, hollow);
                    break;
                case Burning burning:
                    DrawBurning(batch, pixel, burning);
                    break;
                case Devourer devourer:
                    DrawDevourer(batch, pixel, devourer);
                    break;
            }
        }
    }

    private static void DrawHollow(SpriteBatch batch, Texture2D pixel, Hollow hollow)
    {
        if (hollow.State is not (HollowState.Telegraph or HollowState.Swipe))
        {
            return;
        }

        float direction = MathF.Atan2(hollow.FacingDirection.Y, hollow.FacingDirection.X);
        const float halfSweep = 0.72f;
        float alpha = hollow.State == HollowState.Telegraph
            ? 0.32f + hollow.TelegraphProgress * 0.42f
            : 0.9f;
        Color edge = (hollow.State == HollowState.Swipe ? GameBalance.SoulWhite : GameBalance.DeathFlameBright) * alpha;

        DrawWedge(batch, pixel, hollow.Position, direction, halfSweep, GameBalance.HollowSwipeRange,
            Separator * 0.8f, edge);
    }

    private static void DrawBurning(SpriteBatch batch, Texture2D pixel, Burning burning)
    {
        if (burning.State == BurningState.Telegraph)
        {
            float progress = burning.TelegraphProgress;
            Vector2 direction = SafeDirection(burning.ChargeDirection);
            float laneLength = GameBalance.BurningChargeSpeed * GameBalance.BurningChargeDuration;
            DrawBrokenLane(batch, pixel, burning.Position, direction, laneLength,
                0.28f + progress * 0.46f, progress);
        }
        else if (burning.State == BurningState.Charge)
        {
            Vector2 direction = SafeDirection(burning.ChargeDirection);
            DrawChevron(batch, pixel, burning.Position - direction * 38f, direction, 18f,
                Separator * 0.75f, 7f);
            DrawChevron(batch, pixel, burning.Position - direction * 38f, direction, 18f,
                GameBalance.SoulWhite * 0.82f, 2.5f);
        }
    }

    private static void DrawDevourer(SpriteBatch batch, Texture2D pixel, Devourer devourer)
    {
        if (devourer.State == DevourerState.SlamTelegraph)
        {
            float progress = devourer.TelegraphProgress;
            float sweep = MathHelper.TwoPi * MathF.Max(0.04f, progress);
            batch.DrawCircle(pixel, devourer.Position, GameBalance.DevourerSlamRange,
                Separator * 0.72f, 7f, 44);
            batch.DrawArc(pixel, devourer.Position, GameBalance.DevourerSlamRange,
                -MathHelper.PiOver2, sweep, GameBalance.DeathFlameBright * (0.34f + progress * 0.45f), 3f, 44);
        }
        else if (devourer.State == DevourerState.Slam)
        {
            batch.DrawCircle(pixel, devourer.Position, GameBalance.DevourerSlamRange,
                Separator * 0.86f, 10f, 44);
            batch.DrawCircle(pixel, devourer.Position, GameBalance.DevourerSlamRange,
                GameBalance.SoulWhite * 0.88f, 4f, 44);
        }
    }

    private static void DrawWedge(
        SpriteBatch batch,
        Texture2D pixel,
        Vector2 origin,
        float direction,
        float halfSweep,
        float range,
        Color separator,
        Color edge)
    {
        Vector2 left = Unit(direction - halfSweep);
        Vector2 right = Unit(direction + halfSweep);
        batch.DrawArc(pixel, origin, range, direction - halfSweep, halfSweep * 2f, separator, 8f, 24);
        batch.DrawLine(pixel, origin + left * 28f, origin + left * range, separator, 7f);
        batch.DrawLine(pixel, origin + right * 28f, origin + right * range, separator, 7f);
        batch.DrawArc(pixel, origin, range, direction - halfSweep, halfSweep * 2f, edge, 3f, 24);
        batch.DrawLine(pixel, origin + left * 28f, origin + left * range, edge * 0.72f, 2f);
        batch.DrawLine(pixel, origin + right * 28f, origin + right * range, edge, 3f);
    }

    private static void DrawBrokenLane(
        SpriteBatch batch,
        Texture2D pixel,
        Vector2 origin,
        Vector2 direction,
        float length,
        float alpha,
        float progress)
    {
        Vector2 side = new(-direction.Y, direction.X);
        const float halfWidth = 24f;
        for (float distance = 42f; distance < length; distance += 62f)
        {
            float segmentLength = MathF.Min(34f, length - distance);
            Vector2 start = origin + direction * distance;
            Vector2 end = start + direction * segmentLength;
            batch.DrawLine(pixel, start - side * halfWidth, end - side * halfWidth, Separator * 0.65f, 6f);
            batch.DrawLine(pixel, start + side * halfWidth, end + side * halfWidth, Separator * 0.65f, 6f);
            batch.DrawLine(pixel, start - side * halfWidth, end - side * halfWidth,
                GameBalance.DeathFlameBright * alpha, 2f);
            batch.DrawLine(pixel, start + side * halfWidth, end + side * halfWidth,
                GameBalance.DeathFlameBright * alpha, 2f);
        }

        float advancingDistance = MathHelper.Lerp(64f, length - 22f, progress);
        DrawChevron(batch, pixel, origin + direction * advancingDistance, direction, 17f,
            Separator * 0.75f, 7f);
        DrawChevron(batch, pixel, origin + direction * advancingDistance, direction, 17f,
            GameBalance.SoulWhite * alpha, 2.5f);
    }

    private static void DrawChevron(
        SpriteBatch batch,
        Texture2D pixel,
        Vector2 center,
        Vector2 direction,
        float size,
        Color color,
        float thickness)
    {
        Vector2 side = new(-direction.Y, direction.X);
        Vector2 tip = center + direction * size;
        batch.DrawLine(pixel, center - direction * size + side * size, tip, color, thickness);
        batch.DrawLine(pixel, center - direction * size - side * size, tip, color, thickness);
    }

    private static Vector2 Unit(float angle) => new(MathF.Cos(angle), MathF.Sin(angle));

    private static Vector2 SafeDirection(Vector2 direction) =>
        direction.LengthSquared() > 0.0001f ? Vector2.Normalize(direction) : Vector2.UnitX;
}
