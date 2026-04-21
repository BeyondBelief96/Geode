namespace Geode.Rendering.Shaders.Uniforms
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
