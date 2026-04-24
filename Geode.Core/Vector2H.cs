using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Geode.Core
{
    /// <summary>
    /// Half-precision (16-bit) 2D vector. Primarily a GPU-facing storage type
    /// for vertex attributes such as texture coordinates uploaded as
    /// <c>GL_HALF_FLOAT</c>. For CPU math, convert to <see cref="Vector2"/>
    /// or <see cref="Vector2D"/>.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public readonly struct Vector2H : IEquatable<Vector2H>
    {
        public readonly Half X;
        public readonly Half Y;

        public Vector2H(Half x, Half y)
        {
            X = x;
            Y = y;
        }

        public Vector2H(float x, float y)
        {
            X = (Half)x;
            Y = (Half)y;
        }

        public Vector2H(double x, double y)
        {
            X = (Half)x;
            Y = (Half)y;
        }

        public Vector2H(Vector2 v) : this(v.X, v.Y) { }
        public Vector2H(Vector2D v) : this(v.X, v.Y) { }

        public static Vector2H Zero => new(Half.Zero, Half.Zero);

        public Vector2 ToVector2() => new((float)X, (float)Y);
        public Vector2D ToVector2D() => new((double)X, (double)Y);

        public void Deconstruct(out Half x, out Half y)
        {
            x = X;
            y = Y;
        }

        public static explicit operator Vector2H(Vector2 v) => new(v);
        public static implicit operator Vector2(Vector2H v) => v.ToVector2();
        public static explicit operator Vector2H(Vector2D v) => new(v);
        public static implicit operator Vector2D(Vector2H v) => v.ToVector2D();

        public bool Equals(Vector2H other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override bool Equals(object? obj) => obj is Vector2H v && Equals(v);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public override string ToString() => $"({X}, {Y})";

        public static bool operator ==(Vector2H a, Vector2H b) => a.Equals(b);
        public static bool operator !=(Vector2H a, Vector2H b) => !a.Equals(b);
    }
}
