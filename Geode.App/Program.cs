using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using Silk.NET.Input;
using System;

namespace Geode.App;

public class Program
{
    private static IWindow? _window;
    private static GL? _gl;

    public static void Main(string[] args)
    {
        var options = WindowOptions.Default with
        {
            Size = new Vector2D<int>(1280, 720),
            Title = "Geode",
            API = new GraphicsAPI(
                ContextAPI.OpenGL, ContextProfile.Core,
                ContextFlags.Default, new APIVersion(3, 3))
        };

        _window = Window.Create(options);
        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Closing += OnClose;
        _window.Resize += OnResize;

        _window.Run();
    }

    private static void OnLoad()
    {
        _gl = GL.GetApi(_window!);

        var input = _window!.CreateInput();
        foreach (var keyboard in input.Keyboards)
            keyboard.KeyDown += OnKeyDown;

        _gl.Enable(EnableCap.DepthTest);
        _gl.Enable(EnableCap.CullFace);
        _gl.CullFace(TriangleFace.Back);
        _gl.FrontFace(FrontFaceDirection.Ccw);
        _gl.ClearColor(0f, 0f, 0f, 1f);

        Console.WriteLine($"OpenGL {_gl.GetStringS(StringName.Version)}");
        Console.WriteLine($"GPU:   {_gl.GetStringS(StringName.Renderer)}");
    }

    private static void OnUpdate(double deltaTime)
    {
    }

    private static void OnRender(double deltaTime)
    {
        _gl!.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    private static void OnResize(Vector2D<int> size)
    {
        _gl?.Viewport(0, 0, (uint)size.X, (uint)size.Y);
    }

    private static void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        if (key == Key.Escape)
            _window?.Close();
    }

    private static void OnClose()
    {
        _gl?.Dispose();
    }
}
