using Geode.Rendering.State;
using Geode.Rendering.Uniforms;
using System.Numerics;

namespace Geode.Rendering.Shaders.Uniforms.DrawAutomaticUniforms
{
    /// <summary>
    /// Factory for <c>geode_modelMatrix</c> (mat4).
    /// The model transform alone -- moves object-local geometry into world
    /// space. Needed by shaders that do world-space lighting, or that pass
    /// world-space positions to the fragment shader for sampling
    /// world-space textures (e.g., environment maps).
    /// </summary>
    public sealed class ModelMatrixUniformFactory : DrawAutomaticUniformFactory
    {
        public override string Name => "geode_modelMatrix";
        public override UniformType DataType => UniformType.FloatMatrix44;
        public override DrawAutomaticUniform Create(Uniform uniform) =>
            new ModelMatrixUniform((Uniform<Matrix4x4>)uniform);
    }

    internal sealed class ModelMatrixUniform : DrawAutomaticUniform
    {
        private readonly Uniform<Matrix4x4> _uniform;

        public ModelMatrixUniform(Uniform<Matrix4x4> uniform)
        {
            _uniform = uniform;
        }

        public override void Set(RenderContext ctx, DrawState ds, SceneState ss)
        {
            _uniform.Value = ss.ModelMatrix;
        }
    }
}
