using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Effects;

public static class SoulfireLighting
{
    public static void Draw(
        SpriteBatch batch,
        SoulfireRenderer renderer,
        Matrix worldTransform,
        Player player,
        IReadOnlyList<Enemy> enemies,
        IReadOnlyList<Soul> souls,
        IReadOnlyList<CannonShot> cannonShots,
        ParticleSystem particles,
        SpriteVfxSystem spriteVfx,
        ArenaAtmosphere arenaAtmosphere,
        float presentationTime,
        float soulSenseAmount,
        bool endingComplete,
        Vector2 lifeFlamePosition,
        float lifeFlameAlpha)
    {
        renderer.BeginLighting(batch, worldTransform);
        float breathe = 0.88f + MathF.Sin(presentationTime * 4.6f) * 0.12f;

        arenaAtmosphere.DrawLighting(batch, renderer, soulSenseAmount);
        particles.DrawLighting(batch, renderer);
        spriteVfx.DrawLighting(batch, renderer);
        DrawSouls(batch, renderer, souls, soulSenseAmount, breathe);
        DrawEnemyEnergy(batch, renderer, enemies, soulSenseAmount, breathe);
        DrawCannonEnergy(batch, renderer, player, cannonShots);
        DrawPlayerEnergy(batch, renderer, player, presentationTime, soulSenseAmount, breathe);
        DrawEndingLight(batch, renderer, endingComplete, lifeFlamePosition, lifeFlameAlpha, breathe);

        batch.End();
    }

    private static void DrawSouls(
        SpriteBatch batch,
        SoulfireRenderer renderer,
        IReadOnlyList<Soul> souls,
        float soulSenseAmount,
        float breathe)
    {
        foreach (Soul soul in souls)
        {
            if (soul.State is SoulState.Released or SoulState.Consumed)
            {
                continue;
            }

            float radius = soul.State == SoulState.Residue
                ? SoulfireRenderSettings.SoulGlowRadius * 0.48f
                : SoulfireRenderSettings.SoulGlowRadius * breathe;
            float intensity = SoulfireRenderSettings.SoulGlowIntensity * MathHelper.Lerp(1f, 1.42f, soulSenseAmount);
            renderer.DrawGlow(batch, soul.Position, radius, GameBalance.SoulWhite, intensity);
            renderer.DrawGlow(batch, soul.Position, radius * 0.48f, GameBalance.DeathFlameBright, intensity * 0.72f);
        }
    }

    private static void DrawEnemyEnergy(
        SpriteBatch batch,
        SoulfireRenderer renderer,
        IReadOnlyList<Enemy> enemies,
        float soulSenseAmount,
        float breathe)
    {
        foreach (Enemy enemy in enemies)
        {
            if (enemy is Burning burning && burning.State != BurningState.Dead)
            {
                float fractureIntensity = MathHelper.Lerp(0.11f, 0.27f, soulSenseAmount);
                foreach (Vector2 fracture in burning.GetFracturePositions())
                {
                    renderer.DrawGlow(batch, fracture, 30f * breathe, GameBalance.DeathFlame, fractureIntensity);
                }

                if (burning.State is BurningState.Charge or BurningState.Detonating)
                {
                    renderer.DrawGlow(batch, burning.Position, 78f, GameBalance.DeathFlameBright, 0.28f);
                }
            }

            if (soulSenseAmount <= 0.001f)
            {
                continue;
            }

            switch (enemy)
            {
                case Hollow hollow when hollow.State is not (HollowState.Dying or HollowState.Dead):
                    renderer.DrawGlow(batch, hollow.CorePosition, 50f * breathe, GameBalance.SoulWhite, 0.38f * soulSenseAmount);
                    break;
                case Devourer devourer when devourer.State != DevourerState.Dead:
                    float torsoIntensity = 0.22f + devourer.ConsumedSoulCount * 0.07f;
                    renderer.DrawGlow(batch, devourer.TorsoPosition, 66f, GameBalance.DeathFlameBright, torsoIntensity * soulSenseAmount);
                    break;
            }
        }
    }

    private static void DrawCannonEnergy(
        SpriteBatch batch,
        SoulfireRenderer renderer,
        Player player,
        IReadOnlyList<CannonShot> cannonShots)
    {
        foreach (CannonShot shot in cannonShots)
        {
            if (shot.IsFinished)
            {
                continue;
            }

            float radius = MathHelper.Lerp(38f, 72f, shot.Charge);
            Color color = shot.IsFullCharge ? GameBalance.SoulWhite : GameBalance.DeathFlameBright;
            renderer.DrawGlow(batch, shot.Position, radius, color, 0.25f + shot.Charge * 0.24f);
        }

        if (player.Cannon.State != SoulCannonState.Charging)
        {
            return;
        }

        float charge = player.Cannon.ChargeProgress;
        Vector2 muzzle = player.Position + player.FacingDirection * 74f;
        float chargeRadius = SoulfireRenderSettings.CannonGlowRadius * MathHelper.Lerp(0.68f, 1.55f, charge);
        Color chargeColor = player.Cannon.IsFullCharge ? GameBalance.SoulWhite : GameBalance.DeathFlameBright;
        float intensity = SoulfireRenderSettings.CannonGlowIntensity * MathHelper.Lerp(0.55f, 1.35f, charge);
        renderer.DrawGlow(batch, muzzle, chargeRadius, chargeColor, intensity);
        if (player.Cannon.IsFullCharge)
        {
            renderer.DrawGlow(batch, muzzle, chargeRadius * 0.48f, GameBalance.SoulWhite, 0.58f);
        }
    }

    private static void DrawPlayerEnergy(
        SpriteBatch batch,
        SoulfireRenderer renderer,
        Player player,
        float presentationTime,
        float soulSenseAmount,
        float breathe)
    {
        Vector2 playerCore = player.Position + player.FacingDirection * 2f;
        if (player.IsDead)
        {
            renderer.DrawGlow(
                batch,
                player.Position,
                SoulfireRenderSettings.DeathFlameGlowRadius * breathe,
                GameBalance.DeathFlame,
                SoulfireRenderSettings.DeathFlameGlowIntensity);
            renderer.DrawGlow(batch, player.Position, 38f, GameBalance.SoulWhite, 0.26f);
            return;
        }

        renderer.DrawGlow(
            batch,
            playerCore,
            SoulfireRenderSettings.PlayerCoreGlowRadius * breathe,
            GameBalance.DeathFlameBright,
            SoulfireRenderSettings.PlayerCoreGlowIntensity);

        if (soulSenseAmount > 0.001f)
        {
            Vector2 eye = player.Position + player.FacingDirection * 26f;
            renderer.DrawGlow(batch, eye, 34f, GameBalance.SoulWhite, 0.25f * soulSenseAmount);
        }

        if (player.IsResonanceReady)
        {
            renderer.DrawGlow(
                batch,
                playerCore,
                SoulfireRenderSettings.ReadyCoreGlowRadius * breathe,
                GameBalance.SoulWhite,
                SoulfireRenderSettings.ReadyCoreGlowIntensity);
        }

        if (player.ResonanceActive)
        {
            float resonancePulse = 0.88f + MathF.Sin(presentationTime * 7.2f) * 0.12f;
            renderer.DrawGlow(
                batch,
                playerCore,
                SoulfireRenderSettings.ResonanceGlowRadius * resonancePulse,
                GameBalance.DeathFlame,
                SoulfireRenderSettings.ResonanceGlowIntensity);
            renderer.DrawGlow(batch, playerCore, 58f * resonancePulse, GameBalance.SoulWhite, 0.46f);
        }
    }

    private static void DrawEndingLight(
        SpriteBatch batch,
        SoulfireRenderer renderer,
        bool endingComplete,
        Vector2 lifeFlamePosition,
        float lifeFlameAlpha,
        float breathe)
    {
        if (!endingComplete || lifeFlameAlpha <= 0f)
        {
            return;
        }

        renderer.DrawGlow(batch, lifeFlamePosition, 86f * breathe, new Color(255, 154, 72), 0.3f * lifeFlameAlpha);
    }
}
