namespace Geode.Rendering.Shaders.Uniforms
{
    /// <summary>
    /// Receives "I'm dirty" notifications from owned cleanables (typically Uniforms).
    /// ShaderProgram implements this; it appends the cleanable to its dirty list
    /// so the next draw flushes the change.
    /// </summary>
    public interface ICleanableObserver
    {
        void NotifyDirty(ICleanable cleanable);
    }
}
