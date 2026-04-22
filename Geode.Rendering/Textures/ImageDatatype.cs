namespace Geode.Rendering.Textures
{
    /// <summary>
    /// Component datatype of source pixel data. Pairs with <see cref="ImageFormat"/>
    /// to fully describe a client-side pixel layout. Book §3.6.
    /// </summary>
    public enum ImageDatatype
    {
        Byte,
        UnsignedByte,
        Short,
        UnsignedShort,
        Int,
        UnsignedInt,
        HalfFloat,
        Float,
        UnsignedInt248,
        UnsignedShort565,
        UnsignedShort4444,
        UnsignedShort5551,
        UnsignedInt1010102,
        UnsignedInt8888
    }
}
