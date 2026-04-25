using Geode.Core;
using Geode.Rendering.State;
using Geode.Rendering.Uniforms;

namespace Geode.Rendering.Shaders.Uniforms.DrawAutomaticUniforms
{
    /// <summary>
    /// Factory for <c>geode_viewMatrix</c> (mat4).
    /// The view transform alone -- moves world-space geometry into eye space.
    /// Needed by shaders that do lighting in eye space, or that need to
    /// reconstruct world-space positions from eye-space depth.
    /// </summary>
    public sealed class ViewMatrixUniformFactory : DrawAutomaticUniformFactory
    {
        public override string Name => "geode_viewMatrix";
        public override UniformType DataType => UniformType.FloatMatrix44;
        public override DrawAutomaticUniform Create(Uniform uniform) =>
            new ViewMatrixUniform((Uniform<Matrix4F>)uniform);
    }

    internal sealed class ViewMatrixUniform : DrawAutomaticUniform
    {
        private readonly Uniform<Matrix4F> _uniform;

        public ViewMatrixUniform(Uniform<Matrix4F> uniform)
        {
            _uniform = uniform;
        }

        public override void Set(RenderContext ctx, DrawState ds, SceneState ss)
        {
            _uniform.Value = ss.ViewMatrix;
        }
    }
}
