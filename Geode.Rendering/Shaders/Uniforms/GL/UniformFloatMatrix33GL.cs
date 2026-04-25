using Geode.Core;
using Geode.Rendering.Uniforms;

namespace Geode.Rendering.Shaders.Uniforms.GL
{
    /// <summary>
    /// A <c>mat3</c> uniform backed by <see cref="Matrix3F"/>. Same conventions
    /// as <see cref="UniformFloatMatrix44GL"/>: column-major storage,
    /// column-vector convention, uploaded with <c>transpose = GL_FALSE</c>.
    /// </summary>
    public sealed class UniformFloatMatrix33GL : Uniform<Matrix3F>
    {
        private readonly Silk.NET.OpenGL.GL _gl;
        private readonly uint _program;
        private readonly int _location;

        public UniformFloatMatrix33GL(Silk.NET.OpenGL.GL gl, uint program,
            string name, int location, ICleanableObserver observer)
            : base(name, UniformType.FloatMatrix33, observer)
        {
            _gl = gl;
            _program = program;
            _location = location;
        }

        public override unsafe void Clean()
        {
            Matrix3F m = CurrentValue;
            _gl.ProgramUniformMatrix3(_program, _location, 1, false, (float*)&m);
            MarkClean();
        }
    }
}
