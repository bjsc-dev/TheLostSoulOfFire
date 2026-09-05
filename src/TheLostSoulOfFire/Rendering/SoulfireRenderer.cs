using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// Central tuning surface for the restrained scene grade and supernatural light layer.
/// World art remains PointClamp; only the generated, low-frequency light texture is filtered.
/// </summary>
public static class SoulfireRenderSettings
{
    public static readonly Color SceneGrade = new(234, 228, 244);
    public static readonly Color GradeShadow = new(6, 5, 12);

    public const float GradeShadowOpacity = 0.08f;
    public const float VignetteOpacity = 0.28f;
    public const float SoulSenseVignetteBoost = 0.1f;
    public const float SoulSenseWorldVeilOpacity = 0.25f;
    public const float ResonanceVignetteReduction = 0.05f;

    public const int GlowTextureSize = 128;
    public const int VignetteTextureWidth = 256;
    public const int VignetteTextureHeight = 144;
    public const float GlowFalloff = 2.35f;
    public const float SoftEmissionOpacity = 0.22f;
    public const float SoftEmissionDownsampleOpacity = 0.68f;

    public const float PlayerCoreGlowRadius = 46f;
    public const float PlayerCoreGlowIntensity = 0.25f;
    public const float ReadyCoreGlowRadius = 78f;
    public const float ReadyCoreGlowIntensity = 0.42f;
    public const float ResonanceGlowRadius = 128f;
    public const float ResonanceGlowIntensity = 0.34f;
    public const float SoulGlowRadius = 66f;
    public const float SoulGlowIntensity = 0.34f;
    public const float CannonGlowRadius = 54f;
    public const float CannonGlowIntensity = 0.38f;
    public const float DeathFlameGlowRadius = 92f;
    public const float DeathFlameGlowIntensity = 0.48f;
    public const float ParticleGlowRadiusMultiplier = 3.4f;
    public const float ParticleGlowIntensity = 0.17f;
}

/// <summary>
/// A deliberately small render foundation: a crisp scene target, an emissive target that
/// never contains HUD pixels, and procedural glow/vignette textures. High quality adds a
/// low-resolution copy of emission for a restrained soft halo without requiring an .fx file.
/// </summary>
public sealed class SoulfireRenderer : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly PresentationSettings _settings;
    private readonly BlendState _lightBlend;
    private RenderTarget2D _sceneTarget;
    private RenderTarget2D _emissionTarget;
    private RenderTarget2D _softEmissionTarget = null!;
    private Texture2D _solidTexture;
    private Texture2D _glowTexture;
    private Texture2D _vignetteTexture;
    private int _targetWidth;
    private int _targetHeight;

    public SoulfireRenderer(GraphicsDevice graphicsDevice, PresentationSettings settings)
    {
        _graphicsDevice = graphicsDevice;
        _settings = settings;
        _lightBlend = new BlendState
        {
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.One,
            ColorBlendFunction = BlendFunction.Add,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,
            AlphaBlendFunction = BlendFunction.Add
        };
        _solidTexture = new Texture2D(graphicsDevice, 1, 1);
        _solidTexture.SetData([Color.White]);
        _glowTexture = CreateGlowTexture(graphicsDevice);
        _vignetteTexture = CreateVignetteTexture(graphicsDevice);
    }

    public void BeginScene(Viewport viewport)
    {
        EnsureTargets(viewport.Width, viewport.Height);
        _graphicsDevice.SetRenderTarget(_sceneTarget);
        _graphicsDevice.Clear(GameBalance.VoidColor);
    }

    public void PresentScene(SpriteBatch batch, Viewport viewport, float soulSenseWorldSuppression = 0f)
    {
        _graphicsDevice.SetRenderTarget(null);
        _graphicsDevice.Clear(GameBalance.VoidColor);

        Rectangle destination = new(0, 0, viewport.Width, viewport.Height);
        float suppression = MathHelper.Clamp(soulSenseWorldSuppression, 0f, 1f);
        Color sceneGrade = Color.Lerp(
            Color.White,
            Color.Lerp(
                SoulfireRenderSettings.SceneGrade,
                GameBalance.SoulSenseWorldGrade,
                suppression),
            0.94f);
        batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp);
        batch.Draw(_sceneTarget, destination, sceneGrade);
        batch.End();

        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        batch.Draw(
            _solidTexture,
            destination,
            SoulfireRenderSettings.GradeShadow * SoulfireRenderSettings.GradeShadowOpacity);
        if (suppression > 0f)
        {
            batch.Draw(
                _solidTexture,
                destination,
                GameBalance.SoulSenseWorldVeil * (SoulfireRenderSettings.SoulSenseWorldVeilOpacity * suppression));
        }
        batch.End();
    }

    public void BeginEmission(Viewport viewport)
    {
        EnsureTargets(viewport.Width, viewport.Height);
        _graphicsDevice.SetRenderTarget(_emissionTarget);
        _graphicsDevice.Clear(Color.Transparent);
    }

    public void BeginLighting(SpriteBatch batch, Matrix worldTransform) =>
        batch.Begin(
            SpriteSortMode.Deferred,
            _lightBlend,
            SamplerState.LinearClamp,
            transformMatrix: worldTransform);

    public void DrawGlow(SpriteBatch batch, Vector2 position, float radius, Color color, float intensity)
    {
        float diameter = MathF.Max(1f, radius * 2f);
        batch.Draw(
            _glowTexture,
            position,
            null,
            color * MathHelper.Clamp(intensity * _settings.GlowIntensityScale, 0f, 1f),
            0f,
            new Vector2(_glowTexture.Width, _glowTexture.Height) * 0.5f,
            diameter / _glowTexture.Width,
            SpriteEffects.None,
            0f);
    }

    public void CompositeEmission(SpriteBatch batch, Viewport viewport)
    {
        _graphicsDevice.SetRenderTarget(null);
        Rectangle destination = new(0, 0, viewport.Width, viewport.Height);

        if (_settings.UsesSoftEmission)
        {
            EnsureSoftEmissionTarget(viewport.Width, viewport.Height);
            _graphicsDevice.SetRenderTarget(_softEmissionTarget);
            _graphicsDevice.Clear(Color.Transparent);
            batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp);
            batch.Draw(
                _emissionTarget,
                new Rectangle(0, 0, _softEmissionTarget!.Width, _softEmissionTarget.Height),
                Color.White * SoulfireRenderSettings.SoftEmissionDownsampleOpacity);
            batch.End();
            _graphicsDevice.SetRenderTarget(null);

            batch.Begin(SpriteSortMode.Deferred, _lightBlend, SamplerState.LinearClamp);
            batch.Draw(_softEmissionTarget, destination, Color.White * SoulfireRenderSettings.SoftEmissionOpacity);
            batch.End();
        }

        batch.Begin(SpriteSortMode.Deferred, _lightBlend, SamplerState.LinearClamp);
        batch.Draw(_emissionTarget, destination, Color.White);
        batch.End();
    }

    public void DrawVignette(SpriteBatch batch, Viewport viewport, float soulSenseAmount, bool resonanceActive)
    {
        float opacity = SoulfireRenderSettings.VignetteOpacity +
            SoulfireRenderSettings.SoulSenseVignetteBoost * MathHelper.Clamp(soulSenseAmount, 0f, 1f);
        if (resonanceActive)
        {
            opacity -= SoulfireRenderSettings.ResonanceVignetteReduction;
        }
        opacity *= _settings.VignetteScale;

        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
        batch.Draw(
            _vignetteTexture,
            new Rectangle(0, 0, viewport.Width, viewport.Height),
            Color.White * MathHelper.Clamp(opacity, 0f, 1f));
        batch.End();
    }

    public void Dispose()
    {
        _sceneTarget?.Dispose();
        _emissionTarget?.Dispose();
        _softEmissionTarget?.Dispose();
        _solidTexture.Dispose();
        _glowTexture.Dispose();
        _vignetteTexture.Dispose();
        _lightBlend.Dispose();
        GC.SuppressFinalize(this);
    }

    private void EnsureTargets(int width, int height)
    {
        if (_sceneTarget is not null && _emissionTarget is not null && _targetWidth == width && _targetHeight == height)
        {
            return;
        }

        _sceneTarget?.Dispose();
        _emissionTarget?.Dispose();
        _softEmissionTarget?.Dispose();
        _softEmissionTarget = null;
        _targetWidth = width;
        _targetHeight = height;
        _sceneTarget = new RenderTarget2D(
            _graphicsDevice,
            width,
            height,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.DiscardContents);
        _emissionTarget = new RenderTarget2D(
            _graphicsDevice,
            width,
            height,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.DiscardContents);
    }

    private void EnsureSoftEmissionTarget(int width, int height)
    {
        int softWidth = Math.Max(1, width / 2);
        int softHeight = Math.Max(1, height / 2);
        if (_softEmissionTarget is not null && _softEmissionTarget.Width == softWidth && _softEmissionTarget.Height == softHeight)
        {
            return;
        }

        _softEmissionTarget?.Dispose();
        _softEmissionTarget = new RenderTarget2D(
            _graphicsDevice,
            softWidth,
            softHeight,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.DiscardContents);
    }

    private static Texture2D CreateGlowTexture(GraphicsDevice graphicsDevice)
    {
        int size = SoulfireRenderSettings.GlowTextureSize;
        Texture2D texture = new(graphicsDevice, size, size, false, SurfaceFormat.Color);
        Color[] data = new Color[size * size];
        Vector2 center = new((size - 1) * 0.5f);
        float inverseRadius = 1f / (size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) * inverseRadius;
                float glow = MathF.Pow(1f - MathHelper.Clamp(distance, 0f, 1f), SoulfireRenderSettings.GlowFalloff);
                byte value = (byte)MathF.Round(glow * 255f);
                // Premultiplied white lets the custom One/One blend add light without dark fringes.
                data[y * size + x] = new Color(value, value, value, value);
            }
        }

        texture.SetData(data);
        return texture;
    }

    private static Texture2D CreateVignetteTexture(GraphicsDevice graphicsDevice)
    {
        int width = SoulfireRenderSettings.VignetteTextureWidth;
        int height = SoulfireRenderSettings.VignetteTextureHeight;
        Texture2D texture = new(graphicsDevice, width, height, false, SurfaceFormat.Color);
        Color[] data = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            float normalizedY = (y + 0.5f) / height * 2f - 1f;
            for (int x = 0; x < width; x++)
            {
                float normalizedX = (x + 0.5f) / width * 2f - 1f;
                float distance = MathF.Sqrt(normalizedX * normalizedX + normalizedY * normalizedY * 0.82f);
                float edge = SmoothStep(0.48f, 1.24f, distance);
                byte alpha = (byte)MathF.Round(edge * 255f);
                data[y * width + x] = new Color(0, 0, 0, (int)alpha);
            }
        }

        texture.SetData(data);
        return texture;
    }

    private static float SmoothStep(float minimum, float maximum, float value)
    {
        float amount = MathHelper.Clamp((value - minimum) / (maximum - minimum), 0f, 1f);
        return amount * amount * (3f - 2f * amount);
    }
}
