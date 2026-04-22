using System.Collections.Generic;

namespace Geode.Core.Geometry
{
    public class VertexAttributeCollection
    {
        private readonly Dictionary<string, VertexAttribute> _byName = new();

        public VertexAttribute this[string name] => _byName[name];
        public void Add(VertexAttribute attribute) => _byName[attribute.Name] = attribute;
        public bool Contains(string name) => _byName.ContainsKey(name);
        public IEnumerable<VertexAttribute> All => _byName.Values;
    }
}
