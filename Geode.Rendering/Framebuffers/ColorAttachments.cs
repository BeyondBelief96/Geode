using Geode.Rendering.Textures;

namespace Geode.Rendering.Framebuffers
{
    /// <summary>
    /// Indexer-accessed collection of color texture attachments on a
    /// <see cref="Framebuffer"/>. Slot <c>i</c> maps to
    /// <c>GL_COLOR_ATTACHMENT0 + i</c> and, in GLSL, to
    /// <c>layout(location = i) out vec4 ...</c>. Book §3.7.
    /// <para>
    /// This collection only stores the desired assignment. The actual
    /// <c>glNamedFramebufferTexture</c> calls happen on
    /// <see cref="Framebuffer.Clean"/> immediately before a draw, so that
    /// repeated writes to the same slot between draws collapse to a single
    /// GL call per actual change.
    /// </para>
    /// <para>
    /// Size is set at construction to the driver's
    /// <c>GL_MAX_COLOR_ATTACHMENTS</c> (at least 8 on GL 4.x).
    /// </para>
    /// </summary>
    public class ColorAttachments
    {
        private readonly Texture2D?[] _slots;

        /// <summary>Number of color attachment slots -- <c>GL_MAX_COLOR_ATTACHMENTS</c>.</summary>
        public int Count => _slots.Length;

        internal ColorAttachments(int count)
        {
            _slots = new Texture2D?[count];
        }

        /// <summary>
        /// Get or set the texture attached at <paramref name="index"/>. Null
        /// detaches the slot on the next <see cref="Framebuffer.Clean"/>.
        /// </summary>
        public Texture2D? this[int index]
        {
            get => _slots[index];
            set => _slots[index] = value;
        }
    }
}
