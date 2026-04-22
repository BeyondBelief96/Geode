using Geode.Core;
using Geode.Core.Geometry;
using Geode.Rendering.Buffers;
using Geode.Rendering.Shaders;
using Geode.Rendering.State;
using Geode.Rendering.Textures;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using GlPrimitiveType = Silk.NET.OpenGL.PrimitiveType;
using PrimitiveType = Geode.Core.Geometry.PrimitiveType;

namespace Geode.Rendering
{
    public class RenderContext : IDisposable
    {
        private readonly GL _gl;
        private RenderState _shadowState;

        /// <summary>
        /// Process-wide cache of compiled shader programs keyed by application-chosen
        /// strings. Owns every program it hands out -- callers Release when done,
        /// and Dispose(this) disposes any still-cached programs.
        /// See <see cref="Shaders.ShaderCache"/> for the model and Book Section 3.4.6.
        /// </summary>
        public ShaderCache Shaders { get; }

        /// <summary>
        /// Texture-unit binding table. Callers assign textures/samplers to units
        /// via <c>TextureUnits[i].Texture = ...</c>; bindings are flushed to GL
        /// once per draw. See <see cref="Textures.TextureUnits"/> and Book §3.6.
        /// </summary>
        public TextureUnits TextureUnits { get; }

        public RenderContext(GL gl)
        {
            _gl = gl;

            _gl.Enable(EnableCap.DebugOutput);
            _gl.Enable(EnableCap.DebugOutputSynchronous);
            _gl.DebugMessageCallback(DebugCallback, IntPtr.Zero);

            // Reversed-Z: NDC z in [0, 1] rather than [-1, 1].
            // Pairs with a near -> 1.0 / far -> 0.0 depth convention for
            // planetary-scale depth precision (see Book Chapter 6).
            _gl.ClipControl(ClipControlOrigin.LowerLeft, ClipControlDepth.ZeroToOne);

            // Seed the shadow with our preferred defaults (DepthTest/FacetCulling
            // enabled, etc. -- these differ from the GL defaults). The shadow
            // does NOT match actual GL state yet; ForceApplyRenderState below
            // pushes every field to GL so shadow and GL are in sync from the
            // first draw onward. Without this push, the first ApplyDepthTest
            // would see shadow.Enabled == desired.Enabled == true and skip the
            // glEnable, leaving the depth test actually disabled.
            _shadowState = new RenderState();
            ForceApplyRenderState(_shadowState);

            // ShaderCache is a context-owned resource: it holds GL program handles,
            // which are only valid for this context's lifetime. Dispose(this) tears it down.
            Shaders = new ShaderCache(_gl);

            // TextureUnits queries GL_MAX_COMBINED_TEXTURE_IMAGE_UNITS now, so
            // construct it after the GL context is fully set up.
            TextureUnits = new TextureUnits(_gl);
        }

        /// <summary>
        /// Debug callback invoked by the GL driver for errors, warnings, and info.
        /// </summary>
        private static void DebugCallback(GLEnum source, GLEnum type, int id,
            GLEnum severity, int length, nint message, nint userParam)
        {
            // Decode the message string from the native pointer
            string msg = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(message)
                ?? "(null)";

            // Filter out notification-level messages (very noisy)
            if (severity == GLEnum.DebugSeverityNotification)
                return;

            string severityStr = severity switch
            {
                GLEnum.DebugSeverityHigh => "HIGH",
                GLEnum.DebugSeverityMedium => "MEDIUM",
                GLEnum.DebugSeverityLow => "LOW",
                _ => "INFO"
            };

            Console.WriteLine($"[GL {severityStr}] {msg}");
        }

        /// <summary>
        /// Clears the framebuffer according to the given clear state.
        /// Temporarily overrides color mask and depth mask to ensure clearing works,
        /// then restores the shadow state values.
        /// </summary>
        /// <param name="clearState">What to clear and to what values.</param>
        public void Clear(ClearState clearState)
        {
            // Set clear values
            _gl.ClearColor(clearState.Color.X, clearState.Color.Y,
                clearState.Color.Z, clearState.Color.W);
            _gl.ClearDepth(clearState.Depth);
            _gl.ClearStencil(clearState.Stencil);

            // Temporarily enable full color + depth writes so the clear works.
            // If the current shadow state has writes disabled, the clear would
            // silently do nothing without this override.
            _gl.ColorMask(
                clearState.ColorMask.Red,
                clearState.ColorMask.Green,
                clearState.ColorMask.Blue,
                clearState.ColorMask.Alpha);
            _gl.DepthMask(clearState.DepthMask);

            // Build the clear mask from the ClearBuffers flags
            ClearBufferMask mask = 0;
            if (clearState.Buffers.HasFlag(ClearBuffers.ColorBuffer))
                mask |= ClearBufferMask.ColorBufferBit;
            if (clearState.Buffers.HasFlag(ClearBuffers.DepthBuffer))
                mask |= ClearBufferMask.DepthBufferBit;
            if (clearState.Buffers.HasFlag(ClearBuffers.StencilBuffer))
                mask |= ClearBufferMask.StencilBufferBit;

            _gl.Clear((uint)mask);

            // Restore shadow state for color mask and depth mask
            // (so subsequent draws use the correct values)
            _gl.ColorMask(
                _shadowState.ColorMask.Red,
                _shadowState.ColorMask.Green,
                _shadowState.ColorMask.Blue,
                _shadowState.ColorMask.Alpha);
            _gl.DepthMask(_shadowState.DepthMask.Enabled);
        }

        /// <summary>
        /// Issue an indexed draw call. This is the canonical draw path for geometry
        /// that shares vertices between primitives (i.e., has an element buffer);
        /// for non-indexed geometry (e.g., simple point or triangle strips built
        /// from a flat vertex stream) use <see cref="DrawArrays"/>.
        /// </summary>
        /// <param name="primitiveType">Primitive topology -- triangles, lines, points, etc.</param>
        /// <param name="drawState">Render state + shader program + vertex array to draw.</param>
        /// <param name="sceneState">Camera and scene data consumed by automatic uniforms.</param>
        /// <remarks>
        /// The per-call sequence is:
        /// <list type="number">
        ///   <item>Apply render state (shadow-filtered).</item>
        ///   <item>Bind the shader, which runs every draw-automatic uniform and
        ///   flushes the program's dirty-uniform list.</item>
        ///   <item>Bind the VAO.</item>
        ///   <item>Issue <c>glDrawElements</c>.</item>
        /// </list>
        /// Index type is hard-coded to <c>GL_UNSIGNED_INT</c> because
        /// <see cref="Buffers.VertexArrayObject"/> uses <c>uint[]</c> index buffers. If a
        /// smaller index type is ever supported, branch here on the VAO's index type.
        /// </remarks>
        public unsafe void Draw(PrimitiveType primitiveType, DrawState drawState, SceneState sceneState)
        {
            ApplyRenderState(drawState.RenderState);
            TextureUnits.Clean();
            drawState.ShaderProgram.Bind(this, drawState, sceneState);
            _gl.BindVertexArray(drawState.VertexArrayObject.Handle);

            // null indices pointer -- offset 0 into the bound element buffer,
            // which is what we want for a simple "draw the whole VAO" call.
            _gl.DrawElements(ToGlPrimitiveType(primitiveType),
                (uint)drawState.VertexArrayObject.IndexCount,
                drawState.VertexArrayObject.IndexType, (void*)0);
        }

        /// <summary>
        /// Issue a non-indexed draw call (<c>glDrawArrays</c>). Used when the VAO has
        /// no element buffer -- the GPU walks the vertex stream in order, consuming
        /// primitives according to <paramref name="primitiveType"/>.
        /// </summary>
        /// <param name="primitiveType">Primitive topology.</param>
        /// <param name="first">Starting vertex index in the bound vertex stream.</param>
        /// <param name="count">Number of vertices to consume.</param>
        /// <param name="drawState">Render state + shader program + vertex array to draw.</param>
        /// <param name="sceneState">Camera and scene data consumed by automatic uniforms.</param>
        public void DrawArrays(PrimitiveType primitiveType, int first, uint count,
            DrawState drawState, SceneState sceneState)
        {
            ApplyRenderState(drawState.RenderState);
            TextureUnits.Clean();
            drawState.ShaderProgram.Bind(this, drawState, sceneState);
            _gl.BindVertexArray(drawState.VertexArrayObject.Handle);

            _gl.DrawArrays(ToGlPrimitiveType(primitiveType), first, count);
        }

        #region Mesh -> VAO Bridge

        /// <summary>
        /// Build a <see cref="VertexArrayObject"/> from a <see cref="Mesh"/> and the
        /// shader program that will draw it. Matches mesh attributes to shader
        /// attributes by name, packs the vertex data interleaved into a single VBO,
        /// allocates a typed EBO, and wires attribute formats to shader locations
        /// via DSA.
        /// <para>
        /// Lives on <see cref="RenderContext"/> rather than <see cref="Rendering.Device"/>
        /// because VAO objects are not shared across GL contexts -- their lifetime is
        /// bound to the context that created them. The underlying buffers (VBO/EBO)
        /// <i>are</i> shareable, but the VAO binding them is not.
        /// </para>
        /// </summary>
        /// <param name="mesh">Source geometry. Its attribute values and indices are read but not retained.</param>
        /// <param name="shader">The shader program whose <c>in</c> attributes define which mesh attributes are needed and at which locations.</param>
        /// <param name="bufferHint">Usage hint for the backing GPU buffers. Currently advisory -- <see cref="VertexBuffer"/> and <see cref="IndexBuffer"/> always use immutable storage with <c>DynamicStorageBit</c>.</param>
        /// <remarks>
        /// Name matching rules:
        /// <list type="bullet">
        ///   <item>Every active shader attribute must have a same-name mesh attribute.
        ///     A missing match throws with a descriptive error.</item>
        ///   <item><see cref="VertexAttributeType.EmulatedDoubleVector3"/> mesh
        ///     attributes feed two shader attributes: <c>"nameHigh"</c> and
        ///     <c>"nameLow"</c>. Both halves must be present in the shader or the
        ///     call throws.</item>
        /// </list>
        /// </remarks>
        public VertexArrayObject CreateVertexArray(Mesh mesh, ShaderProgram shader, BufferHint bufferHint)
        {
            Dictionary<string, ShaderAttribInfo> shaderAttribs = EnumerateShaderAttributes(shader);
            List<AttributeBinding> bindings = BindMeshToShader(mesh, shaderAttribs);

            // Pack attributes in ascending shader-location order for stable layout.
            bindings.Sort((a, b) => a.Location.CompareTo(b.Location));

            // Compute byte offsets and total stride.
            uint offset = 0;
            var formats = new InterleavedVertexAttribute[bindings.Count];
            for (int i = 0; i < bindings.Count; i++)
            {
                AttributeBinding b = bindings[i];
                formats[i] = new InterleavedVertexAttribute(
                    b.Location, b.Components, b.Type, b.Normalized, offset);
                bindings[i] = b with { Offset = offset };
                offset += ComponentSizeBytes(b.Type) * (uint)b.Components;
            }
            uint stride = offset;

            int vertexCount = GetVertexCount(mesh);
            byte[] vboData = new byte[vertexCount * (int)stride];
            Span<byte> vboSpan = vboData.AsSpan();
            for (int v = 0; v < vertexCount; v++)
            {
                foreach (AttributeBinding b in bindings)
                {
                    Span<byte> dest = vboSpan.Slice((int)(v * stride + b.Offset));
                    WriteVertexAttribute(dest, b.Source, v, b.Split);
                }
            }
            var vbo = new VertexBuffer(_gl, bufferHint, vboData.Length);
            vbo.CopyFromSystemMemory(vboData);

            IndexBuffer? ebo = CreateElementBuffer(mesh, bufferHint);

            return new VertexArrayObject(_gl, vbo, stride, ebo, formats);
        }

        private readonly record struct ShaderAttribInfo(
            uint Location, int Components, VertexAttribType Type);

        private record struct AttributeBinding(
            uint Location,
            int Components,
            VertexAttribType Type,
            bool Normalized,
            VertexAttribute Source,
            DoubleSplit Split,
            uint Offset);

        private enum DoubleSplit { None, High, Low }

        private Dictionary<string, ShaderAttribInfo> EnumerateShaderAttributes(ShaderProgram shader)
        {
            _gl.GetProgram(shader.Handle, ProgramPropertyARB.ActiveAttributes, out int count);
            _gl.GetProgram(shader.Handle, ProgramPropertyARB.ActiveAttributeMaxLength, out int maxLen);

            var result = new Dictionary<string, ShaderAttribInfo>(count);
            for (uint i = 0; i < (uint)count; i++)
            {
                _gl.GetActiveAttrib(shader.Handle, i, (uint)maxLen,
                    out _, out _, out AttributeType glType, out string name);

                // Builtins (gl_VertexID, gl_InstanceID) report as active but have
                // no location; skip them silently.
                int location = _gl.GetAttribLocation(shader.Handle, name);
                if (location < 0) continue;

                (VertexAttribType type, int components) = DescribeShaderAttribType(glType, name);
                result.Add(name, new ShaderAttribInfo((uint)location, components, type));
            }
            return result;
        }

        private static (VertexAttribType Type, int Components) DescribeShaderAttribType(
            AttributeType glType, string name)
        {
            return glType switch
            {
                AttributeType.Float     => (VertexAttribType.Float, 1),
                AttributeType.FloatVec2 => (VertexAttribType.Float, 2),
                AttributeType.FloatVec3 => (VertexAttribType.Float, 3),
                AttributeType.FloatVec4 => (VertexAttribType.Float, 4),
                _ => throw new NotSupportedException(
                    $"Shader attribute '{name}' has GLSL type {glType}, which is not " +
                    $"yet supported by CreateVertexArray. Extend DescribeShaderAttribType.")
            };
        }

        private static List<AttributeBinding> BindMeshToShader(
            Mesh mesh,
            Dictionary<string, ShaderAttribInfo> shaderAttribs)
        {
            var bindings = new List<AttributeBinding>(shaderAttribs.Count);
            var consumed = new HashSet<string>();

            foreach ((string name, ShaderAttribInfo info) in shaderAttribs)
            {
                if (mesh.Attributes.Contains(name))
                {
                    VertexAttribute meshAttr = mesh.Attributes[name];
                    ValidateComponentMatch(name, meshAttr, info);
                    bindings.Add(new AttributeBinding(
                        info.Location, info.Components, info.Type,
                        NormalizedForType(meshAttr.DataType),
                        meshAttr, DoubleSplit.None, 0));
                    consumed.Add(name);
                    continue;
                }

                // EmulatedDoubleVector3: "<base>High" / "<base>Low" pair.
                string? baseName = TryStripSuffix(name, out DoubleSplit split);
                if (baseName != null
                    && mesh.Attributes.Contains(baseName)
                    && mesh.Attributes[baseName].DataType == VertexAttributeType.EmulatedDoubleVector3)
                {
                    bindings.Add(new AttributeBinding(
                        info.Location, info.Components, info.Type, false,
                        mesh.Attributes[baseName], split, 0));
                    continue;
                }

                throw new InvalidOperationException(
                    $"Shader declares attribute '{name}' but the mesh has no matching " +
                    $"attribute (and no '{baseName}' emulated-double source).");
            }

            // Validate that every half of a double split is present in the shader.
            foreach (VertexAttribute a in mesh.Attributes.All)
            {
                if (a.DataType != VertexAttributeType.EmulatedDoubleVector3) continue;
                bool hasHigh = shaderAttribs.ContainsKey(a.Name + "High");
                bool hasLow = shaderAttribs.ContainsKey(a.Name + "Low");
                if (hasHigh ^ hasLow)
                {
                    throw new InvalidOperationException(
                        $"Emulated-double mesh attribute '{a.Name}' requires both " +
                        $"'{a.Name}High' and '{a.Name}Low' in the shader; found only one.");
                }
            }

            return bindings;
        }

        private static string? TryStripSuffix(string name, out DoubleSplit split)
        {
            if (name.EndsWith("High", StringComparison.Ordinal))
            {
                split = DoubleSplit.High;
                return name.Substring(0, name.Length - 4);
            }
            if (name.EndsWith("Low", StringComparison.Ordinal))
            {
                split = DoubleSplit.Low;
                return name.Substring(0, name.Length - 3);
            }
            split = DoubleSplit.None;
            return null;
        }

        private static void ValidateComponentMatch(
            string name, VertexAttribute meshAttr, ShaderAttribInfo info)
        {
            int meshComponents = meshAttr.DataType switch
            {
                VertexAttributeType.UnsignedByte => 1,
                VertexAttributeType.HalfFloat or VertexAttributeType.Float => 1,
                VertexAttributeType.HalfFloatVector2 or VertexAttributeType.FloatVector2 => 2,
                VertexAttributeType.HalfFloatVector3 or VertexAttributeType.FloatVector3 => 3,
                VertexAttributeType.HalfFloatVector4 or VertexAttributeType.FloatVector4 => 4,
                VertexAttributeType.EmulatedDoubleVector3 => 3,
                _ => throw new NotSupportedException(
                    $"Mesh attribute '{name}' has unsupported type {meshAttr.DataType}.")
            };
            if (meshComponents != info.Components)
            {
                throw new InvalidOperationException(
                    $"Attribute '{name}' component mismatch: shader expects " +
                    $"{info.Components}, mesh provides {meshComponents}.");
            }
        }

        private static bool NormalizedForType(VertexAttributeType t)
            => t == VertexAttributeType.UnsignedByte;

        private static uint ComponentSizeBytes(VertexAttribType t) => t switch
        {
            VertexAttribType.UnsignedByte => 1,
            VertexAttribType.HalfFloat => 2,
            VertexAttribType.Float => 4,
            _ => throw new NotSupportedException($"Component size for {t} not defined.")
        };

        private static int GetVertexCount(Mesh mesh)
        {
            int count = -1;
            foreach (VertexAttribute a in mesh.Attributes.All)
            {
                if (count < 0) count = a.Count;
                else if (a.Count != count)
                    throw new InvalidOperationException(
                        $"Mesh attributes disagree on vertex count: '{a.Name}' has " +
                        $"{a.Count}, expected {count}.");
            }
            if (count < 0)
                throw new InvalidOperationException("Mesh has no attributes.");
            return count;
        }

        private IndexBuffer? CreateElementBuffer(Mesh mesh, BufferHint hint)
        {
            switch (mesh.Indices)
            {
                case null:
                    return null;
                case IndicesUnsignedInt u:
                    {
                        uint[] arr = new uint[u.Values.Count];
                        u.Values.CopyTo(arr, 0);
                        var buf = new IndexBuffer(_gl, hint, arr.Length * sizeof(uint),
                            IndexBufferDatatype.UnsignedInt);
                        buf.CopyFromSystemMemory(arr);
                        return buf;
                    }
                case IndicesUnsignedShort s:
                    {
                        ushort[] arr = new ushort[s.Values.Count];
                        s.Values.CopyTo(arr, 0);
                        var buf = new IndexBuffer(_gl, hint, arr.Length * sizeof(ushort),
                            IndexBufferDatatype.UnsignedShort);
                        buf.CopyFromSystemMemory(arr);
                        return buf;
                    }
                default:
                    throw new NotSupportedException(
                        $"Index buffer type {mesh.Indices.GetType().Name} is not supported.");
            }
        }

        private static void WriteVertexAttribute(
            Span<byte> dest, VertexAttribute src, int vertex, DoubleSplit split)
        {
            switch (src)
            {
                case VertexAttributeUnsignedByte a:
                    dest[0] = a.Values[vertex];
                    return;

                case VertexAttributeFloat a:
                    BitConverter.TryWriteBytes(dest, a.Values[vertex]);
                    return;

                case VertexAttributeFloatVector2 a:
                    {
                        Vector2 v = a.Values[vertex];
                        MemoryMarshal.Write(dest, in v);
                        return;
                    }

                case VertexAttributeFloatVector3 a:
                    {
                        Vector3 v = a.Values[vertex];
                        MemoryMarshal.Write(dest, in v);
                        return;
                    }

                case VertexAttributeFloatVector4 a:
                    {
                        Vector4 v = a.Values[vertex];
                        MemoryMarshal.Write(dest, in v);
                        return;
                    }

                case VertexAttributeHalfFloat a:
                    BitConverter.TryWriteBytes(dest, a.Values[vertex]);
                    return;

                case VertexAttributeHalfFloatVector2 a:
                    {
                        (Half x, Half y) = a.Values[vertex];
                        BitConverter.TryWriteBytes(dest, x);
                        BitConverter.TryWriteBytes(dest.Slice(2), y);
                        return;
                    }

                case VertexAttributeHalfFloatVector3 a:
                    {
                        (Half x, Half y, Half z) = a.Values[vertex];
                        BitConverter.TryWriteBytes(dest, x);
                        BitConverter.TryWriteBytes(dest.Slice(2), y);
                        BitConverter.TryWriteBytes(dest.Slice(4), z);
                        return;
                    }

                case VertexAttributeHalfFloatVector4 a:
                    {
                        (Half x, Half y, Half z, Half w) = a.Values[vertex];
                        BitConverter.TryWriteBytes(dest, x);
                        BitConverter.TryWriteBytes(dest.Slice(2), y);
                        BitConverter.TryWriteBytes(dest.Slice(4), z);
                        BitConverter.TryWriteBytes(dest.Slice(6), w);
                        return;
                    }

                case VertexAttributeDoubleVector3 a:
                    {
                        // DSFP/RTE split: high = (float)d; low = (float)(d - high).
                        // Both halves together reconstruct the double to 2^-24 + 2^-53 error.
                        Vector3D d = a.Values[vertex];
                        float hx = (float)d.X, hy = (float)d.Y, hz = (float)d.Z;
                        Vector3 w = split == DoubleSplit.High
                            ? new Vector3(hx, hy, hz)
                            : new Vector3((float)(d.X - hx), (float)(d.Y - hy), (float)(d.Z - hz));
                        MemoryMarshal.Write(dest, in w);
                        return;
                    }

                default:
                    throw new NotSupportedException(
                        $"Vertex attribute type {src.DataType} is not supported by CreateVertexArray.");
            }
        }

        #endregion

        #region Render State Application (with shadowing)

        /// <summary>
        /// Applied the desired render state, comparing against the shadow state to minimize GL calls.
        /// </summary>
        /// <param name="desired"></param>
        private void ApplyRenderState(RenderState desired)
        {
            ApplyDepthTest(desired.DepthTest);
            ApplyFacetCulling(desired.FacetCulling);
            ApplyBlending(desired.Blending);
            ApplyDepthRange(desired.DepthRange);
            ApplyDepthMask(desired.DepthMask);
            ApplyColorMask(desired.ColorMask);
            ApplyScissorTest(desired.ScissorTest);
            ApplyRasterizationMode(desired.RasterizationMode);
        }

        /// <summary>
        /// Force-applies all render state to the GPU without shadow comparison.
        /// Used at initialization to synchronize the shadow with the actual GPU state.
        /// </summary>
        private void ForceApplyRenderState(RenderState state)
        {
            // Depth test
            if (state.DepthTest.Enabled)
                _gl.Enable(EnableCap.DepthTest);
            else
                _gl.Disable(EnableCap.DepthTest);
            _gl.DepthFunc(ToGlDepthFunction(state.DepthTest.Function));

            // Facet culling
            if (state.FacetCulling.Enabled)
                _gl.Enable(EnableCap.CullFace);
            else
                _gl.Disable(EnableCap.CullFace);
            _gl.CullFace(ToGlCullFace(state.FacetCulling.Face));
            _gl.FrontFace(ToGlFrontFace(state.FacetCulling.FrontFaceWindingOrder));

            // Blending
            if (state.Blending.Enabled)
                _gl.Enable(EnableCap.Blend);
            else
                _gl.Disable(EnableCap.Blend);
            _gl.BlendFunc(
                ToGlBlendFactor(state.Blending.SourceFactor),
                ToGlBlendFactor(state.Blending.DestinationFactor));

            // Depth range
            _gl.DepthRange(state.DepthRange.Near, state.DepthRange.Far);

            // Depth mask
            _gl.DepthMask(state.DepthMask.Enabled);

            // Color mask
            _gl.ColorMask(
                state.ColorMask.Red,
                state.ColorMask.Green,
                state.ColorMask.Blue,
                state.ColorMask.Alpha);

            // Scissor test
            if (state.ScissorTest.Enabled)
                _gl.Enable(EnableCap.ScissorTest);
            else
                _gl.Disable(EnableCap.ScissorTest);
            _gl.Scissor(
                state.ScissorTest.X,
                state.ScissorTest.Y,
                (uint)state.ScissorTest.Width,
                (uint)state.ScissorTest.Height);

            // Rasterization mode
            _gl.PolygonMode(
                TriangleFace.FrontAndBack,
                ToGlPolygonMode(state.RasterizationMode));
        }

        #endregion

        #region State Applicators (with shadow state updates)

        private void ApplyDepthTest(DepthTest desired)
        {
            DepthTest shadow = _shadowState.DepthTest;
            if (desired.Enabled != shadow.Enabled)
            {
                if (desired.Enabled)
                    _gl.Enable(EnableCap.DepthTest);
                else
                    _gl.Disable(EnableCap.DepthTest);
                shadow.Enabled = desired.Enabled;
            }

            if (desired.Function != shadow.Function)
            {
                _gl.DepthFunc(ToGlDepthFunction(desired.Function));
                shadow.Function = desired.Function;
            }
        }

        private void ApplyFacetCulling(FacetCulling desired)
        {
            FacetCulling shadow = _shadowState.FacetCulling;
            if (desired.Enabled != shadow.Enabled)
            {
                if (desired.Enabled)
                {
                    _gl.Enable(EnableCap.CullFace);

                }
                else
                {
                    _gl.Disable(EnableCap.CullFace);
                }
                shadow.Enabled = desired.Enabled;
            }

            if(desired.Face != shadow.Face)
            {
                _gl.CullFace(ToGlCullFace(desired.Face));
                shadow.Face = desired.Face;
            }
        }

        private void ApplyBlending(Blending desired)
        {
            Blending shadow = _shadowState.Blending;

            if (desired.Enabled != shadow.Enabled)
            {
                if (desired.Enabled)
                    _gl.Enable(EnableCap.Blend);
                else
                    _gl.Disable(EnableCap.Blend);
                shadow.Enabled = desired.Enabled;
            }

            if (desired.SourceFactor != shadow.SourceFactor ||
                desired.DestinationFactor != shadow.DestinationFactor)
            {
                _gl.BlendFunc(
                    ToGlBlendFactor(desired.SourceFactor),
                    ToGlBlendFactor(desired.DestinationFactor));
                shadow.SourceFactor = desired.SourceFactor;
                shadow.DestinationFactor = desired.DestinationFactor;
            }
        }

        private void ApplyDepthRange(DepthRange desired)
        {
            DepthRange shadow = _shadowState.DepthRange;

            if (desired.Near != shadow.Near || desired.Far != shadow.Far)
            {
                _gl.DepthRange(desired.Near, desired.Far);
                shadow.Near = desired.Near;
                shadow.Far = desired.Far;
            }
        }

        private void ApplyDepthMask(DepthMask desired)
        {
            DepthMask shadow = _shadowState.DepthMask;

            if (desired.Enabled != shadow.Enabled)
            {
                _gl.DepthMask(desired.Enabled);
                shadow.Enabled = desired.Enabled;
            }
        }

        private void ApplyColorMask(ColorMask desired)
        {
            ColorMask shadow = _shadowState.ColorMask;

            if (desired.Red != shadow.Red ||
                desired.Green != shadow.Green ||
                desired.Blue != shadow.Blue ||
                desired.Alpha != shadow.Alpha)
            {
                _gl.ColorMask(desired.Red, desired.Green, desired.Blue, desired.Alpha);
                shadow.Red = desired.Red;
                shadow.Green = desired.Green;
                shadow.Blue = desired.Blue;
                shadow.Alpha = desired.Alpha;
            }
        }

        private void ApplyScissorTest(ScissorTest desired)
        {
            ScissorTest shadow = _shadowState.ScissorTest;

            if (desired.Enabled != shadow.Enabled)
            {
                if (desired.Enabled)
                    _gl.Enable(EnableCap.ScissorTest);
                else
                    _gl.Disable(EnableCap.ScissorTest);
                shadow.Enabled = desired.Enabled;
            }

            if (desired.X != shadow.X || desired.Y != shadow.Y ||
                desired.Width != shadow.Width || desired.Height != shadow.Height)
            {
                _gl.Scissor(desired.X, desired.Y,
                    (uint)desired.Width, (uint)desired.Height);
                shadow.X = desired.X;
                shadow.Y = desired.Y;
                shadow.Width = desired.Width;
                shadow.Height = desired.Height;
            }
        }

        private void ApplyRasterizationMode(RasterizationMode desired)
        {
            if (desired != _shadowState.RasterizationMode)
            {
                _gl.PolygonMode(TriangleFace.FrontAndBack, ToGlPolygonMode(desired));
                _shadowState.RasterizationMode = desired;
            }
        }

        #endregion

        #region Enum Conversion Helpers

        private static DepthFunction ToGlDepthFunction(DepthTestFunction f) => f switch
        {
            DepthTestFunction.Never => DepthFunction.Never,
            DepthTestFunction.Less => DepthFunction.Less,
            DepthTestFunction.Equal => DepthFunction.Equal,
            DepthTestFunction.LessThanOrEqual => DepthFunction.Lequal,
            DepthTestFunction.Greater => DepthFunction.Greater,
            DepthTestFunction.NotEqual => DepthFunction.Notequal,
            DepthTestFunction.GreaterThanOrEqual => DepthFunction.Gequal,
            DepthTestFunction.Always => DepthFunction.Always,
            _ => DepthFunction.Less

        };

        private static TriangleFace ToGlCullFace(CullFace f) => f switch
        {
            CullFace.Front => TriangleFace.Front,
            CullFace.Back => TriangleFace.Back,
            CullFace.FrontAndBack => TriangleFace.FrontAndBack,
            _ => TriangleFace.Back
        };

        private static FrontFaceDirection ToGlFrontFace(WindingOrder w) => w switch
        {
            WindingOrder.Clockwise => FrontFaceDirection.CW,
            WindingOrder.CounterClockwise => FrontFaceDirection.Ccw,
            _ => FrontFaceDirection.Ccw
        };

        private static Silk.NET.OpenGL.BlendingFactor ToGlBlendFactor(State.BlendingFactor f) => f switch
        {
            State.BlendingFactor.Zero => Silk.NET.OpenGL.BlendingFactor.Zero,
            State.BlendingFactor.One => Silk.NET.OpenGL.BlendingFactor.One,
            State.BlendingFactor.SourceColor => Silk.NET.OpenGL.BlendingFactor.SrcColor,
            State.BlendingFactor.OneMinusSourceColor => Silk.NET.OpenGL.BlendingFactor.OneMinusSrcColor,
            State.BlendingFactor.DestinationColor => Silk.NET.OpenGL.BlendingFactor.DstColor,
            State.BlendingFactor.OneMinusDestinationColor => Silk.NET.OpenGL.BlendingFactor.OneMinusDstColor,
            State.BlendingFactor.SourceAlpha => Silk.NET.OpenGL.BlendingFactor.SrcAlpha,
            State.BlendingFactor.OneMinusSourceAlpha => Silk.NET.OpenGL.BlendingFactor.OneMinusSrcAlpha,
            State.BlendingFactor.DestinationAlpha => Silk.NET.OpenGL.BlendingFactor.DstAlpha,
            State.BlendingFactor.OneMinusDestinationAlpha => Silk.NET.OpenGL.BlendingFactor.OneMinusDstAlpha,
            _ => Silk.NET.OpenGL.BlendingFactor.One
        };

        private static GlPrimitiveType ToGlPrimitiveType(PrimitiveType p) => p switch
        {
            PrimitiveType.Points => GlPrimitiveType.Points,
            PrimitiveType.Lines => GlPrimitiveType.Lines,
            PrimitiveType.LineLoop => GlPrimitiveType.LineLoop,
            PrimitiveType.LineStrip => GlPrimitiveType.LineStrip,
            PrimitiveType.Triangles => GlPrimitiveType.Triangles,
            PrimitiveType.TriangleStrip => GlPrimitiveType.TriangleStrip,
            PrimitiveType.TriangleFan => GlPrimitiveType.TriangleFan,
            PrimitiveType.LinesAdjacency => GlPrimitiveType.LinesAdjacency,
            PrimitiveType.LineStripAdjacency => GlPrimitiveType.LineStripAdjacency,
            PrimitiveType.TrianglesAdjacency => GlPrimitiveType.TrianglesAdjacency,
            PrimitiveType.TriangleStripAdjacency => GlPrimitiveType.TriangleStripAdjacency,
            _ => GlPrimitiveType.Triangles
        };

        private static PolygonMode ToGlPolygonMode(RasterizationMode rasterizationMode) => rasterizationMode switch
        {
            RasterizationMode.Fill => PolygonMode.Fill,
            RasterizationMode.Line => PolygonMode.Line,
            RasterizationMode.Point => PolygonMode.Point,
            _ => PolygonMode.Fill
        };

        #endregion

        /// <summary>
        /// Dispose the render context and every GL resource it owns.
        /// Currently that is just the <see cref="Shaders"/> cache; as more
        /// context-owned resources are added (framebuffers, VAOs, etc.) they
        /// dispose here in reverse creation order.
        /// Must be called on the render thread.
        /// </summary>
        public void Dispose()
        {
            Shaders.Dispose();
        }
    }
}
