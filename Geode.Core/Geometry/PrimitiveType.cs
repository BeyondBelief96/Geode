namespace Geode.Core.Geometry
{
    /// <summary>
    /// Primitive topology used when interpreting a vertex stream -- how the GPU
    /// groups consecutive vertices into points, lines, or triangles.
    /// </summary>
    /// <remarks>
    /// GL-independent by design. <see cref="Geode.Core"/> must not reference
    /// <c>Silk.NET.OpenGL</c>, so <see cref="Mesh"/> stores this Core enum and the
    /// Rendering layer translates to <c>Silk.NET.OpenGL.PrimitiveType</c> at the
    /// draw-call boundary (see <c>RenderContext.ToGlPrimitiveType</c>).
    /// </remarks>
    public enum PrimitiveType
    {
        /// <summary>Each vertex is an independent point.</summary>
        Points,

        /// <summary>Each pair of vertices is an independent line segment.</summary>
        Lines,

        /// <summary>Connected line segments that close back to the first vertex.</summary>
        LineLoop,

        /// <summary>Connected line segments; each vertex after the first extends the polyline.</summary>
        LineStrip,

        /// <summary>Each triple of vertices is an independent triangle. The default for meshes.</summary>
        Triangles,

        /// <summary>Connected triangles sharing edges; each new vertex forms a triangle with the previous two.</summary>
        TriangleStrip,

        /// <summary>Connected triangles sharing the first vertex as a common hub.</summary>
        TriangleFan,

        /// <summary>Lines with adjacency info, for geometry shaders.</summary>
        LinesAdjacency,

        /// <summary>Line strip with adjacency info, for geometry shaders.</summary>
        LineStripAdjacency,

        /// <summary>Triangles with adjacency info, for geometry shaders.</summary>
        TrianglesAdjacency,

        /// <summary>Triangle strip with adjacency info, for geometry shaders.</summary>
        TriangleStripAdjacency
    }
}
