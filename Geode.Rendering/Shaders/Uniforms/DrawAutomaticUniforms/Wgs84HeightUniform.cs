using Geode.Core;
using Geode.Rendering.State;
using Geode.Rendering.Uniforms;

namespace Geode.Rendering.Shaders.Uniforms.DrawAutomaticUniforms
{
    /// <summary>
    /// Factory for <c>geode_wgs84Height</c> (float).
    /// Camera altitude above the WGS84 ellipsoid surface, in meters.
    /// Used by globe-rendering shaders for atmospheric scattering depth,
    /// LOD selection, and fog density -- anything that varies with how
    /// close the camera is to the surface. Cast to float is lossy for
    /// altitudes over ~10 km, but at those distances the remaining
    /// precision (~cm) exceeds what the effects need.
    /// </summary>
    public sealed class Wgs84HeightUniformFactory : DrawAutomaticUniformFactory
    {
        public override string Name => "geode_wgs84Height";
        public override UniformType DataType => UniformType.Float;
        public override DrawAutomaticUniform Create(Uniform uniform) =>
            new Wgs84HeightUniform((Uniform<float>)uniform);
    }

    internal sealed class Wgs84HeightUniform : DrawAutomaticUniform
    {
        private readonly Uniform<float> _uniform;

        public Wgs84HeightUniform(Uniform<float> uniform)
        {
            _uniform = uniform;
        }

        public override void Set(RenderContext ctx, DrawState ds, SceneState ss)
        {
            _uniform.Value = (float)ss.Camera.Height(Ellipsoid.Wgs84);
        }
    }
}
