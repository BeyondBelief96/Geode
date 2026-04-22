using Silk.NET.OpenGL;

namespace Geode.Rendering.Textures
{
    /// <summary>
    /// The context's texture-unit binding table. Indexed like an array --
    /// <c>context.TextureUnits[0].Texture = tex</c> -- and flushed to GL
    /// immediately before each draw via <see cref="Clean"/>. Book §3.6.
    /// <para>
    /// Shadow-compares desired vs last-applied handles per unit so that
    /// unchanged bindings cost no GL call. Unit count is capped by
    /// <c>GL_MAX_COMBINED_TEXTURE_IMAGE_UNITS</c>, queried at construction.
    /// </para>
    /// </summary>
    public class TextureUnits
    {
        private readonly GL _gl;
        private readonly TextureUnit[] _units;
        private readonly uint[] _shadowTextures;
        private readonly uint[] _shadowSamplers;

        /// <summary>Number of texture units available on this hardware.</summary>
        public int Count => _units.Length;

        public TextureUnits(GL gl)
        {
            _gl = gl;

            // GL 4.6 guarantees >= 80 combined image units. The hardware
            // limit is typically higher (e.g. 192 on modern deskp GPUs);
            // query it so we don't pessimistically cap the caller.to
            _gl.GetInteger(GetPName.MaxCombinedTextureImageUnits, out int maxUnits);

            _units = new TextureUnit[maxUnits];
            _shadowTextures = new uint[maxUnits];
            _shadowSamplers = new uint[maxUnits];
            for (int i = 0; i < maxUnits; i++)
                _units[i] = new TextureUnit();
        }

        /// <summary>Access the unit at index <paramref name="index"/>.</summary>
        public TextureUnit this[int index] => _units[index];

        /// <summary>
        /// Flush desired bindings to GL. Called once at the top of every draw
        /// before the shader runs; issues <c>glBindTextureUnit</c> and
        /// <c>glBindSampler</c> only where the shadow indicates a change.
        /// </summary>
        internal void Clean()
        {
            for (int i = 0; i < _units.Length; i++)
            {
                uint textureHandle = _units[i].Texture?.Handle ?? 0;
                uint samplerHandle = _units[i].TextureSampler?.Handle ?? 0;

                if (textureHandle != _shadowTextures[i])
                {
                    _gl.BindTextureUnit((uint)i, textureHandle);
                    _shadowTextures[i] = textureHandle;
                }
                if (samplerHandle != _shadowSamplers[i])
                {
                    _gl.BindSampler((uint)i, samplerHandle);
                    _shadowSamplers[i] = samplerHandle;
                }
            }
        }
    }
}
