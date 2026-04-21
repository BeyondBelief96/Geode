using Geode.Rendering.Uniforms;
using Silk.NET.Maths;

namespace Geode.Rendering.Shaders.Uniforms.GL
{
    /// <summary>
    /// A mat3 uniform backed by <see cref="Matrix3X3{T}"/> of <see cref="float"/>.
    /// <para>
    /// Most commonly used as the normal matrix -- the inverse-transpose of the
    /// upper 3x3 of the model-view matrix -- which transforms normals from
    /// model space to eye space without introducing scale distortion.
    /// </para>
    /// <para>
    /// <see cref="System.Numerics"/> does not ship a 3x3 matrix type; Silk.NET's
    /// generic <see cref="Matrix3X3{T}"/> is used instead for consistency with
    /// the rest of the Silk.NET-based types in this project.
    /// </para>
    /// <para>
    /// The matrix is uploaded with <c>transpose = true</c> so GLSL sees it in
    /// its native column-major layout. Silk.NET / System.Numerics matrices are
    /// row-major; the flag lets OpenGL do the transpose during upload rather
    /// than the caller swapping fields in C#.
    /// </para>
    /// </summary>
    public sealed class UniformFloatMatrix33GL : Uniform<Matrix3X3<float>>
    {
        private readonly Silk.NET.OpenGL.GL _gl;
        private readonly uint _program;
        private readonly int _location;

        /// <summary>
        /// Create a mat3 uniform wrapper.
        /// </summary>
        /// <param name="gl">The Silk.NET OpenGL context.</param>
        /// <param name="program">Handle of the owning shader program.</param>
        /// <param name="name">GLSL uniform name.</param>
        /// <param name="location">Location returned by <c>glGetUniformLocation</c>.</param>
        /// <param name="observer">The owning <see cref="ShaderProgram"/>, which is notified when the cached value changes.</param>
        public UniformFloatMatrix33GL(Silk.NET.OpenGL.GL gl, uint program,
            string name, int location, ICleanableObserver observer)
            : base(name, UniformType.FloatMatrix33, observer)
        {
            _gl = gl;
            _program = program;
            _location = location;
        }

        /// <summary>
        /// Flush the cached 3x3 matrix to the GPU via <c>glProgramUniformMatrix3fv</c>.
        /// Uploads with <c>transpose = true</c> to convert from row-major CPU
        /// layout to column-major GLSL layout.
        /// </summary>
        public override unsafe void Clean()
        {
            Matrix3X3<float> m = CurrentValue;
            _gl.ProgramUniformMatrix3(_program, _location, 1, true, (float*)&m);
            MarkClean();
        }
    }
}
