using Silk.NET.OpenGL;

namespace Geode.Rendering.Buffers
{
    /// <summary>
    /// Rich attribute descriptor for a Mesh-sourced interleaved VAO. Captures
    /// everything <c>glVertexArrayAttribFormat</c> needs: shader location,
    /// component count, component GL type, normalized flag, and byte offset
    /// within the interleaved vertex.
    /// </summary>
    /// <param name="Location">Shader attribute location (from <c>glGetAttribLocation</c>).</param>
    /// <param name="Components">Number of components (1-4).</param>
    /// <param name="Type">Per-component GL type (Float, HalfFloat, UnsignedByte, ...).</param>
    /// <param name="Normalized">
    /// If true, integer values are mapped to [0, 1] or [-1, 1] when read by the shader.
    /// Only meaningful for integer <see cref="Type"/>s.
    /// </param>
    /// <param name="Offset">Byte offset of this attribute inside one interleaved vertex.</param>
    public readonly record struct InterleavedVertexAttribute(
        uint Location,
        int Components,
        VertexAttribType Type,
        bool Normalized,
        uint Offset);
}
