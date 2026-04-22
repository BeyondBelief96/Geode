using Geode.Rendering.State;
using Geode.Rendering.Uniforms;
using System.Numerics;

namespace Geode.Rendering.Shaders.Uniforms.DrawAutomaticUniforms
{
    /// <summary>
    /// Factory for <c>geode_sunPosition</c> (vec3).
    /// Sun position in world (ECEF) space, single precision. At Earth's
    /// distance from the Sun (~1.5e11 m), the direction from any point on
    /// the globe to the Sun is effectively constant, so shaders typically
    /// normalize this to a direction. Cast to float is lossy at that scale;
    /// normalize-first in the shader rather than comparing magnitudes.
    /// </summary>
    public sealed class SunPositionUniformFactory : DrawAutomaticUniformFactory
    {
        public override string Name => "geode_sunPosition";
        public override UniformType DataType => UniformType.FloatVector3;
        public override DrawAutomaticUniform Create(Uniform uniform) =>
            new SunPositionUniform((Uniform<Vector3>)uniform);
    }

    internal sealed class SunPositionUniform : DrawAutomaticUniform
    {
        private readonly Uniform<Vector3> _uniform;

        public SunPositionUniform(Uniform<Vector3> uniform)
        {
            _uniform = uniform;
        }

        public override void Set(RenderContext ctx, DrawState ds, SceneState ss)
        {
            _uniform.Value = ss.SunPositionFloat;
        }
    }
}
