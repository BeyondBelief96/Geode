using Geode.Rendering.Uniforms;

namespace Geode.Rendering.Shaders.Uniforms.GL
{
    public sealed class UniformFloatGL : Uniform<float>
    {
        private Silk.NET.OpenGL.GL _gl;
        private readonly uint _program;
        private readonly int _location;

        public UniformFloatGL(Silk.NET.OpenGL.GL gl, uint program, string name, int location, ICleanableObserver observer) : base(name, UniformType.Float, observer)
        {
            _gl = gl;
            _program = program;
            _location = location;
        }

        public override void Clean()
        {
            float v = CurrentValue;
            _gl.ProgramUniform1(_program, _location, v);
            MarkClean();
            
        }
    }
}
