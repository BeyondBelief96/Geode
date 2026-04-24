using Geode.Core.Geometry;
using Geode.Rendering;
using Geode.Rendering.Buffers;
using Geode.Rendering.Shaders;
using Geode.Rendering.State;
using Geode.Rendering.Textures;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System;
using System.IO;
using System.Numerics;
using PrimitiveType = Geode.Core.Geometry.PrimitiveType;

namespace Geode.App.Examples;

/// <summary>
/// Textured version of the Book §3.8 triangle demo.
/// </summary>
public sealed class Chapter03Triangle : IDisposable
{
    private const string TexturePath = "data/textures/geode-test.tga";

    // Book Listing 3.30 -- the canonical field set for a renderer example.
    private readonly GraphicsWindow _window;
    private readonly SceneState _sceneState = new();
    private readonly ClearState _clearState = new()
    {
        // Dark blue-grey clear so a black triangle-fail is distinguishable
        // from a failed-to-run window.
        Color = new Vector4(0.1f, 0.1f, 0.15f, 1f)
    };
    private DrawState? _drawState;

    // GPU resources we own and must dispose when the window closes.
    private VertexArrayObject? _vertexArray;
    private ShaderProgram? _shaderProgram;
    private Texture2D? _texture;
    private TextureSampler? _sampler;

    public Chapter03Triangle()
    {
        _window = Device.CreateWindow(1280, 720, "Geode - Chapter 3 Textured Triangle");

        _window.Window.Load += OnLoad;
        _window.Window.Render += OnRender;
        _window.Window.Resize += OnResize;
        _window.Window.Closing += OnClose;
    }

    /// <summary>Block and run the window's event loop until it closes.</summary>
    public void Run() => _window.Run();

    private void OnLoad()
    {
        Device device = _window.Device;
        RenderContext context = _window.Context;

        IInputContext input = _window.Window.CreateInput();
        foreach (IKeyboard keyboard in input.Keyboards)
            keyboard.KeyDown += OnKeyDown;

        // --- Texture + sampler --------------------------------------------
        EnsureTestTextureExists(TexturePath);

        _texture = device.CreateTexture2DFromFile(
            TexturePath,
            TextureFormat.RedGreenBlueAlpha8,
            generateMipmaps: true);

        _sampler = device.CreateTextureSampler(new TextureSamplerDescription(
            TextureMinificationFilter.LinearMipmapLinear,
            TextureMagnificationFilter.Linear,
            TextureWrap.Repeat,
            TextureWrap.Repeat,
            MaximumAnisotropy: 1.0f));

        context.TextureUnits[0].Texture = _texture;
        context.TextureUnits[0].TextureSampler = _sampler;

        // --- Shader --------------------------------------------------------
        _shaderProgram = device.CreateShaderProgram(VertexShaderSource, FragmentShaderSource);

        // Tell the sampler uniform which texture unit to sample from. Value
        // lands on the GPU during the first Draw's uniform-flush pass.
        _shaderProgram.SetInt("colorTexture", 0);

        // --- Mesh ----------------------------------------------------------
        // Position and textureCoordinate attribute NAMES must match the
        // `in` variables in the vertex shader -- that is how the Mesh -> VAO
        // bridge pairs CPU data with GPU attribute locations.
        Mesh mesh = new Mesh { PrimitiveType = PrimitiveType.Triangles };

        var positions = new VertexAttributeFloatVector3("position", capacity: 3);
        positions.Values.Add(new Vector3(-0.5f, -0.5f, 0f));
        positions.Values.Add(new Vector3( 0.5f, -0.5f, 0f));
        positions.Values.Add(new Vector3( 0.0f,  0.5f, 0f));
        mesh.Attributes.Add(positions);

        var uvs = new VertexAttributeHalfFloatVector2("textureCoordinate", capacity: 3);
        uvs.Values.Add(new Core.Vector2H(0.0f, 0.0f));
        uvs.Values.Add(new Core.Vector2H(1.0f, 0.0f));
        uvs.Values.Add(new Core.Vector2H(0.5f, 1.0f));
        mesh.Attributes.Add(uvs);

        var indices = new IndicesUnsignedInt(capacity: 3);
        indices.Values.Add(0);
        indices.Values.Add(1);
        indices.Values.Add(2);
        mesh.Indices = indices;

        // Bridge: pack the mesh into an interleaved VertexBuffer, build an
        // IndexBuffer, and wire attribute formats to shader locations by name.
        _vertexArray = context.CreateVertexArray(mesh, _shaderProgram, BufferHint.StaticDraw);

        // --- Draw bundle ---------------------------------------------------
        _drawState = new DrawState(new RenderState(), _shaderProgram, _vertexArray);

        _sceneState.Viewport = new Vector4(0, 0,
            _window.Window.Size.X, _window.Window.Size.Y);

        // --- Capability readout -------------------------------------------
        GL gl = _window.Gl;
        Console.WriteLine($"OpenGL {gl.GetStringS(StringName.Version)}");
        Console.WriteLine($"GPU:   {gl.GetStringS(StringName.Renderer)}");
        Console.WriteLine($"Texture loaded from: {Path.GetFullPath(TexturePath)}");
    }

    private void OnRender(double deltaTime)
    {
        RenderContext context = _window.Context;
        context.Clear(_clearState);
        context.Draw(PrimitiveType.Triangles, _drawState!, _sceneState);
    }

    private void OnResize(Vector2D<int> size)
    {
        _window.Gl.Viewport(0, 0, (uint)size.X, (uint)size.Y);
        _sceneState.Viewport = new Vector4(0, 0, size.X, size.Y);
    }

    private void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        if (key == Key.Escape)
            _window.Window.Close();
    }

    private void OnClose()
    {
        _vertexArray?.Dispose();
        _shaderProgram?.Dispose();
        _sampler?.Dispose();
        _texture?.Dispose();
    }

    public void Dispose()
    {
        _vertexArray?.Dispose();
        _shaderProgram?.Dispose();
        _sampler?.Dispose();
        _texture?.Dispose();
        _window.Dispose();
    }

    // -----------------------------------------------------------------------
    // Auto-generate a test texture if none exists. Writes an uncompressed
    // 24-bit TGA -- stb_image reads TGA the same as PNG so the rest of the
    // code path is unchanged. 64x64 magenta/white checkerboard in 8x8 cells.
    // Replace this file with any PNG/JPG/TGA to try a different image.
    // -----------------------------------------------------------------------

    private static void EnsureTestTextureExists(string path)
    {
        if (File.Exists(path)) return;

        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        const int size = 64;
        const int cell = 8;

        byte[] data = new byte[18 + size * size * 3];

        // TGA header (18 bytes). All fields we don't touch are already zero.
        data[2] = 2;                             // image type: uncompressed true-color
        data[12] = (byte)(size & 0xFF);          // width LSB
        data[13] = (byte)((size >> 8) & 0xFF);   // width MSB
        data[14] = (byte)(size & 0xFF);          // height LSB
        data[15] = (byte)((size >> 8) & 0xFF);   // height MSB
        data[16] = 24;                           // bits per pixel
        data[17] = 0;                            // image descriptor: bottom-up, no alpha bits

        // Pixel data is BGR, row-major, bottom-up.
        int offset = 18;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool magentaCell = ((x / cell) + (y / cell)) % 2 == 0;
                if (magentaCell)
                {
                    data[offset++] = 255;  // B
                    data[offset++] = 0;    // G
                    data[offset++] = 255;  // R
                }
                else
                {
                    data[offset++] = 255;
                    data[offset++] = 255;
                    data[offset++] = 255;
                }
            }
        }

        File.WriteAllBytes(path, data);
    }

    // -----------------------------------------------------------------------
    // Shaders. Book Listing 3.31 pattern: vertex passes UV through, fragment
    // samples a 2D texture bound to unit 0.
    // -----------------------------------------------------------------------

    private const string VertexShaderSource = @"#version 460 core
layout(location = 0) in vec3 position;
layout(location = 1) in vec2 textureCoordinate;

out vec2 fragmentTextureCoordinate;

void main()
{
    gl_Position = vec4(position, 1.0);
    fragmentTextureCoordinate = textureCoordinate;
}
";

    private const string FragmentShaderSource = @"#version 460 core
in vec2 fragmentTextureCoordinate;

uniform sampler2D colorTexture;

layout(location = 0) out vec4 fragmentColor;

void main()
{
    fragmentColor = texture(colorTexture, fragmentTextureCoordinate);
}
";
}
