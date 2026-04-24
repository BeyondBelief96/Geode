using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;

namespace Geode.Rendering
{
    /// <summary>
    /// A window + its OpenGL context + the engine objects (<see cref="Device"/>,
    /// <see cref="RenderContext"/>) bound to that GL. Returned by
    /// <see cref="Device.CreateWindow"/>. Book §2.
    /// <para>
    /// Wraps Silk.NET's <see cref="IWindow"/>. Exposes the wrapped window for
    /// access to input, events, and lifecycle controls; <see cref="Device"/>
    /// and <see cref="Context"/> are accessible only after the window has
    /// fired its <c>Load</c> event (i.e. after the GL context exists).
    /// </para>
    /// </summary>
    public sealed class GraphicsWindow : IDisposable
    {
        private readonly IWindow _window;
        private GL? _gl;
        private Device? _device;
        private RenderContext? _context;

        /// <summary>The underlying Silk.NET window. Subscribe to its events directly.</summary>
        public IWindow Window => _window;

        /// <summary>
        /// The GL binding for this window. Available only after <c>Load</c> has fired.
        /// </summary>
        public GL Gl => _gl
            ?? throw new InvalidOperationException(
                "GL is not available until the window's Load event has fired.");

        /// <summary>
        /// The <see cref="Device"/> bound to this window's GL context. Available
        /// only after <c>Load</c> has fired.
        /// </summary>
        public Device Device => _device
            ?? throw new InvalidOperationException(
                "Device is not available until the window's Load event has fired.");

        /// <summary>
        /// The <see cref="RenderContext"/> bound to this window's GL context.
        /// Available only after <c>Load</c> has fired.
        /// </summary>
        public RenderContext Context => _context
            ?? throw new InvalidOperationException(
                "RenderContext is not available until the window's Load event has fired.");

        internal GraphicsWindow(int width, int height, string title, WindowType type)
        {
            WindowOptions options = WindowOptions.Default with
            {
                Size = new Vector2D<int>(width, height),
                Title = title,
                WindowState = type == WindowType.FullScreen
                    ? WindowState.Fullscreen
                    : WindowState.Normal,
                // Target the same GL version the rest of the engine is written
                // against; DSA paths require 4.5, a few ARB bits require 4.6.
                API = new GraphicsAPI(
                    ContextAPI.OpenGL, ContextProfile.Core,
                    ContextFlags.Debug, new APIVersion(4, 6))
            };

            _window = Silk.NET.Windowing.Window.Create(options);

            // Register BEFORE the caller has a chance to attach their own Load
            // handler, so Device/Context exist by the time their handler fires.
            _window.Load += InitializeGl;
        }

        private void InitializeGl()
        {
            _gl = GL.GetApi(_window);
            _device = new Device(_gl);
            // Wire the shader cache's creation path through Device so every
            // ShaderProgram in the system -- cached or one-off -- flows
            // through a single factory.
            _context = new RenderContext(_gl, _device.CreateShaderProgram);
        }

        /// <summary>Run the window's event loop until it closes.</summary>
        public void Run() => _window.Run();

        /// <summary>
        /// Dispose the render context and the underlying window. The GL object
        /// is released by Silk.NET when the window closes.
        /// </summary>
        public void Dispose()
        {
            _context?.Dispose();
            _window.Dispose();
        }
    }
}
