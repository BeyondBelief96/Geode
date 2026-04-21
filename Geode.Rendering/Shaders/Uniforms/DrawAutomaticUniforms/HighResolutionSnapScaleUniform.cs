using Geode.Rendering.Uniforms;

namespace Geode.Rendering.Shaders.Uniforms.DrawAutomaticUniforms
{
    /// <summary>
    /// Factory for <c>geode_highResolutionSnapScale</c> (float).
    /// Sub-pixel snap factor used by techniques that require pixel-aligned
    /// geometry to avoid shimmering -- high-quality billboard text,
    /// pixel-accurate line rendering, etc. The shader multiplies
    /// clip-space coordinates by this factor, rounds, and divides back.
    /// Default 1 is a no-op snap.
    /// </summary>
    public sealed class HighResolutionSnapScaleUniformFactory : DrawAutomaticUniformFactory
    {
        public override string Name => "geode_highResolutionSnapScale";
        public override UniformType DataType => UniformType.Float;
        public override DrawAutomaticUniform Create(Uniform uniform) =>
            new HighResolutionSnapScaleUniform((Uniform<float>)uniform);
    }

    internal sealed class HighResolutionSnapScaleUniform : DrawAutomaticUniform
    {
        private readonly Uniform<float> _uniform;

        public HighResolutionSnapScaleUniform(Uniform<float> uniform)
        {
            _uniform = uniform;
        }

        public override void Set(RenderContext ctx, DrawState ds, SceneState ss)
        {
            _uniform.Value = ss.HighResolutionSnapScale;
        }
    }
}
