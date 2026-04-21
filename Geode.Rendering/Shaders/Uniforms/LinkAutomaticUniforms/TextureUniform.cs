using Geode.Rendering.Uniforms;

namespace Geode.Rendering.Shaders.Uniforms.LinkAutomaticUniforms
{
    public sealed class TextureUniform : LinkAutomaticUniform
    {
        private readonly int _textureUnit;
        private readonly string _name;

        public TextureUniform(int textureUnit)
        {
            _textureUnit = textureUnit;
            _name = $"geode_texture{textureUnit}";
        }

        public override string Name => _name;
        public override UniformType DataType => UniformType.Sampler2D;

        public override void Set(Uniform uniform)
        {
            ((Uniform<int>)uniform).Value = _textureUnit;
        }
    }
}
