using Geode.Rendering.State;
using Geode.Rendering.Uniforms;

namespace Geode.Rendering.Shaders.Uniforms.DrawAutomaticUniforms
{
    /// <summary>
    /// Factory for <c>geode_perspectiveNearPlaneDistance</c> (float).
    /// Distance from the eye to the near clipping plane, in world units.
    /// Needed by shaders that reconstruct linear eye-space depth from the
    /// sampled depth buffer (reversed-Z depth reconstruction,
    /// logarithmic-depth linearization, screen-space effects like SSAO).
    /// </summary>
    public sealed class PerspectiveNearPlaneDistanceUniformFactory : DrawAutomaticUniformFactory
    {
        public override string Name => "geode_perspectiveNearPlaneDistance";
        public override UniformType DataType => UniformType.Float;
        public override DrawAutomaticUniform Create(Uniform uniform) =>
            new PerspectiveNearPlaneDistanceUniform((Uniform<float>)uniform);
    }

    internal sealed class PerspectiveNearPlaneDistanceUniform : DrawAutomaticUniform
    {
        private readonly Uniform<float> _uniform;

        public PerspectiveNearPlaneDistanceUniform(Uniform<float> uniform)
        {
            _uniform = uniform;
        }

        public override void Set(RenderContext ctx, DrawState ds, SceneState ss)
        {
            _uniform.Value = (float)ss.Camera.NearPlane;
        }
    }
}
