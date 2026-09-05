using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheLostSoulOfFire.Rendering;

public static class ShapeRenderer
{
    public static void FillEllipse(this SpriteBatch batch, Texture2D pixel, Vector2 center, float width, float height, Color color)
    {
        for (int y = -(int)height; y <= (int)height; y += 2)
        {
            float extent = width * MathF.Sqrt(MathF.Max(0f, 1f - y * y / (height * height)));
            batch.DrawLine(pixel, center + new Vector2(-extent, y), center + new Vector2(extent, y), color, 2f);
        }
    }

    public static void FillRectangle(this SpriteBatch batch, Texture2D pixel, Rectangle rectangle, Color color) =>
        batch.Draw(pixel, rectangle, color);

    public static void DrawRectangle(this SpriteBatch batch, Texture2D pixel, Rectangle rectangle, Color color, float thickness = 2f)
    {
        batch.DrawLine(pixel, new Vector2(rectangle.Left, rectangle.Top), new Vector2(rectangle.Right, rectangle.Top), color, thickness);
        batch.DrawLine(pixel, new Vector2(rectangle.Right, rectangle.Top), new Vector2(rectangle.Right, rectangle.Bottom), color, thickness);
        batch.DrawLine(pixel, new Vector2(rectangle.Right, rectangle.Bottom), new Vector2(rectangle.Left, rectangle.Bottom), color, thickness);
        batch.DrawLine(pixel, new Vector2(rectangle.Left, rectangle.Bottom), new Vector2(rectangle.Left, rectangle.Top), color, thickness);
    }

    public static void DrawLine(this SpriteBatch batch, Texture2D pixel, Vector2 start, Vector2 end, Color color, float thickness = 2f)
    {
        Vector2 delta = end - start;
        if (delta.LengthSquared() < 0.001f)
        {
            return;
        }

        batch.Draw(
            pixel,
            start,
            null,
            color,
            MathF.Atan2(delta.Y, delta.X),
            new Vector2(0f, 0.5f),
            new Vector2(delta.Length(), thickness),
            SpriteEffects.None,
            0f);
    }

    public static void FillCircle(this SpriteBatch batch, Texture2D pixel, Vector2 center, float radius, Color color)
    {
        int roundedRadius = Math.Max(1, (int)MathF.Ceiling(radius));
        for (int y = -roundedRadius; y <= roundedRadius; y += 2)
        {
            float halfWidth = MathF.Sqrt(MathF.Max(0f, radius * radius - y * y));
            batch.DrawLine(pixel, center + new Vector2(-halfWidth, y), center + new Vector2(halfWidth, y), color, 2f);
        }
    }

    public static void DrawCircle(this SpriteBatch batch, Texture2D pixel, Vector2 center, float radius, Color color, float thickness = 2f, int segments = 32)
    {
        Vector2 previous = center + new Vector2(radius, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = MathHelper.TwoPi * i / segments;
            Vector2 next = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            batch.DrawLine(pixel, previous, next, color, thickness);
            previous = next;
        }
    }

    public static void DrawArc(this SpriteBatch batch, Texture2D pixel, Vector2 center, float radius, float startAngle, float sweep, Color color, float thickness, int segments = 24)
    {
        Vector2 previous = center + new Vector2(MathF.Cos(startAngle), MathF.Sin(startAngle)) * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = startAngle + sweep * i / segments;
            Vector2 next = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            batch.DrawLine(pixel, previous, next, color, thickness);
            previous = next;
        }
    }
}
