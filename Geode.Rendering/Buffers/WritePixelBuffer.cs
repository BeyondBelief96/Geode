using Silk.NET.OpenGL;

namespace Geode.Rendering.Buffers
{
    /// <summary>
    /// A PBO used for CPU -> GPU pixel uploads. Bound to <c>PIXEL_UNPACK_BUFFER</c>
    /// during <see cref="Textures.Texture2D.CopyFromBuffer"/> to redirect the
    /// upload's data source from client memory to this buffer. Book §3.6.
    /// <para>
    /// Compared to <see cref="Textures.Texture2D.CopyFromSystemMemory"/>, the
    /// PBO path lets the driver schedule DMA asynchronously while the CPU moves
    /// on -- useful for tile streaming and large texture updates.
    /// </para>
    /// </summary>
    public class WritePixelBuffer : PixelBuffer
    {
        /// <summary>
        /// Normally constructed indirectly via
        /// <see cref="Device.CreateWritePixelBuffer"/>.
        /// </summary>
        public WritePixelBuffer(GL gl, PixelBufferHint hint, int sizeInBytes)
            : base(gl, hint, sizeInBytes)
        {
        }

        /// <summary>
        /// Copy data from a managed array into this PBO. Uploads are written to
        /// the buffer starting at <paramref name="offsetInBytes"/>.
        /// </summary>
        /// <typeparam name="T">Unmanaged element type.</typeparam>
        /// <param name="data">Source data. Its total byte length must fit in <see cref="PixelBuffer.SizeInBytes"/> starting from <paramref name="offsetInBytes"/>.</param>
        /// <param name="offsetInBytes">Byte offset into the PBO to start writing. Defaults to 0.</param>
        public unsafe void CopyFromSystemMemory<T>(T[] data, int offsetInBytes = 0)
            where T : unmanaged
        {
            fixed (T* ptr = data)
            {
                _gl.NamedBufferSubData(
                    Handle,
                    (nint)offsetInBytes,
                    (nuint)(data.Length * sizeof(T)),
                    ptr);
            }
        }
    }
}
