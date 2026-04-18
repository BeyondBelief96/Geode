// Geode.Rendering/Texture2D.cs
//
// A 2D texture with automatic mipmap storage allocation and generation.
//
// Uses DSA: glCreateTextures, glTextureStorage2D, glTextureSubImage2D,
// glTextureParameterI, glGenerateTextureMipmap, glBindTextureUnit.

using Silk.NET.OpenGL;
using StbImageSharp;
using System;

namespace Geode.Rendering
{
    /// <summary>
    /// A 2D texture backed by immutable storage (<c>glTextureStorage2D</c>) with
    /// a full mipmap chain. Pixel data is uploaded once at construction time.
    /// </summary>
    public class Texture2D : IDisposable
    {
        private readonly GL _gl;
        private readonly uint _handle;

        /// <summary>The raw OpenGL texture handle.</summary>
        public uint Handle => _handle;

        /// <summary>Texture width in texels.</summary>
        public int Width { get; }

        /// <summary>Texture height in texels.</summary>
        public int Height { get; }

        /// <summary>
        /// Creates a 2D texture, uploads pixel data, configures sampling, and generates mipmaps.
        /// </summary>
        /// <param name="gl">The Silk.NET OpenGL context.</param>
        /// <param name="width">Width of the base mip level in texels.</param>
        /// <param name="height">Height of the base mip level in texels.</param>
        /// <param name="pixels">
        /// Raw pixel data in RGBA8 format (4 bytes per texel, row-major, bottom-left origin).
        /// </param>
        public unsafe Texture2D(GL gl, int width, int height, byte[] pixels)
        {
            _gl = gl;
            Width = width;
            Height = height;

            // DSA: create a texture object without binding it to a target
            _handle = _gl.CreateTexture(TextureTarget.Texture2D);

            // Calculate the full mipmap chain depth: floor(log2(max(w,h))) + 1
            int levels = 1 + (int)Math.Floor(Math.Log2(Math.Max(width, height)));

            // Allocate immutable storage for all mip levels at once
            _gl.TextureStorage2D(_handle, (uint)levels, SizedInternalFormat.Rgba8, (uint)width, (uint)height);

            // Upload the base mip level (level 0) pixel data
            fixed(byte* ptr = pixels)
            {
                _gl.TextureSubImage2D(_handle, 0, 0, 0, (uint)width, (uint)height, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            }

            // Configure texture sampling parameters
            _gl.TextureParameterI(_handle, TextureParameterName.TextureMinFilter,
                (int)GLEnum.LinearMipmapLinear);   // Trilinear filtering for minification

            _gl.TextureParameterI(_handle, TextureParameterName.TextureMagFilter,
                (int)GLEnum.Linear);               // Bilinear filtering for magnification

            _gl.TextureParameterI(_handle, TextureParameterName.TextureWrapS,
                (int)GLEnum.Repeat);               // Horizontal wrap: repeat

            _gl.TextureParameterI(_handle, TextureParameterName.TextureWrapT,
                (int)GLEnum.ClampToEdge);          // Vertical wrap: clamp to edge

            // Generate all mip levels from the base level data
            _gl.GenerateTextureMipmap(_handle);
        }

        /// <summary>
        /// Binds this texture to the given texture unit for sampling in shaders.
        /// </summary>
        /// <param name="unit">The texture unit index (e.g., 0 for <c>GL_TEXTURE0</c>).</param>
        public void Bind(uint unit)
        {
            _gl.BindTextureUnit(unit, _handle);
        }

        /// <summary>
        /// Loads an image from disk (any format supported by stb_image) and creates a texture.
        /// The image is converted to RGBA regardless of its source format.
        /// </summary>
        /// <param name="gl">The Silk.NET OpenGL context.</param>
        /// <param name="path">File path to the image.</param>
        /// <returns>A new <see cref="Texture2D"/> with the image data uploaded.</returns>
        public static Texture2D FromFile(GL gl, string path)
        {
            using var stream = System.IO.File.OpenRead(path);
            var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            return new Texture2D(gl, image.Width, image.Height, image.Data);
        }

        /// <summary>Deletes the GPU texture object.</summary>
        public void Dispose()
        {
            _gl.DeleteTexture(_handle);
        }
    }
}
