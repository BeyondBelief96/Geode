using Geode.Rendering.Uniforms;

namespace Geode.Rendering.Shaders.Uniforms.GL
{
    /// <summary>
    /// An ivec2 uniform backed by a <see cref="ValueTuple{T1, T2}"/> of
    /// <see cref="int"/>. Common uses: texel-space coordinates, tile indices,
    /// viewport integer dimensions. Uploaded via <c>glProgramUniform2i</c>.
    /// <para>
    /// A tuple is used instead of a dedicated struct to avoid introducing a
    /// bespoke <c>Vector2i</c> type. <see cref="EqualityComparer{T}.Default"/>
    /// handles value-equality for <see cref="ValueTuple{T1, T2}"/> correctly,
    /// so the dirty-check in <see cref="Uniform{T}.Value"/> behaves as expected.
    /// </para>
    /// </summary>
    public sealed class UniformIntVector2GL : Uniform<(int, int)>
    {
        private readonly Silk.NET.OpenGL.GL _gl;
        private readonly uint _program;
        private readonly int _location;

        /// <summary>
        /// Create an ivec2 uniform wrapper.
        /// </summary>
        /// <param name="gl">The Silk.NET OpenGL context.</param>
        /// <param name="program">Handle of the owning shader program.</param>
        /// <param name="name">GLSL uniform name.</param>
        /// <param name="location">Location returned by <c>glGetUniformLocation</c>.</param>
        /// <param name="observer">The owning <see cref="ShaderProgram"/>, which is notified when the cached value changes.</param>
        public UniformIntVector2GL(Silk.NET.OpenGL.GL gl, uint program,
            string name, int location, ICleanableObserver observer)
            : base(name, UniformType.IntVector2, observer)
        {
            _gl = gl;
            _program = program;
            _location = location;
        }

        /// <summary>
        /// Flush the cached ivec2 value to the GPU via <c>glProgramUniform2i</c>.
        /// </summary>
        public override void Clean()
        {
            (int x, int y) = CurrentValue;
            _gl.ProgramUniform2(_program, _location, x, y);
            MarkClean();
        }
    }
}
