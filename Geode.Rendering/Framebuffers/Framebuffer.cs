using Geode.Rendering.Textures;
using Silk.NET.OpenGL;
using System;

namespace Geode.Rendering.Framebuffers
{
    /// <summary>
    /// A Framebuffer Object (FBO) -- a named bundle of texture attachments
    /// that the GPU writes into during a draw. Book §3.7.
    /// <para>
    /// Set <see cref="RenderContext.Framebuffer"/> to an instance to redirect
    /// draws into this FBO's attached textures; set it back to <c>null</c> to
    /// resume drawing into the window. Attachments are assigned via the
    /// <see cref="ColorAttachments"/> indexer and the
    /// <see cref="DepthAttachment"/> / <see cref="DepthStencilAttachment"/>
    /// properties; changes are flushed to GL during
    /// <see cref="RenderContext"/>'s per-draw <see cref="Clean"/> so multiple
    /// assignments between draws collapse to a single set of GL calls.
    /// </para>
    /// <para>
    /// Lives on <see cref="RenderContext"/> rather than <see cref="Device"/>
    /// because FBO handles are not shareable across GL contexts -- they bind
    /// to the context that created them, same as VAOs.
    /// </para>
    /// </summary>
    public sealed class Framebuffer : IDisposable
    {
        private readonly GL _gl;
        private readonly uint _handle;
        private readonly ColorAttachments _colorAttachments;
        private Texture2D? _depthAttachment;
        private Texture2D? _depthStencilAttachment;

        // Shadow state: what we've most recently pushed to the GL FBO, so
        // Clean() can compare and skip no-op rebinds.
        private readonly uint[] _shadowColor;
        private uint _shadowDepth;
        private uint _shadowDepthStencil;

        // DrawBuffers gets rebuilt from the color-attachment presence mask
        // whenever any color slot changes. Buffer reused to avoid per-draw allocs.
        private readonly GLEnum[] _drawBuffersScratch;

        // Whether we've checked completeness since the last attachment change.
        private bool _completenessChecked;

        /// <summary>The raw OpenGL framebuffer handle.</summary>
        public uint Handle => _handle;

        /// <summary>
        /// Color texture attachments indexed by slot. Slot <c>i</c> corresponds
        /// to <c>GL_COLOR_ATTACHMENT0 + i</c> and fragment-shader output
        /// <c>layout(location = i)</c>.
        /// </summary>
        public ColorAttachments ColorAttachments => _colorAttachments;

        /// <summary>
        /// The depth-only attachment. Mutually exclusive with
        /// <see cref="DepthStencilAttachment"/> -- setting one while the other
        /// is non-null will leave the framebuffer in an incomplete state until
        /// the other is cleared.
        /// </summary>
        public Texture2D? DepthAttachment
        {
            get => _depthAttachment;
            set => _depthAttachment = value;
        }

        /// <summary>
        /// Combined depth-stencil attachment. Use for formats like
        /// <see cref="TextureFormat.Depth24Stencil8"/> and
        /// <see cref="TextureFormat.Depth32fStencil8"/>.
        /// </summary>
        public Texture2D? DepthStencilAttachment
        {
            get => _depthStencilAttachment;
            set => _depthStencilAttachment = value;
        }

        internal Framebuffer(GL gl, int maximumColorAttachments)
        {
            _gl = gl;
            _handle = _gl.CreateFramebuffer();
            _colorAttachments = new ColorAttachments(maximumColorAttachments);
            _shadowColor = new uint[maximumColorAttachments];
            _drawBuffersScratch = new GLEnum[maximumColorAttachments];
        }

        /// <summary>
        /// Flush any pending attachment changes to GL and validate
        /// completeness. Invoked by <see cref="RenderContext"/> immediately
        /// before any draw targeting this FBO.
        /// </summary>
        internal unsafe void Clean()
        {
            bool colorChanged = false;

            // Push color attachments that differ from shadow.
            for (int i = 0; i < _colorAttachments.Count; i++)
            {
                uint desired = _colorAttachments[i]?.Handle ?? 0;
                if (desired != _shadowColor[i])
                {
                    _gl.NamedFramebufferTexture(
                        _handle,
                        (FramebufferAttachment)((int)FramebufferAttachment.ColorAttachment0 + i),
                        desired, 0);
                    _shadowColor[i] = desired;
                    colorChanged = true;
                }
            }

            // Depth.
            uint depthHandle = _depthAttachment?.Handle ?? 0;
            if (depthHandle != _shadowDepth)
            {
                _gl.NamedFramebufferTexture(_handle,
                    FramebufferAttachment.DepthAttachment, depthHandle, 0);
                _shadowDepth = depthHandle;
                _completenessChecked = false;
            }

            // Combined depth-stencil.
            uint dsHandle = _depthStencilAttachment?.Handle ?? 0;
            if (dsHandle != _shadowDepthStencil)
            {
                _gl.NamedFramebufferTexture(_handle,
                    FramebufferAttachment.DepthStencilAttachment, dsHandle, 0);
                _shadowDepthStencil = dsHandle;
                _completenessChecked = false;
            }

            // Rebuild the DrawBuffers array when any color slot changed.
            // Without this, fragment outputs would not route to attachments --
            // GL's default is GL_COLOR_ATTACHMENT0 only for FBOs.
            if (colorChanged)
            {
                for (int i = 0; i < _colorAttachments.Count; i++)
                {
                    _drawBuffersScratch[i] = _shadowColor[i] != 0
                        ? (GLEnum)((int)GLEnum.ColorAttachment0 + i)
                        : GLEnum.None;
                }
                fixed (GLEnum* p = _drawBuffersScratch)
                {
                    _gl.NamedFramebufferDrawBuffers(
                        _handle, (uint)_colorAttachments.Count, p);
                }
                _completenessChecked = false;
            }

            if (!_completenessChecked)
            {
                VerifyComplete();
                _completenessChecked = true;
            }
        }

        private void VerifyComplete()
        {
            GLEnum status = _gl.CheckNamedFramebufferStatus(_handle, FramebufferTarget.Framebuffer);
            if (status != GLEnum.FramebufferComplete)
            {
                throw new InvalidOperationException(
                    $"Framebuffer is not complete: {status}. Check that every " +
                    $"attachment's format is renderable and that attachments " +
                    $"agree on size. See glCheckNamedFramebufferStatus for the " +
                    $"enum meaning.");
            }
        }

        /// <summary>Deletes the GPU framebuffer object. Attachments are not disposed -- the caller owns them.</summary>
        public void Dispose() => _gl.DeleteFramebuffer(_handle);
    }
}
