using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// Owns the short, authored beats around the arena loop. This is deliberately
/// specific to Soulfire: it is presentation timing and composition, not a
/// general cutscene or timeline system.
/// </summary>
public sealed class CinematicPresentation
{
    public const float FullIntroDuration = 1.55f;
    public const float RetryIntroDuration = 0.52f;
    public const float WaveTransitionDuration = 1.05f;
    public const float LifeFlameRevealTime = 1.05f;

    private float _titleTime;
    private float _stateTime;
    private float _introDuration = FullIntroDuration;
    private bool _quickIntro;

    public float StateTime => _stateTime;
    public bool TransitionComplete => _stateTime >= _introDuration;
    public bool WaveTransitionComplete => _stateTime >= WaveTransitionDuration;

    public void Update(float deltaTime, ArenaLoopState loopState)
    {
        if (loopState == ArenaLoopState.Title)
        {
            _titleTime += deltaTime;
        }
        else
        {
            _stateTime += deltaTime;
        }
    }

    public void BeginIntro(bool quick)
    {
        _stateTime = 0f;
        _quickIntro = quick;
        _introDuration = quick ? RetryIntroDuration : FullIntroDuration;
    }

    public void BeginWaveTransition()
    {
        _stateTime = 0f;
        _quickIntro = false;
    }

    public void BeginDeath() => _stateTime = 0f;

    public void BeginCompletion() => _stateTime = 0f;

    public bool ShouldDrawPlayer(ArenaLoopState loopState, bool playerDead) =>
        playerDead ||
        loopState is ArenaLoopState.Combat or ArenaLoopState.Transition or ArenaLoopState.Complete ||
        loopState == ArenaLoopState.Intro && (_quickIntro || _stateTime >= 0.52f);

    public bool ShouldDrawCombatHud(ArenaLoopState loopState, bool playerDead) =>
        !playerDead && loopState is ArenaLoopState.Combat or ArenaLoopState.Transition;

    public bool ShouldDrawAim(ArenaLoopState loopState, bool playerDead) =>
        !playerDead && loopState is ArenaLoopState.Combat or ArenaLoopState.Transition;

    public void UpdateCamera(
        Camera2D camera,
        ArenaLoopState loopState,
        bool playerDead,
        Vector2 playerPosition,
        Rectangle worldBounds,
        Rectangle combatBounds,
        Viewport viewport,
        float deltaTime)
    {
        Vector2 arenaCenter = combatBounds.Center.ToVector2();
        Vector2 target = playerPosition;
        float targetZoom = 1f;
        float followSpeed = 9f;

        if (loopState == ArenaLoopState.Title)
        {
            target = arenaCenter + new Vector2(0f, -36f);
            targetZoom = 0.9f;
            followSpeed = 2.4f;
        }
        else if (playerDead)
        {
            target = playerPosition;
            targetZoom = 1.055f;
            followSpeed = 3.2f;
        }
        else if (loopState == ArenaLoopState.Intro)
        {
            float settle = Ease(_stateTime / MathF.Max(0.01f, _introDuration));
            target = Vector2.Lerp(arenaCenter + new Vector2(0f, -46f), playerPosition, settle);
            targetZoom = MathHelper.Lerp(_quickIntro ? 0.97f : 0.9f, 1f, settle);
            followSpeed = _quickIntro ? 10f : 4.5f;
        }
        else if (loopState == ArenaLoopState.Transition)
        {
            target = Vector2.Lerp(playerPosition, arenaCenter, 0.12f);
            targetZoom = 0.975f;
            followSpeed = 5f;
        }
        else if (loopState == ArenaLoopState.Complete)
        {
            target = Vector2.Lerp(playerPosition, GetLifeFlamePosition(combatBounds), 0.2f) + new Vector2(70f, -72f);
            targetZoom = 0.92f;
            followSpeed = 2.8f;
        }

        float zoomSmoothing = 1f - MathF.Exp(-deltaTime * followSpeed);
        camera.Zoom = MathHelper.Lerp(camera.Zoom, targetZoom, zoomSmoothing);
        camera.Follow(target, worldBounds, viewport, zoomSmoothing);
    }

    public void DrawWorldAccents(
        SpriteBatch batch,
        Texture2D pixel,
        ArtAssets art,
        ArenaLoopState loopState,
        bool playerDead,
        Player player,
        Rectangle combatBounds)
    {
        Vector2 center = combatBounds.Center.ToVector2();

        if (loopState == ArenaLoopState.Title)
        {
            float reveal = Ease((_titleTime - 0.15f) / 1.1f);
            float breathe = 0.94f + MathF.Sin(_titleTime * 2.1f) * 0.04f;
            art.DrawLoopingEffect(
                batch,
                this,
                "death_flame_loop",
                center + new Vector2(0f, -92f),
                0f,
                0.54f * breathe,
                Color.White * (0.72f * reveal));
            batch.DrawCircle(pixel, center + new Vector2(0f, -92f), 47f + breathe * 4f, GameBalance.DeathFlame * (0.12f * reveal), 2f, 28);
            return;
        }

        if (playerDead)
        {
            float collapse = Ease(_stateTime / 1.2f);
            float scale = MathHelper.Lerp(0.68f, 0.34f, collapse);
            float alpha = MathHelper.Lerp(1f, 0.34f, collapse);
            art.DrawLoopingEffect(batch, player, "death_flame_loop", player.Position, 0f, scale, Color.White * alpha);
            batch.DrawCircle(
                pixel,
                player.Position,
                MathHelper.Lerp(52f, 17f, collapse),
                GameBalance.DeathFlameBright * (0.42f * (1f - collapse)),
                3f,
                26);
            return;
        }

        if (loopState == ArenaLoopState.Intro && !_quickIntro && _stateTime is >= 0.48f and <= 1.2f)
        {
            float reveal = 1f - MathF.Abs(_stateTime - 0.82f) / 0.38f;
            reveal = MathHelper.Clamp(reveal, 0f, 1f);
            art.DrawLoopingEffect(batch, this, "death_flame_loop", player.Position, 0f, 0.46f, Color.White * (0.58f * reveal));
            batch.DrawCircle(pixel, player.Position, 24f + reveal * 31f, GameBalance.DeathFlameBright * (0.3f * reveal), 3f, 28);
        }

        if (loopState == ArenaLoopState.Complete)
        {
            DrawLifeFlame(batch, pixel, art, combatBounds);
        }
    }

    public void DrawOverlay(
        SpriteBatch batch,
        Texture2D pixel,
        Viewport viewport,
        ArenaLoopState loopState,
        bool playerDead,
        int waveNumber)
    {
        if (loopState == ArenaLoopState.Title)
        {
            DrawTitle(batch, pixel, viewport);
        }
        else if (playerDead)
        {
            DrawDeath(batch, pixel, viewport);
        }
        else if (loopState == ArenaLoopState.Intro)
        {
            DrawIntro(batch, pixel, viewport);
        }
        else if (loopState == ArenaLoopState.Transition)
        {
            DrawWaveTransition(batch, pixel, viewport, waveNumber + 1);
        }
        else if (loopState == ArenaLoopState.Complete)
        {
            DrawCompletion(batch, pixel, viewport);
        }
    }

    public Vector2 GetLifeFlamePosition(Rectangle combatBounds)
    {
        Vector2 origin = new(combatBounds.Right - 246f, combatBounds.Top + 178f);
        float rise = Ease((_stateTime - 1.15f) / 4.6f) * 38f;
        return origin - Vector2.UnitY * rise;
    }

    public float GetLifeFlameAlpha() => Ease((_stateTime - 1.05f) / 1.15f);

    private void DrawTitle(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        float reveal = Ease((_titleTime - 0.1f) / 1.05f);
        float darkness = MathHelper.Lerp(0.96f, 0.68f, Ease(_titleTime / 1.25f));
        batch.FillRectangle(pixel, viewport.Bounds, Color.Black * darkness);
        DrawLetterbox(batch, pixel, viewport, 44, 0.94f);

        float centerX = viewport.Width * 0.5f;
        float titleY = viewport.Height * 0.37f;
        DrawTitleRules(batch, pixel, viewport, titleY - 42f, reveal);
        PixelText.DrawCentered(batch, pixel, "THE LOST", centerX, titleY - 24f, 2, new Color(178, 168, 190) * (0.82f * reveal));
        PixelText.DrawCentered(batch, pixel, "SOUL OF FIRE", centerX, titleY + 9f, 6, GameBalance.SoulWhite * reveal);
        PixelText.DrawCentered(batch, pixel, "DEATH IS NOT THE END", centerX, titleY + 82f, 2, GameBalance.DeathFlameBright * (0.56f * reveal));

        float promptReveal = Ease((_titleTime - 1.15f) / 0.75f);
        float promptBreathe = 0.58f + MathF.Sin(_titleTime * 2.4f) * 0.12f;
        PixelText.DrawCentered(
            batch,
            pixel,
            "PRESS ANY KEY OR CLICK",
            centerX,
            viewport.Height * 0.72f,
            2,
            GameBalance.SoulWhite * (promptReveal * promptBreathe));
        DrawPromptMark(batch, pixel, new Vector2(centerX, viewport.Height * 0.72f - 20f), promptReveal * promptBreathe);
    }

    private void DrawIntro(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        if (_quickIntro)
        {
            float darkness = 1f - Ease(_stateTime / RetryIntroDuration);
            batch.FillRectangle(pixel, viewport.Bounds, Color.Black * darkness);
            return;
        }

        float worldReveal = Ease((_stateTime - 0.12f) / 0.86f);
        batch.FillRectangle(pixel, viewport.Bounds, Color.Black * (1f - worldReveal));
        DrawLetterbox(batch, pixel, viewport, (int)MathHelper.Lerp(52f, 18f, worldReveal), 0.88f * (1f - worldReveal * 0.55f));

        float placeIn = Ease((_stateTime - 0.3f) / 0.34f);
        float placeOut = 1f - Ease((_stateTime - 1.12f) / 0.32f);
        float placeAlpha = placeIn * placeOut;
        PixelText.DrawCentered(batch, pixel, "ABANDONED SOUL FURNACE", viewport.Width * 0.5f, viewport.Height * 0.18f, 2, GameBalance.SoulWhite * (0.68f * placeAlpha));

        float warning = Ease((_stateTime - 0.88f) / 0.26f) * (1f - Ease((_stateTime - 1.38f) / 0.14f));
        DrawTitleRules(batch, pixel, viewport, viewport.Height * 0.78f - 16f, warning * 0.58f);
        PixelText.DrawCentered(batch, pixel, "THE FURNACE WAKES", viewport.Width * 0.5f, viewport.Height * 0.78f, 2, GameBalance.DeathFlameBright * (0.76f * warning));
    }

    private void DrawWaveTransition(SpriteBatch batch, Texture2D pixel, Viewport viewport, int nextWave)
    {
        float open = Ease(_stateTime / 0.2f);
        float close = 1f - Ease((_stateTime - 0.82f) / 0.23f);
        float alpha = open * close;
        batch.FillRectangle(pixel, viewport.Bounds, Color.Black * (0.18f * alpha));
        DrawLetterbox(batch, pixel, viewport, 13, 0.54f * alpha);

        string label = nextWave >= 4 ? "FINAL WAVE" : $"WAVE {ToRoman(nextWave)}";
        DrawTitleRules(batch, pixel, viewport, viewport.Height * 0.5f - 28f, alpha * 0.62f);
        PixelText.DrawCentered(batch, pixel, label, viewport.Width * 0.5f, viewport.Height * 0.5f - 9f, 3, GameBalance.SoulWhite * (0.88f * alpha));
    }

    private void DrawDeath(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        float collapse = Ease(_stateTime / 1.25f);
        float darkness = MathHelper.Lerp(0.12f, 0.84f, collapse);
        batch.FillRectangle(pixel, viewport.Bounds, Color.Black * darkness);
        DrawLetterbox(batch, pixel, viewport, (int)MathHelper.Lerp(18f, 64f, collapse), 0.9f * collapse);

        float titleReveal = Ease((_stateTime - 0.38f) / 0.58f);
        float centerX = viewport.Width * 0.5f;
        float titleY = viewport.Height * 0.42f;
        DrawTitleRules(batch, pixel, viewport, titleY - 29f, titleReveal * 0.72f);
        PixelText.DrawCentered(batch, pixel, "THE FLAME", centerX, titleY - 11f, 2, new Color(172, 158, 186) * (0.72f * titleReveal));
        PixelText.DrawCentered(batch, pixel, "IS EXTINGUISHED", centerX, titleY + 23f, 4, GameBalance.DeathFlameBright * titleReveal);

        float promptReveal = Ease((_stateTime - 1.0f) / 0.45f);
        float promptBreathe = 0.58f + MathF.Sin(_stateTime * 2.2f) * 0.1f;
        PixelText.DrawCentered(batch, pixel, "R TO RETRY", centerX, viewport.Height * 0.66f, 2, GameBalance.SoulWhite * (promptReveal * promptBreathe));
    }

    private void DrawCompletion(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        float calm = Ease(_stateTime / 1.4f);
        batch.FillRectangle(pixel, viewport.Bounds, Color.Black * (0.26f * calm));
        DrawLetterbox(batch, pixel, viewport, (int)MathHelper.Lerp(10f, 34f, calm), 0.72f * calm);

        float stillIn = Ease((_stateTime - 0.48f) / 0.7f);
        float stillOut = 1f - Ease((_stateTime - 2.15f) / 0.7f);
        float stillAlpha = stillIn * stillOut;
        PixelText.DrawCentered(batch, pixel, "THE ARENA IS STILL", viewport.Width * 0.5f, viewport.Height * 0.48f, 3, GameBalance.SoulWhite * (0.72f * stillAlpha));

        float endingReveal = Ease((_stateTime - 2.8f) / 1.05f);
        DrawTitleRules(batch, pixel, viewport, viewport.Height * 0.39f - 34f, endingReveal * 0.66f);
        PixelText.DrawCentered(batch, pixel, "THE LOST SOUL OF FIRE", viewport.Width * 0.5f, viewport.Height * 0.39f, 5, GameBalance.SoulWhite * endingReveal);
        Color lifeFlame = new(255, 178, 82);
        PixelText.DrawCentered(batch, pixel, "A FLAME REMAINS", viewport.Width * 0.5f, viewport.Height * 0.46f, 2, lifeFlame * (0.76f * Ease((_stateTime - 3.8f) / 0.7f)));

        float promptReveal = Ease((_stateTime - 5.2f) / 0.65f);
        float promptBreathe = 0.56f + MathF.Sin(_stateTime * 2f) * 0.1f;
        PixelText.DrawCentered(batch, pixel, "R TO RESTART", viewport.Width * 0.5f, viewport.Height * 0.82f, 2, GameBalance.SoulWhite * (promptReveal * promptBreathe));
    }

    private void DrawLifeFlame(SpriteBatch batch, Texture2D pixel, ArtAssets art, Rectangle combatBounds)
    {
        float alpha = GetLifeFlameAlpha();
        if (alpha <= 0f)
        {
            return;
        }

        Vector2 position = GetLifeFlamePosition(combatBounds);
        float breathe = 0.96f + MathF.Sin(_stateTime * 1.7f) * 0.035f;
        art.DrawLifeFlame(batch, position, alpha, 0.88f * breathe);
        batch.DrawLine(pixel, position + new Vector2(-22f, 50f), position + new Vector2(22f, 50f), new Color(255, 192, 116) * (0.22f * alpha), 2f);
    }

    private static void DrawLetterbox(SpriteBatch batch, Texture2D pixel, Viewport viewport, int height, float alpha)
    {
        if (height <= 0 || alpha <= 0f)
        {
            return;
        }

        batch.FillRectangle(pixel, new Rectangle(0, 0, viewport.Width, height), Color.Black * alpha);
        batch.FillRectangle(pixel, new Rectangle(0, viewport.Height - height, viewport.Width, height), Color.Black * alpha);
    }

    private static void DrawTitleRules(SpriteBatch batch, Texture2D pixel, Viewport viewport, float y, float alpha)
    {
        if (alpha <= 0f)
        {
            return;
        }

        float centerX = viewport.Width * 0.5f;
        Color rule = GameBalance.DeathFlame * (0.42f * alpha);
        batch.DrawLine(pixel, new Vector2(centerX - 244f, y), new Vector2(centerX - 72f, y), rule, 2f);
        batch.DrawLine(pixel, new Vector2(centerX + 72f, y), new Vector2(centerX + 244f, y), rule, 2f);
        batch.DrawLine(pixel, new Vector2(centerX - 6f, y), new Vector2(centerX, y - 6f), GameBalance.DeathFlameBright * (0.62f * alpha), 2f);
        batch.DrawLine(pixel, new Vector2(centerX, y - 6f), new Vector2(centerX + 6f, y), GameBalance.DeathFlameBright * (0.62f * alpha), 2f);
    }

    private static void DrawPromptMark(SpriteBatch batch, Texture2D pixel, Vector2 center, float alpha)
    {
        Color color = GameBalance.DeathFlameBright * (0.48f * alpha);
        batch.DrawLine(pixel, center + new Vector2(-5f, 0f), center + new Vector2(0f, -5f), color, 2f);
        batch.DrawLine(pixel, center + new Vector2(0f, -5f), center + new Vector2(5f, 0f), color, 2f);
        batch.DrawLine(pixel, center + new Vector2(5f, 0f), center + new Vector2(0f, 5f), color, 2f);
        batch.DrawLine(pixel, center + new Vector2(0f, 5f), center + new Vector2(-5f, 0f), color, 2f);
    }

    private static float Ease(float amount)
    {
        float value = MathHelper.Clamp(amount, 0f, 1f);
        return value * value * (3f - 2f * value);
    }

    private static string ToRoman(int number) => number switch
    {
        1 => "I",
        2 => "II",
        3 => "III",
        4 => "IV",
        _ => number.ToString()
    };
}
