using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// Builds a deterministic, low-contrast material pass for the painted arena floor.
/// The overlay is kept separate from the source asset so it can be disposed normally.
/// </summary>
public static class ArenaFloorSurface
{
    public static Texture2D Create(Texture2D arena)
    {
        int width = arena.Width;
        int height = arena.Height;
        Texture2D surface = new(arena.GraphicsDevice, width, height, false, SurfaceFormat.Color);
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                uint hash = Hash(x / 3, y / 3);
                float normalizedX = (x + 0.5f) / width * 2f - 1f;
                float normalizedY = (y + 0.5f) / height * 2f - 1f;
                float centerDistance = normalizedX * normalizedX + normalizedY * normalizedY;

                // Broad, irregular stone courses are most visible near the perimeter.
                bool horizontalJoint = y % 118 is >= 0 and < 2;
                int staggeredX = x + ((y / 118 & 1) == 0 ? 0 : 91);
                bool verticalJoint = staggeredX % 182 is >= 0 and < 2;
                if ((horizontalJoint || verticalJoint) && centerDistance > 0.16f)
                {
                    pixels[y * width + x] = new Color(0, 0, 0, 18);
                    continue;
                }

                // Sparse clustered flecks keep the center calm and avoid single-pixel noise.
                if ((hash & 0x3ffu) < (centerDistance > 0.28f ? 7u : 2u))
                {
                    byte alpha = (byte)(8 + ((hash >> 12) & 7u));
                    pixels[y * width + x] = (hash & 0x800u) == 0
                        ? new Color(0, 0, 0, (int)alpha)
                        : new Color(alpha, alpha, alpha, alpha);
                }
            }
        }

        surface.SetData(pixels);
        return surface;
    }

    private static uint Hash(int x, int y)
    {
        uint value = (uint)x * 0x8da6b343u ^ (uint)y * 0xd8163841u;
        value ^= value >> 13;
        value *= 0xcb1ab31fu;
        return value ^ value >> 16;
    }
}
