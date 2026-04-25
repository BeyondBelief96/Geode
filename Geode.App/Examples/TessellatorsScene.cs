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
using System.Numerics;
using PrimitiveType = Geode.Core.Geometry.PrimitiveType;
// Silk also exports a generic Vector3D<T>; disambiguate to Geode's double-precision one.
using Vector3D = Geode.Core.Vector3D;

namespace Geode.App.Examples;

/// <summary>
/// Visual comparison of the two tessellators in <c>Geode.Core.Tessellation</c>:
/// <see cref="SubdivisionSphereTessellatorSimple"/> and
/// <see cref="CubeMapEllipsoidTessellator"/>. Auto-rotates the mesh; keyboard
/// swaps tessellator, adjusts subdivision level, and toggles wireframe.
/// </summary>
/// <remarks>
/// Both tessellators emit positions as an <c>EmulatedDoubleVector3</c> named
/// "position", so the vertex shader declares <c>positionHigh</c> and
/// <c>positionLow</c>. For a unit-scale mesh the "low" contribution is tiny,
/// but wiring it up exercises the RTE/DSFP split path end-to-end. Fragment
/// colors come from the surface direction, so the shape is legible without
/// any lighting setup.
/// </remarks>
public sealed class TessellatorsScene : IDisposable
{
    private enum TessellatorKind { SubdivisionSphere, CubeMap }

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

        _shaderProgram = device.CreateShaderProgram(VertexShaderSource, FragmentShaderSource);

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
            _ => throw new InvalidOperationException()
        };

        _vertexArray = context.CreateVertexArray(mesh, _shaderProgram!, BufferHint.StaticDraw);
        _drawState = new DrawState(_renderState, _shaderProgram!, _vertexArray);

        DumpMeshDiagnostics(mesh);
        DumpShaderAttributes();
        DumpVaoLayout();
    }

    /// <summary>
    /// Queries OpenGL directly about what attribute format/binding got programmed
    /// into the VAO. Useful for spotting stride/offset/binding mismatches.
    /// Uses raw GLenum ints because Silk.NET's binding for these 4.5 DSA
    /// queries exposes them differently across versions.
    /// </summary>
    private unsafe void DumpVaoLayout()
    {
        if (_vertexArray == null) return;
        GL gl = _window.Gl;
        uint vao = _vertexArray.Handle;

        // Need the VAO bound for legacy (non-DSA) queries to work.
        gl.BindVertexArray(vao);

        // Per-attribute state. glGetVertexAttribiv reads from the bound VAO.
        for (uint i = 0; i < 2; i++)
        {
            int enabled = 0, size = 0, type = 0, stride = 0, relOffset = 0, binding = 0;
            gl.GetVertexAttrib(i, GLEnum.VertexAttribArrayEnabled, &enabled);
            gl.GetVertexAttrib(i, GLEnum.VertexAttribArraySize, &size);
            gl.GetVertexAttrib(i, GLEnum.VertexAttribArrayType, &type);
            gl.GetVertexAttrib(i, GLEnum.VertexAttribArrayStride, &stride);
            gl.GetVertexAttrib(i, GLEnum.VertexAttribRelativeOffset, &relOffset);
            gl.GetVertexAttrib(i, GLEnum.VertexAttribBinding, &binding);
            Console.WriteLine(
                $"[vao] attr[{i}] enabled={enabled} size={size} type=0x{type:X4} " +
                $"stride={stride} relOffset={relOffset} binding={binding}");
        }
    }

    /// <summary>
    /// Queries the compiled shader for its active attributes and prints the
    /// location, type, and size of each. If <c>positionHigh</c> isn't at
    /// location 0 or <c>positionLow</c> isn't at location 1, the VBO layout
    /// (which the Mesh-to-VAO bridge sorts by location) won't match the
    /// attribute bindings the shader expects.
    /// </summary>
    private unsafe void DumpShaderAttributes()
    {
        if (_shaderProgram == null) return;
        GL gl = _window.Gl;
        uint prog = _shaderProgram.Handle;

        gl.GetProgram(prog, ProgramPropertyARB.ActiveAttributes, out int count);
        gl.GetProgram(prog, ProgramPropertyARB.ActiveAttributeMaxLength, out int maxLen);
        Console.WriteLine($"[shader] active attributes = {count}");
        for (uint i = 0; i < (uint)count; i++)
        {
            gl.GetActiveAttrib(prog, i, (uint)maxLen,
                out _, out int size, out AttributeType type, out string name);
            int loc = gl.GetAttribLocation(prog, name);
            Console.WriteLine($"         [{i}] name='{name}' location={loc} type={type} size={size}");
        }

        gl.GetProgram(prog, ProgramPropertyARB.ActiveUniforms, out int uniformCount);
        Console.WriteLine($"[shader] active uniforms = {uniformCount}");
        gl.GetProgram(prog, ProgramPropertyARB.ActiveUniformMaxLength, out int uMaxLen);
        for (uint i = 0; i < (uint)uniformCount; i++)
        {
            gl.GetActiveUniform(prog, i, (uint)uMaxLen,
                out _, out int size, out UniformType type, out string name);
            int loc = gl.GetUniformLocation(prog, name);
            Console.WriteLine($"         [{i}] name='{name}' location={loc} type={type} size={size}");
        }
    }

    /// <summary>
    /// Prints vertex count, triangle count, and the magnitude range of the
    /// position attribute. For a tessellated unit sphere / unit ellipsoid,
    /// every position must be on the surface, so min ≈ max ≈ 1. Any deviation
    /// points straight at a tessellator bug (missing projection, bad index,
    /// etc.).
    /// </summary>
    private static void DumpMeshDiagnostics(Mesh mesh)
    {
        VertexAttribute? posAttr = null;
        foreach (VertexAttribute a in mesh.Attributes.All)
            if (a.Name == "position") { posAttr = a; break; }

        int indexCount = mesh.Indices switch
        {
            IndicesUnsignedInt u => u.Values.Count,
            _ => 0
        };

        if (posAttr is VertexAttributeDoubleVector3 p)
        {
            double minMag = double.PositiveInfinity, maxMag = 0;
            foreach (Vector3D v in p.Values)
            {
                double m = v.Magnitude;
                if (m < minMag) minMag = m;
                if (m > maxMag) maxMag = m;
            }
            Console.WriteLine(
                $"[mesh] vertices={p.Values.Count}  triangles={indexCount / 3}  " +
                $"|position| in [{minMag:F4}, {maxMag:F4}]");

            int show = Math.Min(4, p.Values.Count);
            for (int i = 0; i < show; i++)
                Console.WriteLine($"       v{i} = {p.Values[i]}  |v|={p.Values[i].Magnitude:F4}");
        }
        else
        {
            Console.WriteLine($"[mesh] no 'position' EmulatedDoubleVector3 attribute found!");
        }
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
            "(1=Sphere 2=CubeMap  W=wire  C=cull  R=rotate  +/-=subdiv  Esc=quit)");
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

    // Back to the standard shader -- now that we know hardcoded MVP works,
    // the bug is in how the auto-uniform uploads the matrix. We're going to
    // override that upload manually in OnRender.
    private const string VertexShaderSource = @"#version 460 core
layout(location = 0) in vec3 positionHigh;
layout(location = 1) in vec3 positionLow;

uniform mat4 geode_modelViewPerspectiveMatrix;

out vec3 vDirection;

void main()
{
    vec3 p = positionHigh + positionLow;
    vDirection = normalize(p);
    gl_Position = geode_modelViewPerspectiveMatrix * vec4(p, 1.0);
}
";

    // |direction| gives a pastel palette that highlights the underlying
    // lattice -- useful for spotting tessellation seams and pole pinches.
    private const string FragmentShaderSource = @"#version 460 core
in vec3 vDirection;
layout(location = 0) out vec4 fragmentColor;

void main()
{
    vec3 c = 0.5 + 0.5 * vDirection;
    fragmentColor = vec4(c, 1.0);
}
";
}
