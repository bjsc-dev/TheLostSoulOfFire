using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Input;

namespace TheLostSoulOfFire.Debugging;

/// <summary>
/// Drives real input at fixed update ticks. Single-subject fixtures arrange real
/// entities through a narrow debug seam; combat values and state machines stay
/// untouched. Sidecars report observed phases, not just scenario labels.
/// </summary>
public sealed class VisualScenarioRunner
{
    private static readonly IReadOnlyDictionary<string, int> CaptureTicks =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["title-arrival"] = 180,
            ["arena-idle"] = 112,
            ["dash"] = 130,
            ["scythe-combo"] = 164,
            ["hollow-swipe"] = 164,
            ["burning-charge"] = 130,
            ["devourer-slam"] = 205,
            ["cannon-sense"] = 208,
            ["resonance-busy"] = 156,
            ["soul-release"] = 206,
            ["death-retry"] = 220,
            ["ending"] = 2100
        };

    private readonly string _scenario;
    private readonly int _captureTick;
    private readonly int[] _captureTicks;
    private readonly bool _forceSense;
    private readonly bool _forceResonance;
    private readonly bool _semanticEnding;
    private int _tick;
    private int _combatTicks;
    private int _completedWave;

    public static string KnownScenarioList => string.Join(", ", CaptureTicks.Keys);
    public bool CaptureRequested { get; private set; }
    public bool TimedOut => _tick > _captureTick + 180;
    public int Tick => _tick;
    public string Scenario => _scenario;
    public bool IsLastCapture => _semanticEnding || _tick >= _captureTick;
    public string OutputName => _captureTicks.Length > 1 ? $"{_scenario}-{_tick:D4}" : _scenario;

    public VisualScenarioRunner(VisualRunOptions options)
    {
        _scenario = options.VisualScenario;
        _forceSense = options.ForceSoulSense;
        _forceResonance = options.ForceResonance;
        _semanticEnding = _scenario == "ending" && options.CaptureTicks.Length == 0 && options.CaptureAfterTicks < 0;
        _captureTicks = options.CaptureTicks.Length > 0 ? options.CaptureTicks :
            [options.CaptureAfterTicks >= 0 ? options.CaptureAfterTicks : CaptureTicks[_scenario]];
        _captureTick = _captureTicks.Last();
    }

    public static bool IsKnownScenario(string scenario) => CaptureTicks.ContainsKey(scenario);

    public void Update(InputState input, GameWorld world, Viewport viewport)
    {
        _tick++;
        if (!string.Equals(_scenario, "title-arrival", StringComparison.OrdinalIgnoreCase))
        {
            input.InjectMousePosition(new Point(viewport.Width / 2 + 280, viewport.Height / 2));
        }

        if (_tick == 2 && !string.Equals(_scenario, "title-arrival", StringComparison.OrdinalIgnoreCase))
        {
            input.InjectKeyPress(Keys.Space);
        }

        switch (_scenario)
        {
            case "dash":
                if (_tick is >= 116 and <= 124)
                {
                    input.InjectHeldKey(Keys.D);
                }
                if (_tick == 120)
                {
                    input.InjectKeyPress(Keys.Space);
                }
                break;

            case "scythe-combo":
                if (_tick == 139) world.ArrangeVisualSubject(_scenario);
                if (_tick is 142 or 150 or 171)
                {
                    input.InjectLeftMouseDown();
                }
                break;

            case "hollow-swipe":
                if (_tick == 100) world.ArrangeVisualSubject(_scenario);
                break;

            case "burning-charge":
                if (_tick == 100) world.ArrangeVisualSubject(_scenario);
                break;

            case "devourer-slam":
                if (_tick == 100) world.ArrangeVisualSubject(_scenario);
                break;

            case "cannon-sense":
                SpawnAt(input, Keys.F2, 100);
                if (_tick == 104)
                {
                    input.InjectKeyPress(Keys.F7);
                }
                if (_tick is >= 120 and <= 200)
                {
                    input.InjectRightMouseDown();
                }
                break;

            case "resonance-busy":
                SpawnAt(input, Keys.F2, 100);
                SpawnAt(input, Keys.F3, 100);
                SpawnAt(input, Keys.F4, 100);
                if (_tick == 112)
                {
                    input.InjectKeyPress(Keys.F5);
                }
                if (_tick == 114)
                {
                    input.InjectKeyPress(Keys.R);
                }
                break;

            case "soul-release":
                SpawnAt(input, Keys.F2, 100);
                if (_tick == 144)
                {
                    input.InjectKeyPress(Keys.F6);
                }
                break;

            case "death-retry":
                if (_tick == 112)
                {
                    world.RequestAudioTestFatalDamage();
                }
                break;

            case "ending":
                AdvanceEnding(input, world);
                break;
        }

        if (_forceSense && _tick == 104 && _scenario != "cannon-sense")
            input.InjectKeyPress(Keys.F7);
        if (_forceResonance && _scenario != "resonance-busy")
        {
            if (_tick == 112) input.InjectKeyPress(Keys.F5);
            if (_tick == 114) input.InjectKeyPress(Keys.R);
        }

        if (_semanticEnding ? world.LoopState == ArenaLoopState.Complete && world.PresentationStateTime >= 6f : _captureTicks.Contains(_tick))
        {
            CaptureRequested = true;
        }
    }

    public void MarkCaptureHandled() => CaptureRequested = false;

    private void SpawnAt(InputState input, Keys key, int tick)
    {
        if (_tick == tick)
        {
            input.InjectKeyPress(key);
        }
    }

    private void AdvanceEnding(InputState input, GameWorld world)
    {
        if (world.LoopState != ArenaLoopState.Combat)
        {
            _combatTicks = 0;
            return;
        }

        _combatTicks++;
        if (_completedWave == world.WaveNumber || _combatTicks < 26)
        {
            return;
        }

        _completedWave = world.WaveNumber;
        input.InjectKeyPress(Keys.F6);
    }
}
