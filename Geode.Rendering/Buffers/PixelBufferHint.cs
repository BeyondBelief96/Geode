namespace Geode.Rendering.Buffers
{
    /// <summary>
    /// Advisory usage hint for a <see cref="PixelBuffer"/>. Currently ignored
    /// by the implementation -- immutable storage (<c>glNamedBufferStorage</c>)
    /// with <c>DynamicStorageBit</c> is used regardless. Retained to preserve
    /// the book's API surface and to give the driver a hook for future mapped
    /// persistent-storage paths. Book §3.6.
    /// </summary>
    public enum PixelBufferHint
    {
        Stream,
        Static,
        Dynamic
    }
}
