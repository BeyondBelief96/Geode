using Geode.Rendering.State;
using Geode.Rendering.Uniforms;
using System.Numerics;

namespace Geode.Rendering.Shaders.Uniforms.DrawAutomaticUniforms
{
    /// <summary>
    /// Factory for <c>geode_perspectiveMatrix</c> (mat4).
    /// The projection transform alone -- moves eye-space geometry into clip
    /// space. Needed by shaders that reconstruct depth in linear eye-space
    /// units (logarithmic depth, reversed-Z diagnostic), or that do any
    /// projection-aware screen-space work.
    /// </summary>
    public sealed class PerspectiveMatrixUniformFactory : DrawAutomaticUniformFactory
    {
        public override string Name => "geode_perspectiveMatrix";
        public override UniformType DataType => UniformType.FloatMatrix44;
        public override DrawAutomaticUniform Create(Uniform uniform) =>
            new PerspectiveMatrixUniform((Uniform<Matrix4x4>)uniform);
    }

    internal sealed class PerspectiveMatrixUniform : DrawAutomaticUniform
    {
        private readonly Uniform<Matrix4x4> _uniform;

        public PerspectiveMatrixUniform(Uniform<Matrix4x4> uniform)
        {
            _uniform = uniform;
        }

        public override void Set(RenderContext ctx, DrawState ds, SceneState ss)
        {
            _uniform.Value = ss.PerspectiveMatrix;
        }
    }
}
