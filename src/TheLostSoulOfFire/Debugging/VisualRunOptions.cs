using System;
using System.Linq;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Debugging;

/// <summary>
/// Small command-line surface for repeatable renderer review. It intentionally stays in
/// the executable instead of introducing a second rendering host or test framework.
/// </summary>
public sealed class VisualRunOptions
{
    public int CaptureAfterTicks { get; private set; } = -1;
    public int CaptureStartAtTick { get; private set; } = -1;
    public string CaptureOutput { get; private set; } = string.Empty;
    public string VisualScenario { get; private set; } = string.Empty;
    public bool ExitAfterCapture { get; private set; }
    public bool HasQuality { get; private set; }
    public VisualQuality Quality { get; private set; } = VisualQuality.High;
    public bool ReducedEffects { get; private set; }
    public int[] CaptureTicks { get; private set; } = [];
    public bool ForceSoulSense { get; private set; }
    public bool ForceResonance { get; private set; }

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
                case "--capture-ticks":
                    if (!TryTakeValue(args, ref index, argument, out string tickList, out error)) return false;
                    string[] entries = tickList.Split(',');
                    if (entries.Length > 120 || entries.Any(entry => !int.TryParse(entry, out int tick) || tick < 1 || tick > 7200))
                    {
                        error = "--capture-ticks requires 1–120 comma-separated ticks between 1 and 7200.";
                        return false;
                    }
                    options.CaptureTicks = entries.Select(int.Parse).Distinct().OrderBy(tick => tick).ToArray();
                    break;

                case "--soul-sense":
                    options.ForceSoulSense = true;
                    break;

                case "--resonance":
                    options.ForceResonance = true;
                    break;
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

                case "--capture-start-at-tick":
                    if (!TryTakePositiveInt(args, ref index, argument, out int startTick, out error))
                    {
                        return false;
                    }
                    options.CaptureStartAtTick = startTick;
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
                    options.VisualScenario = scenario.ToLowerInvariant();
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

        if ((options.CaptureTicks.Length > 0 || options.ForceSoulSense || options.ForceResonance) && string.IsNullOrEmpty(options.VisualScenario))
        {
            error = "--capture-ticks, --soul-sense and --resonance require --visual-scenario.";
            return false;
        }
        if (options.CaptureTicks.Length > 0 && options.CaptureAfterTicks >= 0)
        {
            error = "Use --capture-ticks or --capture-after-ticks, not both.";
            return false;
        }

        if (options.HasCaptureRequest && string.IsNullOrEmpty(options.CaptureOutput))
        {
            options.CaptureOutput = "artifacts/visual-max/local-run";
        }

        if (options.CaptureStartAtTick >= 0 && !options.HasCaptureRequest)
        {
            error = "--capture-start-at-tick requires --capture-after-ticks or --visual-scenario.";
            return false;
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
