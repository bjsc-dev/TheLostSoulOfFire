using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheLostSoulOfFire.Debugging;

public static class ScreenshotCapture
{
    public static bool TrySaveBackBuffer(GraphicsDevice graphicsDevice, string context, out string result)
    {
        string root = FindRepositoryRoot() ?? Directory.GetCurrentDirectory();
        return TrySaveBackBuffer(
            graphicsDevice,
            context,
            Path.Combine(root, "artifacts", "screenshots"),
            string.Empty,
            out result);
    }

    public static bool TrySaveBackBuffer(
        GraphicsDevice graphicsDevice,
        string context,
        string outputDirectory,
        string outputName,
        out string result)
    {
        try
        {
            string root = FindRepositoryRoot() ?? Directory.GetCurrentDirectory();
            string directory = Path.IsPathRooted(outputDirectory)
                ? outputDirectory
                : Path.GetFullPath(outputDirectory, root);
            Directory.CreateDirectory(directory);

            string safeContext = string.Concat(context
                .ToLowerInvariant()
                .Select(character => char.IsLetterOrDigit(character) ? character : '_'))
                .Trim('_');
            string name = string.IsNullOrWhiteSpace(outputName)
                ? $"{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{safeContext}"
                : string.Concat(outputName.Select(character => char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '_')).Trim('_');
            string path = Path.Combine(directory, $"{name}.png");

            int width = graphicsDevice.PresentationParameters.BackBufferWidth;
            int height = graphicsDevice.PresentationParameters.BackBufferHeight;
            Color[] pixels = new Color[width * height];
            graphicsDevice.GetBackBufferData(pixels);

            using Texture2D screenshot = new(graphicsDevice, width, height);
            screenshot.SetData(pixels);
            using FileStream stream = File.Create(path);
            screenshot.SaveAsPng(stream, width, height);

            result = Path.GetRelativePath(root, path);
            return true;
        }
        catch (Exception exception)
        {
            result = exception.Message;
            return false;
        }
    }

    private static string FindRepositoryRoot()
    {
        return FindRepositoryRoot(Directory.GetCurrentDirectory()) ??
               FindRepositoryRoot(AppContext.BaseDirectory);
    }

    private static string FindRepositoryRoot(string startPath)
    {
        DirectoryInfo directory = new(startPath);
        while (directory is not null)
        {
            string gitMarker = Path.Combine(directory.FullName, ".git");
            if (Directory.Exists(gitMarker) || File.Exists(gitMarker))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
