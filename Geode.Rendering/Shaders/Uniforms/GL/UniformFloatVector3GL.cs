using Geode.Rendering.Uniforms;
using System.Numerics;

namespace Geode.Rendering.Shaders.Uniforms.GL
{
    /// <summary>
    /// A vec3 uniform backed by <see cref="Vector3"/>.
    /// Common uses: camera position, light direction, colors as RGB, world-space
    /// positions at non-planetary scale. Uploaded via <c>glProgramUniform3f</c>.
    /// </summary>
    public sealed class UniformFloatVector3GL : Uniform<Vector3>
    {
        private readonly Silk.NET.OpenGL.GL _gl;
        private readonly uint _program;
        private readonly int _location;

        /// <summary>
        /// Create a vec3 uniform wrapper.
        /// </summary>
        /// <param name="gl">The Silk.NET OpenGL context.</param>
        /// <param name="program">Handle of the owning shader program.</param>
        /// <param name="name">GLSL uniform name.</param>
        /// <param name="location">Location returned by <c>glGetUniformLocation</c>.</param>
        /// <param name="observer">The owning <see cref="ShaderProgram"/>, which is notified when the cached value changes.</param>
        public UniformFloatVector3GL(Silk.NET.OpenGL.GL gl, uint program,
            string name, int location, ICleanableObserver observer)
            : base(name, UniformType.FloatVector3, observer)
        {
            _gl = gl;
            _program = program;
            _location = location;
        }

        /// <summary>
        /// Flush the cached vec3 value to the GPU via <c>glProgramUniform3f</c>.
        /// </summary>
        public override void Clean()
        {
            Vector3 v = CurrentValue;
            _gl.ProgramUniform3(_program, _location, v.X, v.Y, v.Z);
            MarkClean();
        }
    }
}
