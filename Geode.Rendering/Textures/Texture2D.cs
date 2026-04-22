using Geode.Rendering.Buffers;
using Silk.NET.OpenGL;
using System;

namespace Geode.Rendering.Textures
{
    /// <summary>
    /// A 2D texture backed by immutable storage (<c>glTextureStorage2D</c>, GL 4.2+).
    /// Sampler state -- filters, wrap modes, anisotropy -- lives on a separate
    /// <see cref="TextureSampler"/> and is bound independently via
    /// <see cref="TextureUnit"/>. Book §3.6.
    /// <para>
    /// Pixel data is uploaded after construction through one of the
    /// <c>CopyFrom*</c> methods. <c>glTextureStorage2D</c> only allocates
    /// storage; the texture is defined but its texels are undefined until
    /// written.
    /// </para>
    /// </summary>
    public class Texture2D : IDisposable
    {
        private readonly GL _gl;
        private readonly uint _handle;
        private readonly Texture2DDescription _description;

        /// <summary>The raw OpenGL texture handle.</summary>
        public uint Handle => _handle;

        /// <summary>The creation-time description -- width, height, format, mipmap policy.</summary>
        public Texture2DDescription Description => _description;

        /// <summary>
        /// Creates a 2D texture with immutable storage allocated for the full mip
        /// chain (if <see cref="Texture2DDescription.GenerateMipmaps"/>) or just
        /// the base level. Texels are undefined after construction -- upload data
        /// via <see cref="CopyFromSystemMemory"/> or <see cref="CopyFromBuffer"/>.
        /// </summary>
        /// <remarks>
        /// Normally constructed indirectly via
        /// <see cref="Device.CreateTexture2D(Texture2DDescription)"/>. The public
        /// constructor is exposed for test plumbing and edge cases where callers
        /// hold a <see cref="GL"/> without a <see cref="Device"/>.
        /// </remarks>
        public Texture2D(GL gl, Texture2DDescription description)
        {
            _gl = gl;
            _description = description;

            _handle = _gl.CreateTexture(TextureTarget.Texture2D);

            uint levels = description.GenerateMipmaps
                ? (uint)(1 + Math.Floor(Math.Log2(Math.Max(description.Width, description.Height))))
                : 1u;

            _gl.TextureStorage2D(_handle, levels,
                TextureFormatGL.ToSizedInternalFormat(description.Format),
                (uint)description.Width, (uint)description.Height);
        }

        /// <summary>
        /// Upload the base mip level from a managed byte array. Source data is
        /// interpreted per (<paramref name="format"/>, <paramref name="datatype"/>).
        /// If the description requests mipmaps, the mip chain is regenerated
        /// afterwards via <c>glGenerateTextureMipmap</c>.
        /// </summary>
        public unsafe void CopyFromSystemMemory(
            byte[] pixels, ImageFormat format, ImageDatatype datatype)
        {
            fixed (byte* ptr = pixels)
            {
                _gl.TextureSubImage2D(
                    _handle, 0, 0, 0,
                    (uint)_description.Width, (uint)_description.Height,
                    TextureFormatGL.ToPixelFormat(format),
                    TextureFormatGL.ToPixelType(datatype),
                    ptr);
            }

            if (_description.GenerateMipmaps)
                _gl.GenerateTextureMipmap(_handle);
        }

        /// <summary>
        /// Upload the base mip level from a <see cref="WritePixelBuffer"/> (PBO).
        /// The upload is kicked off asynchronously by the driver -- the CPU does
        /// not block waiting for the copy to land.
        /// </summary>
        public unsafe void CopyFromBuffer(
            WritePixelBuffer pixelBuffer, ImageFormat format, ImageDatatype datatype)
        {
            // A PBO bound to PIXEL_UNPACK_BUFFER redirects TextureSubImage2D's
            // data pointer to read from the buffer at the given byte offset
            // (null => offset 0) instead of from client memory.
            _gl.BindBuffer(BufferTargetARB.PixelUnpackBuffer, pixelBuffer.Handle);
            _gl.TextureSubImage2D(
                _handle, 0, 0, 0,
                (uint)_description.Width, (uint)_description.Height,
                TextureFormatGL.ToPixelFormat(format),
                TextureFormatGL.ToPixelType(datatype),
                (void*)0);
            _gl.BindBuffer(BufferTargetARB.PixelUnpackBuffer, 0);

            if (_description.GenerateMipmaps)
                _gl.GenerateTextureMipmap(_handle);
        }

        /// <summary>
        /// Read the base mip level back into a <see cref="ReadPixelBuffer"/> (PBO).
        /// Asynchronous -- the returned PBO becomes readable on the CPU only after
        /// a subsequent sync or map (see <see cref="ReadPixelBuffer.CopyToSystemMemory{T}(int)"/>).
        /// </summary>
        public unsafe void CopyToBuffer(
            ReadPixelBuffer pixelBuffer, ImageFormat format, ImageDatatype datatype)
        {
            _gl.BindBuffer(BufferTargetARB.PixelPackBuffer, pixelBuffer.Handle);
            _gl.GetTextureImage(
                _handle, 0,
                TextureFormatGL.ToPixelFormat(format),
                TextureFormatGL.ToPixelType(datatype),
                (uint)pixelBuffer.SizeInBytes,
                (void*)0);
            _gl.BindBuffer(BufferTargetARB.PixelPackBuffer, 0);
        }

        /// <summary>
        /// Regenerate the mip chain from the base level. Called automatically
        /// after <c>CopyFrom*</c> when <see cref="Texture2DDescription.GenerateMipmaps"/>
        /// is true; exposed here for callers that mutate the base level through
        /// other paths (e.g. framebuffer render-to-texture).
        /// </summary>
        public void GenerateMipmaps() => _gl.GenerateTextureMipmap(_handle);

        /// <summary>Deletes the GPU texture object.</summary>
        public void Dispose() => _gl.DeleteTexture(_handle);
    }
}
