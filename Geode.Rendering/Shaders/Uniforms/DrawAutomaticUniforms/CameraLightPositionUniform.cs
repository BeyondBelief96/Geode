using Geode.Rendering.State;
using Geode.Rendering.Uniforms;
using System.Numerics;

namespace Geode.Rendering.Shaders.Uniforms.DrawAutomaticUniforms
{
    /// <summary>
    /// Factory for <c>geode_cameraLightPosition</c> (vec3).
    /// Position of a world-space light attached to the camera -- the
    /// "headlamp" / "miner's lamp" pattern. Setting this equal to
    /// <see cref="SceneState.CameraEyeFloat"/> avoids the specular term
    /// going dark on the unlit side of an object as the camera orbits it.
    /// </summary>
    public sealed class CameraLightPositionUniformFactory : DrawAutomaticUniformFactory
    {
        public override string Name => "geode_cameraLightPosition";
        public override UniformType DataType => UniformType.FloatVector3;
        public override DrawAutomaticUniform Create(Uniform uniform) =>
            new CameraLightPositionUniform((Uniform<Vector3>)uniform);
    }

    internal sealed class CameraLightPositionUniform : DrawAutomaticUniform
    {
        private readonly Uniform<Vector3> _uniform;

        public CameraLightPositionUniform(Uniform<Vector3> uniform)
        {
            _uniform = uniform;
        }

        public override void Set(RenderContext ctx, DrawState ds, SceneState ss)
        {
            _uniform.Value = ss.CameraLightPosition;
        }
    }
}
