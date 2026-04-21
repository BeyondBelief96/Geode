using Geode.Rendering.Uniforms;

namespace Geode.Rendering.Shaders.Uniforms
{
    /// <summary>
    /// Builds a <see cref="DrawAutomaticUniform"/> bound to a specific uniform
    /// in a specific shader program. Consulted at link time by ShaderProgram.
    /// </summary>
    public abstract class DrawAutomaticUniformFactory
    {
        /// <summary>
        /// The GLSL uniform name this factory handles (e.g. "geode_modelViewMatrix").
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The GLSL type the uniform must have. Used for link-time validation.
        /// </summary>
        public abstract UniformType DataType { get; }

        /// <summary>
        /// Creates a new instance of a DrawAutomaticUniform associated with the specified uniform.
        /// </summary>
        /// <param name="uniform">The uniform for which to create the automatic uniform binding. Cannot be null.</param>
        /// <returns>A DrawAutomaticUniform instance that manages the specified uniform.</returns>
        public abstract DrawAutomaticUniform Create(Uniform uniform);
    }
}
