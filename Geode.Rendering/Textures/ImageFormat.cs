namespace Geode.Rendering.Textures
{
    /// <summary>
    /// Layout of source pixel data when uploading to or reading back from a texture.
    /// Describes the channels present, not the component datatype (that is
    /// <see cref="ImageDatatype"/>). Book §3.6.
    /// </summary>
    public enum ImageFormat
    {
        StencilIndex,
        DepthComponent,
        Red,
        Green,
        Blue,
        RedGreenBlue,
        RedGreenBlueAlpha,
        BlueGreenRed,
        BlueGreenRedAlpha,
        RedGreen,
        DepthStencil,
        RedInteger,
        GreenInteger,
        BlueInteger,
        RedGreenBlueInteger,
        RedGreenBlueAlphaInteger,
        BlueGreenRedInteger,
        BlueGreenRedAlphaInteger,
        RedGreenInteger
    }
}
