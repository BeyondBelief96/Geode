namespace Geode.Rendering.Textures
{
    /// <summary>
    /// Immutable description of a <see cref="TextureSampler"/> -- filters, wrap modes,
    /// anisotropy. Book §3.6.
    /// </summary>
    /// <param name="MinificationFilter">Filter used when a texel covers less than one pixel.</param>
    /// <param name="MagnificationFilter">Filter used when a texel covers more than one pixel.</param>
    /// <param name="WrapS">Wrap behavior along the S (horizontal) axis.</param>
    /// <param name="WrapT">Wrap behavior along the T (vertical) axis.</param>
    /// <param name="MaximumAnisotropy">
    /// Max anisotropic samples per fragment. 1.0 disables anisotropy; typical
    /// values are 2, 4, 8, 16. Driver-clamped to the hardware limit.
    /// </param>
    public readonly record struct TextureSamplerDescription(
        TextureMinificationFilter MinificationFilter,
        TextureMagnificationFilter MagnificationFilter,
        TextureWrap WrapS,
        TextureWrap WrapT,
        float MaximumAnisotropy = 1.0f);
}
