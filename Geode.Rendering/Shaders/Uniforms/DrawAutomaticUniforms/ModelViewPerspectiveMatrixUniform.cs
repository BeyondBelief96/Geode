using Geode.Core;
using Geode.Rendering.State;
using Geode.Rendering.Uniforms;

namespace Geode.Rendering.Shaders.Uniforms.DrawAutomaticUniforms
{
    /// <summary>
    /// Factory for <c>geode_modelViewPerspectiveMatrix</c> (mat4).
    /// The combined Perspective * View * Model matrix -- the single transform
    /// a vertex shader usually needs to move a position from model space to
    /// clip space. Most Geode shaders declare only this uniform and leave the
    /// individual matrices for shaders that need them (lighting in eye space,
    /// depth reconstruction, etc.).
    /// </summary>
    public sealed class ModelViewPerspectiveMatrixUniformFactory : DrawAutomaticUniformFactory
    {
        public override string Name => "geode_modelViewPerspectiveMatrix";
        public override UniformType DataType => UniformType.FloatMatrix44;
        public override DrawAutomaticUniform Create(Uniform uniform) =>
            new ModelViewPerspectiveMatrixUniform((Uniform<Matrix4F>)uniform);
    }

    internal sealed class ModelViewPerspectiveMatrixUniform : DrawAutomaticUniform
    {
        private readonly Uniform<Matrix4F> _uniform;

        public ModelViewPerspectiveMatrixUniform(Uniform<Matrix4F> uniform)
        {
            _uniform = uniform;
        }

        public override void Set(RenderContext ctx, DrawState ds, SceneState ss)
        {
            _uniform.Value = ss.ModelViewPerspectiveMatrix;
        }
    }
}
