// Geode.Rendering/ClearState.cs
//
// Describes which framebuffer attachments to clear and the values to clear them to.
// Consumed by the rendering context before each draw pass.

using System;
using System.Numerics;

namespace Geode.Rendering
{
    /// <summary>
    /// Flags that select which framebuffer attachments are cleared.
    /// </summary>
    [Flags]
    public enum ClearBuffers
    {
        /// <summary>Clear the color attachment.</summary>
        ColorBuffer = 1 << 0,

        /// <summary>Clear the depth attachment.</summary>
        DepthBuffer = 1 << 1,

        /// <summary>Clear the stencil attachment.</summary>
        StencilBuffer = 1 << 2,

        /// <summary>Clear both color and depth attachments.</summary>
        ColorAndDepthBuffer = ColorBuffer | DepthBuffer,

        /// <summary>Clear color, depth, and stencil attachments.</summary>
        All = ColorBuffer | DepthBuffer | StencilBuffer
    }

    /// <summary>
    /// Encapsulates every parameter needed for a framebuffer clear operation:
    /// which buffers to clear, the clear values, and the per-channel write masks.
    /// </summary>
    public class ClearState
    {
        /// <summary>Which framebuffer attachments to clear. Default: color and depth.</summary>
        public ClearBuffers Buffers { get; set; } = ClearBuffers.ColorAndDepthBuffer;

        /// <summary>The RGBA clear color. Default: opaque black (0, 0, 0, 1).</summary>
        public Vector4 Color { get; set; } = new(0f, 0f, 0f, 1f);

        /// <summary>The depth clear value. Default: 1.0 (far plane).</summary>
        public float Depth { get; set; } = 1f;

        /// <summary>The stencil clear value. Default: 0.</summary>
        public int Stencil { get; set; } = 0;

        /// <summary>Per-channel write mask applied during the clear. Default: all channels enabled.</summary>
        public ColorMask ColorMask { get; set; } = new();

        /// <summary>Whether depth writes are enabled during the clear. Default: true.</summary>
        public bool DepthMask { get; set; } = true;

        /// <summary>Returns a <see cref="ClearState"/> with all default values.</summary>
        public static ClearState Default => new();
    }
}
