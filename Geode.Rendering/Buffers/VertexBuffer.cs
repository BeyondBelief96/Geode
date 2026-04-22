using Silk.NET.OpenGL;
using System;

namespace Geode.Rendering.Buffers
{
    /// <summary>
    /// A GL buffer object holding vertex attribute data -- positions, normals,
    /// texture coordinates, and so on, typically packed interleaved. Book §3.5.2.
    /// <para>
    /// Distinct from <see cref="IndexBuffer"/> and <see cref="UniformBuffer"/> so
    /// that call sites (like <see cref="VertexArrayObject"/>) encode intent at
    /// the type level: you cannot accidentally pass an index buffer where a
    /// vertex buffer is expected.
    /// </para>
    /// <para>
    /// Storage is immutable (<c>glNamedBufferStorage</c>) with
    /// <c>DynamicStorageBit</c>, so the size is fixed at construction but the
    /// contents can be updated via <see cref="CopyFromSystemMemory"/>.
    /// </para>
    /// </summary>
    public sealed class VertexBuffer : IDisposable
    {
        private readonly GL _gl;
        private readonly uint _handle;

        /// <summary>The raw OpenGL buffer handle.</summary>
        public uint Handle => _handle;

        /// <summary>Size of the buffer's immutable storage in bytes.</summary>
        public int SizeInBytes { get; }

        /// <summary>
        /// Normally constructed indirectly via
        /// <see cref="Device.CreateVertexBuffer(BufferHint, int)"/> or the
        /// data-initialized overloads. <paramref name="hint"/> is advisory --
        /// immutable storage with <c>DynamicStorageBit</c> is used regardless.
        /// </summary>
        public unsafe VertexBuffer(GL gl, BufferHint hint, int sizeInBytes)
        {
            _ = hint;
            _gl = gl;
            SizeInBytes = sizeInBytes;

            _handle = _gl.CreateBuffer();
            _gl.NamedBufferStorage(
                _handle, (nuint)sizeInBytes, (void*)null,
                BufferStorageMask.DynamicStorageBit);
        }

        /// <summary>
        /// Copy data into the buffer starting at <paramref name="destinationOffsetInBytes"/>.
        /// The total byte length of <paramref name="data"/> must fit within
        /// <see cref="SizeInBytes"/> from that offset.
        /// </summary>
        public unsafe void CopyFromSystemMemory<T>(T[] data, int destinationOffsetInBytes = 0)
            where T : unmanaged
        {
            fixed (T* ptr = data)
            {
                _gl.NamedBufferSubData(
                    _handle,
                    (nint)destinationOffsetInBytes,
                    (nuint)(data.Length * sizeof(T)),
                    ptr);
            }
        }

        /// <summary>Deletes the GPU buffer.</summary>
        public void Dispose() => _gl.DeleteBuffer(_handle);
    }
}
