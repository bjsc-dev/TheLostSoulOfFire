using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Input;

namespace TheLostSoulOfFire.Debugging;

/// <summary>
/// Drives real input/debug seams at fixed update ticks. Scenarios never edit combat
/// values or entity internals: they use the same title transition, debug spawn/clear
/// hooks and input paths as a developer running the game.
/// </summary>
public sealed class VisualScenarioRunner
{
    private static readonly IReadOnlyDictionary<string, int> CaptureTicks =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["title-arrival"] = 20,
            ["arena-idle"] = 112,
            ["dash"] = 130,
            ["scythe-combo"] = 164,
            ["hollow-swipe"] = 248,
            ["burning-charge"] = 266,
            ["devourer-slam"] = 306,
            ["cannon-sense"] = 208,
            ["resonance-busy"] = 156,
            ["soul-release"] = 206,
            ["death-retry"] = 126,
            ["ending"] = 700
        };

    private readonly string _scenario;
    private readonly int _captureTick;
    private int _tick;
    private int _combatTicks;
    private int _completedWave;

    public static string KnownScenarioList => string.Join(", ", CaptureTicks.Keys);
    public bool CaptureRequested { get; private set; }
    public bool TimedOut => _tick > _captureTick + 180;
    public int Tick => _tick;
    public string Scenario => _scenario;

    public VisualScenarioRunner(string scenario)
    {
        _scenario = scenario;
        _captureTick = CaptureTicks[scenario];
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
                SpawnAt(input, Keys.F2, 104);
                if (_tick is 142 or 150 or 158)
                {
                    input.InjectLeftMouseDown();
                }
                break;

            case "hollow-swipe":
                SpawnAt(input, Keys.F2, 100);
                break;

            case "burning-charge":
                SpawnAt(input, Keys.F3, 100);
                break;

            case "devourer-slam":
                SpawnAt(input, Keys.F4, 100);
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

        if (_tick == _captureTick)
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
