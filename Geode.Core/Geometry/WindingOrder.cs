namespace Geode.Core.Geometry
{
    /// <summary>
    /// Vertex winding order used to determine a triangle's front face.
    /// </summary>
    /// <remarks>
    /// Lives in <see cref="Geode.Core"/> rather than the Rendering layer so that
    /// <see cref="Mesh"/> can declare <c>FrontFaceWindingOrder</c> without pulling
    /// in OpenGL. The Rendering layer translates this to
    /// <c>Silk.NET.OpenGL.FrontFaceDirection</c> at the draw-call boundary.
    /// </remarks>
    public enum WindingOrder
    {
        /// <summary>Clockwise-wound triangles are front-facing.</summary>
        Clockwise,

        /// <summary>Counter-clockwise-wound triangles are front-facing (OpenGL default).</summary>
        CounterClockwise
    }
}
