# Geode

A 3D virtual globe rendering engine for .NET, built on Silk.NET and OpenGL 3.3.

Based on *3D Engine Design for Virtual Globes* by Patrick Cozzi & Kevin Ring.

## Packages

| Package | Description |
|---------|-------------|
| **Geode.Core** | Double-precision geodetic math: WGS84 ellipsoid, coordinate transforms, ray intersection. No GPU dependency. |
| **Geode.Rendering** | OpenGL 3.3 abstractions: shaders, vertex arrays, textures. Built on Silk.NET. |
| **Geode.Visualization** | High-level globe rendering: tessellation, camera, Phong shading, texture mapping. |

## Quick Start

```bash
dotnet add package Geode.Visualization
```

```csharp
using Geode.Core;

var seattle = new Geodetic3D(Trig.ToRadians(-122.33), Trig.ToRadians(47.61));
var cartesian = Ellipsoid.Wgs84.ToVector3D(seattle);
```

## Build from Source

```bash
git clone https://github.com/yourusername/Geode.git
cd Geode
dotnet run --project Geode.App
```

Requires .NET 9+ and a GPU with OpenGL 3.3 support.

## License

MIT
