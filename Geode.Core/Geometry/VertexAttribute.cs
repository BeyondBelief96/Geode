using System;
using System.Collections.Generic;
using System.Numerics;

namespace Geode.Core.Geometry
{
    /// <summary>
    /// Abstract base for a named vertex attribute ("position", "normal", etc.).
    /// Concrete subclasses (<see cref="VertexAttributeFloatVector3"/> et al.) store
    /// the actual per-vertex values in a strongly typed list.
    /// </summary>
    /// <remarks>
    /// The <see cref="Name"/> is the contract between producers (tessellators) and
    /// consumers (shaders): a shader's <c>in vec3 position;</c> declaration is
    /// matched to the mesh attribute whose <see cref="Name"/> is <c>"position"</c>
    /// when the Rendering layer builds the VAO. See GUIDE.md §3.5.6.
    /// </remarks>
    public abstract class VertexAttribute
    {
        /// <summary>Shader-facing name of this attribute (e.g. <c>"position"</c>, <c>"normal"</c>).</summary>
        public string Name { get; }

        /// <summary>GLSL type this attribute populates on the GPU.</summary>
        public VertexAttributeType DataType { get; }

        /// <summary>Number of per-vertex values currently held.</summary>
        public abstract int Count { get; }

        protected VertexAttribute(string name, VertexAttributeType dataType)
        {
            Name = name;
            DataType = dataType;
        }
    }

    /// <summary>
    /// A vertex attribute with a typed value list. <typeparamref name="T"/> is the
    /// per-vertex element type (<see cref="Vector3"/>, <see cref="Vector2"/>,
    /// <see cref="float"/>, <see cref="byte"/>, etc.).
    /// </summary>
    public abstract class VertexAttribute<T> : VertexAttribute
    {
        /// <summary>The per-vertex values. One entry per vertex in the mesh.</summary>
        public IList<T> Values { get; }

        protected VertexAttribute(string name, VertexAttributeType dataType, int capacity)
            : base(name, dataType)
        {
            Values = new List<T>(capacity);
        }

        public override int Count => Values.Count;
    }

    /// <summary>Single <see cref="byte"/> per vertex. Targets a GLSL <c>uint</c> / normalized <c>float</c>.</summary>
    public sealed class VertexAttributeUnsignedByte : VertexAttribute<byte>
    {
        public VertexAttributeUnsignedByte(string name, int capacity = 0)
            : base(name, VertexAttributeType.UnsignedByte, capacity) { }
    }

    /// <summary>Single <see cref="Half"/> per vertex. Targets a GLSL <c>float</c> via <c>GL_HALF_FLOAT</c>.</summary>
    public sealed class VertexAttributeHalfFloat : VertexAttribute<Half>
    {
        public VertexAttributeHalfFloat(string name, int capacity = 0)
            : base(name, VertexAttributeType.HalfFloat, capacity) { }
    }

    /// <summary>Two half-precision floats per vertex. Targets a GLSL <c>vec2</c>.</summary>
    public sealed class VertexAttributeHalfFloatVector2 : VertexAttribute<Vector2H>
    {
        public VertexAttributeHalfFloatVector2(string name, int capacity = 0)
            : base(name, VertexAttributeType.HalfFloatVector2, capacity) { }
    }

    /// <summary>Three half-precision floats per vertex. Targets a GLSL <c>vec3</c>.</summary>
    public sealed class VertexAttributeHalfFloatVector3 : VertexAttribute<Vector3H>
    {
        public VertexAttributeHalfFloatVector3(string name, int capacity = 0)
            : base(name, VertexAttributeType.HalfFloatVector3, capacity) { }
    }

    /// <summary>Four half-precision floats per vertex. Targets a GLSL <c>vec4</c>.</summary>
    public sealed class VertexAttributeHalfFloatVector4 : VertexAttribute<Vector4H>
    {
        public VertexAttributeHalfFloatVector4(string name, int capacity = 0)
            : base(name, VertexAttributeType.HalfFloatVector4, capacity) { }
    }

    /// <summary>Single <see cref="float"/> per vertex. Targets a GLSL <c>float</c>.</summary>
    public sealed class VertexAttributeFloat : VertexAttribute<float>
    {
        public VertexAttributeFloat(string name, int capacity = 0)
            : base(name, VertexAttributeType.Float, capacity) { }
    }

    /// <summary>Two-component float vector per vertex. Targets a GLSL <c>vec2</c>.</summary>
    public sealed class VertexAttributeFloatVector2 : VertexAttribute<Vector2>
    {
        public VertexAttributeFloatVector2(string name, int capacity = 0)
            : base(name, VertexAttributeType.FloatVector2, capacity) { }
    }

    /// <summary>Three-component float vector per vertex. Targets a GLSL <c>vec3</c>.</summary>
    /// <remarks>The most common position/normal attribute type.</remarks>
    public sealed class VertexAttributeFloatVector3 : VertexAttribute<Vector3>
    {
        public VertexAttributeFloatVector3(string name, int capacity = 0)
            : base(name, VertexAttributeType.FloatVector3, capacity) { }
    }

    /// <summary>Four-component float vector per vertex. Targets a GLSL <c>vec4</c>.</summary>
    public sealed class VertexAttributeFloatVector4 : VertexAttribute<Vector4>
    {
        public VertexAttributeFloatVector4(string name, int capacity = 0)
            : base(name, VertexAttributeType.FloatVector4, capacity) { }
    }

    /// <summary>
    /// Three-component double vector per vertex. Values carry full double precision
    /// on the CPU; the Rendering layer splits each component into a (high, low)
    /// float pair at upload time to feed GPU RTE / DSFP shaders (GUIDE.md §27).
    /// </summary>
    public sealed class VertexAttributeDoubleVector3 : VertexAttribute<Vector3D>
    {
        public VertexAttributeDoubleVector3(string name, int capacity = 0)
            : base(name, VertexAttributeType.EmulatedDoubleVector3, capacity) { }
    }
}
