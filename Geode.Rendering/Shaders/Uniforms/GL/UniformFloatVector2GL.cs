using Geode.Rendering.Uniforms;
using System.Numerics;

namespace Geode.Rendering.Shaders.Uniforms.GL
{
    /// <summary>
    /// A vec2 uniform backed by <see cref="Vector2"/>.
    /// Uploaded via <c>glProgramUniform2f</c> so <see cref="Clean"/> works
    /// regardless of which program is currently bound to the GL context.
    /// </summary>
    public sealed class UniformFloatVector2GL : Uniform<Vector2>
    {
        private readonly Silk.NET.OpenGL.GL _gl;
        private readonly uint _program;
        private readonly int _location;

        /// <summary>
        /// Create a vec2 uniform wrapper.
        /// </summary>
        /// <param name="gl">The Silk.NET OpenGL context.</param>
        /// <param name="program">Handle of the owning shader program.</param>
        /// <param name="name">GLSL uniform name.</param>
        /// <param name="location">Location returned by <c>glGetUniformLocation</c>.</param>
        /// <param name="observer">The owning <see cref="ShaderProgram"/>, which is notified when the cached value changes.</param>
        public UniformFloatVector2GL(Silk.NET.OpenGL.GL gl, uint program,
            string name, int location, ICleanableObserver observer)
            : base(name, UniformType.FloatVector2, observer)
        {
            _gl = gl;
            _program = program;
            _location = location;
        }

        /// <summary>
        /// Flush the cached vec2 value to the GPU via <c>glProgramUniform2f</c>.
        /// </summary>
        public override void Clean()
        {
            Vector2 v = CurrentValue;
            _gl.ProgramUniform2(_program, _location, v.X, v.Y);
            MarkClean();
        }
    }
}
