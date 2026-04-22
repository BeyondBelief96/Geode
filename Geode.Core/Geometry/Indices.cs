using System.Collections.Generic;

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
    }
}
