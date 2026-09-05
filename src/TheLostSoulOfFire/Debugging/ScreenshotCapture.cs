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
    public static bool TrySaveBackBuffer(GraphicsDevice graphicsDevice, string context, out string result)
    {
        string root = FindRepositoryRoot() ?? Directory.GetCurrentDirectory();
        return TrySaveBackBuffer(graphicsDevice, context, Path.Combine(root, "artifacts", "screenshots"), string.Empty, -1, out result);
    }

    public static bool TrySaveBackBuffer(
        GraphicsDevice graphicsDevice,
        string context,
        string outputDirectory,
        string outputName,
        out string result) =>
        TrySaveBackBuffer(graphicsDevice, context, outputDirectory, outputName, -1, out result);

    public static bool TrySaveBackBuffer(
        GraphicsDevice graphicsDevice,
        string context,
        string outputDirectory,
        string outputName,
        int updateTick,
        out string result)
    {
        try
        {
            string root = FindRepositoryRoot() ?? Directory.GetCurrentDirectory();
            ResolveCapturePath(root, outputDirectory, outputName, context, out string directory, out string path);
            Directory.CreateDirectory(directory);

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

    private static void ResolveCapturePath(string root, string output, string outputName, string context, out string directory, out string path)
    {
        output ??= Path.Combine(root, "artifacts", "screenshots");
        string fullOutput = Path.IsPathRooted(output) ? output : Path.GetFullPath(output, root);
        if (string.IsNullOrWhiteSpace(outputName) && fullOutput.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            directory = Path.GetDirectoryName(fullOutput) ?? throw new ArgumentException("Capture output has no parent directory.");
            path = fullOutput;
            return;
        }
        if (Path.HasExtension(fullOutput) && !Directory.Exists(fullOutput))
        {
            throw new ArgumentException("Capture output file must use a .png extension.");
        }

        directory = fullOutput;
        string safeContext = string.Concat(context.ToLowerInvariant().Select(character => char.IsLetterOrDigit(character) ? character : '_')).Trim('_');
        string name = string.IsNullOrWhiteSpace(outputName)
            ? $"{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{safeContext}"
            : string.Concat(outputName.Select(character => char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '_')).Trim('_');
        path = Path.Combine(directory, $"{name}.png");
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

    private static string FindRepositoryRoot() =>
        FindRepositoryRoot(Directory.GetCurrentDirectory()) ?? FindRepositoryRoot(AppContext.BaseDirectory);

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
