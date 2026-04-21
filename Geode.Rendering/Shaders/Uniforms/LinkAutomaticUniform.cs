using Geode.Rendering.Uniforms;

namespace Geode.Rendering.Shaders.Uniforms
{
    /// <summary>
    /// A uniform whose value is determined solely by the shader it appears in,
    /// set once at link time. Contrasts with <see cref="DrawAutomaticUniform"/>
    /// which is set before every draw.
    /// </summary>
    public abstract class LinkAutomaticUniform
    {
        /// <summary>
        /// Gets the name of the uniform.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The GLSL type the uniform must have.
        /// </summary>
        public abstract UniformType DataType { get; }
        
        /// <summary>
        /// Sets the value of the specified uniform variable for use in a shader program.
        /// </summary>
        /// <param name="uniform">The uniform variable whose value is to be set. Cannot be null.</param>
        public abstract void Set(Uniform uniform);
    }
}
