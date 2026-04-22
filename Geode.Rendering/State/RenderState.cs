// Geode.Rendering/RenderState.cs
//
// A snapshot of every GPU pipeline state needed to draw geometry:
// depth test, face culling, blending, scissor, rasterization mode, etc.
//
// Each sub-state is a small mutable class with a Clone() method so that
// RenderState itself can be deep-copied cheaply.


// Geode.Rendering/RenderState.cs
//
// A snapshot of every GPU pipeline state needed to draw geometry:
// depth test, face culling, blending, scissor, rasterization mode, etc.
//
// Each sub-state is a small mutable class with a Clone() method so that
// RenderState itself can be deep-copied cheaply.

using Geode.Core.Geometry;

namespace Geode.Rendering.State;

// ──────────────────────────────────────────────
//  Enumerations
// ──────────────────────────────────────────────

/// <summary>
/// Comparison function used by the depth test to decide whether a fragment passes.
/// Maps directly to <c>GL_NEVER</c> .. <c>GL_ALWAYS</c>.
/// </summary>
public enum DepthTestFunction
{
    /// <summary>Never passes.</summary>
    Never,
    /// <summary>Passes if the incoming depth is less than the stored depth.</summary>
    Less,
    /// <summary>Passes if the incoming depth equals the stored depth.</summary>
    Equal,
    /// <summary>Passes if the incoming depth is less than or equal to the stored depth.</summary>
    LessThanOrEqual,
    /// <summary>Passes if the incoming depth is greater than the stored depth.</summary>
    Greater,
    /// <summary>Passes if the incoming depth is not equal to the stored depth.</summary>
    NotEqual,
    /// <summary>Passes if the incoming depth is greater than or equal to the stored depth.</summary>
    GreaterThanOrEqual,
    /// <summary>Always passes.</summary>
    Always
}

/// <summary>
/// Selects which faces are culled during rasterization.
/// </summary>
public enum CullFace
{
    /// <summary>Cull front-facing triangles.</summary>
    Front,
    /// <summary>Cull back-facing triangles.</summary>
    Back,
    /// <summary>Cull both front- and back-facing triangles.</summary>
    FrontAndBack
}

/// <summary>
/// Source and destination factors for the blending equation.
/// Maps to <c>GL_ZERO</c>, <c>GL_ONE</c>, <c>GL_SRC_ALPHA</c>, etc.
/// </summary>
public enum BlendingFactor
{
    /// <summary>Factor is 0.</summary>
    Zero,
    /// <summary>Factor is 1.</summary>
    One,
    /// <summary>Factor is the source alpha.</summary>
    SourceAlpha,
    /// <summary>Factor is 1 minus the source alpha.</summary>
    OneMinusSourceAlpha,
    /// <summary>Factor is the destination alpha.</summary>
    DestinationAlpha,
    /// <summary>Factor is 1 minus the destination alpha.</summary>
    OneMinusDestinationAlpha,
    /// <summary>Factor is the source color.</summary>
    SourceColor,
    /// <summary>Factor is 1 minus the source color.</summary>
    OneMinusSourceColor,
    /// <summary>Factor is the destination color.</summary>
    DestinationColor,
    /// <summary>Factor is 1 minus the destination color.</summary>
    OneMinusDestinationColor,
}

/// <summary>
/// How primitives are rasterized: filled, wireframe, or point cloud.
/// </summary>
public enum RasterizationMode
{
    /// <summary>Filled polygons (default).</summary>
    Fill,
    /// <summary>Wireframe edges only.</summary>
    Line,
    /// <summary>Vertices only.</summary>
    Point
}

// ──────────────────────────────────────────────
//  Sub-state classes
// ──────────────────────────────────────────────

/// <summary>
/// Controls the depth test: whether it is active and which comparison function to use.
/// Default: enabled with <see cref="DepthTestFunction.Less"/>.
/// </summary>
public class DepthTest
{
    /// <summary>Whether the depth test is enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>The comparison function used by the depth test.</summary>
    public DepthTestFunction Function { get; set; } = DepthTestFunction.Less;

    public DepthTest() { }

    public DepthTest(bool enabled, DepthTestFunction function)
    {
        Enabled = enabled;
        Function = function;
    }

    /// <summary>Returns a deep copy of this instance.</summary>
    public DepthTest Clone() => new(Enabled, Function);
}

/// <summary>
/// Controls back-face (or front-face) culling.
/// Default: enabled, cull <see cref="CullFace.Back"/>, counter-clockwise front face.
/// </summary>
public class FacetCulling
{
    /// <summary>Whether face culling is enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Which face(s) to cull.</summary>
    public CullFace Face { get; set; } = CullFace.Back;

    /// <summary>Winding order that defines the front face.</summary>
    public WindingOrder FrontFaceWindingOrder { get; set; } = WindingOrder.CounterClockwise;

    public FacetCulling() { }
    public FacetCulling(bool enabled, CullFace face, WindingOrder windingOrder)
    {
        Enabled = enabled;
        Face = face;
        FrontFaceWindingOrder = windingOrder;
    }

    /// <summary>Returns a deep copy of this instance.</summary>
    public FacetCulling Clone() => new(Enabled, Face, FrontFaceWindingOrder);
}

/// <summary>
/// Controls alpha blending between the fragment output and the framebuffer.
/// Default: disabled, source alpha / one-minus-source-alpha.
/// </summary>
public class Blending
{
    /// <summary>Whether blending is enabled.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>The source blending factor.</summary>
    public BlendingFactor SourceFactor { get; set; } = BlendingFactor.SourceAlpha;

    /// <summary>The destination blending factor.</summary>
    public BlendingFactor DestinationFactor { get; set; } = BlendingFactor.OneMinusSourceAlpha;

    public Blending() { }
    public Blending(bool enabled, BlendingFactor sourceFactor, BlendingFactor destinationFactor)
    {
        Enabled = enabled;
        SourceFactor = sourceFactor;
        DestinationFactor = destinationFactor;
    }

    /// <summary>Returns a deep copy of this instance.</summary>
    public Blending Clone() => new(Enabled, SourceFactor, DestinationFactor);
}

/// <summary>
/// The near/far range that incoming depth values are mapped to.
/// Default: [0.0, 1.0].
/// </summary>
public class DepthRange
{
    /// <summary>Mapped near value. Default: 0.0.</summary>
    public double Near { get; set; } = 0.0;

    /// <summary>Mapped far value. Default: 1.0.</summary>
    public double Far { get; set; } = 1.0;

    public DepthRange() { }
    public DepthRange(double near, double far)
    {
        Near = near;
        Far = far;
    }

    /// <summary>Returns a deep copy of this instance.</summary>
    public DepthRange Clone() => new(Near, Far);
}

/// <summary>
/// Controls whether fragments can write to the depth buffer.
/// Default: enabled.
/// </summary>
public class DepthMask
{
    /// <summary>Whether depth writes are enabled.</summary>
    public bool Enabled { get; set; } = true;
    public DepthMask() { }

    public DepthMask(bool enabled)
    {
        Enabled = enabled;
    }

    /// <summary>Returns a deep copy of this instance.</summary>
    public DepthMask Clone() => new(Enabled);
}

/// <summary>
/// Per-channel write mask for color buffer writes.
/// Default: all channels enabled.
/// </summary>
public class ColorMask
{
    /// <summary>Whether the red channel is writable.</summary>
    public bool Red { get; set; } = true;

    /// <summary>Whether the green channel is writable.</summary>
    public bool Green { get; set; } = true;

    /// <summary>Whether the blue channel is writable.</summary>
    public bool Blue { get; set; } = true;

    /// <summary>Whether the alpha channel is writable.</summary>
    public bool Alpha { get; set; } = true;

    public ColorMask() { }
    public ColorMask(bool red, bool green, bool blue, bool alpha)
    {
        Red = red;
        Green = green;
        Blue = blue;
        Alpha = alpha;
    }

    /// <summary>Returns a deep copy of this instance.</summary>
    public ColorMask Clone() => new(Red, Green, Blue, Alpha);
}

/// <summary>
/// Controls the scissor test, which discards fragments outside a rectangular region.
/// Default: disabled.
/// </summary>
public class ScissorTest
{
    /// <summary>Whether the scissor test is enabled.</summary>
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

    /// <summary>Returns a deep copy of this instance.</summary>
    public ScissorTest Clone() => new(Enabled, X, Y, Width, Height);
}

// ──────────────────────────────────────────────
//  Aggregate render state
// ──────────────────────────────────────────────

/// <summary>
/// An immutable-style snapshot of the complete GPU pipeline state required to draw geometry.
/// Use <see cref="Clone"/> to create an independent copy before modifying individual sub-states.
/// </summary>
public class RenderState
{
    /// <summary>Depth test configuration.</summary>
    public DepthTest DepthTest { get; set; } = new();

    /// <summary>Face culling configuration.</summary>
    public FacetCulling FacetCulling { get; set; } = new();

    /// <summary>Alpha blending configuration.</summary>
    public Blending Blending { get; set; } = new();

    /// <summary>Depth range mapping.</summary>
    public DepthRange DepthRange { get; set; } = new();

    /// <summary>Depth write mask.</summary>
    public DepthMask DepthMask { get; set; } = new();

    /// <summary>Color channel write mask.</summary>
    public ColorMask ColorMask { get; set; } = new();

    /// <summary>Scissor test configuration.</summary>
    public ScissorTest ScissorTest { get; set; } = new();

    /// <summary>Polygon rasterization mode. Default: <see cref="RasterizationMode.Fill"/>.</summary>
    public RasterizationMode RasterizationMode { get; set; } = RasterizationMode.Fill;

    public RenderState() { }

    /// <summary>Returns a deep copy of this render state and all sub-states.</summary>
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
