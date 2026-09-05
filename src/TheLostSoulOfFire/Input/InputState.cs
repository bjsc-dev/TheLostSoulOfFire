using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace TheLostSoulOfFire.Input;

public sealed class InputState
{
    private KeyboardState _previousKeyboard;
    private KeyboardState _keyboard;
    private MouseState _previousMouse;
    private MouseState _mouse;
    private readonly HashSet<Keys> _injectedPresses = [];
    private readonly HashSet<Keys> _injectedHeldKeys = [];
    private readonly HashSet<Keys> _previousInjectedHeldKeys = [];
    private Point? _injectedMousePosition;
    private bool _injectedLeftMouseDown;
    private bool _previousInjectedLeftMouseDown;
    private bool _injectedRightMouseDown;
    private bool _previousInjectedRightMouseDown;

    public Point MousePosition => _injectedMousePosition ?? _mouse.Position;
    public bool AnyInputPressed
    {
        get
        {
            if (_injectedPresses.Count > 0)
            {
                return true;
            }
            foreach (Keys key in _keyboard.GetPressedKeys())
            {
                if (_previousKeyboard.IsKeyUp(key))
                {
                    return true;
                }
            }

            return WasLeftMousePressed || WasRightMousePressed;
        }
    }

    public void Update()
    {
        _injectedPresses.Clear();
        _previousInjectedHeldKeys.Clear();
        _previousInjectedHeldKeys.UnionWith(_injectedHeldKeys);
        _injectedHeldKeys.Clear();
        _previousInjectedLeftMouseDown = _injectedLeftMouseDown;
        _previousInjectedRightMouseDown = _injectedRightMouseDown;
        _injectedLeftMouseDown = false;
        _injectedRightMouseDown = false;
        _injectedMousePosition = null;
        _previousKeyboard = _keyboard;
        _previousMouse = _mouse;
        _keyboard = Keyboard.GetState();
        _mouse = Mouse.GetState();
    }

    public bool IsKeyDown(Keys key) => _injectedHeldKeys.Contains(key) || _keyboard.IsKeyDown(key);

    public bool WasKeyPressed(Keys key) =>
        _injectedPresses.Contains(key) || _keyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);

    public bool WasKeyReleased(Keys key) =>
        _keyboard.IsKeyUp(key) && _previousKeyboard.IsKeyDown(key);

    public bool IsLeftMouseDown => _injectedLeftMouseDown || _mouse.LeftButton == ButtonState.Pressed;
    public bool WasLeftMousePressed =>
        _injectedLeftMouseDown && !_previousInjectedLeftMouseDown ||
        _mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;
    public bool WasLeftMouseReleased =>
        !_injectedLeftMouseDown && _previousInjectedLeftMouseDown ||
        _mouse.LeftButton == ButtonState.Released && _previousMouse.LeftButton == ButtonState.Pressed;
    public bool IsRightMouseDown => _injectedRightMouseDown || _mouse.RightButton == ButtonState.Pressed;
    public bool WasRightMousePressed =>
        _injectedRightMouseDown && !_previousInjectedRightMouseDown ||
        _mouse.RightButton == ButtonState.Pressed && _previousMouse.RightButton == ButtonState.Released;
    public bool WasRightMouseReleased =>
        !_injectedRightMouseDown && _previousInjectedRightMouseDown ||
        _mouse.RightButton == ButtonState.Released && _previousMouse.RightButton == ButtonState.Pressed;

    internal void InjectKeyPress(Keys key) => _injectedPresses.Add(key);
    internal void InjectHeldKey(Keys key) => _injectedHeldKeys.Add(key);
    internal void InjectMousePosition(Point position) => _injectedMousePosition = position;
    internal void InjectLeftMouseDown() => _injectedLeftMouseDown = true;
    internal void InjectRightMouseDown() => _injectedRightMouseDown = true;
}
