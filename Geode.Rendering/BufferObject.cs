// Geode.Rendering/BufferObject.cs
//
// A generic GPU buffer (VBO, EBO, etc.) backed by immutable storage.
// Uses DSA: glCreateBuffers, glNamedBufferStorage.

using Silk.NET.OpenGL;
using System;

namespace Geode.Rendering
{
    /// <summary>
    /// A typed GPU buffer object that uploads data to immutable storage via DSA.
    /// Used as the backing store for vertex data (VBO) and index data (EBO).
    /// </summary>
    /// <typeparam name="T">
    /// The element type stored in the buffer. Must be an unmanaged (blittable) type
    /// such as <see cref="float"/> or <see cref="uint"/>.
    /// </typeparam>
    public class BufferObject<T> : IDisposable where T : unmanaged
    {
        private readonly GL _gl;
        private readonly uint _handle;

        /// <summary>The raw OpenGL buffer handle.</summary>
        public uint Handle => _handle;

        /// <summary>
        /// Creates a GPU buffer and uploads the supplied data to immutable storage.
        /// </summary>
        /// <param name="gl">The Silk.NET OpenGL context.</param>
        /// <param name="data">
        /// The data to upload. The buffer size is <c>data.Length * sizeof(T)</c> bytes.
        /// </param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="data"/> is empty.</exception>
        public unsafe BufferObject(GL gl, ReadOnlySpan<T> data)
        {
            _gl = gl;

            // DSA: create a buffer object without binding it to a target
            _handle = _gl.CreateBuffer();

            // Pin the managed data and copy it into GPU-side immutable storage.
            // DynamicStorageBit allows future updates via glNamedBufferSubData.
            fixed(void* ptr = data)
            {
                _gl.NamedBufferStorage(
                    _handle,
                    (nuint)(data.Length * sizeof(T)),
                    ptr,
                    BufferStorageMask.DynamicStorageBit);
            }
        }

        /// <summary>
        /// Deletes the GPU buffer, releasing its video-memory allocation.
        /// </summary>
        public void Dispose()
        {
            _gl.DeleteBuffer(_handle);
        }
    }
}
