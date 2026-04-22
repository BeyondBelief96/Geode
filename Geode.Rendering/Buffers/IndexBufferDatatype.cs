namespace Geode.Rendering.Buffers
{
    /// <summary>
    /// Component datatype of an <see cref="IndexBuffer"/>. Determines the
    /// <c>type</c> parameter passed to <c>glDrawElements</c> at draw time and
    /// the per-index stride of the buffer's storage. Book §3.5.2.
    /// </summary>
    public enum IndexBufferDatatype
    {
        /// <summary>16-bit unsigned integers -- covers up to 65,535 vertices.</summary>
        UnsignedShort,

        /// <summary>32-bit unsigned integers -- covers up to ~4.29 billion vertices.</summary>
        UnsignedInt
    }
}
