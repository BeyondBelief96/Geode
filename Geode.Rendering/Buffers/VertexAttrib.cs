namespace Geode.Rendering
{
    /// <summary>
    /// Describes a single vertex attribute within an interleaved vertex layout.
    /// </summary>
    /// <param name="Index">
    /// The attribute location index as declared in the vertex shader
    /// (e.g., <c>layout(location = 0)</c>).
    /// </param>
    /// <param name="Components">
    /// The number of components (1–4). For example, a <c>vec3</c> has 3 components.
    /// </param>
    public readonly record struct VertexAttrib(uint Index, int Components);
}
