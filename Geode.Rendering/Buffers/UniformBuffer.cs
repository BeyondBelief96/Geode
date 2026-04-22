using Silk.NET.OpenGL;
using System;

namespace Geode.Rendering.Buffers
{
    /// <summary>
    /// A GL buffer object used as a Uniform Buffer Object (UBO) -- shared
    /// blocks of uniform data bound to shader uniform blocks via
    /// <c>glBindBufferBase</c> / <c>glBindBufferRange</c>. Book §3.4.4.
    /// <para>
    /// Not yet consumed by the rest of the engine; created now to match the
    /// book's <see cref="Device"/> surface and to give later chapters a typed
    /// handle to pass into material / camera uniform blocks.
    /// </para>
    /// </summary>
    public sealed class UniformBuffer : IDisposable
    {
        private readonly GL _gl;
        private readonly uint _handle;

        /// <summary>The raw OpenGL buffer handle.</summary>
        public uint Handle => _handle;

        /// <summary>Size of the buffer's immutable storage in bytes.</summary>
        public int SizeInBytes { get; }

        /// <summary>
        /// Normally constructed indirectly via
        /// <see cref="Device.CreateUniformBuffer"/>. <paramref name="hint"/>
        /// is advisory.
        /// </summary>
        public unsafe UniformBuffer(GL gl, BufferHint hint, int sizeInBytes)
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
        /// Callers are responsible for matching std140 / std430 layout rules
        /// expected by the consuming shader uniform block.
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
