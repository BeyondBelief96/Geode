// FragmentOutputs -- name -> color-attachment-index mapping for a fragment shader.
//
// Book Chapter 3, Section 3.4.3.
// OpenGlobe: Source/Renderer/FragmentOutputs.cs
//
// When a fragment shader declares multiple outputs:
//
//     out vec4 dayColor;
//     out vec4 nightColor;
//
// OpenGL assigns each a "fragment data location" that determines which
// color attachment of the bound framebuffer it writes to. The assignment is
// fixed at link time. Clients use this class to look up the assigned index
// by name, which is then used when configuring a Framebuffer:
//
//     framebuffer.ColorAttachments[shader.FragmentOutputs["dayColor"]] = dayTexture;
//
// Making the binding name-based (not positional) means the shader author
// can reorder out declarations without breaking framebuffer setup code.

using Silk.NET.OpenGL;
using System.Collections.Generic;

namespace Geode.Rendering.Shaders
{
    /// <summary>
    /// Lazy, cached lookup of <c>glGetFragDataLocation</c> for a linked shader program.
    /// Maps each fragment shader <c>out</c> variable name to the color attachment
    /// index it writes into.
    /// </summary>
    public sealed class FragmentOutputs
    {
        private readonly GL _gl;
        private readonly uint _program;
        private readonly Dictionary<string, int> _cache = new();

        internal FragmentOutputs(GL gl, uint program)
        {
            _gl = gl;
            _program = program;
        }

        /// <summary>
        /// Returns the color attachment index that the fragment shader's named
        /// <c>out</c> variable writes to. Cached after the first lookup.
        /// </summary>
        /// <param name="name">The <c>out</c> variable name as declared in GLSL.</param>
        /// <exception cref="System.Exception">
        /// Thrown when the fragment shader has no <c>out</c> with the given name
        /// (e.g. declared with a different name, or optimized away because it is
        /// never written).
        /// </exception>
        public int this[string name]
        {
            get
            {
                if (_cache.TryGetValue(name, out int cachedLocation))
                    return cachedLocation;

                int location = _gl.GetFragDataLocation(_program, name);
                if (location < 0)
                    throw new System.Exception(
                        $"Fragment shader has no active `out` named '{name}'.");

                _cache[name] = location;
                return location;
            }
        }
    }
}
