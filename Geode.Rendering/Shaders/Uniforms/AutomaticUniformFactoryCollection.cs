using Geode.Rendering.Shaders.Uniforms.DrawAutomaticUniforms;
using Geode.Rendering.Shaders.Uniforms.LinkAutomaticUniforms;
using System.Collections.Generic;

namespace Geode.Rendering.Shaders.Uniforms
{
    /// <summary>
    /// Process-wide registry of link-automatic uniforms and draw-automatic factories.
    /// Populated at first access; consulted by ShaderProgram at link time.
    /// </summary>
    public static class AutomaticUniformFactoryCollection
    {
        /// <summary>
        /// Number of named texture-unit link-automatic uniforms to register --
        /// one per GLSL sampler binding (geode_texture0 .. geode_textureN-1).
        /// OpenGL 4.6 guarantees at least 16 combined texture units, so eight is a
        /// safe default for named bindings; raise if a shader needs more.
        /// </summary>
        private const int TextureUnitBindings = 8;

        private static readonly Dictionary<string, LinkAutomaticUniform> _link = new();
        private static readonly Dictionary<string, DrawAutomaticUniformFactory> _draw = new();

        static AutomaticUniformFactoryCollection()
        {
            // --- Link-automatic uniforms (set once at link time) ------------

            // Bind geode_texture0..geode_texture{N-1} to texture units 0..N-1.
            // Any shader that declares `uniform sampler2D geode_textureN;` gets the
            // fixed unit binding at link time without the application ever touching it.
            for (int i = 0; i < TextureUnitBindings; i++)
            {
                RegisterLink(new TextureUniform(i));
            }

            // --- Draw-automatic factories (set before every draw) ------------

            // Transforms
            RegisterDraw(new ModelViewPerspectiveMatrixUniformFactory());
            RegisterDraw(new ViewMatrixUniformFactory());
            RegisterDraw(new PerspectiveMatrixUniformFactory());
            RegisterDraw(new ModelMatrixUniformFactory());
            RegisterDraw(new NormalMatrixUniformFactory());

            // Camera / lighting
            RegisterDraw(new CameraEyeUniformFactory());
            RegisterDraw(new CameraLightPositionUniformFactory());
            RegisterDraw(new SunPositionUniformFactory());
            RegisterDraw(new DiffuseSpecularAmbientShininessUniformFactory());

            // Viewport / screen-space
            RegisterDraw(new ViewportUniformFactory());
            RegisterDraw(new InverseViewportUniformFactory());
            RegisterDraw(new PixelSizePerDistanceUniformFactory());

            // WGS84 camera height (for LOD selection on the globe)
            RegisterDraw(new Wgs84HeightUniformFactory());

            // Near/far planes (for depth buffer math)
            RegisterDraw(new PerspectiveNearPlaneDistanceUniformFactory());
            RegisterDraw(new PerspectiveFarPlaneDistanceUniformFactory());

            // DSFP RTE (Section 27 populates)
            // DSFP log depth (Section 28 populates)
        }

        public static void RegisterLink(LinkAutomaticUniform u) => _link[u.Name] = u;
        public static void RegisterDraw(DrawAutomaticUniformFactory f) => _draw[f.Name] = f;

        /// <summary>
        /// Look up a link-automatic uniform by GLSL name.
        /// Returns <c>true</c> and the registered instance if one is registered under
        /// <paramref name="name"/>; otherwise <c>false</c> with <paramref name="u"/> null.
        /// </summary>
        public static bool TryGetLink(string name, out LinkAutomaticUniform? u) => _link.TryGetValue(name, out u);

        /// <summary>
        /// Look up a draw-automatic factory by GLSL name.
        /// Returns <c>true</c> and the registered factory if one is registered under
        /// <paramref name="name"/>; otherwise <c>false</c> with <paramref name="f"/> null.
        /// </summary>
        public static bool TryGetDrawFactory(string name, out DrawAutomaticUniformFactory? f) => _draw.TryGetValue(name, out f);
    }
}
