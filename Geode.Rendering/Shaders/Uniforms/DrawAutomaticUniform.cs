namespace Geode.Rendering.Shaders.Uniforms
{
    /// <summary>
    /// A setter invoked before every draw that pushes a scene-derived value
    /// into its captured <see cref="Uniform{T}"/>. The Value assignment flows
    /// through the dirty-list mechanism, so the GPU upload happens only if the
    /// value actually changed since the last draw.
    /// </summary>
    public abstract class DrawAutomaticUniform
    {
        /// <summary>
        /// Read from <paramref name="sceneState"/> / <paramref name="drawState"/> and write to the captured <see cref="Uniform{T}"/>. 
        /// This is invoked before every draw, so it should be efficient.
        /// </summary>
        public abstract void Set(RenderContext context, DrawState drawState, SceneState sceneState);
    }
}
