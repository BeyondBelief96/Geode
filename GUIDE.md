# Geode - Complete Build Guide

**Based on:** *3D Engine Design for Virtual Globes* by Patrick Cozzi & Kevin Ring (2011)
**Reference code:** [OpenGlobe](https://github.com/virtualglobebook/OpenGlobe) (C#)
**Stack:** C# / .NET 9, Silk.NET (OpenGL 3.3 Core Profile)

---

## How to Use This Guide

Each step explains **what** you're building, **why** the book says to do it that way, and gives you the complete C# reference code. Build each file yourself, verify it compiles, then move on.

Since the original OpenGlobe was written in C#, the core math ports almost 1:1. The main difference is we use Silk.NET instead of OpenTK for the OpenGL bindings.

---

## Table of Contents

**Part I -- Foundation**
1. [Project Architecture](#1-project-architecture)
2. [OpenGL Fundamentals](#2-opengl-fundamentals)
3. [Step 1: Window, Context, and the Render Loop](#3-step-1-window-context-and-the-render-loop)
4. [Step 2: The Shader Pipeline](#4-step-2-the-shader-pipeline)
5. [Step 3: Vertex Buffers and Drawing Triangles](#5-step-3-vertex-buffers-and-drawing-triangles)

**Part II -- Globe Math (Book Chapter 2)**
6. [Step 4: Core Math Types](#6-step-4-core-math-types)
7. [Step 5: The Ellipsoid Class](#7-step-5-the-ellipsoid-class)
8. [Step 6: Coordinate Transformations](#8-step-6-coordinate-transformations)

**Part III -- Rendering a Globe (Book Chapter 4)**
9. [Step 7: Globe Tessellation](#9-step-7-globe-tessellation)
10. [Step 8: Camera System](#10-step-8-camera-system)
11. [Step 9: Phong Shading](#11-step-9-phong-shading)
12. [Step 10: Texture Mapping the Earth](#12-step-10-texture-mapping-the-earth)
13. [Step 11: Correct Ellipsoid Normals in Shaders](#13-step-11-correct-ellipsoid-normals-in-shaders)

**Part IV -- Interaction and Precision (Book Chapters 4-6)**
14. [Step 12: Ray-Ellipsoid Intersection (Picking)](#14-step-12-ray-ellipsoid-intersection-picking)
15. [Step 13: Curves on the Ellipsoid Surface](#15-step-13-curves-on-the-ellipsoid-surface)
16. [Step 14: Fixing Vertex Jitter (Precision)](#16-step-14-fixing-vertex-jitter-precision)
17. [Step 15: Fixing Depth Buffer Precision](#17-step-15-fixing-depth-buffer-precision)

**Appendices**
- [A: Build Instructions](#a-build-instructions)
- [B: OpenGlobe to This Project Translation Table](#b-openglobe-to-this-project-translation-table)
- [C: Silk.NET vs OpenTK Quick Reference](#c-silknet-vs-opentk-quick-reference)

---

# Part I -- Foundation

## 1. Project Architecture

The book (Section 1.3) organizes OpenGlobe into three assemblies: **Core**, **Renderer**, and **Scene**. We mirror this exactly:

```
Geode/
├── Geode.slnx                        # Solution
├── Directory.Build.props              # Shared NuGet metadata and build settings
├── .gitignore
├── GUIDE.md
│
├── Geode.Core/                        # Pure math -- no GPU, no Silk.NET
│   ├── Vector3D.cs                    # Double-precision 3D vector
│   ├── Geodetic2D.cs                  # Longitude + latitude
│   ├── Geodetic3D.cs                  # Longitude + latitude + height
│   ├── Ellipsoid.cs                   # WGS84 model, coordinate transforms
│   └── Trig.cs                        # Degree/radian utilities
│
├── Geode.Rendering/                   # Silk.NET OpenGL wrappers
│   ├── ShaderProgram.cs               # Compile & link GLSL shaders
│   ├── VertexArrayObject.cs           # VAO + VBO + EBO
│   ├── BufferObject.cs                # Typed GPU buffer
│   └── Texture2D.cs                   # 2D texture loading
│
├── Geode.Visualization/              # High-level geographic visualization
│   ├── Camera.cs                      # Orbit camera
│   └── Globe.cs                       # Tessellation + draw calls
│
├── Geode.App/                         # Demo application (not published to NuGet)
│   └── Program.cs                     # Entry point, window, render loop
│
├── shaders/
│   ├── globe.vert
│   └── globe.frag
│
└── data/
    └── textures/                      # Earth imagery
```

### Dependency chain (book Figure 1.5)

```
Geode.App
    ↓
Geode.Visualization  →  Geode.Rendering  →  Geode.Core
                              ↓
                         Silk.NET (NuGet)
```

### Design rules

1. **`Core` never references Silk.NET.** It's pure math. You could use it in a web API or CLI tool with no GPU.
2. **`Renderer` wraps Silk.NET's OpenGL** behind `IDisposable` types. GPU resources are freed deterministically.
3. **`Visualization` uses `Rendering` and `Core`** to implement high-level objects like a textured, lit globe.
4. **`App` is glue** -- window creation, input wiring, render loop. Stays short.

---

## 2. OpenGL Fundamentals

### The GPU pipeline

```
Your C# code
    │
    ▼
┌─────────────┐
│ Vertex       │  ← Runs once per vertex. Transforms 3D → screen space.
│ Shader       │
└─────┬───────┘
      │
┌─────▼───────┐
│ Rasterizer   │  ← Hardware. Determines which pixels each triangle covers.
└─────┬───────┘
      │
┌─────▼───────┐
│ Fragment     │  ← Runs once per pixel. Computes the color.
│ Shader       │
└─────┬───────┘
      │
      ▼
   Screen
```

### Five core OpenGL objects

| Object | What it is | Silk.NET creation |
|--------|-----------|-------------------|
| **VBO** (Vertex Buffer) | Vertex data on the GPU | `_gl.GenBuffer()` |
| **EBO** (Index Buffer) | Triangle indices | `_gl.GenBuffer()` |
| **VAO** (Vertex Array) | Describes vertex layout | `_gl.GenVertexArray()` |
| **Shader Program** | Linked vertex + fragment shaders | `_gl.CreateProgram()` |
| **Texture** | 2D image on the GPU | `_gl.GenTexture()` |

### Silk.NET vs raw OpenGL

Silk.NET provides a **thin, type-safe wrapper** over OpenGL. The function names are identical to the C API but use C# conventions:

| C OpenGL | Silk.NET |
|----------|----------|
| `glGenBuffers(1, &vbo)` | `uint vbo = _gl.GenBuffer()` |
| `glBindBuffer(GL_ARRAY_BUFFER, vbo)` | `_gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo)` |
| `glBufferData(...)` | `_gl.BufferData(...)` |
| `glUniformMatrix4fv(loc, 1, false, &m)` | `_gl.UniformMatrix4(loc, 1, false, m)` |

The `GL` object (`Silk.NET.OpenGL.GL`) is your handle to all OpenGL functions. You get it once on window load and pass it to your renderer classes.

### GLSL basics

GLSL looks like C with vectors built in:

```glsl
#version 330 core
layout(location = 0) in vec3 position;   // vertex input
uniform mat4 u_mvp;                       // set from C# each frame

void main() {
    gl_Position = u_mvp * vec4(position, 1.0);  // transform to clip space
}
```

---

## 3. Step 1: Window, Context, and the Render Loop

**Book reference:** Chapter 3 - Renderer Design
**Goal:** Open a window with an OpenGL 3.3 Core context and run a clear-screen loop.

### Silk.NET windowing

Silk.NET replaces both GLFW (windowing) and GLEW (GL loading) with a single unified API. You create a window with `Window.Create(options)`, and Silk.NET:
- Creates the native window (GLFW under the hood)
- Creates the OpenGL context
- Loads all GL function pointers
- Gives you an event-driven API (`Load`, `Render`, `Update`, `Resize`)

### Code

```csharp
// Geode.App/Program.cs
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
            Title = "Virtual Globe",
            API = new GraphicsAPI(
                ContextAPI.OpenGL, ContextProfile.Core,
                ContextFlags.Default, new APIVersion(3, 3))
        };

        _window = Window.Create(options);

        // Silk.NET uses an event-driven model instead of a manual while-loop.
        // The window calls these callbacks at the right time.
        _window.Load    += OnLoad;
        _window.Update  += OnUpdate;   // called once per frame for logic
        _window.Render  += OnRender;   // called once per frame for drawing
        _window.Closing += OnClose;
        _window.Resize  += OnResize;

        // This blocks until the window is closed.
        // Internally it runs the event loop: poll input → update → render → swap.
        _window.Run();
    }

    private static void OnLoad()
    {
        // Get the GL API object -- this is your handle to ALL OpenGL functions.
        // Equivalent to glewInit() + glfwMakeContextCurrent() in C.
        _gl = GL.GetApi(_window!);

        // Register keyboard input
        var input = _window!.CreateInput();
        foreach (var keyboard in input.Keyboards)
            keyboard.KeyDown += OnKeyDown;

        // OpenGL state (book Section 3.3 - State Management)
        _gl.Enable(EnableCap.DepthTest);      // closer fragments win
        _gl.Enable(EnableCap.CullFace);        // don't draw back faces
        _gl.CullFace(TriangleFace.Back);
        _gl.FrontFace(FrontFaceDirection.Ccw); // CCW winding = front
        _gl.ClearColor(0f, 0f, 0f, 1f);       // black background (space)

        Console.WriteLine($"OpenGL {_gl.GetStringS(StringName.Version)}");
        Console.WriteLine($"GPU:   {_gl.GetStringS(StringName.Renderer)}");
    }

    private static void OnUpdate(double dt)
    {
        // Game logic, camera updates, etc.
    }

    private static void OnRender(double dt)
    {
        _gl!.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Drawing goes here
    }

    private static void OnResize(Vector2D<int> size)
    {
        _gl?.Viewport(0, 0, (uint)size.X, (uint)size.Y);
    }

    private static void OnKeyDown(IKeyboard kb, Key key, int scancode)
    {
        if (key == Key.Escape) _window?.Close();
    }

    private static void OnClose()
    {
        _gl?.Dispose();
    }
}
```

### What you should see

A black window titled "Virtual Globe" that closes on Escape. Console prints your GL version and GPU.

---

## 4. Step 2: The Shader Pipeline

**Book reference:** Chapter 3, Section 3.4
**Goal:** A reusable class that compiles GLSL shaders and provides uniform setters.

### Code

```csharp
// Geode.Rendering/ShaderProgram.cs
using Silk.NET.OpenGL;
using System;
using System.IO;
using System.Numerics;

namespace Geode.Rendering;

public class ShaderProgram : IDisposable
{
    private readonly GL _gl;
    private readonly uint _handle;

    public uint Handle => _handle;

    public ShaderProgram(GL gl, string vertexSource, string fragmentSource)
    {
        _gl = gl;

        uint vert = CompileShader(ShaderType.VertexShader, vertexSource);
        uint frag = CompileShader(ShaderType.FragmentShader, fragmentSource);

        _handle = _gl.CreateProgram();
        _gl.AttachShader(_handle, vert);
        _gl.AttachShader(_handle, frag);
        _gl.LinkProgram(_handle);

        _gl.GetProgram(_handle, ProgramPropertyARB.LinkStatus, out int status);
        if (status == 0)
            throw new Exception($"Shader link error: {_gl.GetProgramInfoLog(_handle)}");

        // Shaders are baked into the program now -- delete the intermediates
        _gl.DetachShader(_handle, vert);
        _gl.DetachShader(_handle, frag);
        _gl.DeleteShader(vert);
        _gl.DeleteShader(frag);
    }

    /// <summary>Load shaders from file paths.</summary>
    public static ShaderProgram FromFiles(GL gl, string vertPath, string fragPath)
    {
        string vertSrc = File.ReadAllText(vertPath);
        string fragSrc = File.ReadAllText(fragPath);
        return new ShaderProgram(gl, vertSrc, fragSrc);
    }

    public void Use() => _gl.UseProgram(_handle);

    // ── Uniform setters ─────────────────────────────────
    // These correspond to OpenGlobe's "automatic uniforms" (Section 3.4.5)
    // but are set manually.

    public void SetInt(string name, int value)
        => _gl.Uniform1(GetLocation(name), value);

    public void SetFloat(string name, float value)
        => _gl.Uniform1(GetLocation(name), value);

    public void SetVec3(string name, Vector3 value)
        => _gl.Uniform3(GetLocation(name), value.X, value.Y, value.Z);

    public void SetVec4(string name, Vector4 value)
        => _gl.Uniform4(GetLocation(name), value.X, value.Y, value.Z, value.W);

    public unsafe void SetMat4(string name, Matrix4x4 value)
        => _gl.UniformMatrix4(GetLocation(name), 1, false, (float*)&value);

    private int GetLocation(string name)
    {
        int loc = _gl.GetUniformLocation(_handle, name);
        // -1 means the uniform doesn't exist or was optimized out.
        // Silently ignore -- this matches OpenGlobe's behavior.
        return loc;
    }

    private uint CompileShader(ShaderType type, string source)
    {
        uint shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);

        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
        if (status == 0)
        {
            string log = _gl.GetShaderInfoLog(shader);
            _gl.DeleteShader(shader);
            throw new Exception($"{type} compile error:\n{log}");
        }
        return shader;
    }

    public void Dispose()
    {
        _gl.DeleteProgram(_handle);
    }
}
```

---

## 5. Step 3: Vertex Buffers and Drawing Triangles

**Book reference:** Chapter 3, Section 3.5 - Vertex Data
**Goal:** Reusable buffer and VAO wrappers, then draw a test triangle.

### Code: BufferObject

```csharp
// Geode.Rendering/BufferObject.cs
using Silk.NET.OpenGL;
using System;

namespace Geode.Rendering;

/// <summary>
/// A typed GPU buffer (VBO or EBO).
/// </summary>
public class BufferObject<T> : IDisposable where T : unmanaged
{
    private readonly GL _gl;
    private readonly uint _handle;
    private readonly BufferTargetARB _type;

    public uint Handle => _handle;

    public unsafe BufferObject(GL gl, ReadOnlySpan<T> data, BufferTargetARB type)
    {
        _gl = gl;
        _type = type;
        _handle = _gl.GenBuffer();
        Bind();
        fixed (void* ptr = data)
        {
            _gl.BufferData(_type, (nuint)(data.Length * sizeof(T)), ptr,
                           BufferUsageARB.StaticDraw);
        }
    }

    public void Bind()   => _gl.BindBuffer(_type, _handle);
    public void Unbind() => _gl.BindBuffer(_type, 0);

    public void Dispose() => _gl.DeleteBuffer(_handle);
}
```

### Code: VertexArrayObject

```csharp
// Geode.Rendering/VertexArrayObject.cs
using Silk.NET.OpenGL;
using System;

namespace Geode.Rendering;

/// <summary>
/// Encapsulates a VAO with its associated VBO and EBO.
/// Describes vertex layout and issues draw calls.
/// </summary>
public class VertexArrayObject : IDisposable
{
    private readonly GL _gl;
    private readonly uint _handle;
    private readonly int _indexCount;
    private readonly BufferObject<float> _vbo;
    private readonly BufferObject<uint> _ebo;

    public VertexArrayObject(GL gl,
                             ReadOnlySpan<float> vertices,
                             ReadOnlySpan<uint> indices,
                             params VertexAttrib[] attributes)
    {
        _gl = gl;
        _indexCount = indices.Length;

        _handle = _gl.GenVertexArray();
        _gl.BindVertexArray(_handle);

        _vbo = new BufferObject<float>(gl, vertices, BufferTargetARB.ArrayBuffer);
        _ebo = new BufferObject<uint>(gl, indices, BufferTargetARB.ElementArrayBuffer);

        // Compute stride from attributes
        uint stride = 0;
        foreach (var attr in attributes)
            stride += (uint)(attr.Components * sizeof(float));

        // Configure each attribute
        uint offset = 0;
        foreach (var attr in attributes)
        {
            _gl.VertexAttribPointer(attr.Index, attr.Components,
                                    VertexAttribPointerType.Float,
                                    false, stride, (void*)(nuint)offset);
            _gl.EnableVertexAttribArray(attr.Index);
            offset += (uint)(attr.Components * sizeof(float));
        }

        _gl.BindVertexArray(0);
    }

    public void Draw()
    {
        _gl.BindVertexArray(_handle);
        unsafe
        {
            _gl.DrawElements(PrimitiveType.Triangles, (uint)_indexCount,
                             DrawElementsType.UnsignedInt, null);
        }
    }

    public void Dispose()
    {
        _vbo.Dispose();
        _ebo.Dispose();
        _gl.DeleteVertexArray(_handle);
    }
}

/// <summary>Describes one vertex attribute.</summary>
public readonly record struct VertexAttrib(uint Index, int Components);
```

### Test: Draw a triangle

Create these test shaders:

```glsl
// shaders/test.vert
#version 330 core
layout(location = 0) in vec3 position;
void main() {
    gl_Position = vec4(position, 1.0);
}
```

```glsl
// shaders/test.frag
#version 330 core
out vec4 fragColor;
void main() {
    fragColor = vec4(0.2, 0.6, 1.0, 1.0);
}
```

Then in your `OnLoad` and `OnRender`:

```csharp
private static ShaderProgram? _shader;
private static VertexArrayObject? _triangle;

private static void OnLoad()
{
    _gl = GL.GetApi(_window!);
    // ... GL state setup ...

    _shader = ShaderProgram.FromFiles(_gl, "shaders/test.vert", "shaders/test.frag");

    float[] verts = {
        -0.5f, -0.5f, 0f,
         0.5f, -0.5f, 0f,
         0.0f,  0.5f, 0f
    };
    uint[] indices = { 0, 1, 2 };

    _triangle = new VertexArrayObject(_gl, verts, indices,
        new VertexAttrib(0, 3)  // location 0, 3 floats (position)
    );
}

private static void OnRender(double dt)
{
    _gl!.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    _shader!.Use();
    _triangle!.Draw();
}
```

You should see a blue triangle on a black background.

---

# Part II -- Globe Math

## 6. Step 4: Core Math Types

**Book reference:** Chapter 2, Section 2.1
**OpenGlobe reference:** `Vector3D.cs`, `Geodetic2D.cs`, `Geodetic3D.cs`

### Why doubles?

The book (Section 2.1.2) explains: WGS84 coordinates are in meters. Earth's radius is ~6,378,137 m. A 32-bit float has ~7 significant digits -- that only resolves to about 1 meter at Earth scale. A 64-bit double has ~15 digits, giving sub-millimeter precision.

All CPU-side math uses doubles. The GPU uses floats. The trick (Steps 14-15) is to subtract the camera position in double precision on the CPU before uploading small residuals as floats.

### Code: Vector3D

This is a near-direct port of OpenGlobe's `Vector3D.cs`:

```csharp
// Geode.Core/Vector3D.cs
using System;

namespace Geode.Core;

/// <summary>
/// Double-precision 3D vector. Matches OpenGlobe's Vector3D.
/// Uses doubles because WGS84 coordinates are in meters at planetary scale.
/// </summary>
public readonly struct Vector3D : IEquatable<Vector3D>
{
    public readonly double X;
    public readonly double Y;
    public readonly double Z;

    public Vector3D(double x, double y, double z) { X = x; Y = y; Z = z; }

    public static readonly Vector3D Zero  = new(0, 0, 0);
    public static readonly Vector3D UnitX = new(1, 0, 0);
    public static readonly Vector3D UnitY = new(0, 1, 0);
    public static readonly Vector3D UnitZ = new(0, 0, 1);

    public double MagnitudeSquared => X * X + Y * Y + Z * Z;
    public double Magnitude => Math.Sqrt(MagnitudeSquared);

    public Vector3D Normalize()
    {
        double m = Magnitude;
        return new Vector3D(X / m, Y / m, Z / m);
    }

    public double Dot(Vector3D other) => X * other.X + Y * other.Y + Z * other.Z;

    public Vector3D Cross(Vector3D other) => new(
        Y * other.Z - Z * other.Y,
        Z * other.X - X * other.Z,
        X * other.Y - Y * other.X);

    /// <summary>
    /// Component-wise multiplication. NOT the dot product.
    /// result.X = this.X * scale.X, etc.
    /// Used throughout Ellipsoid because each axis has a different radius.
    /// Matches OpenGlobe's Vector3D.MultiplyComponents.
    /// </summary>
    public Vector3D MultiplyComponents(Vector3D scale) =>
        new(X * scale.X, Y * scale.Y, Z * scale.Z);

    public double AngleBetween(Vector3D other) =>
        Math.Acos(Math.Clamp(Normalize().Dot(other.Normalize()), -1.0, 1.0));

    /// <summary>
    /// Rotate this vector around an axis by theta radians (Rodrigues' formula).
    /// Used for computing curves on the ellipsoid (book Section 2.4).
    /// Ported from OpenGlobe's Vector3D.RotateAroundAxis.
    /// </summary>
    public Vector3D RotateAroundAxis(Vector3D axis, double theta)
    {
        double u = axis.X, v = axis.Y, w = axis.Z;
        double cosT = Math.Cos(theta);
        double sinT = Math.Sin(theta);
        double ms = axis.MagnitudeSquared;
        double m = Math.Sqrt(ms);

        return new Vector3D(
            ((u * (u * X + v * Y + w * Z)) +
             (((X * (v * v + w * w)) - (u * (v * Y + w * Z))) * cosT) +
             (m * ((-w * Y) + (v * Z)) * sinT)) / ms,

            ((v * (u * X + v * Y + w * Z)) +
             (((Y * (u * u + w * w)) - (v * (u * X + w * Z))) * cosT) +
             (m * ((w * X) - (u * Z)) * sinT)) / ms,

            ((w * (u * X + v * Y + w * Z)) +
             (((Z * (u * u + v * v)) - (w * (u * X + v * Y))) * cosT) +
             (m * (-(v * X) + (u * Y)) * sinT)) / ms);
    }

    // Operators
    public static Vector3D operator +(Vector3D a, Vector3D b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3D operator -(Vector3D a, Vector3D b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vector3D operator *(Vector3D v, double s)   => new(v.X * s, v.Y * s, v.Z * s);
    public static Vector3D operator *(double s, Vector3D v)   => v * s;
    public static Vector3D operator /(Vector3D v, double s)   => new(v.X / s, v.Y / s, v.Z / s);
    public static Vector3D operator -(Vector3D v)             => new(-v.X, -v.Y, -v.Z);
    public static bool operator ==(Vector3D a, Vector3D b)    => a.Equals(b);
    public static bool operator !=(Vector3D a, Vector3D b)    => !a.Equals(b);

    public bool Equals(Vector3D other) => X == other.X && Y == other.Y && Z == other.Z;
    public override bool Equals(object? obj) => obj is Vector3D v && Equals(v);
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    public override string ToString() => $"({X}, {Y}, {Z})";
}
```

### Code: Geodetic2D and Geodetic3D

```csharp
// Geode.Core/Geodetic2D.cs
using System;

namespace Geode.Core;

/// <summary>
/// A position on the ellipsoid surface: longitude + latitude in radians.
/// Matches OpenGlobe's Geodetic2D.
/// </summary>
public readonly struct Geodetic2D : IEquatable<Geodetic2D>
{
    public readonly double Longitude; // radians, [-pi, pi]
    public readonly double Latitude;  // radians, [-pi/2, pi/2]

    public Geodetic2D(double longitude, double latitude)
    {
        Longitude = longitude;
        Latitude = latitude;
    }

    public bool Equals(Geodetic2D other) =>
        Longitude == other.Longitude && Latitude == other.Latitude;
    public override bool Equals(object? obj) => obj is Geodetic2D g && Equals(g);
    public override int GetHashCode() => HashCode.Combine(Longitude, Latitude);

    public static bool operator ==(Geodetic2D a, Geodetic2D b) => a.Equals(b);
    public static bool operator !=(Geodetic2D a, Geodetic2D b) => !a.Equals(b);
}
```

```csharp
// Geode.Core/Geodetic3D.cs
using System;

namespace Geode.Core;

/// <summary>
/// A position relative to the ellipsoid: longitude + latitude + height.
/// Height is in meters above (positive) or below (negative) the surface.
/// Matches OpenGlobe's Geodetic3D.
/// </summary>
public readonly struct Geodetic3D : IEquatable<Geodetic3D>
{
    public readonly double Longitude; // radians
    public readonly double Latitude;  // radians
    public readonly double Height;    // meters

    public Geodetic3D(double longitude, double latitude, double height = 0.0)
    {
        Longitude = longitude;
        Latitude = latitude;
        Height = height;
    }

    public Geodetic3D(Geodetic2D g, double height = 0.0)
        : this(g.Longitude, g.Latitude, height) { }

    public bool Equals(Geodetic3D other) =>
        Longitude == other.Longitude && Latitude == other.Latitude && Height == other.Height;
    public override bool Equals(object? obj) => obj is Geodetic3D g && Equals(g);
    public override int GetHashCode() => HashCode.Combine(Longitude, Latitude, Height);

    public static bool operator ==(Geodetic3D a, Geodetic3D b) => a.Equals(b);
    public static bool operator !=(Geodetic3D a, Geodetic3D b) => !a.Equals(b);
}
```

### Code: Trig

```csharp
// Geode.Core/Trig.cs
using System;

namespace Geode.Core;

/// <summary>
/// Degree/radian conversion utilities. Matches OpenGlobe's Trig class.
/// </summary>
public static class Trig
{
    public const double TwoPi  = 2.0 * Math.PI;
    public const double HalfPi = Math.PI / 2.0;

    public static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
    public static double ToDegrees(double radians) => radians * 180.0 / Math.PI;
}
```

---

## 7. Step 5: The Ellipsoid Class

**Book reference:** Chapter 2, Sections 2.2-2.4
**OpenGlobe reference:** `Source/Core/Geometry/Ellipsoid.cs`

This is the heart of the engine. The code below is very close to the original OpenGlobe C# -- the book was written with this exact code.

### Code

```csharp
// Geode.Core/Ellipsoid.cs
using System;
using System.Collections.Generic;

namespace Geode.Core;

/// <summary>
/// An ellipsoid defined by three radii (a, b, c) centered at the origin.
/// Provides surface normals, coordinate transforms, ray intersection,
/// and curve computation. Matches OpenGlobe's Ellipsoid class.
/// </summary>
public class Ellipsoid
{
    // ── Standard ellipsoids ─────────────────────────────
    public static readonly Ellipsoid Wgs84 = new(6378137.0, 6378137.0, 6356752.314245);
    public static readonly Ellipsoid ScaledWgs84 = new(1.0, 1.0, 6356752.314245 / 6378137.0);
    public static readonly Ellipsoid UnitSphere = new(1.0, 1.0, 1.0);

    // ── Precomputed values (book Section 2.2) ───────────
    private readonly Vector3D _radii;
    private readonly Vector3D _radiiSquared;
    private readonly Vector3D _radiiToTheFourth;
    private readonly Vector3D _oneOverRadiiSquared;

    public Vector3D Radii => _radii;
    public Vector3D RadiiSquared => _radiiSquared;
    public Vector3D OneOverRadiiSquared => _oneOverRadiiSquared;

    public Ellipsoid(double x, double y, double z) : this(new Vector3D(x, y, z)) { }

    public Ellipsoid(Vector3D radii)
    {
        _radii = radii;
        _radiiSquared = new Vector3D(
            radii.X * radii.X,
            radii.Y * radii.Y,
            radii.Z * radii.Z);
        _radiiToTheFourth = new Vector3D(
            _radiiSquared.X * _radiiSquared.X,
            _radiiSquared.Y * _radiiSquared.Y,
            _radiiSquared.Z * _radiiSquared.Z);
        _oneOverRadiiSquared = new Vector3D(
            1.0 / _radiiSquared.X,
            1.0 / _radiiSquared.Y,
            1.0 / _radiiSquared.Z);
    }

    public double MinimumRadius => Math.Min(_radii.X, Math.Min(_radii.Y, _radii.Z));
    public double MaximumRadius => Math.Max(_radii.X, Math.Max(_radii.Y, _radii.Z));

    // ═════════════════════════════════════════════════════
    //  Surface Normals (Book Section 2.2.2)
    // ═════════════════════════════════════════════════════

    /// <summary>Geocentric normal: just normalize the position. Only exact for spheres.</summary>
    public static Vector3D CentricSurfaceNormal(Vector3D positionOnEllipsoid) =>
        positionOnEllipsoid.Normalize();

    /// <summary>
    /// Geodetic normal: the true surface normal perpendicular to the tangent plane.
    /// Book Listing 2.4.
    /// </summary>
    public Vector3D GeodeticSurfaceNormal(Vector3D positionOnEllipsoid) =>
        positionOnEllipsoid.MultiplyComponents(_oneOverRadiiSquared).Normalize();

    /// <summary>
    /// Geodetic normal from geographic coordinates (no surface point needed).
    /// Book Equation 2.3.
    /// </summary>
    public Vector3D GeodeticSurfaceNormal(Geodetic3D geodetic)
    {
        double cosLatitude = Math.Cos(geodetic.Latitude);
        return new Vector3D(
            cosLatitude * Math.Cos(geodetic.Longitude),
            cosLatitude * Math.Sin(geodetic.Longitude),
            Math.Sin(geodetic.Latitude));
    }

    // ═════════════════════════════════════════════════════
    //  Geographic → Cartesian (Book Section 2.3.1)
    // ═════════════════════════════════════════════════════

    /// <summary>
    /// Convert geographic coordinates to WGS84 Cartesian. Closed form.
    /// Book Listing 2.6.
    /// </summary>
    public Vector3D ToVector3D(Geodetic3D geodetic)
    {
        Vector3D n = GeodeticSurfaceNormal(geodetic);
        Vector3D k = _radiiSquared.MultiplyComponents(n);
        double gamma = Math.Sqrt(k.X * n.X + k.Y * n.Y + k.Z * n.Z);

        Vector3D rSurface = k / gamma;
        return rSurface + (geodetic.Height * n);
    }

    public Vector3D ToVector3D(Geodetic2D geodetic) =>
        ToVector3D(new Geodetic3D(geodetic.Longitude, geodetic.Latitude, 0.0));

    // ═════════════════════════════════════════════════════
    //  Cartesian → Geographic (Book Section 2.3.2)
    // ═════════════════════════════════════════════════════

    /// <summary>Surface point → lon/lat. Book Listing 2.7.</summary>
    public Geodetic2D ToGeodetic2D(Vector3D positionOnEllipsoid)
    {
        Vector3D n = GeodeticSurfaceNormal(positionOnEllipsoid);
        return new Geodetic2D(
            Math.Atan2(n.Y, n.X),
            Math.Asin(Math.Clamp(n.Z / n.Magnitude, -1.0, 1.0)));
    }

    /// <summary>
    /// Scale a point to the geocentric surface along its position vector.
    /// Book Listing 2.8.
    /// </summary>
    public Vector3D ScaleToGeocentricSurface(Vector3D position)
    {
        double beta = 1.0 / Math.Sqrt(
            (position.X * position.X) * _oneOverRadiiSquared.X +
            (position.Y * position.Y) * _oneOverRadiiSquared.Y +
            (position.Z * position.Z) * _oneOverRadiiSquared.Z);
        return beta * position;
    }

    /// <summary>
    /// Scale a point to the geodetic surface using Newton-Raphson.
    /// Converges in 1-2 iterations for WGS84 (book Table 2.1).
    /// Book Listing 2.9.
    /// </summary>
    public Vector3D ScaleToGeodeticSurface(Vector3D position)
    {
        double beta = 1.0 / Math.Sqrt(
            (position.X * position.X) * _oneOverRadiiSquared.X +
            (position.Y * position.Y) * _oneOverRadiiSquared.Y +
            (position.Z * position.Z) * _oneOverRadiiSquared.Z);
        double n = new Vector3D(
            beta * position.X * _oneOverRadiiSquared.X,
            beta * position.Y * _oneOverRadiiSquared.Y,
            beta * position.Z * _oneOverRadiiSquared.Z).Magnitude;
        double alpha = (1.0 - beta) * (position.Magnitude / n);

        double x2 = position.X * position.X;
        double y2 = position.Y * position.Y;
        double z2 = position.Z * position.Z;

        double da = 0, db = 0, dc = 0;
        double s = 0, dSdA = 1;

        do
        {
            alpha -= (s / dSdA);

            da = 1.0 + (alpha * _oneOverRadiiSquared.X);
            db = 1.0 + (alpha * _oneOverRadiiSquared.Y);
            dc = 1.0 + (alpha * _oneOverRadiiSquared.Z);

            double da2 = da * da, db2 = db * db, dc2 = dc * dc;
            double da3 = da * da2, db3 = db * db2, dc3 = dc * dc2;

            s = x2 / (_radiiSquared.X * da2) +
                y2 / (_radiiSquared.Y * db2) +
                z2 / (_radiiSquared.Z * dc2) - 1.0;

            dSdA = -2.0 * (
                x2 / (_radiiToTheFourth.X * da3) +
                y2 / (_radiiToTheFourth.Y * db3) +
                z2 / (_radiiToTheFourth.Z * dc3));
        }
        while (Math.Abs(s) > 1e-10);

        return new Vector3D(position.X / da, position.Y / db, position.Z / dc);
    }

    /// <summary>
    /// Arbitrary Cartesian point → geographic coordinates.
    /// Book Listing 2.10.
    /// </summary>
    public Geodetic3D ToGeodetic3D(Vector3D position)
    {
        Vector3D p = ScaleToGeodeticSurface(position);
        Vector3D h = position - p;
        double height = Math.Sign(h.Dot(position)) * h.Magnitude;
        return new Geodetic3D(ToGeodetic2D(p), height);
    }

    // ═════════════════════════════════════════════════════
    //  Curves on the Ellipsoid (Book Section 2.4)
    // ═════════════════════════════════════════════════════

    /// <summary>
    /// Compute a curve between two surface points by subsampling.
    /// Book Listing 2.11.
    /// </summary>
    public IList<Vector3D> ComputeCurve(Vector3D start, Vector3D stop, double granularity)
    {
        Vector3D normal = start.Cross(stop).Normalize();
        double theta = start.AngleBetween(stop);
        int n = Math.Max((int)(theta / granularity) - 1, 0);

        var positions = new List<Vector3D>(2 + n);
        positions.Add(start);

        for (int i = 1; i <= n; ++i)
        {
            double phi = i * granularity;
            positions.Add(ScaleToGeocentricSurface(start.RotateAroundAxis(normal, phi)));
        }

        positions.Add(stop);
        return positions;
    }

    // ═════════════════════════════════════════════════════
    //  Ray Intersection (Book Section 4.3)
    // ═════════════════════════════════════════════════════

    /// <summary>
    /// Returns 0, 1, or 2 intersection distances along a ray.
    /// Used for mouse picking.
    /// </summary>
    public double[] Intersections(Vector3D origin, Vector3D direction)
    {
        direction = direction.Normalize();

        double a = direction.X * direction.X * _oneOverRadiiSquared.X +
                   direction.Y * direction.Y * _oneOverRadiiSquared.Y +
                   direction.Z * direction.Z * _oneOverRadiiSquared.Z;
        double b = 2.0 * (
                   origin.X * direction.X * _oneOverRadiiSquared.X +
                   origin.Y * direction.Y * _oneOverRadiiSquared.Y +
                   origin.Z * direction.Z * _oneOverRadiiSquared.Z);
        double c = origin.X * origin.X * _oneOverRadiiSquared.X +
                   origin.Y * origin.Y * _oneOverRadiiSquared.Y +
                   origin.Z * origin.Z * _oneOverRadiiSquared.Z - 1.0;

        double discriminant = b * b - 4 * a * c;

        if (discriminant < 0.0)
            return Array.Empty<double>();
        if (discriminant == 0.0)
            return new[] { -0.5 * b / a };

        double t = -0.5 * (b + (b > 0 ? 1.0 : -1.0) * Math.Sqrt(discriminant));
        double root1 = t / a;
        double root2 = c / t;

        return root1 < root2
            ? new[] { root1, root2 }
            : new[] { root2, root1 };
    }
}
```

### Verify it works

```csharp
// Quick test in Program.cs OnLoad:
var seattle = new Geodetic3D(Trig.ToRadians(-122.3321), Trig.ToRadians(47.6062), 0.0);
var cart = Ellipsoid.Wgs84.ToVector3D(seattle);
Console.WriteLine($"Seattle: {cart}");

var back = Ellipsoid.Wgs84.ToGeodetic3D(cart);
Console.WriteLine($"Round-trip: lon={Trig.ToDegrees(back.Longitude):F4}, " +
                  $"lat={Trig.ToDegrees(back.Latitude):F4}, h={back.Height:F6}");
```

---

## 8. Step 6: Coordinate Transformations

Already implemented in the Ellipsoid class above. Summary:

### Geographic → Cartesian (closed form, Listing 2.6)
```
1. n̂ = (cosφ·cosλ, cosφ·sinλ, sinφ)
2. k = radiiSquared ⊙ n̂
3. γ = √(k · n̂)
4. r_surface = k / γ
5. r = r_surface + h·n̂
```

### Cartesian → Geographic (iterative, Listing 2.10)
```
1. r_surface = ScaleToGeodeticSurface(position)   [Newton-Raphson, ~2 iterations]
2. h = sign((r - r_surface) · r) × |r - r_surface|
3. λ = atan2(n_y, n_x),  φ = asin(n_z / |n|)
```

---

# Part III -- Rendering a Globe

## 9. Step 7: Globe Tessellation

**Book reference:** Chapter 4, Section 4.1
**Goal:** Generate a triangle mesh of the ellipsoid surface.

We use the **geographic grid** algorithm (Section 4.1.4, Listing 4.5) because it maps naturally to equirectangular textures.

### Code

```csharp
// Geode.Visualization/Globe.cs
using Silk.NET.OpenGL;
using Geode.Core;
using Geode.Rendering;
using System;
using System.Collections.Generic;

namespace Geode.Visualization;

public class Globe : IDisposable
{
    private VertexArrayObject? _vao;

    /// <summary>
    /// Generate the globe mesh. Each vertex has:
    /// position (3) + normal (3) + texcoord (2) = 8 floats.
    /// </summary>
    public void Create(GL gl, Ellipsoid ellipsoid, int stacks, int slices)
    {
        var vertices = new List<float>();
        var indices = new List<uint>();

        Vector3D r = ellipsoid.Radii;
        int vertsPerRow = slices + 1;

        // North pole row
        {
            var pos = new Vector3D(0, 0, r.Z);
            Vector3D norm = ellipsoid.GeodeticSurfaceNormal(pos);
            for (int j = 0; j <= slices; j++)
            {
                float u = (float)j / slices;
                AddVertex(vertices, pos, norm, u, 0f);
            }
        }

        // Middle rows
        for (int i = 1; i < stacks; i++)
        {
            double phi = Math.PI * i / stacks;
            double cosPhi = Math.Cos(phi);
            double sinPhi = Math.Sin(phi);
            float v = (float)i / stacks;

            for (int j = 0; j <= slices; j++)
            {
                double theta = 2.0 * Math.PI * j / slices;
                var pos = new Vector3D(
                    r.X * Math.Cos(theta) * sinPhi,
                    r.Y * Math.Sin(theta) * sinPhi,
                    r.Z * cosPhi);
                Vector3D norm = ellipsoid.GeodeticSurfaceNormal(pos);
                float u = (float)j / slices;
                AddVertex(vertices, pos, norm, u, v);
            }
        }

        // South pole row
        {
            var pos = new Vector3D(0, 0, -r.Z);
            Vector3D norm = ellipsoid.GeodeticSurfaceNormal(pos);
            for (int j = 0; j <= slices; j++)
            {
                float u = (float)j / slices;
                AddVertex(vertices, pos, norm, u, 1f);
            }
        }

        // Indices: connect adjacent rows with triangle pairs
        for (int i = 0; i < stacks; i++)
        {
            uint row1 = (uint)(i * vertsPerRow);
            uint row2 = (uint)((i + 1) * vertsPerRow);

            for (int j = 0; j < slices; j++)
            {
                indices.Add(row1 + (uint)j);
                indices.Add(row2 + (uint)j);
                indices.Add(row1 + (uint)j + 1);

                indices.Add(row1 + (uint)j + 1);
                indices.Add(row2 + (uint)j);
                indices.Add(row2 + (uint)j + 1);
            }
        }

        _vao = new VertexArrayObject(gl,
            vertices.ToArray(), indices.ToArray(),
            new VertexAttrib(0, 3),  // position
            new VertexAttrib(1, 3),  // normal
            new VertexAttrib(2, 2)); // texcoord
    }

    public void Draw() => _vao?.Draw();

    public void Dispose() => _vao?.Dispose();

    private static void AddVertex(List<float> verts, Vector3D pos, Vector3D norm, float u, float v)
    {
        verts.Add((float)pos.X); verts.Add((float)pos.Y); verts.Add((float)pos.Z);
        verts.Add((float)norm.X); verts.Add((float)norm.Y); verts.Add((float)norm.Z);
        verts.Add(u); verts.Add(v);
    }
}
```

---

## 10. Step 8: Camera System

**Book reference:** Chapter 5 (precision)

```csharp
// Geode.Visualization/Camera.cs
using System;
using System.Numerics;

namespace Geode.Visualization;

public class Camera
{
    public double Distance  = 20_000_000;  // meters from center
    public double Longitude = 0;           // radians
    public double Latitude  = 0.3;         // radians

    public float FovY        = 60f;        // degrees
    public float AspectRatio  = 16f / 9f;
    public float NearPlane    = 100f;
    public float FarPlane     = 100_000_000f;

    private Vector3 _position;
    private Vector3 _up;

    public Vector3 Position => _position;

    public void Update()
    {
        double cosLat = Math.Cos(Latitude);
        _position = new Vector3(
            (float)(Distance * cosLat * Math.Cos(Longitude)),
            (float)(Distance * cosLat * Math.Sin(Longitude)),
            (float)(Distance * Math.Sin(Latitude)));
        _up = Vector3.Normalize(_position);
    }

    public Matrix4x4 ViewMatrix()
        => Matrix4x4.CreateLookAt(_position, Vector3.Zero, _up);

    public Matrix4x4 ProjectionMatrix()
        => Matrix4x4.CreatePerspectiveFieldOfView(
            FovY * MathF.PI / 180f, AspectRatio, NearPlane, FarPlane);

    public Matrix4x4 ViewProjectionMatrix()
        => ViewMatrix() * ProjectionMatrix();

    public void OnMouseDrag(double dx, double dy)
    {
        Longitude -= dx * 0.003;
        Latitude  += dy * 0.003;
        Latitude = Math.Clamp(Latitude, -1.5, 1.5);
        Update();
    }

    public void OnScroll(double yOffset)
    {
        Distance *= yOffset > 0 ? 0.9 : 1.1;
        Distance = Math.Clamp(Distance, 6_400_000, 50_000_000);
        Update();
    }
}
```

### Wiring input in Program.cs

```csharp
private static Camera _camera = new();
private static bool _mouseDown;
private static float _lastX, _lastY;

// In OnLoad, after creating input:
foreach (var mouse in input.Mice)
{
    mouse.MouseDown += (m, btn) => { if (btn == MouseButton.Left) _mouseDown = true; };
    mouse.MouseUp   += (m, btn) => { if (btn == MouseButton.Left) _mouseDown = false; };
    mouse.MouseMove += (m, pos) =>
    {
        if (_mouseDown) _camera.OnMouseDrag(pos.X - _lastX, pos.Y - _lastY);
        _lastX = pos.X; _lastY = pos.Y;
    };
    mouse.Scroll += (m, wheel) => _camera.OnScroll(wheel.Y);
}
_camera.Update();

// In OnRender:
_shader!.Use();
_shader.SetMat4("u_mvp", _camera.ViewProjectionMatrix());
_globe!.Draw();
```

---

## 11. Step 9: Phong Shading

**Book reference:** Chapter 4, Section 4.2, Listings 4.6-4.10

### Vertex shader

```glsl
// shaders/globe.vert
#version 330 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aTexCoord;

out vec3 vNormal;
out vec2 vTexCoord;
out vec3 vToLight;
out vec3 vToEye;

uniform mat4 u_mvp;
uniform vec3 u_lightPosition;
uniform vec3 u_eyePosition;

void main() {
    gl_Position = u_mvp * vec4(aPosition, 1.0);
    vNormal     = aNormal;
    vTexCoord   = aTexCoord;
    vToLight    = u_lightPosition - aPosition;
    vToEye      = u_eyePosition   - aPosition;
}
```

### Fragment shader

```glsl
// shaders/globe.frag
#version 330 core
in vec3 vNormal;
in vec2 vTexCoord;
in vec3 vToLight;
in vec3 vToEye;

out vec4 fragColor;

// (diffuse, specular, ambient, shininess)
uniform vec4 u_dsas;

float PhongIntensity(vec3 n, vec3 toLight, vec3 toEye, vec4 dsas) {
    vec3 reflected = reflect(-toLight, n);
    float diff = max(dot(toLight, n), 0.0);
    float spec = pow(max(dot(reflected, toEye), 0.0), dsas.w);
    return dsas.x * diff + dsas.y * spec + dsas.z;
}

void main() {
    vec3 n = normalize(vNormal);
    float intensity = PhongIntensity(n, normalize(vToLight), normalize(vToEye), u_dsas);
    fragColor = vec4(vec3(intensity), 1.0);
}
```

### Setting uniforms

```csharp
_shader.SetVec3("u_lightPosition", _camera.Position);
_shader.SetVec3("u_eyePosition", _camera.Position);
_shader.SetVec4("u_dsas", new Vector4(0.7f, 0.3f, 0.1f, 12f));
```

---

## 12. Step 10: Texture Mapping the Earth

**Book reference:** Chapter 4, Section 4.2.2

Download an equirectangular Earth image (NASA Blue Marble) and place it in `data/textures/earth.jpg`.

For image loading, add **StbImageSharp** to the Renderer project:

```bash
dotnet add Geode.Rendering package StbImageSharp
```

### Code

```csharp
// Geode.Rendering/Texture2D.cs
using Silk.NET.OpenGL;
using StbImageSharp;
using System;
using System.IO;

namespace Geode.Rendering;

public class Texture2D : IDisposable
{
    private readonly GL _gl;
    private readonly uint _handle;

    public Texture2D(GL gl, string path)
    {
        _gl = gl;
        _handle = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, _handle);

        StbImage.stbi_set_flip_vertically_on_load(1);
        var image = ImageResult.FromStream(File.OpenRead(path), ColorComponents.RedGreenBlueAlpha);

        unsafe
        {
            fixed (byte* ptr = image.Data)
            {
                _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba,
                    (uint)image.Width, (uint)image.Height, 0,
                    PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            }
        }

        _gl.GenerateMipmap(TextureTarget.Texture2D);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);

        Console.WriteLine($"Loaded texture: {path} ({image.Width}x{image.Height})");
    }

    public void Bind(uint unit = 0)
    {
        _gl.ActiveTexture(TextureUnit.Texture0 + (int)unit);
        _gl.BindTexture(TextureTarget.Texture2D, _handle);
    }

    public void Dispose() => _gl.DeleteTexture(_handle);
}
```

### Updated fragment shader

```glsl
uniform sampler2D u_texture;

void main() {
    // ... lighting as before ...
    vec3 albedo = texture(u_texture, vTexCoord).rgb;
    fragColor = vec4(albedo * intensity, 1.0);
}
```

```csharp
// In OnRender:
_earthTexture!.Bind(0);
_shader.SetInt("u_texture", 0);
```

---

## 13. Step 11: Correct Ellipsoid Normals in Shaders

**Book reference:** Chapter 2, Section 2.2.2, Listing 2.5

For the WGS84 ellipsoid, the geodetic normal differs from `normalize(position)`:

```glsl
// In globe.frag:
uniform vec3 u_oneOverRadiiSquared;

vec3 GeodeticNormal(vec3 p) {
    return normalize(p * u_oneOverRadiiSquared);
}

void main() {
    vec3 n = GeodeticNormal(vWorldPosition);
    // ... use n for lighting ...
}
```

```csharp
var oors = Ellipsoid.Wgs84.OneOverRadiiSquared;
_shader.SetVec3("u_oneOverRadiiSquared",
    new Vector3((float)oors.X, (float)oors.Y, (float)oors.Z));
```

---

# Part IV -- Interaction and Precision

## 14. Step 12: Ray-Ellipsoid Intersection (Picking)

**Book reference:** Chapter 4, Section 4.3

```csharp
public static Geodetic3D? PickGlobe(float mouseX, float mouseY,
                                     int windowW, int windowH,
                                     Camera camera)
{
    float ndcX = (2f * mouseX / windowW) - 1f;
    float ndcY = 1f - (2f * mouseY / windowH);

    Matrix4x4.Invert(camera.ViewProjectionMatrix(), out var invVP);
    var nearPt = Vector4.Transform(new Vector4(ndcX, ndcY, -1, 1), invVP);
    var farPt  = Vector4.Transform(new Vector4(ndcX, ndcY,  1, 1), invVP);
    nearPt /= nearPt.W;
    farPt  /= farPt.W;

    var origin = new Vector3D(nearPt.X, nearPt.Y, nearPt.Z);
    var dir    = new Vector3D(farPt.X - nearPt.X, farPt.Y - nearPt.Y, farPt.Z - nearPt.Z);

    double[] hits = Ellipsoid.Wgs84.Intersections(origin, dir);
    if (hits.Length == 0) return null;

    Vector3D hitPoint = origin + dir.Normalize() * hits[0];
    return Ellipsoid.Wgs84.ToGeodetic3D(hitPoint);
}
```

---

## 15. Step 13: Curves on the Ellipsoid Surface

**Book reference:** Chapter 2, Section 2.4

Already in `Ellipsoid.ComputeCurve()`. Usage:

```csharp
var seattle = Ellipsoid.Wgs84.ToVector3D(
    new Geodetic3D(Trig.ToRadians(-122.33), Trig.ToRadians(47.61)));
var tokyo = Ellipsoid.Wgs84.ToVector3D(
    new Geodetic3D(Trig.ToRadians(139.69), Trig.ToRadians(35.69)));

var points = Ellipsoid.Wgs84.ComputeCurve(seattle, tokyo, Trig.ToRadians(1.0));
// Upload points to a VBO and draw as GL_LINE_STRIP
```

---

## 16. Step 14: Fixing Vertex Jitter (Precision)

**Book reference:** Chapter 5, Sections 5.1-5.2

At WGS84 scale, GPU floats can't represent sub-meter differences. The solution: **Render Relative to Center (RTC)** -- subtract the camera position on the CPU before uploading.

```glsl
// globe.vert:
uniform vec3 u_cameraPosition;
void main() {
    vec3 relative = aPosition - u_cameraPosition;
    gl_Position = u_viewProjection * vec4(relative, 1.0);
}
```

The view matrix becomes rotation-only (camera is at origin after subtraction).

---

## 17. Step 15: Fixing Depth Buffer Precision

**Book reference:** Chapter 6

### Logarithmic depth buffer (Section 6.4)

```glsl
// globe.vert:
uniform float u_logDepthFarPlusOne;
out float vLogZ;
void main() {
    // ... transform ...
    vLogZ = log2(max(1e-6, gl_Position.w + 1.0)) * u_logDepthFarPlusOne;
}

// globe.frag:
in float vLogZ;
void main() {
    gl_FragDepth = vLogZ;
    // ... shading ...
}
```

---

# Appendices

## A: Build Instructions

### Any platform (Windows, macOS, Linux)

```bash
git clone <your-repo>
cd Geode
dotnet run --project Geode.App
```

That's it. NuGet restores Silk.NET automatically. No manual setup.

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download) (or newer)
- A GPU with OpenGL 3.3+ support
- Linux only: `sudo apt install libgl-dev` (or equivalent for your distro)

### From Visual Studio / Rider

Open `Geode.slnx`. Set `Geode.App` as the startup project. F5.

### Using the engine as a local project reference

```xml
<!-- In your app's .csproj -->
<ItemGroup>
  <ProjectReference Include="..\Geode\Geode.Core\Geode.Core.csproj" />
  <ProjectReference Include="..\Geode\Geode.Rendering\Geode.Rendering.csproj" />
  <ProjectReference Include="..\Geode\Geode.Visualization\Geode.Visualization.csproj" />
</ItemGroup>
```

### Publishing to NuGet

All three library projects are pre-configured for NuGet publishing via `Directory.Build.props`.
The demo app (`Geode.App`) has `<IsPackable>false</IsPackable>` so it's excluded.

**1. Pack all libraries:**

```bash
dotnet pack -c Release
```

This produces `.nupkg` and `.snupkg` (symbol package) files under each project's `bin/Release/` folder:
- `Geode.Core/bin/Release/Geode.Core.0.1.0.nupkg`
- `Geode.Rendering/bin/Release/Geode.Rendering.0.1.0.nupkg`
- `Geode.Visualization/bin/Release/Geode.Visualization.0.1.0.nupkg`

**2. Push to nuget.org:**

```bash
# Get an API key from https://www.nuget.org/account/apikeys
dotnet nuget push Geode.Core/bin/Release/Geode.Core.0.1.0.nupkg \
    --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json

dotnet nuget push Geode.Rendering/bin/Release/Geode.Rendering.0.1.0.nupkg \
    --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json

dotnet nuget push Geode.Visualization/bin/Release/Geode.Visualization.0.1.0.nupkg \
    --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

**3. Consumers install via:**

```bash
dotnet add package Geode.Core              # just the math (no GPU)
dotnet add package Geode.Rendering         # OpenGL wrappers + Core
dotnet add package Geode.Visualization     # full globe engine + Rendering + Core
```

**Before publishing, update these in `Directory.Build.props`:**
- `<VersionPrefix>` -- bump for each release (semver)
- `<PackageProjectUrl>` / `<RepositoryUrl>` -- your actual GitHub URL
- `<Authors>` -- your name or org

**What Source Link gives you:** When someone debugging their app steps into your library code, their IDE automatically downloads your source from GitHub and shows the exact line. This is configured via the `Microsoft.SourceLink.GitHub` package in `Directory.Build.props`.

---

## B: OpenGlobe to This Project Translation Table

The original OpenGlobe is C#, so these are nearly 1:1:

| OpenGlobe | This Project | Notes |
|-----------|-------------|-------|
| `Vector3D` | `Vector3D` | Same API, `readonly struct` |
| `Geodetic2D` | `Geodetic2D` | Same |
| `Geodetic3D` | `Geodetic3D` | Same |
| `Ellipsoid` | `Ellipsoid` | Same methods and fields |
| `Ellipsoid.Wgs84` | `Ellipsoid.Wgs84` | Same |
| `Trig.ToRadians()` | `Trig.ToRadians()` | Same |
| `MultiplyComponents()` | `MultiplyComponents()` | Same |
| `ToVector3D()` | `ToVector3D()` | Same |
| `ToGeodetic3D()` | `ToGeodetic3D()` | Same |
| `ScaleToGeodeticSurface()` | `ScaleToGeodeticSurface()` | Same |
| `GeodeticSurfaceNormal()` | `GeodeticSurfaceNormal()` | Same |
| `ComputeCurve()` | `ComputeCurve()` | Same |
| `Intersections()` | `Intersections()` | Same |
| OpenTK `GL.*` | Silk.NET `_gl.*` | Instance method vs static |
| OpenTK `GameWindow` | Silk.NET `IWindow` | Event-driven model |

---

## C: Silk.NET vs OpenTK Quick Reference

| Concept | OpenTK (book) | Silk.NET (us) |
|---------|--------------|---------------|
| Get GL context | `GL` static methods | `GL.GetApi(window)` instance |
| Window creation | `GameWindow` class | `Window.Create(options)` |
| Render loop | Override `OnRenderFrame` | `window.Render += callback` |
| Input | Override `OnKeyDown` | `input.Keyboards[0].KeyDown += callback` |
| Gen buffer | `GL.GenBuffer()` | `gl.GenBuffer()` |
| Bind buffer | `GL.BindBuffer(target, id)` | `gl.BindBuffer(target, id)` |
| Uniform | `GL.UniformMatrix4(loc, false, ref m)` | `gl.UniformMatrix4(loc, 1, false, m)` |
| Enum names | `BufferTarget.ArrayBuffer` | `BufferTargetARB.ArrayBuffer` |

The biggest difference: Silk.NET uses an **instance-based** `GL` object rather than static methods. This makes it easier to test and supports multiple contexts.

---

*Every code listing references its book section and OpenGlobe source file.
Build each step incrementally. Verify it compiles before moving on.*
