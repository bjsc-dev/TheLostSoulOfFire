using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Debugging;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Input;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire;

public sealed class Game1 : Microsoft.Xna.Framework.Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixel = null!;
    private InputState _input = null!;
    private GameWorld _world = null!;
    private ArtAssets _art = null!;
    private SoulfireRenderer _soulfireRenderer = null!;
    private readonly PresentationSettings _presentationSettings = new();
    private readonly VisualRunOptions _visualOptions;
    private readonly VisualScenarioRunner _visualScenarioRunner = null!;
    private readonly bool _audioGameplayTest;
    private readonly bool _audioDeathRestartTest;
    private bool _screenshotRequested;
    private string _screenshotStatus = string.Empty;
    private float _audioTestTotalTime;
    private float _audioTestStateTime;
    private int _audioTestWave;
    private bool _audioTestWaveKilled;
    private bool _audioTestCompleteSeen;
    private bool _audioTestRestartInjected;
    private bool _audioTestDeathRequested;
    private int _visualTick;
    private bool _exitAfterVisualCapture;

    public Game1(
        bool audioGameplayTest = false,
        bool audioDeathRestartTest = false,
        VisualRunOptions visualOptions = null!)
    {
        _audioGameplayTest = audioGameplayTest;
        _audioDeathRestartTest = audioDeathRestartTest;
        _visualOptions = visualOptions ?? new VisualRunOptions();
        if (_visualOptions.HasQuality)
        {
            _presentationSettings.SetQuality(_visualOptions.Quality);
        }
        _presentationSettings.SetReducedEffects(_visualOptions.ReducedEffects);
        if (!string.IsNullOrEmpty(_visualOptions.VisualScenario))
        {
            _visualScenarioRunner = new VisualScenarioRunner(_visualOptions.VisualScenario);
        }
        _exitAfterVisualCapture = _visualOptions.ExitAfterCapture || !string.IsNullOrEmpty(_visualOptions.VisualScenario);
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = GameBalance.BackBufferWidth,
            PreferredBackBufferHeight = GameBalance.BackBufferHeight,
            SynchronizeWithVerticalRetrace = true
        };

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1d / 60d);
        Window.Title = "The Lost Soul of Fire";
    }

    protected override void Initialize()
    {
        _input = new InputState();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
        _art = new ArtAssets(Content);
        _world = new GameWorld(GraphicsDevice.Viewport, _art, Content, _presentationSettings);
        _soulfireRenderer = new SoulfireRenderer(GraphicsDevice, _presentationSettings);
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update();
        _visualTick++;
        if (_visualScenarioRunner is not null)
        {
            _visualScenarioRunner.Update(_input, _world, GraphicsDevice.Viewport);
        }
        if (_audioGameplayTest || _audioDeathRestartTest)
        {
            ConfigureAutomatedTest((float)gameTime.ElapsedGameTime.TotalSeconds);
        }

        if (_input.IsKeyDown(Keys.Escape) ||
            GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
        {
            Exit();
            return;
        }

        if (_input.WasKeyPressed(Keys.F9))
        {
            _screenshotRequested = true;
        }

        if (_input.WasKeyPressed(Keys.F10))
        {
            _presentationSettings.ToggleReducedEffects();
            _screenshotStatus = $"Visual effects: {_presentationSettings.Summary}";
        }

        if (_input.WasKeyPressed(Keys.F11))
        {
            _presentationSettings.ToggleQuality();
            _screenshotStatus = $"Visual quality: {_presentationSettings.Summary}";
        }

        _world.Update(gameTime, _input, GraphicsDevice.Viewport);
        if (_visualOptions.CaptureAfterTicks == _visualTick || _visualScenarioRunner is not null && _visualScenarioRunner.CaptureRequested)
        {
            _screenshotRequested = true;
        }
        if (_visualScenarioRunner is not null && _visualScenarioRunner.TimedOut)
        {
            Console.Error.WriteLine($"VISUAL_SCENARIO_TIMEOUT scenario={_visualScenarioRunner.Scenario} tick={_visualScenarioRunner.Tick}");
            Environment.ExitCode = 1;
            Exit();
            return;
        }
        if (_audioGameplayTest || _audioDeathRestartTest)
        {
            FinishAutomatedTestFrame();
        }
        Window.Title = string.IsNullOrEmpty(_screenshotStatus) ? _world.WindowTitle : _screenshotStatus;
        _screenshotStatus = string.Empty;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(GameBalance.VoidColor);
        Viewport viewport = GraphicsDevice.Viewport;
        _world.Draw(_spriteBatch, _pixel, viewport, _soulfireRenderer);

        if (_screenshotRequested)
        {
            _screenshotRequested = false;
            string context = _visualScenarioRunner is not null ? _visualScenarioRunner.Scenario : _world.ScreenshotContext;
            string outputName = _visualScenarioRunner is not null ? _visualScenarioRunner.Scenario : string.Empty;
            string path;
            bool captured = _visualOptions.HasCaptureRequest
                ? ScreenshotCapture.TrySaveBackBuffer(
                    GraphicsDevice,
                    context,
                    _visualOptions.CaptureOutput,
                    outputName,
                    out path)
                : ScreenshotCapture.TrySaveBackBuffer(GraphicsDevice, context, out path);
            _screenshotStatus = captured
                ? $"Screenshot saved — {path}"
                : $"Screenshot failed — {path}";
            if (_visualScenarioRunner is not null)
            {
                _visualScenarioRunner.MarkCaptureHandled();
            }
            if (_exitAfterVisualCapture)
            {
                Console.WriteLine($"VISUAL_CAPTURE_{(captured ? "PASS" : "FAIL")} context={context} path={path} tick={_visualTick} settings={_presentationSettings.Summary}");
                Environment.ExitCode = captured ? 0 : 1;
                Exit();
            }
        }

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _world.Dispose();
        _soulfireRenderer.Dispose();
        _pixel.Dispose();
        _spriteBatch.Dispose();
        base.UnloadContent();
    }

    private void ConfigureAutomatedTest(float deltaTime)
    {
        _audioTestTotalTime += deltaTime;
        _audioTestStateTime += deltaTime;

        if (_world.LoopState == ArenaLoopState.Title)
        {
            _input.InjectKeyPress(Keys.Space);
            return;
        }

        if (_audioDeathRestartTest && _world.PlayerDead)
        {
            if (!_audioTestRestartInjected && _audioTestStateTime >= 1.4f)
            {
                _audioTestRestartInjected = true;
                _input.InjectKeyPress(Keys.R);
            }
            return;
        }

        if (_world.LoopState == ArenaLoopState.Combat)
        {
            if (_audioDeathRestartTest)
            {
                if (!_audioTestDeathRequested)
                {
                    _audioTestDeathRequested = true;
                    _audioTestStateTime = 0f;
                    _world.RequestAudioTestFatalDamage();
                }
                return;
            }

            if (_audioTestWave != _world.WaveNumber)
            {
                _audioTestWave = _world.WaveNumber;
                _audioTestStateTime = 0f;
                _audioTestWaveKilled = false;
                Console.WriteLine($"AUDIO_GAMEPLAY_WAVE {_audioTestWave}");
            }

            if (!_audioTestWaveKilled && _audioTestStateTime >= 0.65f)
            {
                _audioTestWaveKilled = true;
                _input.InjectKeyPress(Keys.F6);
            }
            return;
        }

        if (_world.LoopState == ArenaLoopState.Complete)
        {
            if (!_audioTestCompleteSeen)
            {
                _audioTestCompleteSeen = true;
                _audioTestStateTime = 0f;
                Console.WriteLine("AUDIO_GAMEPLAY_COMPLETE");
            }

            if (!_audioTestRestartInjected && _audioTestStateTime >= 5.2f)
            {
                _audioTestRestartInjected = true;
                _input.InjectKeyPress(Keys.R);
            }
        }

    }

    private void FinishAutomatedTestFrame()
    {
        if (_audioDeathRestartTest)
        {
            if (_audioTestRestartInjected && !_world.PlayerDead && _world.LoopState == ArenaLoopState.Intro)
            {
                Console.WriteLine("AUDIO_DEATH_RESTART_TEST_PASS death=true restart=true");
                Environment.ExitCode = 0;
                Exit();
            }
            else if (_audioTestTotalTime >= 10f)
            {
                Console.WriteLine($"AUDIO_DEATH_RESTART_TEST_FAIL dead={_world.PlayerDead} state={_world.LoopState}");
                Environment.ExitCode = 1;
                Exit();
            }
            return;
        }

        if (_world.PlayerDead || _audioTestTotalTime >= 35f)
        {
            Console.WriteLine($"AUDIO_GAMEPLAY_TEST_FAIL dead={_world.PlayerDead} state={_world.LoopState} wave={_world.WaveNumber}");
            Environment.ExitCode = 1;
            Exit();
            return;
        }

        if (_audioTestRestartInjected && _world.LoopState == ArenaLoopState.Intro && _world.WaveNumber == 0)
        {
            Console.WriteLine("AUDIO_GAMEPLAY_TEST_PASS waves=4 completion=true restart=true");
            Environment.ExitCode = 0;
            Exit();
        }
    }
}
