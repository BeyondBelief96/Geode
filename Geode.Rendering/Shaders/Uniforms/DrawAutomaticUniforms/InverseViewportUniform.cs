using Geode.Rendering.Uniforms;
using System.Numerics;

namespace Geode.Rendering.Shaders.Uniforms.DrawAutomaticUniforms
{
    /// <summary>
    /// Factory for <c>geode_inverseViewport</c> (vec4).
    /// Reciprocal of <see cref="ViewportUniform"/> -- <c>(1/x, 1/y, 1/width, 1/height)</c>
    /// -- so shaders can convert <c>gl_FragCoord</c> to normalized device
    /// coordinates with a multiply rather than a divide. Components for
    /// any zero fields of the viewport are zero (division-safe default).
    /// </summary>
    public sealed class InverseViewportUniformFactory : DrawAutomaticUniformFactory
    {
        public override string Name => "geode_inverseViewport";
        public override UniformType DataType => UniformType.FloatVector4;
        public override DrawAutomaticUniform Create(Uniform uniform) =>
            new InverseViewportUniform((Uniform<Vector4>)uniform);
    }

    internal sealed class InverseViewportUniform : DrawAutomaticUniform
    {
        private readonly Uniform<Vector4> _uniform;

        public InverseViewportUniform(Uniform<Vector4> uniform)
        {
            _uniform = uniform;
        }

        public override void Set(RenderContext ctx, DrawState ds, SceneState ss)
        {
            _uniform.Value = ss.InverseViewport;
        }
    }
}
