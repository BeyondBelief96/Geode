namespace Geode.Core.Geometry
{
    public sealed class Mesh
    {
        public VertexAttributeCollection Attributes { get;} = new VertexAttributeCollection();
        public IndicesBase Indices { get; set; } 
        public PrimitiveType PrimitiveType { get; set; } = PrimitiveType.Triangles;
        public WindingOrder FrontFaceWindingOrder { get; set; } = WindingOrder.CounterClockwise;
    }
}
