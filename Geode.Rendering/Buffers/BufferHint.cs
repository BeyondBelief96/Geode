namespace Geode.Rendering
{
    /// <summary>
    /// Hint about how often a GPU buffer's contents will change after initial upload.
    /// In OpenGL 4.6 DSA, this is translated to <c>BufferStorageMask</c> flags at
    /// allocation time (the legacy <c>BufferUsageARB</c> hint no longer applies).
    /// </summary>
    public enum BufferHint
    {
        /// <summary>Uploaded once, drawn many times (static tile geometry, static meshes).</summary>
        StaticDraw,

        /// <summary>Uploaded occasionally (morphing terrain between LODs).</summary>
        DynamicDraw,

        /// <summary>Uploaded every frame (skinned meshes, particle systems).</summary>
        StreamDraw,
    }
}
