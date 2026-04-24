using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Geode.Core
{
    /// <summary>
    /// Half-precision (16-bit) 4D vector. Primarily a GPU-facing storage type
    /// for vertex attributes such as colors or tangents uploaded as
    /// <c>GL_HALF_FLOAT</c>. For CPU math, convert to <see cref="Vector4"/>.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public readonly struct Vector4H : IEquatable<Vector4H>
    {
        public readonly Half X;
        public readonly Half Y;
        public readonly Half Z;
        public readonly Half W;

        public Vector4H(Half x, Half y, Half z, Half w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public Vector4H(float x, float y, float z, float w)
        {
            X = (Half)x;
            Y = (Half)y;
            Z = (Half)z;
            W = (Half)w;
        }

        public Vector4H(double x, double y, double z, double w)
        {
            X = (Half)x;
            Y = (Half)y;
            Z = (Half)z;
            W = (Half)w;
        }

        public Vector4H(Vector4 v) : this(v.X, v.Y, v.Z, v.W) { }

        public static Vector4H Zero => new(Half.Zero, Half.Zero, Half.Zero, Half.Zero);

        public Vector4 ToVector4() => new((float)X, (float)Y, (float)Z, (float)W);

        public static explicit operator Vector4H(Vector4 v) => new(v);
        public static implicit operator Vector4(Vector4H v) => v.ToVector4();

        public bool Equals(Vector4H other) =>
            X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z) && W.Equals(other.W);
        public override bool Equals(object? obj) => obj is Vector4H v && Equals(v);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);
        public override string ToString() => $"({X}, {Y}, {Z}, {W})";

        public static bool operator ==(Vector4H a, Vector4H b) => a.Equals(b);
        public static bool operator !=(Vector4H a, Vector4H b) => !a.Equals(b);
    }
}
