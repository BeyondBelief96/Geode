namespace Geode.Rendering.Textures
{
    /// <summary>
    /// One slot in the <see cref="TextureUnits"/> collection. Holds the texture
    /// and sampler the application wants bound to this unit; the actual GL
    /// binding is deferred until draw time (see <see cref="TextureUnits.Clean"/>).
    /// Book §3.6.
    /// <para>
    /// Texture and sampler are independent: a single <see cref="Texture2D"/>
    /// can be bound at multiple units with different <see cref="TextureSampler"/>s,
    /// or vice versa.
    /// </para>
    /// </summary>
    public class TextureUnit
    {
        /// <summary>The texture to bind at this unit. Null unbinds the texture.</summary>
        public Texture2D? Texture { get; set; }

        /// <summary>The sampler to bind at this unit. Null restores the texture's own sampler state.</summary>
        public TextureSampler? TextureSampler { get; set; }
    }
}
