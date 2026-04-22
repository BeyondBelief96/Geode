using Geode.Rendering.State;
using Geode.Rendering.Uniforms;
using System.Numerics;

namespace Geode.Rendering.Shaders.Uniforms.DrawAutomaticUniforms
{
    /// <summary>
    /// Factory for <c>geode_viewport</c> (vec4).
    /// Current viewport rectangle as <c>(x, y, width, height)</c>. Consumers
    /// typically care about <c>(width, height)</c> for screen-space math;
    /// the origin is included for pipelines that render to offset viewports
    /// (e.g., split-screen). Must be kept in sync with <c>glViewport</c>.
    /// </summary>
    public sealed class ViewportUniformFactory : DrawAutomaticUniformFactory
    {
        public override string Name => "geode_viewport";
        public override UniformType DataType => UniformType.FloatVector4;
        public override DrawAutomaticUniform Create(Uniform uniform) =>
            new ViewportUniform((Uniform<Vector4>)uniform);
    }

    internal sealed class ViewportUniform : DrawAutomaticUniform
    {
        private readonly Uniform<Vector4> _uniform;

        public ViewportUniform(Uniform<Vector4> uniform)
        {
            _uniform = uniform;
        }

        public override void Set(RenderContext ctx, DrawState ds, SceneState ss)
        {
            _uniform.Value = ss.Viewport;
        }
    }
}
