using Geode.Rendering.Buffers;
using Geode.Rendering.Shaders;
using Geode.Rendering.Textures;
using Silk.NET.OpenGL;
using StbImageSharp;
using System.IO;

namespace Geode.Rendering
{
    /// <summary>
    /// Stateless factory for context-shareable GPU resources: buffers, textures,
    /// samplers, pixel buffers, and shader programs. Mirrors the book's
    /// <c>Device</c> class (§3.4). Constructed independently of
    /// <see cref="RenderContext"/> -- both are siblings over the same
    /// <see cref="GL"/> instance, and neither owns the other.
    /// <para>
    /// Resources created here wrap GL objects that OpenGL permits to be shared
    /// across contexts via share groups: buffers, textures, samplers, shader
    /// programs. Context-bound objects (VAOs, framebuffers) stay on
    /// <see cref="RenderContext"/> where their lifetime is naturally scoped.
    /// </para>
    /// <para>
    /// The book's <c>Device</c> is a static class because OpenTK exposes GL as
    /// a process-global. Silk.NET ties <see cref="GL"/> to a specific window /
    /// share group, so this <c>Device</c> is instance-scoped -- one per GL.
    /// Window creation stays static (<see cref="CreateWindow"/>) because the
    /// window bootstraps the GL in the first place.
    /// </para>
    /// </summary>
    public class Device
    {
        private readonly GL _gl;

        /// <summary>
        /// Maximum number of vertex attributes the driver supports
        /// (<c>GL_MAX_VERTEX_ATTRIBS</c>). At least 16 on any conformant GL 4.x.
        /// </summary>
        public int MaximumNumberOfVertexAttributes { get; }

        /// <summary>
        /// Number of combined texture image units across all shader stages
        /// (<c>GL_MAX_COMBINED_TEXTURE_IMAGE_UNITS</c>). At least 80 on GL 4.6.
        /// </summary>
        public int NumberOfTextureUnits { get; }

        /// <summary>
        /// Maximum number of color attachments on a single framebuffer
        /// (<c>GL_MAX_COLOR_ATTACHMENTS</c>). At least 8 on GL 4.x.
        /// </summary>
        public int MaximumNumberOfColorAttachments { get; }

        public Device(GL gl)
        {
            _gl = gl;

            _gl.GetInteger(GetPName.MaxVertexAttribs, out int maxVertexAttribs);
            _gl.GetInteger(GetPName.MaxCombinedTextureImageUnits, out int maxTextureUnits);
            _gl.GetInteger(GetPName.MaxColorAttachments, out int maxColorAttachments);

            MaximumNumberOfVertexAttributes = maxVertexAttribs;
            NumberOfTextureUnits = maxTextureUnits;
            MaximumNumberOfColorAttachments = maxColorAttachments;
        }

        #region Window

        /// <summary>
        /// Create a window and its associated GL context, <see cref="Device"/>,
        /// and <see cref="RenderContext"/>. Static because the GL doesn't exist
        /// until the window does. Book §2.
        /// </summary>
        public static GraphicsWindow CreateWindow(int width, int height, string title)
            => new GraphicsWindow(width, height, title, WindowType.Default);

        /// <summary>
        /// Create a window of the given <see cref="WindowType"/> (windowed or
        /// full-screen).
        /// </summary>
        public static GraphicsWindow CreateWindow(int width, int height, string title, WindowType windowType)
            => new GraphicsWindow(width, height, title, windowType);

        #endregion

        #region Buffers

        /// <summary>Create an empty vertex buffer of <paramref name="sizeInBytes"/>.</summary>
        public VertexBuffer CreateVertexBuffer(BufferHint hint, int sizeInBytes)
            => new VertexBuffer(_gl, hint, sizeInBytes);

        /// <summary>Create a vertex buffer sized to <paramref name="data"/> and upload it.</summary>
        public VertexBuffer CreateVertexBuffer<T>(BufferHint hint, T[] data)
            where T : unmanaged
        {
            VertexBuffer buffer;
            unsafe { buffer = new VertexBuffer(_gl, hint, data.Length * sizeof(T)); }
            buffer.CopyFromSystemMemory(data);
            return buffer;
        }

        /// <summary>Create an empty index buffer of <paramref name="sizeInBytes"/>.</summary>
        public IndexBuffer CreateIndexBuffer(BufferHint hint, int sizeInBytes, IndexBufferDatatype datatype)
            => new IndexBuffer(_gl, hint, sizeInBytes, datatype);

        /// <summary>Create a <see cref="IndexBufferDatatype.UnsignedShort"/> index buffer from <paramref name="indices"/>.</summary>
        public IndexBuffer CreateIndexBuffer(BufferHint hint, ushort[] indices)
        {
            var buffer = new IndexBuffer(_gl, hint, indices.Length * sizeof(ushort),
                IndexBufferDatatype.UnsignedShort);
            buffer.CopyFromSystemMemory(indices);
            return buffer;
        }

        /// <summary>Create a <see cref="IndexBufferDatatype.UnsignedInt"/> index buffer from <paramref name="indices"/>.</summary>
        public IndexBuffer CreateIndexBuffer(BufferHint hint, uint[] indices)
        {
            var buffer = new IndexBuffer(_gl, hint, indices.Length * sizeof(uint),
                IndexBufferDatatype.UnsignedInt);
            buffer.CopyFromSystemMemory(indices);
            return buffer;
        }

        /// <summary>Create an empty uniform buffer of <paramref name="sizeInBytes"/>.</summary>
        public UniformBuffer CreateUniformBuffer(BufferHint hint, int sizeInBytes)
            => new UniformBuffer(_gl, hint, sizeInBytes);

        #endregion

        #region Shaders

        /// <summary>
        /// Compile and link a shader program from GLSL source strings. Returns
        /// a fresh <see cref="ShaderProgram"/> the caller owns; for cached
        /// programs keyed by a logical name, use
        /// <see cref="RenderContext.Shaders"/>.
        /// </summary>
        public ShaderProgram CreateShaderProgram(string vertexSource, string fragmentSource)
            => new ShaderProgram(_gl, vertexSource, fragmentSource);

        /// <summary>
        /// Compile and link a shader program from GLSL source files on disk.
        /// </summary>
        public ShaderProgram CreateShaderProgramFromFiles(string vertexPath, string fragmentPath)
            => ShaderProgram.FromFiles(_gl, vertexPath, fragmentPath);

        #endregion

        #region Textures and samplers

        /// <summary>
        /// Create a texture with immutable storage allocated but no texels
        /// written. Upload data via <see cref="Texture2D.CopyFromSystemMemory"/>
        /// or <see cref="Texture2D.CopyFromBuffer"/>.
        /// </summary>
        public Texture2D CreateTexture2D(Texture2DDescription description)
            => new Texture2D(_gl, description);

        /// <summary>
        /// Load an image from disk (any format supported by stb_image), decode
        /// to RGBA8, and return a ready-to-sample <see cref="Texture2D"/>.
        /// </summary>
        /// <param name="path">Absolute or working-directory-relative path to the image file.</param>
        /// <param name="format">
        /// Internal format to allocate. Typically <see cref="TextureFormat.RedGreenBlueAlpha8"/>
        /// or <see cref="TextureFormat.SRedGreenBlue8Alpha8"/> for sRGB color textures.
        /// </param>
        /// <param name="generateMipmaps">If true, allocate a full mip chain and generate it from the base level.</param>
        public Texture2D CreateTexture2DFromFile(
            string path, TextureFormat format, bool generateMipmaps)
        {
            using FileStream stream = File.OpenRead(path);
            ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            Texture2D texture = new Texture2D(_gl,
                new Texture2DDescription(image.Width, image.Height, format, generateMipmaps));
            texture.CopyFromSystemMemory(image.Data,
                ImageFormat.RedGreenBlueAlpha, ImageDatatype.UnsignedByte);
            return texture;
        }

        /// <summary>Create a sampler with the given filter/wrap/anisotropy state.</summary>
        public TextureSampler CreateTextureSampler(TextureSamplerDescription description)
            => new TextureSampler(_gl, description);

        #endregion

        #region Pixel buffers

        /// <summary>
        /// Create a <see cref="WritePixelBuffer"/> (CPU -> GPU PBO) with
        /// <paramref name="sizeInBytes"/> of immutable storage.
        /// </summary>
        public WritePixelBuffer CreateWritePixelBuffer(PixelBufferHint hint, int sizeInBytes)
            => new WritePixelBuffer(_gl, hint, sizeInBytes);

        /// <summary>
        /// Create a <see cref="ReadPixelBuffer"/> (GPU -> CPU PBO) with
        /// <paramref name="sizeInBytes"/> of immutable storage.
        /// </summary>
        public ReadPixelBuffer CreateReadPixelBuffer(PixelBufferHint hint, int sizeInBytes)
            => new ReadPixelBuffer(_gl, hint, sizeInBytes);

        #endregion
    }
}
