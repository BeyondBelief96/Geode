using System.Collections.Generic;
using Geode.Core.Tessellation;

namespace Geode.Core.Geometry
{
    public enum IndicesType
    {
        UnsignedShort,
        UnsignedInt
    }

    public abstract class IndicesBase
    {
        public IndicesType DataType { get; }
        public abstract int Count { get; }

        protected IndicesBase(IndicesType dataType)
        {
            DataType = dataType;
        }
    }

    public sealed class IndicesUnsignedShort : IndicesBase
    {
        public IList<ushort> Values { get; }
        public override int Count => Values.Count;
        public IndicesUnsignedShort(int capacity = 0)
            : base(IndicesType.UnsignedShort)
        {
            Values = new List<ushort>(capacity);
        }
    }

    public sealed class IndicesUnsignedInt : IndicesBase
    {
        public IList<uint> Values { get; }
        public override int Count => Values.Count;
        public IndicesUnsignedInt(int capacity = 0)
            : base(IndicesType.UnsignedInt)
        {
            Values = new List<uint>(capacity);
        }

        public void AddTriangle(TriangleIndicesUnsignedInt triangle)
        {
            Values.Add(triangle.UI0);
            Values.Add(triangle.UI1);
            Values.Add(triangle.UI2);
        }
    }
}
