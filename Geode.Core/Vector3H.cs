using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Geode.Core
{
    /// <summary>
    /// Half-precision (16-bit) 3D vector. Primarily a GPU-facing storage type
    /// for vertex attributes such as normals uploaded as <c>GL_HALF_FLOAT</c>.
    /// Not suitable for world-space positions on a globe — <see cref="Half"/>
    /// maxes out at 65504 and has ~3-4 decimal digits of precision.
    /// For CPU math, convert to <see cref="Vector3"/> or <see cref="Vector3D"/>.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public readonly struct Vector3H : IEquatable<Vector3H>
    {
        public readonly Half X;
        public readonly Half Y;
        public readonly Half Z;

        public Vector3H(Half x, Half y, Half z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3H(float x, float y, float z)
        {
            X = (Half)x;
            Y = (Half)y;
            Z = (Half)z;
        }

        public Vector3H(double x, double y, double z)
        {
            X = (Half)x;
            Y = (Half)y;
            Z = (Half)z;
        }

        public Vector3H(Vector3 v) : this(v.X, v.Y, v.Z) { }
        public Vector3H(Vector3D v) : this(v.X, v.Y, v.Z) { }

        public static Vector3H Zero => new(Half.Zero, Half.Zero, Half.Zero);

        public Vector3 ToVector3() => new((float)X, (float)Y, (float)Z);
        public Vector3D ToVector3D() => new((double)X, (double)Y, (double)Z);

        public void Deconstruct(out Half x, out Half y, out Half z)
        {
            x = X;
            y = Y;
            z = Z;
        }

        public static explicit operator Vector3H(Vector3 v) => new(v);
        public static implicit operator Vector3(Vector3H v) => v.ToVector3();
        public static explicit operator Vector3H(Vector3D v) => new(v);
        public static implicit operator Vector3D(Vector3H v) => v.ToVector3D();

        public bool Equals(Vector3H other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        public override bool Equals(object? obj) => obj is Vector3H v && Equals(v);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
        public override string ToString() => $"({X}, {Y}, {Z})";

        public static bool operator ==(Vector3H a, Vector3H b) => a.Equals(b);
        public static bool operator !=(Vector3H a, Vector3H b) => !a.Equals(b);
    }
}
