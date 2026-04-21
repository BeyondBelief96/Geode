using Geode.Rendering.Uniforms;

namespace Geode.Rendering.Shaders.Uniforms.GL
{
    /// <summary>
    /// A GLSL <c>bool</c> uniform backed by a C# <see cref="bool"/>.
    /// <para>
    /// GLSL booleans are uploaded as integers (0 or 1) via
    /// <c>glProgramUniform1i</c> -- there is no dedicated <c>glUniform*b</c>
    /// entry point. The conversion happens inside <see cref="Clean"/>.
    /// </para>
    /// </summary>
    public sealed class UniformBoolGL : Uniform<bool>
    {
        private readonly Silk.NET.OpenGL.GL _gl;
        private readonly uint _program;
        private readonly int _location;

        /// <summary>
        /// Create a bool uniform wrapper.
        /// </summary>
        /// <param name="gl">The Silk.NET OpenGL context.</param>
        /// <param name="program">Handle of the owning shader program.</param>
        /// <param name="name">GLSL uniform name.</param>
        /// <param name="location">Location returned by <c>glGetUniformLocation</c>.</param>
        /// <param name="observer">The owning <see cref="ShaderProgram"/>, which is notified when the cached value changes.</param>
        public UniformBoolGL(Silk.NET.OpenGL.GL gl, uint program,
            string name, int location, ICleanableObserver observer)
            : base(name, UniformType.Bool, observer)
        {
            _gl = gl;
            _program = program;
            _location = location;
        }

        /// <summary>
        /// Flush the cached bool value to the GPU as an int (0 or 1) via <c>glProgramUniform1i</c>.
        /// </summary>
        public override void Clean()
        {
            _gl.ProgramUniform1(_program, _location, CurrentValue ? 1 : 0);
            MarkClean();
        }
    }
}
