using Silk.NET.OpenGL;
using System;
using System.Numerics;

namespace Geode.Rendering
{
    public class RenderContext : IDisposable
    {
        private readonly GL _gl;
        private RenderState _shadowState;

        public RenderContext(GL gl)
        {
            _gl = gl;

            _gl.Enable(EnableCap.DebugOutput);
            _gl.Enable(EnableCap.DebugOutputSynchronous);
            _gl.DebugMessageCallback(DebugCallback, IntPtr.Zero);

            _gl.ClipControl(ClipControlOrigin.LowerLeft, ClipControlDepth.ZeroToOne);

            _shadowState = new RenderState();
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

        #region Set Common Scene Uniforms

        private static float[] Matrix4x4ToArray(Matrix4x4 m)
        {
            return new float[16]
            {
                m.M11, m.M12, m.M13, m.M14,
                m.M21, m.M22, m.M23, m.M24,
                m.M31, m.M32, m.M33, m.M34,
                m.M41, m.M42, m.M43, m.M44
            };
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
            if(desired.Enabled != shadow.Enabled)
            {
                if (desired.Enabled)
                {
                    _gl.Enable(EnableCap.DepthTest);
                }
                else
                {
                    _gl.Disable(EnableCap.DepthTest);
                }
            }

            if(desired.Function != shadow.Function)
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

        public void Dispose()
        {
            
        }
    }
}
