using Geode.Rendering.State;
using Geode.Rendering.Uniforms;
using System.Numerics;

namespace Geode.Rendering.Shaders.Uniforms.DrawAutomaticUniforms
{
    /// <summary>
    /// Factory for <c>geode_cameraEye</c> (vec3).
    /// Camera position in world space (ECEF for the globe), single precision.
    /// Used by shaders that need the view vector (specular lighting,
    /// atmospheric scattering, ray-cast globes). At planetary scale, prefer
    /// the RTE/DSFP variants <c>geode_cameraEyeHigh</c> /
    /// <c>geode_cameraEyeLow</c> introduced in Section 27.
    /// </summary>
    public sealed class CameraEyeUniformFactory : DrawAutomaticUniformFactory
    {
        public override string Name => "geode_cameraEye";
        public override UniformType DataType => UniformType.FloatVector3;
        public override DrawAutomaticUniform Create(Uniform uniform) =>
            new CameraEyeUniform((Uniform<Vector3>)uniform);
    }

    internal sealed class CameraEyeUniform : DrawAutomaticUniform
    {
        private readonly Uniform<Vector3> _uniform;

        public CameraEyeUniform(Uniform<Vector3> uniform)
        {
            _uniform = uniform;
        }

        public override void Set(RenderContext ctx, DrawState ds, SceneState ss)
        {
            _uniform.Value = ss.CameraEyeFloat;
        }
    }
}
