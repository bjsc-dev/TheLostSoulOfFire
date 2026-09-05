using System;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Debugging;

/// <summary>
/// Small command-line surface for repeatable renderer review. It intentionally stays in
/// the executable instead of introducing a second rendering host or test framework.
/// </summary>
public sealed class VisualRunOptions
{
    public int CaptureAfterTicks { get; private set; } = -1;
    public string CaptureOutput { get; private set; } = string.Empty;
    public string VisualScenario { get; private set; } = string.Empty;
    public bool ExitAfterCapture { get; private set; }
    public bool HasQuality { get; private set; }
    public VisualQuality Quality { get; private set; } = VisualQuality.High;
    public bool ReducedEffects { get; private set; }

    public bool HasCaptureRequest => CaptureAfterTicks >= 0 || !string.IsNullOrEmpty(VisualScenario);

    public static bool TryParse(string[] args, out VisualRunOptions options, out string error)
    {
        options = new VisualRunOptions();
        error = string.Empty;

        for (int index = 0; index < args.Length; index++)
        {
            string argument = args[index];
            switch (argument)
            {
                case "--capture-after-ticks":
                    if (!TryTakePositiveInt(args, ref index, argument, out int ticks, out error))
                    {
                        return false;
                    }
                    options.CaptureAfterTicks = ticks;
                    break;

                case "--capture-output":
                    if (!TryTakeValue(args, ref index, argument, out string output, out error))
                    {
                        return false;
                    }
                    options.CaptureOutput = output;
                    break;

                case "--visual-scenario":
                    if (!TryTakeValue(args, ref index, argument, out string scenario, out error))
                    {
                        return false;
                    }
                    if (!VisualScenarioRunner.IsKnownScenario(scenario))
                    {
                        error = $"Unknown visual scenario '{scenario}'. Use one of: {VisualScenarioRunner.KnownScenarioList}.";
                        return false;
                    }
                    options.VisualScenario = scenario;
                    break;

                case "--visual-quality":
                    if (!TryTakeValue(args, ref index, argument, out string qualityValue, out error))
                    {
                        return false;
                    }
                    if (!PresentationSettings.TryParseQuality(qualityValue, out VisualQuality quality))
                    {
                        error = $"Invalid --visual-quality '{qualityValue}'. Expected baseline or high.";
                        return false;
                    }
                    options.Quality = quality;
                    options.HasQuality = true;
                    break;

                case "--reduced-effects":
                    options.ReducedEffects = true;
                    break;

                case "--exit-after-capture":
                    options.ExitAfterCapture = true;
                    break;
            }
        }

        if (options.HasCaptureRequest && string.IsNullOrEmpty(options.CaptureOutput))
        {
            options.CaptureOutput = "artifacts/visual-max/local-run";
        }

        return true;
    }

    private static bool TryTakePositiveInt(string[] args, ref int index, string argument, out int value, out string error)
    {
        value = 0;
        if (!TryTakeValue(args, ref index, argument, out string text, out error))
        {
            return false;
        }
        if (!int.TryParse(text, out value) || value < 0)
        {
            error = $"{argument} requires a non-negative integer.";
            return false;
        }
        return true;
    }

    private static bool TryTakeValue(string[] args, ref int index, string argument, out string value, out string error)
    {
        if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            value = string.Empty;
            error = $"{argument} requires a value.";
            return false;
        }

        value = args[++index];
        error = string.Empty;
        return true;
    }
}
