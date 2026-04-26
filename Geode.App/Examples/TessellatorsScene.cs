using Geode.Core;
using Geode.Core.Geometry;
using Geode.Core.Tessellation;
using Geode.Rendering;
using Geode.Rendering.Buffers;
using Geode.Rendering.Shaders;
using Geode.Rendering.State;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System;
using System.IO;
using System.Numerics;
using PrimitiveType = Geode.Core.Geometry.PrimitiveType;
// Silk also exports a generic Vector3D<T>; disambiguate to Geode's double-precision one.
using Vector3D = Geode.Core.Vector3D;

namespace Geode.App.Examples;

/// <summary>
/// Visual comparison of the tessellators in <c>Geode.Core.Tessellation</c>:
/// <see cref="SubdivisionSphereTessellatorSimple"/>,
/// <see cref="CubeMapEllipsoidTessellator"/>, and
/// <see cref="GeographicGridEllipsoidTessellator"/>. Auto-rotates the mesh;
/// keyboard swaps tessellator, adjusts subdivision level, and toggles wireframe.
/// </summary>
/// <remarks>
/// The tessellators emit positions as an <c>EmulatedDoubleVector3</c>
/// (DSFP/RTE high+low split) named "position". The Mesh-to-VAO bridge in
/// <see cref="RenderContext.CreateVertexArray"/> binds that attribute
/// directly to the shader's <c>in vec4 position</c> -- the double is
/// uploaded as a float cast, and OpenGL fills the missing <c>w</c>
/// component with <c>1.0</c>. Switching to the RTE path later is a shader
/// change only: declare <c>positionHigh</c> + <c>positionLow</c> and the
/// bridge picks up the split automatically.
///
/// Shading is the diffuse-lighting model from "3D Engine Design for Virtual
/// Globes" Listing 4.7 (camera-attached headlamp). The shader source lives
/// in <c>Examples/Shaders/Tessellators.{vert,frag}</c>; those files are
/// copied next to the executable by the .csproj.
/// </remarks>
public sealed class TessellatorsScene : IDisposable
{
    private enum TessellatorKind { SubdivisionSphere, CubeMap, GeographicGrid }

    private readonly GraphicsWindow _window;
    private readonly SceneState _sceneState = new();
    private readonly ClearState _clearState = new()
    {
        Color = new Vector4(0.08f, 0.09f, 0.12f, 1f)
    };

    private readonly RenderState _renderState = new()
    {
        // Start in wireframe so the topology is visible without lighting, and
        // we can immediately see if anything's degenerate.
        RasterizationMode = RasterizationMode.Line
    };
    private DrawState? _drawState;

    private ShaderProgram? _shaderProgram;
    private VertexArrayObject? _vertexArray;

    private TessellatorKind _kind = TessellatorKind.SubdivisionSphere;
    private int _subdivisionLevel = 3;
    private double _elapsed;
    private bool _rotate = true;

    public TessellatorsScene()
    {
        _window = Device.CreateWindow(1280, 720, "Geode - Tessellators");

        _window.Window.Load += OnLoad;
        _window.Window.Render += OnRender;
        _window.Window.Resize += OnResize;
        _window.Window.Closing += OnClose;
    }

    public void Run() => _window.Run();

    private void OnLoad()
    {
        Device device = _window.Device;
        RenderContext context = _window.Context;

        IInputContext input = _window.Window.CreateInput();
        foreach (IKeyboard keyboard in input.Keyboards)
            keyboard.KeyDown += OnKeyDown;

        string shaderDir = Path.Combine(AppContext.BaseDirectory, "Examples", "Shaders");
        _shaderProgram = device.CreateShaderProgramFromFiles(
            Path.Combine(shaderDir, "Tessellators.vert"),
            Path.Combine(shaderDir, "Tessellators.frag"));

        BuildMesh();

        // Unit sphere at origin. Camera at z=4 with a 45° FOV leaves the sphere
        // filling ~30% of the vertical viewport.
        _sceneState.Camera.Eye = new Vector3D(0, 0, 4.0);
        _sceneState.Camera.Target = new Vector3D(0, 0, 0);
        _sceneState.Camera.Up = new Vector3D(0, 1, 0);
        _sceneState.Camera.NearPlane = 0.1;
        _sceneState.Camera.FarPlane = 50.0;
        _sceneState.Camera.FieldOfViewY = Trigonometry.ToRadians(45.0);
        _sceneState.Camera.AspectRatio =
            (double)_window.Window.Size.X / _window.Window.Size.Y;
        _sceneState.Viewport = new Vector4(0, 0,
            _window.Window.Size.X, _window.Window.Size.Y);

        // Headlamp pattern: pin the diffuse light to the camera eye so the
        // lit hemisphere is always the one facing the viewer. The model
        // rotates beneath a fixed light, which gives a clean read of the
        // tessellation as faces sweep through the terminator.
        _sceneState.CameraLightPosition = new Vector3(0, 0, 4f);

        // Some drivers don't initialize the GL viewport to match the window's
        // client area until a resize event fires. Setting it explicitly here
        // ensures rendering before the first OnResize still covers the window.
        _window.Gl.Viewport(0, 0, (uint)_window.Window.Size.X, (uint)_window.Window.Size.Y);

        GL gl = _window.Gl;
        Console.WriteLine($"OpenGL {gl.GetStringS(StringName.Version)}");
        Console.WriteLine($"GPU:   {gl.GetStringS(StringName.Renderer)}");
        PrintStatus();
    }

    /// <summary>
    /// (Re)builds the mesh from whichever tessellator is active, rebuilds the
    /// VAO against the linked shader, and wraps both in a fresh DrawState.
    /// Called on load, on tessellator swap, and on subdivision change.
    /// </summary>
    private void BuildMesh()
    {
        RenderContext context = _window.Context;

        // The old VAO owns GPU buffers for the previous mesh; dispose it so
        // we don't leak on swap. The shader program is reused across builds.
        _vertexArray?.Dispose();

        Mesh mesh = _kind switch
        {
            TessellatorKind.SubdivisionSphere =>
                SubdivisionSphereTessellatorSimple.Compute(_subdivisionLevel),
            TessellatorKind.CubeMap =>
                CubeMapEllipsoidTessellator.Compute(
                    Ellipsoid.UnitSphere,
                    Math.Max(1, _subdivisionLevel),
                    CubeMapEllipsoidVertexAttributes.Position),
            TessellatorKind.GeographicGrid =>
                GeographicGridEllipsoidTessellator.Compute(
                    Ellipsoid.UnitSphere,
                    Math.Max(3, 8 + _subdivisionLevel * 4),  // slices (meridians)
                    Math.Max(2, 4 + _subdivisionLevel * 2),  // stacks (parallels)
                    GeographicGridEllipsoidVertexAttributes.Position),
            _ => throw new InvalidOperationException()
        };

        _vertexArray = context.CreateVertexArray(mesh, _shaderProgram!, BufferHint.StaticDraw);
        _drawState = new DrawState(_renderState, _shaderProgram!, _vertexArray);
    }

   
    private void OnRender(double deltaTime)
    {
        if (_rotate) _elapsed += deltaTime;

        float angle = (float)(_elapsed * 0.6);
        _sceneState.ModelMatrix = Matrix4F.RotationY(angle);

        RenderContext context = _window.Context;
        context.Clear(_clearState);
        context.Draw(PrimitiveType.Triangles, _drawState!, _sceneState);
    }

    private void OnResize(Vector2D<int> size)
    {
        _window.Gl.Viewport(0, 0, (uint)size.X, (uint)size.Y);
        _sceneState.Viewport = new Vector4(0, 0, size.X, size.Y);
        _sceneState.Camera.AspectRatio = size.X / (double)size.Y;
    }

    private void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        switch (key)
        {
            case Key.Escape:
                _window.Window.Close();
                break;

            case Key.Number1:
                _kind = TessellatorKind.SubdivisionSphere;
                BuildMesh();
                PrintStatus();
                break;

            case Key.Number2:
                _kind = TessellatorKind.CubeMap;
                BuildMesh();
                PrintStatus();
                break;

            case Key.Number3:
                _kind = TessellatorKind.GeographicGrid;
                BuildMesh();
                PrintStatus();
                break;

            case Key.W:
                _renderState.RasterizationMode =
                    _renderState.RasterizationMode == RasterizationMode.Fill
                        ? RasterizationMode.Line
                        : RasterizationMode.Fill;
                PrintStatus();
                break;

            case Key.C:
                // Toggle back-face culling. If a tessellator emits inverted
                // winding, disabling cull makes the problem obvious: interior
                // faces suddenly become visible.
                _renderState.FacetCulling.Enabled = !_renderState.FacetCulling.Enabled;
                PrintStatus();
                break;

            case Key.R:
                _rotate = !_rotate;
                PrintStatus();
                break;

            case Key.KeypadAdd:
            case Key.Equal: // '+' on US layout without shift is '='
                _subdivisionLevel = Math.Min(_subdivisionLevel + 1, 8);
                BuildMesh();
                PrintStatus();
                break;

            case Key.KeypadSubtract:
            case Key.Minus:
                _subdivisionLevel = Math.Max(_subdivisionLevel - 1, 0);
                BuildMesh();
                PrintStatus();
                break;
        }
    }

    private void PrintStatus()
    {
        Console.WriteLine(
            $"[tessellator] {_kind,-18}  subdivisions={_subdivisionLevel}  " +
            $"mode={_renderState.RasterizationMode}  " +
            $"cull={_renderState.FacetCulling.Enabled}   " +
            $"rotate={_rotate}   " +
            "(1=Sphere 2=CubeMap 3=GeoGrid  W=wire  C=cull  R=rotate  +/-=subdiv  Esc=quit)");
    }

    private void OnClose()
    {
        _vertexArray?.Dispose();
        _shaderProgram?.Dispose();
    }

    public void Dispose()
    {
        _vertexArray?.Dispose();
        _shaderProgram?.Dispose();
        _window.Dispose();
    }

}
