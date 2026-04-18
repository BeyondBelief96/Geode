// Geode.Rendering/VertexArrayObject.cs
//
// A Vertex Array Object that binds vertex data (VBO), index data (EBO),
// and attribute layout into a single drawable unit.
//
// Uses DSA: glCreateVertexArrays, glVertexArrayAttribFormat,
// glVertexArrayVertexBuffer, glVertexArrayElementBuffer.

using Silk.NET.OpenGL;
using System;

namespace Geode.Rendering
{
    /// <summary>
    /// A Vertex Array Object that owns a VBO and EBO and describes the vertex layout.
    /// Bind this VAO before issuing <c>glDrawElements</c>.
    /// </summary>
    public class VertexArrayObject : IDisposable
    {
        private readonly GL _gl;
        private readonly uint _handle;
        private readonly BufferObject<float> _vbo;
        private readonly BufferObject<uint> _ebo;
        private readonly int _indexCount;

        /// <summary>The raw OpenGL VAO handle.</summary>
        public uint Handle => _handle;

        /// <summary>The number of indices in the element buffer.</summary>
        public int IndexCount => _indexCount;

        /// <summary>
        /// Creates a VAO with the given vertex data, index data, and attribute layout.
        /// </summary>
        /// <param name="gl">The Silk.NET OpenGL context.</param>
        /// <param name="vertices">Interleaved vertex data (floats).</param>
        /// <param name="indices">Triangle indices (unsigned ints).</param>
        /// <param name="attributes">
        /// Attribute descriptors in order. The stride is computed automatically
        /// by summing all component counts × <c>sizeof(float)</c>.
        /// Example: <c>[VertexAttribute(0, 3), VertexAttribute(1, 3)]</c> describes
        /// a vertex with position (vec3) + color (vec3) = 6 floats = 24 bytes stride.
        /// </param>
        public VertexArrayObject(GL gl, float[] vertices, uint[] indices, params VertexAttribute[] attributes)
        {
            _gl = gl;
            _indexCount = indices.Length;

            // Upload vertex and index data to the GPU immediately
            _vbo = new BufferObject<float>(gl, vertices);
            _ebo = new BufferObject<uint>(gl, indices);

            // DSA: create the VAO without binding it to the context
            _handle = _gl.CreateVertexArray();

            // Compute stride: total floats per vertex × sizeof(float).
            // This is the byte distance from the start of one vertex to the next.
            int totalFloats = 0;
            foreach(var attrib in attributes)
            {
                totalFloats += attrib.Components;
            }
            uint stride = (uint)(totalFloats * sizeof(float));

            // Bind the VBO to binding point 0 of this VAO.
            // Offset 0 means we start reading from the beginning of the buffer.
            _gl.VertexArrayVertexBuffer(_handle, 0, _vbo.Handle, 0, stride);

            // Attach the EBO so glDrawElements knows which indices to use
            _gl.VertexArrayElementBuffer(_handle, _ebo.Handle);

            // Configure each attribute's format, binding, and enable state.
            // Offset tracks where each attribute starts within a single vertex.
            uint offset = 0;
            foreach(var attrib in attributes)
            {
                // Describe the attribute: location, component count, type, normalized, byte offset
                _gl.VertexArrayAttribFormat(
                    _handle,
                    attrib.Index,
                    attrib.Components,
                    VertexAttribType.Float,
                    false,
                    offset);

                // Associate this attribute with binding point 0 (where our VBO is bound)
                _gl.VertexArrayAttribBinding(_handle, attrib.Index, 0);

                // Enable the attribute slot in the VAO
                _gl.EnableVertexArrayAttrib(_handle, attrib.Index);

                // Advance offset to the next attribute's position within the vertex
                offset += (uint)(attrib.Components * sizeof(float));
            }
        }

        /// <summary>
        /// Deletes the VAO and its owned VBO and EBO.
        /// </summary>
        public void Dispose()
        {
            _gl.DeleteVertexArray(_handle);
            _vbo.Dispose();
            _ebo.Dispose();
        }
    }
}
