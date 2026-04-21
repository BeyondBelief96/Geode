using Geode.Rendering.Shaders;
using Silk.NET.OpenGL;
using System;

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
            drawState.ShaderProgram.Bind(this, drawState, sceneState);
            _gl.BindVertexArray(drawState.VertexArrayObject.Handle);

            // null indices pointer -- offset 0 into the bound element buffer,
            // which is what we want for a simple "draw the whole VAO" call.
            _gl.DrawElements(primitiveType,
                (uint)drawState.VertexArrayObject.IndexCount,
                DrawElementsType.UnsignedInt, (void*)0);
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
            drawState.ShaderProgram.Bind(this, drawState, sceneState);
            _gl.BindVertexArray(drawState.VertexArrayObject.Handle);

            _gl.DrawArrays(primitiveType, first, count);
        }

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

        private static Silk.NET.OpenGL.BlendingFactor ToGlBlendFactor(Rendering.BlendingFactor f) => f switch
        {
            Rendering.BlendingFactor.Zero => Silk.NET.OpenGL.BlendingFactor.Zero,
            Rendering.BlendingFactor.One => Silk.NET.OpenGL.BlendingFactor.One,
            Rendering.BlendingFactor.SourceColor => Silk.NET.OpenGL.BlendingFactor.SrcColor,
            Rendering.BlendingFactor.OneMinusSourceColor => Silk.NET.OpenGL.BlendingFactor.OneMinusSrcColor,
            Rendering.BlendingFactor.DestinationColor => Silk.NET.OpenGL.BlendingFactor.DstColor,
            Rendering.BlendingFactor.OneMinusDestinationColor => Silk.NET.OpenGL.BlendingFactor.OneMinusDstColor,
            Rendering.BlendingFactor.SourceAlpha => Silk.NET.OpenGL.BlendingFactor.SrcAlpha,
            Rendering.BlendingFactor.OneMinusSourceAlpha => Silk.NET.OpenGL.BlendingFactor.OneMinusSrcAlpha,
            Rendering.BlendingFactor.DestinationAlpha => Silk.NET.OpenGL.BlendingFactor.DstAlpha,
            Rendering.BlendingFactor.OneMinusDestinationAlpha => Silk.NET.OpenGL.BlendingFactor.OneMinusDstAlpha,
            _ => Silk.NET.OpenGL.BlendingFactor.One
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
