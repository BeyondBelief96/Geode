using Silk.NET.OpenGL;
using System;

namespace Geode.Rendering.Buffers
{
    /// <summary>
    /// A Vertex Array Object that owns a <see cref="VertexBuffer"/> and
    /// (optionally) an <see cref="IndexBuffer"/>, and describes the vertex
    /// layout. Bind this VAO before issuing a draw. Book §3.5.2.
    /// </summary>
    public class VertexArrayObject : IDisposable
    {
        private readonly GL _gl;
        private readonly uint _handle;
        private readonly VertexBuffer _vbo;
        private readonly IndexBuffer? _ebo;

        /// <summary>The raw OpenGL VAO handle.</summary>
        public uint Handle => _handle;

        /// <summary>The number of indices in the element buffer (0 if non-indexed).</summary>
        public int IndexCount => _ebo?.Count ?? 0;

        /// <summary>
        /// GL component type of the element buffer's indices. Feeds the
        /// <c>glDrawElements</c> <c>type</c> parameter. Undefined when
        /// <see cref="IndexCount"/> is 0.
        /// </summary>
        public DrawElementsType IndexType => _ebo?.GlDrawElementsType ?? DrawElementsType.UnsignedInt;

        /// <summary>
        /// Hand-built path used by the §20 triangle demo. Allocates a
        /// <see cref="VertexBuffer"/> from <paramref name="vertices"/> and an
        /// <see cref="IndexBuffer"/> from <paramref name="indices"/>, then
        /// wires float attributes with the given per-attribute component
        /// counts at binding 0. For Mesh-sourced geometry, use
        /// <see cref="RenderContext.CreateVertexArray"/> instead.
        /// </summary>
        public VertexArrayObject(GL gl, float[] vertices, uint[] indices, params VertexAttrib[] attributes)
        {
            _gl = gl;

            _vbo = new VertexBuffer(gl, BufferHint.StaticDraw, vertices.Length * sizeof(float));
            _vbo.CopyFromSystemMemory(vertices);

            _ebo = new IndexBuffer(gl, BufferHint.StaticDraw,
                indices.Length * sizeof(uint), IndexBufferDatatype.UnsignedInt);
            _ebo.CopyFromSystemMemory(indices);

            _handle = _gl.CreateVertexArray();

            int totalFloats = 0;
            foreach (var attrib in attributes)
                totalFloats += attrib.Components;
            uint stride = (uint)(totalFloats * sizeof(float));

            _gl.VertexArrayVertexBuffer(_handle, 0, _vbo.Handle, 0, stride);
            _gl.VertexArrayElementBuffer(_handle, _ebo.Handle);

            uint offset = 0;
            foreach (var attrib in attributes)
            {
                _gl.VertexArrayAttribFormat(
                    _handle,
                    attrib.Index,
                    attrib.Components,
                    VertexAttribType.Float,
                    false,
                    offset);

                _gl.VertexArrayAttribBinding(_handle, attrib.Index, 0);
                _gl.EnableVertexArrayAttrib(_handle, attrib.Index);

                offset += (uint)(attrib.Components * sizeof(float));
            }
        }

        /// <summary>
        /// Mesh-bridge constructor. Adopts an already-populated
        /// <see cref="VertexBuffer"/> and an optional <see cref="IndexBuffer"/>,
        /// then wires attribute formats to shader locations via DSA.
        /// </summary>
        /// <param name="gl">The Silk.NET OpenGL context.</param>
        /// <param name="vertexBuffer">Interleaved vertex data. Ownership is transferred to this VAO.</param>
        /// <param name="stride">Byte distance between consecutive vertices in <paramref name="vertexBuffer"/>.</param>
        /// <param name="indexBuffer">
        /// Optional element buffer. Pass <c>null</c> for non-indexed geometry.
        /// Ownership is transferred.
        /// </param>
        /// <param name="attributes">Per-attribute format + binding-offset descriptors.</param>
        public VertexArrayObject(
            GL gl,
            VertexBuffer vertexBuffer,
            uint stride,
            IndexBuffer? indexBuffer,
            InterleavedVertexAttribute[] attributes)
        {
            _gl = gl;
            _vbo = vertexBuffer;
            _ebo = indexBuffer;

            _handle = _gl.CreateVertexArray();

            _gl.VertexArrayVertexBuffer(_handle, 0, _vbo.Handle, 0, stride);
            if (_ebo != null)
                _gl.VertexArrayElementBuffer(_handle, _ebo.Handle);

            foreach (InterleavedVertexAttribute a in attributes)
            {
                _gl.VertexArrayAttribFormat(
                    _handle,
                    a.Location,
                    a.Components,
                    a.Type,
                    a.Normalized,
                    a.Offset);

                _gl.VertexArrayAttribBinding(_handle, a.Location, 0);
                _gl.EnableVertexArrayAttrib(_handle, a.Location);
            }
        }

        /// <summary>
        /// Deletes the VAO and disposes the vertex and (if present) index buffers it owns.
        /// </summary>
        public void Dispose()
        {
            _gl.DeleteVertexArray(_handle);
            _vbo.Dispose();
            _ebo?.Dispose();
        }
    }
}
