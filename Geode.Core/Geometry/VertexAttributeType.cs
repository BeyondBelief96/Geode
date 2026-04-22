namespace Geode.Core.Geometry
{
    public enum VertexAttributeType
    {
        UnsignedByte,
        HalfFloat,
        HalfFloatVector2, HalfFloatVector3, HalfFloatVector4,
        Float,
        FloatVector2, FloatVector3, FloatVector4,
        // Emulated doubles -- produces TWO float attributes (high + low) when
        // uploaded. The tessellator writes Vector3D values; the Rendering layer's
        // Mesh-to-VAO converter splits them into two float vec3 streams for
        // GPU RTE / DSFP (Section 27).
        EmulatedDoubleVector3
    }
}
