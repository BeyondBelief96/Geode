using Silk.NET.OpenGL;

namespace Geode.Rendering.Buffers
{
    /// <summary>
    /// A PBO used for GPU -> CPU pixel readback. Bound to <c>PIXEL_PACK_BUFFER</c>
    /// during <see cref="Textures.Texture2D.CopyToBuffer"/> so the driver writes
    /// the readback into this buffer instead of stalling on a synchronous fetch
    /// to client memory. Book §3.6.
    /// </summary>
    public class ReadPixelBuffer : PixelBuffer
    {
        /// <summary>
        /// Normally constructed indirectly via
        /// <see cref="Device.CreateReadPixelBuffer"/>.
        /// </summary>
        public ReadPixelBuffer(GL gl, PixelBufferHint hint, int sizeInBytes)
            : base(gl, hint, sizeInBytes)
        {
        }

        /// <summary>
        /// Copy the PBO's contents back into a managed array. Returns a new array
        /// of <typeparamref name="T"/> holding the readback bytes, reinterpreted
        /// as <typeparamref name="T"/>.
        /// </summary>
        /// <param name="sizeInBytes">Number of bytes to copy from the PBO.</param>
        /// <param name="offsetInBytes">Byte offset into the PBO. Defaults to 0.</param>
        /// <remarks>
        /// Issuing this immediately after <see cref="Textures.Texture2D.CopyToBuffer"/>
        /// will stall the CPU on the in-flight readback. For true async, insert
        /// a fence/sync point between the copy and this call (Book §3.6).
        /// </remarks>
        public unsafe T[] CopyToSystemMemory<T>(int sizeInBytes, int offsetInBytes = 0)
            where T : unmanaged
        {
            int elementCount = sizeInBytes / sizeof(T);
            T[] result = new T[elementCount];
            fixed (T* ptr = result)
            {
                _gl.GetNamedBufferSubData(
                    Handle,
                    (nint)offsetInBytes,
                    (nuint)sizeInBytes,
                    ptr);
            }
            return result;
        }
    }
}
