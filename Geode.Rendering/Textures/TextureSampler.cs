using Silk.NET.OpenGL;
using System;

namespace Geode.Rendering.Textures
{
    /// <summary>
    /// Filter used when a texel covers less than one pixel (i.e. the texture
    /// is sampled at a smaller scale than it was authored).
    /// </summary>
    public enum TextureMinificationFilter
    {
        Nearest,
        Linear,
        NearestMipmapNearest,
        LinearMipmapNearest,
        NearestMipmapLinear,
        LinearMipmapLinear,
    }

    /// <summary>
    /// Filter used when a texel covers more than one pixel (i.e. the texture
    /// is sampled at a larger scale than it was authored).
    /// </summary>
    public enum TextureMagnificationFilter
    {
        Nearest,
        Linear
    }

    /// <summary>
    /// Behavior when a sample coordinate falls outside [0, 1].
    /// </summary>
    public enum TextureWrap
    {
        Clamp,
        Repeat,
        MirroredRepeat
    }

    /// <summary>
    /// An immutable sampler object -- filters, wrap modes, anisotropy --
    /// decoupled from any specific texture. Bound to a texture unit alongside
    /// a <see cref="Texture2D"/>; the combination determines how the shader
    /// samples the texture. Book §3.6.
    /// <para>
    /// Decoupling mirrors GL 3.3+ (<c>glCreateSamplers</c>) and Vulkan/D3D12:
    /// the same texture can be sampled with different filter/wrap settings
    /// by binding different sampler objects at different units.
    /// </para>
    /// </summary>
    public class TextureSampler : IDisposable
    {
        private readonly GL _gl;
        private readonly uint _handle;
        private readonly TextureSamplerDescription _description;

        /// <summary>The raw OpenGL sampler handle.</summary>
        public uint Handle => _handle;

        /// <summary>The creation-time description.</summary>
        public TextureSamplerDescription Description => _description;

        /// <summary>
        /// Normally constructed indirectly via
        /// <see cref="Device.CreateTextureSampler"/>.
        /// </summary>
        public TextureSampler(GL gl, TextureSamplerDescription description)
        {
            _gl = gl;
            _description = description;

            _handle = _gl.CreateSampler();

            _gl.SamplerParameter(_handle, SamplerParameterI.MinFilter,
                (int)ToGlMinFilter(description.MinificationFilter));
            _gl.SamplerParameter(_handle, SamplerParameterI.MagFilter,
                (int)ToGlMagFilter(description.MagnificationFilter));
            _gl.SamplerParameter(_handle, SamplerParameterI.WrapS,
                (int)ToGlWrap(description.WrapS));
            _gl.SamplerParameter(_handle, SamplerParameterI.WrapT,
                (int)ToGlWrap(description.WrapT));

            if (description.MaximumAnisotropy > 1.0f)
            {
                // GL_TEXTURE_MAX_ANISOTROPY became core in 4.6; prior versions
                // used EXT_texture_filter_anisotropic. Silk.NET exposes it as
                // a GLEnum; the driver clamps silently if the requested value
                // exceeds GL_MAX_TEXTURE_MAX_ANISOTROPY.
                _gl.SamplerParameter(_handle, (SamplerParameterF)GLEnum.TextureMaxAnisotropy,
                    description.MaximumAnisotropy);
            }
        }

        private static GLEnum ToGlMinFilter(TextureMinificationFilter f) => f switch
        {
            TextureMinificationFilter.Nearest              => GLEnum.Nearest,
            TextureMinificationFilter.Linear               => GLEnum.Linear,
            TextureMinificationFilter.NearestMipmapNearest => GLEnum.NearestMipmapNearest,
            TextureMinificationFilter.LinearMipmapNearest  => GLEnum.LinearMipmapNearest,
            TextureMinificationFilter.NearestMipmapLinear  => GLEnum.NearestMipmapLinear,
            TextureMinificationFilter.LinearMipmapLinear   => GLEnum.LinearMipmapLinear,
            _ => GLEnum.NearestMipmapLinear
        };

        private static GLEnum ToGlMagFilter(TextureMagnificationFilter f) => f switch
        {
            TextureMagnificationFilter.Nearest => GLEnum.Nearest,
            TextureMagnificationFilter.Linear  => GLEnum.Linear,
            _ => GLEnum.Linear
        };

        private static GLEnum ToGlWrap(TextureWrap w) => w switch
        {
            TextureWrap.Clamp          => GLEnum.ClampToEdge,
            TextureWrap.Repeat         => GLEnum.Repeat,
            TextureWrap.MirroredRepeat => GLEnum.MirroredRepeat,
            _ => GLEnum.Repeat
        };

        /// <summary>Deletes the GPU sampler object.</summary>
        public void Dispose() => _gl.DeleteSampler(_handle);
    }
}
