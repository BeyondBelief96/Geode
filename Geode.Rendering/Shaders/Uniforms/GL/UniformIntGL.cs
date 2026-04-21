using Geode.Rendering.Uniforms;

namespace Geode.Rendering.Shaders.Uniforms.GL
{
    /// <summary>
    /// An int uniform -- also the backing type for <b>every</b> sampler variant
    /// (<c>sampler2D</c>, <c>sampler3D</c>, <c>samplerCube</c>, <c>sampler2DArray</c>,
    /// <c>sampler2DShadow</c>, etc.). In GLSL a sampler uniform holds the
    /// texture-unit index it reads from, and GL exposes that as an <c>int</c>
    /// uploaded via <c>glProgramUniform1i</c>.
    /// <para>
    /// <see cref="Datatype"/> reflects the GLSL type actually declared in the
    /// shader (plain <c>int</c>, or one of the sampler variants), so shader
    /// validation against a factory's <see cref="UniformType"/> still works.
    /// The <see cref="Clean"/> call is identical regardless.
    /// </para>
    /// </summary>
    public sealed class UniformIntGL : Uniform<int>
    {
        private readonly Silk.NET.OpenGL.GL _gl;
        private readonly uint _program;
        private readonly int _location;

        /// <summary>
        /// Create an int / sampler uniform wrapper.
        /// </summary>
        /// <param name="gl">The Silk.NET OpenGL context.</param>
        /// <param name="program">Handle of the owning shader program.</param>
        /// <param name="name">GLSL uniform name.</param>
        /// <param name="location">Location returned by <c>glGetUniformLocation</c>.</param>
        /// <param name="datatype">
        /// The actual GLSL type as reported by <c>glGetActiveUniform</c> --
        /// <see cref="UniformType.Int"/> for an <c>int</c> uniform, or one of the
        /// <c>Sampler*</c> values for sampler uniforms. Preserving the real type
        /// lets automatic-uniform factories (e.g. <c>TextureUniform</c>) validate
        /// that the shader declared what they expect.
        /// </param>
        /// <param name="observer">The owning <see cref="ShaderProgram"/>, which is notified when the cached value changes.</param>
        public UniformIntGL(Silk.NET.OpenGL.GL gl, uint program,
            string name, int location, UniformType datatype, ICleanableObserver observer)
            : base(name, datatype, observer)
        {
            _gl = gl;
            _program = program;
            _location = location;
        }

        /// <summary>
        /// Flush the cached int value to the GPU via <c>glProgramUniform1i</c>.
        /// </summary>
        public override void Clean()
        {
            _gl.ProgramUniform1(_program, _location, CurrentValue);
            MarkClean();
        }
    }
}
