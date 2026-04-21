using System.Collections.Generic;

namespace Geode.Rendering.Shaders.Uniforms
{
    /// <summary>
    /// Named collection of <see cref="Uniform"/> objects owned by a <see cref="ShaderProgram"/>.
    /// Populated at link time by scanning active uniforms.
    /// </summary>
    public class UniformCollection
    {
        private readonly Dictionary<string, Uniform> _uniforms = new();

        /// <summary>
        /// Gets the uniform with the specified name.
        /// </summary>
        /// <remarks>Throws a KeyNotFoundException if a uniform with the specified name does not
        /// exist.</remarks>
        /// <param name="name">The name of the uniform to retrieve. Cannot be null.</param>
        /// <returns>The uniform associated with the specified name.</returns>
        public Uniform this[string name] => _uniforms[name];

        /// <summary>
        /// Determines whether a uniform with the specified name exists in the collection.
        /// </summary>
        /// <param name="name">The name of the uniform to locate. Cannot be null.</param>
        /// <returns>true if a uniform with the specified name exists; otherwise, false.</returns>
        public bool Contains(string name) => _uniforms.ContainsKey(name);

        /// <summary>
        /// Attempts to retrieve a uniform with the specified name.
        /// </summary>
        /// <param name="name">The name of the uniform to locate. Cannot be null.</param>
        /// <param name="uniform">When this method returns, contains the uniform associated with the specified name, if found; otherwise,
        /// null.</param>
        /// <returns>true if a uniform with the specified name is found; otherwise, false.</returns>
        public bool TryGet(string name, out Uniform? uniform) => _uniforms.TryGetValue(name, out uniform);

        internal void Add(Uniform uniform) => _uniforms.Add(uniform.Name, uniform);

        public IEnumerator<Uniform> GetEnumerator() => _uniforms.Values.GetEnumerator();
    }
}
