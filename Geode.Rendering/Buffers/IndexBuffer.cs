using Silk.NET.OpenGL;
using System;

namespace Geode.Rendering.Buffers
{
    /// <summary>
    /// A GL buffer object holding element (index) data -- the sequence of
    /// vertex indices consumed by <c>glDrawElements</c>. Book §3.5.2.
    /// <para>
    /// The datatype (<see cref="IndexBufferDatatype.UnsignedShort"/> vs
    /// <see cref="IndexBufferDatatype.UnsignedInt"/>) is stored on the buffer
    /// itself so the VAO can pass the correct <c>type</c> to
    /// <c>glDrawElements</c> without the caller tracking it.
    /// </para>
    /// </summary>
    public sealed class IndexBuffer : IDisposable
    {
        private readonly GL _gl;
        private readonly uint _handle;

        /// <summary>The raw OpenGL buffer handle.</summary>
        public uint Handle => _handle;

        /// <summary>Size of the buffer's immutable storage in bytes.</summary>
        public int SizeInBytes { get; }

        /// <summary>Component datatype of each index -- 16-bit or 32-bit unsigned.</summary>
        public IndexBufferDatatype Datatype { get; }

        /// <summary>Number of indices held in the buffer (<see cref="SizeInBytes"/> / stride).</summary>
        public int Count => SizeInBytes / BytesPerIndex(Datatype);

        /// <summary>
        /// Normally constructed indirectly via
        /// <see cref="Device.CreateIndexBuffer(BufferHint, int, IndexBufferDatatype)"/>
        /// or the data-initialized overloads. <paramref name="hint"/> is advisory.
        /// </summary>
        public unsafe IndexBuffer(GL gl, BufferHint hint, int sizeInBytes, IndexBufferDatatype datatype)
        {
            _ = hint;
            _gl = gl;
            SizeInBytes = sizeInBytes;
            Datatype = datatype;

            _handle = _gl.CreateBuffer();
            _gl.NamedBufferStorage(
                _handle, (nuint)sizeInBytes, (void*)null,
                BufferStorageMask.DynamicStorageBit);
        }

        /// <summary>
        /// Copy index data into the buffer starting at <paramref name="destinationOffsetInBytes"/>.
        /// The element type of <typeparamref name="T"/> must match <see cref="Datatype"/>
        /// (<c>ushort</c> or <c>uint</c>); callers get no compile-time check but a
        /// size mismatch will trip at runtime.
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

        /// <summary>The <see cref="DrawElementsType"/> value the VAO passes to <c>glDrawElements</c>.</summary>
        internal DrawElementsType GlDrawElementsType => Datatype switch
        {
            IndexBufferDatatype.UnsignedShort => DrawElementsType.UnsignedShort,
            IndexBufferDatatype.UnsignedInt   => DrawElementsType.UnsignedInt,
            _ => DrawElementsType.UnsignedInt
        };

        private static int BytesPerIndex(IndexBufferDatatype datatype) => datatype switch
        {
            IndexBufferDatatype.UnsignedShort => 2,
            IndexBufferDatatype.UnsignedInt   => 4,
            _ => 4
        };

        /// <summary>Deletes the GPU buffer.</summary>
        public void Dispose() => _gl.DeleteBuffer(_handle);
    }
}
