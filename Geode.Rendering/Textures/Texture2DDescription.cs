namespace Geode.Rendering.Textures
{
    /// <summary>
    /// Immutable description of a <see cref="Texture2D"/> -- resolution, internal
    /// format, mipmap policy. Produced by the caller and handed to
    /// <see cref="Device.CreateTexture2D"/>. Exposed back from
    /// <see cref="Texture2D.Description"/> so callers can query a texture's
    /// properties without tracking them separately. Book §3.6.
    /// </summary>
    public readonly record struct Texture2DDescription(
        int Width,
        int Height,
        TextureFormat Format,
        bool GenerateMipmaps)
    {
        /// <summary>True when <see cref="Format"/> targets a color-renderable internal format.</summary>
        public bool ColorRenderable => !DepthRenderable && !DepthStencilRenderable;

        /// <summary>True when <see cref="Format"/> targets a depth-renderable internal format.</summary>
        public bool DepthRenderable =>
            Format == TextureFormat.Depth16 ||
            Format == TextureFormat.Depth24 ||
            Format == TextureFormat.Depth32f ||
            Format == TextureFormat.Depth24Stencil8 ||
            Format == TextureFormat.Depth32fStencil8;

        /// <summary>True when <see cref="Format"/> targets a combined depth-stencil format.</summary>
        public bool DepthStencilRenderable =>
            Format == TextureFormat.Depth24Stencil8 ||
            Format == TextureFormat.Depth32fStencil8;
    }
}
