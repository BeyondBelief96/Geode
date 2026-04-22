using Silk.NET.OpenGL;
using System;

namespace Geode.Rendering.Buffers
{
    /// <summary>
    /// Abstract base for pixel-transfer buffer objects (PBOs). Subclassed by
    /// <see cref="WritePixelBuffer"/> (CPU -> GPU) and <see cref="ReadPixelBuffer"/>
    /// (GPU -> CPU). A PBO is a regular GL buffer bound to the
    /// <c>PIXEL_UNPACK_BUFFER</c> or <c>PIXEL_PACK_BUFFER</c> target to
    /// redirect pixel-transfer commands through GPU memory instead of
    /// client memory. Book §3.6.
    /// </summary>
    public abstract class PixelBuffer : IDisposable
    {
        protected readonly GL _gl;
        private readonly uint _handle;
        private readonly int _sizeInBytes;

        /// <summary>The raw OpenGL buffer handle.</summary>
        public uint Handle => _handle;

        /// <summary>Size of the buffer's immutable storage in bytes.</summary>
        public int SizeInBytes => _sizeInBytes;

        protected unsafe PixelBuffer(GL gl, PixelBufferHint hint, int sizeInBytes)
        {
            _ = hint; // advisory -- immutable storage with DynamicStorageBit is used regardless.
            _gl = gl;
            _sizeInBytes = sizeInBytes;

            _handle = _gl.CreateBuffer();
            _gl.NamedBufferStorage(
                _handle,
                (nuint)sizeInBytes,
                (void*)null,
                BufferStorageMask.DynamicStorageBit);
        }

        /// <summary>Deletes the GPU buffer, releasing its video-memory allocation.</summary>
        public void Dispose() => _gl.DeleteBuffer(_handle);
    }
}
