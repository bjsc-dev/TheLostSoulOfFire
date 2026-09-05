using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheLostSoulOfFire.Debugging;

public static class ScreenshotCapture
{
    public static bool TrySaveBackBuffer(
        GraphicsDevice graphicsDevice,
        string context,
        int updateTick,
        string explicitOutputPath,
        out string result)
    {
        try
        {
            string root = FindRepositoryRoot() ?? Directory.GetCurrentDirectory();
            string directory = ResolveOutputDirectory(root, explicitOutputPath, out string explicitFileName);
            Directory.CreateDirectory(directory);

            string safeContext = string.Concat(context
                .ToLowerInvariant()
                .Select(character => char.IsLetterOrDigit(character) ? character : '_'))
                .Trim('_');
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
            string path = Path.Combine(directory, explicitFileName ?? $"{timestamp}_{safeContext}.png");
            if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Capture output file must use a .png extension.");
            }

            int width = graphicsDevice.PresentationParameters.BackBufferWidth;
            int height = graphicsDevice.PresentationParameters.BackBufferHeight;
            Color[] pixels = new Color[width * height];
            graphicsDevice.GetBackBufferData(pixels);

            using Texture2D screenshot = new(graphicsDevice, width, height);
            screenshot.SetData(pixels);
            using FileStream stream = File.Create(path);
            screenshot.SaveAsPng(stream, width, height);

            WriteMetadata(path, root, context, updateTick, width, height);

            result = Path.GetRelativePath(root, path);
            return true;
        }
        catch (Exception exception)
        {
            result = exception.Message;
            return false;
        }
    }

    private static string ResolveOutputDirectory(string root, string explicitOutputPath, out string explicitFileName)
    {
        explicitFileName = null;
        if (string.IsNullOrWhiteSpace(explicitOutputPath))
        {
            return Path.Combine(root, "artifacts", "screenshots");
        }

        string fullPath = Path.GetFullPath(explicitOutputPath, Directory.GetCurrentDirectory());
        if (Directory.Exists(fullPath) ||
            explicitOutputPath.EndsWith(Path.DirectorySeparatorChar) ||
            explicitOutputPath.EndsWith(Path.AltDirectorySeparatorChar))
        {
            return fullPath;
        }

        if (fullPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            explicitFileName = Path.GetFileName(fullPath);
            return Path.GetDirectoryName(fullPath) ?? throw new ArgumentException("Capture output has no parent directory.");
        }

        if (Path.HasExtension(fullPath))
        {
            throw new ArgumentException("Capture output file must use a .png extension.");
        }

        return fullPath;
    }

    private static void WriteMetadata(string imagePath, string root, string context, int updateTick, int width, int height)
    {
        string metadataPath = Path.ChangeExtension(imagePath, ".json");
        object metadata = new
        {
            schemaVersion = 1,
            capturedAtUtc = DateTime.UtcNow.ToString("O"),
            context,
            updateTick,
            dimensions = new { width, height },
            image = Path.GetRelativePath(root, imagePath),
            build = new
            {
                assembly = Assembly.GetEntryAssembly()?.GetName().Name,
                assemblyVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(),
                framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                operatingSystem = System.Runtime.InteropServices.RuntimeInformation.OSDescription
            }
        };
        File.WriteAllText(metadataPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));
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
            string marker = Path.Combine(directory.FullName, ".git");
            if (Directory.Exists(marker) || File.Exists(marker))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
