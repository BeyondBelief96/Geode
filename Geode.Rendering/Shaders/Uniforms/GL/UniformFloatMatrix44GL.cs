using Geode.Core;
using Geode.Rendering.Uniforms;

namespace Geode.Rendering.Shaders.Uniforms.GL
{
    /// <summary>
    /// A <c>mat4</c> uniform backed by <see cref="Matrix4F"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Matrix4F"/> is stored column-major with column-vector
    /// convention — exactly what GLSL expects. The struct's bytes can be fed
    /// straight to <c>glProgramUniformMatrix4fv</c> with
    /// <c>transpose = GL_FALSE</c>, no per-upload conversion.
    /// </para>
    /// <para>
    /// User code that wants to assign from a <see cref="System.Numerics.Matrix4x4"/>
    /// should convert via <see cref="Matrix4F.FromSystemNumerics"/> — that is
    /// where the row-vector ↔ column-vector transpose happens, once at the
    /// boundary instead of every draw.
    /// </para>
    /// </remarks>
    public sealed class UniformFloatMatrix44GL : Uniform<Matrix4F>
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
            Matrix4F v = CurrentValue;
            // Direct upload: struct memory is already column-major.
            _gl.ProgramUniformMatrix4(_program, _location, 1, false, (float*)&v);
            MarkClean();
        }
    }
}
