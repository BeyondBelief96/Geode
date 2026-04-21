using Geode.Rendering.Uniforms;
using System.Numerics;

namespace Geode.Rendering.Shaders.Uniforms.GL
{
    /// <summary>
    /// A vec4 uniform backed by <see cref="Vector4"/>.
    /// Common uses: RGBA color, homogeneous position, the packed
    /// diffuse/specular/ambient/shininess material vector,
    /// viewport (x, y, width, height). Uploaded via <c>glProgramUniform4f</c>.
    /// </summary>
    public sealed class UniformFloatVector4GL : Uniform<Vector4>
    {
        private readonly Silk.NET.OpenGL.GL _gl;
        private readonly uint _program;
        private readonly int _location;

        /// <summary>
        /// Create a vec4 uniform wrapper.
        /// </summary>
        /// <param name="gl">The Silk.NET OpenGL context.</param>
        /// <param name="program">Handle of the owning shader program.</param>
        /// <param name="name">GLSL uniform name.</param>
        /// <param name="location">Location returned by <c>glGetUniformLocation</c>.</param>
        /// <param name="observer">The owning <see cref="ShaderProgram"/>, which is notified when the cached value changes.</param>
        public UniformFloatVector4GL(Silk.NET.OpenGL.GL gl, uint program,
            string name, int location, ICleanableObserver observer)
            : base(name, UniformType.FloatVector4, observer)
        {
            _gl = gl;
            _program = program;
            _location = location;
        }

        /// <summary>
        /// Flush the cached vec4 value to the GPU via <c>glProgramUniform4f</c>.
        /// </summary>
        public override void Clean()
        {
            Vector4 v = CurrentValue;
            _gl.ProgramUniform4(_program, _location, v.X, v.Y, v.Z, v.W);
            MarkClean();
        }
    }
}
