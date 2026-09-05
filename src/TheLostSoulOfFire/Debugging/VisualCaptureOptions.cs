using System;
using System.Globalization;

namespace TheLostSoulOfFire.Debugging;

/// <summary>
/// Small, intentionally non-interactive capture contract. It is a capture
/// primitive, not a scenario runner: later fixtures can supply input and use
/// the same request/output path without changing normal startup.
/// </summary>
public sealed class VisualCaptureOptions
{
    public int CaptureAfterTicks { get; private init; }
    public string OutputPath { get; private init; }
    public bool ExitAfterCapture { get; private init; }
    public int StartAtTick { get; private init; }

    public bool IsEnabled => CaptureAfterTicks > 0;

    public static bool TryParse(string[] args, out VisualCaptureOptions options, out string error)
    {
        int? captureAfterTicks = null;
        int startAtTick = 0;
        string outputPath = null;
        bool exitAfterCapture = false;

        for (int index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--capture-after-ticks":
                    if (!TryReadPositiveInt(args, ref index, "--capture-after-ticks", out int captureTicks, out error))
                    {
                        options = null;
                        return false;
                    }
                    captureAfterTicks = captureTicks;
                    break;

                case "--capture-output":
                    if (index + 1 >= args.Length || string.IsNullOrWhiteSpace(args[++index]))
                    {
                        options = null;
                        error = "--capture-output requires a directory or .png file path.";
                        return false;
                    }
                    outputPath = args[index];
                    break;

                case "--exit-after-capture":
                    exitAfterCapture = true;
                    break;

                case "--capture-start-at-tick":
                    if (!TryReadPositiveInt(args, ref index, "--capture-start-at-tick", out startAtTick, out error))
                    {
                        options = null;
                        return false;
                    }
                    break;
            }
        }

        if (!captureAfterTicks.HasValue)
        {
            if (exitAfterCapture || outputPath is not null || startAtTick > 0)
            {
                options = null;
                error = "--capture-output, --exit-after-capture, and --capture-start-at-tick require --capture-after-ticks N.";
                return false;
            }

            options = null;
            error = string.Empty;
            return true;
        }

        options = new VisualCaptureOptions
        {
            CaptureAfterTicks = captureAfterTicks.Value,
            OutputPath = outputPath,
            ExitAfterCapture = exitAfterCapture,
            StartAtTick = startAtTick
        };
        error = string.Empty;
        return true;
    }

    public static string Usage =>
        "Visual capture: --capture-after-ticks N [--capture-output <directory-or-file.png>] " +
        "[--capture-start-at-tick N] [--exit-after-capture]";

    private static bool TryReadPositiveInt(string[] args, ref int index, string option, out int value, out string error)
    {
        value = 0;
        if (index + 1 >= args.Length ||
            !int.TryParse(args[++index], NumberStyles.None, CultureInfo.InvariantCulture, out value) ||
            value <= 0)
        {
            error = $"{option} requires a positive integer.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
