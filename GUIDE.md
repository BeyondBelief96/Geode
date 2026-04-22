# Geode - Complete Build Guide

**Based on:** *3D Engine Design for Virtual Globes* by Patrick Cozzi & Kevin Ring (2011, CRC Press)

**Reference implementation:** [OpenGlobe](https://github.com/virtualglobebook/OpenGlobe) (C#, MIT License)

**Stack:** C# / .NET 9, Silk.NET (OpenGL 4.6 Core Profile)

---

## How to Use This Guide

This guide walks you through building every file in the Geode virtual globe engine from scratch. Each section presents one or more complete, compilable source files in **strict build order** -- when you encounter a type, every type it depends on has already been defined in an earlier section. You can read this document from start to finish, creating each file as you go, and the solution will compile at every step.

The guide follows the structure of Cozzi & Ring's book, but the code is not a line-for-line port of OpenGlobe. It has been modernized for .NET 9, Silk.NET, and OpenGL 4.6 Core Profile. Where the book uses OpenGL 3.3, we use the Direct State Access (DSA) equivalents. Where the book uses C# conventions from 2011, we use current language features: `readonly struct`, file-scoped namespaces, `ReadOnlySpan<T>`, and target-typed `new`.

---

## OpenGL 4.6 vs 3.3 -- What Changes and Why

The book targets OpenGL 3.3 Core Profile. Geode targets **OpenGL 4.6 Core Profile**. This table summarizes every significant difference and where it affects our code.

| Feature | OpenGL 3.3 (Book) | OpenGL 4.6 (Geode) | Impact |
|---|---|---|---|
| **GLSL version** | `#version 330 core` | `#version 460 core` | All shader source strings change |
| **Buffer creation** | `glGenBuffers` + `glBindBuffer` + `glBufferData` | `glCreateBuffers` + `glNamedBufferStorage` (DSA) | `BufferObject.cs` -- no bind-to-edit pattern |
| **Immutable buffers** | Not available; `glBufferData` allows reallocation | `glNamedBufferStorage` -- size is fixed at creation | Prevents accidental reallocation bugs |
| **Texture creation** | `glGenTextures` + `glBindTexture` + `glTexImage2D` | `glCreateTextures` + `glTextureStorage2D` + `glTextureSubImage2D` (DSA) | `Texture2D.cs` -- no bind-to-edit |
| **VAO attribute setup** | `glBindVertexArray` + `glVertexAttribPointer` | `glCreateVertexArrays` + `glVertexArrayVertexBuffer` + `glVertexArrayAttribFormat` (DSA) | `VertexArrayObject.cs` -- separates format from buffer binding |
| **Shader creation** | `glCreateShader` (same) | `glCreateShader` (same) | No change for `ShaderProgram.cs` |
| **Clip control** | `glDepthRange(-1, 1)` (NDC z in [-1, 1]) | `glClipControl(GL_LOWER_LEFT, GL_ZERO_TO_ONE)` -- NDC z in [0, 1] | Enables reversed-Z depth buffer (Section 28) |
| **Debug output** | Requires `GL_ARB_debug_output` extension | Core `GL_KHR_debug` -- `glDebugMessageCallback` | Free GPU error messages; enabled in `RenderContext.cs` |
| **Reversed-Z depth** | Difficult: NDC z range is symmetric | Natural: clip control + `glDepthFunc(GL_GREATER)` + clear depth to 0.0 | Solves the depth buffer precision problem (Section 28) |
| **Compute shaders** | Not available | Core since 4.3 | Future: tile-based terrain LOD on GPU |
| **SPIR-V shaders** | Not available | `GL_ARB_gl_spirv` (core in 4.6) | Future: offline shader compilation |
| **Direct State Access** | Requires `GL_ARB_direct_state_access` extension | Core since 4.5 | Every GL wrapper benefits; no global bind-state mutation |

> **Practical summary:** DSA eliminates the "bind-to-edit" pattern. In OpenGL 3.3, to upload data to a buffer you first bind it to a target (`GL_ARRAY_BUFFER`), then call `glBufferData`. Any other code that binds that same target in between corrupts your state. With DSA, every operation takes the object's handle directly -- `glNamedBufferStorage(handle, ...)`. This makes resource creation order-independent and thread-safe at the API level.

---

## Table of Contents

The guide's sections track the textbook's section numbering. Book §X.Y is the book's section X.Y; Geode §N is this guide's section N. Where the guide adds material not in the book (OpenGL fundamentals, GLSL reference), the section is marked *scaffold*.

### Part I -- Introduction (Book Chapter 1)
- [Section 1: Rendering Challenges in Virtual Globes](#section-1-rendering-challenges-in-virtual-globes) -- Book §1.1
- [Section 2: Project Architecture](#section-2-project-architecture) -- Book §1.3 + §1.4

### Part II -- Math Foundations (Book Chapter 2)
- [Section 3: Virtual Globe Coordinate Systems](#section-3-virtual-globe-coordinate-systems) -- Book §2.1
- [Section 4: Core Math Types](#section-4-core-math-types) -- Book §2.1 / §2.2 -- `Trigonometry.cs`, `Constants.cs`, `Vector3D.cs`, `Geodetic2D.cs`, `Geodetic3D.cs`
- [Section 5: The Ellipsoid Class](#section-5-the-ellipsoid-class) -- Book §2.2 -- `Ellipsoid.cs`
- [Section 6: Coordinate Transformations](#section-6-coordinate-transformations) -- Book §2.3
- [Section 7: Curves on an Ellipsoid](#section-7-curves-on-an-ellipsoid) -- Book §2.4

### Part III -- Renderer Design (Book Chapter 3)
- [Section 8: Coordinate Spaces and Transform Chain](#section-8-coordinate-spaces-and-transform-chain) -- *scaffold*
- [Section 9: OpenGL Fundamentals](#section-9-opengl-fundamentals) -- *scaffold*
- [Section 10: Renderer Architecture Deep Dive](#section-10-renderer-architecture-deep-dive) -- Book §3.1 + §3.2
- [Section 11: The Shader Pipeline](#section-11-the-shader-pipeline) -- Book §3.4.1-3 -- `ShaderProgram.cs`
- [Section 15: Renderer State Objects](#section-15-renderer-state-objects) -- Book §3.3.2 + §3.3.5 -- `RenderState.cs`, `ClearState.cs`
- [Section 16: Camera and Scene State](#section-16-camera-and-scene-state) -- Book §3.3 -- `CameraState.cs`, `SceneState.cs`
- [Section 17: Draw State](#section-17-draw-state) -- Book §3.3.4 -- `DrawState.cs`
- [Section 18: Render Context](#section-18-render-context) -- Book §3.2 + §3.3.3 -- `RenderContext.cs`
- [Section 19: The Automatic Uniform System](#section-19-the-automatic-uniform-system) -- Book §3.4.5 -- `IAutomaticUniform.cs`, `AutomaticUniforms.cs`, concrete uniform classes
- [Section 19.25: The Shader Cache](#section-1925-the-shader-cache) -- Book §3.4.6 -- `ShaderCache.cs`
- [Section 12: Vertex Buffers and Vertex Arrays](#section-12-vertex-buffers-and-vertex-arrays) -- Book §3.5 -- `BufferObject.cs`, `VertexAttrib.cs`, `VertexArrayObject.cs`
- [Section 14: Vertex Data Layouts](#section-14-vertex-data-layouts) -- Book §3.5.3
- [Section 14.5: Meshes](#section-145-meshes) -- Book §3.5.6 -- *Design section; files in Geode.Core/Geometry/ when tessellators arrive*
- [Section 13: Textures](#section-13-textures) -- Book §3.6 -- `Texture2D.cs`, `Texture2DDescription.cs`, `TextureFormat.cs`, `TextureSampler.cs`, `WritePixelBuffer.cs` / `ReadPixelBuffer.cs` (staged rollout)
- [Section 19.5: Framebuffers](#section-195-framebuffers) -- Book §3.7 -- `Framebuffer.cs`
- [Section 20: Window, Context, Render Loop, and Drawing a Triangle](#section-20-window-context-render-loop-and-drawing-a-triangle) -- Book §3.8 -- `Program.cs`
- [Section 20.5: Resources](#section-205-resources) -- Book §3.9

> **Reading note for Part III.** The TOC above is in book reading order, which matches the file's physical order from §19.25 onward. The remaining ordering mismatch is that §11 (ShaderProgram) appears physically before §15-§18 (state objects) -- the original build-dependency order kept simpler classes first. When reading alongside the book's §3.3 State Management chapter, jump to §15-§18 before §11 and §19. A future revision will move §15-§18 ahead of §11 to close that gap.

### Part IV -- Globe Rendering (Book Chapter 4)
- [Section 21: Tessellating the Globe](#section-21-step-2--globe-tessellation) -- Book §4.1
- [Section 22: Camera System](#section-22-step-3--camera-system) -- *scaffold (not a book section)*
- [Section 23: Phong Shading](#section-23-step-4--phong-shading) -- Book §4.2.1
- [Section 24: Latitude-Longitude Grid](#section-24-step-5--latitude-longitude-grid) -- Book §4.2.4
- [Section 25: GPU Ray-Casted Globe](#section-25-step-6--gpu-ray-casted-globe) -- Book §4.3
- [Section 26: Day/Night Globe Shading](#section-26-step-7--daynight-globe-shading) -- Book §4.2.5

### Part V -- Vertex Transform Precision (Book Chapter 5)
- [Section 27: RTE and DSFP Vertex Transforms](#section-27-step-8--fixing-vertex-jitter) -- Book §5.1-§5.5

### Part VI -- Depth Buffer Precision (Book Chapter 6)
- [Section 28: Reversed-Z and Logarithmic Depth](#section-28-step-9--fixing-depth-buffer-precision) -- Book §6.1-§6.5

### Appendices
- [Appendix A: Solution and Project Setup](#appendix-a-build-instructions)
- [Appendix B: OpenGlobe to Geode Translation Table](#appendix-b-openglobe-to-geode-translation-table)
- [Appendix C: Silk.NET vs OpenTK Quick Reference](#appendix-c-silknet-vs-opentk-quick-reference)
- [Appendix D: Rendering Challenges Unique to Virtual Globes](#appendix-d-rendering-challenges-unique-to-virtual-globes)

---

# Part I -- Introduction

*Corresponds to Book Chapter 1: "Introduction"*

---

## Section 1: Rendering Challenges in Virtual Globes

Before writing a single line of code, we need to understand *why* virtual globe engines exist as a distinct category of 3D software. A virtual globe is not simply a large-scale video game. The problems are different in kind, not just in degree.

This section corresponds to Book Section 1.1, which identifies five core technical challenges. Every architectural decision in the rest of this guide traces back to one of these.

### Challenge 1: Precision

The Earth's equatorial radius is approximately 6,378,137 meters. A 32-bit IEEE 754 float (`float` in C#) has about 7 decimal digits of precision. At the equator, an ECEF X coordinate might be 6,378,137.0 meters. The smallest representable step at that magnitude is:

```
2^(23 - floor(log2(6378137))) ~= 2^(23 - 22) = 2^1 = 0.5 meters
```

Half a meter. You cannot represent positions more precisely than 50 centimeters. A person standing on a building rooftop, a road lane boundary, or a cable on a bridge -- all of these require centimeter or millimeter precision. Single-precision floats cannot deliver it.

**Our solution:** Every coordinate in `Geode.Core` uses `double` (64-bit, ~15-16 decimal digits). At 6,378,137 meters, the smallest step is about 0.001 millimeters -- more than sufficient. The GPU still uses 32-bit floats for rendering, but we handle the float-to-double conversion explicitly and carefully using the Relative-to-Eye (RTE) and Double-Single Floating-Point (DSFP) techniques from Book Chapter 5.

### Challenge 2: Accuracy

The Earth is not a sphere. It is an **oblate ellipsoid** -- flattened at the poles and bulging at the equator. The difference between the equatorial radius (6,378,137 m) and the polar radius (6,356,752.314245 m) is approximately **21,385 meters** -- more than 21 kilometers. A spherical approximation produces visible errors at continental scales: coastlines shift, mountain peaks move, and GPS coordinates do not line up with imagery.

**Our solution:** The `Ellipsoid` class (Section 5) models a triaxial ellipsoid with independently specified X, Y, and Z radii. All surface normals, coordinate transforms, and curve computations use the full ellipsoidal math. The `Ellipsoid.Wgs84` static instance encodes the World Geodetic System 1984 reference ellipsoid, the same datum used by GPS.

### Challenge 3: Curvature

On a flat plane or even a small game map, you can draw a straight line between two points and it stays on the surface. On an ellipsoid, a straight line in 3D space between two surface points passes *through the interior*. A line from New York to London cuts through hundreds of kilometers of rock and mantle.

Visible artifacts include:
- Polylines (roads, borders, flight paths) that dip underground between vertices
- Tessellated surfaces with gaps at triangle edges
- Camera fly-to animations that clip through the planet

**Our solution:** `Ellipsoid.ComputeCurve` (Section 5) generates great-arc curves by rotating vectors around the plane normal defined by the two endpoints, then projecting each intermediate point onto the ellipsoid surface. The `granularity` parameter controls how many intermediate points are generated.

### Challenge 4: Depth Buffer Precision

The OpenGL depth buffer stores z-values in a nonlinear distribution governed by the perspective projection matrix. Precision is concentrated near the near plane and falls off rapidly toward the far plane. The ratio of far to near determines how bad it gets:

- Near = 1 m, Far = 1,000 m (ratio 1,000): depth buffer works well
- Near = 1 m, Far = 10,000,000 m (ratio 10^7): catastrophic z-fighting

A virtual globe must render objects from a few meters away (a building you are standing next to) out to the horizon (thousands of kilometers). The near-to-far ratio easily exceeds 10^8.

**Our solution:** OpenGL 4.6 gives us `glClipControl(GL_LOWER_LEFT, GL_ZERO_TO_ONE)`, which changes the NDC z range from [-1, 1] to [0, 1]. Combined with a reversed-Z projection matrix (`glDepthFunc(GL_GREATER)`, clear depth to 0.0), floating-point depth buffers distribute precision nearly uniformly in log-distance. This is covered in detail in Section 28 (Book Chapter 6).

### Challenge 5: Massive Datasets

A virtual globe at 1-meter resolution across the entire Earth surface requires approximately:
- Surface area: 510 trillion square meters
- At 1 pixel per meter, 8 bits per channel, RGB: ~1.5 petabytes uncompressed
- Even tiled, compressed, and LOD-managed: terabytes of data

No machine has enough RAM to hold this. Data must be streamed from disk or network, decoded, and uploaded to the GPU on demand, then evicted when no longer visible.

**Our solution:** Tile-based level-of-detail with asynchronous loading. Geode.Visualization manages a tile quadtree, requests tiles from a provider, and uploads them to GPU textures as they arrive. This is Part IV of this guide.

### Additional Challenges

**Multithreading and the GL context.** An OpenGL context is bound to exactly one thread. You cannot call `glTexImage2D` from a worker thread. Data loading must happen on background threads, but all GL resource creation must happen on the render thread. This constraint shapes the entire architecture: loading produces raw byte arrays; the render loop consumes them and creates GL objects.

**Real-world applications.** Virtual globe engines power Google Earth, Cesium (formerly WebGL-based, now also native), AGI's STK (Systems Tool Kit), NASA WorldWind, and Marble. The techniques in this guide are the same ones used in production systems that serve millions of users.

---

## Section 2: Project Architecture

This section corresponds to Book Section 1.3, which describes the OpenGlobe architecture. Our project follows the same layered pattern, adapted for .NET 9 and Silk.NET.

### The Three-Layer Pattern

The engine is split into **five assemblies** organized in three logical layers, plus tests:

| Layer | Assembly | Depends On | Purpose |
|---|---|---|---|
| **Core** (pure math) | `Geode.Core` | Nothing (no GPU, no Silk.NET) | Double-precision vectors, geodetic types, ellipsoid math, coordinate transforms |
| **Rendering** (GPU wrapper) | `Geode.Rendering` | `Geode.Core`, `Silk.NET.*` | OpenGL 4.6 abstractions: shaders, buffers, textures, VAOs, render state, draw commands |
| **Visualization** (high-level) | `Geode.Visualization` | `Geode.Core`, `Geode.Rendering` | Globe tessellation, camera control, scene management, tile loading |
| **App** (executable) | `Geode.App` | All three above, plus `Silk.NET.*` | Entry point, window creation, render loop, demo scenes |
| **Tests** | `Geode.Core.Tests` | `Geode.Core`, `xunit` | Unit tests for all Core types |

### Directory Tree

Here is every file we will build in this guide, listed in the order they appear:

```
Geode/
|-- Directory.Build.props              # Shared build settings (.NET 9, NuGet metadata)
|-- Geode.slnx                        # Solution file
|-- GUIDE.md                           # This file
|
|-- Geode.Core/
|   |-- Geode.Core.csproj
|   |-- Trigonometry.cs                # Section 4  -- ToRadians, ToDegrees
|   |-- Constants.cs                   # Section 4  -- WGS84 radii
|   |-- Vector3D.cs                    # Section 4  -- Double-precision 3D vector
|   |-- Geodetic2D.cs                  # Section 4  -- Longitude + Latitude
|   |-- Geodetic3D.cs                  # Section 4  -- Longitude + Latitude + Height
|   |-- Ellipsoid.cs                   # Section 5  -- Ellipsoid math (surface normals, transforms, curves)
|
|-- Geode.Core.Tests/
|   |-- Geode.Core.Tests.csproj
|   |-- Vector3DTests.cs              # Tests for Vector3D
|   |-- Geodetic2DTests.cs            # Tests for Geodetic2D
|   |-- Geodetic3DTests.cs            # Tests for Geodetic3D
|   |-- EllipsoidTests.cs             # Tests for Ellipsoid
|
|-- Geode.Rendering/
|   |-- Geode.Rendering.csproj
|   |-- ShaderProgram.cs              # Section 11 -- Compile + link + uniform setters
|   |-- BufferObject.cs               # Section 12 -- Immutable GPU buffer (DSA)
|   |-- VertexAttrib.cs               # Section 12 -- Vertex attribute descriptor
|   |-- VertexArrayObject.cs          # Section 12 -- VAO (DSA)
|   |-- Texture2D.cs                  # Section 13 -- 2D texture (DSA)
|   |-- RenderState.cs                # Section 15 -- Depth test, culling, blending, etc.
|   |-- ClearState.cs                 # Section 15 -- Clear color/depth/stencil config
|   |-- CameraState.cs                # Section 16 -- View + projection matrices
|   |-- SceneState.cs                 # Section 16 -- Camera + lighting + time
|   |-- DrawState.cs                  # Section 17 -- Shader + VAO + RenderState bundle
|   |-- RenderContext.cs              # Section 18 -- Apply state, issue draws, clear
|
|-- Geode.Visualization/
|   |-- Geode.Visualization.csproj
|   |-- (Globe tessellation, camera, shading -- Part IV)
|
|-- Geode.App/
|   |-- Geode.App.csproj
|   |-- Program.cs                    # Section 20 -- Entry point and render loop
|   |-- Shaders/
|       |-- triangle.vert             # Section 20 -- Vertex shader (GLSL 460)
|       |-- triangle.frag             # Section 20 -- Fragment shader (GLSL 460)
```

### Dependency Chain

```
Geode.App
  |---> Geode.Visualization
  |       |---> Geode.Rendering ---> Silk.NET.OpenGL
  |       |---> Geode.Core            Silk.NET.Windowing
  |       |                           Silk.NET.Input
  |---> Geode.Rendering              StbImageSharp
  |---> Geode.Core
  |---> Silk.NET.* (direct, for window creation)

Geode.Core.Tests
  |---> Geode.Core
  |---> xunit
```

### Four Design Rules

These rules govern every line of code in the engine. Violating any of them creates coupling that will make the engine impossible to maintain at scale.

**Rule 1: Core never references Silk.NET.**
`Geode.Core` contains pure mathematics -- vectors, geodetic types, ellipsoid computations. It has zero NuGet dependencies. It compiles to a standalone library that could be used in a console app, a web server, or a test harness without any GPU.

**Rule 2: Rendering wraps Silk.NET behind `IDisposable`.**
Every GL resource (`ShaderProgram`, `BufferObject`, `Texture2D`, `VertexArrayObject`) is a C# class that holds a GL handle and implements `IDisposable`. The rest of the engine never calls `GL.CreateBuffer()` directly -- it creates a `BufferObject` and lets the wrapper manage the lifetime. This means we can change the GL backend (Silk.NET to raw P/Invoke, or even Vulkan) without touching Visualization or App code.

**Rule 3: Visualization uses both Core and Rendering.**
`Geode.Visualization` is where the globe lives. It uses Core for geodetic math (where to put triangles) and Rendering for GPU resources (how to draw them). It never calls Silk.NET directly.

**Rule 4: App is glue.**
`Geode.App` creates the window (using Silk.NET.Windowing directly), obtains the `GL` context, and passes it to Rendering and Visualization types. It is the only project that talks to the operating system. It handles the render loop, input, and window lifecycle.

### Shared Build Configuration

All projects inherit from `Directory.Build.props` in the solution root:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <VersionPrefix>0.1.0</VersionPrefix>
  </PropertyGroup>
</Project>
```

Key choices:
- **`ImplicitUsings` disabled.** Every file explicitly states its `using` directives. This makes dependencies visible and prevents accidental coupling.
- **`Nullable` enabled.** All reference types must be explicitly marked nullable (`?`) or guaranteed non-null. This catches null-reference bugs at compile time.
- **`LangVersion latest`.** We use C# 13 features: `readonly struct`, file-scoped namespaces, target-typed `new`, and `ReadOnlySpan<T>`.

---

# Part II -- Math Foundations

*Corresponds to Book Chapter 2: "Math Foundations"*

Every virtual globe engine starts with math. Before we can render a single pixel, we need types that can represent positions on a planetary body with sub-millimeter precision, convert between coordinate systems, compute surface normals, and generate curves that follow the surface.

This part builds the entire `Geode.Core` assembly -- six source files with zero external dependencies.

---

## Section 3: Virtual Globe Coordinate Systems

*Corresponds to Book Section 2.1*

A virtual globe engine works with two fundamentally different ways of describing where something is.

### Geographic Coordinates (Geodetic)

This is how humans think about location. A position is described by three values:

- **Longitude** (lambda): the east-west angle from the Prime Meridian, ranging from -180 to +180 degrees (or -pi to +pi radians). New York is about -74 degrees. Tokyo is about +140 degrees.
- **Latitude** (phi): the north-south angle from the equator, ranging from -90 to +90 degrees (or -pi/2 to +pi/2 radians). The equator is 0. The North Pole is +90.
- **Height** (h): meters above (positive) or below (negative) the reference ellipsoid surface. Sea level is approximately 0, though there are small deviations due to gravity anomalies (the geoid).

These coordinates are **curvilinear** -- they are angles on a curved surface, not positions in 3D space. You cannot add two geodetic coordinates and get a meaningful result. You cannot interpolate between them linearly and get a straight path through 3D space.

### Cartesian Coordinates (ECEF)

This is how the GPU thinks about location. A position is described by three values in a right-handed Cartesian coordinate system centered at the Earth's center:

- **X-axis** points from the center through the intersection of the equator and the Prime Meridian (0N, 0E -- the Gulf of Guinea).
- **Y-axis** points from the center through the intersection of the equator and 90E longitude (the Indian Ocean).
- **Z-axis** points from the center through the North Pole.

This is called **Earth-Centered, Earth-Fixed** (ECEF) because the axes rotate with the Earth. All values are in meters. The X coordinate of a point on the equator at the Prime Meridian is approximately 6,378,137 meters.

### Why Both Are Needed

**Geodetic coordinates are the input.** Map data, GPS readings, user queries ("show me 40.7128N, 74.0060W"), and tile boundaries are all specified in longitude/latitude/height. Every dataset you will ever load uses geodetic coordinates.

**Cartesian coordinates are the computation format.** To render geometry on a GPU, to compute surface normals, to find the distance between two points in 3D space, to intersect a ray with the ellipsoid, to interpolate along a great arc -- all of these require Cartesian (ECEF) coordinates.

The `Ellipsoid` class (Section 5) provides methods to convert between the two:
- `ToVector3D(Geodetic3D)` converts geodetic to Cartesian
- `ToGeodetic3D(Vector3D)` converts Cartesian to geodetic
- `ToGeodetic2D(Vector3D)` converts a surface point to longitude/latitude only

These conversions are not trivial. The geodetic-to-Cartesian direction has a closed-form solution. The Cartesian-to-geodetic direction requires Newton-Raphson iteration because the relationship is defined by a fourth-degree polynomial.

---

## Section 4: Core Math Types

*Corresponds to Book Sections 2.1 and 2.2*

This section builds five source files. They must be created in the order presented -- each file depends only on files that precede it.

**Build order:**
1. `Trigonometry.cs` -- standalone utility, no dependencies
2. `Constants.cs` -- standalone constants, no dependencies
3. `Vector3D.cs` -- depends on `System.Math` only
4. `Geodetic2D.cs` -- depends on `System.Math` only
5. `Geodetic3D.cs` -- depends on `Geodetic2D`

All files go in the `Geode.Core/` directory and belong to the `Geode.Core` namespace.

---

### Trigonometry.cs

A minimal static utility class for angle conversion between degrees and radians. All internal calculations use radians (as required by `System.Math`), but human-readable data and many geospatial formats use degrees. This class provides the bridge.

```csharp
// Geode.Core/Trigonometry.cs
//
// Degree/Radian conversion and common constants for trigonometric calculations.
// All geodetic math in the engine works in radians internally.
// This class provides conversion for human-facing input/output.

using System;

namespace Geode.Core
{
    /// <summary>
    /// Degree/Radian conversion and common constants for trigonometric calculations.
    /// </summary>
    public static class Trigonometry
    {
        /// <summary>Two times pi (full circle in radians).</summary>
        public const double TwoPi = 2 * Math.PI;

        /// <summary>Half of pi (quarter turn, 90 degrees in radians).</summary>
        public const double HalfPi = Math.PI / 2;

        /// <summary>
        /// Converts an angle from degrees to radians.
        /// </summary>
        /// <param name="degrees">The angle in degrees.</param>
        /// <returns>The angle in radians.</returns>
        public static double ToRadians(double degrees) => degrees * (Math.PI / 180);

        /// <summary>
        /// Converts an angle from radians to degrees.
        /// </summary>
        /// <param name="radians">The angle in radians.</param>
        /// <returns>The angle in degrees.</returns>
        public static double ToDegrees(double radians) => radians * (180 / Math.PI);
    }
}
```

**Why a separate class?** The conversion factor `Math.PI / 180` is easy to type wrong. Centralizing it prevents bugs and makes intent clear at call sites: `Trigonometry.ToRadians(40.7128)` is unambiguous.

---

### Constants.cs

WGS84 datum constants. These are the official values defined by the National Geospatial-Intelligence Agency (NGA). The semi-major axis is the equatorial radius; the semi-minor axis is the polar radius.

```csharp
// Geode.Core/Constants.cs
//
// Geodetic constants for the WGS84 reference ellipsoid.
// Source: NGA.STND.0036_1.0.0_WGS84, Table 3.2

namespace Geode.Core
{
    /// <summary>
    /// WGS84 ellipsoid constants.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// WGS84 semi-major axis (equatorial radius) in meters.
        /// This is the distance from the Earth's center to a point on the equator.
        /// Value: 6,378,137.0 meters (exact by definition).
        /// </summary>
        public const double Wgs84SemiMajorAxis = 6378137.0;

        /// <summary>
        /// WGS84 semi-minor axis (polar radius) in meters.
        /// This is the distance from the Earth's center to a pole.
        /// Value: 6,356,752.314245 meters (derived from the flattening parameter).
        /// The difference from the semi-major axis (~21,385 m) is the reason we
        /// cannot treat the Earth as a sphere.
        /// </summary>
        public const double Wgs84SemiMinorAxis = 6356752.314245;
    }
}
```

**Why separate from Ellipsoid?** The `Ellipsoid` class is a general triaxial ellipsoid. The WGS84 constants are specific to the Earth. Keeping them separate means `Ellipsoid` can model Mars, the Moon, or any other body without Earth-specific constants cluttering the class.

---

### Vector3D.cs

The fundamental math type. A double-precision 3D vector used for all Cartesian positions (ECEF coordinates), surface normals, and directions.

This is a `readonly struct` -- value type, no heap allocation, no mutation after construction. Every operation returns a new `Vector3D`. This makes the type safe for concurrent use without locks.

```csharp
// Geode.Core/Vector3D.cs
//
// Double-precision 3D vector for planetary-scale Cartesian coordinates.
//
// Why doubles? At Earth's equatorial radius (6,378,137 m), a 32-bit float
// has only ~0.5 meter precision. A 64-bit double has ~0.001 mm precision.
// All ECEF positions, surface normals, and directions use this type.
//
// Why readonly struct? Value semantics, no heap allocation, safe to pass
// by value in hot loops. Immutability prevents aliasing bugs.

using System;

namespace Geode.Core
{
    /// <summary>
    /// Double-precision 3D vector. 
    /// Uses doubles because WGS84 coordinates are in meters at planetary scale, 
    /// and single-precision floats would not be accurate enough for geodetic calculations.
    /// These are generally used for cartesian coordinates (ECEF/WGS84), 
    /// while Geodetic2D and Geodetic3D are used for geodetic coordinates (latitude, longitude, altitude).
    /// </summary>
    public readonly struct Vector3D
    {
        // ---------------------------------------------------------------
        // Fields
        // ---------------------------------------------------------------

        /// <summary>The X component (meters in ECEF, or unitless for normals/directions).</summary>
        public readonly double X;

        /// <summary>The Y component.</summary>
        public readonly double Y;

        /// <summary>The Z component.</summary>
        public readonly double Z;

        // ---------------------------------------------------------------
        // Constructor
        // ---------------------------------------------------------------

        /// <summary>
        /// Creates a new Vector3D with the specified components.
        /// </summary>
        public Vector3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        // ---------------------------------------------------------------
        // Properties
        // ---------------------------------------------------------------

        /// <summary>
        /// The squared magnitude (length squared) of this vector.
        /// Use this instead of Magnitude when you only need to compare lengths,
        /// since it avoids the expensive square root.
        /// </summary>
        public double MagnitudeSquared => X * X + Y * Y + Z * Z;

        /// <summary>
        /// The magnitude (Euclidean length) of this vector.
        /// For a position vector in ECEF, this is the distance from the origin
        /// (Earth's center) in meters.
        /// </summary>
        public double Magnitude => Math.Sqrt(MagnitudeSquared);

        // ---------------------------------------------------------------
        // Methods
        // ---------------------------------------------------------------

        /// <summary>
        /// Returns a unit vector (magnitude = 1) in the same direction as this vector.
        /// WARNING: Does not check for zero-length vectors. Calling Normalize()
        /// on a zero vector produces NaN components.
        /// </summary>
        public Vector3D Normalize()
        {
            double mag = Magnitude;
            return new Vector3D(X / mag, Y / mag, Z / mag);
        }

        /// <summary>
        /// Computes the dot product of this vector with another.
        /// The dot product has two geometric interpretations:
        ///   1. a . b = |a| * |b| * cos(theta)
        ///   2. The projection of a onto b (times |b|)
        /// </summary>
        public double Dot(Vector3D other) => X * other.X + Y * other.Y + Z * other.Z;

        /// <summary>
        /// Computes the cross product of this vector with another.
        /// The result is a vector perpendicular to both inputs, with magnitude
        /// |a| * |b| * sin(theta). Direction follows the right-hand rule.
        /// Used extensively for: plane normals, rotation axes, winding order.
        /// </summary>
        public Vector3D Cross(Vector3D other)
        {
            return new Vector3D(
                Y * other.Z - Z * other.Y,
                Z * other.X - X * other.Z,
                X * other.Y - Y * other.X
            );
        }

        /// <summary>
        /// Component-wise multiplication. NOT the dot product or cross product.
        /// Returns (X*scale.X, Y*scale.Y, Z*scale.Z).
        /// Used in ellipsoid math where each axis has a different scale factor
        /// (e.g., multiplying by OneOverRadiiSquared).
        /// </summary>
        public Vector3D MultiplyComponents(Vector3D scale)
        {
            return new Vector3D(X * scale.X, Y * scale.Y, Z * scale.Z);
        }

        /// <summary>
        /// Computes the angle (in radians) between this vector and another.
        /// Uses the formula: theta = acos(dot(a,b) / (|a| * |b|))
        /// Returns a value in [0, pi].
        /// </summary>
        public double AngleBetween(Vector3D other)
        {
            double dot = Dot(other);
            double mags = Magnitude * other.Magnitude;
            return Math.Acos(dot / mags);
        }

        /// <summary>
        /// Rotates this vector around the given axis by the specified angle (in radians)
        /// using Rodrigues' rotation formula:
        ///
        ///   v_rot = v * cos(theta) + (k x v) * sin(theta) + k * (k . v) * (1 - cos(theta))
        ///
        /// where k is the unit axis of rotation, v is this vector, and theta is the angle.
        ///
        /// This is used by ComputeCurve to generate intermediate points along a great arc
        /// by rotating the start vector toward the end vector.
        /// </summary>
        /// <param name="axis">The axis of rotation (will be normalized internally).</param>
        /// <param name="angle">The rotation angle in radians. Positive = right-hand rule.</param>
        public Vector3D RotateAroundAxis(Vector3D axis, double angle)
        {
            // Rodrigues' rotation formula:
            // v' = v*cos(theta) + (k x v)*sin(theta) + k*(k.v)*(1 - cos(theta))
            Vector3D k = axis.Normalize();
            double cosTheta = Math.Cos(angle);
            double sinTheta = Math.Sin(angle);
            return this * cosTheta + k.Cross(this) * sinTheta + k * (k.Dot(this) * (1 - cosTheta));
        }

        // ---------------------------------------------------------------
        // Static constants
        // ---------------------------------------------------------------

        /// <summary>The zero vector (0, 0, 0).</summary>
        public static readonly Vector3D Zero = new(0, 0, 0);

        /// <summary>Unit vector along the X axis (1, 0, 0).</summary>
        public static readonly Vector3D UnitX = new(1, 0, 0);

        /// <summary>Unit vector along the Y axis (0, 1, 0).</summary>
        public static readonly Vector3D UnitY = new(0, 1, 0);

        /// <summary>Unit vector along the Z axis (0, 0, 1).</summary>
        public static readonly Vector3D UnitZ = new(0, 0, 1);

        // ---------------------------------------------------------------
        // Operators
        // ---------------------------------------------------------------

        #region Operators

        /// <summary>Vector addition: component-wise sum.</summary>
        public static Vector3D operator +(Vector3D a, Vector3D b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        /// <summary>Vector subtraction: component-wise difference.</summary>
        public static Vector3D operator -(Vector3D a, Vector3D b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        /// <summary>Scalar multiplication (vector * scalar).</summary>
        public static Vector3D operator *(Vector3D v, double s) => new(v.X * s, v.Y * s, v.Z * s);

        /// <summary>Scalar multiplication (scalar * vector).</summary>
        public static Vector3D operator *(double s, Vector3D v) => v * s;

        /// <summary>Scalar division.</summary>
        public static Vector3D operator /(Vector3D v, double s) => new(v.X / s, v.Y / s, v.Z / s);

        /// <summary>Unary negation: returns (-X, -Y, -Z).</summary>
        public static Vector3D operator -(Vector3D v) => new(-v.X, -v.Y, -v.Z);

        /// <summary>Equality comparison (exact bitwise equality of doubles).</summary>
        public static bool operator ==(Vector3D a, Vector3D b) => a.Equals(b);

        /// <summary>Inequality comparison.</summary>
        public static bool operator !=(Vector3D a, Vector3D b) => !a.Equals(b);

        /// <summary>
        /// Value equality: returns true if all three components are bitwise identical.
        /// For approximate comparison (floating-point tolerance), use a helper method.
        /// </summary>
        public bool Equals(Vector3D other) => X == other.X && Y == other.Y && Z == other.Z;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Vector3D v && Equals(v);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);

        /// <summary>Returns a string in the format "(X, Y, Z)".</summary>
        public override string ToString() => $"({X}, {Y}, {Z})";

        #endregion
    }
}
```

**Design notes:**

- **No `IsZero` property with epsilon comparison.** In geodetic math, we compare against exact zero only when checking for degenerate input (e.g., a zero-length axis). Epsilon-based "near zero" checks are error-prone because the right epsilon depends on context. We leave that to the caller.
- **`readonly struct` prevents accidental mutation.** All fields are `readonly`. The compiler enforces that no method modifies `this`. Every method returns a new instance.
- **`MultiplyComponents` is not an operator overload.** Using `*` for component-wise multiplication would be ambiguous (is `a * b` the dot product, the cross product, or component-wise?). An explicit method name eliminates confusion.
- **`RotateAroundAxis` normalizes the axis internally.** The caller does not need to pre-normalize. This is a safety net -- passing an unnormalized axis to Rodrigues' formula produces incorrect results. The cost of an extra `Normalize()` is negligible compared to the trig functions.

---

### Geodetic2D.cs

A position on the ellipsoid surface with no height component. Used for texture coordinates, tile boundaries, and any context where height is irrelevant or implied to be zero.

```csharp
// Geode.Core/Geodetic2D.cs
//
// A surface position on the reference ellipsoid: longitude and latitude in radians.
// No height component -- use Geodetic3D when height above/below the surface matters.
//
// Convention: longitude first, latitude second. This matches the (x, y) convention
// used in most geospatial APIs (GeoJSON, WKT) and the book's code.
// Note: Google Maps and Leaflet use latitude-first, but we follow the book.

using System;

namespace Geode.Core
{
    /// <summary>
    /// A position on the ellipsoid surface: longitude + latitude in radians.
    /// </summary>
    public readonly struct Geodetic2D
    {
        /// <summary>
        /// Longitude in radians. Range: [-pi, pi].
        /// Negative = west of Prime Meridian. Positive = east.
        /// </summary>
        public readonly double Longitude; // radians [-pi, pi]

        /// <summary>
        /// Latitude in radians. Range: [-pi/2, pi/2].
        /// Negative = south of equator. Positive = north.
        /// </summary>
        public readonly double Latitude; // radians [-pi/2, pi/2]

        /// <summary>
        /// Creates a new Geodetic2D from longitude and latitude in radians.
        /// No range validation is performed -- the caller is responsible for
        /// ensuring longitude is in [-pi, pi] and latitude is in [-pi/2, pi/2].
        /// </summary>
        /// <param name="longitude">Longitude in radians, range [-pi, pi].</param>
        /// <param name="latitude">Latitude in radians, range [-pi/2, pi/2].</param>
        public Geodetic2D(double longitude, double latitude)
        {
            Longitude = longitude;
            Latitude = latitude;
        }

        /// <summary>Value equality: both longitude and latitude are bitwise identical.</summary>
        public bool Equals(Geodetic2D other) =>
            Longitude == other.Longitude && Latitude == other.Latitude;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Geodetic2D g && Equals(g);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Longitude, Latitude);

        /// <summary>Equality operator.</summary>
        public static bool operator ==(Geodetic2D a, Geodetic2D b) => a.Equals(b);

        /// <summary>Inequality operator.</summary>
        public static bool operator !=(Geodetic2D a, Geodetic2D b) => !a.Equals(b);
    }
}
```

**Why no range validation?** Validating that longitude is in [-pi, pi] and latitude is in [-pi/2, pi/2] would require either throwing exceptions (expensive in hot paths) or clamping (silently wrong). The book's OpenGlobe does not validate either. Users working with degrees will go through `Trigonometry.ToRadians()`, and the range of the result depends on the input.

---

### Geodetic3D.cs

A full geodetic position: longitude, latitude, and height above (or below) the reference ellipsoid. This is the primary type for representing positions in geographic coordinates.

```csharp
// Geode.Core/Geodetic3D.cs
//
// A full geodetic position: longitude + latitude + height.
// Height is in meters relative to the reference ellipsoid surface:
//   positive = above the surface (e.g., an aircraft)
//   negative = below the surface (e.g., a submarine or underground)
//
// Note: height is relative to the ellipsoid, NOT to mean sea level (the geoid).
// The difference between the ellipsoid and the geoid (called "geoid undulation")
// varies from about -106 m to +85 m across the Earth's surface.

using System;

namespace Geode.Core
{
    /// <summary>
    /// A position relative to the ellipsoid: longitude + latitude + height.
    /// Height is in meters above (positive) or below (negative) the surface.
    /// </summary>
    public readonly struct Geodetic3D : IEquatable<Geodetic3D>
    {
        /// <summary>
        /// Longitude in radians. Range: [-pi, pi].
        /// Negative = west of Prime Meridian. Positive = east.
        /// </summary>
        public readonly double Longitude; // radians [-pi, pi]

        /// <summary>
        /// Latitude in radians. Range: [-pi/2, pi/2].
        /// Negative = south of equator. Positive = north.
        /// </summary>
        public readonly double Latitude;  // radians [-pi/2, pi/2]

        /// <summary>
        /// Height in meters above the ellipsoid surface.
        /// Positive = above the surface. Negative = below.
        /// Default is 0.0 (on the surface).
        /// </summary>
        public readonly double Height;    // meters

        /// <summary>
        /// Creates a new Geodetic3D from explicit longitude, latitude, and height.
        /// </summary>
        /// <param name="longitude">Longitude in radians.</param>
        /// <param name="latitude">Latitude in radians.</param>
        /// <param name="height">Height in meters above the ellipsoid (default 0.0).</param>
        public Geodetic3D(double longitude, double latitude, double height = 0.0)
        {
            Longitude = longitude;
            Latitude = latitude;
            Height = height;
        }

        /// <summary>
        /// Creates a new Geodetic3D from a Geodetic2D (surface position) plus an optional height.
        /// This is the bridge between the 2D and 3D geodetic types.
        /// </summary>
        /// <param name="g">The surface position (longitude + latitude).</param>
        /// <param name="height">Height in meters above the ellipsoid (default 0.0).</param>
        public Geodetic3D(Geodetic2D g, double height = 0.0)
            : this(g.Longitude, g.Latitude, height) { }

        /// <summary>Value equality: all three components are bitwise identical.</summary>
        public bool Equals(Geodetic3D other) =>
            Longitude == other.Longitude && Latitude == other.Latitude && Height == other.Height;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Geodetic3D g && Equals(g);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Longitude, Latitude, Height);

        /// <summary>Equality operator.</summary>
        public static bool operator ==(Geodetic3D a, Geodetic3D b) => a.Equals(b);

        /// <summary>Inequality operator.</summary>
        public static bool operator !=(Geodetic3D a, Geodetic3D b) => !a.Equals(b);
    }
}
```

**The `Geodetic2D` constructor** is the key design decision here. It means you can write:

```csharp
var surface = new Geodetic2D(lon, lat);        // Tile boundary (no height)
var marker  = new Geodetic3D(surface, 100.0);  // 100 meters above the surface
```

This pattern is used constantly in globe tessellation, where tile corners are defined as `Geodetic2D` and then projected to Cartesian coordinates with `height = 0`.

**Why `IEquatable<Geodetic3D>` here but not on `Geodetic2D`?** This is an inconsistency in the current codebase. Both types implement `Equals(T)` via a public method, which provides the same functionality. Adding the interface on `Geodetic3D` makes it explicit for generic code that constrains on `IEquatable<T>`. Ideally, both types would declare the interface.

---

## Section 5: The Ellipsoid Class

*Corresponds to Book Sections 2.2 and 2.3*

This is the most important class in `Geode.Core`. It models a triaxial ellipsoid centered at the origin and provides every coordinate transform needed for a virtual globe:

- **Geodetic to Cartesian** (`ToVector3D`) -- closed-form, Book Listing 2.6
- **Cartesian to Geodetic** (`ToGeodetic3D`) -- Newton-Raphson iteration, Book Listing 2.9
- **Surface normals** from both Cartesian and geodetic inputs
- **Surface projection** (geocentric and geodetic)
- **Great-arc curve generation** (`ComputeCurve`)

The class precomputes several derived quantities from the radii to avoid redundant multiplication in hot paths.

### Precomputed Values

Given radii `(a, b, c)`:

| Property | Formula | Purpose |
|---|---|---|
| `Radii` | `(a, b, c)` | The defining radii |
| `RadiiSquared` | `(a^2, b^2, c^2)` | Used in surface normal computation and geodetic-to-Cartesian |
| `RadiiToTheFourth` | `(a^4, b^4, c^4)` | Used in Newton-Raphson derivative for geodetic projection |
| `OneOverRadiiSquared` | `(1/a^2, 1/b^2, 1/c^2)` | Used in surface normal from Cartesian and geocentric scaling |

For WGS84, `a = b = 6378137.0` (the equatorial radii are equal because WGS84 is an oblate spheroid, not a general triaxial ellipsoid) and `c = 6356752.314245` (the polar radius).

### Complete Source

```csharp
// Geode.Core/Ellipsoid.cs
//
// A triaxial ellipsoid centered at the origin, defined by three radii (a, b, c).
// Provides:
//   - Geodetic surface normals (from Cartesian or geodetic input)
//   - Geodetic <-> Cartesian coordinate transforms
//   - Geocentric and geodetic surface projection
//   - Great-arc curve computation
//
// The math follows Cozzi & Ring, "3D Engine Design for Virtual Globes",
// Chapter 2, Sections 2.2-2.4. Specific book listings are noted in comments.
//
// The WGS84 instance models the Earth. The UnitSphere instance is useful for
// testing and for rendering simple spheres.

using System;
using System.Collections.Generic;

namespace Geode.Core;

/// <summary>
/// An ellipsoid defined by three radii (a, b, c) centered at the origin.
/// Provides surface normals, coordinate transforms, ray intersection,
/// and curve computation.
/// </summary>
public class Ellipsoid
{
    // -------------------------------------------------------------------
    // Static instances
    // -------------------------------------------------------------------

    /// <summary>
    /// The WGS84 reference ellipsoid used for GPS and global mapping.
    /// Semi-major axis (equatorial): 6,378,137.0 m
    /// Semi-minor axis (polar):      6,356,752.314245 m
    /// </summary>
    public static readonly Ellipsoid Wgs84 = new Ellipsoid(
        Constants.Wgs84SemiMajorAxis,
        Constants.Wgs84SemiMajorAxis,
        Constants.Wgs84SemiMinorAxis);

    /// <summary>
    /// A unit sphere (all radii = 1.0). Useful for testing and normalized computations.
    /// </summary>
    public static readonly Ellipsoid UnitSphere = new Ellipsoid(1.0, 1.0, 1.0);

    // -------------------------------------------------------------------
    // Precomputed values (stored as Vector3D for component-wise operations)
    // -------------------------------------------------------------------

    private readonly Vector3D _radii;
    private readonly Vector3D _radiiSquared;
    private readonly Vector3D _radiiToTheFourth;
    private readonly Vector3D _oneOverRadiiSquared;

    /// <summary>Gets the radii of the ellipsoid along the X, Y, and Z axes.</summary>
    public Vector3D Radii => _radii;

    /// <summary>
    /// Gets the squared radii: (a^2, b^2, c^2).
    /// Used in the geodetic-to-Cartesian transform and surface normal computation.
    /// </summary>
    public Vector3D RadiiSquared => _radiiSquared;

    /// <summary>
    /// Gets the fourth-power radii: (a^4, b^4, c^4).
    /// Used in the Newton-Raphson derivative for geodetic surface projection.
    /// </summary>
    public Vector3D RadiiToTheFourth => _radiiToTheFourth;

    /// <summary>
    /// Gets the reciprocal squared radii: (1/a^2, 1/b^2, 1/c^2).
    /// Used in geocentric scaling and surface normal from Cartesian coordinates.
    /// </summary>
    public Vector3D OneOverRadiiSquared => _oneOverRadiiSquared;

    // -------------------------------------------------------------------
    // Constructors
    // -------------------------------------------------------------------

    /// <summary>
    /// Creates an ellipsoid from three individual radii.
    /// </summary>
    /// <param name="a">Radius along the X axis (meters).</param>
    /// <param name="b">Radius along the Y axis (meters).</param>
    /// <param name="c">Radius along the Z axis (meters).</param>
    public Ellipsoid(double a, double b, double c) : this(new Vector3D(a, b, c)) { }

    /// <summary>
    /// Creates an ellipsoid from a radii vector.
    /// Precomputes RadiiSquared, RadiiToTheFourth, and OneOverRadiiSquared
    /// so that hot-path methods avoid redundant multiplication.
    /// </summary>
    /// <param name="radii">A vector whose X, Y, Z components are the ellipsoid radii.</param>
    public Ellipsoid(Vector3D radii)
    {
        _radii = radii;

        // Precompute squared radii: used in ToVector3D and GeodeticSurfaceNormal
        _radiiSquared = new Vector3D(
            radii.X * radii.X,
            radii.Y * radii.Y,
            radii.Z * radii.Z);

        // Precompute fourth-power radii: used in Newton-Raphson derivative (S'(alpha))
        _radiiToTheFourth = new Vector3D(
            _radiiSquared.X * _radiiSquared.X,
            _radiiSquared.Y * _radiiSquared.Y,
            _radiiSquared.Z * _radiiSquared.Z);

        // Precompute reciprocal squared radii: used in geocentric scaling
        // and surface normal from Cartesian coordinates
        _oneOverRadiiSquared = new Vector3D(
            1.0 / _radiiSquared.X,
            1.0 / _radiiSquared.Y,
            1.0 / _radiiSquared.Z);
    }

    // -------------------------------------------------------------------
    // Surface normals
    // -------------------------------------------------------------------

    /// <summary>
    /// Computes the geodetic surface normal at a Cartesian point on (or near) the ellipsoid.
    ///
    /// The geodetic surface normal is NOT the same as the geocentric normal (which
    /// just points from the origin through the point). On an ellipsoid, the surface
    /// normal is tilted toward the equator because the surface is more curved there.
    ///
    /// Formula: n = normalize(p * (1/a^2, 1/b^2, 1/c^2))
    ///
    /// This works because the gradient of the ellipsoid implicit equation
    ///   f(x,y,z) = x^2/a^2 + y^2/b^2 + z^2/c^2 - 1 = 0
    /// is:
    ///   grad(f) = (2x/a^2, 2y/b^2, 2z/c^2)
    /// Normalizing and dropping the constant 2 gives the surface normal.
    /// </summary>
    /// <param name="positionOnEllipsoid">
    /// A Cartesian (ECEF) point on or near the ellipsoid surface.
    /// </param>
    /// <returns>A unit vector perpendicular to the ellipsoid surface at that point.</returns>
    public Vector3D GeodeticSurfaceNormal(Vector3D positionOnEllipsoid)
    {
        return positionOnEllipsoid.MultiplyComponents(_oneOverRadiiSquared).Normalize();
    }

    /// <summary>
    /// Computes the geodetic surface normal from geodetic coordinates.
    ///
    /// This is a direct trigonometric computation -- no iteration needed:
    ///   n = (cos(lat) * cos(lon), cos(lat) * sin(lon), sin(lat))
    ///
    /// This is the direction that "up" points at the given location.
    /// On a sphere, this would be the same as the geocentric normal.
    /// On an ellipsoid, it differs by up to ~0.19 degrees (at 45 deg latitude).
    ///
    /// Note: This overload accepts Geodetic3D but only uses Longitude and Latitude.
    /// The Height component is ignored because the surface normal direction does
    /// not depend on altitude.
    /// </summary>
    /// <param name="geodetic">The geodetic position (longitude and latitude in radians).</param>
    /// <returns>A unit vector pointing "up" from the ellipsoid surface.</returns>
    public Vector3D GeodeticSurfaceNormal(Geodetic3D geodetic)
    {
        double cosLatitude = Math.Cos(geodetic.Latitude);
        return new Vector3D(
            cosLatitude * Math.Cos(geodetic.Longitude),
            cosLatitude * Math.Sin(geodetic.Longitude),
            Math.Sin(geodetic.Latitude));
    }

    // -------------------------------------------------------------------
    // Geodetic -> Cartesian (Book Listing 2.6)
    // -------------------------------------------------------------------

    /// <summary>
    /// Converts geodetic coordinates (longitude, latitude, height) to a Cartesian
    /// (ECEF) position vector.
    ///
    /// This implements the standard geodetic-to-Cartesian transform (Book Listing 2.6):
    ///
    ///   1. Compute the surface normal: n = (cos(lat)*cos(lon), cos(lat)*sin(lon), sin(lat))
    ///   2. Scale by radii squared: k = (a^2, b^2, c^2) * n  (component-wise)
    ///   3. Compute gamma = sqrt(k . n)
    ///      gamma equals the prime vertical radius of curvature N(phi) at this latitude
    ///   4. Surface point: r_surface = k / gamma
    ///   5. Add height offset: r = r_surface + h * n
    ///
    /// For WGS84, this produces ECEF coordinates in meters, consistent with GPS.
    ///
    /// The gamma term deserves explanation. For an oblate ellipsoid (a = b):
    ///   N(phi) = a^2 / sqrt(a^2 cos^2(phi) + c^2 sin^2(phi))
    /// This is the radius of curvature in the prime vertical plane. The k/gamma
    /// expression is algebraically equivalent to the standard cartographic formula:
    ///   X = (N + h) cos(phi) cos(lambda)
    ///   Y = (N + h) cos(phi) sin(lambda)
    ///   Z = (N(1 - e^2) + h) sin(phi)
    /// but generalizes to triaxial ellipsoids and avoids computing eccentricity.
    /// </summary>
    /// <param name="geodetic">The geodetic position to convert.</param>
    /// <returns>The ECEF Cartesian position in meters.</returns>
    public Vector3D ToVector3D(Geodetic3D geodetic)
    {
        // Step 1: Unit vector perpendicular to the ellipsoid surface
        // n = (cos(phi) * cos(lambda), cos(phi) * sin(lambda), sin(phi))
        Vector3D n = GeodeticSurfaceNormal(geodetic);

        // Step 2: Scale by (a^2, b^2, c^2) to get an unnormalized surface position
        // k_i = radiiSquared_i * n_i
        Vector3D k = _radiiSquared.MultiplyComponents(n);

        // Step 3: gamma = sqrt(k . n) = sqrt(a^2*n_x^2 + b^2*n_y^2 + c^2*n_z^2)
        // For oblate ellipsoids this equals the prime vertical radius of curvature N(phi)
        double gamma = Math.Sqrt(k.X * n.X + k.Y * n.Y + k.Z * n.Z);

        // Step 4: k/gamma is the point on the ellipsoid surface directly below
        // the geodetic position (the "foot point")
        Vector3D rSurface = k / gamma;

        // Step 5: Offset along the surface normal by height h
        return rSurface + (geodetic.Height * n);
    }

    // -------------------------------------------------------------------
    // Cartesian -> Geodetic
    // -------------------------------------------------------------------

    /// <summary>
    /// Converts a Cartesian position on the ellipsoid surface to geodetic
    /// longitude and latitude (no height).
    ///
    /// This simply computes the surface normal at the point and extracts
    /// the angles:
    ///   longitude = atan2(n_y, n_x)
    ///   latitude  = asin(n_z)
    ///
    /// The input MUST be a point on (or very near) the ellipsoid surface.
    /// For arbitrary points, use ToGeodetic3D instead.
    /// </summary>
    /// <param name="positionOnEllipsoid">A Cartesian point on the ellipsoid surface.</param>
    /// <returns>The geodetic longitude and latitude in radians.</returns>
    public Geodetic2D ToGeodetic2D(Vector3D positionOnEllipsoid)
    {
        Vector3D n = GeodeticSurfaceNormal(positionOnEllipsoid);
        return new Geodetic2D(
            Math.Atan2(n.Y, n.X),  // Longitude: angle in the XY (equatorial) plane
            Math.Asin(n.Z)         // Latitude: angle from the equatorial plane
        );
    }

    /// <summary>
    /// Converts an arbitrary Cartesian (ECEF) position to full geodetic coordinates
    /// (longitude, latitude, height).
    ///
    /// This is the inverse of ToVector3D. The algorithm:
    ///   1. Project the position onto the ellipsoid surface using ScaleToSurfaceGeodetic
    ///      (Newton-Raphson iteration -- see detailed explanation below)
    ///   2. Compute the height as the distance from the surface point to the input point,
    ///      with sign determined by whether the input is above or below the surface
    ///   3. Extract longitude/latitude from the surface point using ToGeodetic2D
    ///
    /// This method works for any point in 3D space -- on the surface, above it,
    /// or below it. The height will be positive for points above the surface and
    /// negative for points below.
    /// </summary>
    /// <param name="position">Any Cartesian (ECEF) position in meters.</param>
    /// <returns>The geodetic longitude, latitude (radians), and height (meters).</returns>
    public Geodetic3D ToGeodetic3D(Vector3D position)
    {
        // Step 1: Find the closest point on the ellipsoid surface
        // (along the geodetic surface normal)
        Vector3D r_s = ScaleToSurfaceGeodetic(position);

        // Step 2: Height vector from surface to input position
        Vector3D h = position - r_s;

        // Step 3: Determine sign of height.
        // We take the dot product of the height vector with the position vector.
        // If h points in the same direction as the position (dot product > 0),
        // the input is above the surface and the height is positive.
        // If it points opposite (dot < 0), the input is below the surface
        // and the height is negative.
        double height = Math.Sign(h.Dot(position)) * h.Magnitude;

        // Step 4: Get lon/lat from the surface point
        return new Geodetic3D(ToGeodetic2D(r_s), height);
    }

    // -------------------------------------------------------------------
    // Surface projection
    // -------------------------------------------------------------------

    /// <summary>
    /// Projects a position onto the ellipsoid surface along the geocentric direction
    /// (i.e., along the line from the origin through the point).
    ///
    /// This is a simple closed-form scaling:
    ///   beta = 1 / sqrt(x^2/a^2 + y^2/b^2 + z^2/c^2)
    ///   result = beta * position
    ///
    /// The math: a point (x, y, z) is on the ellipsoid surface when
    ///   x^2/a^2 + y^2/b^2 + z^2/c^2 = 1
    /// Scaling the input by beta makes the left side equal 1.
    ///
    /// NOTE: This is NOT the closest point on the surface. The geocentric projection
    /// follows a line through the origin, while the closest point follows the surface
    /// normal (geodetic projection). The difference matters for oblate ellipsoids --
    /// it can be up to ~21 km on Earth.
    ///
    /// Use this for a fast approximation. Use ScaleToSurfaceGeodetic for accuracy.
    /// </summary>
    /// <param name="position">The position to project onto the ellipsoid.</param>
    /// <returns>The geocentrically projected point on the ellipsoid surface.</returns>
    public Vector3D ScaleToSurfaceGeocentric(Vector3D position)
    {
        double beta = 1.0 / Math.Sqrt(
            (position.X * position.X) * _oneOverRadiiSquared.X +
            (position.Y * position.Y) * _oneOverRadiiSquared.Y +
            (position.Z * position.Z) * _oneOverRadiiSquared.Z
        );

        return new Vector3D(position.X * beta, position.Y * beta, position.Z * beta);
    }

    /// <summary>
    /// Projects a position onto the ellipsoid surface along the geodetic surface normal.
    /// This finds the closest point on the ellipsoid to the input position.
    ///
    /// This implements the iterative algorithm from Book Listing 2.9.
    ///
    /// The problem: given an arbitrary point P, find the point Q on the ellipsoid such
    /// that (P - Q) is parallel to the surface normal at Q. This is equivalent to:
    ///
    ///   Q_i = P_i / (1 + alpha / a_i^2)    for each component i in {x, y, z}
    ///
    /// where alpha is a scalar that satisfies the ellipsoid equation:
    ///   S(alpha) = sum( (P_i / (a_i * d_i))^2 ) - 1 = 0
    ///   d_i = 1 + alpha / a_i^2
    ///
    /// We solve S(alpha) = 0 using Newton's method:
    ///   alpha_new = alpha - S(alpha) / S'(alpha)
    ///
    /// The derivative S'(alpha) is computed analytically:
    ///   S'(alpha) = -2 * sum( P_i^2 / (a_i^4 * d_i^3) )
    ///
    /// Convergence: The initial guess is derived from the geocentric projection.
    /// Newton's method converges quadratically, typically requiring 2-3 iterations
    /// for sub-nanometer accuracy.
    ///
    /// Tolerance: |S(alpha)| less than 1e-10. At Earth scale (~6.4e6 m), this
    /// corresponds to sub-nanometer accuracy on the surface.
    /// </summary>
    /// <param name="position">The position to project onto the ellipsoid.</param>
    /// <returns>The closest point on the ellipsoid surface (along the geodetic normal).</returns>
    public Vector3D ScaleToSurfaceGeodetic(Vector3D position)
    {
        // --- Initial guess from geocentric projection ---

        // beta scales the position so it lies on the ellipsoid surface
        // (along the line from the origin through the position)
        double beta = 1.0 / Math.Sqrt(
            (position.X * position.X) * _oneOverRadiiSquared.X +
            (position.Y * position.Y) * _oneOverRadiiSquared.Y +
            (position.Z * position.Z) * _oneOverRadiiSquared.Z
        );

        // Compute the magnitude of the geocentric normal at the geocentric surface point.
        // The geocentric normal at a surface point (beta*P) is:
        //   n_geocentric = (beta*P_x / a^2, beta*P_y / b^2, beta*P_z / c^2)
        // Its magnitude tells us how far the geocentric normal is from being a
        // geodetic normal, which guides our initial alpha guess.
        double geocentricNormalMagnitude = new Vector3D(
            beta * position.X * _oneOverRadiiSquared.X,
            beta * position.Y * _oneOverRadiiSquared.Y,
            beta * position.Z * _oneOverRadiiSquared.Z).Magnitude;

        // Initial guess for alpha: the distance along the normal from the geocentric
        // surface point to the input position, divided by the normal magnitude.
        // This approximation is close enough that Newton's method converges in 2-3 steps.
        double alpha = (1.0 - beta) * (position.Magnitude / geocentricNormalMagnitude);

        // Precompute squared position components (used in every iteration)
        double xSquared = (position.X * position.X);
        double ySquared = (position.Y * position.Y);
        double zSquared = (position.Z * position.Z);

        // Denominators: d_i = 1 + alpha / a_i^2
        // These are updated each iteration as alpha changes
        double da = 0.0;
        double db = 0.0;
        double dc = 0.0;

        // S(alpha): the ellipsoid equation evaluated at the current guess.
        // We want S(alpha) = 0. Initialize to MaxValue to ensure the loop runs
        // at least once.
        double s = double.MaxValue;

        // --- Newton-Raphson iteration ---
        // Solve S(alpha) = 0 where:
        //   S(alpha)  = sum( P_i^2 / (a_i^2 * d_i^2) ) - 1
        //   S'(alpha) = -2 * sum( P_i^2 / (a_i^4 * d_i^3) )
        //   d_i = 1 + alpha / a_i^2
        //
        // Convergence: quadratic (Newton's method with a good initial guess).
        // Typical iterations: 2-3 for |S| < 1e-10
        while (Math.Abs(s) > 1e-10)
        {
            // Update denominators for current alpha
            da = 1.0 + (alpha * _oneOverRadiiSquared.X);
            db = 1.0 + (alpha * _oneOverRadiiSquared.Y);
            dc = 1.0 + (alpha * _oneOverRadiiSquared.Z);

            double daSquared = da * da;
            double dbSquared = db * db;
            double dcSquared = dc * dc;

            double daCubed = daSquared * da;
            double dbCubed = dbSquared * db;
            double dcCubed = dcSquared * dc;

            // S(alpha) = x^2/(a^2 * da^2) + y^2/(b^2 * db^2) + z^2/(c^2 * dc^2) - 1
            s = xSquared / (_radiiSquared.X * daSquared) +
                ySquared / (_radiiSquared.Y * dbSquared) +
                zSquared / (_radiiSquared.Z * dcSquared) - 1.0;

            // S'(alpha) = -2 * [x^2/(a^4 * da^3) + y^2/(b^4 * db^3) + z^2/(c^4 * dc^3)]
            double dSdA = -2.0 * (
                xSquared / (_radiiToTheFourth.X * daCubed) +
                ySquared / (_radiiToTheFourth.Y * dbCubed) +
                zSquared / (_radiiToTheFourth.Z * dcCubed));

            // Newton step: alpha_new = alpha - S(alpha) / S'(alpha)
            alpha -= (s / dSdA);
        }

        // The surface point is P / d, component-wise:
        // Q_i = P_i / d_i = P_i / (1 + alpha / a_i^2)
        return new Vector3D(position.X / da, position.Y / db, position.Z / dc);
    }

    // -------------------------------------------------------------------
    // Curve computation (Book Section 2.4)
    // -------------------------------------------------------------------

    /// <summary>
    /// Generates a sequence of points along a great-arc curve between two positions.
    ///
    /// The algorithm (Book Section 2.4):
    ///   1. Compute the plane containing the two points and the origin.
    ///      The plane normal is: cross(start, end).normalize()
    ///   2. Compute the total angle: theta = angleBetween(start, end)
    ///   3. For each intermediate step, rotate the start vector around the
    ///      plane normal by the step angle using Rodrigues' formula.
    ///
    /// The resulting points lie in the plane defined by the origin, start, and end.
    /// They are NOT projected back onto the ellipsoid surface, so for a non-spherical
    /// ellipsoid there will be a small deviation from the true surface. For Earth's
    /// WGS84 ellipsoid, this deviation is negligible for visualization purposes.
    ///
    /// Granularity: the angular distance (in radians) between consecutive points.
    /// Smaller values produce smoother curves but more points.
    /// A typical value is pi/180 (1 degree), producing ~1 point per degree of arc.
    /// </summary>
    /// <param name="start">The starting Cartesian position.</param>
    /// <param name="end">The ending Cartesian position.</param>
    /// <param name="granularity">Angular step size in radians. Must be greater than 0.</param>
    /// <returns>
    /// A list of Cartesian positions along the curve, including start and end.
    /// If the angle between start and end is less than granularity, returns [start, end].
    /// </returns>
    public IList<Vector3D> ComputeCurve(Vector3D start, Vector3D end, double granularity)
    {
        // Step 1: Find the rotation axis
        // The cross product gives a vector normal to the plane containing
        // the origin, start, and end. This is the axis we rotate around.
        Vector3D planeNormal = start.Cross(end).Normalize();

        // Step 2: Total angle between the two position vectors
        double theta = start.AngleBetween(end);

        // Step 3: How many intermediate points?
        // We subtract 1 because start and end are added separately.
        // Math.Max ensures we never get negative points.
        int numPoints = Math.Max((int)(theta / granularity) - 1, 0);

        // Step 4: Build the point list
        // Preallocate for start + intermediates + end
        List<Vector3D> points = new List<Vector3D>(2 + numPoints) { start };

        // Step 5: Generate intermediate points using Rodrigues' rotation formula
        for (int i = 1; i <= numPoints; i++)
        {
            double phi = i * granularity;
            Vector3D rotatedPoint = start.RotateAroundAxis(planeNormal, phi);
            points.Add(rotatedPoint);
        }

        // Step 6: Always include the exact end point
        // (not a rotated approximation that might accumulate error)
        points.Add(end);
        return points;
    }
}
```

### Understanding the Newton-Raphson Iteration

The `ScaleToSurfaceGeodetic` method deserves a detailed walkthrough because it is the most mathematically dense code in the engine.

**The problem:** Given an arbitrary point P in 3D space, find the point Q on the ellipsoid surface such that the vector (P - Q) is parallel to the geodetic surface normal at Q.

**Why not just use the geocentric projection?** The geocentric projection (`ScaleToSurfaceGeocentric`) scales P along the line from the origin. This gives a *different* surface point than the geodetic projection, because the geodetic normal is *not* generally directed toward the origin on a non-spherical body. For Earth, the maximum difference between geocentric and geodetic surface projections is about 21 km (at 45 degrees latitude). That is far too large to ignore.

**The formulation:** We parameterize the surface point Q as:

```
Q_x = P_x / (1 + alpha / a^2)
Q_y = P_y / (1 + alpha / b^2)
Q_z = P_z / (1 + alpha / c^2)
```

where `alpha` is a single scalar we need to find. Substituting into the ellipsoid equation `x^2/a^2 + y^2/b^2 + z^2/c^2 = 1` gives us the function S(alpha) that we set to zero.

**The function S(alpha):**

```
S(alpha) = P_x^2 / (a^2 * d_x^2)  +  P_y^2 / (b^2 * d_y^2)  +  P_z^2 / (c^2 * d_z^2)  -  1
```

where `d_i = 1 + alpha / a_i^2`.

When S(alpha) = 0, the point Q lies exactly on the ellipsoid.

**The derivative S'(alpha):**

Newton's method requires S'(alpha). We compute it analytically:

```
S'(alpha) = -2 * [ P_x^2 / (a^4 * d_x^3)  +  P_y^2 / (b^4 * d_y^3)  +  P_z^2 / (c^4 * d_z^3) ]
```

**The Newton step:**

```
alpha_new = alpha - S(alpha) / S'(alpha)
```

**Convergence:** Newton's method converges quadratically when the initial guess is close enough. Our initial guess comes from the geocentric projection, which is always within about 21 km of the correct answer on Earth. At that distance, convergence takes 2-3 iterations to reach sub-nanometer accuracy (tolerance 1e-10).

**Why `RadiiToTheFourth`?** The `a^4` terms in the derivative are why we precompute `_radiiToTheFourth` in the constructor. Without precomputation, each Newton iteration would require six extra multiplications. Since this method is called for every vertex during globe tessellation, the savings add up.

---

## Section 6: Coordinate Transformations

*Corresponds to Book Section 2.3*

With the `Ellipsoid` class complete, we can now trace the full coordinate conversion pipeline that the engine uses to go from a user-specified location to a pixel on screen.

### The Pipeline

```
   Geodetic (lon, lat, height)
        |
        | Ellipsoid.ToVector3D()
        v
   Cartesian ECEF (X, Y, Z in meters, doubles)
        |
        | Subtract camera position (RTE -- Section 27)
        v
   Eye-relative (X, Y, Z in meters, doubles)
        |
        | Cast to float, apply view matrix
        v
   Eye space (floats, single precision)
        |
        | Apply projection matrix
        v
   Clip space (floats)
        |
        | Perspective divide (GPU hardware)
        v
   NDC [-1,1] x [-1,1] x [0,1]
        |
        | Viewport transform (GPU hardware)
        v
   Screen pixels
```

The first two steps happen on the CPU in double precision. The remaining steps happen on the GPU in single precision. The critical insight is that by subtracting the camera position *before* converting to float, we ensure that the values sent to the GPU are small (relative to the camera) even though the original ECEF coordinates are enormous. This is the **Relative-to-Eye (RTE)** technique from Book Chapter 5.

### Round-Trip Accuracy

The geodetic-to-Cartesian and Cartesian-to-geodetic conversions are inverses of each other. We can verify their accuracy with a round-trip test:

```csharp
// Example: New York City (40.7128 N, 74.0060 W, 10 meters altitude)
var nyc = new Geodetic3D(
    Trigonometry.ToRadians(-74.0060),  // longitude (west is negative)
    Trigonometry.ToRadians(40.7128),   // latitude
    10.0);                              // height in meters

// Geodetic -> Cartesian
Vector3D cartesian = Ellipsoid.Wgs84.ToVector3D(nyc);
// cartesian ~ (1334998.4, -4654050.0, 4138297.0) meters

// Cartesian -> Geodetic (round-trip)
Geodetic3D roundTrip = Ellipsoid.Wgs84.ToGeodetic3D(cartesian);
// roundTrip.Longitude ~ -74.0060 degrees (after ToDegrees)
// roundTrip.Latitude  ~  40.7128 degrees (after ToDegrees)
// roundTrip.Height    ~  10.0 meters

// The round-trip error is sub-nanometer (< 1e-9 meters)
```

This sub-nanometer accuracy is a direct consequence of:
1. Using `double` (64-bit) for all intermediate calculations
2. The Newton-Raphson iteration converging to tolerance 1e-10
3. The `ToVector3D` method being a closed-form expression (no iteration, no approximation)

### Placing a Marker at Known Coordinates

Here is the practical workflow for placing a 3D object at a known geographic location:

```csharp
// 1. Define the location in geodetic coordinates
double lonDeg = -74.0060;  // New York City longitude
double latDeg =  40.7128;  // New York City latitude
double height = 100.0;     // 100 meters above the ellipsoid

var position = new Geodetic3D(
    Trigonometry.ToRadians(lonDeg),
    Trigonometry.ToRadians(latDeg),
    height);

// 2. Convert to Cartesian (ECEF)
Vector3D ecef = Ellipsoid.Wgs84.ToVector3D(position);

// 3. Compute the surface normal (this is "up" at that location)
Vector3D up = Ellipsoid.Wgs84.GeodeticSurfaceNormal(position);

// 4. The marker's model matrix would orient it along "up"
//    and translate it to "ecef" (after RTE subtraction in Section 27)
```

The surface normal is critical for correct orientation. A building in New York must point "up" relative to the local surface, not relative to the Z axis. At 40 degrees latitude, the geodetic "up" direction is tilted about 40 degrees from the Z axis.

### Why Two Surface Normal Overloads?

The `Ellipsoid` class provides two `GeodeticSurfaceNormal` overloads:

1. **From Cartesian (`Vector3D`)**: Uses the gradient of the implicit ellipsoid equation. This is used when you have a Cartesian position and need the normal direction -- for example, when computing the height above the surface in `ToGeodetic3D`.

2. **From Geodetic (`Geodetic3D`)**: Uses direct trigonometry. This is used when you have geodetic coordinates and need the normal -- for example, in `ToVector3D` where the normal is needed to compute the surface point.

On a sphere, both overloads would give the same result (the geocentric and geodetic normals are identical). On an ellipsoid, they agree only when the input Cartesian point actually corresponds to the input geodetic coordinates. The two overloads exist for efficiency: each avoids a coordinate conversion that would be needed if only one existed.

---

## Section 7: Curves on an Ellipsoid

*Corresponds to Book Section 2.4*

### Why Curves Are Needed

Consider drawing a line from New York to London on a virtual globe. In Cartesian space, a straight line between these two cities passes *through the Earth's interior*. At the midpoint of the straight-line segment, the path would be approximately 200 km underground.

If you tessellate a polyline between two points by linearly interpolating their Cartesian coordinates, the intermediate vertices will be underground. This affects every kind of line-like feature on a globe:

- **Borders and coastlines:** Political boundaries follow the surface. A border drawn as a straight line between two Cartesian vertices dips underground if the vertices are far apart.
- **Flight paths:** Great-circle routes must follow the surface curvature.
- **Latitude/longitude grid lines:** Parallels and meridians must lie on the surface, not cut through the interior.
- **Camera fly-to animations:** A camera path from one continent to another must arc over the surface, not tunnel through the core.

### The Algorithm

`Ellipsoid.ComputeCurve` generates points along a great arc between two positions. The algorithm has three steps:

**Step 1: Find the rotation plane.**

The cross product `start x end` gives a vector normal to the plane containing the origin, start, and end. This plane intersects the ellipsoid in an ellipse -- the great ellipse that connects the two points via the shortest path through the plane.

```csharp
Vector3D planeNormal = start.Cross(end).Normalize();
```

**Step 2: Compute the total angle.**

The angle between the two position vectors determines how many intermediate points are needed:

```csharp
double theta = start.AngleBetween(end);
```

**Step 3: Generate intermediate points.**

For each step, rotate the start vector around the plane normal by the step angle using **Rodrigues' rotation formula**:

```
v_rot = v * cos(theta) + (k x v) * sin(theta) + k * (k . v) * (1 - cos(theta))
```

where `k` is the unit rotation axis (the plane normal) and `theta` is the rotation angle for that step.

This formula rotates a vector in 3D space around an arbitrary axis without constructing a rotation matrix. It is more efficient than matrix multiplication for a single vector and avoids the gimbal-lock issues of Euler angles.

### Granularity and Quality Tradeoffs

The `granularity` parameter controls the angular spacing between intermediate points, in radians:

| Granularity (radians) | Degrees | Points for NYC-London (~5,570 km) | Use Case |
|---|---|---|---|
| `pi / 18` | 10 | ~5 | Debug visualization, coarse grid |
| `pi / 180` | 1 | ~51 | Standard visualization (good default) |
| `pi / 1800` | 0.1 | ~510 | Close-up views, smooth animation paths |
| `pi / 18000` | 0.01 | ~5,100 | Survey-grade display (rarely needed) |

A good default is `Trigonometry.ToRadians(1.0)` (1 degree), which gives roughly 1 point per 111 km of arc. At typical zoom levels, this produces smooth curves with no visible segmentation.

### Accuracy Limitations

The current `ComputeCurve` implementation generates points by rotating in the plane containing the origin and the two endpoints. These rotated points lie on a circle (or ellipse) at the same distance from the origin as the start point. They do **not** lie exactly on the ellipsoid surface. The deviation depends on the ellipsoid flattening and the arc length:

- For a 1-degree arc at 45 degrees latitude: deviation < 0.1 meters
- For a 10-degree arc at 45 degrees latitude: deviation ~ 10 meters
- For a 90-degree arc at 45 degrees latitude: deviation ~ several kilometers

The book's `ComputeCurve` (Listing 2.11) projects each rotated point using `ScaleToGeocentricSurface`, not `ScaleToGeodeticSurface`. This is deliberate and important: the geocentric projection scales *along the origin-to-point line*, which keeps the projected point in the original rotation plane. The geodetic projection scales *along the surface normal*, which on an oblate ellipsoid does not pass through the origin -- so it moves the projected point out of the plane and produces a curve that is no longer planar. For the planar-arc visualization `ComputeCurve` is intended to produce, geocentric projection is correct and geodetic projection is wrong.

If you are considering swapping in `ScaleToGeodeticSurface`, stop: you actually want a different routine, something closer to a true geodesic. See "True Geodesics vs Plane Arcs" below for when that matters.

If the book-faithful geocentric projection is good enough:

```csharp
// Book-faithful: project each point onto the ellipsoid while preserving the plane
IList<Vector3D> rawCurve = ellipsoid.ComputeCurve(start, end, granularity);
List<Vector3D> onSurface = new List<Vector3D>(rawCurve.Count);
foreach (Vector3D point in rawCurve)
{
    onSurface.Add(ellipsoid.ScaleToGeocentricSurface(point));
}
```

`ScaleToGeocentricSurface` has a closed-form solution (no iteration) and is cheap. The resulting curve lies on the ellipsoid surface and stays in the original rotation plane, which is what the book wants.

### True Geodesics vs Plane Arcs

A **true geodesic** on an ellipsoid is the shortest path along the surface between two points. It is a slightly different curve from the plane-arc approximation computed by `ComputeCurve`. The difference is caused by the ellipsoid's flattening -- the plane arc follows a great circle (as if the body were a sphere), while the geodesic follows the actual shortest surface path.

For Earth's WGS84 ellipsoid (flattening ~ 1/298.257), the maximum deviation between a plane arc and a true geodesic is approximately:
- 0 for north-south paths (meridians are geodesics)
- ~20 km for long east-west paths near 45 degrees latitude

For visualization purposes, this deviation is rarely visible. For navigation and surveying applications, true geodesic algorithms (Vincenty 1975, Karney 2013 via GeographicLib) should be used instead.

### Edge Cases

**Antipodal points:** When start and end are exactly opposite each other (antipodal), `start.Cross(end)` is the zero vector and there is no unique great-arc plane. The algorithm will produce NaN values. A robust implementation would detect this case and choose an arbitrary great-arc plane (e.g., through the north pole).

**Identical points:** When start equals end, the angle is zero and `numPoints` is zero. The result is `[start, end]` -- a list with two identical points. This is correct behavior.

**Very short arcs:** When the angle between start and end is less than `granularity`, no intermediate points are generated. The result is `[start, end]`. This is correct -- there is no need to subdivide an arc that is already shorter than the requested resolution.

---

*End of Parts I and II. Part III continues with the Rendering layer.*

---

# Part III -- Renderer Design

*Corresponds to Book Chapter 3: "Renderer Design"*

The rendering layer sits between the raw OpenGL API (exposed through Silk.NET) and the high-level globe visualization. Its job is to turn the mathematical foundations from Part II into pixels on screen. This part builds the entire `Geode.Rendering` assembly and the initial `Geode.App` entry point -- source files that compile and run as a complete, working rendering pipeline.

Every file appears in **strict build-dependency order**. When a class references another type, that type has already been defined in an earlier section. You can follow this part from start to finish, creating each file as you go, and the solution will compile at every step.

### Following along with the book

The book's Chapter 3 presents topics in this sequence:

| Book | Topic | Geode section |
|---|---|---|
| §3.1 | The Need for a Renderer | §10 |
| §3.2 | Bird's-Eye View (Device/Context) | §10, §18 |
| §3.3 | State Management (RenderState, ClearState, DrawState, Context sync) | §15, §16, §17, §18 |
| §3.4 | Shaders (compilation, vertex attributes, fragment outputs, uniforms, automatic uniforms, shader cache) | §11, §19, §19.25 |
| §3.5 | Vertex Data (buffers, index buffers, vertex arrays, layouts, meshes) | §12, §14, §14.5 |
| §3.6 | Textures (textures, samplers, texture units) | §13 |
| §3.7 | Framebuffers | §19.5 |
| §3.8 | Putting It All Together: Rendering a Triangle | §20 |
| §3.9 | Resources | §20.5 |

The guide's physical order **now matches book order** from §19.25 onward -- §3.4.6 (ShaderCache) → §3.5 (Vertex Data) → §3.6 (Textures) → §3.7 (Framebuffers) → §3.8 (Triangle) reads top-to-bottom as the book does.

One mismatch remains in the first half: §11 (ShaderProgram) is physically before §15-§18 (state objects), because the original build-dependency order introduced simpler classes first. The book's §3.3 State Management comes before its §3.4 Shaders. Reading alongside the book, follow this order:

```
§8 (transform chain)  →  §9 (OpenGL primer)  →  §10 (renderer architecture)
  →  §15 (RenderState, ClearState)  →  §16 (CameraState, SceneState)       ← book §3.3
  →  §17 (DrawState)  →  §18 (RenderContext)
  →  §11 (ShaderProgram)                                                    ← book §3.4.1-3
  →  §19 (Automatic uniforms)  →  §19.25 (Shader cache)                     ← book §3.4.5-6
  →  §12 (BufferObject, VertexAttrib, VAO)  →  §14 (Vertex data layouts)    ← book §3.5.1-5
  →  §14.5 (Mesh, Core-layer geometry)                                       ← book §3.5.6
  →  §13 (Texture2D)                                                         ← book §3.6
  →  §19.5 (Framebuffer)                                                    ← book §3.7
  →  §20 (Triangle)  →  §20.5 (Resources)                                   ← book §3.8-9
```

The remaining `§11 before §15-§18` inversion will be closed in a future revision that moves the state-object sections ahead of the shader section.

### What the guide faithfully implements from the book

Part III now matches the book's architecture on every major structure:

- **Book §3.3 State Management.** `RenderState`, `ClearState`, `DrawState` match the book. Geode's `RenderContext` does shadow-state comparison before issuing GL state-change calls (§18). Every `ApplyXxx` helper compares against the shadow before calling GL, exactly as the book prescribes.
- **Book §3.4.1-3 Shader compilation and fragment outputs.** `ShaderProgram` compiles, links, discovers active uniforms at link time via `glGetActiveUniform`, and populates a `FragmentOutputs` collection via `glGetFragDataLocation` (§11).
- **Book §3.4.4 Uniforms.** Typed `Uniform` / `Uniform<T>` hierarchy, concrete `UniformFloatMatrix44GL` et al., named `UniformCollection` on `ShaderProgram`, dirty list + `Clean` flush via `glProgramUniform*` (§19).
- **Book §3.4.5 Automatic Uniforms.** Full `LinkAutomaticUniform` + `DrawAutomaticUniformFactory` + `DrawAutomaticUniform` split, `AutomaticUniformFactoryCollection` registry, per-program automatic-uniform list populated at link time (§19). The `geode_` prefix is this guide's equivalent of the book's `og_` convention.
- **Book §3.4.6 ShaderCache.** Reference-counted cache of compiled `ShaderProgram` instances, keyed by application-chosen strings, with thread-safe `Find` / `FindOrAdd` / `Release` semantics. Owned by `RenderContext`. Required for sort-by-state (once that lands) to use reference equality on the shader field (§19.25).
- **Book §3.5 Vertex Data.** `BufferObject<T>`, `VertexAttrib`, `VertexArrayObject` with DSA (§12, §14). The §3.5.6 Mesh design -- system-memory geometry living in `Geode.Core`, with a named-attribute collection and typed indices base -- is specified in §14.5; the actual `.cs` files come online when Part IV tessellators start producing geometry.
- **Book §3.6 Textures.** Full surface specified in §13 across eleven sub-sections: `Texture2DDescription` (immutable spec with renderability flags), `TextureFormat` enum, pixel buffers (`WritePixelBuffer` / `ReadPixelBuffer`), `ImageFormat` / `ImageDatatype`, `Texture2D.CopyFromBuffer` / `CopyToBuffer` / `Save`, texture rectangles (unnormalized coords for §11 ray-casting), `TextureSampler` decoupled from textures, the four pre-made samplers on `RenderContext.Samplers`, and multitexturing via `RenderContext.TextureUnits[]`. Current `Texture2D.cs` is the starter -- the "Current vs target" table at the end of §13 orders the rollout by consuming feature (Framebuffer, §26 day/night, §22 tile streaming, §11 height fields).
- **Book §3.7 Framebuffers.** Indexable `ColorAttachments` collection, single `DepthAttachment` / `DepthStencilAttachment` slots, format validation at assignment time, delayed `glNamedFramebufferTexture` flushed on `Bind()`, named fragment-output routing via `shader.FragmentOutputs["name"]` (§19.5).
- **Book §3.8 Triangle.** The payoff example (§20) uses the automatic uniform for MVP and the manual `Uniforms[...]` collection for per-draw values.

### What the book covers that this guide does *not* yet cover

Reading the book in parallel, you will still encounter concepts the guide does not yet implement:

- **Book §3.2 Device/Context split.** The book separates a shareable `Device` (for `ShaderProgram`, `VertexBuffer`, `Texture2D`) from a non-shareable `Context` (for `VertexArray`, `Framebuffer`). GL forbids sharing VAOs and FBOs across contexts. Geode conflates both into `RenderContext` -- fine for a single-window demo, breaks for multi-window. The automatic-uniform registry that the book puts on `Device` is a static class in Geode (`AutomaticUniformFactoryCollection`).
- **Book §3.3.1 failed-approach walkthrough.** The book walks through three incorrect approaches to state management (naive enable/disable, `glPushAttrib`/`glPopAttrib`, full-reset) before presenting the shadow-state design. The guide skips straight to the final design.
- **Book §3.3.2 PrimitiveRestart and StencilTest.** Not in Geode's `RenderState` yet.
- **Book §3.3.3 version-integer coarse-check.** Comparing a single integer before fine-grained state field comparison. A cheap optimization not yet implemented.
- **Book §3.3.6 sort-by-state.** Sorting draws by shader → depth-test → blending before issuing them is the standard optimization for scenes with many objects. The ShaderCache (§19.25) provides the reference-equality that sort-by-shader needs; sort-by-state itself is the remaining piece.
- **Book §3.4.1 embedded shader resources.** OpenGlobe ships shaders as assembly-embedded resources (`EmbeddedResources.GetText(...)`). Geode reads them from disk. Cosmetic difference.
- **Book §3.4.1 built-in constant preamble.** `og_pi`, `og_halfPi`, etc. injected as a shader-source preamble. Geode expects shaders to use GLSL's `radians(180.0)` instead.
- **Book §3.6 PBOs and TextureRectangle.** Pixel Buffer Objects enable async texture uploads from disk. `TextureRectangle` supports unnormalized coordinates, used in Chapter 11 height-field ray casting.

These are candidate follow-up work, prioritized by which globe-rendering features they enable.

---

## Section 8: Coordinate Spaces and the Transform Chain

*Corresponds to Book Section 3.1*

Before we write any GPU code, we need to understand the coordinate spaces that every vertex passes through on its way from application memory to a lit pixel on screen. This is the **transform chain** -- a sequence of matrix multiplications that progressively reshape the world until it fits in a 2D rectangle of pixels.

### The Six Coordinate Spaces

Every 3D rendering engine -- game, CAD, virtual globe -- uses the same six spaces. The only differences are the conventions (Y-up vs Z-up, left-hand vs right-hand) and the numeric precision at each stage.

```
Model Space ──[Model Matrix]──> World Space ──[View Matrix]──> Eye Space
                                                                  │
                                                            [Projection Matrix]
                                                                  │
                                                                  v
Screen Space <──[Viewport Transform]──< NDC <──[Perspective Divide]──< Clip Space
```

| Space | Origin | Units | Typical Range | Who defines it |
|---|---|---|---|---|
| **Model** | Center of the mesh | Meters (or any) | +-10^1 to 10^3 | The artist / modeler |
| **World** | Center of the Earth (ECEF) | Meters | +-6.4 x 10^6 | The geodetic coordinate system |
| **Eye** (Camera) | Camera position | Meters | +-10^7 (far plane) | The view matrix |
| **Clip** | Camera position | Homogeneous (xyzw) | +-w | The projection matrix |
| **NDC** | Center of the viewport | Unitless | x,y in [-1,1]; z in [0,1] | Perspective divide (xyz/w) |
| **Screen** (Window) | Lower-left pixel | Pixels | [0, width] x [0, height] | `glViewport` |

### The View Matrix (LookAt)

The view matrix transforms world-space positions into eye space -- a coordinate system where the camera is at the origin, looking down the -Z axis (OpenGL convention), with +Y pointing up.

The standard **LookAt** construction takes three inputs:
- `eye` -- the camera position in world space
- `target` -- the point the camera looks at
- `up` -- a hint vector for which direction is "up" (usually +Z or +Y)

From these, we compute an orthonormal basis:

```
forward = normalize(target - eye)       // Camera's -Z axis (into the screen)
right   = normalize(forward x up)       // Camera's +X axis
trueUp  = right x forward              // Camera's +Y axis (orthogonal to forward and right)
```

The view matrix is then:

```
        |  right.x    right.y    right.z   -dot(right, eye)   |
  V  =  |  trueUp.x   trueUp.y   trueUp.z  -dot(trueUp, eye)  |
        | -forward.x  -forward.y  -forward.z  dot(forward, eye) |
        |  0           0           0          1                  |
```

This matrix simultaneously rotates and translates: it reorients the world so the camera's axes align with the coordinate axes, then shifts the origin to the camera position. The negation of the forward vector is because OpenGL's eye space looks down -Z.

**For virtual globes**, we compute this matrix in **double precision** on the CPU. The camera might be at `(6378137.0, 0.0, 0.0)` (on the equator) looking at `(6378137.0, 100.0, 0.0)` (100 meters east). If we computed the view matrix in 32-bit floats, the subtraction `target - eye` would lose precision catastrophically -- the difference of two large, nearly-equal numbers. In double precision, we get all 15 significant digits.

### The Projection Matrix

The projection matrix transforms eye space into clip space. For perspective projection with a vertical field of view `fovY`, aspect ratio `a`, near plane `n`, and far plane `f`:

**Standard OpenGL (NDC z in [-1, 1]):**

```
        | 1/(a*tan(fovY/2))   0                0                    0              |
  P  =  | 0                   1/tan(fovY/2)    0                    0              |
        | 0                   0               -(f+n)/(f-n)         -2fn/(f-n)      |
        | 0                   0               -1                    0              |
```

**Our projection (NDC z in [0, 1]) -- using `glClipControl(GL_LOWER_LEFT, GL_ZERO_TO_ONE)`:**

```
        | 1/(a*tan(fovY/2))   0                0                    0              |
  P  =  | 0                   1/tan(fovY/2)    0                    0              |
        | 0                   0               -f/(f-n)             -fn/(f-n)       |
        | 0                   0               -1                    0              |
```

The [0,1] depth range has a critical advantage: it maps naturally to the floating-point distribution, which has more precision near zero. Combined with a reversed-Z depth buffer (`glDepthFunc(GL_GREATER)`, clear depth to 0.0), this provides nearly uniform log-precision across the entire depth range. Section 28 covers this in detail.

> **3.3 vs 4.6 -- Clip Control**
>
> OpenGL 3.3 always uses NDC z in [-1, 1]. There is no `glClipControl`. To get reversed-Z, you must hack the projection matrix and use `glDepthRange(1, 0)`, which is fragile. OpenGL 4.6 (via `GL_ARB_clip_control`, core since 4.5) provides `glClipControl(GL_LOWER_LEFT, GL_ZERO_TO_ONE)`, which changes the depth mapping at the hardware level. This is the correct, robust solution.

### Why Virtual Globes Break the Standard Pipeline

Three problems make the textbook transform chain fail at planetary scale:

**Problem 1: The model matrix is identity.** In a typical game, objects are modeled at a convenient local origin and then placed in the world with a model matrix. On a virtual globe, there is no meaningful "model space" for the terrain -- the terrain *is* the world. The globe mesh is generated directly in ECEF (world) coordinates. The model matrix is the identity. This means the model-view-projection matrix degenerates to just view-projection, and we lose the opportunity to use the model matrix to center geometry near the origin.

**Problem 2: World-space coordinates are enormous.** ECEF positions are in the millions of meters. A 32-bit float cannot represent two points on the Earth's surface that are 1 meter apart -- the mantissa does not have enough bits. If we naively pass world-space positions to the vertex shader as `float` attributes, we get visible jitter: vertices that should be stationary wobble as the camera moves.

**Problem 3: The depth range is absurd.** A virtual globe must render from ~1 meter (a building the user is standing next to) out to ~20,000 km (the far side of the planet visible from orbit). The near-to-far ratio exceeds 10^7. A 24-bit integer depth buffer has only ~16 million distinct values. Z-fighting is catastrophic.

**Our solutions** (implemented in later sections):

| Problem | Solution | Section |
|---|---|---|
| Model matrix is identity | Relative-to-Eye (RTE): subtract camera position on CPU in double, pass residual as float | 27 |
| Enormous coordinates | Double-Single Floating-Point (DSFP): encode each double as two floats in the vertex shader | 27 |
| Depth buffer exhaustion | Reversed-Z with [0,1] depth range + `glClipControl` | 28 |

For now (Part III), we compute the MVP matrix in double precision on the CPU and cast to `float` for upload. This works correctly for scenes near the origin and for our initial test triangle. Parts V and VI will address the precision issues for planetary-scale rendering.

---

## Section 9: OpenGL Fundamentals

*Corresponds to Book Section 3.2*

This section covers the minimum OpenGL knowledge needed to understand the renderer we are about to build. It is not a complete OpenGL tutorial -- it covers only what you need to read and write the code in Sections 11-20.

### The GPU Pipeline

Every draw call (`glDrawElements`, `glDrawArrays`) pushes vertices through this pipeline:

```
  Vertex Data (VBO)
       │
       v
  ┌─────────────────┐
  │  Vertex Shader   │  Runs once per vertex. Transforms position, passes data to next stage.
  │  (programmable)  │  Input: vertex attributes (position, normal, texcoord)
  │                  │  Output: gl_Position (clip-space vec4) + varying outputs
  └────────┬────────┘
           │
           v
  ┌─────────────────┐
  │  Rasterizer      │  Interpolates vertex outputs across triangle pixels ("fragments").
  │  (fixed-function)│  Handles clipping, perspective divide, viewport transform.
  └────────┬────────┘
           │
           v
  ┌─────────────────┐
  │ Fragment Shader  │  Runs once per fragment (pixel candidate). Computes final color.
  │  (programmable)  │  Input: interpolated varyings from vertex shader
  │                  │  Output: fragment color (vec4)
  └────────┬────────┘
           │
           v
  ┌─────────────────┐
  │ Per-Fragment Ops │  Depth test, stencil test, blending.
  │  (fixed-function)│  Decides whether the fragment survives to the framebuffer.
  └────────┬────────┘
           │
           v
     Framebuffer (Screen)
```

The two programmable stages -- vertex shader and fragment shader -- are the only parts we write code for. Everything else is configured through state (depth test, culling, blending) that our `RenderState` class manages.

### Five Core OpenGL Objects

Every OpenGL application uses the same five fundamental object types. The table shows both the 3.3 (bind-to-edit) and 4.6 (DSA) creation patterns:

| Object | Purpose | 3.3 Creation | 4.6 Creation (DSA) | Our Wrapper |
|---|---|---|---|---|
| **VBO** (Vertex Buffer Object) | Stores vertex data on GPU | `glGenBuffers` + `glBindBuffer` + `glBufferData` | `glCreateBuffers` + `glNamedBufferStorage` | `BufferObject<T>` |
| **EBO** (Element Buffer Object) | Stores index data on GPU | Same as VBO with `GL_ELEMENT_ARRAY_BUFFER` | Same as VBO (DSA is target-agnostic) | `BufferObject<T>` |
| **VAO** (Vertex Array Object) | Describes vertex layout + binds VBO/EBO | `glGenVertexArrays` + `glBindVertexArray` + `glVertexAttribPointer` | `glCreateVertexArrays` + `glVertexArrayAttribFormat` + `glVertexArrayVertexBuffer` | `VertexArrayObject` |
| **Shader Program** | Linked vertex + fragment shaders | `glCreateShader` + `glCompileShader` + `glCreateProgram` + `glLinkProgram` | Same (no DSA equivalent for shaders) | `ShaderProgram` |
| **Texture** | 2D image data for sampling in shaders | `glGenTextures` + `glBindTexture` + `glTexImage2D` | `glCreateTextures` + `glTextureStorage2D` + `glTextureSubImage2D` | `Texture2D` |

### Silk.NET Mapping

Silk.NET wraps the raw C OpenGL API in a type-safe C# object. The mapping is direct:

| C OpenGL | Silk.NET C# | Notes |
|---|---|---|
| `glCreateBuffers(1, &handle)` | `uint handle = gl.CreateBuffer()` | Silk.NET unwraps single-object versions |
| `glNamedBufferStorage(handle, size, data, flags)` | `gl.NamedBufferStorage(handle, size, data, flags)` | Same parameters, typed enums |
| `glCreateVertexArrays(1, &vao)` | `uint vao = gl.CreateVertexArray()` | Singular convenience method |
| `glVertexArrayAttribFormat(vao, index, size, type, normalized, offset)` | `gl.VertexArrayAttribFormat(vao, index, size, type, normalized, offset)` | 1:1 mapping |
| `glCreateTextures(GL_TEXTURE_2D, 1, &tex)` | `uint tex = gl.CreateTexture(TextureTarget.Texture2D)` | Enum for target |
| `glCreateProgram()` | `uint prog = gl.CreateProgram()` | Identical |
| `glUniformMatrix4fv(loc, 1, false, ptr)` | `gl.UniformMatrix4(loc, 1, false, data)` | Span overloads available |

The `GL` object is obtained from a Silk.NET window and must be used only on the thread that created it. This is a fundamental OpenGL constraint, not a Silk.NET limitation.

### GLSL Basics

Shaders are written in GLSL (OpenGL Shading Language). Since we target OpenGL 4.6, all shaders begin with `#version 460 core`. Here is the minimum viable pair:

```glsl
// Vertex shader
#version 460 core

layout(location = 0) in vec3 aPosition;   // Vertex attribute: position
layout(location = 1) in vec3 aColor;      // Vertex attribute: color

out vec3 vColor;                           // Output to fragment shader

uniform mat4 og_modelViewPerspectiveMatrix;        // Automatic uniform (see Section 19)

void main()
{
    gl_Position = og_modelViewPerspectiveMatrix * vec4(aPosition, 1.0);  // Transform to clip space
    vColor = aColor;                                              // Pass color through
}
```

```glsl
// Fragment shader
#version 460 core

in vec3 vColor;           // Interpolated from vertex shader
out vec4 FragColor;       // Output to framebuffer

void main()
{
    FragColor = vec4(vColor, 1.0);  // RGB color, full opacity
}
```

Key points:
- `layout(location = N)` binds an attribute to a specific index. This must match the index used in `glVertexArrayAttribFormat`.
- `uniform` values are set from C# code via `glUniform*` calls. They are constant across all vertices/fragments in a single draw call.
- `in`/`out` variables are interpolated by the rasterizer between the vertex and fragment stages.
- `gl_Position` is the mandatory vertex shader output -- the clip-space position of the vertex.

---

## Section 10: Renderer Architecture Deep Dive

*Corresponds to Book Chapter 3, pp. 41-120*

This section explains the *design* of the renderer before we start implementing it. Understanding the architecture first makes the code straightforward to follow.

### Why Build a Renderer?

The book (Section 3.1) gives six reasons for wrapping OpenGL behind an abstraction layer:

1. **State management.** OpenGL is a global state machine. Calling `glEnable(GL_DEPTH_TEST)` changes a global flag that affects every subsequent draw call until someone calls `glDisable(GL_DEPTH_TEST)`. If you forget to restore state, distant parts of the code break in mysterious ways. A renderer encapsulates state into objects (`RenderState`) and applies it deterministically before each draw.

2. **Resource lifetime.** OpenGL objects (buffers, textures, shaders) are identified by integer handles. If you delete a handle but still reference it, you get silent corruption. Wrapping handles in `IDisposable` classes ensures cleanup.

3. **Error isolation.** The OpenGL debug callback (`GL_KHR_debug`) reports errors asynchronously. Without a centralized renderer, errors are hard to trace. Our `RenderContext` enables debug output at initialization.

4. **API evolution.** OpenGL 3.3 and 4.6 use different patterns (bind-to-edit vs DSA). By wrapping the API, we can switch patterns without changing application code.

5. **Testability.** Render state objects (`RenderState`, `ClearState`, `DrawState`) are plain C# classes with no GPU dependency. They can be unit-tested.

6. **Portability.** If we ever move to Vulkan or WebGPU, only the `Geode.Rendering` assembly changes. `Geode.Visualization` and `Geode.App` are insulated.

### The Device/Context Split

The book describes a `Device` (factory for GPU resources) and a `Context` (applies state and issues draw calls). In OpenGlobe, these are separate classes:

```
Device              Context
  CreateVertexArray()   Clear()
  CreateShaderProgram() Draw()
  CreateTexture2D()     ApplyRenderState()
```

In our implementation, we simplify this: GPU resources are created directly (each wrapper takes a `GL` object in its constructor), and `RenderContext` handles state application and drawing. This reduces boilerplate without losing any functionality.

### State Management: DrawState

The central design pattern is the **DrawState bundle**. Instead of setting render state, shader, and vertex array separately, the application creates a `DrawState` that groups them:

```
DrawState
  ├── RenderState        (depth test, culling, blending, etc.)
  ├── ShaderProgram      (the compiled GLSL program)
  └── VertexArrayObject  (the vertex + index data and layout)
```

When `RenderContext.Draw()` is called with a `DrawState`, it:
1. Applies the `RenderState` (comparing against the current shadow state to minimize GL calls)
2. Binds the `ShaderProgram`
3. Binds the `VertexArrayObject`
4. Sets scene uniforms (MVP matrix, camera, lighting)
5. Issues `glDrawElements` or `glDrawArrays`

This pattern comes directly from the book (Section 3.3). It eliminates the class of bugs where state from one draw call leaks into the next.

### State Shadowing

The renderer maintains a **shadow copy** of the current GPU state. Before setting a GL value, it compares against the shadow:

```csharp
// Only issue the GL call if the value actually changed
if (desired.DepthTest.Enabled != _shadow.DepthTest.Enabled)
{
    if (desired.DepthTest.Enabled)
        _gl.Enable(EnableCap.DepthTest);
    else
        _gl.Disable(EnableCap.DepthTest);
    _shadow.DepthTest.Enabled = desired.DepthTest.Enabled;
}
```

This avoids redundant GL calls, which are expensive because each one is a context switch from CPU to GPU driver. For a globe renderer that draws hundreds of terrain tiles per frame with similar state, shadowing can eliminate 80-90% of state changes.

### The Draw Call Pipeline

Here is the complete sequence for a single draw call:

```
Application calls:  context.Draw(PrimitiveType.Triangles, drawState, sceneState)
                          │
                          v
                    ApplyRenderState(drawState.RenderState)
                      ├── Compare depth test      -> glEnable/glDisable + glDepthFunc
                      ├── Compare facet culling    -> glEnable/glDisable + glCullFace + glFrontFace
                      ├── Compare blending         -> glEnable/glDisable + glBlendFunc
                      ├── Compare depth range      -> glDepthRange
                      ├── Compare depth mask       -> glDepthMask
                      ├── Compare color mask       -> glColorMask
                      ├── Compare scissor test     -> glEnable/glDisable + glScissor
                      └── Compare rasterization    -> glPolygonMode
                          │
                          v
                    shader.Bind(ctx, drawState, sceneState)
                      ├── glUseProgram
                      ├── For each DrawAutomaticUniform in the program:
                      │     auto.Set(...)  →  Uniform<T>.Value = newValue
                      │                       (dirties the uniform if changed)
                      │   Examples (only those the shader actually declares):
                      │     og_modelViewPerspectiveMatrix        (mat4)
                      │     og_viewMatrix                        (mat4)
                      │     og_perspectiveMatrix                 (mat4)
                      │     og_cameraEye                         (vec3)
                      │     og_cameraLightPosition               (vec3)
                      │     og_sunPosition                       (vec3)
                      │     og_diffuseSpecularAmbientShininess   (vec4)
                      │     og_viewport / og_inverseViewport     (vec4)
                      │     og_wgs84Height                       (float)
                      │     og_texture0..7                       (sampler2D, link-automatic)
                      │   (see Section 19 for the full registry/factory architecture)
                      └── Clean():
                            foreach (ICleanable u in _dirtyUniforms)
                                u.Clean();   // glProgramUniform*
                            _dirtyUniforms.Clear();
                          │
                          v
                    Bind VAO (glBindVertexArray)
                          │
                          v
                    glDrawElements(primitiveType, indexCount, GL_UNSIGNED_INT, 0)
```

### Mesh as CPU-Side Bridge

The book describes a `Mesh` class that holds vertex and index data on the CPU side before uploading to the GPU. A mesh is a collection of vertex attributes (position, normal, texcoord) plus an index array. When you create a `VertexArrayObject` from a mesh, the data is uploaded to the GPU and the mesh is no longer needed.

In our implementation, we skip the formal `Mesh` class for now and pass raw arrays directly to `VertexArrayObject`. When we build the globe tessellator in Part IV, we will introduce a `Mesh` abstraction.

### Build Order

The following sections implement these concepts in build-dependency order. Each file depends only on types defined in earlier sections:

| Section | File(s) | Depends On |
|---|---|---|
| 11 | `ShaderProgram.cs` | Silk.NET only |
| 12 | `BufferObject.cs`, `VertexAttrib.cs`, `VertexArrayObject.cs` | Silk.NET only |
| 13 | `Texture2D.cs` | Silk.NET only |
| 14 | *(conceptual -- no new files)* | -- |
| 15 | `RenderState.cs`, `ClearState.cs` | No GPU deps |
| 16 | `CameraState.cs`, `SceneState.cs` | `Vector3D` (Geode.Core) |
| 17 | `DrawState.cs` | `RenderState`, `ShaderProgram`, `VertexArrayObject` |
| 18 | `RenderContext.cs` | Everything above |
| 20 | `Program.cs` | Everything above |

---

## Section 11: The Shader Pipeline

*Corresponds to Book Chapter 3, Section 3.4.1 (Compiling and Linking Shaders) and Section 3.4.3 (Fragment Outputs)*

*OpenGlobe source: `Source/Renderer/GL3x/Shaders/ShaderProgramGL3x.cs`*

A shader program is a pair of GLSL source files (vertex + fragment) compiled and linked into a single GPU program. `ShaderProgram` is the largest class in `Geode.Rendering` because it is the orchestrator: it compiles source, links the program, discovers active uniforms and fragment outputs, constructs typed `Uniform` wrappers for each, consults the automatic-uniform registry, and tracks which of its uniforms are dirty so they can be flushed to the GPU before a draw.

This section builds the compile/link/fragment-output portion. Section 19 introduces the uniform subsystem and the rest of the `ShaderProgram` class (the `Uniforms` collection, the dirty list, `Bind`, `Clean`, `InitializeUniforms`, `InitializeAutomaticUniforms`).

### Design decisions

**Fail-fast on compile/link errors.** A shader that fails to compile is a programmer error. Silently returning a null program would push the error to the first draw call, making it much harder to diagnose. Throw with the full GLSL info log attached to the exception message.

**Delete individual shader objects after linking.** `glLinkProgram` copies the compiled code into the linked program. The individual shader objects are no longer needed. Detach and delete them in the constructor.

**Expose a typed uniform collection, not raw `SetInt/SetVec3` helpers.** The book's design routes every uniform through a `Uniform<T>.Value` setter that caches the value and dirties the uniform. Raw `glUniform*` helpers bypass the dirty list; keeping them would encourage patterns that duplicate work on every draw. Section 19 presents the collection; this section does not expose any per-type setter on `ShaderProgram`.

**Discover fragment outputs after link.** `glGetProgramInterface(GL_PROGRAM_OUTPUT)` yields the name-to-location mapping for every `out vec4 xxx;` declaration in the fragment shader. The `FragmentOutputs` collection is used by `Framebuffer` (Section 19.5) to route named outputs to specific color attachments: `framebuffer.ColorAttachments[shader.FragmentOutputs["dayColor"]] = dayTexture`.

### Complete source (compile/link + fragment outputs)

The uniform-related fields and methods appear here as forward declarations to keep everything in one class; Section 19 defines the types they refer to (`UniformCollection`, `Uniform`, `DrawAutomaticUniform`, `ICleanable`, etc.) and implements `InitializeUniforms`/`InitializeAutomaticUniforms`/`Bind`/`Clean`.

```csharp
// Geode.Rendering/ShaderProgram.cs
//
// Compiles GLSL vertex + fragment shaders, links them into a program,
// discovers active uniforms and fragment outputs, and wires up automatic
// uniforms from the device-level registry.
//
// Book Chapter 3, Sections 3.4.1, 3.4.3, 3.4.4, 3.4.5.
// OpenGlobe: Source/Renderer/GL3x/Shaders/ShaderProgramGL3x.cs.

using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using Geode.Rendering.Uniforms;

namespace Geode.Rendering
{
    /// <summary>
    /// A compiled and linked GLSL shader program.
    /// Owns a typed <see cref="UniformCollection"/>, a list of draw-automatic
    /// uniform setters populated at link time, and a dirty list of uniforms
    /// pending upload to the GPU.
    /// </summary>
    public class ShaderProgram : IDisposable, ICleanableObserver
    {
        private readonly GL _gl;
        private readonly uint _handle;
        private readonly UniformCollection _uniforms = new();
        private readonly List<ICleanable> _dirtyUniforms = new();
        private readonly List<DrawAutomaticUniform> _drawAutomaticUniforms = new();
        private readonly FragmentOutputs _fragmentOutputs;

        /// <summary>The raw GL program handle.</summary>
        public uint Handle => _handle;

        /// <summary>Every active uniform declared by this program, keyed by GLSL name.</summary>
        public UniformCollection Uniforms => _uniforms;

        /// <summary>Name-to-location mapping for the fragment shader's `out` variables.</summary>
        public FragmentOutputs FragmentOutputs => _fragmentOutputs;

        // ---------------------------------------------------------------
        // Construction
        // ---------------------------------------------------------------

        public ShaderProgram(GL gl, string vertexSource, string fragmentSource)
        {
            _gl = gl;

            uint vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
            uint fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);

            _handle = _gl.CreateProgram();
            _gl.AttachShader(_handle, vertexShader);
            _gl.AttachShader(_handle, fragmentShader);
            _gl.LinkProgram(_handle);

            _gl.GetProgram(_handle, ProgramPropertyARB.LinkStatus, out int status);
            if (status == 0)
            {
                string log = _gl.GetProgramInfoLog(_handle);
                _gl.DeleteProgram(_handle);
                _gl.DeleteShader(vertexShader);
                _gl.DeleteShader(fragmentShader);
                throw new Exception($"Shader link error: {log}");
            }

            _gl.DetachShader(_handle, vertexShader);
            _gl.DetachShader(_handle, fragmentShader);
            _gl.DeleteShader(vertexShader);
            _gl.DeleteShader(fragmentShader);

            // Populate the uniform collection and wire up automatic uniforms.
            // Implementation in Section 19.
            InitializeUniforms();
            InitializeAutomaticUniforms();

            // Discover fragment outputs for framebuffer binding (Section 19.5).
            _fragmentOutputs = new FragmentOutputs(_gl, _handle);
        }

        public static ShaderProgram FromFiles(GL gl, string vertexPath, string fragmentPath)
        {
            return new ShaderProgram(gl,
                System.IO.File.ReadAllText(vertexPath),
                System.IO.File.ReadAllText(fragmentPath));
        }

        // ---------------------------------------------------------------
        // Bind + Clean (flush dirty uniforms). Called by RenderContext.
        // Implementation in Section 19.
        // ---------------------------------------------------------------

        public void Bind(RenderContext ctx, DrawState drawState, SceneState sceneState) { /* Section 19 */ }
        public void Clean() { /* Section 19 */ }

        /// <summary>
        /// Low-level: make this program the current GL program (glUseProgram).
        /// For draws, prefer Bind(ctx, drawState, sceneState), which also runs
        /// automatic uniforms and flushes the dirty list. Use() exists for the
        /// rare case where client code wants the program current outside a draw,
        /// e.g. for diagnostics. Setting uniforms through the Uniforms collection
        /// does NOT require the program to be current -- glProgramUniform* works
        /// on any program handle.
        /// </summary>
        public void Use() => _gl.UseProgram(_handle);

        // ICleanableObserver: Uniform<T>.Value setters call this when a cached
        // value changes so we know to flush it on the next Clean().
        void ICleanableObserver.NotifyDirty(ICleanable c) => _dirtyUniforms.Add(c);

        // ---------------------------------------------------------------
        // Uniform and automatic-uniform initialization (Section 19)
        // ---------------------------------------------------------------

        private void InitializeUniforms() { /* Section 19 */ }
        private void InitializeAutomaticUniforms() { /* Section 19 */ }

        // ---------------------------------------------------------------
        // Typed convenience setters.
        // These are pure shortcuts over Uniforms[name].Value = value --
        // they go through the same typed collection and dirty-list path
        // that the book's architecture uses. They do NOT bypass dirtying
        // like the old raw-glUniform helpers did.
        // ---------------------------------------------------------------

        public void SetInt(string name, int value)
            => ((Uniform<int>)_uniforms[name]).Value = value;

        public void SetFloat(string name, float value)
            => ((Uniform<float>)_uniforms[name]).Value = value;

        public void SetVec3(string name, float x, float y, float z)
            => ((Uniform<System.Numerics.Vector3>)_uniforms[name]).Value
                = new System.Numerics.Vector3(x, y, z);

        public void SetVec4(string name, float x, float y, float z, float w)
            => ((Uniform<System.Numerics.Vector4>)_uniforms[name]).Value
                = new System.Numerics.Vector4(x, y, z, w);

        public void SetMat4(string name, System.Numerics.Matrix4x4 m)
            => ((Uniform<System.Numerics.Matrix4x4>)_uniforms[name]).Value = m;

        // ---------------------------------------------------------------
        // Shader compilation
        // ---------------------------------------------------------------

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
                throw new Exception($"Failed to compile {type}: {log}");
            }

            return shader;
        }

        // ---------------------------------------------------------------
        // Disposal
        // ---------------------------------------------------------------

        public void Dispose() => _gl.DeleteProgram(_handle);
    }
}
```

### FragmentOutputs

A small class that populates itself from `glGetProgramResourceIndex` / `glGetFragDataLocation`.

```csharp
// Geode.Rendering/FragmentOutputs.cs
//
// Maps fragment shader out-variable names to color attachment indices.
// Example: with "out vec4 dayColor; out vec4 nightColor;" in the fragment shader,
// FragmentOutputs["dayColor"] might be 0 and FragmentOutputs["nightColor"] 1.

using Silk.NET.OpenGL;
using System.Collections.Generic;

namespace Geode.Rendering
{
    /// <summary>
    /// Name-to-location mapping for a linked fragment shader's `out` variables.
    /// Used by Framebuffer.ColorAttachments to connect named outputs to attachment slots.
    /// </summary>
    public sealed class FragmentOutputs
    {
        private readonly GL _gl;
        private readonly uint _program;
        private readonly Dictionary<string, int> _cache = new();

        internal FragmentOutputs(GL gl, uint program)
        {
            _gl = gl;
            _program = program;
        }

        /// <summary>
        /// Returns the color attachment index that the fragment shader's named
        /// `out` variable writes to. Cached after first lookup.
        /// </summary>
        public int this[string name]
        {
            get
            {
                if (_cache.TryGetValue(name, out int loc)) return loc;

                loc = _gl.GetFragDataLocation(_program, name);
                if (loc < 0)
                    throw new System.Exception($"Fragment shader has no `out` named '{name}'.");

                _cache[name] = loc;
                return loc;
            }
        }
    }
}
```

### What this gives us

Any pair of GLSL source strings can be compiled, linked, and used in three lines:

```csharp
var shader = new ShaderProgram(gl, vertSrc, fragSrc);

// Per-draw manual uniform (e.g., object tint):
((Uniform<Vector4>)shader.Uniforms["u_objectColor"]).Value = tint;

// Automatic uniforms (og_modelViewPerspectiveMatrix, og_cameraEye, ...)
// are populated by the engine during RenderContext.Draw -- see Section 19.
```

For shaders with many color attachments (§19.5 Framebuffers), the FragmentOutputs collection connects the shader's named outputs to specific framebuffer attachment slots.

---

## Section 15: Renderer State Objects

*Corresponds to Book Chapter 3, Section 3.3*

*OpenGlobe source: `Source/Renderer/RenderState/`, `Source/Renderer/ClearState.cs`*

These two files define pure data containers that describe *how* to render -- depth testing, face culling, blending, clear colors, etc. They have no GPU dependencies and no references to other Geode types. They are plain C# classes with sensible defaults and `Clone()` methods for deep copying.

The design follows the book's principle: **separate state description from state application**. The `RenderState` class describes the desired state; the `RenderContext` (Section 18) applies it by comparing against the shadow state and issuing only the GL calls that differ.

### RenderState.cs

This file contains all the enums, sub-state classes, and the top-level `RenderState` class.

```csharp
// Geode.Rendering/RenderState.cs
//
// Pure data classes that describe GPU render state.
// No GL calls -- these are just containers for desired values.
// RenderContext compares these against a shadow state to minimize GL calls.
//
// Book Chapter 3, Section 3.3.
// OpenGlobe: Source/Renderer/RenderState/*.cs
//
// Default state:
//   Depth test:     enabled, function = Less
//   Facet culling:  enabled, back faces, CCW winding
//   Blending:       disabled, src=SrcAlpha, dst=1-SrcAlpha
//   Depth range:    [0.0, 1.0]
//   Depth mask:     enabled (writes to depth buffer)
//   Color mask:     all channels enabled
//   Scissor test:   disabled
//   Rasterization:  Fill

namespace Geode.Rendering
{
    // -------------------------------------------------------------------
    // Enums
    // -------------------------------------------------------------------

    /// <summary>
    /// Depth comparison function. Determines which fragments pass the depth test.
    /// </summary>
    public enum DepthTestFunction
    {
        Never,
        Less,
        Equal,
        LessThanOrEqual,
        Greater,
        NotEqual,
        GreaterThanOrEqual,
        Always
    }

    /// <summary>
    /// Which face(s) of a triangle to cull (not draw).
    /// </summary>
    public enum CullFace
    {
        Front,
        Back,
        FrontAndBack
    }

    /// <summary>
    /// Vertex winding order that defines the front face of a triangle.
    /// CounterClockwise is the OpenGL default and the standard mathematical convention.
    /// </summary>
    public enum WindingOrder
    {
        Clockwise,
        CounterClockwise
    }

    /// <summary>
    /// Source and destination factors for the blending equation.
    /// The final color is: src_factor * src_color + dst_factor * dst_color.
    /// </summary>
    public enum BlendingFactor
    {
        Zero,
        One,
        SourceAlpha,
        OneMinusSourceAlpha,
        DestinationAlpha,
        OneMinusDestinationAlpha,
        SourceColor,
        OneMinusSourceColor,
        DestinationColor,
        OneMinusDestinationColor
    }

    /// <summary>
    /// How triangles are rasterized. Fill is normal rendering.
    /// Line draws wireframe. Point draws vertices only.
    /// </summary>
    public enum RasterizationMode
    {
        Fill,
        Line,
        Point
    }

    // -------------------------------------------------------------------
    // Sub-state classes (each independently cloneable)
    // -------------------------------------------------------------------

    /// <summary>
    /// Controls whether fragments are tested against the depth buffer
    /// and which comparison function is used.
    /// </summary>
    public class DepthTest
    {
        /// <summary>Whether the depth test is enabled. Default: true.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>The comparison function. Default: Less (nearer fragments win).</summary>
        public DepthTestFunction Function { get; set; } = DepthTestFunction.Less;

        public DepthTest() { }

        public DepthTest(bool enabled, DepthTestFunction function)
        {
            Enabled = enabled;
            Function = function;
        }

        /// <summary>Creates a deep copy of this state.</summary>
        public DepthTest Clone() => new(Enabled, Function);
    }

    /// <summary>
    /// Controls back-face culling. Triangles whose vertices appear in the
    /// non-front winding order (as seen from the camera) are discarded before
    /// the fragment shader runs. This halves the fragment workload for closed meshes.
    /// </summary>
    public class FacetCulling
    {
        /// <summary>Whether face culling is enabled. Default: true.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Which face to cull. Default: Back.</summary>
        public CullFace Face { get; set; } = CullFace.Back;

        /// <summary>
        /// Winding order that defines the front face. Default: CounterClockwise.
        /// Vertices wound CCW (as seen from outside the mesh) are the front face.
        /// </summary>
        public WindingOrder FrontFaceWindingOrder { get; set; } = WindingOrder.CounterClockwise;

        public FacetCulling() { }

        public FacetCulling(bool enabled, CullFace face, WindingOrder windingOrder)
        {
            Enabled = enabled;
            Face = face;
            FrontFaceWindingOrder = windingOrder;
        }

        /// <summary>Creates a deep copy of this state.</summary>
        public FacetCulling Clone() => new(Enabled, Face, FrontFaceWindingOrder);
    }

    /// <summary>
    /// Controls alpha blending. When enabled, fragment colors are combined
    /// with the existing framebuffer color using the specified factors.
    /// Used for transparency and translucency.
    /// </summary>
    public class Blending
    {
        /// <summary>Whether blending is enabled. Default: false (opaque rendering).</summary>
        public bool Enabled { get; set; } = false;

        /// <summary>Source factor. Default: SourceAlpha.</summary>
        public BlendingFactor SourceFactor { get; set; } = BlendingFactor.SourceAlpha;

        /// <summary>Destination factor. Default: OneMinusSourceAlpha.</summary>
        public BlendingFactor DestinationFactor { get; set; } = BlendingFactor.OneMinusSourceAlpha;

        public Blending() { }

        public Blending(bool enabled, BlendingFactor sourceFactor, BlendingFactor destinationFactor)
        {
            Enabled = enabled;
            SourceFactor = sourceFactor;
            DestinationFactor = destinationFactor;
        }

        /// <summary>Creates a deep copy of this state.</summary>
        public Blending Clone() => new(Enabled, SourceFactor, DestinationFactor);
    }

    /// <summary>
    /// The depth range maps NDC z values to the depth buffer range.
    /// Default: [0.0, 1.0]. For reversed-Z (Section 28), this becomes [1.0, 0.0].
    /// </summary>
    public class DepthRange
    {
        /// <summary>The near value of the depth range. Default: 0.0.</summary>
        public double Near { get; set; } = 0.0;

        /// <summary>The far value of the depth range. Default: 1.0.</summary>
        public double Far { get; set; } = 1.0;

        public DepthRange() { }

        public DepthRange(double near, double far)
        {
            Near = near;
            Far = far;
        }

        /// <summary>Creates a deep copy of this state.</summary>
        public DepthRange Clone() => new(Near, Far);
    }

    /// <summary>
    /// Controls whether fragments write to the depth buffer.
    /// Disabled for transparent objects that should be depth-tested but not
    /// contribute to the depth buffer.
    /// </summary>
    public class DepthMask
    {
        /// <summary>Whether depth writes are enabled. Default: true.</summary>
        public bool Enabled { get; set; } = true;

        public DepthMask() { }

        public DepthMask(bool enabled)
        {
            Enabled = enabled;
        }

        /// <summary>Creates a deep copy of this state.</summary>
        public DepthMask Clone() => new(Enabled);
    }

    /// <summary>
    /// Controls which color channels are written to the framebuffer.
    /// Rarely changed, but useful for specialized rendering passes
    /// (e.g., writing only to the alpha channel).
    /// </summary>
    public class ColorMask
    {
        public bool Red { get; set; } = true;
        public bool Green { get; set; } = true;
        public bool Blue { get; set; } = true;
        public bool Alpha { get; set; } = true;

        public ColorMask() { }

        public ColorMask(bool red, bool green, bool blue, bool alpha)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }

        /// <summary>Creates a deep copy of this state.</summary>
        public ColorMask Clone() => new(Red, Green, Blue, Alpha);
    }

    /// <summary>
    /// Restricts rendering to a rectangular region of the framebuffer.
    /// Used for split-screen rendering, HUD regions, or optimization
    /// (avoid shading pixels that will be overwritten).
    /// </summary>
    public class ScissorTest
    {
        /// <summary>Whether the scissor test is enabled. Default: false.</summary>
        public bool Enabled { get; set; } = false;

        /// <summary>Left edge of the scissor rectangle in pixels.</summary>
        public int X { get; set; } = 0;

        /// <summary>Bottom edge of the scissor rectangle in pixels.</summary>
        public int Y { get; set; } = 0;

        /// <summary>Width of the scissor rectangle in pixels.</summary>
        public int Width { get; set; } = 0;

        /// <summary>Height of the scissor rectangle in pixels.</summary>
        public int Height { get; set; } = 0;

        public ScissorTest() { }

        public ScissorTest(bool enabled, int x, int y, int width, int height)
        {
            Enabled = enabled;
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>Creates a deep copy of this state.</summary>
        public ScissorTest Clone() => new(Enabled, X, Y, Width, Height);
    }

    // -------------------------------------------------------------------
    // Top-level RenderState
    // -------------------------------------------------------------------

    /// <summary>
    /// Complete description of GPU render state for a draw call.
    /// All properties have sensible defaults for opaque, depth-tested,
    /// backface-culled rendering.
    /// </summary>
    public class RenderState
    {
        public DepthTest DepthTest { get; set; } = new();
        public FacetCulling FacetCulling { get; set; } = new();
        public Blending Blending { get; set; } = new();
        public DepthRange DepthRange { get; set; } = new();
        public DepthMask DepthMask { get; set; } = new();
        public ColorMask ColorMask { get; set; } = new();
        public ScissorTest ScissorTest { get; set; } = new();
        public RasterizationMode RasterizationMode { get; set; } = RasterizationMode.Fill;

        public RenderState() { }

        /// <summary>
        /// Creates a deep copy of the entire render state.
        /// Used by RenderContext to maintain the shadow state.
        /// </summary>
        public RenderState Clone() => new()
        {
            DepthTest = DepthTest.Clone(),
            FacetCulling = FacetCulling.Clone(),
            Blending = Blending.Clone(),
            DepthRange = DepthRange.Clone(),
            DepthMask = DepthMask.Clone(),
            ColorMask = ColorMask.Clone(),
            ScissorTest = ScissorTest.Clone(),
            RasterizationMode = RasterizationMode
        };
    }
}
```

**Line count:** ~280 lines.

---

### ClearState.cs

Describes what to clear and to what values before each frame (or render pass). Separated from `RenderState` because clearing is a distinct operation from drawing -- it happens once per frame, not per draw call.

```csharp
// Geode.Rendering/ClearState.cs
//
// Describes what buffers to clear and to what values.
// Used by RenderContext.Clear() at the start of each frame.
//
// Book Chapter 3, Section 3.3.
// OpenGlobe: Source/Renderer/ClearState.cs

using System;
using System.Numerics;

namespace Geode.Rendering
{
    /// <summary>
    /// Flags specifying which framebuffer attachments to clear.
    /// </summary>
    [Flags]
    public enum ClearBuffers
    {
        /// <summary>Clear the color buffer.</summary>
        ColorBuffer = 1 << 0,

        /// <summary>Clear the depth buffer.</summary>
        DepthBuffer = 1 << 1,

        /// <summary>Clear the stencil buffer.</summary>
        StencilBuffer = 1 << 2,

        /// <summary>Clear both color and depth buffers (most common).</summary>
        ColorAndDepthBuffer = ColorBuffer | DepthBuffer,

        /// <summary>Clear all three buffers.</summary>
        All = ColorBuffer | DepthBuffer | StencilBuffer
    }

    /// <summary>
    /// Describes what to clear and to what values.
    /// </summary>
    public class ClearState
    {
        /// <summary>Which buffers to clear. Default: color + depth.</summary>
        public ClearBuffers Buffers { get; set; } = ClearBuffers.ColorAndDepthBuffer;

        /// <summary>The clear color (RGBA). Default: black, full opacity.</summary>
        public Vector4 Color { get; set; } = new(0f, 0f, 0f, 1f);

        /// <summary>
        /// The clear depth value. Default: 1.0 (farthest).
        /// For reversed-Z (Section 28), this should be 0.0.
        /// </summary>
        public float Depth { get; set; } = 1f;

        /// <summary>The clear stencil value. Default: 0.</summary>
        public int Stencil { get; set; } = 0;

        /// <summary>Color mask applied during clear. Default: all channels enabled.</summary>
        public ColorMask ColorMask { get; set; } = new();

        /// <summary>Whether to write to the depth buffer during clear. Default: true.</summary>
        public bool DepthMask { get; set; } = true;

        /// <summary>Creates a ClearState with default values.</summary>
        public static ClearState Default => new();
    }
}
```

**Line count:** ~60 lines.

---

## Section 16: Camera and Scene State

*Corresponds to Book Chapter 3, Section 3.7*

*OpenGlobe source: `Source/Scene/Camera.cs`, `Source/Scene/SceneState.cs`*

These two files bridge the gap between the pure math in `Geode.Core` and the GPU rendering in `Geode.Rendering`. `CameraState` holds the camera parameters. `SceneState` computes the view and projection matrices in double precision, then provides `float`-precision versions for upload to the GPU.

### CameraState.cs

A plain data class holding the six parameters that define a perspective camera.

```csharp
// Geode.Rendering/CameraState.cs
//
// Camera parameters: position, orientation, and projection.
// The view and projection matrices are computed by SceneState,
// which uses these values plus double-precision math.
//
// Book Chapter 3, Section 3.7.
// OpenGlobe: Source/Scene/Camera.cs

using Geode.Core;

namespace Geode.Rendering
{
    /// <summary>
    /// Describes a perspective camera in world space.
    /// All positions use double-precision Vector3D for planetary-scale accuracy.
    /// </summary>
    public class CameraState
    {
        /// <summary>Camera position in world (ECEF) coordinates.</summary>
        public Vector3D Eye { get; set; } = new(0, 0, 10);

        /// <summary>The point the camera is looking at in world coordinates.</summary>
        public Vector3D Target { get; set; } = new(0, 0, 0);

        /// <summary>The up direction hint. Default: +Y up.</summary>
        public Vector3D Up { get; set; } = new(0, 1, 0);

        /// <summary>Vertical field of view in radians. Default: 60 degrees.</summary>
        public double FieldOfViewY { get; set; } = Trigonometry.ToRadians(60.0);

        /// <summary>Viewport aspect ratio (width / height). Default: 16:9.</summary>
        public double AspectRatio { get; set; } = 16.0 / 9.0;

        /// <summary>Near clip plane distance in meters. Default: 0.1.</summary>
        public double NearPlane { get; set; } = 0.1;

        /// <summary>Far clip plane distance in meters. Default: 1000.0.</summary>
        public double FarPlane { get; set; } = 1000.0;
    }
}
```

**Line count:** ~30 lines.

**Why `Vector3D` instead of `System.Numerics.Vector3`?** The camera position on a virtual globe might be `(6378137.0, 0.0, 100.0)` -- the equator at 100 meters altitude. The difference between this and the target might be less than 1 meter. A 32-bit float cannot represent this difference accurately. By using `Vector3D` (double), we maintain sub-millimeter precision for all camera math. The conversion to 32-bit floats happens only when uploading uniforms to the GPU.

---

### SceneState.cs

The central class that computes all matrices and provides scene-wide uniforms (lighting, camera) to the shader pipeline. All matrix math is done in **double precision** on the CPU, then converted to `float` for GPU upload.

```csharp
// Geode.Rendering/SceneState.cs
//
// Computes view, projection, and model-view-projection matrices in double precision.
// Provides float-precision versions for GPU uniform upload.
//
// The double-precision computation is critical for virtual globes:
//   - Camera at (6378137, 0, 100) looking at (6378137, 1, 100)
//   - The view direction is (0, 1, 0) -- a 1-meter offset at 6 million meters
//   - In float: 6378137.0f + 1.0f == 6378137.0f (the 1m is lost!)
//   - In double: 6378137.0 + 1.0 == 6378138.0 (15 digits of precision)
//
// Book Chapter 3, Section 3.7.
// OpenGlobe: Source/Scene/SceneState.cs

using System;
using System.Numerics;
using Geode.Core;

namespace Geode.Rendering
{
    /// <summary>
    /// Scene-wide state: camera, lighting, and computed matrices.
    /// Matrices are computed in double precision and stored as both
    /// double[16] (for CPU math) and Matrix4x4 (for GPU upload).
    /// </summary>
    public class SceneState
    {
        // ---------------------------------------------------------------
        // Camera
        // ---------------------------------------------------------------

        /// <summary>The camera parameters.</summary>
        public CameraState Camera { get; set; } = new();

        // ---------------------------------------------------------------
        // Lighting
        // ---------------------------------------------------------------

        /// <summary>
        /// The sun position in world (ECEF) coordinates.
        /// Used for directional lighting in the fragment shader.
        /// Default: along +Y axis at a large distance.
        /// </summary>
        public Vector3D SunPosition { get; set; } = new(0, 100000000, 0);

        /// <summary>Diffuse light intensity [0, 1]. Default: 0.8.</summary>
        public float DiffuseIntensity { get; set; } = 0.8f;

        /// <summary>Specular light intensity [0, 1]. Default: 0.5.</summary>
        public float SpecularIntensity { get; set; } = 0.5f;

        /// <summary>Ambient light intensity [0, 1]. Default: 0.1.</summary>
        public float AmbientIntensity { get; set; } = 0.1f;

        /// <summary>Specular shininess exponent. Higher = tighter highlight. Default: 32.</summary>
        public float Shininess { get; set; } = 32.0f;

        // ---------------------------------------------------------------
        // Computed matrices (double precision)
        // ---------------------------------------------------------------

        /// <summary>
        /// Computes the view matrix (LookAt) in double precision.
        /// Returns a 16-element array in column-major order.
        /// </summary>
        public double[] ComputeViewMatrix()
        {
            Vector3D eye = Camera.Eye;
            Vector3D target = Camera.Target;
            Vector3D up = Camera.Up;

            // Forward direction: from eye toward target
            Vector3D forward = (target - eye).Normalize();

            // Right direction: perpendicular to forward and up
            Vector3D right = forward.Cross(up).Normalize();

            // True up: perpendicular to forward and right (orthogonal)
            Vector3D trueUp = right.Cross(forward);

            // View matrix in column-major order.
            // The matrix simultaneously rotates and translates:
            //   - Rotation: aligns world axes to camera axes
            //   - Translation: moves origin to camera position
            // The negation of forward is because OpenGL eye space looks down -Z.
            return new double[16]
            {
                right.X,     trueUp.X,    -forward.X,   0,
                right.Y,     trueUp.Y,    -forward.Y,   0,
                right.Z,     trueUp.Z,    -forward.Z,   0,
                -right.Dot(eye), -trueUp.Dot(eye), forward.Dot(eye), 1
            };
        }

        /// <summary>
        /// Computes the perspective projection matrix in double precision.
        /// Uses [0, 1] depth range (for glClipControl(GL_LOWER_LEFT, GL_ZERO_TO_ONE)).
        /// Returns a 16-element array in column-major order.
        /// </summary>
        public double[] ComputePerspectiveMatrix()
        {
            double fovY = Camera.FieldOfViewY;
            double aspect = Camera.AspectRatio;
            double near = Camera.NearPlane;
            double far = Camera.FarPlane;

            double tanHalfFov = Math.Tan(fovY / 2.0);

            // Perspective matrix for [0, 1] depth range.
            // This differs from the standard [-1, 1] matrix in row 2:
            //   Standard:  -(f+n)/(f-n)  and  -2fn/(f-n)
            //   [0,1]:     -f/(f-n)      and  -fn/(f-n)
            return new double[16]
            {
                1.0 / (aspect * tanHalfFov), 0,                  0,                    0,
                0,                           1.0 / tanHalfFov,   0,                    0,
                0,                           0,                  -far / (far - near),  -1,
                0,                           0,                  -(far * near) / (far - near), 0
            };
        }

        /// <summary>
        /// Computes the combined Model-View-Projection matrix in double precision.
        /// Since the model matrix is identity for globe rendering, this is just V * P.
        /// Returns a 16-element array in column-major order.
        /// </summary>
        public double[] ComputeModelViewPerspectiveMatrix()
        {
            double[] view = ComputeViewMatrix();
            double[] proj = ComputePerspectiveMatrix();
            return MultiplyMatrices(proj, view);
        }

        // ---------------------------------------------------------------
        // Float-precision properties for GPU upload
        // ---------------------------------------------------------------

        /// <summary>
        /// The view matrix as a System.Numerics.Matrix4x4 (float precision).
        /// Use this for GPU uniform upload.
        /// </summary>
        public Matrix4x4 ViewMatrix => ToMatrix4x4(ComputeViewMatrix());

        /// <summary>
        /// The projection matrix as a System.Numerics.Matrix4x4 (float precision).
        /// </summary>
        public Matrix4x4 PerspectiveMatrix => ToMatrix4x4(ComputePerspectiveMatrix());

        /// <summary>
        /// The MVP matrix as a System.Numerics.Matrix4x4 (float precision).
        /// </summary>
        public Matrix4x4 ModelViewPerspectiveMatrix => ToMatrix4x4(ComputeModelViewPerspectiveMatrix());

        /// <summary>
        /// Camera eye position as a float-precision Vector3 for GPU upload.
        /// </summary>
        public Vector3 CameraEyeFloat => new(
            (float)Camera.Eye.X,
            (float)Camera.Eye.Y,
            (float)Camera.Eye.Z);

        /// <summary>
        /// Packs diffuse, specular, ambient, and shininess into a single vec4
        /// for efficient uniform upload.
        /// </summary>
        public Vector4 DiffuseSpecularAmbientShininess => new(
            DiffuseIntensity,
            SpecularIntensity,
            AmbientIntensity,
            Shininess);

        // ---------------------------------------------------------------
        // Matrix math helpers (double precision)
        // ---------------------------------------------------------------

        /// <summary>
        /// Multiplies two 4x4 matrices in column-major order.
        /// Result = A * B (where A is applied after B in the transform chain).
        /// </summary>
        private static double[] MultiplyMatrices(double[] a, double[] b)
        {
            double[] result = new double[16];

            for (int col = 0; col < 4; col++)
            {
                for (int row = 0; row < 4; row++)
                {
                    double sum = 0;
                    for (int k = 0; k < 4; k++)
                    {
                        // Column-major: element at (row, col) is at index col*4 + row
                        sum += a[k * 4 + row] * b[col * 4 + k];
                    }
                    result[col * 4 + row] = sum;
                }
            }

            return result;
        }

        /// <summary>
        /// Converts a double[16] column-major matrix to a System.Numerics.Matrix4x4.
        /// This is where we lose precision -- going from 64-bit to 32-bit.
        /// For near-origin scenes this is fine. For planetary scale, RTE (Section 27)
        /// is needed to preserve precision.
        /// </summary>
        private static Matrix4x4 ToMatrix4x4(double[] m)
        {
            return new Matrix4x4(
                (float)m[0],  (float)m[1],  (float)m[2],  (float)m[3],
                (float)m[4],  (float)m[5],  (float)m[6],  (float)m[7],
                (float)m[8],  (float)m[9],  (float)m[10], (float)m[11],
                (float)m[12], (float)m[13], (float)m[14], (float)m[15]);
        }
    }
}
```

**Line count:** ~200 lines.

**Important precision note:** The `ToMatrix4x4` conversion is the point where double precision is lost. For scenes near the origin (like our test triangle), this is fine. For planetary-scale rendering, the Relative-to-Eye (RTE) technique from Section 27 subtracts the camera position *before* the conversion, so the float values represent small offsets rather than enormous absolute positions.

---

## Section 17: Draw State

*Corresponds to Book Chapter 3, Section 3.3*

*OpenGlobe source: `Source/Renderer/DrawState.cs`*

`DrawState` bundles everything needed for a single draw call: the render state configuration, the shader program, and the vertex array. This is the central pattern from the book -- instead of setting state piecemeal, the application describes a complete draw call as a single object.

This file must come after Sections 11, 12, and 15 because it references `ShaderProgram`, `VertexArrayObject`, and `RenderState`.

```csharp
// Geode.Rendering/DrawState.cs
//
// Bundles the three things needed for a draw call:
//   1. RenderState  -- how to configure the GPU pipeline
//   2. ShaderProgram -- the compiled GLSL program
//   3. VertexArrayObject -- the vertex + index data
//
// This pattern eliminates the class of bugs where state from one draw call
// leaks into the next. The RenderContext applies the entire DrawState
// atomically before each draw.
//
// Book Chapter 3, Section 3.3.
// OpenGlobe: Source/Renderer/DrawState.cs

namespace Geode.Rendering
{
    /// <summary>
    /// Everything needed for a single draw call: pipeline state, shader, and geometry.
    /// </summary>
    public class DrawState
    {
        /// <summary>GPU pipeline configuration (depth test, culling, blending, etc.).</summary>
        public RenderState RenderState { get; set; }

        /// <summary>The compiled GLSL shader program.</summary>
        public ShaderProgram ShaderProgram { get; set; }

        /// <summary>The vertex array (VBO + EBO + attribute layout).</summary>
        public VertexArrayObject VertexArrayObject { get; set; }

        /// <summary>
        /// Creates a DrawState with all components specified.
        /// </summary>
        /// <param name="renderState">GPU pipeline configuration.</param>
        /// <param name="shaderProgram">Compiled shader program.</param>
        /// <param name="vertexArrayObject">Vertex array with geometry data.</param>
        public DrawState(RenderState renderState, ShaderProgram shaderProgram,
            VertexArrayObject vertexArrayObject)
        {
            RenderState = renderState;
            ShaderProgram = shaderProgram;
            VertexArrayObject = vertexArrayObject;
        }

        /// <summary>
        /// Creates a DrawState with default render state.
        /// Convenience constructor for simple cases where the default
        /// (depth on, backface culling, no blending) is appropriate.
        /// </summary>
        /// <param name="shaderProgram">Compiled shader program.</param>
        /// <param name="vertexArrayObject">Vertex array with geometry data.</param>
        public DrawState(ShaderProgram shaderProgram, VertexArrayObject vertexArrayObject)
            : this(new RenderState(), shaderProgram, vertexArrayObject)
        {
        }
    }
}
```

**Line count:** ~40 lines.

**Why does `DrawState` not own its components?** A single `ShaderProgram` might be shared across many draw states (all terrain tiles use the same globe shader). A single `RenderState` might be shared across transparent objects. By not owning the components, `DrawState` is a lightweight reference bundle that does not impose lifetime constraints.

---

## Section 18: The Render Context

*Corresponds to Book Chapter 3, Sections 3.8-3.10*

*OpenGlobe source: `Source/Renderer/GL3x/ContextGL3x.cs`*

The `RenderContext` is the central GPU interface. It holds the `GL` instance, manages a shadow copy of the current render state, and provides `Clear()` and `Draw()` methods that apply state and issue draw calls.

### State Shadowing in Detail

Every time the renderer changes a GL state value, it records the new value in a shadow `RenderState`. Before the next draw call, it compares the desired `RenderState` against the shadow. Only values that differ generate GL calls. This is critical for performance:

- A globe renderer draws hundreds of terrain tiles per frame.
- Most tiles use identical render state (depth on, backface culling, same winding).
- Without shadowing, each tile would redundantly set ~10 GL state values.
- With shadowing, only the first tile sets state; subsequent tiles generate zero state-change calls.

### Debug Output

OpenGL 4.6 includes `GL_KHR_debug` as a core feature. When enabled, the driver reports errors, performance warnings, and deprecation notices through a callback. This is invaluable during development -- instead of checking `glGetError()` after every call, the driver tells you exactly what went wrong and where.

> **3.3 vs 4.6 -- Debug Output**
>
> In OpenGL 3.3, debug output requires the `GL_ARB_debug_output` extension, which not all drivers support. In 4.6, `GL_KHR_debug` is core -- every conformant driver must support it. We enable it unconditionally in `RenderContext`.

### Complete Source

```csharp
// Geode.Rendering/RenderContext.cs
//
// The central GPU interface: applies render state, clears, and issues draw calls.
// Maintains a shadow copy of the current GL state to minimize redundant calls.
//
// Key responsibilities:
//   1. Enable debug output (GL_KHR_debug) for error reporting
//   2. Set glClipControl for [0,1] depth range
//   3. Clear the framebuffer (color, depth, stencil)
//   4. Apply render state with full shadowing
//   5. Bind shader + VAO, set scene uniforms, issue glDrawElements
//
// Book Chapter 3, Sections 3.8-3.10.
// OpenGlobe: Source/Renderer/GL3x/ContextGL3x.cs

using Silk.NET.OpenGL;
using System;
using System.Numerics;

namespace Geode.Rendering
{
    /// <summary>
    /// The rendering context: clears buffers, applies state, and issues draw calls.
    /// </summary>
    public class RenderContext : IDisposable
    {
        private readonly GL _gl;

        // Shadow state: tracks what the GPU currently has set.
        // We compare desired state against this to avoid redundant GL calls.
        private RenderState _shadowState;

        // ---------------------------------------------------------------
        // Construction
        // ---------------------------------------------------------------

        /// <summary>
        /// Creates a render context, enables debug output, and sets clip control.
        /// </summary>
        /// <param name="gl">The Silk.NET OpenGL context (from the window).</param>
        public RenderContext(GL gl)
        {
            _gl = gl;

            // Enable debug output so the driver reports errors and warnings
            // through a callback instead of requiring manual glGetError checks.
            _gl.Enable(EnableCap.DebugOutput);
            _gl.Enable(EnableCap.DebugOutputSynchronous);
            _gl.DebugMessageCallback(DebugCallback, IntPtr.Zero);

            // Set clip control for [0, 1] depth range.
            // This changes the NDC z mapping from [-1, 1] to [0, 1],
            // enabling proper reversed-Z in Section 28.
            // GL_LOWER_LEFT: origin at lower-left (OpenGL convention).
            // GL_ZERO_TO_ONE: depth range [0, 1] instead of [-1, 1].
            _gl.ClipControl(ClipControlOrigin.LowerLeft, ClipControlDepth.ZeroToOne);

            // Initialize shadow state with defaults and force-apply to GPU.
            // This ensures the GPU state matches our shadow from the start.
            _shadowState = new RenderState();
            ForceApplyRenderState(_shadowState);
        }

        /// <summary>
        /// Debug callback invoked by the GL driver for errors, warnings, and info.
        /// </summary>
        private static void DebugCallback(GLEnum source, GLEnum type, int id,
            GLEnum severity, int length, nint message, nint userParam)
        {
            // Decode the message string from the native pointer
            string msg = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(message)
                ?? "(null)";

            // Filter out notification-level messages (very noisy)
            if (severity == GLEnum.DebugSeverityNotification)
                return;

            string severityStr = severity switch
            {
                GLEnum.DebugSeverityHigh => "HIGH",
                GLEnum.DebugSeverityMedium => "MEDIUM",
                GLEnum.DebugSeverityLow => "LOW",
                _ => "INFO"
            };

            Console.WriteLine($"[GL {severityStr}] {msg}");
        }

        // ---------------------------------------------------------------
        // Clear
        // ---------------------------------------------------------------

        /// <summary>
        /// Clears the framebuffer according to the given clear state.
        /// Temporarily overrides color mask and depth mask to ensure clearing works,
        /// then restores the shadow state values.
        /// </summary>
        /// <param name="clearState">What to clear and to what values.</param>
        public void Clear(ClearState clearState)
        {
            // Set clear values
            _gl.ClearColor(clearState.Color.X, clearState.Color.Y,
                clearState.Color.Z, clearState.Color.W);
            _gl.ClearDepth(clearState.Depth);
            _gl.ClearStencil(clearState.Stencil);

            // Temporarily enable full color + depth writes so the clear works.
            // If the current shadow state has writes disabled, the clear would
            // silently do nothing without this override.
            _gl.ColorMask(
                clearState.ColorMask.Red,
                clearState.ColorMask.Green,
                clearState.ColorMask.Blue,
                clearState.ColorMask.Alpha);
            _gl.DepthMask(clearState.DepthMask);

            // Build the clear mask from the ClearBuffers flags
            ClearBufferMask mask = 0;
            if (clearState.Buffers.HasFlag(ClearBuffers.ColorBuffer))
                mask |= ClearBufferMask.ColorBufferBit;
            if (clearState.Buffers.HasFlag(ClearBuffers.DepthBuffer))
                mask |= ClearBufferMask.DepthBufferBit;
            if (clearState.Buffers.HasFlag(ClearBuffers.StencilBuffer))
                mask |= ClearBufferMask.StencilBufferBit;

            _gl.Clear((uint)mask);

            // Restore shadow state for color mask and depth mask
            // (so subsequent draws use the correct values)
            _gl.ColorMask(
                _shadowState.ColorMask.Red,
                _shadowState.ColorMask.Green,
                _shadowState.ColorMask.Blue,
                _shadowState.ColorMask.Alpha);
            _gl.DepthMask(_shadowState.DepthMask.Enabled);
        }

        // ---------------------------------------------------------------
        // Draw (indexed)
        // ---------------------------------------------------------------

        /// <summary>
        /// Applies render state, binds shader and VAO, pushes automatic uniforms,
        /// and issues an indexed draw call (glDrawElements).
        /// </summary>
        /// <param name="primitiveType">Triangle, line, point, etc.</param>
        /// <param name="drawState">Shader + VAO + render state bundle.</param>
        /// <param name="sceneState">Camera, lighting, and computed matrices.</param>
        public unsafe void Draw(PrimitiveType primitiveType, DrawState drawState,
            SceneState sceneState)
        {
            // 1. Apply render state (shadow comparison)
            ApplyRenderState(drawState.RenderState);

            // 2. Depth-required rule (Section 19.5)
            if (drawState.RenderState.DepthTest.Enabled
                && _currentFramebuffer is not null
                && !_currentFramebuffer.HasDepthAttachment)
            {
                throw new InvalidOperationException(
                    "DepthTest is enabled but the current framebuffer has no depth attachment.");
            }

            // 3. Bind the shader. This does three things under the hood (Section 19):
            //    a. glUseProgram
            //    b. Evaluate every DrawAutomaticUniform, writing into its Uniform<T>.Value
            //       (which may or may not dirty the uniform depending on value change)
            //    c. Flush the dirty-uniform list to the GPU via glProgramUniform*
            drawState.ShaderProgram.Bind(this, drawState, sceneState);

            // 4. Bind VAO
            _gl.BindVertexArray(drawState.VertexArrayObject.Handle);

            // 5. Issue draw call
            _gl.DrawElements(primitiveType,
                (uint)drawState.VertexArrayObject.IndexCount,
                DrawElementsType.UnsignedInt, null);
        }

        /// <summary>
        /// Non-indexed draw call (glDrawArrays). Used for simple geometry
        /// that does not share vertices.
        /// </summary>
        /// <param name="primitiveType">Triangle, line, point, etc.</param>
        /// <param name="drawState">Shader + VAO + render state bundle.</param>
        /// <param name="sceneState">Camera, lighting, and computed matrices.</param>
        /// <param name="first">Starting vertex index.</param>
        /// <param name="count">Number of vertices to draw.</param>
        public void DrawArrays(PrimitiveType primitiveType, DrawState drawState,
            SceneState sceneState, uint first, uint count)
        {
            ApplyRenderState(drawState.RenderState);
            drawState.ShaderProgram.Bind(this, drawState, sceneState);
            _gl.BindVertexArray(drawState.VertexArrayObject.Handle);
            _gl.DrawArrays(primitiveType, (int)first, count);
        }

        /// <summary>
        /// Converts a Matrix4x4 to a float[16] in column-major order for glUniformMatrix4fv.
        /// </summary>
        private static float[] Matrix4x4ToArray(Matrix4x4 m)
        {
            return new float[16]
            {
                m.M11, m.M21, m.M31, m.M41,
                m.M12, m.M22, m.M32, m.M42,
                m.M13, m.M23, m.M33, m.M43,
                m.M14, m.M24, m.M34, m.M44
            };
        }

        // ---------------------------------------------------------------
        // Render state application (with shadowing)
        // ---------------------------------------------------------------

        /// <summary>
        /// Applies the desired render state, comparing against the shadow state
        /// to issue only the GL calls that actually change a value.
        /// </summary>
        private void ApplyRenderState(RenderState desired)
        {
            ApplyDepthTest(desired.DepthTest);
            ApplyFacetCulling(desired.FacetCulling);
            ApplyBlending(desired.Blending);
            ApplyDepthRange(desired.DepthRange);
            ApplyDepthMask(desired.DepthMask);
            ApplyColorMask(desired.ColorMask);
            ApplyScissorTest(desired.ScissorTest);
            ApplyRasterizationMode(desired.RasterizationMode);
        }

        /// <summary>
        /// Force-applies all render state to the GPU without shadow comparison.
        /// Used at initialization to synchronize the shadow with the actual GPU state.
        /// </summary>
        private void ForceApplyRenderState(RenderState state)
        {
            // Depth test
            if (state.DepthTest.Enabled)
                _gl.Enable(EnableCap.DepthTest);
            else
                _gl.Disable(EnableCap.DepthTest);
            _gl.DepthFunc(ToGlDepthFunction(state.DepthTest.Function));

            // Facet culling
            if (state.FacetCulling.Enabled)
                _gl.Enable(EnableCap.CullFace);
            else
                _gl.Disable(EnableCap.CullFace);
            _gl.CullFace(ToGlCullFace(state.FacetCulling.Face));
            _gl.FrontFace(ToGlFrontFace(state.FacetCulling.FrontFaceWindingOrder));

            // Blending
            if (state.Blending.Enabled)
                _gl.Enable(EnableCap.Blend);
            else
                _gl.Disable(EnableCap.Blend);
            _gl.BlendFunc(
                ToGlBlendFactor(state.Blending.SourceFactor),
                ToGlBlendFactor(state.Blending.DestinationFactor));

            // Depth range
            _gl.DepthRange(state.DepthRange.Near, state.DepthRange.Far);

            // Depth mask
            _gl.DepthMask(state.DepthMask.Enabled);

            // Color mask
            _gl.ColorMask(
                state.ColorMask.Red,
                state.ColorMask.Green,
                state.ColorMask.Blue,
                state.ColorMask.Alpha);

            // Scissor test
            if (state.ScissorTest.Enabled)
                _gl.Enable(EnableCap.ScissorTest);
            else
                _gl.Disable(EnableCap.ScissorTest);
            _gl.Scissor(
                state.ScissorTest.X,
                state.ScissorTest.Y,
                (uint)state.ScissorTest.Width,
                (uint)state.ScissorTest.Height);

            // Rasterization mode
            _gl.PolygonMode(
                TriangleFace.FrontAndBack,
                ToGlPolygonMode(state.RasterizationMode));
        }

        // ---------------------------------------------------------------
        // Individual state applicators (with shadow comparison)
        // ---------------------------------------------------------------

        private void ApplyDepthTest(DepthTest desired)
        {
            DepthTest shadow = _shadowState.DepthTest;

            if (desired.Enabled != shadow.Enabled)
            {
                if (desired.Enabled)
                    _gl.Enable(EnableCap.DepthTest);
                else
                    _gl.Disable(EnableCap.DepthTest);
                shadow.Enabled = desired.Enabled;
            }

            if (desired.Function != shadow.Function)
            {
                _gl.DepthFunc(ToGlDepthFunction(desired.Function));
                shadow.Function = desired.Function;
            }
        }

        private void ApplyFacetCulling(FacetCulling desired)
        {
            FacetCulling shadow = _shadowState.FacetCulling;

            if (desired.Enabled != shadow.Enabled)
            {
                if (desired.Enabled)
                    _gl.Enable(EnableCap.CullFace);
                else
                    _gl.Disable(EnableCap.CullFace);
                shadow.Enabled = desired.Enabled;
            }

            if (desired.Face != shadow.Face)
            {
                _gl.CullFace(ToGlCullFace(desired.Face));
                shadow.Face = desired.Face;
            }

            if (desired.FrontFaceWindingOrder != shadow.FrontFaceWindingOrder)
            {
                _gl.FrontFace(ToGlFrontFace(desired.FrontFaceWindingOrder));
                shadow.FrontFaceWindingOrder = desired.FrontFaceWindingOrder;
            }
        }

        private void ApplyBlending(Blending desired)
        {
            Blending shadow = _shadowState.Blending;

            if (desired.Enabled != shadow.Enabled)
            {
                if (desired.Enabled)
                    _gl.Enable(EnableCap.Blend);
                else
                    _gl.Disable(EnableCap.Blend);
                shadow.Enabled = desired.Enabled;
            }

            if (desired.SourceFactor != shadow.SourceFactor ||
                desired.DestinationFactor != shadow.DestinationFactor)
            {
                _gl.BlendFunc(
                    ToGlBlendFactor(desired.SourceFactor),
                    ToGlBlendFactor(desired.DestinationFactor));
                shadow.SourceFactor = desired.SourceFactor;
                shadow.DestinationFactor = desired.DestinationFactor;
            }
        }

        private void ApplyDepthRange(DepthRange desired)
        {
            DepthRange shadow = _shadowState.DepthRange;

            if (desired.Near != shadow.Near || desired.Far != shadow.Far)
            {
                _gl.DepthRange(desired.Near, desired.Far);
                shadow.Near = desired.Near;
                shadow.Far = desired.Far;
            }
        }

        private void ApplyDepthMask(DepthMask desired)
        {
            DepthMask shadow = _shadowState.DepthMask;

            if (desired.Enabled != shadow.Enabled)
            {
                _gl.DepthMask(desired.Enabled);
                shadow.Enabled = desired.Enabled;
            }
        }

        private void ApplyColorMask(ColorMask desired)
        {
            ColorMask shadow = _shadowState.ColorMask;

            if (desired.Red != shadow.Red ||
                desired.Green != shadow.Green ||
                desired.Blue != shadow.Blue ||
                desired.Alpha != shadow.Alpha)
            {
                _gl.ColorMask(desired.Red, desired.Green, desired.Blue, desired.Alpha);
                shadow.Red = desired.Red;
                shadow.Green = desired.Green;
                shadow.Blue = desired.Blue;
                shadow.Alpha = desired.Alpha;
            }
        }

        private void ApplyScissorTest(ScissorTest desired)
        {
            ScissorTest shadow = _shadowState.ScissorTest;

            if (desired.Enabled != shadow.Enabled)
            {
                if (desired.Enabled)
                    _gl.Enable(EnableCap.ScissorTest);
                else
                    _gl.Disable(EnableCap.ScissorTest);
                shadow.Enabled = desired.Enabled;
            }

            if (desired.X != shadow.X || desired.Y != shadow.Y ||
                desired.Width != shadow.Width || desired.Height != shadow.Height)
            {
                _gl.Scissor(desired.X, desired.Y,
                    (uint)desired.Width, (uint)desired.Height);
                shadow.X = desired.X;
                shadow.Y = desired.Y;
                shadow.Width = desired.Width;
                shadow.Height = desired.Height;
            }
        }

        private void ApplyRasterizationMode(RasterizationMode desired)
        {
            if (desired != _shadowState.RasterizationMode)
            {
                _gl.PolygonMode(TriangleFace.FrontAndBack, ToGlPolygonMode(desired));
                _shadowState.RasterizationMode = desired;
            }
        }

        // ---------------------------------------------------------------
        // Enum conversion helpers
        // ---------------------------------------------------------------

        private static DepthFunction ToGlDepthFunction(DepthTestFunction f) => f switch
        {
            DepthTestFunction.Never => DepthFunction.Never,
            DepthTestFunction.Less => DepthFunction.Less,
            DepthTestFunction.Equal => DepthFunction.Equal,
            DepthTestFunction.LessThanOrEqual => DepthFunction.Lequal,
            DepthTestFunction.Greater => DepthFunction.Greater,
            DepthTestFunction.NotEqual => DepthFunction.Notequal,
            DepthTestFunction.GreaterThanOrEqual => DepthFunction.Gequal,
            DepthTestFunction.Always => DepthFunction.Always,
            _ => DepthFunction.Less
        };

        private static TriangleFace ToGlCullFace(CullFace f) => f switch
        {
            CullFace.Front => TriangleFace.Front,
            CullFace.Back => TriangleFace.Back,
            CullFace.FrontAndBack => TriangleFace.FrontAndBack,
            _ => TriangleFace.Back
        };

        private static FrontFaceDirection ToGlFrontFace(WindingOrder w) => w switch
        {
            WindingOrder.Clockwise => FrontFaceDirection.CW,
            WindingOrder.CounterClockwise => FrontFaceDirection.Ccw,
            _ => FrontFaceDirection.Ccw
        };

        private static BlendingFactor ToGlBlendFactor(Rendering.BlendingFactor f) => f switch
        {
            Rendering.BlendingFactor.Zero => Silk.NET.OpenGL.BlendingFactor.Zero,
            Rendering.BlendingFactor.One => Silk.NET.OpenGL.BlendingFactor.One,
            Rendering.BlendingFactor.SourceAlpha => Silk.NET.OpenGL.BlendingFactor.SrcAlpha,
            Rendering.BlendingFactor.OneMinusSourceAlpha => Silk.NET.OpenGL.BlendingFactor.OneMinusSrcAlpha,
            Rendering.BlendingFactor.DestinationAlpha => Silk.NET.OpenGL.BlendingFactor.DstAlpha,
            Rendering.BlendingFactor.OneMinusDestinationAlpha => Silk.NET.OpenGL.BlendingFactor.OneMinusDstAlpha,
            Rendering.BlendingFactor.SourceColor => Silk.NET.OpenGL.BlendingFactor.SrcColor,
            Rendering.BlendingFactor.OneMinusSourceColor => Silk.NET.OpenGL.BlendingFactor.OneMinusSrcColor,
            Rendering.BlendingFactor.DestinationColor => Silk.NET.OpenGL.BlendingFactor.DstColor,
            Rendering.BlendingFactor.OneMinusDestinationColor => Silk.NET.OpenGL.BlendingFactor.OneMinusDstColor,
            _ => Silk.NET.OpenGL.BlendingFactor.One
        };

        private static PolygonMode ToGlPolygonMode(RasterizationMode m) => m switch
        {
            RasterizationMode.Fill => PolygonMode.Fill,
            RasterizationMode.Line => PolygonMode.Line,
            RasterizationMode.Point => PolygonMode.Point,
            _ => PolygonMode.Fill
        };

        // ---------------------------------------------------------------
        // Disposal
        // ---------------------------------------------------------------

        /// <summary>
        /// Disposes the render context. Currently a no-op (the GL context
        /// is owned by the window), but reserved for future cleanup.
        /// </summary>
        public void Dispose()
        {
            // The GL context is owned by the window, not by us.
            // This method exists for consistency and future use.
        }
    }
}
```

**Line count:** ~500 lines.

### Namespace Disambiguation: BlendingFactor

The `ToGlBlendFactor` method uses fully qualified names (`Rendering.BlendingFactor` and `Silk.NET.OpenGL.BlendingFactor`) because both our enum and Silk.NET define a type with the same name. This is the only place where the naming collision surfaces -- everywhere else, the Silk.NET types are accessed through the `GL` object and our enums are used in `RenderState` without ambiguity.

### A Note on Framebuffers

The book (Section 3.6) covers Framebuffer Objects (FBOs) for off-screen rendering. We defer FBOs until they are needed -- specifically, when we implement multi-pass rendering for atmospheric effects and post-processing in later parts. The default framebuffer (the window's backbuffer) is sufficient for everything in Parts III and IV.

---

## Section 19: The Automatic Uniform System

*Corresponds to Book Chapter 3, Section 3.4.4 (Uniforms) and Section 3.4.5 (Automatic Uniforms)*

*OpenGlobe source: `Source/Renderer/Shaders/Uniform.cs`, `Uniform{T}.cs`, `UniformType.cs`, `Source/Renderer/GL3x/Shaders/UniformFloatMatrix44GL3x.cs` (and siblings), `Source/Renderer/DrawAutomaticUniforms/`, `Source/Renderer/LinkAutomaticUniforms/`, `Source/Renderer/AutomaticUniformFactoryCollection.cs`.*

This section ports the book's full uniform architecture. It is a three-tier design -- a typed uniform value cache, a dirty-list upload mechanism, and an automatic-uniform factory system -- that together replace the naive "call `glUniform*` for every uniform every frame" pattern.

*Files we build in this section:*

```
Geode.Rendering/Uniforms/
  UniformType.cs                              -- enum for all GLSL uniform types
  ICleanable.cs                               -- anything with pending GPU work
  ICleanableObserver.cs                       -- callback for dirty notification
  Uniform.cs                                  -- abstract base
  Uniform{T}.cs                               -- generic, holds Value + dirties on change
  UniformCollection.cs                        -- named collection on ShaderProgram
  GL/UniformFloatGL.cs                        -- concrete: float
  GL/UniformFloatVector2GL.cs                 -- concrete: vec2
  GL/UniformFloatVector3GL.cs                 -- concrete: vec3
  GL/UniformFloatVector4GL.cs                 -- concrete: vec4
  GL/UniformFloatMatrix33GL.cs                -- concrete: mat3
  GL/UniformFloatMatrix44GL.cs                -- concrete: mat4
  GL/UniformIntGL.cs                          -- concrete: int / sampler*
  LinkAutomaticUniform.cs                     -- abstract base
  LinkAutomaticUniforms/
    TextureUniform.cs                         -- og_texture0..7 -> unit 0..7
  DrawAutomaticUniform.cs                     -- abstract base (set per draw)
  DrawAutomaticUniformFactory.cs              -- abstract factory
  DrawAutomaticUniforms/
    ModelViewPerspectiveMatrixUniform.cs      -- og_modelViewPerspectiveMatrix
    ViewMatrixUniform.cs                      -- og_viewMatrix
    PerspectiveMatrixUniform.cs               -- og_perspectiveMatrix
    CameraEyeUniform.cs                       -- og_cameraEye
    CameraLightPositionUniform.cs             -- og_cameraLightPosition
    SunPositionUniform.cs                     -- og_sunPosition
    DiffuseSpecularAmbientShininessUniform.cs -- og_diffuseSpecularAmbientShininess
    ViewportUniform.cs                        -- og_viewport
    InverseViewportUniform.cs                 -- og_inverseViewport
    PixelSizePerDistanceUniform.cs            -- og_pixelSizePerDistance
    Wgs84HeightUniform.cs                     -- og_wgs84Height
  AutomaticUniformFactoryCollection.cs        -- the registry (lives on Device)
```

Plus modifications to `Geode.Rendering/ShaderProgram.cs` (adds the `Uniforms` collection, dirty list, `Clean`, `InitializeAutomaticUniforms`, `Bind`) and `Geode.Rendering/RenderContext.cs` (simplified `Draw`).

### Architecture at a glance

```
Application code
   │
   │  ((Uniform<Matrix4x4>)sp.Uniforms["u_modelMatrix"]).Value = m   (manual, per draw)
   │
   ▼
Uniform<T>.Value setter
   │
   │  1. compare new value to cached; return if identical
   │  2. store new value in _value field
   │  3. if not already dirty: mark dirty, notify owning ShaderProgram
   │
   ▼
ShaderProgram._dirtyUniforms  (List<ICleanable>)
   │
   │  next time RenderContext.Draw fires, after glUseProgram:
   │    foreach (ICleanable c in _dirtyUniforms) c.Clean();
   │    _dirtyUniforms.Clear();
   │
   ▼
UniformFloatMatrix44GL.Clean()  ──> glProgramUniformMatrix4fv(...)
```

Draw-automatic uniforms feed into the same pipeline:

```
RenderContext.Draw(drawState, sceneState)
   │
   │  applies render state
   │  calls drawState.ShaderProgram.Bind(this, drawState, sceneState)
   │
   ▼
ShaderProgram.Bind
   │
   │  glUseProgram
   │  foreach (DrawAutomaticUniform u in _drawAutomaticUniforms)
   │      u.Set(context, drawState, sceneState)   // calls _uniform.Value = ...
   │                                              // which dirties via observer
   │  foreach (ICleanable c in _dirtyUniforms) c.Clean();
   │  _dirtyUniforms.Clear();
   │
   ▼
Bind VAO, glDrawElements/glDrawArrays
```

The key property: **one GL call per uniform per frame, regardless of how many times client code sets the value**. Setting `uniform.Value = m` a hundred times in the same frame from different places (shared matrix, redundant assignment) still results in exactly one `glProgramUniformMatrix4fv` call.

### Tier 1: `UniformType` enum

The full set of GLSL uniform types the book covers. `UniformType` values map directly onto `GL_FLOAT`, `GL_FLOAT_VEC3`, `GL_FLOAT_MAT4`, etc. that `glGetActiveUniform` returns. Silk.NET already has a `UniformType` enum; we wrap it in our own so the uniform subsystem doesn't leak GL enums into higher layers.

```csharp
// Geode.Rendering/Uniforms/UniformType.cs
//
// Enumeration of every GLSL uniform type we handle.
// The values are the GL token numbers so casts to/from Silk.NET's
// UniformType are identity -- but we keep a Geode-side type so the
// uniform classes don't reference Silk.NET.OpenGL enums directly.

namespace Geode.Rendering.Uniforms
{
    public enum UniformType : uint
    {
        // Scalars
        Float            = 0x1406,  // GL_FLOAT
        Int              = 0x1404,  // GL_INT
        UnsignedInt      = 0x1405,  // GL_UNSIGNED_INT
        Bool             = 0x8B56,  // GL_BOOL

        // Float vectors
        FloatVector2     = 0x8B50,  // GL_FLOAT_VEC2
        FloatVector3     = 0x8B51,  // GL_FLOAT_VEC3
        FloatVector4     = 0x8B52,  // GL_FLOAT_VEC4

        // Int vectors
        IntVector2       = 0x8B53,
        IntVector3       = 0x8B54,
        IntVector4       = 0x8B55,

        // Bool vectors
        BoolVector2      = 0x8B57,
        BoolVector3      = 0x8B58,
        BoolVector4      = 0x8B59,

        // Square matrices
        FloatMatrix22    = 0x8B5A,
        FloatMatrix33    = 0x8B5B,
        FloatMatrix44    = 0x8B5C,

        // Non-square matrices (rarely used but part of GLSL)
        FloatMatrix23    = 0x8B65,
        FloatMatrix24    = 0x8B66,
        FloatMatrix32    = 0x8B67,
        FloatMatrix34    = 0x8B6C,
        FloatMatrix42    = 0x8B69,
        FloatMatrix43    = 0x8B6B,

        // Samplers (all treated as Int for upload; the value is the texture unit)
        Sampler1D        = 0x8B5D,
        Sampler2D        = 0x8B5E,
        Sampler3D        = 0x8B5F,
        SamplerCube      = 0x8B60,
        Sampler2DArray   = 0x8DC1,
        Sampler2DShadow  = 0x8B62,
    }
}
```

### Tier 2: `ICleanable` and `ICleanableObserver`

An `ICleanable` is anything with a pending GPU upload. A `Uniform<T>` whose CPU-side value was just changed is the canonical example. The `ICleanableObserver` is the interface `ShaderProgram` implements so uniforms can tell it "I'm dirty, queue me for the next `Clean()`."

```csharp
// Geode.Rendering/Uniforms/ICleanable.cs

namespace Geode.Rendering.Uniforms
{
    /// <summary>
    /// An object with pending GPU work. ShaderProgram aggregates dirty cleanables
    /// and flushes them with Clean() immediately before a draw.
    /// </summary>
    public interface ICleanable
    {
        void Clean();
    }
}
```

```csharp
// Geode.Rendering/Uniforms/ICleanableObserver.cs

namespace Geode.Rendering.Uniforms
{
    /// <summary>
    /// Receives "I'm dirty" notifications from owned cleanables (typically Uniforms).
    /// ShaderProgram implements this; it appends the cleanable to its dirty list
    /// so the next draw flushes the change.
    /// </summary>
    public interface ICleanableObserver
    {
        void NotifyDirty(ICleanable cleanable);
    }
}
```

### Tier 2: `Uniform` (abstract base)

Holds metadata only: name and GLSL type. Doesn't touch the value because concrete subclasses are strongly typed via the generic `Uniform<T>`.

```csharp
// Geode.Rendering/Uniforms/Uniform.cs

namespace Geode.Rendering.Uniforms
{
    /// <summary>
    /// Abstract base for one GLSL uniform in a linked shader program.
    /// Clients do not use this type directly -- cast to <see cref="Uniform{T}"/>
    /// to read or write the value.
    /// </summary>
    public abstract class Uniform : ICleanable
    {
        /// <summary>The GLSL uniform name as declared in the shader source.</summary>
        public string Name { get; }

        /// <summary>The GLSL type. Used to dispatch to the correct concrete subclass.</summary>
        public UniformType Datatype { get; }

        protected Uniform(string name, UniformType datatype)
        {
            Name = name;
            Datatype = datatype;
        }

        /// <summary>Flush this uniform's cached value to the GPU. Called by ShaderProgram.</summary>
        public abstract void Clean();
    }
}
```

### Tier 2: `Uniform<T>` (generic with caching setter)

This is where the dirty-list mechanism lives. Setting `Value` to something different from the cached value marks the uniform dirty and notifies the owning program. Setting it to the same value is a no-op.

```csharp
// Geode.Rendering/Uniforms/Uniform{T}.cs

using System.Collections.Generic;

namespace Geode.Rendering.Uniforms
{
    /// <summary>
    /// A strongly typed uniform. Concrete subclasses (<see cref="GL.UniformFloatMatrix44GL"/>, etc.)
    /// override <see cref="Uniform.Clean"/> to call the appropriate glUniform* function.
    /// </summary>
    /// <typeparam name="T">CPU-side value type (float, Vector3, Matrix4x4, int, ...).</typeparam>
    public abstract class Uniform<T> : Uniform
    {
        private T _value;
        private readonly ICleanableObserver _observer;
        private bool _dirty;

        protected Uniform(string name, UniformType datatype, ICleanableObserver observer)
            : base(name, datatype)
        {
            _observer = observer;
            _value = default!;
            // Mark dirty up front so the first draw pushes the initial value.
            _dirty = true;
            observer.NotifyDirty(this);
        }

        /// <summary>
        /// The cached CPU-side value. Setting a different value marks the uniform dirty
        /// (scheduled for GPU upload before the next draw). Setting the same value is a no-op.
        /// </summary>
        public T Value
        {
            get => _value;
            set
            {
                if (EqualityComparer<T>.Default.Equals(_value, value))
                    return;

                _value = value;

                if (!_dirty)
                {
                    _dirty = true;
                    _observer.NotifyDirty(this);
                }
            }
        }

        /// <summary>For concrete subclasses: read the cached value without a dirty check.</summary>
        protected T CurrentValue => _value;

        /// <summary>For concrete subclasses: clear the dirty flag after a successful upload.</summary>
        protected void MarkClean() => _dirty = false;
    }
}
```

### Tier 2: Concrete GL implementations

One per GLSL type. They are small and mechanical -- each overrides `Clean()` to call the matching `glProgramUniform*` function. Using `glProgramUniform*` (core since 4.1) instead of `glUniform*` means Clean works regardless of which program is currently bound, which simplifies the design.

**Template (mat4):**

```csharp
// Geode.Rendering/Uniforms/GL/UniformFloatMatrix44GL.cs

using Silk.NET.OpenGL;
using System.Numerics;

namespace Geode.Rendering.Uniforms.GL
{
    /// <summary>
    /// A mat4 uniform. Value is a row-major System.Numerics.Matrix4x4;
    /// uploaded with transpose=true so glsl sees it in its native column-major layout.
    /// </summary>
    public sealed class UniformFloatMatrix44GL : Uniform<Matrix4x4>
    {
        private readonly Silk.NET.OpenGL.GL _gl;
        private readonly uint _program;
        private readonly int _location;

        public UniformFloatMatrix44GL(Silk.NET.OpenGL.GL gl, uint program,
            string name, int location, ICleanableObserver observer)
            : base(name, UniformType.FloatMatrix44, observer)
        {
            _gl = gl;
            _program = program;
            _location = location;
        }

        public override unsafe void Clean()
        {
            Matrix4x4 v = CurrentValue;
            _gl.ProgramUniformMatrix4(_program, _location, 1, true, (float*)&v);
            MarkClean();
        }
    }
}
```

**The rest follow the same pattern.** Only the `Clean` body differs:

| File | T | Datatype | `Clean` body |
|---|---|---|---|
| `UniformFloatGL.cs` | `float` | `Float` | `_gl.ProgramUniform1(_program, _location, CurrentValue);` |
| `UniformFloatVector2GL.cs` | `Vector2` | `FloatVector2` | `var v = CurrentValue; _gl.ProgramUniform2(_program, _location, v.X, v.Y);` |
| `UniformFloatVector3GL.cs` | `Vector3` | `FloatVector3` | `var v = CurrentValue; _gl.ProgramUniform3(_program, _location, v.X, v.Y, v.Z);` |
| `UniformFloatVector4GL.cs` | `Vector4` | `FloatVector4` | `var v = CurrentValue; _gl.ProgramUniform4(_program, _location, v.X, v.Y, v.Z, v.W);` |
| `UniformFloatMatrix33GL.cs` | `Matrix3x3` | `FloatMatrix33` | same as mat4 but `ProgramUniformMatrix3` |
| `UniformIntGL.cs` | `int` | `Int` / any `Sampler*` | `_gl.ProgramUniform1(_program, _location, CurrentValue);` |
| `UniformBoolGL.cs` | `bool` | `Bool` | `_gl.ProgramUniform1(_program, _location, CurrentValue ? 1 : 0);` |
| `UniformIntVector2GL.cs` | `(int, int)` | `IntVector2` | `var v = CurrentValue; _gl.ProgramUniform2(_program, _location, v.Item1, v.Item2);` |

Add the remaining permutations (ivec3/4, bvec2/3/4, non-square matrices) as you need them. Each is a 15-line file.

A `Matrix3x3` type is not in `System.Numerics`; use a local `readonly struct` with nine floats, or borrow one from Silk.NET.

### Tier 2: `UniformCollection`

A dictionary wrapper on `ShaderProgram` exposing all declared uniforms by name.

```csharp
// Geode.Rendering/Uniforms/UniformCollection.cs

using System.Collections;
using System.Collections.Generic;

namespace Geode.Rendering.Uniforms
{
    /// <summary>
    /// Named collection of <see cref="Uniform"/> objects owned by a <see cref="ShaderProgram"/>.
    /// Populated at link time by scanning active uniforms.
    /// </summary>
    public sealed class UniformCollection : IEnumerable<Uniform>
    {
        private readonly Dictionary<string, Uniform> _uniforms = new();

        /// <summary>Look up a uniform by its GLSL name. Throws if not found.</summary>
        public Uniform this[string name] => _uniforms[name];

        /// <summary>True if the shader declares a uniform with this name.</summary>
        public bool Contains(string name) => _uniforms.ContainsKey(name);

        /// <summary>Try to look up a uniform. Used by automatic-uniform initialization.</summary>
        public bool TryGet(string name, out Uniform? uniform) => _uniforms.TryGetValue(name, out uniform);

        internal void Add(Uniform uniform) => _uniforms.Add(uniform.Name, uniform);

        public IEnumerator<Uniform> GetEnumerator() => _uniforms.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
```

### Tier 3: Link-automatic uniforms

A `LinkAutomaticUniform` fires *once*, at shader link time, to set a value that depends only on the shader and never changes. The only production example in the book is texture-unit binding: `uniform sampler2D og_texture0` always binds to unit 0.

```csharp
// Geode.Rendering/Uniforms/LinkAutomaticUniform.cs

namespace Geode.Rendering.Uniforms
{
    /// <summary>
    /// A uniform whose value is determined solely by the shader it appears in,
    /// set once at link time. Contrasts with <see cref="DrawAutomaticUniform"/>
    /// which is set before every draw.
    /// </summary>
    public abstract class LinkAutomaticUniform
    {
        /// <summary>The GLSL uniform name this setter handles.</summary>
        public abstract string Name { get; }

        /// <summary>The GLSL type the uniform must have.</summary>
        public abstract UniformType Datatype { get; }

        /// <summary>Called once at link time. Implementations cast to the concrete Uniform{T} type.</summary>
        public abstract void Set(Uniform uniform);
    }
}
```

**Concrete: texture-unit bindings.** The `og_texture0..og_texture7` family binds sampler uniforms to their fixed texture units. OpenGlobe ships one class instance per unit; Geode can use a parameterized class and register eight instances.

```csharp
// Geode.Rendering/Uniforms/LinkAutomaticUniforms/TextureUniform.cs

namespace Geode.Rendering.Uniforms.LinkAutomaticUniforms
{
    /// <summary>
    /// Binds a sampler2D uniform named "og_textureN" to texture unit N.
    /// Register one instance per unit (N = 0..7) at startup.
    /// </summary>
    public sealed class TextureUniform : LinkAutomaticUniform
    {
        private readonly int _textureUnit;
        private readonly string _name;

        public TextureUniform(int textureUnit)
        {
            _textureUnit = textureUnit;
            _name = $"og_texture{textureUnit}";
        }

        public override string Name => _name;
        public override UniformType Datatype => UniformType.Sampler2D;

        public override void Set(Uniform uniform)
        {
            ((Uniform<int>)uniform).Value = _textureUnit;
        }
    }
}
```

### Tier 3: Draw-automatic uniforms

Set before every draw. Each `DrawAutomaticUniform` holds a reference to the specific `Uniform<T>` it targets and, in its `Set`, does `_uniform.Value = <something from SceneState>`. The assignment flows through `Uniform<T>.Value`'s setter, which dirties the uniform if the value changed. Then `ShaderProgram.Clean()` flushes.

**Factory.**

```csharp
// Geode.Rendering/Uniforms/DrawAutomaticUniformFactory.cs

namespace Geode.Rendering.Uniforms
{
    /// <summary>
    /// Builds a <see cref="DrawAutomaticUniform"/> bound to a specific uniform
    /// in a specific shader program. Consulted at link time by ShaderProgram.
    /// </summary>
    public abstract class DrawAutomaticUniformFactory
    {
        /// <summary>The GLSL uniform name this factory handles (e.g. "og_modelViewPerspectiveMatrix").</summary>
        public abstract string Name { get; }

        /// <summary>The GLSL type the uniform must have. Used for link-time validation.</summary>
        public abstract UniformType Datatype { get; }

        /// <summary>Create a setter bound to the given uniform. Called once per shader at link time.</summary>
        public abstract DrawAutomaticUniform Create(Uniform uniform);
    }
}
```

**Abstract base.**

```csharp
// Geode.Rendering/Uniforms/DrawAutomaticUniform.cs

namespace Geode.Rendering.Uniforms
{
    /// <summary>
    /// A setter invoked before every draw that pushes a scene-derived value
    /// into its captured <see cref="Uniform{T}"/>. The Value assignment flows
    /// through the dirty-list mechanism, so the GPU upload happens only if the
    /// value actually changed since the last draw.
    /// </summary>
    public abstract class DrawAutomaticUniform
    {
        /// <summary>Read from SceneState / DrawState and write to the captured Uniform{T}.Value.</summary>
        public abstract void Set(RenderContext context, DrawState drawState, SceneState sceneState);
    }
}
```

**Concrete template.** Every draw-automatic has the same shape: hold a typed `Uniform<T>`, in `Set` read from scene, assign to `.Value`.

```csharp
// Geode.Rendering/Uniforms/DrawAutomaticUniforms/ModelViewPerspectiveMatrixUniform.cs

using System.Numerics;

namespace Geode.Rendering.Uniforms.DrawAutomaticUniforms
{
    public sealed class ModelViewPerspectiveMatrixUniformFactory : DrawAutomaticUniformFactory
    {
        public override string Name => "og_modelViewPerspectiveMatrix";
        public override UniformType Datatype => UniformType.FloatMatrix44;
        public override DrawAutomaticUniform Create(Uniform uniform) =>
            new ModelViewPerspectiveMatrixUniform((Uniform<Matrix4x4>)uniform);
    }

    internal sealed class ModelViewPerspectiveMatrixUniform : DrawAutomaticUniform
    {
        private readonly Uniform<Matrix4x4> _uniform;

        public ModelViewPerspectiveMatrixUniform(Uniform<Matrix4x4> uniform) { _uniform = uniform; }

        public override void Set(RenderContext ctx, DrawState ds, SceneState ss)
        {
            _uniform.Value = ss.ModelViewPerspectiveMatrix;
        }
    }
}
```

**The full set** mirrors Book Table 3.1. Every row below is one file in `Uniforms/DrawAutomaticUniforms/` following the template above. Only the GLSL name, the `Uniform<T>` target type, and the value expression change.

| GLSL name | Target | Value expression |
|---|---|---|
| `og_modelViewPerspectiveMatrix` | `Uniform<Matrix4x4>` | `ss.ModelViewPerspectiveMatrix` |
| `og_viewMatrix` | `Uniform<Matrix4x4>` | `ss.ViewMatrix` |
| `og_perspectiveMatrix` | `Uniform<Matrix4x4>` | `ss.PerspectiveMatrix` |
| `og_modelMatrix` | `Uniform<Matrix4x4>` | `ss.ModelMatrix` |
| `og_normalMatrix` | `Uniform<Matrix3x3>` | `ss.NormalMatrix` |
| `og_cameraEye` | `Uniform<Vector3>` | `ss.CameraEyeFloat` |
| `og_cameraLightPosition` | `Uniform<Vector3>` | `ss.CameraLightPosition` |
| `og_sunPosition` | `Uniform<Vector3>` | `ss.SunPositionFloat` |
| `og_diffuseSpecularAmbientShininess` | `Uniform<Vector4>` | `ss.DiffuseSpecularAmbientShininess` |
| `og_viewport` | `Uniform<Vector4>` | `ss.Viewport` |
| `og_inverseViewport` | `Uniform<Vector4>` | `ss.InverseViewport` |
| `og_pixelSizePerDistance` | `Uniform<float>` | `ss.PixelSizePerDistance` |
| `og_wgs84Height` | `Uniform<float>` | `(float)ss.Camera.Height(Ellipsoid.Wgs84)` |
| `og_perspectiveNearPlaneDistance` | `Uniform<float>` | `(float)ss.Camera.NearPlane` |
| `og_perspectiveFarPlaneDistance` | `Uniform<float>` | `(float)ss.Camera.FarPlane` |
| `og_highResolutionSnapScale` | `Uniform<float>` | `ss.HighResolutionSnapScale` |

Some of these (`CameraLightPosition`, `Viewport`, `InverseViewport`, `NormalMatrix`, `PixelSizePerDistance`, `HighResolutionSnapScale`) require matching properties on `SceneState`. Add them as you wire up the corresponding factory.

**Deferred to later sections:**

| Section | Added |
|---|---|
| §27 (RTE/DSFP) | `og_cameraEyeHigh`, `og_cameraEyeLow` |
| §28 (log depth) | `og_modelZToClipCoordinates` |

### Tier 3: `AutomaticUniformFactoryCollection` (the registry)

Lives on the `Device` in OpenGlobe. Geode doesn't have a Device type today -- its functionality is split between `RenderContext` (context-bound state) and a small static holder for context-shareable registries. The automatic-uniform registry is shareable; keep it in a static class.

```csharp
// Geode.Rendering/Uniforms/AutomaticUniformFactoryCollection.cs

using System.Collections.Generic;
using Geode.Rendering.Uniforms.DrawAutomaticUniforms;
using Geode.Rendering.Uniforms.LinkAutomaticUniforms;

namespace Geode.Rendering.Uniforms
{
    /// <summary>
    /// Process-wide registry of link-automatic uniforms and draw-automatic factories.
    /// Populated at first access; consulted by ShaderProgram at link time.
    /// </summary>
    public static class AutomaticUniformFactoryCollection
    {
        private static readonly Dictionary<string, LinkAutomaticUniform> _link = new();
        private static readonly Dictionary<string, DrawAutomaticUniformFactory> _draw = new();

        static AutomaticUniformFactoryCollection()
        {
            // --- Link-automatic uniforms (set once at link) -----------------

            // og_texture0..og_texture7 -> texture units 0..7
            for (int i = 0; i < 8; i++)
                RegisterLink(new TextureUniform(i));

            // --- Draw-automatic factories (set before every draw) ------------

            // Transforms
            RegisterDraw(new ModelViewPerspectiveMatrixUniformFactory());
            RegisterDraw(new ViewMatrixUniformFactory());
            RegisterDraw(new PerspectiveMatrixUniformFactory());
            RegisterDraw(new ModelMatrixUniformFactory());
            RegisterDraw(new NormalMatrixUniformFactory());

            // Camera / lighting
            RegisterDraw(new CameraEyeUniformFactory());
            RegisterDraw(new CameraLightPositionUniformFactory());
            RegisterDraw(new SunPositionUniformFactory());
            RegisterDraw(new DiffuseSpecularAmbientShininessUniformFactory());

            // Viewport / screen-space
            RegisterDraw(new ViewportUniformFactory());
            RegisterDraw(new InverseViewportUniformFactory());
            RegisterDraw(new PixelSizePerDistanceUniformFactory());

            // WGS84 camera height (for LOD selection on the globe)
            RegisterDraw(new Wgs84HeightUniformFactory());

            // Near/far planes (for depth buffer math)
            RegisterDraw(new PerspectiveNearPlaneDistanceUniformFactory());
            RegisterDraw(new PerspectiveFarPlaneDistanceUniformFactory());

            // DSFP RTE (Section 27 populates)
            // DSFP log depth (Section 28 populates)
        }

        public static void RegisterLink(LinkAutomaticUniform u) => _link[u.Name] = u;
        public static void RegisterDraw(DrawAutomaticUniformFactory f) => _draw[f.Name] = f;

        public static bool TryGetLink(string name, out LinkAutomaticUniform? u) => _link.TryGetValue(name, out u);
        public static bool TryGetDrawFactory(string name, out DrawAutomaticUniformFactory? f) => _draw.TryGetValue(name, out f);
    }
}
```

### ShaderProgram integration

`ShaderProgram` becomes the orchestrator. Changes to Section 11's class:

1. Implement `ICleanableObserver`.
2. Add `Uniforms` (a `UniformCollection`), `_dirtyUniforms` (a `List<ICleanable>`), and `_drawAutomaticUniforms` (a `List<DrawAutomaticUniform>`).
3. After link, call `InitializeUniforms()` to scan active uniforms and build the collection.
4. Then call `InitializeAutomaticUniforms()` to handle link-automatics and populate `_drawAutomaticUniforms`.
5. Expose `Clean()` and `Bind(ctx, ds, ss)` for `RenderContext` to call instead of `Use()`.

**New fields + observer callback:**

```csharp
// Additions inside ShaderProgram:

private readonly UniformCollection _uniforms = new();
private readonly List<ICleanable> _dirtyUniforms = new();
private readonly List<DrawAutomaticUniform> _drawAutomaticUniforms = new();

public UniformCollection Uniforms => _uniforms;

// Called by Uniform<T>.Value setter when the cached value changes.
void ICleanableObserver.NotifyDirty(ICleanable cleanable) => _dirtyUniforms.Add(cleanable);
```

**`InitializeUniforms` (scan active uniforms, dispatch on type):**

```csharp
private void InitializeUniforms()
{
    _gl.GetProgram(_handle, ProgramPropertyARB.ActiveUniforms, out int count);
    _gl.GetProgram(_handle, ProgramPropertyARB.ActiveUniformMaxLength, out int maxNameLength);

    for (uint i = 0; i < (uint)count; i++)
    {
        _gl.GetActiveUniform(_handle, i, (uint)maxNameLength, out _, out _,
            out Silk.NET.OpenGL.UniformType glType, out string name);

        // Strip "[0]" suffix that GLSL compilers append to array uniforms.
        int bracket = name.IndexOf('[');
        if (bracket >= 0) name = name.Substring(0, bracket);

        int location = _gl.GetUniformLocation(_handle, name);
        if (location < 0) continue;  // optimized out by the driver -- skip

        // Dispatch on GLSL type to the right concrete Uniform subclass.
        UniformType type = (UniformType)glType;
        Uniform uniform = CreateConcreteUniform(type, name, location);
        _uniforms.Add(uniform);
    }
}

private Uniform CreateConcreteUniform(UniformType type, string name, int location)
{
    return type switch
    {
        UniformType.Float         => new UniformFloatGL(_gl, _handle, name, location, this),
        UniformType.FloatVector2  => new UniformFloatVector2GL(_gl, _handle, name, location, this),
        UniformType.FloatVector3  => new UniformFloatVector3GL(_gl, _handle, name, location, this),
        UniformType.FloatVector4  => new UniformFloatVector4GL(_gl, _handle, name, location, this),
        UniformType.FloatMatrix33 => new UniformFloatMatrix33GL(_gl, _handle, name, location, this),
        UniformType.FloatMatrix44 => new UniformFloatMatrix44GL(_gl, _handle, name, location, this),
        UniformType.Int or UniformType.Sampler1D or UniformType.Sampler2D
            or UniformType.Sampler3D or UniformType.SamplerCube
            or UniformType.Sampler2DArray or UniformType.Sampler2DShadow
            => new UniformIntGL(_gl, _handle, name, location, this),
        UniformType.Bool          => new UniformBoolGL(_gl, _handle, name, location, this),
        _ => throw new NotSupportedException($"Uniform type {type} for '{name}' is not yet wired up.")
    };
}
```

**`InitializeAutomaticUniforms` (consult the registry):**

```csharp
private void InitializeAutomaticUniforms()
{
    foreach (Uniform uniform in _uniforms)
    {
        // Link-automatic first -- set once here, never again.
        if (AutomaticUniformFactoryCollection.TryGetLink(uniform.Name, out LinkAutomaticUniform? linkAuto))
        {
            if (linkAuto!.Datatype != uniform.Datatype)
                throw new Exception($"Shader declares '{uniform.Name}' as {uniform.Datatype}, " +
                                    $"but the engine expects {linkAuto.Datatype}.");
            linkAuto.Set(uniform);
            continue;
        }

        // Draw-automatic: build and store the setter.
        if (AutomaticUniformFactoryCollection.TryGetDrawFactory(uniform.Name, out DrawAutomaticUniformFactory? drawFactory))
        {
            if (drawFactory!.Datatype != uniform.Datatype)
                throw new Exception($"Shader declares '{uniform.Name}' as {uniform.Datatype}, " +
                                    $"but the engine expects {drawFactory.Datatype}.");
            _drawAutomaticUniforms.Add(drawFactory.Create(uniform));
        }
    }
}
```

**`Bind` (called by RenderContext):**

```csharp
public void Bind(RenderContext ctx, DrawState drawState, SceneState sceneState)
{
    _gl.UseProgram(_handle);

    // 1. Pull draw-automatic values from the scene. Each Set(...) writes into
    //    a Uniform<T>.Value, which dirties the uniform if the value changed.
    foreach (DrawAutomaticUniform auto in _drawAutomaticUniforms)
        auto.Set(ctx, drawState, sceneState);

    // 2. Flush every dirty uniform (manual + automatic) to the GPU.
    Clean();
}

/// <summary>Upload all dirty uniforms to the GPU. Called by Bind; exposed for tests.</summary>
public void Clean()
{
    foreach (ICleanable c in _dirtyUniforms)
        c.Clean();
    _dirtyUniforms.Clear();
}
```

**Constructor: call order.**

```csharp
// At the end of the constructor, after shader objects are detached/deleted:
InitializeUniforms();
InitializeAutomaticUniforms();
```

**The typed `SetInt/SetFloat/SetVec3/SetVec4/SetMat4` helpers from Section 11 stay, but their implementations change.** Each is now a one-line shortcut for `((Uniform<T>)Uniforms[name]).Value = value;` -- i.e., they route through the typed collection and the dirty list. They do **not** bypass dirtying the way the pre-rewrite helpers did (`glUniform*` on every call, no caching). Client code can use whichever is more convenient:

```csharp
// These are equivalent. Both set the value through Uniform<T>.Value,
// dirty the uniform if the value changed, and defer the GPU upload
// to the next ShaderProgram.Bind() -> Clean() cycle.
shader.SetMat4("u_modelMatrix", m);
((Uniform<Matrix4x4>)shader.Uniforms["u_modelMatrix"]).Value = m;
```

### RenderContext integration

`Draw` becomes trivial -- apply state, hand off to the program:

```csharp
public void Draw(PrimitiveType primitiveType, DrawState drawState, SceneState sceneState)
{
    ApplyRenderState(drawState.RenderState);
    drawState.ShaderProgram.Bind(this, drawState, sceneState);
    _gl.BindVertexArray(drawState.VertexArrayObject.Handle);

    _gl.DrawElements(primitiveType,
        (uint)drawState.VertexArrayObject.IndexCount,
        DrawElementsType.UnsignedInt, default);
}
```

Same shape for `DrawArrays`. The whole uniform-upload pipeline is invisible at this layer.

### Naming convention (book-faithful)

This guide uses **`og_` for every automatic uniform** -- both link and draw. Manual per-draw uniforms use `u_` or no prefix. The registry is the source of truth, but the prefix gives shader authors a visual signal at a glance.

### Three-step recipe: adding a new draw-automatic uniform

1. Add a property to `SceneState` exposing the value.
2. Add two classes to `DrawAutomaticUniforms/`: the factory (`MyThingUniformFactory`) and the setter (`MyThingUniform`), following the template.
3. Register the factory in `AutomaticUniformFactoryCollection`'s static constructor.

Any shader that declares `uniform <type> og_myThing;` now picks the value up for free. Nothing else changes.

### Built-in GLSL constants (not injected)

OpenGlobe prepends every shader with constants like:

```glsl
const float og_pi           = 3.14159265358979323846;
const float og_halfPi       = 1.57079632679489661923;
const float og_twoPi        = 6.28318530717958647693;
const float og_oneOverPi    = 0.31830988618379067154;
const float og_oneOverTwoPi = 0.15915494309189533577;
const float og_e            = 2.71828182845904523536;
```

Geode does not yet inject these. Shaders that need pi use `radians(180.0)` or declare a local constant. If you add preamble injection later, the hook is `ShaderProgram`'s constructor -- prepend to `vertexSource`/`fragmentSource` before `CompileShader`, preserving `#version 460 core` as the first non-comment line.

---

## Section 19.25: The Shader Cache

*Corresponds to Book Chapter 3, Section 3.4.6*

*OpenGlobe source: `Source/Renderer/ShaderCache.cs`*

*File we build in this section:* `Geode.Rendering/Shaders/ShaderCache.cs`. Also modifies `RenderContext` to own one.

### Why a shader cache exists

A `ShaderCache` maps application-chosen string keys to compiled `ShaderProgram` instances, with a reference count per key. It does two jobs:

1. **Deduplication of shader compilation.** A globe tile renderer with 1,000 tiles on screen wants one shared `ShaderProgram`, not 1,000 freshly compiled copies. The cache guarantees that the same key returns the same instance across the whole program.

2. **Enabling sort-by-state (Book §3.3.6).** The sort-by-state optimization buckets draws by shader using *reference equality* on the `ShaderProgram` field in `DrawState`. Two draws "use the same shader" if and only if they hold a pointer to the same instance. That identity requires shared instances, which requires a cache. Without this, sort-by-shader degrades to "can't sort by shader at all."

The book introduces ShaderCache right after automatic uniforms (§3.4.5) because everything from Chapter 4 onward assumes shared shaders. For Geode, we build it here so it's ready when Part IV lands.

### API

Three public operations plus lifecycle:

```csharp
public sealed class ShaderCache : IDisposable
{
    public ShaderCache(GL gl);

    // Peek only -- returns null if not cached. Does NOT change reference counts.
    // Useful for procedural-shader paths that want to skip source generation
    // when the compiled result is already cached.
    public ShaderProgram? Find(string key);

    // Canonical lookup. Increments refcount whether hit or miss. On miss,
    // compiles the given sources and caches under the key.
    public ShaderProgram FindOrAdd(string key, string vertexSource, string fragmentSource);

    // Deferred-source variant. Source delegates invoked only on a cache miss --
    // prefer this when source generation is expensive (disk I/O, procedural).
    public ShaderProgram FindOrAdd(string key, Func<string> vertexSource, Func<string> fragmentSource);

    // Convenience: key derived from the two paths, files read lazily via the deferred overload.
    public ShaderProgram FindOrAddFromFiles(string vertexPath, string fragmentPath);

    // Decrement. At zero, the program is Disposed and removed. Safe no-op if key is absent.
    public void Release(string key);

    public int Count { get; }
    public void Dispose();   // Disposes every remaining program.
}
```

Ownership rule: the cache owns every `ShaderProgram` it hands out. Callers **do not** `Dispose` returned programs -- they call `Release(key)`. `RenderContext.Dispose` calls `ShaderCache.Dispose`, which disposes all still-cached programs.

### Reference counting, in practice

Every `FindOrAdd` increments. Every `Release` decrements. At zero, the cache removes the entry and disposes the program. The pattern at call sites:

```csharp
// Early in a globe tile's lifecycle:
ShaderProgram tileShader = ctx.Shaders.FindOrAddFromFiles("Shaders/tile.vert", "Shaders/tile.frag");
_drawState = new DrawState(tileShader, _tileVao);

// When the tile is evicted from the scene:
ctx.Shaders.Release("file:Shaders/tile.vert|Shaders/tile.frag");
```

For a process-lifetime shader (the globe surface program in a single-scene demo) you can simply skip the `Release` and let `RenderContext.Dispose` clean up at shutdown. The refcount will sit at 1 forever, which is fine. Ref counting matters when shaders have shorter lifetimes than the context -- tile LOD systems, scene-switching, hot-reload during development.

### Thread safety

The cache uses a single coarse-grained lock for all public operations, matching the book's design.

- **Lookups** (`Find`, `Count`) are safe from any thread.
- **`FindOrAdd` must be called on the render thread** when it takes the miss path, because `new ShaderProgram(...)` calls `glCreateShader` / `glLinkProgram`. A background tile-preparation thread that wants to know "is this shader compiled yet?" should call `Find` first; if null, marshal to the render thread before calling `FindOrAdd`.
- **`Release` must be called on the render thread** because the last release triggers `glDeleteProgram`.

Disposal of the program happens *outside* the lock in `Release` -- keeps the critical section as short as possible and avoids any risk of deadlock if the program's dispose ever reaches back into other locked subsystems later.

### Why not file path as the key universally?

`FindOrAddFromFiles` derives its key from the two file paths (e.g. `"file:Shaders/tile.vert|Shaders/tile.frag"`). That works for file-backed shaders. But for **procedurally-generated shaders** -- e.g., compiling a tessellation shader with different `#define` flags for each LOD level -- there is no file. The caller picks the key:

```csharp
string key = $"tile-shader/lod={lod},lit={lit}";
ShaderProgram p = ctx.Shaders.FindOrAdd(key,
    () => GenerateVertexSource(lod, lit),
    () => GenerateFragmentSource(lod, lit));
```

The deferred overload means the expensive generation happens only on cache miss. Subsequent calls with the same `(lod, lit)` pair return the cached program for the cost of a dictionary lookup.

### Integration with RenderContext

`ShaderCache` is a context-owned resource -- it holds GL handles that are only valid for a particular context's lifetime. `RenderContext` exposes it as a `Shaders` property, constructs it in the constructor, and disposes it in `Dispose`. The unified lifetime means callers never need to pass a `ShaderCache` around explicitly -- they already have the `RenderContext`.

```csharp
// Inside RenderContext:
public ShaderCache Shaders { get; }

public RenderContext(GL gl)
{
    // ... existing ctor setup ...
    Shaders = new ShaderCache(_gl);
}

public void Dispose()
{
    Shaders.Dispose();
}
```

### When it doesn't matter yet

Right now, with only the triangle demo shader, the cache is a formality -- a single `FindOrAdd` call replaces a single `new ShaderProgram` call, no sharing opportunity. It starts to carry weight at:

- **§21 Tessellation** -- still only one shader, but calling through the cache means the triangle shader is auto-disposed on `RenderContext.Dispose` instead of the App having to remember to dispose it explicitly.
- **§22+ tile rendering** -- the first real sharing. N tiles → one shared program. Measurable compile-time savings.
- **Sort-by-state** -- whenever that lands, the shader field in the bucketing sort is already a shared reference, so no additional work is needed.

---

## Section 12: Vertex Buffers and Vertex Arrays

*Corresponds to Book Chapter 3, Section 3.5*

*OpenGlobe source: `Source/Renderer/GL3x/Buffers/`, `Source/Renderer/GL3x/VertexArray/`*

This section builds three files that handle vertex data on the GPU:
- `BufferObject<T>` -- a generic typed GPU buffer (VBO or EBO)
- `VertexAttrib` -- a tiny descriptor for one vertex attribute
- `VertexArrayObject` -- a VAO that binds a VBO, EBO, and attribute layout together

All three depend only on Silk.NET. They do not reference any other Geode type.

### BufferObject.cs

A `BufferObject<T>` wraps a single OpenGL buffer. It uses DSA (`glCreateBuffer` + `glNamedBufferStorage`) to create an **immutable** buffer -- once the data is uploaded, the buffer's size cannot change. This is a deliberate design choice:

- Immutable buffers (`glNamedBufferStorage`) cannot be accidentally reallocated. In OpenGL 3.3, `glBufferData` can be called again on the same buffer, silently orphaning the old data. Immutable storage prevents this class of bugs.
- The `DynamicStorageBit` flag allows the data to be updated via `glNamedBufferSubData`, but the buffer size remains fixed. We use this for cases where we need to update vertex data (e.g., streaming terrain tiles) without reallocating.

> **3.3 vs 4.6 -- Buffer Creation**
>
> In OpenGL 3.3, you must bind the buffer to a target (`GL_ARRAY_BUFFER`) before uploading data. This mutates global state and creates ordering dependencies. With DSA, `glNamedBufferStorage` takes the handle directly -- no binding, no global state mutation. The buffer is usable immediately after creation regardless of what else is bound.

```csharp
// Geode.Rendering/Buffers/BufferObject.cs
//
// A generic GPU buffer that stores an array of unmanaged T values.
// Uses DSA (glCreateBuffer + glNamedBufferStorage) for immutable allocation.
//
// Book Chapter 3, Section 3.5.
// OpenGlobe: Source/Renderer/GL3x/Buffers/BufferGL3x.cs

using Silk.NET.OpenGL;
using System;

namespace Geode.Rendering
{
    /// <summary>
    /// A typed GPU buffer (VBO, EBO, or any buffer target).
    /// Created with immutable storage via DSA -- the size is fixed at creation.
    /// </summary>
    /// <typeparam name="T">The element type (float, uint, etc.). Must be unmanaged.</typeparam>
    public class BufferObject<T> : IDisposable where T : unmanaged
    {
        private readonly GL _gl;
        private readonly uint _handle;

        /// <summary>The raw GL buffer handle.</summary>
        public uint Handle => _handle;

        /// <summary>
        /// Creates a GPU buffer and uploads the given data.
        /// Uses glCreateBuffer (DSA) + glNamedBufferStorage (immutable).
        /// </summary>
        /// <param name="gl">The Silk.NET OpenGL context.</param>
        /// <param name="data">The data to upload. The buffer size is fixed to this length.</param>
        public unsafe BufferObject(GL gl, ReadOnlySpan<T> data)
        {
            _gl = gl;

            // DSA: create buffer without binding to any target
            _handle = _gl.CreateBuffer();

            // Upload data with immutable storage.
            // DynamicStorageBit: allows glNamedBufferSubData for updates, but not resize.
            fixed (void* ptr = data)
            {
                _gl.NamedBufferStorage(
                    _handle,
                    (nuint)(data.Length * sizeof(T)),
                    ptr,
                    BufferStorageMask.DynamicStorageBit);
            }
        }

        /// <summary>
        /// Deletes the GL buffer. Must be called on the render thread.
        /// </summary>
        public void Dispose()
        {
            _gl.DeleteBuffer(_handle);
        }
    }
}
```

---

### VertexAttrib

A vertex attribute descriptor. This is intentionally tiny -- just an index and a component count. The index must match the `layout(location = N)` in the GLSL shader.

```csharp
// Geode.Rendering/Buffers/VertexAttribute.cs
//
// Describes a single vertex attribute: which shader location it binds to
// and how many float components it has.
//
// Examples:
//   Position (vec3): new VertexAttrib(0, 3)
//   Normal   (vec3): new VertexAttrib(1, 3)
//   TexCoord (vec2): new VertexAttrib(2, 2)

namespace Geode.Rendering
{
    /// <summary>
    /// Describes one vertex attribute for VAO setup.
    /// </summary>
    /// <param name="Index">The shader attribute location (layout(location = N)).</param>
    /// <param name="Components">Number of float components (2 for vec2, 3 for vec3, etc.).</param>
    public readonly record struct VertexAttrib(uint Index, int Components);
}
```

---

### VertexArrayObject.cs

The VAO ties everything together: it references a VBO (vertex data), an EBO (index data), and a set of attribute format descriptions. When the VAO is bound and a draw call is issued, OpenGL knows exactly how to read vertices from the buffer.

The DSA setup for VAOs is more verbose than the 3.3 pattern, but it separates **format** (what the data looks like) from **binding** (where the data lives). This separation is a significant improvement:

- `glVertexArrayAttribFormat` describes the data type and offset of each attribute
- `glVertexArrayVertexBuffer` tells the VAO which buffer to read from and at what stride
- `glVertexArrayElementBuffer` attaches the index buffer

> **3.3 vs 4.6 -- VAO Attribute Setup**
>
> In OpenGL 3.3, `glVertexAttribPointer` combines format and binding into one call, and requires the VBO to be bound to `GL_ARRAY_BUFFER` at the time of the call. In 4.6, format and binding are separate operations on a named VAO. This means you can change which buffer a VAO reads from without re-specifying the format -- useful for buffer streaming.

```csharp
// Geode.Rendering/Buffers/VertexArrayObject.cs
//
// A Vertex Array Object that binds vertex data (VBO), index data (EBO),
// and attribute layout into a single drawable unit.
//
// Uses DSA: glCreateVertexArrays, glVertexArrayAttribFormat,
// glVertexArrayVertexBuffer, glVertexArrayElementBuffer.
//
// Book Chapter 3, Section 3.5.
// OpenGlobe: Source/Renderer/GL3x/VertexArray/VertexArrayGL3x.cs

using Silk.NET.OpenGL;
using System;

namespace Geode.Rendering.Buffers
{
    /// <summary>
    /// A Vertex Array Object that owns a VBO and EBO and describes the vertex layout.
    /// Bind this VAO before issuing glDrawElements.
    /// </summary>
    public class VertexArrayObject : IDisposable
    {
        private readonly GL _gl;
        private readonly uint _handle;
        private readonly BufferObject<float> _vbo;
        private readonly BufferObject<uint> _ebo;
        private readonly int _indexCount;

        /// <summary>The raw GL VAO handle.</summary>
        public uint Handle => _handle;

        /// <summary>The number of indices in the element buffer.</summary>
        public int IndexCount => _indexCount;

        /// <summary>
        /// Creates a VAO with the given vertex data, index data, and attribute layout.
        /// </summary>
        /// <param name="gl">The Silk.NET OpenGL context.</param>
        /// <param name="vertices">Interleaved vertex data (floats).</param>
        /// <param name="indices">Triangle indices (unsigned ints).</param>
        /// <param name="attributes">
        /// Attribute descriptors in order. The stride is computed automatically
        /// by summing all component counts * sizeof(float).
        /// Example: [VertexAttrib(0, 3), VertexAttrib(1, 3)] describes
        /// a vertex with position (vec3) + color (vec3) = 6 floats = 24 bytes stride.
        /// </param>
        public VertexArrayObject(GL gl, float[] vertices, uint[] indices,
            params VertexAttrib[] attributes)
        {
            _gl = gl;
            _indexCount = indices.Length;

            // Create the VBO and EBO (data is uploaded immediately)
            _vbo = new BufferObject<float>(gl, vertices);
            _ebo = new BufferObject<uint>(gl, indices);

            // DSA: Create the VAO without binding
            _handle = _gl.CreateVertexArray();

            // Compute stride: total floats per vertex * sizeof(float)
            int totalFloats = 0;
            foreach (VertexAttrib attrib in attributes)
                totalFloats += attrib.Components;
            uint stride = (uint)(totalFloats * sizeof(float));

            // Bind the VBO to binding point 0 of this VAO.
            // The offset is 0 (start of buffer) and stride is the total vertex size.
            _gl.VertexArrayVertexBuffer(_handle, 0, _vbo.Handle, 0, stride);

            // Attach the EBO to this VAO.
            _gl.VertexArrayElementBuffer(_handle, _ebo.Handle);

            // Set up each attribute's format and enable it.
            uint offset = 0;
            foreach (VertexAttrib attrib in attributes)
            {
                // Describe the attribute: index, component count, type, normalized, offset
                _gl.VertexArrayAttribFormat(
                    _handle,
                    attrib.Index,
                    attrib.Components,
                    VertexAttribType.Float,
                    false,
                    offset);

                // Associate this attribute with binding point 0 (where our VBO is)
                _gl.VertexArrayAttribBinding(_handle, attrib.Index, 0);

                // Enable the attribute
                _gl.EnableVertexArrayAttrib(_handle, attrib.Index);

                // Advance offset for the next attribute
                offset += (uint)(attrib.Components * sizeof(float));
            }
        }

        /// <summary>
        /// Deletes the VAO and its owned VBO and EBO.
        /// </summary>
        public void Dispose()
        {
            _gl.DeleteVertexArray(_handle);
            _vbo.Dispose();
            _ebo.Dispose();
        }
    }
}
```

**Line count:** ~80 lines for VertexArrayObject, ~40 for BufferObject, ~3 for VertexAttrib.

**The key insight** is that `VertexArrayObject` *owns* its buffers. When the VAO is disposed, the buffers are disposed too. This prevents resource leaks and simplifies the caller's lifetime management -- they only need to track one object.

---

## Section 14: Vertex Data Layouts

*Corresponds to Book Chapter 3, Section 3.5.3*

This is a conceptual section -- no new files. It explains the three approaches to organizing vertex data in GPU buffers and justifies our choice.

### Three Approaches

**Approach 1: Separate Buffers** -- one buffer per attribute.

```
VBO 0 (positions): [P0 P1 P2 P3 P4 ...]
VBO 1 (normals):   [N0 N1 N2 N3 N4 ...]
VBO 2 (texcoords): [T0 T1 T2 T3 T4 ...]
```

Pros: Easy to update one attribute without touching others. Flexible.
Cons: Three separate buffer binds per draw call. Poor cache performance -- reading vertex 0 requires three separate cache lines.

**Approach 2: Non-Interleaved (Array of Structs in a single buffer)**

```
VBO: [P0 P1 P2 ... Pn | N0 N1 N2 ... Nn | T0 T1 T2 ... Tn]
```

Pros: Single buffer bind. Can update one attribute range.
Cons: Still poor cache performance for reading complete vertices.

**Approach 3: Interleaved (Struct of Arrays pattern)**

```
VBO: [P0 N0 T0 | P1 N1 T1 | P2 N2 T2 | ...]
```

Pros: Reading vertex N loads all its attributes into a single cache line. Best performance for static geometry. Single buffer bind.
Cons: Cannot update one attribute without re-uploading the entire vertex.

### Our Choice: Interleaved

For a virtual globe, terrain and imagery tiles are generated on the CPU, uploaded to the GPU once, and drawn many times without modification (until the tile is replaced by a higher-LOD version). This is the ideal case for interleaved layout: maximum cache performance during the many draw calls, no penalty from the write-once upload.

### GlobeVertex (Conceptual)

When we build the globe tessellator in Part IV, each vertex will have this layout:

```
GlobeVertex (32 bytes total):
  ├── Position  (vec3, 12 bytes)  -- ECEF position in meters
  ├── Normal    (vec3, 12 bytes)  -- Geodetic surface normal (unit vector)
  └── TexCoord  (vec2,  8 bytes)  -- Texture coordinates (longitude/latitude mapped to [0,1])
```

This maps to three vertex attributes:
```csharp
new VertexAttrib(0, 3),  // position: location 0, 3 components
new VertexAttrib(1, 3),  // normal:   location 1, 3 components
new VertexAttrib(2, 2),  // texcoord: location 2, 2 components
```

Total stride: 32 bytes. At 1024 vertices per tile edge (a high-detail terrain tile), that is 1024 * 1024 * 32 = 33,554,432 bytes = 32 MB per tile. This is large, which is why LOD management (Part IV) is critical.

---

## Section 14.5: Meshes

*Corresponds to Book Chapter 3, Section 3.5.6*

*OpenGlobe source: `Source/Core/Geometry/Mesh.cs`, `Source/Core/Geometry/VertexAttribute.cs`, `Source/Core/Geometry/IndicesBase.cs`, and the family of concrete vertex-attribute subclasses.*

This is a design section -- no `.cs` files yet. It specifies the shape of `Mesh` and its surrounding types so that when Part IV tessellators start producing geometry, the bridge to the Rendering layer is already decided.

### Why Mesh lives in `Geode.Core`, not `Geode.Rendering`

A `Mesh` is pure system-memory geometry: vertex positions, normals, texture coordinates, triangle indices. **It holds no GL handles, no buffer IDs, nothing GPU-backed.** That's deliberate.

Putting `Mesh` in `Geode.Core` -- the assembly that has zero GL dependencies -- buys three properties:

1. **Thread safety.** A globe tessellator (§21) or terrain-height generator runs on a background thread. GL contexts are bound to one thread; if `Mesh` held a GPU buffer it couldn't be built off-thread. Keeping Mesh pure CPU data means a worker thread can produce a complete Mesh and hand it to the render thread for GPU upload later.

2. **Testability.** Every geometry generator can be unit-tested without a GL context. Build a Mesh, assert on its vertex positions. No window, no Silk.NET, no `OpenGLContext`.

3. **Backend independence.** Nothing in `Mesh` assumes OpenGL. If we ever add a Vulkan or D3D backend, the tessellators and terrain code don't move -- only the Rendering-layer code that turns a `Mesh` into a `VertexArrayObject` gets a sibling implementation.

This is exactly the pattern the book prescribes: Mesh is the **CPU-side bridge** between geometry producers and the renderer.

### Class structure

The design is three intertwined types:

**`VertexAttribute`** -- abstract base for one named attribute on a Mesh. Generic subclasses carry strongly typed value lists.

```csharp
// Geode.Core/Geometry/VertexAttribute.cs

namespace Geode.Core.Geometry
{
    /// <summary>
    /// Abstract base for a named vertex attribute ("position", "normal", etc.).
    /// Concrete subclasses ({VertexAttributeFloatVector3}, etc.) store the actual
    /// per-vertex values.
    /// </summary>
    public abstract class VertexAttribute
    {
        public string Name { get; }
        public VertexAttributeType Datatype { get; }
        public abstract int Count { get; }  // number of vertices

        protected VertexAttribute(string name, VertexAttributeType datatype)
        {
            Name = name;
            Datatype = datatype;
        }
    }

    /// <summary>
    /// A vertex attribute with a typed value list. T is the per-vertex element
    /// type (Vector3, Vector2, float, byte, etc.).
    /// </summary>
    public abstract class VertexAttribute<T> : VertexAttribute
    {
        public IList<T> Values { get; }

        protected VertexAttribute(string name, VertexAttributeType datatype, int capacity)
            : base(name, datatype)
        {
            Values = new List<T>(capacity);
        }

        public override int Count => Values.Count;
    }
}
```

**`VertexAttributeType`** -- enum identifying the GLSL type the values will populate:

```csharp
public enum VertexAttributeType
{
    UnsignedByte,
    HalfFloat,
    HalfFloatVector2, HalfFloatVector3, HalfFloatVector4,
    Float,
    FloatVector2, FloatVector3, FloatVector4,

    // Emulated doubles -- produces TWO float attributes (high + low) when
    // uploaded. The tessellator writes Vector3D values; the Rendering layer's
    // Mesh-to-VAO converter splits them into two float vec3 streams for
    // GPU RTE / DSFP (Section 27).
    EmulatedDoubleVector3,
}
```

**Concrete attribute classes** are one-liners per (type, datatype) pair:

```csharp
public sealed class VertexAttributeFloatVector3 : VertexAttribute<Vector3>
{
    public VertexAttributeFloatVector3(string name, int capacity = 0)
        : base(name, VertexAttributeType.FloatVector3, capacity) { }
}

public sealed class VertexAttributeDoubleVector3 : VertexAttribute<Vector3D>
{
    public VertexAttributeDoubleVector3(string name, int capacity = 0)
        : base(name, VertexAttributeType.EmulatedDoubleVector3, capacity) { }
}

// Siblings for Vector2/Vector4/float/UnsignedByte as needed.
```

**`VertexAttributeCollection`** -- a named dictionary of attributes owned by a Mesh:

```csharp
public sealed class VertexAttributeCollection
{
    private readonly Dictionary<string, VertexAttribute> _byName = new();

    public VertexAttribute this[string name] => _byName[name];
    public void Add(VertexAttribute attribute) => _byName[attribute.Name] = attribute;
    public bool Contains(string name) => _byName.ContainsKey(name);
    public IEnumerable<VertexAttribute> All => _byName.Values;
}
```

**`IndicesBase`** -- abstract; concrete variants for UnsignedByte / UnsignedShort / UnsignedInt. Which one a mesh uses depends on vertex count:

```csharp
public abstract class IndicesBase
{
    public IndicesType Datatype { get; }
    public abstract int Count { get; }
    protected IndicesBase(IndicesType datatype) { Datatype = datatype; }
}

public sealed class IndicesUnsignedInt : IndicesBase
{
    public IList<uint> Values { get; } = new List<uint>();
    public IndicesUnsignedInt() : base(IndicesType.UnsignedInt) { }
    public override int Count => Values.Count;
}

// Similarly IndicesUnsignedShort for meshes < 65,536 vertices (half the memory)
// and IndicesUnsignedByte for small meshes.
```

Choosing the smallest workable index type matters at scale: a terrain tile with 60,000 vertices fits in `ushort`, halving the index-buffer memory vs `uint`.

**`Mesh`** -- pulls it together:

```csharp
// Geode.Core/Geometry/Mesh.cs
using Geode.Core.Geometry;

namespace Geode.Core.Geometry
{
    public sealed class Mesh
    {
        public VertexAttributeCollection Attributes { get; } = new();
        public IndicesBase? Indices { get; set; }                          // null = non-indexed
        public PrimitiveType PrimitiveType { get; set; } = PrimitiveType.Triangles;
        public WindingOrder FrontFaceWindingOrder { get; set; } = WindingOrder.CounterClockwise;
    }
}
```

Note `PrimitiveType` and `WindingOrder` are Core-level enums matching the Rendering-layer names. If sharing them between assemblies is awkward, the Rendering-layer `CreateVertexArray` converts at the bridge.

### The bridge: `Context.CreateVertexArray(mesh, shader, bufferHint)`

Book design: adding a method on `Context` (our `RenderContext`) that takes a Mesh plus a shader program and produces a `VertexArrayObject`. The method:

1. Walks the shader's vertex attributes (discovered at link time via `glGetActiveAttrib` — similar to our uniform scan) to learn each attribute's `layout(location = N)`.
2. For each named attribute in `mesh.Attributes`, matches it to the shader attribute of the same name.
3. Allocates a `BufferObject<byte>` sized for the interleaved layout, packs the attribute values into it, uploads once.
4. Allocates the index buffer (type chosen from `mesh.Indices.Datatype`).
5. Constructs a `VertexArrayObject` wiring attribute formats to locations.
6. Handles `EmulatedDoubleVector3` specially: produces two float vec3 streams (high + low) and wires both to their matching shader attributes (e.g., `positionHigh` and `positionLow`).

```csharp
// Sketch on RenderContext:
public VertexArrayObject CreateVertexArray(Mesh mesh,
                                           ShaderProgram shader,
                                           BufferHint bufferHint);

public enum BufferHint
{
    StaticDraw,   // upload once, draw many times (tile geometry, static meshes)
    DynamicDraw,  // upload occasionally (morphing terrain between LODs)
    StreamDraw,   // upload every frame (skinned meshes, particle systems)
}
```

This replaces the current `new VertexArrayObject(gl, float[], uint[], params VertexAttrib[])` constructor for any geometry that comes from a Mesh producer. The raw `float[]` ctor stays available for hand-crafted test geometry like the triangle demo (§20).

### Why the name-based shader-to-mesh matching

The bridge works because the shader's `in vec3 position;` declaration and the mesh's `new VertexAttributeFloatVector3("position")` share a string key. That means:

- Tessellators don't know or care about `layout(location = N)` -- they just name their outputs.
- Shaders don't know the `Mesh` API -- they just declare inputs by name.
- The renderer connects them at `CreateVertexArray` time.

Compare to hand-crafted geometry where the caller passes `new VertexAttrib(0, 3)` and has to know the shader expects position at location 0. That works for one shader; it scales badly when you have many tessellators feeding many shaders.

### What to implement when

| Need | When to build |
|---|---|
| `VertexAttributeType` enum + `VertexAttribute` + `VertexAttribute<T>` | Now, if you want a clean §21 tessellator |
| `VertexAttributeFloatVector3` (and vec2/vec4/float siblings) | Same -- the minimum set §21 produces |
| `IndicesBase` + `IndicesUnsignedInt` | §21 (indexed triangles) |
| `IndicesUnsignedShort` | §22+ when a single tile fits under 65k vertices |
| `Mesh` + `VertexAttributeCollection` | §21 |
| `RenderContext.CreateVertexArray(mesh, shader, bufferHint)` | §21 -- the tessellator returns a Mesh, the renderer turns it into a VAO |
| `VertexAttributeDoubleVector3` + `EmulatedDoubleVector3` handling | §27 (DSFP/RTE vertex transform precision) |

For now, the triangle demo in §20 can keep using the hand-crafted `VertexArrayObject(GL, float[], uint[], VertexAttrib[])` ctor. When you build the globe tessellator in §21 it will naturally want a Mesh-shaped API, which is when these Core-layer types earn their place.

### Summary of design principles

- **Mesh lives in `Geode.Core`.** Zero GL. Zero threading constraints beyond ordinary mutable-list safety.
- **Named attributes**, not positional. The name is the contract between producers (tessellators) and consumers (shaders).
- **Strongly typed values.** `VertexAttribute<Vector3>` stores `IList<Vector3>`. No raw float arrays on the Mesh side -- only at the GPU upload boundary.
- **Explicit indices type.** Use the smallest type that fits; halves memory for large tile sets.
- **`EmulatedDoubleVector3` is a first-class attribute type.** Chapter 5's DSFP RTE already influences the Mesh design; the alternative is bolting on special cases later.
- **The Rendering layer does the packing.** Mesh is a structure-of-arrays on the CPU; the bridge packs it into an interleaved GPU buffer. The producer doesn't care about GPU memory layout.

---

## Section 13: Textures

*Corresponds to Book Chapter 3, Section 3.6*

*OpenGlobe source: `Source/Renderer/Texture2DDescription.cs`, `Source/Renderer/TextureFormat.cs`, `Source/Renderer/Texture2D.cs`, `Source/Renderer/WritePixelBuffer.cs`, `Source/Renderer/ReadPixelBuffer.cs`, `Source/Renderer/TextureSampler.cs`, `Source/Renderer/TextureUnit.cs`, `Source/Renderer/GL3x/Textures/Texture2DGL3x.cs`.*

Textures are GPU-resident images sampled by fragment shaders. The book splits §3.6 into three topics: (1) **textures and pixel buffers** -- the data flow from system memory to the GPU and back; (2) **samplers** -- the filtering and wrapping rules applied when a shader reads from a texture; (3) **rendering with textures** -- the `Context.TextureUnits[]` collection that connects textures + samplers to the shader's sampler uniforms, enabling multitexturing.

Geode follows all three. The current `Texture2D.cs` is the minimum viable starter -- RGBA8 only, sampler parameters fixed at creation, no pixel buffers, no read-back -- and this section specifies the full book-faithful surface that it grows into. Each sub-section below says what to add and when.

> **3.3 vs 4.6 -- Texture Creation**
>
> In OpenGL 3.3, texture creation requires binding: `glGenTextures` + `glBindTexture(GL_TEXTURE_2D, handle)` + `glTexImage2D(...)`. Any other code that binds `GL_TEXTURE_2D` between your gen and your tex calls corrupts your setup. With DSA, `glCreateTextures(GL_TEXTURE_2D)` + `glTextureStorage2D(handle, ...)` operates directly on the handle -- no binding, no corruption risk. Every GL call in this section uses the DSA variants.

---

### 13.1 Texture2DDescription

A `Texture2DDescription` is an immutable specification of a texture -- everything you need to know to create one, and everything a created texture reports back to you. Book Listing 3.25 defines it as:

```csharp
// Geode.Rendering/Texture2DDescription.cs

using System;

namespace Geode.Rendering
{
    /// <summary>
    /// Immutable description of a Texture2D -- resolution, internal format,
    /// mipmap policy. Passed to Texture2D's constructor. Exposed from
    /// <see cref="Texture2D.Description"/> so callers can query a texture's
    /// properties without tracking them separately.
    /// </summary>
    public readonly record struct Texture2DDescription(
        int Width,
        int Height,
        TextureFormat TextureFormat,
        bool GenerateMipmaps = false) : IEquatable<Texture2DDescription>
    {
        /// <summary>True if this format can be attached to a framebuffer's color attachment (see Section 19.5).</summary>
        public bool ColorRenderable => TextureFormat.IsColorRenderable();

        /// <summary>True if this format can be attached as a framebuffer's depth attachment.</summary>
        public bool DepthRenderable => TextureFormat.IsDepthRenderable();

        /// <summary>True if this format can be attached as a framebuffer's combined depth-stencil attachment.</summary>
        public bool DepthStencilRenderable => TextureFormat.IsDepthStencilRenderable();
    }
}
```

The renderability flags drive the format-validation in `Framebuffer` (Section 19.5). Keeping them on the description means a `Framebuffer.ColorAttachments[i] = texture` assignment can check `texture.Description.TextureFormat.IsColorRenderable()` and throw immediately rather than letting GL surface an opaque "framebuffer incomplete" error at the next draw.

---

### 13.2 TextureFormat enum

The set of internal formats the engine supports. Each value maps one-to-one to a `Silk.NET.OpenGL.SizedInternalFormat`. The book's canonical list (Listing 3.25) covers the common color, float, depth, and depth-stencil formats:

```csharp
// Geode.Rendering/TextureFormat.cs

namespace Geode.Rendering
{
    /// <summary>
    /// Sized internal formats supported by Geode's Texture2D. Each value
    /// corresponds directly to an OpenGL sized internal format.
    /// </summary>
    public enum TextureFormat
    {
        // 8-bit per channel color
        Red8,
        RedGreenBlue8,
        RedGreenBlueAlpha8,

        // 16-bit per channel color
        Red16,
        RedGreenBlue16,
        RedGreenBlueAlpha16,

        // Float color (HDR)
        Red16f,         Red32f,
        RedGreen16f,    RedGreen32f,
        RedGreenBlue16f, RedGreenBlue32f,
        RedGreenBlueAlpha16f, RedGreenBlueAlpha32f,

        // sRGB
        Srgb8,
        Srgb8Alpha8,

        // Depth / depth-stencil (for framebuffer attachments, Section 19.5)
        Depth16,
        Depth24,
        Depth32,
        Depth32f,
        Depth24Stencil8,
        Depth32fStencil8,
    }
}
```

Internally `Texture2D` maps `TextureFormat` to `SizedInternalFormat` via a private switch. Keeping the Geode enum separate from `Silk.NET.OpenGL.SizedInternalFormat` insulates higher layers (Mesh, Framebuffer, application code) from the underlying GL binding.

---

### 13.3 Format flags (renderability)

`Texture2DDescription.ColorRenderable` etc. delegate to extension methods on `TextureFormat`. Section 19.5 already defined these over `Silk.NET.OpenGL.InternalFormat`; once we move to the Geode `TextureFormat` enum, port them to extension methods on the Geode type:

```csharp
// Geode.Rendering/TextureFormatFlags.cs

namespace Geode.Rendering
{
    public static class TextureFormatFlags
    {
        public static bool IsColorRenderable(this TextureFormat f) => f switch
        {
            TextureFormat.Red8 or TextureFormat.RedGreenBlue8 or TextureFormat.RedGreenBlueAlpha8
                or TextureFormat.Red16 or TextureFormat.RedGreenBlue16 or TextureFormat.RedGreenBlueAlpha16
                or TextureFormat.Red16f or TextureFormat.Red32f
                or TextureFormat.RedGreen16f or TextureFormat.RedGreen32f
                or TextureFormat.RedGreenBlue16f or TextureFormat.RedGreenBlue32f
                or TextureFormat.RedGreenBlueAlpha16f or TextureFormat.RedGreenBlueAlpha32f
                or TextureFormat.Srgb8 or TextureFormat.Srgb8Alpha8 => true,
            _ => false
        };

        public static bool IsDepthRenderable(this TextureFormat f) => f switch
        {
            TextureFormat.Depth16 or TextureFormat.Depth24
                or TextureFormat.Depth32 or TextureFormat.Depth32f => true,
            _ => false
        };

        public static bool IsDepthStencilRenderable(this TextureFormat f) => f switch
        {
            TextureFormat.Depth24Stencil8 or TextureFormat.Depth32fStencil8 => true,
            _ => false
        };
    }
}
```

The Framebuffer section's current `TextureFormatFlags` (which extends `InternalFormat`) stays valid while the simpler `Texture2D(GL, int, int, byte[])` constructor is still in use. When `Texture2D` switches to `Texture2DDescription`, migrate `Framebuffer` to check `texture.Description.TextureFormat` instead.

---

### 13.4 Pixel buffers -- `WritePixelBuffer` and `ReadPixelBuffer`

Pixel buffers are the book's (and GL's) mechanism for **asynchronous** texture uploads and downloads. Without a pixel buffer, `glTextureSubImage2D` blocks until the upload completes -- fine for a one-off test texture but unacceptable for streaming hundreds of tile textures per frame. With a pixel buffer, the upload is queued; the CPU can move on while the GPU DMA's the bytes in the background.

Two types, separated by direction of flow (Book Fig 3.16):

- **`WritePixelBuffer`** -- system memory → texture. Backed by a GL `PIXEL_UNPACK_BUFFER`.
- **`ReadPixelBuffer`** -- texture → system memory. Backed by a GL `PIXEL_PACK_BUFFER`.

Both look like untyped vertex buffers (see [§12 BufferObject](#section-12-vertex-buffers-and-vertex-arrays)): they carry raw bytes with generic `CopyFromSystemMemory<T>` / `CopyToSystemMemory<T>` overloads so callers don't cast. They also support `CopyFromBitmap` / `CopyToBitmap` because image data often lives in an image type (e.g., a `StbImageSharp.ImageResult`).

```csharp
// Geode.Rendering/PixelBufferHint.cs

namespace Geode.Rendering
{
    /// <summary>
    /// Hint about how a pixel buffer will be used. Maps to GL's buffer usage hint;
    /// affects how the driver places the storage (streaming vs. persistent).
    /// </summary>
    public enum PixelBufferHint
    {
        Stream,   // write once, use once -- tile upload
        Static,   // write once, use many times -- UI texture
        Dynamic,  // rewritten frequently -- video frame upload
    }
}
```

```csharp
// Geode.Rendering/WritePixelBuffer.cs (sketch)

namespace Geode.Rendering
{
    /// <summary>
    /// Raw-byte GPU buffer used to stage pixel data for a texture upload.
    /// Produced by <see cref="RenderContext.CreateWritePixelBuffer"/>; consumed
    /// by <see cref="Texture2D.CopyFromBuffer"/>.
    /// </summary>
    public abstract class WritePixelBuffer : System.IDisposable
    {
        public abstract PixelBufferHint UsageHint { get; }
        public abstract int SizeInBytes { get; }

        public abstract void CopyFromSystemMemory<T>(T[] bufferInSystemMemory) where T : unmanaged;
        public abstract void CopyFromSystemMemory<T>(T[] bufferInSystemMemory, int destinationOffsetInBytes) where T : unmanaged;
        public abstract void CopyFromBitmap(StbImageSharp.ImageResult bitmap);

        public abstract T[] CopyToSystemMemory<T>() where T : unmanaged;
        public abstract void Dispose();
    }
}
```

`ReadPixelBuffer` is the mirror: `CopyFromBitmap` → `CopyToBitmap`, `CopyFromSystemMemory` → `CopyToSystemMemory`, and its contents are populated by `Texture2D.CopyToBuffer(format, datatype)` when reading back from the GPU.

**On the `Bitmap` divergence.** The book uses `System.Drawing.Bitmap`, which is Windows-only in modern .NET. Geode uses `StbImageSharp.ImageResult` (already a dependency), which is cross-platform. Same contract, different type.

**Why separate write / read types?** Book Listing 3.26 explains: a method like `Texture2D.CopyFromBuffer` should only accept a write buffer, and a method like `Texture2D.CopyToBuffer` should only return a read buffer. A single `PixelBuffer` type would force runtime checks; separate types give compile-time safety. See Book §3.6.1's callout on vertex-buffer-vs-pixel-buffer polymorphism for the deeper design discussion.

---

### 13.5 `ImageFormat` and `ImageDatatype`

When `Texture2D.CopyFromBuffer(buffer, format, datatype)` runs, GL needs to know how to interpret the raw bytes in the pixel buffer. Two enums describe this:

```csharp
// Geode.Rendering/ImageFormat.cs

namespace Geode.Rendering
{
    /// <summary>Channel layout of the bytes in a pixel buffer.</summary>
    public enum ImageFormat
    {
        Red,
        RedGreen,
        RedGreenBlue,
        RedGreenBlueAlpha,
        BlueGreenRed,           // for BMP files
        BlueGreenRedAlpha,
        DepthComponent,
        DepthStencil,
    }
}
```

```csharp
// Geode.Rendering/ImageDatatype.cs

namespace Geode.Rendering
{
    /// <summary>Scalar type of each channel in a pixel buffer.</summary>
    public enum ImageDatatype
    {
        UnsignedByte,
        Byte,
        UnsignedShort,
        Short,
        UnsignedInt,
        Int,
        Float,
        HalfFloat,
    }
}
```

GL converts from `(ImageFormat, ImageDatatype)` to the internal `TextureFormat` the texture was created with. The caller's buffer format doesn't have to match the texture's internal format -- an RGBA8 texture can be populated from a `(RedGreenBlueAlpha, Float)` buffer, with the driver doing the conversion.

---

### 13.6 `Texture2D` full surface

Everything above feeds into `Texture2D`. The book's interface (Listing 3.27) is:

```csharp
// Geode.Rendering/Texture2D.cs (target surface)

using System;

namespace Geode.Rendering
{
    public class Texture2D : IDisposable
    {
        public uint Handle { get; }

        /// <summary>The description this texture was created with. Immutable.</summary>
        public Texture2DDescription Description { get; }

        // ---- Upload (WritePixelBuffer -> Texture) -----------------------

        /// <summary>Replace the entire level-0 image.</summary>
        public void CopyFromBuffer(
            WritePixelBuffer pixelBuffer,
            ImageFormat format,
            ImageDatatype dataType);

        /// <summary>Replace a sub-rectangle of the level-0 image, with explicit row alignment.</summary>
        public void CopyFromBuffer(
            WritePixelBuffer pixelBuffer,
            int xOffset, int yOffset,
            int width, int height,
            ImageFormat format,
            ImageDatatype dataType,
            int rowAlignment);

        // ---- Download (Texture -> ReadPixelBuffer) ----------------------

        public ReadPixelBuffer CopyToBuffer(ImageFormat format, ImageDatatype dataType);

        // ---- Debugging -------------------------------------------------

        /// <summary>Save the level-0 image to disk (PNG). Useful for debugging shader output and tile content.</summary>
        public void Save(string filename);

        public void Dispose();
    }
}
```

Internally the constructor runs:

1. `glCreateTextures(GL_TEXTURE_2D)` -- DSA create.
2. `glTextureStorage2D(handle, mipLevels, sizedInternalFormat, w, h)` -- allocate immutable storage. `mipLevels` is `1 + floor(log2(max(w, h)))` when `Description.GenerateMipmaps`, else `1`.
3. If initial pixel data is supplied via a `WritePixelBuffer`: `glTextureSubImage2D(handle, 0, 0, 0, w, h, pixelFormat, pixelType, offset)` while the pixel buffer is bound to `GL_PIXEL_UNPACK_BUFFER`.
4. If `Description.GenerateMipmaps`: `glGenerateTextureMipmap(handle)` after the initial upload.

Sampler parameters (filter / wrap / anisotropy) are **not** set on the texture -- they live in a separate `TextureSampler` object, per §13.8.

---

### 13.7 Texture Rectangles

A **texture rectangle** is a 2D texture that uses **unnormalized** texture coordinates -- if the texture is 512x256, valid coordinates are `(0..512, 0..256)` instead of `(0..1, 0..1)`. This makes some algorithms simpler -- the book flags ray-casting of height fields in §11.2.3 as the motivating case.

Constraints:
- **No mipmaps.**
- **No repeat wrapping** (only clamp variants).

Implementation: the same `Texture2D` class, constructed via `RenderContext.CreateTexture2DRectangle(description)` which calls `glCreateTextures(GL_TEXTURE_RECTANGLE)` instead of `GL_TEXTURE_2D`. The factory enforces the constraints (throw if `GenerateMipmaps` is true, reject `Repeat` on the attached sampler).

Implementation priority: deferred until §11 terrain ray-casting.

---

### 13.8 `TextureSampler` -- sampling state, decoupled from textures

The book (§3.6.2, Fig 3.17) and modern GL both decouple the texture (what data is stored) from the sampler (how a shader reads it). The same `Texture2D` can be sampled linear-clamp in one pass and nearest-repeat in the next -- no need to duplicate the texture data.

```csharp
// Geode.Rendering/TextureSampler.cs

using System;

namespace Geode.Rendering
{
    public enum TextureMinificationFilter
    {
        Nearest,
        Linear,
        NearestMipmapNearest,
        LinearMipmapNearest,
        NearestMipmapLinear,
        LinearMipmapLinear,   // trilinear -- highest quality minification
    }

    public enum TextureMagnificationFilter
    {
        Nearest,
        Linear,
    }

    public enum TextureWrap
    {
        Clamp,           // GL_CLAMP_TO_EDGE
        Repeat,          // GL_REPEAT
        MirroredRepeat,  // GL_MIRRORED_REPEAT
    }

    public sealed class TextureSampler : IDisposable
    {
        public uint Handle { get; }
        public TextureMinificationFilter MinificationFilter { get; }
        public TextureMagnificationFilter MagnificationFilter { get; }
        public TextureWrap WrapS { get; }
        public TextureWrap WrapT { get; }
        public float MaximumAnisotropy { get; }

        internal TextureSampler(GL gl, TextureMinificationFilter min, TextureMagnificationFilter mag,
                                TextureWrap wrapS, TextureWrap wrapT, float maxAnisotropy = 1.0f);

        public void Dispose();
    }
}
```

Construction goes through a factory on `RenderContext` so the cache (§13.9) is consulted first:

```csharp
TextureSampler sampler = renderContext.CreateTexture2DSampler(
    TextureMinificationFilter.Linear,
    TextureMagnificationFilter.Linear,
    TextureWrap.Repeat,
    TextureWrap.Repeat);
```

Anisotropic filtering is controlled by `MaximumAnisotropy`. Values greater than 1 (up to the GL-reported maximum, typically 16) improve quality on obliquely-viewed surfaces -- textured terrain seen at a grazing angle, for example. Requires `GL_EXT_texture_filter_anisotropic` (core in 4.6).

---

### 13.9 Pre-made sampler collection

The book notes that four sampler combinations cover 90% of uses. Put them on `RenderContext` so every call site gets the same instance instead of creating duplicates:

```csharp
// On RenderContext:

public sealed class Samplers
{
    public TextureSampler LinearRepeat { get; }
    public TextureSampler LinearClamp { get; }
    public TextureSampler NearestRepeat { get; }
    public TextureSampler NearestClamp { get; }
}

public Samplers Samplers { get; }  // populated in the constructor
```

At call sites this lets you write:

```csharp
renderContext.TextureUnits[0].Sampler = renderContext.Samplers.LinearClamp;
```

instead of:

```csharp
// Always creates a new sampler object
renderContext.TextureUnits[0].Sampler = renderContext.CreateTexture2DSampler(
    TextureMinificationFilter.Linear, TextureMagnificationFilter.Linear,
    TextureWrap.Clamp, TextureWrap.Clamp);
```

See Book §3.6.2's "Try This" on a sampler cache: unlike shaders (where compilation is expensive), sampler state is tiny, so a full cache isn't usually worth it. The four pre-made instances are the pragmatic middle ground.

---

### 13.10 `Context.TextureUnits` -- multitexturing

A shader can sample multiple textures in a single draw (day + night, diffuse + normal, diffuse + lightmap). GL exposes this as a fixed array of **texture units** -- numbered slots, each bindable to one texture + one sampler.

```csharp
// Additions to RenderContext:

public sealed class TextureUnit
{
    public Texture2D? Texture { get; set; }
    public TextureSampler? Sampler { get; set; }
}

public sealed class TextureUnits
{
    private readonly TextureUnit[] _slots;  // length = Device.NumberOfTextureUnits

    public int Count => _slots.Length;
    public TextureUnit this[int index] => _slots[index];
}

public TextureUnits TextureUnits { get; }
public int NumberOfTextureUnits => TextureUnits.Count;  // typically 16 on 4.6
```

Before a draw, the application assigns textures and samplers:

```csharp
renderContext.TextureUnits[0].Texture = dayTexture;
renderContext.TextureUnits[0].Sampler = renderContext.Samplers.LinearClamp;

renderContext.TextureUnits[1].Texture = nightTexture;
renderContext.TextureUnits[1].Sampler = renderContext.Samplers.LinearClamp;

renderContext.Draw(primitiveType, drawState, sceneState);
```

`RenderContext.Draw` flushes any dirty unit assignments before issuing the GL draw, via `glBindTextureUnit(i, texture.Handle)` + `glBindSampler(i, sampler.Handle)`.

**Connecting units to shader samplers.** Two options:

1. **Link-automatic (`geode_texture0..geode_texture7`)**. Section 19's `TextureUniform` binds these to units 0..7 at link time, with no application code needed. Just declare `uniform sampler2D geode_texture0;` in the shader; `TextureUnits[0].Texture = dayTexture` is all the application needs.
2. **Custom-named sampler**. For a shader-specific name like `u_dayTexture`, set the sampler uniform explicitly to the unit index:
    ```csharp
    ((Uniform<int>)shader.Uniforms["u_dayTexture"]).Value = 0;
    renderContext.TextureUnits[0].Texture = dayTexture;
    ```

Either way, the contract is: the shader's sampler uniform holds a texture-unit index; `TextureUnits[index]` holds the actual texture + sampler that index refers to at draw time.

**Why is the unit collection on `Context`, not `DrawState`?** Book §3.6.3 flags this as a question. Answer: GL texture-unit bindings are *context* state, not *draw* state. A `DrawState` describes a shader + VAO + render state bundle; two DrawStates can share textures. Putting `TextureUnits` on the context matches how GL actually works and lets the renderer avoid redundant rebinds when two draws use the same units.

---

### 13.11 Current implementation vs target surface

The existing `Texture2D.cs` is the **minimum starter** -- one constructor (`(GL, int, int, byte[])`), hardcoded `SizedInternalFormat.Rgba8`, hardcoded sampler parameters baked into the texture, no pixel buffers, no read-back, no description property:

```csharp
public unsafe Texture2D(GL gl, int width, int height, byte[] pixels) { /* existing */ }
public void Bind(uint unit) { /* glBindTextureUnit */ }
public static Texture2D FromFile(GL gl, string path) { /* StbImageSharp */ }
```

To reach the full book surface above, extend in this order as consumers require:

| When | Add |
|---|---|
| Before the first framebuffer (§19.5 needs format validation) | `Texture2DDescription`, `TextureFormat` enum, `TextureFormatFlags` on the new enum, `Description` property on `Texture2D`, new constructor `Texture2D(GL, Texture2DDescription, byte[]?)` |
| First time a texture needs a non-default sampler | `TextureSampler`, `TextureMinificationFilter` / `TextureMagnificationFilter` / `TextureWrap` enums, `RenderContext.CreateTexture2DSampler`, pre-made `RenderContext.Samplers.{LinearRepeat, LinearClamp, NearestRepeat, NearestClamp}` |
| First multitextured shader (§26 day/night globe is the natural forcing function) | `TextureUnit`, `TextureUnits`, `RenderContext.TextureUnits[]`, bind flush at top of `Draw` |
| First tile-streaming pipeline (§22+) | `WritePixelBuffer`, `ReadPixelBuffer`, `PixelBufferHint`, `ImageFormat`, `ImageDatatype`, `Texture2D.CopyFromBuffer`, `Texture2D.CopyToBuffer` |
| Debugging scenarios | `Texture2D.Save(string filename)` |
| §11 terrain ray-casting | `RenderContext.CreateTexture2DRectangle` factory |

The current `Texture2D.Bind(unit)` can stay as a convenience that forwards to `TextureUnits[unit].Texture = this` once the unit collection lands. Or it can be removed once call sites all go through `TextureUnits`.

**The existing `FromFile` method** (StbImageSharp-backed) stays as the one-liner for loading images in demos; it internally calls the new `(GL, Texture2DDescription, byte[])` constructor once that exists. For production tile streaming, `FromFile` is replaced with a tile-provider pipeline that uses `WritePixelBuffer` + async I/O.

---

## Section 19.5: Framebuffers

*Corresponds to Book Chapter 3, Section 3.7*

*OpenGlobe source: `Source/Renderer/Framebuffer.cs`, `Source/Renderer/ColorAttachments.cs`, `Source/Renderer/GL3x/FramebufferGL3x.cs`.*

*Files we build in this section:*

```
Geode.Rendering/
  Framebuffer.cs           -- the FBO wrapper
  ColorAttachments.cs      -- indexable collection of color slots
  TextureFormatFlags.cs    -- extension properties: ColorRenderable, DepthRenderable, etc.
```

Plus a `Description` property added to `Texture2D`.

Until now every draw call has written to the **default framebuffer** -- the one the window system provides. That framebuffer shows up on the screen. Many effects require writing to an **off-screen** target instead: a color buffer you sample in a later pass, a depth buffer you read for screen-space techniques, multiple color buffers written simultaneously for deferred shading. The abstraction for all of this is a **Framebuffer Object (FBO)**.

### What the book covers

Per Book §3.7, a framebuffer owns three attachment slots:

- **`ColorAttachments`** -- indexable collection, `framebuffer.ColorAttachments[index] = texture`. Up to `glGetIntegerv(GL_MAX_COLOR_ATTACHMENTS)` slots (typically 8). Each attachment is a `Texture2D` whose format must be color-renderable (e.g. `Rgba8`, `Rgba16f`).
- **`DepthAttachment`** -- single slot. `Texture2D` with a depth-renderable format (`Depth24`, `Depth32f`).
- **`DepthStencilAttachment`** -- single slot. `Texture2D` with a depth-stencil format (`Depth24Stencil8`). Mutually exclusive with a separate depth attachment.

Framebuffers are not shareable across GL contexts -- so in OpenGlobe they live on `Context`, not `Device`. In Geode the `RenderContext` owns the current framebuffer: assigning `RenderContext.Framebuffer = fb` makes it current; assigning `null` restores the default window framebuffer.

The book highlights four rules a good wrapper enforces at API boundary rather than letting GL surface them as opaque errors:

1. **Depth-required rule.** If `RenderState.DepthTest.Enabled` and the current framebuffer has no depth attachment, throw before the draw. Common beginner bug.
2. **Texture-format compatibility.** Reject an attachment whose format is not renderable for its slot. A `Depth24` texture cannot be a color attachment; an `Rgba8` texture cannot be a depth attachment. `Texture2DDescription` carries `ColorRenderable`, `DepthRenderable`, `DepthStencilRenderable` flags computed from the internal format.
3. **Fragment-output binding.** When a fragment shader declares `out vec4 dayColor; out vec4 nightColor;`, the binding of names to color-attachment indices is determined by `glGetFragDataLocation`. Write `framebuffer.ColorAttachments[shader.FragmentOutputs["dayColor"]] = dayTexture` so the binding is named, not positional. This is why Section 11's `ShaderProgram` exposes a `FragmentOutputs` collection.
4. **Delayed attachment binding.** Assigning to `ColorAttachments[i]` or `DepthAttachment` does *not* call `glNamedFramebufferTexture` immediately. Instead it marks the slot dirty. The FBO flushes pending attachment changes (plus `glNamedFramebufferDrawBuffers` updates for the draw-buffer mask) on its next `Bind()`. This matches the dirty-list pattern used for uniforms.

### Texture2D description + format flags

`Framebuffer` needs to inspect a texture's format to decide whether it can be used as a color, depth, or depth-stencil attachment. That requires `Texture2D` to carry its description.

**Extend `Texture2D` (Section 13):**

```csharp
// Additions to Texture2D:

public Texture2DDescription Description { get; }

public Texture2D(GL gl, Texture2DDescription description)
{
    Description = description;
    // ... existing DSA creation logic using description.Width, Height, InternalFormat ...
}

public readonly record struct Texture2DDescription(
    int Width,
    int Height,
    InternalFormat InternalFormat,
    bool GenerateMipmaps = false);
```

**New `TextureFormatFlags`** -- extension methods that answer "can this format be attached as X?":

```csharp
// Geode.Rendering/TextureFormatFlags.cs

using Silk.NET.OpenGL;

namespace Geode.Rendering
{
    /// <summary>
    /// Per-format capability flags used to validate framebuffer attachments.
    /// These follow the rules in the OpenGL 4.6 specification, Table 8.12
    /// (Framebuffer-attachable internal formats).
    /// </summary>
    public static class TextureFormatFlags
    {
        public static bool IsColorRenderable(this InternalFormat f) => f switch
        {
            InternalFormat.R8 or InternalFormat.R16 or InternalFormat.R16f or InternalFormat.R32f
                or InternalFormat.Rg8 or InternalFormat.Rg16 or InternalFormat.Rg16f or InternalFormat.Rg32f
                or InternalFormat.Rgb8 or InternalFormat.Rgb16 or InternalFormat.Rgb16f or InternalFormat.Rgb32f
                or InternalFormat.Rgba8 or InternalFormat.Rgba16 or InternalFormat.Rgba16f or InternalFormat.Rgba32f
                or InternalFormat.Srgb8 or InternalFormat.Srgb8Alpha8 => true,
            _ => false
        };

        public static bool IsDepthRenderable(this InternalFormat f) => f switch
        {
            InternalFormat.DepthComponent16 or InternalFormat.DepthComponent24
                or InternalFormat.DepthComponent32 or InternalFormat.DepthComponent32f => true,
            _ => false
        };

        public static bool IsDepthStencilRenderable(this InternalFormat f) => f switch
        {
            InternalFormat.Depth24Stencil8 or InternalFormat.Depth32fStencil8 => true,
            _ => false
        };
    }
}
```

### ColorAttachments collection

The indexable wrapper the book uses, so you can write:

```csharp
framebuffer.ColorAttachments[shader.FragmentOutputs["dayColor"]] = dayTexture;
framebuffer.ColorAttachments[shader.FragmentOutputs["nightColor"]] = nightTexture;
```

```csharp
// Geode.Rendering/ColorAttachments.cs

using System;

namespace Geode.Rendering
{
    /// <summary>
    /// Ordered, indexable collection of color attachments for a <see cref="Framebuffer"/>.
    /// Assignments are deferred: they mark the slot dirty, and the framebuffer
    /// flushes pending changes on its next Bind().
    /// </summary>
    public sealed class ColorAttachments
    {
        internal readonly Texture2D?[] Slots;
        internal bool Dirty;

        internal ColorAttachments(int maxColorAttachments)
        {
            Slots = new Texture2D?[maxColorAttachments];
        }

        /// <summary>The maximum number of slots. Equal to the `maxColorAttachments` passed to Framebuffer.</summary>
        public int Count => Slots.Length;

        /// <summary>
        /// Get or set the Texture2D at the given color attachment index.
        /// Setting null detaches. Setting a non-null texture validates that its
        /// internal format is color-renderable.
        /// </summary>
        public Texture2D? this[int index]
        {
            get => Slots[index];
            set
            {
                if (index < 0 || index >= Slots.Length)
                    throw new ArgumentOutOfRangeException(nameof(index));

                if (value is not null && !value.Description.InternalFormat.IsColorRenderable())
                    throw new InvalidOperationException(
                        $"Texture format {value.Description.InternalFormat} is not color-renderable.");

                if (Slots[index] == value) return;
                Slots[index] = value;
                Dirty = true;
            }
        }
    }
}
```

### Framebuffer

Uses OpenGL 4.6 DSA throughout. Attachment changes are queued; the next `Bind()` flushes them.

```csharp
// Geode.Rendering/Framebuffer.cs
//
// Off-screen render target. Owns ColorAttachments, DepthAttachment,
// DepthStencilAttachment. Validates attachment formats at assignment time
// and flushes pending GL attachment calls on Bind().
//
// DSA throughout: glCreateFramebuffers, glNamedFramebufferTexture,
// glNamedFramebufferDrawBuffers, glCheckNamedFramebufferStatus.

using Silk.NET.OpenGL;
using System;

namespace Geode.Rendering
{
    public sealed class Framebuffer : IDisposable
    {
        private readonly GL _gl;
        private readonly uint _handle;

        private Texture2D? _depthAttachment;
        private bool _depthDirty;

        private Texture2D? _depthStencilAttachment;
        private bool _depthStencilDirty;

        public uint Handle => _handle;
        public int Width { get; }
        public int Height { get; }

        /// <summary>Indexable color attachments. Assigning dirties; Bind flushes.</summary>
        public ColorAttachments ColorAttachments { get; }

        /// <summary>Single depth attachment. Incompatible with <see cref="DepthStencilAttachment"/>.</summary>
        public Texture2D? DepthAttachment
        {
            get => _depthAttachment;
            set
            {
                if (value is not null)
                {
                    if (_depthStencilAttachment is not null)
                        throw new InvalidOperationException("Framebuffer already has a depth-stencil attachment.");
                    if (!value.Description.InternalFormat.IsDepthRenderable())
                        throw new InvalidOperationException(
                            $"Texture format {value.Description.InternalFormat} is not depth-renderable.");
                }
                if (_depthAttachment == value) return;
                _depthAttachment = value;
                _depthDirty = true;
            }
        }

        /// <summary>Combined depth-stencil attachment. Incompatible with <see cref="DepthAttachment"/>.</summary>
        public Texture2D? DepthStencilAttachment
        {
            get => _depthStencilAttachment;
            set
            {
                if (value is not null)
                {
                    if (_depthAttachment is not null)
                        throw new InvalidOperationException("Framebuffer already has a separate depth attachment.");
                    if (!value.Description.InternalFormat.IsDepthStencilRenderable())
                        throw new InvalidOperationException(
                            $"Texture format {value.Description.InternalFormat} is not depth-stencil-renderable.");
                }
                if (_depthStencilAttachment == value) return;
                _depthStencilAttachment = value;
                _depthStencilDirty = true;
            }
        }

        /// <summary>True if this framebuffer has any depth attachment (either DepthAttachment or DepthStencilAttachment).</summary>
        public bool HasDepthAttachment => _depthAttachment is not null || _depthStencilAttachment is not null;

        public Framebuffer(GL gl, int width, int height, int maxColorAttachments = 1)
        {
            if (maxColorAttachments < 1 || maxColorAttachments > 8)
                throw new ArgumentOutOfRangeException(nameof(maxColorAttachments), "must be 1-8");

            _gl = gl;
            Width = width;
            Height = height;
            ColorAttachments = new ColorAttachments(maxColorAttachments);
            _handle = _gl.CreateFramebuffer();
        }

        /// <summary>
        /// Make this framebuffer the active draw target, flushing any pending
        /// attachment changes first. Called by RenderContext's Framebuffer setter.
        /// </summary>
        internal void Bind()
        {
            Clean();
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _handle);
        }

        /// <summary>
        /// Flush pending attachment changes. Called by Bind; exposed for Validate().
        /// </summary>
        public void Clean()
        {
            // Color attachments
            if (ColorAttachments.Dirty)
            {
                for (int i = 0; i < ColorAttachments.Slots.Length; i++)
                {
                    Texture2D? tex = ColorAttachments.Slots[i];
                    _gl.NamedFramebufferTexture(_handle,
                        FramebufferAttachment.ColorAttachment0 + i,
                        tex?.Handle ?? 0, 0);
                }
                UpdateDrawBuffers();
                ColorAttachments.Dirty = false;
            }

            if (_depthDirty)
            {
                _gl.NamedFramebufferTexture(_handle, FramebufferAttachment.DepthAttachment,
                    _depthAttachment?.Handle ?? 0, 0);
                _depthDirty = false;
            }

            if (_depthStencilDirty)
            {
                _gl.NamedFramebufferTexture(_handle, FramebufferAttachment.DepthStencilAttachment,
                    _depthStencilAttachment?.Handle ?? 0, 0);
                _depthStencilDirty = false;
            }
        }

        /// <summary>
        /// Flush pending changes and verify the framebuffer is complete.
        /// Throws if GL reports any incompleteness.
        /// </summary>
        public void Validate()
        {
            Clean();
            GLEnum status = _gl.CheckNamedFramebufferStatus(_handle, FramebufferTarget.Framebuffer);
            if (status != GLEnum.FramebufferComplete)
                throw new Exception($"Framebuffer is incomplete: {status}");
        }

        private unsafe void UpdateDrawBuffers()
        {
            int count = ColorAttachments.Slots.Length;
            Span<GLEnum> bufs = stackalloc GLEnum[count];
            int trailingNoneStart = count;

            for (int i = 0; i < count; i++)
                bufs[i] = ColorAttachments.Slots[i] is not null ? GLEnum.ColorAttachment0 + i : GLEnum.None;

            // Strip trailing Nones so we don't tell GL about slots we don't care about.
            while (trailingNoneStart > 0 && bufs[trailingNoneStart - 1] == GLEnum.None) trailingNoneStart--;

            if (trailingNoneStart == 0)
            {
                _gl.NamedFramebufferDrawBuffer(_handle, DrawBufferMode.None);
                return;
            }

            fixed (GLEnum* p = bufs)
                _gl.NamedFramebufferDrawBuffers(_handle, (uint)trailingNoneStart, p);
        }

        public void Dispose() => _gl.DeleteFramebuffer(_handle);
    }
}
```

### RenderContext integration

`RenderContext` owns the "currently bound framebuffer" state. A setter does the bind (which flushes pending attachments first).

```csharp
// Additions to RenderContext:

private Framebuffer? _currentFramebuffer;

/// <summary>
/// The currently bound framebuffer. Null = default (window) framebuffer.
/// Assignment flushes pending attachment changes and binds.
/// </summary>
public Framebuffer? Framebuffer
{
    get => _currentFramebuffer;
    set
    {
        _currentFramebuffer = value;
        if (value is null)
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        else
            value.Bind();
    }
}
```

And add the depth-required guard inside `Draw`:

```csharp
// Inside Draw, after ApplyRenderState:
if (drawState.RenderState.DepthTest.Enabled
    && _currentFramebuffer is not null
    && !_currentFramebuffer.HasDepthAttachment)
{
    throw new InvalidOperationException(
        "DepthTest is enabled but the current framebuffer has no depth attachment. " +
        "Attach a Texture2D with a depth-renderable format (Depth24, Depth32f) or disable DepthTest.");
}
```

### Example: multi-attachment render-to-texture (book-style)

Two color attachments plus a depth attachment. Shader names drive the routing.

```csharp
// Fragment shader:
//   out vec4 dayColor;
//   out vec4 nightColor;

Texture2D day   = new(gl, new Texture2DDescription(1920, 1080, InternalFormat.Rgba8));
Texture2D night = new(gl, new Texture2DDescription(1920, 1080, InternalFormat.Rgba8));
Texture2D depth = new(gl, new Texture2DDescription(1920, 1080, InternalFormat.DepthComponent32f));

Framebuffer fbo = new(gl, 1920, 1080, maxColorAttachments: 2);
fbo.ColorAttachments[shader.FragmentOutputs["dayColor"]]   = day;
fbo.ColorAttachments[shader.FragmentOutputs["nightColor"]] = night;
fbo.DepthAttachment = depth;
fbo.Validate();

renderContext.Framebuffer = fbo;
renderContext.Clear(clearState);
renderContext.Draw(primitiveType, drawState, sceneState);
renderContext.Framebuffer = null;

// day and night are now sampleable for a compositing pass
```

### Use cases we will encounter

- **§25 GPU Ray-Casted Globe.** Off-screen depth target so `gl_FragDepth` writes are composited in a second pass.
- **§26 Day/Night Globe.** Multiple color attachments for deferred shading (the example above).
- **Post-processing.** Bloom, tone mapping, SSAO -- scene renders to a texture, a full-screen pass composites.
- **High-resolution screen capture.** Render to a 4K or 8K FBO regardless of window size, read back via `glGetTextureImage`.

### D3D parity note

Book flags the D3D origin convention (Y-up in texture coords vs Y-down in pixel coords). A GL-only engine still hits it when saving FBO contents to disk -- the image is upside-down. Flip row order during `glReadPixels` or at save time. Document the flip in any future `Framebuffer.SaveToPng(path)` helper.

---

## Section 20: Window, Context, Render Loop, and Drawing a Triangle

*Corresponds to Book Chapter 3, Section 3.8: "Putting It All Together: Rendering a Triangle"*

This is the payoff section. We bring together every type defined in Part III into a working application that opens a window, creates a rendering context, and draws a colored triangle.

### What We Are Building

- A Silk.NET window (800x600, OpenGL 4.6 Core Profile)
- A `RenderContext` with debug output and [0,1] clip control
- A simple test shader (position + color passthrough)
- A triangle defined by 3 vertices with interleaved position + color
- A `DrawState` bundling shader + VAO + default `RenderState`
- A `SceneState` with a camera looking at the origin
- A `ClearState` that clears to dark blue each frame
- The render loop: clear, draw, swap

### Test Shaders

These shaders live in the `Geode.App/Shaders/` directory and are loaded at runtime. They are the simplest possible pair: the vertex shader transforms by the MVP matrix, and the fragment shader outputs the interpolated color.

**`Geode.App/Shaders/triangle.vert`**

```glsl
#version 460 core

// Vertex attributes (must match VertexAttrib indices in the VAO)
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aColor;

// Output to fragment shader (interpolated by rasterizer)
out vec3 vColor;

// Automatic uniform -- set by the engine before every draw (Section 19).
// Declaring it by name is enough; no call-site setup required.
uniform mat4 og_modelViewPerspectiveMatrix;

void main()
{
    gl_Position = og_modelViewPerspectiveMatrix * vec4(aPosition, 1.0);
    vColor = aColor;
}
```

**`Geode.App/Shaders/triangle.frag`**

```glsl
#version 460 core

in vec3 vColor;
out vec4 FragColor;

void main()
{
    FragColor = vec4(vColor, 1.0);
}
```

These files must be copied to the output directory. Add this to `Geode.App.csproj`:

```xml
<ItemGroup>
  <None Update="Shaders\**\*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

### Program.cs

The entry point that ties everything together.

```csharp
// Geode.App/Program.cs
//
// Entry point for the Geode virtual globe engine.
// Creates a window, initializes the renderer, and draws a test triangle.
//
// This is "Step 1" -- proof that the entire rendering pipeline works:
//   RenderContext -> Clear -> ApplyRenderState -> Bind Shader -> Set Uniforms -> Bind VAO -> Draw
//
// Book Chapter 3, Section 3.11.

using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using Silk.NET.Input;
using System;
using Geode.Core;
using Geode.Rendering;

namespace Geode.App
{
    public class Program
    {
        // ---------------------------------------------------------------
        // Application state
        // ---------------------------------------------------------------

        private static IWindow? _window;
        private static GL? _gl;
        private static RenderContext? _context;
        private static ShaderProgram? _shader;
        private static VertexArrayObject? _triangleVao;
        private static DrawState? _drawState;
        private static SceneState? _sceneState;
        private static ClearState? _clearState;

        // ---------------------------------------------------------------
        // Entry point
        // ---------------------------------------------------------------

        public static void Main(string[] args)
        {
            // Configure the window: 800x600, OpenGL 4.6 Core Profile.
            // VSync is on by default with Silk.NET.
            var options = WindowOptions.Default with
            {
                Size = new Vector2D<int>(800, 600),
                Title = "Geode -- Step 1: Triangle",
                API = new GraphicsAPI(
                    ContextAPI.OpenGL, ContextProfile.Core,
                    ContextFlags.Debug, new APIVersion(4, 6))
            };

            _window = Window.Create(options);
            _window.Load += OnLoad;
            _window.Render += OnRender;
            _window.Closing += OnClose;
            _window.Resize += OnResize;

            _window.Run();
        }

        // ---------------------------------------------------------------
        // Window event handlers
        // ---------------------------------------------------------------

        private static void OnLoad()
        {
            // Obtain the GL context from the window.
            // This must be called on the render thread.
            _gl = GL.GetApi(_window!);

            // Print GPU info
            Console.WriteLine($"OpenGL {_gl.GetStringS(StringName.Version)}");
            Console.WriteLine($"GPU:   {_gl.GetStringS(StringName.Renderer)}");
            Console.WriteLine($"GLSL:  {_gl.GetStringS(StringName.ShadingLanguageVersion)}");

            // Set up keyboard input for ESC to close
            var input = _window!.CreateInput();
            foreach (var keyboard in input.Keyboards)
                keyboard.KeyDown += OnKeyDown;

            // Create the render context (enables debug output, sets clip control)
            _context = new RenderContext(_gl);

            // Load the test shader from files.
            // The shader files are copied to the output directory by the csproj.
            _shader = ShaderProgram.FromFiles(_gl,
                "Shaders/triangle.vert",
                "Shaders/triangle.frag");

            // Define a triangle: 3 vertices with interleaved position (vec3) + color (vec3).
            // The triangle is centered at the origin in the XY plane.
            float[] vertices =
            {
                // Position (x, y, z)     Color (r, g, b)
                -0.5f, -0.5f, 0.0f,      1.0f, 0.0f, 0.0f,  // Bottom-left:  red
                 0.5f, -0.5f, 0.0f,      0.0f, 1.0f, 0.0f,  // Bottom-right: green
                 0.0f,  0.5f, 0.0f,      0.0f, 0.0f, 1.0f,  // Top-center:   blue
            };

            // Indices: a single triangle (3 vertices, CCW winding as seen from +Z)
            uint[] indices = { 0, 1, 2 };

            // Create the VAO with two attributes:
            //   location 0: position (3 floats)
            //   location 1: color    (3 floats)
            _triangleVao = new VertexArrayObject(_gl, vertices, indices,
                new VertexAttrib(0, 3),
                new VertexAttrib(1, 3));

            // Create the draw state: default render state + shader + VAO
            _drawState = new DrawState(_shader, _triangleVao);

            // Disable depth testing and face culling for a simple 2D triangle.
            // These would cull our triangle since it faces +Z and we are looking from +Z.
            _drawState.RenderState.DepthTest.Enabled = false;
            _drawState.RenderState.FacetCulling.Enabled = false;

            // Set up the scene: camera at (0, 0, 3) looking at the origin.
            _sceneState = new SceneState();
            _sceneState.Camera.Eye = new Vector3D(0, 0, 3);
            _sceneState.Camera.Target = new Vector3D(0, 0, 0);
            _sceneState.Camera.Up = new Vector3D(0, 1, 0);
            _sceneState.Camera.FieldOfViewY = Trigonometry.ToRadians(60.0);
            _sceneState.Camera.AspectRatio = 800.0 / 600.0;
            _sceneState.Camera.NearPlane = 0.1;
            _sceneState.Camera.FarPlane = 100.0;

            // Clear to dark blue (evokes the ocean/sky background of a virtual globe)
            _clearState = new ClearState
            {
                Color = new System.Numerics.Vector4(0.05f, 0.05f, 0.15f, 1.0f),
                Depth = 1.0f
            };
        }

        private static void OnRender(double deltaTime)
        {
            // Clear the framebuffer
            _context!.Clear(_clearState!);

            // Draw the triangle
            _context.Draw(PrimitiveType.Triangles, _drawState!, _sceneState!);
        }

        private static void OnResize(Vector2D<int> size)
        {
            // Update the viewport to match the new window size
            _gl?.Viewport(0, 0, (uint)size.X, (uint)size.Y);

            // Update the camera aspect ratio so the triangle does not stretch
            if (_sceneState != null && size.Y > 0)
            {
                _sceneState.Camera.AspectRatio = (double)size.X / size.Y;
            }
        }

        private static void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
        {
            if (key == Key.Escape)
                _window?.Close();
        }

        private static void OnClose()
        {
            // Dispose in reverse creation order
            _triangleVao?.Dispose();
            _shader?.Dispose();
            _context?.Dispose();
            _gl?.Dispose();
        }
    }
}
```

**Line count:** ~140 lines.

### What You Should See

When you run `dotnet run --project Geode.App`, a window opens with a dark blue background and a triangle centered in the viewport:

- The bottom-left vertex is **red**
- The bottom-right vertex is **green**
- The top vertex is **blue**
- The colors blend smoothly across the triangle (the rasterizer interpolates the `vColor` varying)

The console prints the OpenGL version, GPU name, and GLSL version. If there are any GL errors (e.g., from a driver quirk), the debug callback prints them with severity level.

### What This Proves

This simple triangle validates the entire rendering pipeline:

1. **ShaderProgram** compiled and linked the GLSL sources without error.
2. **VertexArrayObject** correctly set up the vertex format -- the GPU found the position and color attributes at the right locations and strides.
3. **BufferObject** uploaded vertex and index data to the GPU via DSA.
4. **DrawState** bundled the render state, shader, and VAO.
5. **RenderContext** applied the render state, called `drawState.ShaderProgram.Bind(...)` (which runs every `DrawAutomaticUniform`, dirties those that changed, then flushes the dirty list with `glProgramUniform*`), bound the VAO, and issued `glDrawElements`.
6. **SceneState** computed the view and projection matrices in double precision and converted them to float for GPU upload.
7. **ClearState** cleared the framebuffer to the correct color.

Every class in `Geode.Rendering` was exercised. The renderer works.

### Next Steps

Part IV will replace this triangle with a tessellated ellipsoid -- the actual globe. The `SceneState` camera will be positioned at a realistic altitude above the WGS84 surface, and the shaders will compute per-pixel lighting using the geodetic surface normal. But the rendering pipeline -- `ClearState` -> `DrawState` -> `RenderContext.Draw()` -- remains exactly the same. That is the power of the abstraction.

---

## Section 20.5: Resources

*Corresponds to Book Chapter 3, Section 3.9: "Resources"*

The book closes Chapter 3 with a reading list. Geode mirrors most of it and adds modern references specific to OpenGL 4.6 DSA and .NET.

### Book-cited references (still authoritative)

- **Wright, Haemel, Sellers, Lipchak.** *OpenGL SuperBible* -- the 7th edition covers OpenGL 4.5 DSA throughout, which is what Geode uses. Best single book on the modern GL pipeline.
- **Segal, Akeley.** *The OpenGL Graphics System: A Specification* -- the official Khronos spec. Download the 4.6 Core Profile PDF from [khronos.org/registry/OpenGL/specs/gl/glspec46.core.pdf](https://www.khronos.org/registry/OpenGL/specs/gl/glspec46.core.pdf). Read the sections relevant to your current work, not cover-to-cover.
- **Rost, Licea-Kane.** *OpenGL Shading Language* (Orange Book) -- still the best GLSL reference for concept-level understanding. 3rd edition covers GLSL 1.50; for 4.60-specific features (compute shaders, SPIR-V) see the Khronos GLSL spec.
- **Eberly.** *3D Game Engine Design* -- general engine architecture; complements this book's narrower focus. See also Eberly's *GPU Computing Gems* for the compute-shader material Geode will use in future LOD work.
- **McReynolds, Blythe.** *Advanced Graphics Programming Using OpenGL* -- older (2005, fixed-function era) but many algorithmic techniques still relevant, especially for blending and stencil tricks.

### Additions for OpenGL 4.6

- **[learnopengl.com](https://learnopengl.com)** by Joey de Vries -- free, up-to-date, idiomatic modern GL. Read the "Getting Started" chapters even if you think you know GL; the 4.5 DSA examples in the later chapters show the DSA patterns Geode uses.
- **[docs.gl](https://docs.gl)** -- per-function reference with a version selector. Faster than the Khronos wiki for quick lookups.
- **Khronos OpenGL 4.6 Core Profile specification** -- cited above. Section 8 (Textures) and Section 9 (Framebuffer Objects) are required reading before extending Section 13 and Section 19.5.
- **Nathan Reed, *Depth Precision Visualized*** -- [reedbeta.com/blog/depth-precision-visualized](https://www.reedbeta.com/blog/depth-precision-visualized/). The canonical explanation of reversed-Z depth buffers. Pair with Section 28 when you build it.
- **Emil Persson (Humus), *A couple of notes about Z*** -- [humus.name/index.php?page=Comments&ID=255](http://www.humus.name/index.php?page=Comments&ID=255). Complements Reed's post with practical engine-integration notes.

### Additions for Silk.NET and .NET

- **[Silk.NET docs](https://dotnet.github.io/Silk.NET/)** and the [samples repository](https://github.com/dotnet/Silk.NET/tree/main/examples). The `OpenGL/Tutorial` samples show idiomatic DSA usage in C#.
- **.NET `System.Numerics.Matrix4x4` row-major vs OpenGL column-major** -- understand the transpose convention before debugging matrix bugs. Silk.NET's `UniformMatrix4(location, count, transpose, data)` accepts either; pass `transpose=true` when handing it a `System.Numerics.Matrix4x4*`.

### Production virtual globe references

- **[Cesium source](https://github.com/CesiumGS/cesium)** -- Patrick Cozzi's follow-on to OpenGlobe. Production tile LOD, terrain quantization, imagery layer blending. The JavaScript is readable even if you don't plan to port it.
- **[CesiumJS Architecture](https://cesium.com/learn/cesiumjs-learn/cesiumjs-architecture/)** -- overview of the runtime's scene graph, tile scheduler, and camera system.
- **[Outerra blog](http://outerra.blogspot.com/)** by Brano Kemen -- the original logarithmic-depth articles the book cites, plus planet-scale rendering techniques not covered by the book (procedural terrain, atmospheric scattering at scale).

### Math and graphics theory

- **Eric Lengyel, *Foundations of Game Engine Development*** Vol. 1 (Mathematics) and Vol. 2 (Rendering) -- modern linear algebra treatment aligned with modern GPU pipelines. Strong on coordinate-system conventions and matrix derivations.
- **Akenine-Moller, Haines, Hoffman, Pesce, Iwanicki, Hillaire.** *Real-Time Rendering* (4th ed.) -- encyclopedic reference for lighting, visibility, shadows. Chapters 4 (transforms) and 5 (shading basics) complement Book Ch 3; Chapter 7 (light and color) complements future globe shading work.

### OpenGlobe source

- **[virtualglobebook.com](http://virtualglobebook.com)** -- the book's companion website, with errata and the OpenGlobe source code.
- **[OpenGlobe repository](https://github.com/virtualglobebook/OpenGlobe)** -- the C# reference implementation the book describes. MIT-licensed; useful to cross-reference when this guide's approach differs from the book's.

---

*End of Part III. Part IV continues with globe tessellation and terrain rendering.*

# Part IV -- Globe Rendering

*Corresponds to Book Chapter 4: "Globe Rendering"*

With the renderer infrastructure in place (Part III), we can now render the Earth. This part builds five incremental steps: tessellate the ellipsoid into triangles, position the camera, shade the surface with Phong lighting, overlay a latitude-longitude grid, and finally replace the tessellated mesh with a pixel-perfect ray-casted globe.

Each step adds one or two source files and one or more shaders. Every step compiles and runs independently -- you can see the globe evolve from a flat-shaded wireframe to a textured, lit, day/night planet.

---

## Section 21: Step 2 -- Globe Tessellation

*Corresponds to Book Chapter 4, Section 4.1, Listings 4.1-4.5*

*OpenGlobe sources: `SubdivisionSphereTessellator.cs`, `GeographicGridEllipsoidTessellator.cs`*

### Why Tessellation Matters

The GPU draws triangles. An ellipsoid is a smooth surface with infinite curvature -- it contains no flat faces. To render it, we must approximate it with a mesh of flat triangles. The quality of this approximation determines how smooth the globe looks at any given zoom level.

Two competing concerns drive the design:

1. **Visual quality.** More triangles produce a smoother silhouette. At a coarse resolution, the globe looks like a faceted gem. At a fine resolution, individual triangles are smaller than pixels and the silhouette is indistinguishable from a true ellipsoid.

2. **Performance.** Every triangle costs vertex processing, rasterization, and fragment shading. A globe with 10 million triangles is wasteful when the viewport is 1920x1080 (about 2 million pixels). There is a sweet spot where adding more triangles produces no visible improvement.

The tessellator's job is to produce two arrays:
- `float[] positions` -- interleaved vertex data (position XYZ + normal XYZ + texture coordinate UV), 8 floats per vertex
- `uint[] indices` -- triangle indices into the vertex array, 3 per triangle

We implement two tessellation algorithms. Each has different trade-offs.

### Algorithm 1: Subdivision Surfaces

The subdivision approach starts with a simple polyhedron (a tetrahedron -- 4 vertices, 4 triangles) inscribed in the unit sphere. Each subdivision pass splits every triangle into four smaller triangles by computing edge midpoints, then projects those midpoints onto the ellipsoid surface. After `n` subdivisions, the mesh has `4 * 4^n` triangles.

| Subdivisions | Triangles | Vertices (approx) |
|---|---|---|
| 0 | 4 | 4 |
| 1 | 16 | 10 |
| 2 | 64 | 34 |
| 3 | 256 | 130 |
| 4 | 1,024 | 514 |
| 5 | 4,096 | 2,050 |
| 6 | 16,384 | 8,194 |
| 7 | 65,536 | 32,770 |

The key advantage of subdivision is **uniform triangle size** -- every triangle on the sphere covers approximately the same solid angle. There are no degenerate slivers near the poles, unlike the geographic grid approach.

```csharp
// Geode.Core/SubdivisionSphereTessellator.cs
//
// Book Section 4.1, Listings 4.1-4.3
// OpenGlobe: Source/Core/Tessellation/SubdivisionSphereTessellator.cs
//
// Generates an ellipsoid mesh by recursive subdivision of a tetrahedron.
// Each subdivision splits every triangle into 4 by computing edge midpoints
// and projecting them onto the ellipsoid surface.

using System;
using System.Collections.Generic;

namespace Geode.Core
{
    /// <summary>
    /// Output of a tessellation: interleaved vertex data and triangle indices.
    /// Vertex layout: [px, py, pz, nx, ny, nz, s, t] -- 8 floats per vertex.
    /// </summary>
    public sealed class MeshData
    {
        /// <summary>
        /// Interleaved vertex data: position (3), normal (3), texcoord (2) per vertex.
        /// Stride = 8 floats.
        /// </summary>
        public float[] Vertices { get; }

        /// <summary>
        /// Triangle indices, 3 per triangle. Counter-clockwise winding (front-facing).
        /// </summary>
        public uint[] Indices { get; }

        /// <summary>Number of vertices (Vertices.Length / 8).</summary>
        public int VertexCount { get; }

        /// <summary>Number of triangles (Indices.Length / 3).</summary>
        public int TriangleCount { get; }

        public MeshData(float[] vertices, uint[] indices)
        {
            Vertices = vertices;
            Indices = indices;
            VertexCount = vertices.Length / 8;
            TriangleCount = indices.Length / 3;
        }
    }

    /// <summary>
    /// Generates an ellipsoid mesh by recursive subdivision of a regular tetrahedron.
    /// </summary>
    public static class SubdivisionSphereTessellator
    {
        /// <summary>
        /// Computes a tessellated ellipsoid mesh.
        /// </summary>
        /// <param name="numberOfSubdivisions">
        /// Number of recursive subdivision passes. 0 = tetrahedron (4 triangles).
        /// Each pass quadruples the triangle count. Values above 8 are not recommended
        /// (4^8 = 65536 triangles, 4^9 = 262144 -- diminishing returns).
        /// </param>
        /// <param name="ellipsoid">
        /// The ellipsoid to tessellate. Vertices are projected onto this surface.
        /// </param>
        /// <returns>A MeshData containing interleaved vertices and triangle indices.</returns>
        public static MeshData Compute(int numberOfSubdivisions, Ellipsoid ellipsoid)
        {
            if (numberOfSubdivisions < 0)
                throw new ArgumentOutOfRangeException(nameof(numberOfSubdivisions),
                    "Number of subdivisions must be non-negative.");

            // ----------------------------------------------------------
            // Step 1: Create the initial tetrahedron (Equation 4.1)
            // ----------------------------------------------------------
            // Four vertices of a regular tetrahedron inscribed in the unit sphere.
            // These coordinates come from embedding the tetrahedron so that one
            // vertex is at (0, 0, 1) and the base is symmetric around the Z axis.
            //
            // v0 = (0, 0, 1)
            // v1 = (sqrt(8/9), 0, -1/3)
            // v2 = (-sqrt(2/9), sqrt(2/3), -1/3)
            // v3 = (-sqrt(2/9), -sqrt(2/3), -1/3)

            double oneThird = 1.0 / 3.0;
            double sqrt8Over9 = Math.Sqrt(8.0 / 9.0);
            double sqrt2Over9 = Math.Sqrt(2.0 / 9.0);
            double sqrt2Over3 = Math.Sqrt(2.0 / 3.0);

            Vector3D v0 = new Vector3D(0.0, 0.0, 1.0);
            Vector3D v1 = new Vector3D(sqrt8Over9, 0.0, -oneThird);
            Vector3D v2 = new Vector3D(-sqrt2Over9, sqrt2Over3, -oneThird);
            Vector3D v3 = new Vector3D(-sqrt2Over9, -sqrt2Over3, -oneThird);

            // The four triangles of the tetrahedron, wound counter-clockwise
            // as seen from outside the sphere.
            List<Vector3D> positions = new List<Vector3D> { v0, v1, v2, v3 };
            List<TriangleIndices> triangles = new List<TriangleIndices>
            {
                new TriangleIndices(0, 1, 2),
                new TriangleIndices(0, 2, 3),
                new TriangleIndices(0, 3, 1),
                new TriangleIndices(1, 3, 2),
            };

            // ----------------------------------------------------------
            // Step 2: Recursive subdivision
            // ----------------------------------------------------------
            // For each subdivision pass, every triangle is split into 4:
            //
            //        v0
            //       / \
            //      m01--m02
            //     / \ / \
            //    v1--m12--v2
            //
            // The midpoints (m01, m02, m12) are computed, normalized to the
            // unit sphere, and deduplicated via a dictionary keyed on the
            // (min, max) index pair of the original edge.

            for (int i = 0; i < numberOfSubdivisions; i++)
            {
                // Edge midpoint cache: maps (minIndex, maxIndex) -> new vertex index
                Dictionary<(int, int), int> edgeMidpoints = new Dictionary<(int, int), int>();
                List<TriangleIndices> newTriangles = new List<TriangleIndices>(triangles.Count * 4);

                foreach (TriangleIndices tri in triangles)
                {
                    int m01 = GetOrCreateMidpoint(positions, edgeMidpoints, tri.I0, tri.I1);
                    int m12 = GetOrCreateMidpoint(positions, edgeMidpoints, tri.I1, tri.I2);
                    int m02 = GetOrCreateMidpoint(positions, edgeMidpoints, tri.I0, tri.I2);

                    // Four new triangles, maintaining counter-clockwise winding
                    newTriangles.Add(new TriangleIndices(tri.I0, m01, m02));
                    newTriangles.Add(new TriangleIndices(m01, tri.I1, m12));
                    newTriangles.Add(new TriangleIndices(m01, m12, m02));
                    newTriangles.Add(new TriangleIndices(m02, m12, tri.I2));
                }

                triangles = newTriangles;
            }

            // ----------------------------------------------------------
            // Step 3: Project onto ellipsoid and build output arrays
            // ----------------------------------------------------------
            int vertexCount = positions.Count;
            float[] vertices = new float[vertexCount * 8];
            uint[] indices = new uint[triangles.Count * 3];

            Vector3D radii = ellipsoid.Radii;

            for (int i = 0; i < vertexCount; i++)
            {
                // The position is on the unit sphere. Scale by ellipsoid radii.
                Vector3D unitPos = positions[i].Normalize();
                Vector3D worldPos = new Vector3D(
                    unitPos.X * radii.X,
                    unitPos.Y * radii.Y,
                    unitPos.Z * radii.Z);

                // The geodetic surface normal at this position
                Vector3D normal = ellipsoid.GeodeticSurfaceNormal(worldPos);

                // Texture coordinates from geodetic normal (Equation 4.5)
                // s = atan2(ny, nx) / 2pi + 0.5
                // t = asin(nz) / pi + 0.5
                double s = Math.Atan2(normal.Y, normal.X) / Trigonometry.TwoPi + 0.5;
                double t = Math.Asin(Math.Clamp(normal.Z, -1.0, 1.0)) / Math.PI + 0.5;

                int baseIndex = i * 8;
                vertices[baseIndex + 0] = (float)worldPos.X;
                vertices[baseIndex + 1] = (float)worldPos.Y;
                vertices[baseIndex + 2] = (float)worldPos.Z;
                vertices[baseIndex + 3] = (float)normal.X;
                vertices[baseIndex + 4] = (float)normal.Y;
                vertices[baseIndex + 5] = (float)normal.Z;
                vertices[baseIndex + 6] = (float)s;
                vertices[baseIndex + 7] = (float)t;
            }

            for (int i = 0; i < triangles.Count; i++)
            {
                indices[i * 3 + 0] = (uint)triangles[i].I0;
                indices[i * 3 + 1] = (uint)triangles[i].I1;
                indices[i * 3 + 2] = (uint)triangles[i].I2;
            }

            return new MeshData(vertices, indices);
        }

        /// <summary>
        /// Finds or creates the midpoint vertex for the edge between index a and index b.
        /// The midpoint is normalized to the unit sphere before being stored.
        /// </summary>
        private static int GetOrCreateMidpoint(
            List<Vector3D> positions,
            Dictionary<(int, int), int> cache,
            int a, int b)
        {
            // Canonical key: always (smaller, larger) to ensure a-b == b-a
            var key = a < b ? (a, b) : (b, a);

            if (cache.TryGetValue(key, out int existingIndex))
                return existingIndex;

            // Compute midpoint and project onto unit sphere
            Vector3D midpoint = (positions[a] + positions[b]) * 0.5;
            midpoint = midpoint.Normalize();

            int newIndex = positions.Count;
            positions.Add(midpoint);
            cache[key] = newIndex;
            return newIndex;
        }

        /// <summary>Simple struct to hold three vertex indices for a triangle.</summary>
        private readonly struct TriangleIndices
        {
            public readonly int I0;
            public readonly int I1;
            public readonly int I2;

            public TriangleIndices(int i0, int i1, int i2)
            {
                I0 = i0;
                I1 = i1;
                I2 = i2;
            }
        }
    }
}
```

**How it works:**

1. **Tetrahedron.** The four vertices from Equation 4.1 are placed on the unit sphere. They form the simplest possible closed triangular mesh.

2. **Subdivision.** Each triangle is split into four by computing midpoints of its three edges. The midpoints are normalized (projected back onto the unit sphere). A dictionary prevents creating duplicate vertices when two adjacent triangles share an edge.

3. **Ellipsoid projection.** After all subdivisions, each unit-sphere position `(x, y, z)` is scaled by `(radii.X, radii.Y, radii.Z)` to produce the actual ellipsoid position. The geodetic surface normal is computed from the ellipsoid equation, and texture coordinates are derived from Equation 4.5.

### Algorithm 2: Geographic Grid

The geographic grid approach parameterizes the ellipsoid directly using longitude and latitude. It sweeps longitude from 0 to 2*pi (slices) and latitude from -pi/2 to pi/2 (stacks), computing the Cartesian position at each grid point from the parametric ellipsoid equation:

```
x = a * cos(latitude) * cos(longitude)
y = b * cos(latitude) * sin(longitude)  
z = c * sin(latitude)
```

where `a`, `b`, `c` are the ellipsoid radii.

This produces a regular grid with predictable triangle layout, making it easy to generate texture coordinates. The downside is **pole singularity**: triangles near the poles become degenerate slivers, wasting vertex processing on near-zero-area geometry.

```csharp
// Geode.Core/GeographicGridEllipsoidTessellator.cs
//
// Book Section 4.1, Listings 4.4-4.5
// OpenGlobe: Source/Core/Tessellation/GeographicGridEllipsoidTessellator.cs
//
// Generates an ellipsoid mesh using a latitude-longitude grid.
// Simple to implement, straightforward texture mapping,
// but produces degenerate triangles at the poles.

using System;

namespace Geode.Core
{
    /// <summary>
    /// Generates an ellipsoid mesh from a regular longitude-latitude grid.
    /// </summary>
    public static class GeographicGridEllipsoidTessellator
    {
        /// <summary>
        /// Computes a tessellated ellipsoid mesh using a geographic grid.
        /// </summary>
        /// <param name="stacks">
        /// Number of latitude bands (rows). Must be >= 2.
        /// Higher values produce a smoother mesh.
        /// </param>
        /// <param name="slices">
        /// Number of longitude segments (columns). Must be >= 3.
        /// Higher values produce a smoother mesh.
        /// </param>
        /// <param name="ellipsoid">
        /// The ellipsoid to tessellate. Vertices are computed on this surface.
        /// </param>
        /// <returns>A MeshData containing interleaved vertices and triangle indices.</returns>
        public static MeshData Compute(int stacks, int slices, Ellipsoid ellipsoid)
        {
            if (stacks < 2)
                throw new ArgumentOutOfRangeException(nameof(stacks),
                    "Stacks must be at least 2.");
            if (slices < 3)
                throw new ArgumentOutOfRangeException(nameof(slices),
                    "Slices must be at least 3.");

            Vector3D radii = ellipsoid.Radii;

            // Total vertices: (stacks + 1) * (slices + 1)
            // We duplicate the first/last longitude column so texture coords
            // wrap correctly (s=0 and s=1 are separate vertices at the seam).
            int vertexRows = stacks + 1;
            int vertexCols = slices + 1;
            int vertexCount = vertexRows * vertexCols;

            float[] vertices = new float[vertexCount * 8];

            double deltaLat = Math.PI / stacks;
            double deltaLon = Trigonometry.TwoPi / slices;

            // ----------------------------------------------------------
            // Generate vertices
            // ----------------------------------------------------------
            int vIdx = 0;
            for (int row = 0; row <= stacks; row++)
            {
                // Latitude ranges from +pi/2 (north pole) to -pi/2 (south pole)
                double latitude = Trigonometry.HalfPi - row * deltaLat;
                double cosLat = Math.Cos(latitude);
                double sinLat = Math.Sin(latitude);

                // Texture t coordinate: 0 at north pole, 1 at south pole
                double t = (double)row / stacks;

                for (int col = 0; col <= slices; col++)
                {
                    // Longitude ranges from 0 to 2*pi
                    double longitude = col * deltaLon;
                    double cosLon = Math.Cos(longitude);
                    double sinLon = Math.Sin(longitude);

                    // Parametric ellipsoid position
                    double px = radii.X * cosLat * cosLon;
                    double py = radii.Y * cosLat * sinLon;
                    double pz = radii.Z * sinLat;

                    // Geodetic surface normal
                    Vector3D position = new Vector3D(px, py, pz);
                    Vector3D normal = ellipsoid.GeodeticSurfaceNormal(position);

                    // Texture s coordinate: 0 at the seam, 1 at the seam (wrapped)
                    double s = (double)col / slices;

                    vertices[vIdx + 0] = (float)px;
                    vertices[vIdx + 1] = (float)py;
                    vertices[vIdx + 2] = (float)pz;
                    vertices[vIdx + 3] = (float)normal.X;
                    vertices[vIdx + 4] = (float)normal.Y;
                    vertices[vIdx + 5] = (float)normal.Z;
                    vertices[vIdx + 6] = (float)s;
                    vertices[vIdx + 7] = (float)t;

                    vIdx += 8;
                }
            }

            // ----------------------------------------------------------
            // Generate indices
            // ----------------------------------------------------------
            // Each cell in the grid is a quad, split into two triangles.
            // North pole fan: first row of quads degenerates into triangles
            // because all top-row vertices coincide at the pole.
            // South pole fan: same for the last row.
            // This is handled naturally by the quad logic -- degenerate
            // triangles are culled by the GPU (zero area).

            int triangleCount = stacks * slices * 2;
            uint[] indices = new uint[triangleCount * 3];

            int iIdx = 0;
            for (int row = 0; row < stacks; row++)
            {
                for (int col = 0; col < slices; col++)
                {
                    // Four corners of the current grid cell
                    uint topLeft = (uint)(row * vertexCols + col);
                    uint topRight = topLeft + 1;
                    uint bottomLeft = (uint)((row + 1) * vertexCols + col);
                    uint bottomRight = bottomLeft + 1;

                    // Triangle 1: top-left, bottom-left, bottom-right
                    indices[iIdx + 0] = topLeft;
                    indices[iIdx + 1] = bottomLeft;
                    indices[iIdx + 2] = bottomRight;

                    // Triangle 2: top-left, bottom-right, top-right
                    indices[iIdx + 3] = topLeft;
                    indices[iIdx + 4] = bottomRight;
                    indices[iIdx + 5] = topRight;

                    iIdx += 6;
                }
            }

            return new MeshData(vertices, indices);
        }
    }
}
```

### Comparison: Tessellation Approaches

| Property | Subdivision | Geographic Grid | Cube-Map (not implemented) |
|---|---|---|---|
| **Triangle uniformity** | Excellent -- nearly equal solid angle per triangle | Poor -- degenerate slivers at poles | Good -- uniform per face, slight distortion at corners |
| **Texture mapping** | Requires atan2/asin computation | Natural latitude-longitude grid | Requires cube-map projection |
| **Implementation complexity** | Medium (edge dedup dictionary) | Low (nested loops) | Medium (6 faces + stitching) |
| **Pole behavior** | No singularity | Degenerate triangles at poles | No singularity |
| **LOD adaptivity** | Easy (vary subdivision count) | Easy (vary stacks/slices) | Easy (vary per-face resolution) |
| **Best for** | Uniform visual quality, low polygon count | Simple visualization, familiar parameterization | Terrain tile mapping (chapter 12+) |

**Recommendation:** Use the geographic grid for quick prototyping and texture mapping tests. Use subdivision for production rendering where triangle uniformity matters. We will use the geographic grid for most examples in this guide because it produces predictable texture coordinates.

---

## Section 22: Step 3 -- Camera System

*Corresponds to Book Chapter 4 camera discussion; see also Section 16 (SceneState)*

The camera is already implemented in `Geode.Rendering`. `CameraState` holds:
- `Eye` (Vector3D) -- the camera position in world space, double-precision
- `Target` (Vector3D) -- the point the camera looks at
- `Up` (Vector3D) -- the up direction
- `FieldOfViewY` (double) -- vertical field of view in radians
- `AspectRatio` (double) -- viewport width / height
- `NearPlane` (double) -- near clip distance
- `FarPlane` (double) -- far clip distance

`SceneState` bundles `CameraState` with lighting and time parameters and computes the `ViewMatrix` and `ProjectionMatrix` as `float[]` arrays (column-major, 4x4) suitable for upload to the GPU.

For globe rendering, the camera is typically positioned at some altitude above the surface, looking toward the center of the Earth:

```csharp
// Example camera setup for viewing the globe from above the equator
double altitude = 20_000_000.0; // 20,000 km above the surface
var cameraState = new CameraState
{
    Eye = new Vector3D(altitude + Constants.Wgs84SemiMajorAxis, 0.0, 0.0),
    Target = Vector3D.Zero,  // Look at Earth's center
    Up = Vector3D.UnitZ,     // Z-up convention
    FieldOfViewY = Trigonometry.ToRadians(60.0),
    AspectRatio = 1920.0 / 1080.0,
    NearPlane = 1.0,
    FarPlane = altitude * 3.0
};
```

The precision challenges of this camera setup (Eye at ~26 million meters with float MVP matrices) are addressed in Part V (Section 27). For now, the camera works correctly at moderate zoom levels.

---

## Section 23: Step 4 -- Phong Shading

*Corresponds to Book Chapter 4, Section 4.2, Listings 4.6-4.11*

*OpenGlobe sources: `Shaders/Globe/` directory*

We build the globe shaders in five progressive stages. Each stage adds one capability and produces a visible result. This incremental approach makes debugging straightforward -- if stage 3 breaks, you know the problem is in the lighting math, not the vertex transform.

### Stage 1: Pass-Through (Solid Color)

The simplest possible globe shader. The vertex shader transforms positions by the MVP matrix. The fragment shader outputs a single solid color. This verifies that tessellation, buffer upload, and the draw pipeline work.

```glsl
// Geode.App/Shaders/globe_passthrough.vert
//
// Book Section 4.2, Listing 4.6 (simplified)
// Minimal vertex shader: transform by MVP only.

#version 460 core

layout(location = 0) in vec3 a_position;
layout(location = 1) in vec3 a_normal;
layout(location = 2) in vec2 a_texcoord;

uniform mat4 og_modelViewPerspectiveMatrix;

void main()
{
    gl_Position = og_modelViewPerspectiveMatrix * vec4(a_position, 1.0);
}
```

```glsl
// Geode.App/Shaders/globe_passthrough.frag
//
// Solid color output. Use this to verify the mesh renders correctly
// before adding lighting.

#version 460 core

out vec4 FragColor;

uniform vec3 u_color;

void main()
{
    FragColor = vec4(u_color, 1.0);
}
```

### Stage 2: Normals Visualization

Output the surface normal as an RGB color. This is the single most useful debugging shader in graphics. If the normals are wrong, lighting will be wrong, and this shader makes the problem immediately visible.

```glsl
// Geode.App/Shaders/globe_normals.vert

#version 460 core

layout(location = 0) in vec3 a_position;
layout(location = 1) in vec3 a_normal;
layout(location = 2) in vec2 a_texcoord;

uniform mat4 og_modelViewPerspectiveMatrix;
uniform mat4 u_modelView;

out vec3 v_normalEC;

void main()
{
    // Transform normal to eye/camera space.
    // We use the upper-left 3x3 of the model-view matrix.
    // For a uniform-scale model matrix, this is correct.
    // For non-uniform scale, use the inverse-transpose instead.
    v_normalEC = mat3(u_modelView) * a_normal;

    gl_Position = og_modelViewPerspectiveMatrix * vec4(a_position, 1.0);
}
```

```glsl
// Geode.App/Shaders/globe_normals.frag

#version 460 core

in vec3 v_normalEC;

out vec4 FragColor;

void main()
{
    // Map normal components from [-1, 1] to [0, 1] for visualization.
    vec3 n = normalize(v_normalEC);
    FragColor = vec4(n * 0.5 + 0.5, 1.0);
}
```

### Stage 3: Phong Lighting

Full Phong lighting with ambient, diffuse, and specular components. The lighting is computed in eye/camera space to avoid issues with world-space coordinates at planetary scale (Section 27 covers this in detail).

```glsl
// Geode.App/Shaders/globe_phong.vert
//
// Book Section 4.2, Listings 4.8-4.9
// Vertex shader for Phong lighting in eye space.

#version 460 core

layout(location = 0) in vec3 a_position;
layout(location = 1) in vec3 a_normal;
layout(location = 2) in vec2 a_texcoord;

uniform mat4 og_modelViewPerspectiveMatrix;
uniform mat4 u_modelView;

out vec3 v_positionEC;
out vec3 v_normalEC;
out vec2 v_texcoord;

void main()
{
    vec4 positionEC = u_modelView * vec4(a_position, 1.0);
    v_positionEC = positionEC.xyz;
    v_normalEC = mat3(u_modelView) * a_normal;
    v_texcoord = a_texcoord;

    gl_Position = og_modelViewPerspectiveMatrix * vec4(a_position, 1.0);
}
```

```glsl
// Geode.App/Shaders/globe_phong.frag
//
// Book Section 4.2, Listings 4.10-4.11
// Per-fragment Phong lighting with ambient + diffuse + specular.

#version 460 core

in vec3 v_positionEC;
in vec3 v_normalEC;
in vec2 v_texcoord;

out vec4 FragColor;

uniform vec3 u_lightDirectionEC;  // Light direction in eye space (normalized, toward the light)
uniform vec3 u_ambientColor;      // Ambient light color (e.g., 0.1, 0.1, 0.1)
uniform vec3 u_diffuseColor;      // Diffuse material color (e.g., 0.4, 0.6, 1.0 for blue globe)
uniform vec3 u_specularColor;     // Specular highlight color (e.g., 1.0, 1.0, 1.0)
uniform float u_shininess;        // Specular exponent (e.g., 32.0)

/// Computes Phong lighting intensity.
///
/// @param normal       Surface normal (eye space, normalized).
/// @param position     Fragment position (eye space).
/// @param lightDir     Direction toward the light source (eye space, normalized).
/// @return             Combined (ambient + diffuse + specular) color.
vec3 PhongIntensity(vec3 normal, vec3 position, vec3 lightDir)
{
    // Ambient
    vec3 ambient = u_ambientColor;

    // Diffuse: Lambert's cosine law
    float nDotL = max(dot(normal, lightDir), 0.0);
    vec3 diffuse = u_diffuseColor * nDotL;

    // Specular: Phong reflection model
    vec3 viewDir = normalize(-position); // From fragment toward camera (at origin in eye space)
    vec3 reflectDir = reflect(-lightDir, normal);
    float rDotV = max(dot(reflectDir, viewDir), 0.0);
    vec3 specular = u_specularColor * pow(rDotV, u_shininess);

    // Only add specular when the surface faces the light
    if (nDotL <= 0.0)
        specular = vec3(0.0);

    return ambient + diffuse + specular;
}

void main()
{
    vec3 normal = normalize(v_normalEC);
    vec3 intensity = PhongIntensity(normal, v_positionEC, normalize(u_lightDirectionEC));
    FragColor = vec4(intensity, 1.0);
}
```

### Stage 4: Textured Globe

Add texture mapping using the texture coordinates computed by the tessellator. The texture coordinates are derived from the geodetic surface normal using Equation 4.5:

```
s = atan2(n.y, n.x) / (2 * pi) + 0.5
t = asin(n.z) / pi + 0.5
```

This maps the entire ellipsoid surface to a single equirectangular (plate carree) texture, the same projection used by Blue Marble imagery from NASA.

```glsl
// Geode.App/Shaders/globe_textured.vert

#version 460 core

layout(location = 0) in vec3 a_position;
layout(location = 1) in vec3 a_normal;
layout(location = 2) in vec2 a_texcoord;

uniform mat4 og_modelViewPerspectiveMatrix;
uniform mat4 u_modelView;

out vec3 v_positionEC;
out vec3 v_normalEC;
out vec2 v_texcoord;

void main()
{
    vec4 posEC = u_modelView * vec4(a_position, 1.0);
    v_positionEC = posEC.xyz;
    v_normalEC = mat3(u_modelView) * a_normal;
    v_texcoord = a_texcoord;

    gl_Position = og_modelViewPerspectiveMatrix * vec4(a_position, 1.0);
}
```

```glsl
// Geode.App/Shaders/globe_textured.frag

#version 460 core

in vec3 v_positionEC;
in vec3 v_normalEC;
in vec2 v_texcoord;

out vec4 FragColor;

uniform sampler2D u_dayTexture;

void main()
{
    vec3 color = texture(u_dayTexture, v_texcoord).rgb;
    FragColor = vec4(color, 1.0);
}
```

### Stage 5: Production Globe (Lighting + Texture)

The final combined shader: Phong lighting modulates the texture color. This is the production globe shader for tessellated rendering.

```glsl
// Geode.App/Shaders/globe.vert
//
// Book Section 4.2, combined vertex shader.
// Computes eye-space position and normal for per-fragment Phong lighting,
// passes texture coordinates through.

#version 460 core

layout(location = 0) in vec3 a_position;
layout(location = 1) in vec3 a_normal;
layout(location = 2) in vec2 a_texcoord;

uniform mat4 og_modelViewPerspectiveMatrix;
uniform mat4 u_modelView;

out vec3 v_positionEC;
out vec3 v_normalEC;
out vec2 v_texcoord;

void main()
{
    vec4 posEC = u_modelView * vec4(a_position, 1.0);
    v_positionEC = posEC.xyz;
    v_normalEC = mat3(u_modelView) * a_normal;
    v_texcoord = a_texcoord;

    gl_Position = og_modelViewPerspectiveMatrix * vec4(a_position, 1.0);
}
```

```glsl
// Geode.App/Shaders/globe.frag
//
// Book Section 4.2, combined fragment shader.
// Per-fragment Phong lighting applied to an equirectangular day texture.

#version 460 core

in vec3 v_positionEC;
in vec3 v_normalEC;
in vec2 v_texcoord;

out vec4 FragColor;

uniform sampler2D u_dayTexture;
uniform vec3 u_lightDirectionEC;
uniform vec3 u_ambientColor;
uniform vec3 u_specularColor;
uniform float u_shininess;

vec3 PhongIntensity(vec3 normal, vec3 position, vec3 lightDir, vec3 diffuseColor)
{
    // Ambient
    vec3 ambient = u_ambientColor * diffuseColor;

    // Diffuse
    float nDotL = max(dot(normal, lightDir), 0.0);
    vec3 diffuse = diffuseColor * nDotL;

    // Specular
    vec3 viewDir = normalize(-position);
    vec3 reflectDir = reflect(-lightDir, normal);
    float rDotV = max(dot(reflectDir, viewDir), 0.0);
    vec3 specular = u_specularColor * pow(rDotV, u_shininess);

    if (nDotL <= 0.0)
        specular = vec3(0.0);

    return ambient + diffuse + specular;
}

void main()
{
    vec3 normal = normalize(v_normalEC);
    vec3 texColor = texture(u_dayTexture, v_texcoord).rgb;
    vec3 lit = PhongIntensity(normal, v_positionEC, normalize(u_lightDirectionEC), texColor);
    FragColor = vec4(lit, 1.0);
}
```

**C# setup for the production globe:**

```csharp
// Example: rendering a textured, lit globe
// Assumes RenderContext, SceneState, and Texture2D from Part III

// 1. Tessellate
MeshData mesh = GeographicGridEllipsoidTessellator.Compute(64, 128, Ellipsoid.Wgs84);

// 2. Upload to GPU
var vbo = new BufferObject<float>(gl, mesh.Vertices);
var ibo = new BufferObject<uint>(gl, mesh.Indices);

// 3. Shader
var shader = ShaderProgram.FromFiles(gl, "Shaders/globe.vert", "Shaders/globe.frag");

// 4. Set manual per-draw uniforms.
//    og_modelViewPerspectiveMatrix, og_viewMatrix, og_diffuseSpecularAmbientShininess,
//    og_texture0..N are handled by the automatic uniform system (Section 19).
//    Only shader-specific manual uniforms need to be set here.
((Uniform<int>)shader.Uniforms["u_dayTexture"]).Value = 0;  // texture unit 0
```

---

## Section 24: Step 5 -- Latitude-Longitude Grid

*Corresponds to Book Chapter 4, Section 4.2.4, Listings 4.12-4.13*

*OpenGlobe sources: `Shaders/Globe/GlobeFS.glsl` (grid portion)*

A latitude-longitude grid overlays the globe with lines every N degrees. It is the visual equivalent of graph paper on a sphere. This grid is rendered procedurally in the fragment shader -- no additional geometry is needed.

The technique uses the `mod()` function on the geodetic coordinates to detect whether the current fragment falls on a grid line. The `fwidth()` function (which uses `dFdx()` and `dFdy()` screen-space derivatives) computes how wide the line should be in texture space to maintain a view-independent pixel width.

```glsl
// Geode.App/Shaders/globe_grid.frag
//
// Book Section 4.2.4, Listings 4.12-4.13
// Fragment shader that overlays a lat-lon grid on the globe.
// Can be used standalone or combined with the textured globe shader.

#version 460 core

in vec3 v_positionEC;
in vec3 v_normalEC;
in vec2 v_texcoord;

out vec4 FragColor;

uniform sampler2D u_dayTexture;
uniform vec3 u_lightDirectionEC;
uniform vec3 u_ambientColor;
uniform vec3 u_specularColor;
uniform float u_shininess;

// Grid parameters
uniform float u_gridLineWidth;      // Width in texcoord space (e.g., 0.002)
uniform float u_gridResolutionS;    // Grid spacing in S (longitude), e.g., 1.0/36.0 for 10-degree lines
uniform float u_gridResolutionT;    // Grid spacing in T (latitude), e.g., 1.0/18.0 for 10-degree lines
uniform vec3 u_gridColor;           // Grid line color (e.g., 1.0, 1.0, 0.0 for yellow)

vec3 PhongIntensity(vec3 normal, vec3 position, vec3 lightDir, vec3 diffuseColor)
{
    vec3 ambient = u_ambientColor * diffuseColor;
    float nDotL = max(dot(normal, lightDir), 0.0);
    vec3 diffuse = diffuseColor * nDotL;
    vec3 viewDir = normalize(-position);
    vec3 reflectDir = reflect(-lightDir, normal);
    float rDotV = max(dot(reflectDir, viewDir), 0.0);
    vec3 specular = u_specularColor * pow(rDotV, u_shininess);
    if (nDotL <= 0.0) specular = vec3(0.0);
    return ambient + diffuse + specular;
}

/// Computes whether the current fragment falls on a grid line.
/// Uses screen-space derivatives for view-independent line width.
///
/// @param texCoord   The texture coordinate to test.
/// @return           1.0 if on a grid line, 0.0 if not, with antialiased transition.
float GridFactor(vec2 texCoord)
{
    // Distance from the nearest grid line in each axis
    vec2 gridSpacing = vec2(u_gridResolutionS, u_gridResolutionT);

    // Compute distance to nearest grid line using mod
    vec2 distToLine = abs(mod(texCoord + 0.5 * gridSpacing, gridSpacing) - 0.5 * gridSpacing);

    // Screen-space derivative of the texture coordinate.
    // fwidth = abs(dFdx) + abs(dFdy), giving us the texcoord change per pixel.
    vec2 fw = fwidth(texCoord);

    // Antialiased step: smoothstep from (lineWidth - fw) to (lineWidth + fw).
    // This produces a smooth 0-to-1 transition over exactly one pixel.
    float lineWidthS = u_gridLineWidth;
    float lineWidthT = u_gridLineWidth;

    float lineS = 1.0 - smoothstep(lineWidthS - fw.x, lineWidthS + fw.x, distToLine.x);
    float lineT = 1.0 - smoothstep(lineWidthT - fw.y, lineWidthT + fw.y, distToLine.y);

    // Combine: on a grid line if either S or T matches
    return max(lineS, lineT);
}

void main()
{
    vec3 normal = normalize(v_normalEC);
    vec3 lightDir = normalize(u_lightDirectionEC);
    vec3 texColor = texture(u_dayTexture, v_texcoord).rgb;

    // Apply lighting to the base texture
    vec3 litColor = PhongIntensity(normal, v_positionEC, lightDir, texColor);

    // Overlay grid lines
    float grid = GridFactor(v_texcoord);
    vec3 finalColor = mix(litColor, u_gridColor, grid);

    FragColor = vec4(finalColor, 1.0);
}
```

This shader reuses the `globe.vert` vertex shader from Stage 5 -- no changes needed on the vertex side.

**C# uniform setup for the grid:**

```csharp
// 10-degree grid lines (36 longitude lines, 18 latitude lines)
shader.SetFloat("u_gridLineWidth", 0.001f);
shader.SetFloat("u_gridResolutionS", 1.0f / 36.0f);  // 360 / 10 = 36 segments
shader.SetFloat("u_gridResolutionT", 1.0f / 18.0f);  // 180 / 10 = 18 segments
shader.SetVec3("u_gridColor", 1.0f, 1.0f, 0.0f);     // Yellow lines
```

**How `fwidth()` works:** The GPU computes partial derivatives of any varying by differencing the value in adjacent fragments within a 2x2 quad. `dFdx(v_texcoord.x)` tells you how much the S texture coordinate changes per pixel horizontally. `fwidth()` returns `abs(dFdx) + abs(dFdy)` -- the total change per pixel in both directions. By comparing the distance to the nearest grid line against `fwidth()`, we get a line that is always approximately one pixel wide regardless of zoom level.

---

## Section 25: Step 6 -- GPU Ray-Casted Globe

*Corresponds to Book Chapter 4, Section 4.3, Listings 4.16-4.18*

*OpenGlobe sources: `RayCastedGlobe.cs`, `Shaders/RayCastedGlobe/`*

### Concept

Instead of tessellating the ellipsoid into thousands of triangles, we render a simple bounding box (12 triangles) and ray-trace the ellipsoid analytically in the fragment shader. For every pixel covered by the bounding box, the shader casts a ray from the camera through that pixel, solves the ray-ellipsoid intersection equation (a quadratic), and computes the surface normal, texture coordinates, and lighting at the intersection point.

This approach produces a **pixel-perfect** silhouette at any zoom level with zero tessellation artifacts. The cost is per-fragment math instead of per-vertex math -- each visible pixel evaluates a quadratic equation, a square root, a normalize, and the Phong lighting equation.

### BoxTessellator

A minimal tessellator that creates an axis-aligned box. The box is sized to tightly contain the ellipsoid.

```csharp
// Geode.Core/BoxTessellator.cs
//
// Book Section 4.3, Listing 4.16
// OpenGlobe: Source/Core/Tessellation/BoxTessellator.cs
//
// Creates an axis-aligned box mesh. Used as the bounding volume
// for ray-cast rendering of the ellipsoid.

using System;

namespace Geode.Core
{
    /// <summary>
    /// Generates a simple axis-aligned box mesh.
    /// </summary>
    public static class BoxTessellator
    {
        /// <summary>
        /// Creates a box mesh centered at the origin with the given half-extents.
        /// </summary>
        /// <param name="halfExtents">
        /// Half the size of the box along each axis. For an ellipsoid bounding box,
        /// pass the ellipsoid radii (possibly with a small margin).
        /// </param>
        /// <returns>
        /// A MeshData with 8 vertices and 36 indices (12 triangles, 2 per face).
        /// Vertex layout: position only (px, py, pz, 0, 0, 0, 0, 0) -- normals
        /// and texcoords are unused because the ray-cast shader computes them analytically.
        /// </returns>
        public static MeshData Compute(Vector3D halfExtents)
        {
            double x = halfExtents.X;
            double y = halfExtents.Y;
            double z = halfExtents.Z;

            // 8 corners of the box
            Vector3D[] corners = new Vector3D[]
            {
                new Vector3D(-x, -y, -z), // 0: left  bottom back
                new Vector3D( x, -y, -z), // 1: right bottom back
                new Vector3D( x,  y, -z), // 2: right top    back
                new Vector3D(-x,  y, -z), // 3: left  top    back
                new Vector3D(-x, -y,  z), // 4: left  bottom front
                new Vector3D( x, -y,  z), // 5: right bottom front
                new Vector3D( x,  y,  z), // 6: right top    front
                new Vector3D(-x,  y,  z), // 7: left  top    front
            };

            float[] vertices = new float[8 * 8]; // 8 vertices * 8 floats
            for (int i = 0; i < 8; i++)
            {
                vertices[i * 8 + 0] = (float)corners[i].X;
                vertices[i * 8 + 1] = (float)corners[i].Y;
                vertices[i * 8 + 2] = (float)corners[i].Z;
                // Normal and texcoord slots zeroed (unused by ray-cast shader)
                vertices[i * 8 + 3] = 0f;
                vertices[i * 8 + 4] = 0f;
                vertices[i * 8 + 5] = 0f;
                vertices[i * 8 + 6] = 0f;
                vertices[i * 8 + 7] = 0f;
            }

            // 12 triangles (2 per face), wound counter-clockwise when viewed from outside
            uint[] indices = new uint[]
            {
                // Front face (+Z)
                4, 5, 6,  4, 6, 7,
                // Back face (-Z)
                1, 0, 3,  1, 3, 2,
                // Right face (+X)
                5, 1, 2,  5, 2, 6,
                // Left face (-X)
                0, 4, 7,  0, 7, 3,
                // Top face (+Y)
                7, 6, 2,  7, 2, 3,
                // Bottom face (-Y)
                0, 1, 5,  0, 5, 4,
            };

            return new MeshData(vertices, indices);
        }
    }
}
```

### Front-Face Culling

Normally we cull back-facing triangles (triangles facing away from the camera). For ray-casting, we do the opposite: we cull **front-facing** triangles and render only the **back faces** of the bounding box. Why?

When the camera is outside the box, the front faces are closer. But we want the fragment shader to run for every pixel that *might* hit the ellipsoid. The back faces guarantee this: if a ray enters the box (hits a front face), it must also exit the box (hits a back face). By rendering back faces, every pixel that could contain the ellipsoid gets a fragment shader invocation.

When the camera is inside the box (very close to the surface), front faces might be behind the camera and get clipped. Back faces remain visible, so the shader still runs correctly.

```csharp
// C# render state for ray-cast globe
var renderState = new RenderState
{
    FacetCulling = new FacetCulling(
        enabled: true,
        face: CullFace.Front,              // Cull FRONT faces
        windingOrder: WindingOrder.CounterClockwise),
    DepthTest = new DepthTest(
        enabled: true,
        function: DepthTestFunction.Less)
};
```

### Ray-Cast Vertex Shader

```glsl
// Geode.App/Shaders/globe_raycast.vert
//
// Book Section 4.3, Listing 4.17
// Vertex shader for the ray-cast globe.
// Transforms box corners and passes the world-space position
// to the fragment shader for ray computation.

#version 460 core

layout(location = 0) in vec3 a_position;

uniform mat4 og_modelViewPerspectiveMatrix;

out vec3 v_worldPosition;

void main()
{
    v_worldPosition = a_position;
    gl_Position = og_modelViewPerspectiveMatrix * vec4(a_position, 1.0);
}
```

### Ray-Cast Fragment Shader

```glsl
// Geode.App/Shaders/globe_raycast.frag
//
// Book Section 4.3, Listing 4.18
// Per-fragment ray-ellipsoid intersection.
// Computes surface normal, texture coordinates, and Phong lighting
// analytically for a pixel-perfect globe.

#version 460 core

in vec3 v_worldPosition;

out vec4 FragColor;

// Ellipsoid parameters
uniform vec3 u_radii;                // Ellipsoid radii (a, b, c) in world space
uniform vec3 u_oneOverRadiiSquared;  // (1/a^2, 1/b^2, 1/c^2)

// Camera
uniform vec3 u_cameraEye;           // Camera position in world space
uniform mat4 u_modelView;
uniform mat4 u_projection;

// Lighting (world space)
uniform vec3 u_lightDirectionWC;    // Direction toward the light (world space, normalized)
uniform vec3 u_ambientColor;
uniform vec3 u_diffuseColor;
uniform vec3 u_specularColor;
uniform float u_shininess;

// Texture
uniform sampler2D u_dayTexture;

/// Intersects a ray with an ellipsoid defined by (x/a)^2 + (y/b)^2 + (z/c)^2 = 1.
///
/// The ray is: P(t) = origin + t * direction, t >= 0.
/// Substituting into the ellipsoid equation and expanding gives a quadratic in t:
///   A*t^2 + 2*B*t + C = 0
/// where:
///   A = sum( dir_i^2 / r_i^2 )
///   B = sum( origin_i * dir_i / r_i^2 )
///   C = sum( origin_i^2 / r_i^2 ) - 1
///
/// @param origin    Ray origin in world space.
/// @param direction Ray direction in world space (need not be normalized).
/// @param oneOverRadiiSq  (1/a^2, 1/b^2, 1/c^2).
/// @param t         Output: the nearest positive t value.
/// @return          true if the ray hits the ellipsoid.
bool RayIntersectEllipsoid(vec3 origin, vec3 direction, vec3 oneOverRadiiSq, out float t)
{
    // Quadratic coefficients
    vec3 oSq = origin * origin;
    vec3 dSq = direction * direction;
    vec3 od  = origin * direction;

    float A = dot(dSq, oneOverRadiiSq);
    float B = dot(od, oneOverRadiiSq);
    float C = dot(oSq, oneOverRadiiSq) - 1.0;

    float discriminant = B * B - A * C;

    if (discriminant < 0.0)
    {
        t = 0.0;
        return false;
    }

    float sqrtDisc = sqrt(discriminant);

    // Two solutions: t0 = (-B - sqrt) / A, t1 = (-B + sqrt) / A
    // We want the nearest positive root.
    float t0 = (-B - sqrtDisc) / A;
    float t1 = (-B + sqrtDisc) / A;

    if (t0 >= 0.0)
    {
        t = t0;
        return true;
    }
    else if (t1 >= 0.0)
    {
        // Camera is inside the ellipsoid; use the exit point.
        t = t1;
        return true;
    }

    t = 0.0;
    return false;
}

/// Computes the geodetic surface normal at a point on the ellipsoid.
/// n = normalize(point * oneOverRadiiSquared)
vec3 GeodeticSurfaceNormal(vec3 point, vec3 oneOverRadiiSq)
{
    return normalize(point * oneOverRadiiSq);
}

/// Computes equirectangular texture coordinates from the geodetic surface normal.
/// s = atan(ny, nx) / (2*pi) + 0.5
/// t = asin(nz) / pi + 0.5
vec2 ComputeTextureCoordinates(vec3 normal)
{
    float s = atan(normal.y, normal.x) / (2.0 * 3.14159265358979) + 0.5;
    float t = asin(clamp(normal.z, -1.0, 1.0)) / 3.14159265358979 + 0.5;
    return vec2(s, t);
}

/// Computes Phong lighting intensity.
float LightIntensity(vec3 normal, vec3 position, vec3 lightDir, vec3 viewDir)
{
    // Diffuse
    float nDotL = max(dot(normal, lightDir), 0.0);

    // Specular
    vec3 reflectDir = reflect(-lightDir, normal);
    float rDotV = max(dot(reflectDir, viewDir), 0.0);
    float specular = pow(rDotV, u_shininess);

    if (nDotL <= 0.0)
        specular = 0.0;

    return nDotL + specular;
}

/// Writes the correct depth value for the ray-casted intersection point.
/// Without this, all fragments would have the depth of the bounding box,
/// causing incorrect depth interactions with other geometry.
float ComputeWorldPositionDepth(vec3 worldPosition, mat4 modelView, mat4 projection)
{
    vec4 clipPos = projection * modelView * vec4(worldPosition, 1.0);
    float ndcDepth = clipPos.z / clipPos.w;

    // Map from NDC [-1, 1] to window [0, 1] (OpenGL default)
    // For reversed-Z (Section 28), this mapping changes.
    return ndcDepth * 0.5 + 0.5;
}

void main()
{
    // Ray from camera through this fragment's world position
    vec3 rayOrigin = u_cameraEye;
    vec3 rayDirection = v_worldPosition - u_cameraEye;

    float t;
    if (!RayIntersectEllipsoid(rayOrigin, rayDirection, u_oneOverRadiiSquared, t))
    {
        discard; // Ray missed the ellipsoid -- this pixel is empty sky
    }

    // Intersection point in world space
    vec3 hitPoint = rayOrigin + t * rayDirection;

    // Geodetic surface normal at the intersection
    vec3 normal = GeodeticSurfaceNormal(hitPoint, u_oneOverRadiiSquared);

    // Texture coordinates
    vec2 texCoord = ComputeTextureCoordinates(normal);

    // Texture color
    vec3 texColor = texture(u_dayTexture, texCoord).rgb;

    // Lighting in world space
    vec3 viewDir = normalize(u_cameraEye - hitPoint);
    vec3 lightDir = normalize(u_lightDirectionWC);

    // Ambient
    vec3 ambient = u_ambientColor * texColor;

    // Diffuse
    float nDotL = max(dot(normal, lightDir), 0.0);
    vec3 diffuse = texColor * nDotL;

    // Specular
    vec3 reflectDir = reflect(-lightDir, normal);
    float rDotV = max(dot(reflectDir, viewDir), 0.0);
    vec3 specular = u_specularColor * pow(rDotV, u_shininess);
    if (nDotL <= 0.0) specular = vec3(0.0);

    vec3 finalColor = ambient + diffuse + specular;

    // Write correct depth for the intersection point
    gl_FragDepth = ComputeWorldPositionDepth(hitPoint, u_modelView, u_projection);

    FragColor = vec4(finalColor, 1.0);
}
```

**C# setup for the ray-cast globe:**

```csharp
// 1. Create bounding box mesh
Vector3D radii = Ellipsoid.Wgs84.Radii;
MeshData boxMesh = BoxTessellator.Compute(radii);

// 2. Upload box to GPU
var vbo = new BufferObject<float>(gl, boxMesh.Vertices);
var ibo = new BufferObject<uint>(gl, boxMesh.Indices);

// 3. Shader
var shader = ShaderProgram.FromFiles(gl, "Shaders/globe_raycast.vert", "Shaders/globe_raycast.frag");

// 4. Per-frame manual uniforms.
//    Automatic uniforms handle og_modelViewPerspectiveMatrix, og_viewMatrix,
//    og_perspectiveMatrix, og_cameraEye, og_sunPosition,
//    og_diffuseSpecularAmbientShininess (Section 19). Only the ray-cast-specific
//    manual uniforms need to be set here:
((Uniform<Vector3>)shader.Uniforms["u_radii"]).Value =
    new Vector3((float)radii.X, (float)radii.Y, (float)radii.Z);
((Uniform<Vector3>)shader.Uniforms["u_oneOverRadiiSquared"]).Value =
    new Vector3(
        (float)Ellipsoid.Wgs84.OneOverRadiiSquared.X,
        (float)Ellipsoid.Wgs84.OneOverRadiiSquared.Y,
        (float)Ellipsoid.Wgs84.OneOverRadiiSquared.Z);
((Uniform<int>)shader.Uniforms["u_dayTexture"]).Value = 0;
```

### Trade-Offs: Tessellated vs Ray-Casted

| Property | Tessellated Globe | Ray-Casted Globe |
|---|---|---|
| **Silhouette quality** | Depends on tessellation level; visible facets at low LOD | Pixel-perfect at all zoom levels |
| **Vertex count** | Thousands to hundreds of thousands | 8 (box corners) |
| **Fragment cost** | Texture lookup + lighting | Quadratic solve + normalize + lighting + depth write |
| **Depth buffer** | Automatic from rasterizer | Manual `gl_FragDepth` write (disables early-Z optimization) |
| **Terrain displacement** | Natural -- displace vertices | Difficult -- requires modified intersection |
| **Texture seams** | Must handle antimeridian seam in tessellator | Must handle atan2 discontinuity in shader |
| **Best for** | Terrain, tile-based imagery, displaced surfaces | Smooth globe with equirectangular texture, debugging |

> **3.3 vs 4.6 Callout:** The ray-cast shader writes `gl_FragDepth`, which disables the GPU's early-Z optimization (fragments are tested against the depth buffer before the shader runs). In OpenGL 4.2+ (and therefore 4.6), we can use `layout(depth_greater) out float gl_FragDepth;` to hint to the GPU that our depth value is always greater than or equal to the interpolated depth, partially restoring early-Z. This is shown in the logarithmic depth buffer shader in Section 28.

---

## Section 26: Step 7 -- Day/Night Globe Shading

*Corresponds to Book Chapter 4, Section 4.2.5, Listings 4.14-4.15*

*OpenGlobe sources: `Shaders/Globe/GlobeFS.glsl`*

### Two-Texture Approach

A day/night globe uses two textures:
1. **Day texture** -- satellite imagery (e.g., NASA Blue Marble) showing the Earth in daylight
2. **Night texture** -- city lights at night (e.g., NASA Black Marble / VIIRS)

The shader blends between them based on the angle between the surface normal and the sun direction. On the illuminated hemisphere, the day texture is shown. On the dark hemisphere, the night lights texture is shown. In the twilight zone (the terminator), the two are blended smoothly.

### Day/Night Fragment Shader

```glsl
// Geode.App/Shaders/globe_daynight.frag
//
// Book Section 4.2.5, Listings 4.14-4.15
// Day/night blending based on sun angle.
// Uses the same vertex shader as globe.vert (Stage 5).

#version 460 core

in vec3 v_positionEC;
in vec3 v_normalEC;
in vec2 v_texcoord;

out vec4 FragColor;

uniform sampler2D u_dayTexture;
uniform sampler2D u_nightTexture;
uniform vec3 u_lightDirectionEC;  // Toward the sun, eye space, normalized
uniform vec3 u_ambientColor;
uniform vec3 u_specularColor;
uniform float u_shininess;
uniform float u_blendDuration;    // Width of the twilight zone in dot-product units (e.g., 0.1)
uniform float u_blendDurationScale; // Scale for the blend transition (e.g., 2.0)

/// Returns the day color (lit satellite imagery).
vec3 DayColor(vec3 normal, vec3 position, vec3 lightDir, vec2 texCoord)
{
    vec3 texColor = texture(u_dayTexture, texCoord).rgb;

    // Diffuse
    float nDotL = max(dot(normal, lightDir), 0.0);
    vec3 diffuse = texColor * nDotL;

    // Specular (ocean glint)
    vec3 viewDir = normalize(-position);
    vec3 reflectDir = reflect(-lightDir, normal);
    float rDotV = max(dot(reflectDir, viewDir), 0.0);
    vec3 specular = u_specularColor * pow(rDotV, u_shininess);
    if (nDotL <= 0.0) specular = vec3(0.0);

    // Ambient
    vec3 ambient = u_ambientColor * texColor;

    return ambient + diffuse + specular;
}

/// Returns the night color (city lights, no lighting applied).
vec3 NightColor(vec2 texCoord)
{
    return texture(u_nightTexture, texCoord).rgb;
}

void main()
{
    vec3 normal = normalize(v_normalEC);
    vec3 lightDir = normalize(u_lightDirectionEC);

    // dot(normal, lightDir):
    //   > 0 : surface faces the sun (day)
    //   = 0 : terminator (exactly 90 degrees from sun)
    //   < 0 : surface faces away from sun (night)
    float nDotL = dot(normal, lightDir);

    vec3 dayColor = DayColor(normal, v_positionEC, lightDir, v_texcoord);
    vec3 nightColor = NightColor(v_texcoord);

    // Three-way branch with smooth twilight transition:
    // - nDotL > blendDuration: fully day
    // - nDotL < -blendDuration: fully night
    // - in between: linear blend
    float blend = smoothstep(-u_blendDuration, u_blendDuration, nDotL);

    vec3 finalColor = mix(nightColor, dayColor, blend);

    FragColor = vec4(finalColor, 1.0);
}
```

**C# setup with dual textures:**

```csharp
// Load textures
// Texture2D dayTexture = Texture2D.FromFile(gl, "Textures/earth_day_8k.jpg");
// Texture2D nightTexture = Texture2D.FromFile(gl, "Textures/earth_night_8k.jpg");

// Bind to texture units
// gl.BindTextureUnit(0, dayTexture.Handle);
// gl.BindTextureUnit(1, nightTexture.Handle);

// Set uniforms
shader.Use();
shader.SetInt("u_dayTexture", 0);
shader.SetInt("u_nightTexture", 1);
shader.SetFloat("u_blendDuration", 0.1f);      // Twilight width
shader.SetFloat("u_blendDurationScale", 2.0f);  // Sharpness
shader.SetVec3("u_ambientColor", 0.05f, 0.05f, 0.05f);
shader.SetVec3("u_specularColor", 0.2f, 0.2f, 0.2f);
shader.SetFloat("u_shininess", 64.0f);

// Sun direction: compute from time-of-day simulation or fixed position
// For a sun at "noon" over the Prime Meridian:
shader.SetVec3("u_lightDirectionEC", lightDirECx, lightDirECy, lightDirECz);
```

### Ray-Cast Day/Night Variant

The same blending logic works in the ray-cast shader. Replace the final color computation in `globe_raycast.frag`:

```glsl
// Replace the final color block in globe_raycast.frag with:

    // Day/Night blending (ray-cast variant)
    vec3 dayColor = ambient + diffuse + specular;
    vec3 nightColor = texture(u_nightTexture, texCoord).rgb;

    float nDotLBlend = dot(normal, lightDir);
    float blend = smoothstep(-u_blendDuration, u_blendDuration, nDotLBlend);
    vec3 finalColor = mix(nightColor, dayColor, blend);

    gl_FragDepth = ComputeWorldPositionDepth(hitPoint, u_modelView, u_projection);
    FragColor = vec4(finalColor, 1.0);
```

### Data Sources

| Dataset | Resolution | Source | License | Size |
|---|---|---|---|---|
| Blue Marble Next Gen | 500m/pixel (86400x43200) | NASA Visible Earth | Public domain | ~800 MB (JPEG) |
| Black Marble (VIIRS) | 500m/pixel | NASA Earth Observatory | Public domain | ~600 MB (JPEG) |
| Natural Earth I | ~10 km/pixel (10800x5400) | naturalearthdata.com | Public domain | ~12 MB (JPEG) |
| Natural Earth II (shaded relief) | ~10 km/pixel | naturalearthdata.com | Public domain | ~15 MB (JPEG) |

For development and testing, the Natural Earth datasets are recommended -- they are small enough to include in version control and load almost instantly.

---

# Part V -- Vertex Transform Precision

*Corresponds to Book Chapter 5: "Vertex Transform Precision"*

When you zoom in on the tessellated or ray-cast globe and the camera is close to the surface, vertices begin to jitter -- they vibrate, shimmer, and shift by visible amounts even when the camera is stationary. This is not a bug in your code. It is a fundamental limitation of 32-bit floating-point arithmetic at planetary scale.

This part explains the problem, presents five progressively better solutions, and recommends the best approach for Geode.

---

## Section 27: Step 8 -- Fixing Vertex Jitter

*Corresponds to Book Chapter 5, Sections 5.1-5.5, Listings 5.1-5.9, Table 5.1*

*OpenGlobe sources: `Source/Examples/Chapter05/`*

### The Problem

Consider a vertex at ECEF position `(6378137.0, 0.0, 0.0)` -- a point on the equator at the Prime Meridian, sitting exactly on the WGS84 surface. As a 64-bit double, this is exact. But the GPU uses 32-bit floats.

**Unit in the Last Place (ULP) analysis:**

A 32-bit float at magnitude 6,378,137 has:
```
exponent = floor(log2(6378137)) = 22
ULP = 2^(exponent - 23) = 2^(22 - 23) = 2^(-1) = 0.5 meters
```

The smallest representable step is **0.5 meters**. Two positions 30 cm apart are stored as the *same* float value.

Now consider the Model-View-Projection (MVP) matrix multiplication. The MVP matrix contains a view-translation term that subtracts the camera position from every vertex. If the camera is at `(6378137.0 + 100.0, 0.0, 0.0)` (100 meters above the surface), the vertex position after subtraction should be `(-100.0, 0.0, 0.0)`. But in float:

```
6378137.0f - 6378237.0f = -128.0f  (not -100.0!)
```

This is **catastrophic cancellation** -- subtracting two large, nearly equal numbers destroys most of the significant digits. The error is 28 meters. As the camera moves, the effective error changes unpredictably, causing vertices to jump between discrete float values. This is the jitter.

### Approach 1: Render the World (RTW) -- Baseline

The simplest approach: convert every double-precision position to float and pass it through a standard float MVP matrix. This is what most tutorials teach.

**C# (CPU side):**

```csharp
// Geode.Core/Transforms/RenderTheWorldTransform.cs
//
// Book Section 5.2, Listing 5.1
// Baseline: double positions truncated to float, standard MVP.

using System;

namespace Geode.Core
{
    public static class RenderTheWorldTransform
    {
        /// <summary>
        /// Converts double-precision positions to float and computes
        /// a standard model-view-projection matrix.
        /// </summary>
        /// <param name="positionsDouble">World-space positions (double precision).</param>
        /// <param name="modelViewProjection">Standard float MVP matrix (column-major, 4x4).</param>
        /// <returns>Transformed clip-space positions as floats.</returns>
        public static float[] Transform(Vector3D[] positionsDouble, float[] modelViewProjection)
        {
            float[] result = new float[positionsDouble.Length * 4];

            for (int i = 0; i < positionsDouble.Length; i++)
            {
                float x = (float)positionsDouble[i].X;
                float y = (float)positionsDouble[i].Y;
                float z = (float)positionsDouble[i].Z;

                // Standard MVP multiply (column-major)
                int b = i * 4;
                result[b + 0] = modelViewProjection[0] * x + modelViewProjection[4] * y +
                                modelViewProjection[8] * z + modelViewProjection[12];
                result[b + 1] = modelViewProjection[1] * x + modelViewProjection[5] * y +
                                modelViewProjection[9] * z + modelViewProjection[13];
                result[b + 2] = modelViewProjection[2] * x + modelViewProjection[6] * y +
                                modelViewProjection[10] * z + modelViewProjection[14];
                result[b + 3] = modelViewProjection[3] * x + modelViewProjection[7] * y +
                                modelViewProjection[11] * z + modelViewProjection[15];
            }

            return result;
        }
    }
}
```

**GLSL:**

```glsl
// Shaders/rtw.vert
// Book Section 5.2, Listing 5.2
// Standard vertex transform. Jitters at ~100m altitude.

#version 460 core

layout(location = 0) in vec3 a_position;

uniform mat4 og_modelViewPerspectiveMatrix;

void main()
{
    gl_Position = og_modelViewPerspectiveMatrix * vec4(a_position, 1.0);
}
```

**Problem:** Jitter becomes visible when the camera is within ~100 meters of the surface. Completely unusable for street-level or aircraft-level views.

### Approach 2: Render Relative to Center (RTC)

Subtract a fixed center point from all positions on the CPU, in double precision, before converting to float. The MVP matrix is also adjusted to account for the shifted origin.

**C# (CPU side):**

```csharp
// Geode.Core/Transforms/RenderRelativeToCenterTransform.cs
//
// Book Section 5.3, Listing 5.3
// Subtract a fixed center from all positions in double, then convert to float.

using System;

namespace Geode.Core
{
    public static class RenderRelativeToCenterTransform
    {
        /// <summary>
        /// Transforms positions relative to a fixed center point.
        /// Good for localized geometry (a single building, a terrain patch)
        /// where all vertices are close to the center.
        /// </summary>
        /// <param name="positionsDouble">World-space positions (double precision).</param>
        /// <param name="center">A point near the geometry (double precision).
        /// Typically the centroid of the bounding box.</param>
        /// <returns>Float-precision positions relative to the center.</returns>
        public static float[] ComputeRelativePositions(Vector3D[] positionsDouble, Vector3D center)
        {
            float[] result = new float[positionsDouble.Length * 3];

            for (int i = 0; i < positionsDouble.Length; i++)
            {
                // Double-precision subtraction preserves accuracy
                double rx = positionsDouble[i].X - center.X;
                double ry = positionsDouble[i].Y - center.Y;
                double rz = positionsDouble[i].Z - center.Z;

                // Now the values are small (relative to center),
                // so truncation to float loses minimal precision.
                result[i * 3 + 0] = (float)rx;
                result[i * 3 + 1] = (float)ry;
                result[i * 3 + 2] = (float)rz;
            }

            return result;
        }

        /// <summary>
        /// Builds an MVP matrix that accounts for the shifted origin.
        /// The view matrix is modified so that the camera eye is also
        /// relative to the center.
        /// </summary>
        public static float[] BuildMvpRelativeToCenter(
            Vector3D cameraEye, Vector3D cameraTarget, Vector3D cameraUp,
            Vector3D center, float[] projectionMatrix)
        {
            // Shift camera into the RTC frame
            Vector3D eyeRtc = cameraEye - center;
            Vector3D targetRtc = cameraTarget - center;

            // Build view matrix in double, then convert to float
            double[] viewD = LookAtDouble(eyeRtc, targetRtc, cameraUp);
            float[] view = new float[16];
            for (int i = 0; i < 16; i++)
                view[i] = (float)viewD[i];

            // Multiply projection * view
            return MultiplyMatrices4x4(projectionMatrix, view);
        }

        /// <summary>
        /// Computes a look-at view matrix in double precision.
        /// Column-major layout matching OpenGL conventions.
        /// </summary>
        private static double[] LookAtDouble(Vector3D eye, Vector3D target, Vector3D up)
        {
            Vector3D f = (target - eye).Normalize();
            Vector3D s = f.Cross(up).Normalize();
            Vector3D u = s.Cross(f);

            double[] m = new double[16];
            m[0] = s.X;   m[4] = s.Y;   m[8]  = s.Z;   m[12] = -s.Dot(eye);
            m[1] = u.X;   m[5] = u.Y;   m[9]  = u.Z;   m[13] = -u.Dot(eye);
            m[2] = -f.X;  m[6] = -f.Y;  m[10] = -f.Z;  m[14] = f.Dot(eye);
            m[3] = 0;     m[7] = 0;     m[11] = 0;     m[15] = 1;

            return m;
        }

        /// <summary>
        /// Multiplies two 4x4 column-major matrices: result = A * B.
        /// </summary>
        private static float[] MultiplyMatrices4x4(float[] a, float[] b)
        {
            float[] r = new float[16];

            for (int col = 0; col < 4; col++)
            {
                for (int row = 0; row < 4; row++)
                {
                    r[col * 4 + row] =
                        a[0 * 4 + row] * b[col * 4 + 0] +
                        a[1 * 4 + row] * b[col * 4 + 1] +
                        a[2 * 4 + row] * b[col * 4 + 2] +
                        a[3 * 4 + row] * b[col * 4 + 3];
                }
            }

            return r;
        }
    }
}
```

**GLSL:** Same as RTW (`rtw.vert`) -- the shader does not change because the CPU has already subtracted the center.

**Limitation:** RTC works well when all geometry is near the center point. For the globe itself (vertices spanning -6M to +6M meters), there is no single center that helps. RTC is best for localized geometry like buildings, terrain patches, or annotations placed on the surface.

### Approach 3: CPU Relative-to-Eye (RTE)

Subtract the camera position (eye) from every vertex on the CPU, in double precision, every frame. The view matrix is then modified to have a zero translation (the "eye" is at the origin).

**C# (CPU side):**

```csharp
// Geode.Core/Transforms/CpuRelativeToEyeTransform.cs
//
// Book Section 5.4, Listing 5.5
// Subtract camera eye from every vertex on CPU per frame.

using System;

namespace Geode.Core
{
    public static class CpuRelativeToEyeTransform
    {
        /// <summary>
        /// Transforms all positions to be relative to the camera eye.
        /// Must be called every frame (or whenever the camera moves).
        /// </summary>
        /// <param name="positionsDouble">World-space positions (double precision).</param>
        /// <param name="cameraEye">Camera position in world space (double precision).</param>
        /// <returns>Float positions relative to the camera eye.</returns>
        public static float[] ComputeRelativeToEye(Vector3D[] positionsDouble, Vector3D cameraEye)
        {
            float[] result = new float[positionsDouble.Length * 3];

            for (int i = 0; i < positionsDouble.Length; i++)
            {
                double rx = positionsDouble[i].X - cameraEye.X;
                double ry = positionsDouble[i].Y - cameraEye.Y;
                double rz = positionsDouble[i].Z - cameraEye.Z;

                result[i * 3 + 0] = (float)rx;
                result[i * 3 + 1] = (float)ry;
                result[i * 3 + 2] = (float)rz;
            }

            return result;
        }

        /// <summary>
        /// Builds a view matrix where the eye is at the origin.
        /// Only the rotation part of the view matrix matters.
        /// </summary>
        public static float[] BuildViewMatrixAtOrigin(
            Vector3D cameraEye, Vector3D cameraTarget, Vector3D cameraUp)
        {
            Vector3D eyeAtOrigin = Vector3D.Zero;
            Vector3D targetRelative = cameraTarget - cameraEye;

            Vector3D f = targetRelative.Normalize();
            Vector3D s = f.Cross(cameraUp).Normalize();
            Vector3D u = s.Cross(f);

            // View matrix with zero translation (eye is at origin)
            float[] m = new float[16];
            m[0] = (float)s.X;   m[4] = (float)s.Y;   m[8]  = (float)s.Z;   m[12] = 0f;
            m[1] = (float)u.X;   m[5] = (float)u.Y;   m[9]  = (float)u.Z;   m[13] = 0f;
            m[2] = (float)(-f.X); m[6] = (float)(-f.Y); m[10] = (float)(-f.Z); m[14] = 0f;
            m[3] = 0f;           m[7] = 0f;           m[11] = 0f;           m[15] = 1f;

            return m;
        }
    }
}
```

**GLSL:** Same as RTW. The CPU has already done the subtraction.

**Limitation:** Excellent precision, but the CPU must recompute and re-upload the entire vertex buffer every frame. For a globe with 65,000+ vertices, this means 65,000 double subtractions and a buffer upload every frame. For static geometry this is wasteful; for dynamic geometry (moving objects, particle systems) it is fine.

### Approach 4: GPU Relative-to-Eye (GPU RTE)

Encode each double-precision position as two floats (high and low) and perform the eye subtraction on the GPU. This avoids per-frame CPU recomputation of the vertex buffer.

**C# helper -- DoubleToTwoFloats:**

```csharp
// Geode.Core/Transforms/GpuRelativeToEyeTransform.cs
//
// Book Section 5.5, Listings 5.6-5.7
// Encode doubles as high+low float pairs; subtract eye on GPU.

using System;

namespace Geode.Core
{
    public static class GpuRelativeToEyeTransform
    {
        /// <summary>
        /// Splits a double-precision value into two floats: high and low.
        /// high = (float)value (the most significant bits)
        /// low  = (float)(value - (double)high) (the residual)
        ///
        /// When added: (double)high + (double)low approximately equals value.
        /// This representation preserves ~15 digits of precision across two
        /// 7-digit float values.
        /// </summary>
        public static void DoubleToTwoFloats(double value, out float high, out float low)
        {
            high = (float)value;
            low = (float)(value - (double)high);
        }

        /// <summary>
        /// Encodes an array of double-precision positions into interleaved
        /// high/low float pairs for GPU upload.
        /// Layout: [xH, yH, zH, xL, yL, zL] per vertex (6 floats).
        /// </summary>
        public static float[] EncodePositions(Vector3D[] positionsDouble)
        {
            float[] result = new float[positionsDouble.Length * 6];

            for (int i = 0; i < positionsDouble.Length; i++)
            {
                DoubleToTwoFloats(positionsDouble[i].X, out float xH, out float xL);
                DoubleToTwoFloats(positionsDouble[i].Y, out float yH, out float yL);
                DoubleToTwoFloats(positionsDouble[i].Z, out float zH, out float zL);

                int b = i * 6;
                result[b + 0] = xH;
                result[b + 1] = yH;
                result[b + 2] = zH;
                result[b + 3] = xL;
                result[b + 4] = yL;
                result[b + 5] = zL;
            }

            return result;
        }

        /// <summary>
        /// Encodes the camera eye position as high/low float pairs
        /// for upload as a per-frame uniform.
        /// </summary>
        public static void EncodeCameraEye(Vector3D eye,
            out float[] eyeHigh, out float[] eyeLow)
        {
            eyeHigh = new float[3];
            eyeLow = new float[3];

            DoubleToTwoFloats(eye.X, out eyeHigh[0], out eyeLow[0]);
            DoubleToTwoFloats(eye.Y, out eyeHigh[1], out eyeLow[1]);
            DoubleToTwoFloats(eye.Z, out eyeHigh[2], out eyeLow[2]);
        }
    }
}
```

**GLSL -- GPU RTE vertex shader:**

```glsl
// Shaders/gpu_rte.vert
//
// Book Section 5.5, Listing 5.8
// GPU Relative-to-Eye: subtract camera eye from position using
// high/low float pairs.

#version 460 core

layout(location = 0) in vec3 a_positionHigh;
layout(location = 1) in vec3 a_positionLow;

uniform vec3 u_cameraEyeHigh;
uniform vec3 u_cameraEyeLow;
uniform mat4 u_viewRotation;     // View matrix with zero translation
uniform mat4 u_projection;

void main()
{
    // Subtract eye from position using high/low pairs:
    // (posH - eyeH) + (posL - eyeL)
    // The high parts cancel most of the magnitude, leaving a small residual.
    vec3 highDiff = a_positionHigh - u_cameraEyeHigh;
    vec3 lowDiff  = a_positionLow  - u_cameraEyeLow;
    vec3 positionRTE = highDiff + lowDiff;

    gl_Position = u_projection * u_viewRotation * vec4(positionRTE, 1.0);
}
```

**Limitation:** This approach loses precision when `positionHigh` and `eyeHigh` differ (the subtraction `highDiff` can lose bits). Jitter becomes visible at approximately 2 meters altitude. Good enough for many applications, but not for centimeter-level views.

### Approach 5: GPU RTE with DSFUN90 Arithmetic

The gold standard. Uses proper double-single arithmetic (the DSFUN90 algorithm by David Bailey, NASA Ames) to perform the subtraction with full double precision on the GPU, using only float hardware.

**C# helpers (same encoding as Approach 4):**

```csharp
// The C# side is identical to GpuRelativeToEyeTransform above.
// DoubleToTwoFloats, EncodePositions, and EncodeCameraEye are reused.
// The only change is on the GPU side.
```

**GLSL -- DSFUN90 vertex shader:**

```glsl
// Shaders/gpu_rte_dsfun90.vert
//
// Book Section 5.5, Listing 5.9
// GPU Relative-to-Eye with DSFUN90 double-single subtraction.
// This is the highest-precision approach using only float hardware.
//
// DSFUN90 reference: David H. Bailey, "A Fortran-90 Double-Single
// Package", NASA Ames Research Center, 2001.

#version 460 core

layout(location = 0) in vec3 a_positionHigh;
layout(location = 1) in vec3 a_positionLow;

uniform vec3 u_cameraEyeHigh;
uniform vec3 u_cameraEyeLow;
uniform mat4 u_viewRotation;     // View matrix with zero translation
uniform mat4 u_projection;

/// Double-single subtraction: computes (aH, aL) - (bH, bL) = result
/// using the DSFUN90 algorithm.
///
/// The key insight: when subtracting two nearly-equal double-single numbers,
/// the naive (aH - bH) + (aL - bL) loses precision because (aH - bH) may
/// have large rounding error. DSFUN90 recovers the lost bits by tracking
/// the rounding error of each float operation.
///
/// @param aH  High part of operand A.
/// @param aL  Low part of operand A.
/// @param bH  High part of operand B.
/// @param bL  Low part of operand B.
/// @return    The result as a single float (the high-order bits of A - B).
///            This is sufficient because the result is small (near zero)
///            when A and B are close (camera near a vertex).
vec3 DsfunSubtract(vec3 aH, vec3 aL, vec3 bH, vec3 bL)
{
    // Step 1: Compute the high-order difference
    vec3 t1 = aH - bH;

    // Step 2: Recover the rounding error of the subtraction.
    // e = ((aH - t1) - bH) captures what was lost.
    vec3 e = (aH - t1) - bH;

    // Step 3: Combine with the low-order parts
    // The full residual is: e + (aL - bL)
    vec3 t2 = e + aL - bL;

    // Step 4: The result is t1 + t2 (which is exact for our purposes
    // because t1 is small when aH and bH are close).
    return t1 + t2;
}

void main()
{
    vec3 positionRTE = DsfunSubtract(
        a_positionHigh, a_positionLow,
        u_cameraEyeHigh, u_cameraEyeLow);

    gl_Position = u_projection * u_viewRotation * vec4(positionRTE, 1.0);
}
```

**Why DSFUN90 works:** The algorithm tracks the rounding error of each float subtraction. When `aH` and `bH` are nearly equal (camera near a vertex), the naive difference `aH - bH` loses most significant bits. DSFUN90 recovers those bits from the algebraic identity `e = (aH - (aH - bH)) - bH`, which captures exactly the rounding error. Adding the low-order parts `aL - bL` completes the double-precision subtraction. The result is a small float (because the camera is near the vertex), so it has full float precision relative to the camera.

**Per-frame uniform update:**

```csharp
// Every frame: encode camera eye and upload
GpuRelativeToEyeTransform.EncodeCameraEye(camera.Eye,
    out float[] eyeHigh, out float[] eyeLow);

shader.Use();
shader.SetVec3("u_cameraEyeHigh", eyeHigh[0], eyeHigh[1], eyeHigh[2]);
shader.SetVec3("u_cameraEyeLow", eyeLow[0], eyeLow[1], eyeLow[2]);
shader.SetMat4("u_viewRotation", viewRotationMatrix);
shader.SetMat4("u_projection", projectionMatrix);
```

### Precision Comparison

| Approach | CPU Cost / Frame | GPU Cost / Vertex | Buffer Size | Jitter Onset (altitude) | Vertex Buffer Static? |
|---|---|---|---|---|---|
| **RTW** | None | 1 mat4 multiply | 3 floats/vert | ~100 m | Yes |
| **RTC** | N double subtracts (once) | 1 mat4 multiply | 3 floats/vert | ~100 m (if center = eye) | Yes (per center) |
| **CPU RTE** | N double subtracts (per frame) | 1 mat4 multiply | 3 floats/vert | ~0.01 m | No (re-upload each frame) |
| **GPU RTE** | Encode once | 2 vec3 subtracts + 2 mat4 multiplies | 6 floats/vert | ~2 m | Yes |
| **GPU RTE DSFUN90** | Encode once | 6 float ops + 2 mat4 multiplies | 6 floats/vert | No observed jitter | Yes |

### Summary and Recommendation

For the Geode engine:

- **Globe surface (static mesh):** Use **GPU RTE DSFUN90**. The vertex buffer is uploaded once and never changes. The per-frame cost is two vec3 uniform uploads and a few extra ALU operations per vertex. Precision is effectively unlimited.

- **Local objects (buildings, annotations):** Use **RTC** with the object's centroid as the center. These objects are small relative to the Earth, so RTC provides excellent precision with zero per-frame cost.

- **Dynamic geometry (particles, trails):** Use **CPU RTE**. The positions change every frame anyway, so the CPU subtraction cost is absorbed into the existing update loop.

---

# Part VI -- Depth Buffer Precision

*Corresponds to Book Chapter 6: "Depth Buffer Precision"*

With vertex jitter solved, the next precision problem surfaces: depth buffer fighting. When two surfaces are close together (terrain and a road overlay, two overlapping buildings, or even the globe surface and an annotation billboard), the depth buffer cannot tell them apart. They flicker between visible and hidden -- z-fighting.

This is a different problem from vertex jitter. Vertex jitter is about *position* precision. Depth buffer fighting is about *depth* precision -- the number of unique depth values available between the near and far planes.

---

## Section 28: Step 9 -- Fixing Depth Buffer Precision

*Corresponds to Book Chapter 6, Sections 6.1-6.5, Listings 6.1-6.3*

*OpenGlobe sources: `Source/Examples/Chapter06/`*

### The Problem

The standard perspective projection maps eye-space z to NDC z using:

```
z_ndc = (f + n) / (f - n) + (2 * f * n) / ((f - n) * z_eye)
```

where `n` is the near plane, `f` is the far plane, and `z_eye` is the (negative) eye-space z coordinate. After the perspective divide and viewport mapping, the depth buffer stores:

```
z_buffer = z_ndc * 0.5 + 0.5    (mapped to [0, 1])
```

The derivative `dz_buffer/dz_eye` is proportional to `1/z_eye^2`. Near the camera, depth values change rapidly per meter of distance. Far from the camera, depth values change extremely slowly. The effective depth resolution at distance `d` from the camera is:

```
S_min(d) ~ (f - n) * d^2 / (f * n * 2^b)
```

where `b` is the number of depth buffer bits (typically 24 for integer, 32 for float). At Earth scale:

- Near = 1 m, Far = 100,000,000 m (100,000 km -- beyond the Moon's orbit is not needed, but the horizon from the surface is ~3,600 km)
- At 100 km distance: `S_min ~ 10^8 * 10^10 / (10^8 * 1 * 16M) ~ 60 meters`
- Sixty meters of depth resolution. Any two surfaces closer than 60 meters apart will z-fight.

### Solution 1: Dynamic Near/Far Planes

The simplest fix: adjust the near and far planes every frame based on the camera's altitude above the ellipsoid surface.

**Horizon distance formula:** For a camera at altitude `h` above a sphere of radius `R`:

```
d_horizon = sqrt(2 * R * h + h^2)
```

For WGS84, `R ~ 6,378,137 m`. At 10 km altitude: `d_horizon ~ 357 km`. We set `far = d_horizon * 1.1` (10% margin) and `near = max(altitude * 0.01, 0.1)`.

```csharp
// Geode.Core/DynamicNearFarPlanes.cs
//
// Book Section 6.2
// Adjusts near and far clip planes based on camera altitude.

using System;

namespace Geode.Core
{
    public static class DynamicNearFarPlanes
    {
        /// <summary>
        /// Computes optimal near and far clip planes for the given camera altitude.
        /// </summary>
        /// <param name="cameraAltitude">Height above the ellipsoid surface in meters.</param>
        /// <param name="ellipsoidRadius">Mean radius of the ellipsoid in meters.
        /// For WGS84, use (Wgs84SemiMajorAxis + Wgs84SemiMinorAxis) / 2.</param>
        /// <param name="near">Output near plane distance (meters).</param>
        /// <param name="far">Output far plane distance (meters).</param>
        public static void Compute(double cameraAltitude, double ellipsoidRadius,
            out double near, out double far)
        {
            if (cameraAltitude < 0)
                cameraAltitude = 0;

            // Horizon distance: sqrt(2Rh + h^2)
            double h = cameraAltitude;
            double horizonDistance = Math.Sqrt(2.0 * ellipsoidRadius * h + h * h);

            // Far plane: just beyond the horizon
            far = horizonDistance * 1.1;

            // Near plane: fraction of altitude, clamped to a minimum
            // At low altitudes, use a very small near plane.
            // At high altitudes, push the near plane out to preserve depth precision.
            near = Math.Max(h * 0.01, 0.1);

            // Clamp the far/near ratio to something reasonable
            // even at extreme altitudes
            double maxRatio = 100_000.0;
            if (far / near > maxRatio)
                near = far / maxRatio;
        }
    }
}
```

**Limitation:** At high altitudes the far/near ratio is still large. At surface level the near plane is very small, which wastes depth bits on the first few centimeters in front of the camera. Dynamic planes help but do not solve the fundamental nonlinearity.

### Solution 2: Reversed-Z Depth Buffering

The most effective single technique for depth buffer precision. It exploits the fact that IEEE 754 floating-point numbers have **more precision near zero** (because the exponent is smallest, so the mantissa bits represent the finest granularity).

Standard depth: near plane maps to 0, far plane maps to 1. Most depth values cluster near 1 (far away), where float precision is worst.

Reversed-Z: near plane maps to **1**, far plane maps to **0**. Now distant objects get depth values near 0, where float has maximum precision. The nonlinear depth distribution of the perspective projection combines with the nonlinear float distribution to produce a *nearly uniform* depth resolution across the entire range.

> **3.3 vs 4.6 Callout:** Reversed-Z requires `glClipControl(GL_LOWER_LEFT, GL_ZERO_TO_ONE)`, which changes the NDC z range from [-1, 1] to [0, 1]. This function is core in OpenGL 4.5+. In OpenGL 3.3, reversed-Z requires the `GL_ARB_clip_control` extension (available on most recent drivers but not guaranteed). The 4.6 Core Profile guarantees it.

**C# setup:**

```csharp
// Geode.Rendering/ReversedZSetup.cs
//
// Book Section 6.3, Listing 6.1
// Configures the OpenGL pipeline for reversed-Z depth buffering.

using Silk.NET.OpenGL;

namespace Geode.Rendering
{
    public static class ReversedZSetup
    {
        /// <summary>
        /// Configures the pipeline for reversed-Z depth buffering.
        /// Call once during initialization, after creating the GL context.
        /// </summary>
        public static void Enable(GL gl)
        {
            // 1. Change clip control: NDC z range from [-1,1] to [0,1]
            //    and set origin to lower-left (standard OpenGL).
            gl.ClipControl(ClipControlOrigin.LowerLeft, ClipControlDepth.ZeroToOne);

            // 2. Clear depth to 0.0 (the new "far" value)
            gl.ClearDepth(0.0f);

            // 3. Depth test: GREATER (closer objects have larger depth values)
            gl.Enable(EnableCap.DepthTest);
            gl.DepthFunc(DepthFunction.Greater);

            // 4. Use a 32-bit float depth buffer for maximum precision.
            //    This is set when creating the framebuffer/window.
            //    Silk.NET: request Depth32f in the WindowOptions.
        }
    }
}
```

**Reversed-Z projection matrix (finite far plane):**

```csharp
// Geode.Core/ProjectionMatrices.cs
//
// Book Section 6.3, Listing 6.2
// Projection matrix helpers for reversed-Z depth.

using System;

namespace Geode.Core
{
    public static class ProjectionMatrices
    {
        /// <summary>
        /// Creates a reversed-Z perspective projection matrix with finite far plane.
        /// Near maps to depth 1.0, far maps to depth 0.0.
        /// Column-major layout, 4x4.
        /// </summary>
        public static float[] ReversedZPerspective(
            double fovYRadians, double aspectRatio, double near, double far)
        {
            double tanHalfFov = Math.Tan(fovYRadians * 0.5);

            float[] m = new float[16];

            // Standard perspective, but swap near/far in the depth mapping.
            // For [0,1] clip control with reversed Z:
            // m[10] = near / (far - near)          (was: -(f+n)/(f-n))
            // m[14] = (far * near) / (far - near)  (was: -(2fn)/(f-n))
            m[0]  = (float)(1.0 / (aspectRatio * tanHalfFov));
            m[5]  = (float)(1.0 / tanHalfFov);
            m[10] = (float)(near / (far - near));
            m[11] = -1.0f;
            m[14] = (float)((far * near) / (far - near));

            // All other elements are 0 (already initialized).
            return m;
        }

        /// <summary>
        /// Creates a reversed-Z perspective projection matrix with infinite far plane.
        /// Near maps to depth 1.0, infinity maps to depth 0.0.
        /// This is the recommended projection for virtual globes.
        ///
        /// As far -> infinity:
        ///   m[10] = 0
        ///   m[14] = near
        /// </summary>
        public static float[] ReversedZPerspectiveInfiniteFar(
            double fovYRadians, double aspectRatio, double near)
        {
            double tanHalfFov = Math.Tan(fovYRadians * 0.5);

            float[] m = new float[16];

            m[0]  = (float)(1.0 / (aspectRatio * tanHalfFov));
            m[5]  = (float)(1.0 / tanHalfFov);
            m[10] = 0.0f;        // lim(near/(far-near)) as far->inf = 0
            m[11] = -1.0f;
            m[14] = (float)near;  // lim(far*near/(far-near)) as far->inf = near

            return m;
        }

        /// <summary>
        /// Creates a standard (non-reversed) perspective projection matrix.
        /// NDC z in [-1, 1] (OpenGL default without glClipControl).
        /// For reference/comparison only.
        /// </summary>
        public static float[] StandardPerspective(
            double fovYRadians, double aspectRatio, double near, double far)
        {
            double tanHalfFov = Math.Tan(fovYRadians * 0.5);

            float[] m = new float[16];

            m[0]  = (float)(1.0 / (aspectRatio * tanHalfFov));
            m[5]  = (float)(1.0 / tanHalfFov);
            m[10] = (float)(-(far + near) / (far - near));
            m[11] = -1.0f;
            m[14] = (float)(-(2.0 * far * near) / (far - near));

            return m;
        }
    }
}
```

**Result:** With reversed-Z and a 32-bit float depth buffer, two surfaces 1 centimeter apart can be correctly distinguished at distances exceeding 10,000 km. This eliminates z-fighting for virtually all virtual globe scenarios.

### Solution 3: Logarithmic Depth Buffer

Maps depth using a logarithmic function, producing nearly uniform precision in log-distance. This is an alternative to reversed-Z that works even with integer depth buffers.

The mapping is:

```
z_log = log(C * z_eye + 1) / log(C * far + 1)
```

where `C` is a tuning constant (typically 1.0). This maps `z_eye = near` to approximately 0 and `z_eye = far` to 1, with logarithmic distribution.

> **3.3 vs 4.6 Callout:** Writing to `gl_FragDepth` disables the GPU's early-Z optimization, which can significantly impact performance (every fragment must run the shader even if it will be depth-rejected). In OpenGL 4.2+ (core in 4.6), you can declare `layout(depth_greater) out float gl_FragDepth;` to partially restore early-Z. This tells the GPU that your depth value will always be >= the interpolated depth, allowing it to cull some fragments before the shader runs. OpenGL 3.3 does not support this layout qualifier.

**Vertex shader:**

```glsl
// Shaders/logdepth.vert
//
// Book Section 6.4, Listing 6.3 (vertex part)
// Logarithmic depth buffer: compute log-depth in vertex shader,
// pass the linear z for interpolation to the fragment shader.

#version 460 core

layout(location = 0) in vec3 a_position;

uniform mat4 og_modelViewPerspectiveMatrix;
uniform float u_logDepthC;       // Tuning constant C (typically 1.0)
uniform float u_logDepthFarPlusOne; // log(C * far + 1)

out float v_logZ;
out float v_clipW;

void main()
{
    gl_Position = og_modelViewPerspectiveMatrix * vec4(a_position, 1.0);

    // Compute log-depth for the fragment shader.
    // We use clip-space w (which equals -z_eye for perspective projection)
    // to avoid needing the raw eye-space z.
    v_clipW = gl_Position.w;
    v_logZ = log(u_logDepthC * gl_Position.w + 1.0) / u_logDepthFarPlusOne;

    // Override the clip-space z so the rasterizer interpolates our log-depth
    // instead of the standard nonlinear depth.
    gl_Position.z = v_logZ * gl_Position.w;
}
```

**Fragment shader:**

```glsl
// Shaders/logdepth.frag
//
// Book Section 6.4, Listing 6.3 (fragment part)
// Writes the logarithmic depth value to gl_FragDepth.
// Uses layout(depth_greater) to partially preserve early-Z (4.2+).

#version 460 core

// This layout qualifier tells the GPU that our depth value will always
// be >= the rasterizer's interpolated depth. This enables partial
// early-Z rejection even though we write gl_FragDepth.
layout(depth_greater) out float gl_FragDepth;

in float v_logZ;
in float v_clipW;

out vec4 FragColor;

uniform float u_logDepthC;
uniform float u_logDepthFarPlusOne;

void main()
{
    // Recompute log-depth per-fragment for correct interpolation.
    // The vertex shader value is linearly interpolated, but log(z) is
    // not linear in z, so we need the per-fragment correction.
    gl_FragDepth = log(u_logDepthC * v_clipW + 1.0) / u_logDepthFarPlusOne;

    // Replace with your actual fragment color computation.
    FragColor = vec4(0.5, 0.5, 0.5, 1.0);
}
```

**C# setup:**

```csharp
// Logarithmic depth buffer setup
float logDepthC = 1.0f;
float logDepthFar = (float)farPlane;
float logDepthFarPlusOne = MathF.Log(logDepthC * logDepthFar + 1.0f);

shader.Use();
shader.SetFloat("u_logDepthC", logDepthC);
shader.SetFloat("u_logDepthFarPlusOne", logDepthFarPlusOne);
```

**Limitation:** Writing `gl_FragDepth` has a performance cost even with the `depth_greater` qualifier. The logarithmic function requires a `log()` per fragment. For virtual globe work where reversed-Z is available, logarithmic depth is usually unnecessary.

### Solution 4: Multiple Frustums

Render the scene in multiple passes, each with a different near/far range. Clear the depth buffer between passes. Render far-to-near so that closer objects correctly occlude farther ones.

```csharp
// Geode.Rendering/MultiFrustumRenderer.cs
//
// Book Section 6.5
// Renders the scene in multiple depth passes to avoid large far/near ratios.

using Silk.NET.OpenGL;
using System;

namespace Geode.Rendering
{
    public static class MultiFrustumRenderer
    {
        /// <summary>
        /// Represents one depth band with its own near and far planes.
        /// </summary>
        public readonly struct FrustumBand
        {
            public readonly double Near;
            public readonly double Far;

            public FrustumBand(double near, double far)
            {
                Near = near;
                Far = far;
            }
        }

        /// <summary>
        /// Computes frustum bands that cover from <paramref name="globalNear"/>
        /// to <paramref name="globalFar"/> with each band having a far/near ratio
        /// no larger than <paramref name="maxRatio"/>.
        /// </summary>
        public static FrustumBand[] ComputeBands(
            double globalNear, double globalFar, double maxRatio)
        {
            if (globalNear <= 0 || globalFar <= globalNear || maxRatio <= 1)
                throw new ArgumentException("Invalid frustum parameters.");

            // Number of bands needed: ceil(log(far/near) / log(maxRatio))
            double totalLogRange = Math.Log(globalFar / globalNear);
            double logMaxRatio = Math.Log(maxRatio);
            int numBands = (int)Math.Ceiling(totalLogRange / logMaxRatio);

            FrustumBand[] bands = new FrustumBand[numBands];

            double currentFar = globalFar;

            for (int i = 0; i < numBands; i++)
            {
                double currentNear;
                if (i == numBands - 1)
                {
                    // Last band: use the global near plane
                    currentNear = globalNear;
                }
                else
                {
                    currentNear = currentFar / maxRatio;
                }

                bands[i] = new FrustumBand(currentNear, currentFar);
                currentFar = currentNear;
            }

            return bands;
        }

        /// <summary>
        /// Renders the scene using multiple frustum passes.
        /// Call this instead of a single Draw call.
        /// </summary>
        /// <param name="gl">The GL context.</param>
        /// <param name="bands">Frustum bands from ComputeBands (far-to-near order).</param>
        /// <param name="renderBand">
        /// Callback that renders all geometry visible in the given band.
        /// The projection matrix should be recomputed for each band's near/far.
        /// </param>
        public static void RenderMultiFrustum(
            GL gl,
            FrustumBand[] bands,
            Action<FrustumBand> renderBand)
        {
            for (int i = 0; i < bands.Length; i++)
            {
                // Clear depth buffer before each band (but not color).
                // The first band also clears color.
                if (i == 0)
                {
                    gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                }
                else
                {
                    gl.Clear(ClearBufferMask.DepthBufferBit);
                }

                renderBand(bands[i]);
            }
        }
    }
}
```

**Usage:**

```csharp
// Example: render with 3 frustum bands
var bands = MultiFrustumRenderer.ComputeBands(
    globalNear: 0.1,
    globalFar: 100_000_000.0,
    maxRatio: 10_000.0);

MultiFrustumRenderer.RenderMultiFrustum(gl, bands, band =>
{
    // Rebuild projection matrix for this band
    float[] projection = ProjectionMatrices.StandardPerspective(
        fovY, aspectRatio, band.Near, band.Far);

    // Render all geometry with the new projection
    shader.Use();
    shader.SetMat4("u_projection", projection);
    // ... draw calls ...
});
```

**Limitation:** Multiple passes mean multiple draw calls, multiple state changes, and multiple traversals of the scene graph. Objects that span band boundaries may be drawn twice (clipped by both near/far planes). The overhead is proportional to the number of bands.

### Recommendation for Geode

Use **reversed-Z with an infinite far plane** as the default depth configuration. This provides:

1. **Optimal precision distribution** -- 32-bit float depth with reversed-Z has more precision at 1,000 km than a standard 24-bit integer depth buffer has at 100 meters.
2. **No far plane clipping** -- an infinite far plane means objects at any distance are rendered. No need to compute horizon distance for the far plane.
3. **Minimal performance impact** -- no per-fragment depth writes, no multiple passes.
4. **Simple setup** -- three GL calls at initialization time.

The depth configuration for Geode in `RenderContext` initialization:

```csharp
// In RenderContext.Initialize():
ReversedZSetup.Enable(gl);

// Use infinite far-plane projection everywhere:
float[] projection = ProjectionMatrices.ReversedZPerspectiveInfiniteFar(
    fovYRadians, aspectRatio, nearPlane);
```

For edge cases where reversed-Z is not sufficient (e.g., sub-millimeter precision at planetary distance), add logarithmic depth as an opt-in feature.

---

# Appendices

---

## Appendix A: Build Instructions

### Prerequisites

| Requirement | Version | Notes |
|---|---|---|
| .NET SDK | 9.0 or later | `dotnet --version` to check |
| GPU driver | OpenGL 4.6 capable | NVIDIA 470+, AMD Adrenalin 21+, Intel Arc |
| Operating system | Windows 10/11, Linux (X11 or Wayland) | macOS supports OpenGL 4.1 max -- the 4.6-specific features (DSA, clip control) will not work |

> **macOS Warning:** macOS deprecates OpenGL and caps support at 4.1. The DSA functions (`glCreate*`, `glNamed*`), `glClipControl`, and `layout(depth_greater)` are unavailable. To run Geode on macOS, you would need to replace the Rendering layer with a Metal or Vulkan backend via Silk.NET. This is outside the scope of this guide.

### Building and Running

```bash
# Clone the repository
git clone https://github.com/your-org/Geode.git
cd Geode

# Restore NuGet packages (Silk.NET, xunit, etc.)
dotnet restore

# Build all projects
dotnet build

# Run the application
dotnet run --project Geode.App

# Run tests
dotnet test
```

### IDE Setup

**Visual Studio 2022 (v17.8+)**
1. Open `Geode.slnx` (the solution file).
2. Build > Build Solution (Ctrl+Shift+B).
3. Set `Geode.App` as the startup project.
4. Press F5 to run.

**JetBrains Rider (2024.1+)**
1. Open the `Geode/` directory or `Geode.slnx`.
2. Rider detects the solution automatically.
3. Select `Geode.App` in the run configuration dropdown.
4. Ctrl+F5 to run without debugging.

**Visual Studio Code**
1. Install the C# Dev Kit extension.
2. Open the `Geode/` folder.
3. Open the Command Palette > `.NET: Build`.
4. Use the integrated terminal: `dotnet run --project Geode.App`.

### NuGet Publishing

The `Geode.Core` and `Geode.Rendering` projects are configured for NuGet packaging via `Directory.Build.props`. To create packages:

```bash
# Pack Core (no dependencies)
dotnet pack Geode.Core -c Release

# Pack Rendering (depends on Core and Silk.NET)
dotnet pack Geode.Rendering -c Release

# Packages are in bin/Release/*.nupkg
# Push to NuGet.org:
dotnet nuget push Geode.Core/bin/Release/Geode.Core.0.1.0.nupkg --source https://api.nuget.org/v3/index.json --api-key YOUR_KEY
```

---

## Appendix B: OpenGlobe to Geode Translation Table

This appendix maps OpenGlobe names to Geode names. Use it when reading the book to find the corresponding Geode code.

### Assembly Names

| OpenGlobe Assembly | Geode Assembly |
|---|---|
| `OpenGlobe.Core` | `Geode.Core` |
| `OpenGlobe.Renderer` | `Geode.Rendering` |
| `OpenGlobe.Scene` | `Geode.Visualization` |
| Various `Chapter*` examples | `Geode.App` |

### Core Types

| OpenGlobe Type | Geode Type | Notes |
|---|---|---|
| `Vector3D` | `Vector3D` | Identical API |
| `Geodetic2D` | `Geodetic2D` | Identical API |
| `Geodetic3D` | `Geodetic3D` | Identical API |
| `Ellipsoid` | `Ellipsoid` | Same algorithms, modernized C# |
| `Trig` | `Trigonometry` | Renamed for clarity |
| `EllipsoidTangentPlane` | (not yet implemented) | Future: local coordinate frames |
| `Matrix4D` | Not needed | Using `float[]` column-major arrays + `System.Numerics` |
| `Quaternion` | Not needed | Using `System.Numerics.Quaternion` when needed |
| `IndicesUInt32` | `uint[]` | Direct array, no wrapper |
| `IndicesUInt16` | `ushort[]` | Direct array, no wrapper |
| `TriangleMeshSubdivision` | `SubdivisionSphereTessellator` | Renamed, same algorithm |
| `GeographicGridEllipsoidTessellator` | `GeographicGridEllipsoidTessellator` | Same name |
| `BoxTessellator` | `BoxTessellator` | Same name |
| `MeshData` (various) | `MeshData` | Unified output type with interleaved vertex data |

### Renderer Types

| OpenGlobe Type | Geode Type | Notes |
|---|---|---|
| `ShaderProgram` | `ShaderProgram` | Same API, `FromFiles` added |
| `VertexBuffer` | `BufferObject<T>` | Generic, DSA creation |
| `IndexBuffer` | `BufferObject<T>` | Same class, T = uint |
| `VertexArray` | `VertexArrayObject` | DSA creation, separated format from binding |
| `Texture2D` | `Texture2D` | DSA creation |
| `RenderState` | `RenderState` | Same structure |
| `ClearState` | `ClearState` | Same structure |
| `DrawState` | `DrawState` | Same structure |
| `SceneState` | `SceneState` | Modernized |
| `Camera` | `CameraState` | Renamed to avoid confusion with Visualization-level camera |
| `Context` | `RenderContext` | Renamed for clarity |
| `Uniform` | `Uniform` | Same abstract base |
| `Uniform<T>` | `Uniform<T>` | Same generic value cache + dirty-on-change |
| `UniformFloatMatrix44GL3x` et al. | `UniformFloatMatrix44GL` et al. | Same concrete pattern, OpenGL 4.6 DSA via `glProgramUniform*` |
| `UniformCollection` | `UniformCollection` | Same named collection on `ShaderProgram` |
| `LinkAutomaticUniform` | `LinkAutomaticUniform` | Same abstract, same TextureUniform example |
| `DrawAutomaticUniformFactory` | `DrawAutomaticUniformFactory` | Same factory pattern |
| `DrawAutomaticUniform` | `DrawAutomaticUniform` | Same abstract, `Set(ctx, drawState, sceneState)` |
| `AutomaticUniformFactoryCollection` (on Device) | `AutomaticUniformFactoryCollection` (static) | Geode has no Device; registry is a static holder |
| `FragmentOutputs` (on ShaderProgram) | `FragmentOutputs` (on ShaderProgram) | Same, `glGetFragDataLocation` |
| `MeshBuffers` | (not wrapped) | Using `BufferObject<T>` directly |
| `VertexBufferAttribute` | `VertexAttrib` | Simplified |
| `Framebuffer` | `Framebuffer` | Same `ColorAttachments` collection + `DepthAttachment` / `DepthStencilAttachment` slots, DSA via `glNamedFramebufferTexture` |
| `ColorAttachments` | `ColorAttachments` | Same indexable wrapper |

### Scene/Visualization Types

| OpenGlobe Type | Geode Type | Notes |
|---|---|---|
| `TessellatedGlobe` | (inline in App) | Tessellation + shader + draw state composed in render loop |
| `RayCastedGlobe` | (inline in App) | Same approach: composed in render loop |
| `DayNightGlobe` | (inline in App) | Shader variant |
| `LatitudeLongitudeGrid` | (fragment shader) | Procedural, not a separate class |

### Tessellator Types

| OpenGlobe Type | Geode Type | Notes |
|---|---|---|
| `SubdivisionSphereTessellator` | `SubdivisionSphereTessellator` | Same algorithm |
| `GeographicGridEllipsoidTessellator` | `GeographicGridEllipsoidTessellator` | Same algorithm |
| `BoxTessellator` | `BoxTessellator` | Same algorithm |
| `CubeMapEllipsoidTessellator` | (not implemented) | Future: terrain tile mapping |

### Uniform Naming Convention

Geode follows the book's `og_*` naming for every automatic uniform exactly. Manual, per-shader, per-draw uniforms use `u_*` (or no prefix). The `AutomaticUniformFactoryCollection` registry (Section 19) is the source of truth for which names are automatic.

**Automatic uniforms (identical to OpenGlobe):**

| Name | Type | Source |
|---|---|---|
| `og_modelViewPerspectiveMatrix` | `mat4` | `SceneState.ModelViewPerspectiveMatrix` |
| `og_viewMatrix` | `mat4` | `SceneState.ViewMatrix` |
| `og_perspectiveMatrix` | `mat4` | `SceneState.PerspectiveMatrix` |
| `og_modelMatrix` | `mat4` | `SceneState.ModelMatrix` |
| `og_normalMatrix` | `mat3` | `SceneState.NormalMatrix` |
| `og_cameraEye` | `vec3` | `SceneState.CameraEyeFloat` |
| `og_cameraEyeHigh` | `vec3` | DSFP RTE high part (Section 27) |
| `og_cameraEyeLow` | `vec3` | DSFP RTE low part (Section 27) |
| `og_cameraLightPosition` | `vec3` | Light attached to the camera |
| `og_sunPosition` | `vec3` | Sun direction / position |
| `og_diffuseSpecularAmbientShininess` | `vec4` | Lighting parameters |
| `og_viewport` | `vec4` | `(x, y, width, height)` |
| `og_inverseViewport` | `vec4` | `(1/x, 1/y, 1/width, 1/height)` |
| `og_perspectiveNearPlaneDistance` | `float` | Near plane |
| `og_perspectiveFarPlaneDistance` | `float` | Far plane |
| `og_wgs84Height` | `float` | Camera altitude above WGS84 |
| `og_pixelSizePerDistance` | `float` | Screen-space LOD metric |
| `og_highResolutionSnapScale` | `float` | Sub-pixel snap factor |
| `og_texture0..og_texture7` | `sampler2D` | Link-automatic; binds to texture unit 0..7 |
| `og_modelZToClipCoordinates` | `mat4x2` | Logarithmic depth (Section 28) |

**Manual per-shader examples:**

| Name | Type | Notes |
|---|---|---|
| `u_dayTexture`, `u_nightTexture` | `sampler2D` | Shader-specific texture sampler; bound via `((Uniform<int>)shader.Uniforms[name]).Value = unit;` |
| `u_radii`, `u_oneOverRadiiSquared` | `vec3` | Ray-cast globe shader only (Section 25) |
| `u_gridLineWidth`, `u_gridColor` | `float`, `vec3` | Lat-long grid shader only (Section 24) |
| `u_blendDuration` | `float` | Day/night terminator width (Section 26) |

---

## Appendix C: Silk.NET vs OpenTK Quick Reference

Both Silk.NET and OpenTK are .NET bindings for OpenGL. OpenTK was the standard for a decade; Silk.NET is the modern replacement. This guide uses Silk.NET exclusively, but if you are porting code from OpenTK tutorials, this comparison helps.

### Window/Context Creation

**Silk.NET:**
```csharp
using Silk.NET.Windowing;
using Silk.NET.OpenGL;

var options = WindowOptions.Default;
options.Size = new(1920, 1080);
options.Title = "Geode";
options.API = new GraphicsAPI(ContextAPI.OpenGL, new APIVersion(4, 6));

IWindow window = Window.Create(options);
GL gl = null!;

window.Load += () =>
{
    gl = window.CreateOpenGL();
};

window.Render += (dt) =>
{
    // Render frame
};

window.Run();
```

**OpenTK:**
```csharp
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;

var settings = GameWindowSettings.Default;
var nativeSettings = new NativeWindowSettings
{
    ClientSize = new(1920, 1080),
    Title = "Geode",
    API = ContextAPI.OpenGL,
    APIVersion = new Version(4, 6)
};

using var window = new GameWindow(settings, nativeSettings);

window.Load += () =>
{
    // GL is a static class in OpenTK
};

window.RenderFrame += (e) =>
{
    // Render frame
};

window.Run();
```

### GL Function Calls

| Operation | Silk.NET (Instance, DSA 4.6) | OpenTK (Static, 3.3 bind-to-edit) |
|---|---|---|
| **Create buffer** | `uint buf = gl.CreateBuffer();` | `GL.GenBuffer(out int buf); GL.BindBuffer(target, buf);` |
| **Upload data** | `gl.NamedBufferStorage(buf, size, data, flags);` | `GL.BufferData(target, size, data, usage);` |
| **Create texture** | `uint tex = gl.CreateTexture(TextureTarget.Texture2D);` | `GL.GenTexture(out int tex); GL.BindTexture(target, tex);` |
| **Set texture data** | `gl.TextureStorage2D(tex, levels, format, w, h);` `gl.TextureSubImage2D(tex, 0, 0, 0, w, h, pf, pt, data);` | `GL.TexImage2D(target, 0, pif, w, h, 0, pf, pt, data);` |
| **Create VAO** | `uint vao = gl.CreateVertexArray();` | `GL.GenVertexArray(out int vao); GL.BindVertexArray(vao);` |
| **Set attrib format** | `gl.VertexArrayAttribFormat(vao, index, size, type, norm, offset);` `gl.VertexArrayVertexBuffer(vao, binding, buf, 0, stride);` | `GL.VertexAttribPointer(index, size, type, norm, stride, offset);` |
| **Upload matrix** | `gl.UniformMatrix4(loc, 1, false, matrix);` | `GL.UniformMatrix4(loc, false, ref matrix);` |

### Six Key Differences

1. **Instance vs static.** Silk.NET's `GL` is an instance obtained from the window. OpenTK's `GL` is a static class. Silk.NET's approach allows multiple contexts and is more testable.

2. **Direct State Access.** Silk.NET naturally supports DSA because it wraps OpenGL 4.5+ functions. OpenTK traditionally wraps 3.3-style bind-to-edit functions. Recent OpenTK versions added DSA support but it is less idiomatic.

3. **Enum names.** Silk.NET uses `EnableCap.DepthTest`, `BufferStorageMask.DynamicStorageBit`. OpenTK uses `EnableCap.DepthTest`, `BufferStorageFlags.DynamicStorageBit`. Similar but not identical.

4. **Matrix upload.** Silk.NET: `gl.UniformMatrix4(loc, 1, false, matrixSpan)` takes a `ReadOnlySpan<float>`. OpenTK: `GL.UniformMatrix4(loc, false, ref matrix4)` takes a `ref Matrix4`.

5. **Windowing.** Silk.NET uses `Silk.NET.Windowing` (wraps GLFW). OpenTK uses `OpenTK.Windowing.Desktop` (wraps GLFW). Both wrap GLFW but with different APIs.

6. **NuGet packages.** Silk.NET: `Silk.NET.OpenGL`, `Silk.NET.Windowing`, `Silk.NET.Input`. OpenTK: `OpenTK` (single package).

---

## Appendix D: Rendering Challenges Unique to Virtual Globes

This appendix summarizes the core challenges from the book's introduction and where they are addressed in this guide. Use it as a quick index when you encounter a specific problem.

### D.1 Precision

**Problem:** IEEE 754 single-precision floats have ~7 decimal digits. At Earth-scale coordinates (6+ million meters), the smallest representable step is 0.5 meters. Vertex positions cannot be more precise than this.

**Solution:** Double-precision coordinates in `Geode.Core` (Section 4). GPU-side precision via GPU RTE with DSFUN90 (Section 27). All internal computation uses `double`; conversion to `float` happens only at the GPU boundary.

### D.2 Accuracy

**Problem:** The Earth is an oblate ellipsoid, not a sphere. The equatorial-to-polar radius difference is ~21.4 km. Spherical approximations produce visible errors at continental scales.

**Solution:** The `Ellipsoid` class (Section 5) models a triaxial ellipsoid. `Ellipsoid.Wgs84` encodes the standard geodetic datum. All normals, coordinate transforms, and tessellation use full ellipsoidal math.

### D.3 Curvature

**Problem:** Straight lines between surface points pass through the planet interior. Polylines, borders, and flight paths dip underground between vertices.

**Solution:** `Ellipsoid.ComputeCurve` (Section 5) generates great-arc curves via Rodrigues' rotation. Tessellated meshes have sufficient vertex density that edges approximate the surface curvature.

### D.4 Depth Buffer

**Problem:** Standard depth buffers have a far/near-dependent nonlinear distribution that concentrates precision near the camera. At planetary scale (far/near ratio > 10^7), z-fighting is catastrophic.

**Solution:** Reversed-Z depth with `glClipControl` (Section 28). 32-bit float depth buffer with near->1.0, far->0.0 mapping. Infinite far plane projection. Optional logarithmic depth or multi-frustum for edge cases.

### D.5 Massive Datasets

**Problem:** Earth-scale imagery at 1 m/pixel resolution requires petabytes of data. No machine can hold this in memory.

**Solution:** Tile-based quadtree with asynchronous loading (future Geode.Visualization work, extending Part IV). Data is streamed from disk or network, decoded on background threads, and uploaded to GPU textures on the render thread.

### D.6 Multithreading

**Problem:** An OpenGL context is bound to one thread. Data loading must happen on worker threads, but GL resource creation must happen on the render thread.

**Solution:** Architectural separation: `Geode.Core` is thread-safe (immutable value types). Data loading produces raw byte arrays on background threads. The render loop in `Geode.App` consumes them and creates GL objects. The Rendering layer's `IDisposable` pattern ensures GL resources are freed on the correct thread.

---

*Every code listing references its book section and OpenGlobe source file. Build each step incrementally. Verify it compiles before moving on.*
