using Geode.Rendering;
using Geode.Rendering.State;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System;

namespace Geode.App;

public class Program
{
    private static GraphicsWindow? _window;
    private static readonly ClearState _clearState = ClearState.Default;

    public static void Main(string[] args)
    {
        _window = Device.CreateWindow(1280, 720, "Geode");

        _window.Window.Load += OnLoad;
        _window.Window.Update += OnUpdate;
        _window.Window.Render += OnRender;
        _window.Window.Resize += OnResize;
        _window.Window.Closing += OnClose;

        _window.Run();
        _window.Dispose();
    }

    private static void OnLoad()
    {
        // By the time this fires, GraphicsWindow has constructed GL, Device,
        // and RenderContext. RenderContext has already seeded its render-state
        // shadow (depth test, culling, reversed-Z ClipControl) -- no raw GL
        // setup needed here.
        IInputContext input = _window!.Window.CreateInput();
        foreach (IKeyboard keyboard in input.Keyboards)
            keyboard.KeyDown += OnKeyDown;

        GL gl = _window.Gl;
        Device device = _window.Device;
        Console.WriteLine($"OpenGL {gl.GetStringS(StringName.Version)}");
        Console.WriteLine($"GPU:   {gl.GetStringS(StringName.Renderer)}");
        Console.WriteLine($"Vertex attrs: {device.MaximumNumberOfVertexAttributes}, " +
                          $"Texture units: {device.NumberOfTextureUnits}, " +
                          $"Color attachments: {device.MaximumNumberOfColorAttachments}");
    }

    private static void OnUpdate(double deltaTime)
    {
    }

    private static void OnRender(double deltaTime)
    {
        _window!.Context.Clear(_clearState);
    }

    private static void OnResize(Vector2D<int> size)
    {
        _window?.Gl.Viewport(0, 0, (uint)size.X, (uint)size.Y);
    }

    private static void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        if (key == Key.Escape)
            _window?.Window.Close();
    }

    private static void OnClose()
    {
    }
}
