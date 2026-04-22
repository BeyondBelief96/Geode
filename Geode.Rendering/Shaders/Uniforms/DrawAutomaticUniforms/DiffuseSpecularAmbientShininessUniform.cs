using Geode.Rendering.State;
using Geode.Rendering.Uniforms;
using System.Numerics;

namespace Geode.Rendering.Shaders.Uniforms.DrawAutomaticUniforms
{
    /// <summary>
    /// Factory for <c>geode_diffuseSpecularAmbientShininess</c> (vec4).
    /// Material parameters packed as <c>(diffuse, specular, ambient, shininess)</c>.
    /// The book packs four scalars into one vec4 uniform -- one GPU upload
    /// instead of four -- and Geode preserves that convention.
    /// </summary>
    public sealed class DiffuseSpecularAmbientShininessUniformFactory : DrawAutomaticUniformFactory
    {
        public override string Name => "geode_diffuseSpecularAmbientShininess";
        public override UniformType DataType => UniformType.FloatVector4;
        public override DrawAutomaticUniform Create(Uniform uniform) =>
            new DiffuseSpecularAmbientShininessUniform((Uniform<Vector4>)uniform);
    }

    internal sealed class DiffuseSpecularAmbientShininessUniform : DrawAutomaticUniform
    {
        private readonly Uniform<Vector4> _uniform;

        public DiffuseSpecularAmbientShininessUniform(Uniform<Vector4> uniform)
        {
            _uniform = uniform;
        }

        public override void Set(RenderContext ctx, DrawState ds, SceneState ss)
        {
            _uniform.Value = ss.DiffuseSpecularAmbientShininess;
        }
    }
}
