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

### Part I -- Introduction (Book Chapter 1)
- [Section 1: Rendering Challenges in Virtual Globes](#section-1-rendering-challenges-in-virtual-globes)
- [Section 2: Project Architecture](#section-2-project-architecture)

### Part II -- Math Foundations (Book Chapter 2)
- [Section 3: Virtual Globe Coordinate Systems](#section-3-virtual-globe-coordinate-systems)
- [Section 4: Core Math Types](#section-4-core-math-types) -- `Trigonometry.cs`, `Constants.cs`, `Vector3D.cs`, `Geodetic2D.cs`, `Geodetic3D.cs`
- [Section 5: The Ellipsoid Class](#section-5-the-ellipsoid-class) -- `Ellipsoid.cs`
- [Section 6: Coordinate Transformations](#section-6-coordinate-transformations)
- [Section 7: Curves on an Ellipsoid](#section-7-curves-on-an-ellipsoid)

### Part III -- Renderer Design (Book Chapter 3)
- [Section 8: Coordinate Spaces and Transform Chain](#section-8-coordinate-spaces-and-transform-chain)
- [Section 9: OpenGL Fundamentals](#section-9-opengl-fundamentals)
- [Section 10: Renderer Architecture Deep Dive](#section-10-renderer-architecture-deep-dive)
- [Section 11: The Shader Pipeline](#section-11-the-shader-pipeline) -- `ShaderProgram.cs`
- [Section 12: Vertex Buffers](#section-12-vertex-buffers) -- `BufferObject.cs`, `VertexAttrib.cs`, `VertexArrayObject.cs`
- [Section 13: Textures](#section-13-textures) -- `Texture2D.cs`
- [Section 14: Vertex Data Layouts](#section-14-vertex-data-layouts)
- [Section 15: Renderer State Objects](#section-15-renderer-state-objects) -- `RenderState.cs`, `ClearState.cs`
- [Section 16: Camera and Scene State](#section-16-camera-and-scene-state) -- `CameraState.cs`, `SceneState.cs`
- [Section 17: Draw State](#section-17-draw-state) -- `DrawState.cs`
- [Section 18: Render Context](#section-18-render-context) -- `RenderContext.cs`
- [Section 19: The Automatic Uniform System](#section-19-the-automatic-uniform-system)
- [Section 20: Window, Context, Render Loop, and Drawing a Triangle](#section-20-window-context-render-loop-and-drawing-a-triangle) -- `Program.cs`

### Part IV -- Globe Rendering (Book Chapter 4)
- [Section 21: Tessellating the Globe](#section-21-tessellating-the-globe)
- [Section 22: Terrain and Imagery Tiles](#section-22-terrain-and-imagery-tiles)
- [Section 23: Level-of-Detail Selection](#section-23-level-of-detail-selection)
- [Section 24: Globe Surface Shading](#section-24-globe-surface-shading)
- [Section 25: Texture Mapping on Ellipsoids](#section-25-texture-mapping-on-ellipsoids)
- [Section 26: Putting It All Together](#section-26-putting-it-all-together)

### Part V -- Vertex Transform Precision (Book Chapter 5)
- [Section 27: RTE and DSFP Vertex Transforms](#section-27-rte-and-dsfp-vertex-transforms)

### Part VI -- Depth Buffer Precision (Book Chapter 6)
- [Section 28: Reversed-Z and Logarithmic Depth](#section-28-reversed-z-and-logarithmic-depth)

### Appendices
- [Appendix A: Solution and Project Setup](#appendix-a-solution-and-project-setup)
- [Appendix B: Silk.NET Windowing Boilerplate](#appendix-b-silknet-windowing-boilerplate)
- [Appendix C: GLSL Quick Reference](#appendix-c-glsl-quick-reference)
- [Appendix D: Further Reading and Resources](#appendix-d-further-reading-and-resources)

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

For high-accuracy applications, each intermediate point should be projected back onto the ellipsoid surface using `ScaleToSurfaceGeodetic`:

```csharp
// High-accuracy curve: project each point back to the surface
IList<Vector3D> rawCurve = ellipsoid.ComputeCurve(start, end, granularity);
List<Vector3D> accurateCurve = new List<Vector3D>(rawCurve.Count);
foreach (Vector3D point in rawCurve)
{
    accurateCurve.Add(ellipsoid.ScaleToSurfaceGeodetic(point));
}
```

This is more expensive (Newton-Raphson per point) but ensures all points lie on the ellipsoid surface to sub-nanometer accuracy.

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

The rendering layer sits between the raw OpenGL API (exposed through Silk.NET) and the high-level globe visualization. Its job is to turn the mathematical foundations from Part II into pixels on screen. This part builds the entire `Geode.Rendering` assembly and the initial `Geode.App` entry point -- twelve source files that compile and run as a complete, working rendering pipeline.

Every file appears in **strict build-dependency order**. When a class references another type, that type has already been defined in an earlier section. You can follow this part from start to finish, creating each file as you go, and the solution will compile at every step.

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

uniform mat4 uMVP;                         // Model-View-Projection matrix

void main()
{
    gl_Position = uMVP * vec4(aPosition, 1.0);  // Transform to clip space
    vColor = aColor;                              // Pass color through
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
                    Bind ShaderProgram (glUseProgram)
                          │
                          v
                    SetSceneUniforms(sceneState)
                      ├── uMVP            (mat4)
                      ├── uView           (mat4)
                      ├── uProjection     (mat4)
                      ├── uCameraEye      (vec3)
                      ├── uSunPosition    (vec3)
                      └── uDiffuseSpecularAmbientShininess (vec4)
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

*Corresponds to Book Chapter 3, Section 3.4*

*OpenGlobe source: `Source/Renderer/GL3x/Shaders/ShaderProgramGL3x.cs`*

A shader program is a pair of GLSL source files (vertex + fragment) compiled and linked into a single GPU program. The `ShaderProgram` class handles compilation, error checking, linking, and uniform setting.

This is the first file in `Geode.Rendering` that we build. It has no internal dependencies -- only Silk.NET.

### Design Decisions

**Why throw exceptions on compile/link errors?** A shader that fails to compile is a programmer error -- the source is wrong. Silently returning a null program would push the error to the first draw call, making it much harder to diagnose. Failing fast with a descriptive message (including the full GLSL error log) makes shader debugging tractable.

**Why delete individual shaders after linking?** Once `glLinkProgram` succeeds, the individual vertex and fragment shader objects are no longer needed. The linked program contains all the compiled code. Deleting them immediately prevents resource leaks. We also detach them first, which is good practice even though deletion would detach them implicitly.

**Why return -1 silently for missing uniforms?** The GLSL compiler is allowed to optimize away uniforms that are not used in the shader output. Setting a uniform at location -1 is a no-op in OpenGL -- it does not generate an error. Silently returning -1 means application code can set uniforms without checking whether the current shader actually uses them. This is important when the same `SceneState` uniform-setting code is used with multiple shaders.

### Complete Source

```csharp
// Geode.Rendering/ShaderProgram.cs
//
// Compiles GLSL vertex + fragment shaders, links them into a program,
// and provides typed uniform setters.
//
// Book Chapter 3, Section 3.4.
// OpenGlobe: Source/Renderer/GL3x/Shaders/ShaderProgramGL3x.cs
//
// Design: Fail-fast on compilation errors. Silent on missing uniforms.
// The caller never needs to check whether a uniform exists -- setting a
// nonexistent uniform is a no-op.

using Silk.NET.OpenGL;
using System;

namespace Geode.Rendering
{
    /// <summary>
    /// A compiled and linked GLSL shader program (vertex + fragment).
    /// Wraps a GL program handle and provides typed uniform setters.
    /// </summary>
    public class ShaderProgram : IDisposable
    {
        private readonly GL _gl;
        private readonly uint _handle;

        /// <summary>The raw GL program handle.</summary>
        public uint Handle => _handle;

        // ---------------------------------------------------------------
        // Construction
        // ---------------------------------------------------------------

        /// <summary>
        /// Compiles vertex and fragment shader sources, links them into a program.
        /// Throws on compilation or link failure with the full GLSL error log.
        /// </summary>
        /// <param name="gl">The Silk.NET OpenGL context.</param>
        /// <param name="vertexSource">Complete GLSL vertex shader source.</param>
        /// <param name="fragmentSource">Complete GLSL fragment shader source.</param>
        /// <exception cref="Exception">Thrown if compilation or linking fails.</exception>
        public ShaderProgram(GL gl, string vertexSource, string fragmentSource)
        {
            _gl = gl;

            // Compile individual shaders
            uint vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
            uint fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);

            // Create program and link
            _handle = _gl.CreateProgram();
            _gl.AttachShader(_handle, vertexShader);
            _gl.AttachShader(_handle, fragmentShader);
            _gl.LinkProgram(_handle);

            // Check link status
            _gl.GetProgram(_handle, ProgramPropertyARB.LinkStatus, out int status);
            if (status == 0)
            {
                string log = _gl.GetProgramInfoLog(_handle);
                // Clean up before throwing
                _gl.DeleteProgram(_handle);
                _gl.DeleteShader(vertexShader);
                _gl.DeleteShader(fragmentShader);
                throw new Exception($"Shader link error: {log}");
            }

            // Once linked, individual shaders are no longer needed.
            // Detach first (good practice), then delete to free GPU memory.
            _gl.DetachShader(_handle, vertexShader);
            _gl.DetachShader(_handle, fragmentShader);
            _gl.DeleteShader(vertexShader);
            _gl.DeleteShader(fragmentShader);
        }

        /// <summary>
        /// Creates a ShaderProgram from vertex and fragment shader files on disk.
        /// </summary>
        /// <param name="gl">The Silk.NET OpenGL context.</param>
        /// <param name="vertexPath">Path to the .vert file.</param>
        /// <param name="fragmentPath">Path to the .frag file.</param>
        /// <returns>A compiled and linked ShaderProgram.</returns>
        public static ShaderProgram FromFiles(GL gl, string vertexPath, string fragmentPath)
        {
            string vertexSource = System.IO.File.ReadAllText(vertexPath);
            string fragmentSource = System.IO.File.ReadAllText(fragmentPath);
            return new ShaderProgram(gl, vertexSource, fragmentSource);
        }

        // ---------------------------------------------------------------
        // Usage
        // ---------------------------------------------------------------

        /// <summary>
        /// Activates this shader program for subsequent draw calls.
        /// Equivalent to glUseProgram(handle).
        /// </summary>
        public void Use() => _gl.UseProgram(_handle);

        // ---------------------------------------------------------------
        // Uniform setters
        // ---------------------------------------------------------------

        /// <summary>Sets an integer uniform (e.g., sampler binding).</summary>
        public void SetInt(string name, int value)
            => _gl.Uniform1(GetUniformLocation(name), value);

        /// <summary>Sets a float uniform.</summary>
        public void SetFloat(string name, float value)
            => _gl.Uniform1(GetUniformLocation(name), value);

        /// <summary>Sets a vec3 uniform (e.g., camera position, light direction).</summary>
        public void SetVec3(string name, float x, float y, float z)
            => _gl.Uniform3(GetUniformLocation(name), x, y, z);

        /// <summary>Sets a vec4 uniform (e.g., color, material properties).</summary>
        public void SetVec4(string name, float x, float y, float z, float w)
            => _gl.Uniform4(GetUniformLocation(name), x, y, z, w);

        /// <summary>
        /// Sets a mat4 uniform (e.g., MVP matrix).
        /// The array must contain exactly 16 floats in column-major order
        /// (OpenGL's default matrix layout).
        /// </summary>
        public void SetMat4(string name, float[] mat)
            => _gl.UniformMatrix4(GetUniformLocation(name), 1, false, mat);

        // ---------------------------------------------------------------
        // Internals
        // ---------------------------------------------------------------

        /// <summary>
        /// Gets the location of a uniform variable by name.
        /// Returns -1 if the uniform does not exist or was optimized away.
        /// Setting a uniform at location -1 is a no-op in OpenGL.
        /// </summary>
        private int GetUniformLocation(string name)
        {
            int loc = _gl.GetUniformLocation(_handle, name);
            return loc;
        }

        /// <summary>
        /// Compiles a single shader stage (vertex or fragment).
        /// </summary>
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

        /// <summary>
        /// Deletes the GL program. Must be called on the render thread.
        /// </summary>
        public void Dispose()
        {
            _gl.DeleteProgram(_handle);
        }
    }
}
```

**Line count:** ~110 lines (excluding blank lines and comments).

**What this gives us:** Any pair of GLSL source strings can be compiled, linked, and used in three lines:

```csharp
var shader = new ShaderProgram(gl, vertSrc, fragSrc);
shader.Use();
shader.SetMat4("uMVP", mvpMatrix);
```

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
// Geode.Rendering/BufferObject.cs
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
// Geode.Rendering/VertexAttrib.cs
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
// Geode.Rendering/VertexArrayObject.cs
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

namespace Geode.Rendering
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

## Section 13: Textures

*Corresponds to Book Chapter 3, Section 3.6*

*OpenGlobe source: `Source/Renderer/GL3x/Textures/Texture2DGL3x.cs`*

A texture is a 2D image stored on the GPU, sampled by fragment shaders to color surfaces. The `Texture2D` class wraps texture creation, parameter setup, mipmap generation, and binding.

### Texture Concepts

**Samplers** are the GLSL mechanism for reading textures. A `sampler2D` uniform is bound to a **texture unit** (an integer slot). The texture unit links the GLSL sampler to a specific texture object. In code:

```csharp
texture.Bind(0);                      // Bind texture to unit 0
shader.SetInt("uTexture", 0);         // Tell the sampler to read from unit 0
```

**Filtering** controls how the GPU interpolates between texels (texture pixels):
- `GL_NEAREST`: nearest-neighbor, pixelated look
- `GL_LINEAR`: bilinear interpolation, smooth
- `GL_LINEAR_MIPMAP_LINEAR`: trilinear, smooth with LOD transitions (best quality for minification)

**Wrapping** controls what happens when texture coordinates go outside [0, 1]:
- `GL_REPEAT`: the texture tiles
- `GL_CLAMP_TO_EDGE`: the edge texel is extended

For globe rendering, longitude wrapping is `REPEAT` (the globe wraps around) and latitude is `CLAMP_TO_EDGE` (the poles do not wrap).

> **3.3 vs 4.6 -- Texture Creation**
>
> In OpenGL 3.3, texture creation requires binding: `glGenTextures` + `glBindTexture(GL_TEXTURE_2D, handle)` + `glTexImage2D(...)`. Any other code that binds `GL_TEXTURE_2D` between your gen and your tex calls corrupts your setup. With DSA, `glCreateTextures(GL_TEXTURE_2D)` + `glTextureStorage2D(handle, ...)` operates directly on the handle. No binding, no corruption risk.

### Complete Source

```csharp
// Geode.Rendering/Texture2D.cs
//
// A 2D texture stored on the GPU with mipmaps.
// Uses DSA: glCreateTextures, glTextureStorage2D, glTextureSubImage2D.
//
// Book Chapter 3, Section 3.6.
// OpenGlobe: Source/Renderer/GL3x/Textures/Texture2DGL3x.cs
//
// Default sampler settings:
//   Min filter: LinearMipmapLinear (trilinear -- best quality for minification)
//   Mag filter: Linear (bilinear -- smooth magnification)
//   Wrap S:     Repeat (longitude wraps around the globe)
//   Wrap T:     ClampToEdge (latitude does not wrap past the poles)

using Silk.NET.OpenGL;
using System;

namespace Geode.Rendering
{
    /// <summary>
    /// A 2D texture on the GPU with automatic mipmap generation.
    /// </summary>
    public class Texture2D : IDisposable
    {
        private readonly GL _gl;
        private readonly uint _handle;

        /// <summary>The raw GL texture handle.</summary>
        public uint Handle => _handle;

        /// <summary>Texture width in pixels.</summary>
        public int Width { get; }

        /// <summary>Texture height in pixels.</summary>
        public int Height { get; }

        /// <summary>
        /// Creates a 2D texture from raw RGBA pixel data.
        /// </summary>
        /// <param name="gl">The Silk.NET OpenGL context.</param>
        /// <param name="width">Texture width in pixels.</param>
        /// <param name="height">Texture height in pixels.</param>
        /// <param name="pixels">
        /// Raw pixel data in RGBA format (4 bytes per pixel).
        /// The array length must be width * height * 4.
        /// </param>
        public unsafe Texture2D(GL gl, int width, int height, byte[] pixels)
        {
            _gl = gl;
            Width = width;
            Height = height;

            // DSA: Create texture object with a specific target
            _handle = _gl.CreateTexture(TextureTarget.Texture2D);

            // Compute mipmap levels: floor(log2(max(width, height))) + 1
            int levels = 1 + (int)Math.Floor(Math.Log2(Math.Max(width, height)));

            // Allocate immutable storage for all mipmap levels.
            // SizedInternalFormat.Rgba8: 8 bits per channel, 4 channels.
            _gl.TextureStorage2D(_handle, (uint)levels, SizedInternalFormat.Rgba8,
                (uint)width, (uint)height);

            // Upload pixel data to mipmap level 0 (the full-resolution image).
            fixed (byte* ptr = pixels)
            {
                _gl.TextureSubImage2D(_handle, 0, 0, 0,
                    (uint)width, (uint)height,
                    PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            }

            // Set default sampler parameters.
            // Min filter: trilinear (linear interpolation between mipmap levels)
            _gl.TextureParameterI(_handle, TextureParameterName.TextureMinFilter,
                (int)GLEnum.LinearMipmapLinear);
            // Mag filter: bilinear (smooth magnification)
            _gl.TextureParameterI(_handle, TextureParameterName.TextureMagFilter,
                (int)GLEnum.Linear);
            // Wrap S (horizontal / longitude): repeat for globe wrap-around
            _gl.TextureParameterI(_handle, TextureParameterName.TextureWrapS,
                (int)GLEnum.Repeat);
            // Wrap T (vertical / latitude): clamp to prevent pole artifacts
            _gl.TextureParameterI(_handle, TextureParameterName.TextureWrapT,
                (int)GLEnum.ClampToEdge);

            // Generate all mipmap levels from the base level.
            // This computes progressively smaller versions of the texture
            // for use when the surface is far from the camera.
            _gl.GenerateTextureMipmap(_handle);
        }

        /// <summary>
        /// Binds this texture to a texture unit for use by a sampler in a shader.
        /// </summary>
        /// <param name="unit">
        /// The texture unit index (0, 1, 2, ...). Must match the integer value
        /// set on the sampler2D uniform in the shader.
        /// </param>
        public void Bind(uint unit)
        {
            // DSA: glBindTextureUnit binds a texture to a specific unit
            // without affecting the active texture state.
            _gl.BindTextureUnit(unit, _handle);
        }

        /// <summary>
        /// Loads a texture from an image file on disk.
        /// Requires StbImageSharp (included in Geode.Rendering.csproj).
        /// </summary>
        /// <param name="gl">The Silk.NET OpenGL context.</param>
        /// <param name="path">Path to the image file (PNG, JPG, BMP, etc.).</param>
        /// <returns>A Texture2D with the image data uploaded and mipmaps generated.</returns>
        public static Texture2D FromFile(GL gl, string path)
        {
            // StbImageSharp loads images into a flat byte array in RGBA format.
            // It handles PNG, JPG, BMP, TGA, and other common formats.
            using var stream = System.IO.File.OpenRead(path);
            var image = StbImageSharp.ImageResult.FromStream(stream,
                StbImageSharp.ColorComponents.RedGreenBlueAlpha);

            return new Texture2D(gl, image.Width, image.Height, image.Data);
        }

        /// <summary>
        /// Deletes the GL texture. Must be called on the render thread.
        /// </summary>
        public void Dispose()
        {
            _gl.DeleteTexture(_handle);
        }
    }
}
```

**Line count:** ~80 lines.

**Note on StbImageSharp:** The `FromFile` factory uses `StbImageSharp`, which is already a dependency in `Geode.Rendering.csproj`. It is a pure-C# image loader -- no native binaries needed. For production globe tiles, you would use a more specialized loader (e.g., for GeoTIFF or JPEG2000), but StbImageSharp is sufficient for development textures.

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
        /// Applies render state, binds shader and VAO, sets scene uniforms,
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

            // 2. Bind shader
            drawState.ShaderProgram.Use();

            // 3. Set scene uniforms
            SetSceneUniforms(drawState.ShaderProgram, sceneState);

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
            drawState.ShaderProgram.Use();
            SetSceneUniforms(drawState.ShaderProgram, sceneState);
            _gl.BindVertexArray(drawState.VertexArrayObject.Handle);
            _gl.DrawArrays(primitiveType, (int)first, count);
        }

        // ---------------------------------------------------------------
        // Scene uniforms
        // ---------------------------------------------------------------

        /// <summary>
        /// Sets standard scene uniforms on the given shader program.
        /// Shaders that do not use a particular uniform will silently
        /// ignore it (setting at location -1 is a no-op).
        /// </summary>
        private void SetSceneUniforms(ShaderProgram shader, SceneState scene)
        {
            // MVP matrix (combined model-view-projection)
            Matrix4x4 mvp = scene.ModelViewPerspectiveMatrix;
            shader.SetMat4("uMVP", Matrix4x4ToArray(mvp));

            // View matrix (for eye-space lighting calculations)
            Matrix4x4 view = scene.ViewMatrix;
            shader.SetMat4("uView", Matrix4x4ToArray(view));

            // Projection matrix (for reconstruction from depth)
            Matrix4x4 proj = scene.PerspectiveMatrix;
            shader.SetMat4("uProjection", Matrix4x4ToArray(proj));

            // Camera position (for specular lighting and atmospheric effects)
            Vector3 eye = scene.CameraEyeFloat;
            shader.SetVec3("uCameraEye", eye.X, eye.Y, eye.Z);

            // Sun position (directional light source)
            shader.SetVec3("uSunPosition",
                (float)scene.SunPosition.X,
                (float)scene.SunPosition.Y,
                (float)scene.SunPosition.Z);

            // Material/lighting parameters packed into a vec4
            Vector4 dsas = scene.DiffuseSpecularAmbientShininess;
            shader.SetVec4("uDiffuseSpecularAmbientShininess",
                dsas.X, dsas.Y, dsas.Z, dsas.W);
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

*Corresponds to Book Chapter 3, Section 3.4.2*

*OpenGlobe source: `Source/Renderer/GL3x/Shaders/UniformFactory.cs`, `Source/Renderer/DrawAutomaticUniforms/`*

This is a conceptual section -- no new code files. It explains the book's automatic uniform system and how our simplified version in `SceneState`/`RenderContext` achieves the same goal.

### The Problem

In a naive renderer, every draw call manually sets its uniforms:

```csharp
shader.SetMat4("uMVP", mvpMatrix);
shader.SetVec3("uCameraEye", eyeX, eyeY, eyeZ);
shader.SetVec3("uSunPosition", sunX, sunY, sunZ);
// ... repeat for every shader, every frame
```

This is error-prone. If you add a new global uniform (say, `uTime`), you must update every draw call site. Miss one, and that shader gets stale data.

### OpenGlobe's Solution: Automatic Uniforms

OpenGlobe solves this with a convention: any uniform whose name starts with `og_` is automatically set by the renderer. The `UniformFactory` scans each shader program's active uniforms at link time and creates setter objects for recognized names:

| Uniform Name | Type | Source |
|---|---|---|
| `og_modelViewPerspectiveMatrix` | `mat4` | MVP matrix |
| `og_viewMatrix` | `mat4` | View matrix |
| `og_perspectiveMatrix` | `mat4` | Projection matrix |
| `og_cameraEye` | `vec3` | Camera position (float) |
| `og_cameraEyeHigh` | `vec3` | Camera position (high float for DSFP) |
| `og_cameraEyeLow` | `vec3` | Camera position (low float for DSFP) |
| `og_sunPosition` | `vec3` | Sun position |
| `og_sunDirectionEC` | `vec3` | Sun direction in eye coordinates |
| `og_diffuseSpecularAmbientShininess` | `vec4` | Material parameters |
| `og_modelZToClipCoordinates` | `mat42` | For logarithmic depth |
| `og_viewport` | `vec4` | Viewport dimensions |
| `og_inverseViewport` | `vec4` | 1/viewport dimensions |
| `og_windowToWorldNearPlane` | `mat4` | For unprojecting screen coords |
| `og_perspectiveNearPlaneDistance` | `float` | Near plane distance |
| `og_perspectiveFarPlaneDistance` | `float` | Far plane distance |
| `og_normalMatrix` | `mat3` | Inverse-transpose of model-view |
| `og_cameraLightPosition` | `vec3` | Light at camera position |
| `og_highResolutionSnapScale` | `float` | For sub-pixel precision |
| `og_pixelSizePerDistance` | `float` | Screen-space LOD metric |
| `og_wgs84Height` | `float` | Camera height above WGS84 |

### Our Simplified Approach

In Geode, we skip the factory/reflection pattern and set a fixed set of uniforms in `RenderContext.SetSceneUniforms()`. If a shader does not use a particular uniform, the `SetMat4`/`SetVec3` call targets location -1, which is a no-op. This is simpler and sufficient until we need the full automatic system.

Our current uniform set:

| Uniform | Type | Set By |
|---|---|---|
| `uMVP` | `mat4` | `SceneState.ModelViewPerspectiveMatrix` |
| `uView` | `mat4` | `SceneState.ViewMatrix` |
| `uProjection` | `mat4` | `SceneState.PerspectiveMatrix` |
| `uCameraEye` | `vec3` | `SceneState.CameraEyeFloat` |
| `uSunPosition` | `vec3` | `SceneState.SunPosition` (cast to float) |
| `uDiffuseSpecularAmbientShininess` | `vec4` | `SceneState.DiffuseSpecularAmbientShininess` |

When we add DSFP transforms (Section 27), we will add `uCameraEyeHigh` and `uCameraEyeLow`. When we add logarithmic depth (Section 28), we will add `uLogDepthC`. The fixed-set approach scales well enough for our needs.

### Built-In GLSL Constants

OpenGlobe also defines built-in constants via a preamble injected into every shader:

```glsl
const float og_pi       = 3.14159265358979323846;
const float og_halfPi   = 1.57079632679489661923;
const float og_twoPi    = 6.28318530717958647693;
const float og_oneOverPi = 0.31830988618379067154;
const float og_oneOverTwoPi = 0.15915494309189533577;
const float og_e        = 2.71828182845904523536;
```

We do not inject these automatically. If a shader needs pi, it uses the GLSL `radians(180.0)` or defines a local constant. This keeps our shader pipeline simple.

---

## Section 20: Step 1 -- Window, Context, Render Loop, and Drawing a Triangle

*Corresponds to Book Chapter 3, Section 3.11*

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

// Scene uniforms (set by RenderContext.SetSceneUniforms)
uniform mat4 uMVP;

void main()
{
    gl_Position = uMVP * vec4(aPosition, 1.0);
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
5. **RenderContext** applied the render state, set the scene uniforms (MVP matrix), bound the shader and VAO, and issued `glDrawElements`.
6. **SceneState** computed the view and projection matrices in double precision and converted them to float for GPU upload.
7. **ClearState** cleared the framebuffer to the correct color.

Every class in `Geode.Rendering` was exercised. The renderer works.

### Next Steps

Part IV will replace this triangle with a tessellated ellipsoid -- the actual globe. The `SceneState` camera will be positioned at a realistic altitude above the WGS84 surface, and the shaders will compute per-pixel lighting using the geodetic surface normal. But the rendering pipeline -- `ClearState` -> `DrawState` -> `RenderContext.Draw()` -- remains exactly the same. That is the power of the abstraction.

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

uniform mat4 u_modelViewProjection;

void main()
{
    gl_Position = u_modelViewProjection * vec4(a_position, 1.0);
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

uniform mat4 u_modelViewProjection;
uniform mat4 u_modelView;

out vec3 v_normalEC;

void main()
{
    // Transform normal to eye/camera space.
    // We use the upper-left 3x3 of the model-view matrix.
    // For a uniform-scale model matrix, this is correct.
    // For non-uniform scale, use the inverse-transpose instead.
    v_normalEC = mat3(u_modelView) * a_normal;

    gl_Position = u_modelViewProjection * vec4(a_position, 1.0);
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

uniform mat4 u_modelViewProjection;
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

    gl_Position = u_modelViewProjection * vec4(a_position, 1.0);
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

uniform mat4 u_modelViewProjection;
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

    gl_Position = u_modelViewProjection * vec4(a_position, 1.0);
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

uniform mat4 u_modelViewProjection;
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

    gl_Position = u_modelViewProjection * vec4(a_position, 1.0);
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

// 4. Set uniforms each frame
shader.Use();
shader.SetMat4("u_modelViewProjection", sceneState.ModelViewProjectionMatrix);
shader.SetMat4("u_modelView", sceneState.ModelViewMatrix);
shader.SetVec3("u_lightDirectionEC", lightDirECx, lightDirECy, lightDirECz);
shader.SetVec3("u_ambientColor", 0.1f, 0.1f, 0.1f);
shader.SetVec3("u_specularColor", 0.3f, 0.3f, 0.3f);
shader.SetFloat("u_shininess", 32.0f);
shader.SetInt("u_dayTexture", 0); // texture unit 0
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

uniform mat4 u_modelViewProjection;

out vec3 v_worldPosition;

void main()
{
    v_worldPosition = a_position;
    gl_Position = u_modelViewProjection * vec4(a_position, 1.0);
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

// 4. Per-frame uniforms
shader.Use();
shader.SetMat4("u_modelViewProjection", sceneState.ModelViewProjectionMatrix);
shader.SetMat4("u_modelView", sceneState.ModelViewMatrix);
shader.SetMat4("u_projection", sceneState.ProjectionMatrix);
shader.SetVec3("u_cameraEye",
    (float)camera.Eye.X, (float)camera.Eye.Y, (float)camera.Eye.Z);
shader.SetVec3("u_radii",
    (float)radii.X, (float)radii.Y, (float)radii.Z);
shader.SetVec3("u_oneOverRadiiSquared",
    (float)Ellipsoid.Wgs84.OneOverRadiiSquared.X,
    (float)Ellipsoid.Wgs84.OneOverRadiiSquared.Y,
    (float)Ellipsoid.Wgs84.OneOverRadiiSquared.Z);
shader.SetVec3("u_lightDirectionWC", sunX, sunY, sunZ);
shader.SetVec3("u_ambientColor", 0.1f, 0.1f, 0.1f);
shader.SetVec3("u_diffuseColor", 0.8f, 0.8f, 0.8f);
shader.SetVec3("u_specularColor", 0.3f, 0.3f, 0.3f);
shader.SetFloat("u_shininess", 32.0f);
shader.SetInt("u_dayTexture", 0);
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

uniform mat4 u_modelViewProjection;

void main()
{
    gl_Position = u_modelViewProjection * vec4(a_position, 1.0);
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

uniform mat4 u_modelViewProjection;
uniform float u_logDepthC;       // Tuning constant C (typically 1.0)
uniform float u_logDepthFarPlusOne; // log(C * far + 1)

out float v_logZ;
out float v_clipW;

void main()
{
    gl_Position = u_modelViewProjection * vec4(a_position, 1.0);

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
| `UniformState` | (integrated) | Uniform setting is done via `ShaderProgram.SetXxx()` |
| `MeshBuffers` | (not wrapped) | Using `BufferObject<T>` directly |
| `VertexBufferAttribute` | `VertexAttrib` | Simplified |
| `Framebuffer` | (not yet implemented) | Future: off-screen rendering |

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

| OpenGlobe Convention | Geode Convention | Example |
|---|---|---|
| `og_modelViewPerspectiveMatrix` | `u_modelViewProjection` | MVP matrix |
| `og_modelViewMatrix` | `u_modelView` | MV matrix |
| `og_perspectiveMatrix` | `u_projection` | Projection matrix |
| `og_viewportTransformationMatrix` | `u_viewportTransform` | Viewport matrix |
| `og_cameraEye` | `u_cameraEye` | Camera position |
| `og_cameraEyeHigh` | `u_cameraEyeHigh` | RTE high part |
| `og_cameraEyeLow` | `u_cameraEyeLow` | RTE low part |
| `og_sunPosition` | `u_lightDirectionEC` / `u_lightDirectionWC` | Light direction |
| `og_diffuseSpecularAmbientShininess` | `u_ambientColor`, `u_diffuseColor`, `u_specularColor`, `u_shininess` | Split into separate uniforms for clarity |
| `og_texture0` | `u_dayTexture` | Descriptive name |
| `og_texture1` | `u_nightTexture` | Descriptive name |

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
