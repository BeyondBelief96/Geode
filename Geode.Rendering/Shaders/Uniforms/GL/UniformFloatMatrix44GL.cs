using Geode.Rendering.Uniforms;
using System.Numerics;

namespace Geode.Rendering.Shaders.Uniforms.GL
{
    /// <summary>
    /// A mat4 uniform. Value is a row-major System.Numerics.Matrix4x4;
    /// uploaded with transpose=true so glsl sees it in its native column-major layout.
    /// </summary>
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
            _gl.ProgramUniformMatrix4(_program, _location, 1, true, (float*)&v);
            MarkClean();
        }
    }
}
