using Geode.Rendering.Uniforms;

namespace Geode.Rendering.Shaders.Uniforms.DrawAutomaticUniforms
{
    /// <summary>
    /// Factory for <c>geode_pixelSizePerDistance</c> (float).
    /// Screen-space size (in pixels) of one world unit at a distance of one
    /// world unit from the camera. Used by LOD selection shaders to compute
    /// the screen-space error of an approximation: a triangle edge's
    /// screen-space size is roughly <c>edgeWorldLength * pixelSizePerDistance
    /// / distanceToCamera</c>.
    /// </summary>
    public sealed class PixelSizePerDistanceUniformFactory : DrawAutomaticUniformFactory
    {
        public override string Name => "geode_pixelSizePerDistance";
        public override UniformType DataType => UniformType.Float;
        public override DrawAutomaticUniform Create(Uniform uniform) =>
            new PixelSizePerDistanceUniform((Uniform<float>)uniform);
    }

    internal sealed class PixelSizePerDistanceUniform : DrawAutomaticUniform
    {
        private readonly Uniform<float> _uniform;

        public PixelSizePerDistanceUniform(Uniform<float> uniform)
        {
            _uniform = uniform;
        }

        public override void Set(RenderContext ctx, DrawState ds, SceneState ss)
        {
            _uniform.Value = ss.PixelSizePerDistance;
        }
    }
}
