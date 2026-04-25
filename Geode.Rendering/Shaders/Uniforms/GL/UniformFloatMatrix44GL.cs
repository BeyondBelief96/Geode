using Geode.Rendering.Uniforms;
using System.Numerics;

namespace Geode.Rendering.Shaders.Uniforms.GL
{
    /// <summary>
    /// A mat4 uniform.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="System.Numerics.Matrix4x4"/> is stored in row-major memory and
    /// uses row-vector convention (translation lives in row 3: M41, M42, M43).
    /// GLSL uses column-vector convention (translation in column 3) and stores
    /// matrices column-major.
    /// </para>
    /// <para>
    /// We upload with <c>transpose = GL_FALSE</c>, which tells OpenGL to read
    /// our row-major bytes as if they were column-major. The net effect is a
    /// transpose: GLSL's column 0 becomes our row 0 (etc.), so translation
    /// lands in GLSL's column 3 — exactly where <c>mat * vec</c> needs it.
    /// </para>
    /// <para>
    /// Uploading with <c>transpose = GL_TRUE</c> (the obvious-looking choice
    /// for "this is row-major, please transpose") would PRESERVE the row
    /// structure, leaving translation stuck in row 3 — which silently kills
    /// the translation component when GLSL multiplies <c>mat * vec4(p, 1)</c>.
    /// </para>
    /// </remarks>
    public sealed class UniformFloatMatrix44GL : Uniform<Matrix4x4>
    {
        private readonly Silk.NET.OpenGL.GL _gl;
        private readonly uint _program;
        private readonly int _location;

        public UniformFloatMatrix44GL(Silk.NET.OpenGL.GL gl, uint program,
            string name, int location, ICleanableObserver observer)
            : base(name, UniformType.FloatMatrix44, observer)
        {
            _gl = gl;
            _program = program;
            _location = location;
        }

        public override unsafe void Clean()
        {
            Matrix4x4 v = CurrentValue;
            _gl.ProgramUniformMatrix4(_program, _location, 1, false, (float*)&v);
            MarkClean();
        }
    }
}
