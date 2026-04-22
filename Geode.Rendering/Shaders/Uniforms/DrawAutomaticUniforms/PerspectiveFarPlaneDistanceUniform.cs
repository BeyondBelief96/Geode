using Geode.Rendering.State;
using Geode.Rendering.Uniforms;

namespace Geode.Rendering.Shaders.Uniforms.DrawAutomaticUniforms
{
    /// <summary>
    /// Factory for <c>geode_perspectiveFarPlaneDistance</c> (float).
    /// Distance from the eye to the far clipping plane, in world units.
    /// Paired with <c>geode_perspectiveNearPlaneDistance</c> to linearize
    /// sampled depth-buffer values in screen-space shaders.
    /// </summary>
    public sealed class PerspectiveFarPlaneDistanceUniformFactory : DrawAutomaticUniformFactory
    {
        public override string Name => "geode_perspectiveFarPlaneDistance";
        public override UniformType DataType => UniformType.Float;
        public override DrawAutomaticUniform Create(Uniform uniform) =>
            new PerspectiveFarPlaneDistanceUniform((Uniform<float>)uniform);
    }

    internal sealed class PerspectiveFarPlaneDistanceUniform : DrawAutomaticUniform
    {
        private readonly Uniform<float> _uniform;

        public PerspectiveFarPlaneDistanceUniform(Uniform<float> uniform)
        {
            _uniform = uniform;
        }

        public override void Set(RenderContext ctx, DrawState ds, SceneState ss)
        {
            _uniform.Value = (float)ss.Camera.FarPlane;
        }
    }
}
