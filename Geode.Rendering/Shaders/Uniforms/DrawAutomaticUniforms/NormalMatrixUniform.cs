using Geode.Core;
using Geode.Rendering.State;
using Geode.Rendering.Uniforms;

namespace Geode.Rendering.Shaders.Uniforms.DrawAutomaticUniforms
{
    /// <summary>
    /// Factory for <c>geode_normalMatrix</c> (mat3).
    /// The inverse-transpose of the upper 3x3 of the model-view matrix.
    /// Transforms normals from model space into eye space without the scale
    /// distortion a plain <c>mat3(modelView)</c> would introduce when the
    /// model-view has non-uniform scale.
    /// </summary>
    public sealed class NormalMatrixUniformFactory : DrawAutomaticUniformFactory
    {
        public override string Name => "geode_normalMatrix";
        public override UniformType DataType => UniformType.FloatMatrix33;
        public override DrawAutomaticUniform Create(Uniform uniform) =>
            new NormalMatrixUniform((Uniform<Matrix3F>)uniform);
    }

    internal sealed class NormalMatrixUniform : DrawAutomaticUniform
    {
        private readonly Uniform<Matrix3F> _uniform;

        public NormalMatrixUniform(Uniform<Matrix3F> uniform)
        {
            _uniform = uniform;
        }

        public override void Set(RenderContext ctx, DrawState ds, SceneState ss)
        {
            _uniform.Value = ss.NormalMatrix;
        }
    }
}
