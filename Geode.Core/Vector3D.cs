using System;
using System.Runtime.InteropServices;

namespace Geode.Core
{
    /// <summary>
    /// Double-precision 3D vector. 
    /// Uses doubles because WGS84 coordinates are in meters at planetary scale, 
    /// and single-precision floats would not be accurate enough for geodetic calculations.
    /// These are generally used for cartesian coordinates (ECEF/WGS84), 
    /// while Geodetic2D and Geodetic3D are used for geodetic coordinates (latitude, longitude, altitude).
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Vector3D
    {
        public readonly double X;
        public readonly double Y;
        public readonly double Z;

        public Vector3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double MagnitudeSquared => X * X + Y * Y + Z * Z;
        public double Magnitude => Math.Sqrt(MagnitudeSquared);

        public Vector3D Normalize()
        {
            double mag = Magnitude;
            return new Vector3D(X / mag, Y / mag, Z / mag); 
        }

        public double Dot(Vector3D other) => X * other.X + Y * other.Y + Z * other.Z;

        public Vector3D Cross(Vector3D other)
        {
            return new Vector3D(
                Y * other.Z - Z * other.Y,
                Z * other.X - X * other.Z,
                X * other.Y - Y * other.X
            );
        }

        public Vector3D MultiplyComponents(Vector3D scale)
        {
            return new Vector3D(X * scale.X, Y * scale.Y, Z * scale.Z);
        }

        public double AngleBetween(Vector3D other)
        {
            double dot = Dot(other);
            double mags = Magnitude * other.Magnitude;
            return Math.Acos(dot / mags);
        }

        public Vector3D RotateAroundAxis(Vector3D axis, double angle)
        {
            // Rodrigues' rotation formula
            Vector3D k = axis.Normalize();
            double cosTheta = Math.Cos(angle);
            double sinTheta = Math.Sin(angle);
            return this * cosTheta + k.Cross(this) * sinTheta + k * (k.Dot(this) * (1 - cosTheta));
        }

        #region Operators

        public static Vector3D operator +(Vector3D a, Vector3D b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3D operator -(Vector3D a, Vector3D b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3D operator *(Vector3D v, double s) => new(v.X * s, v.Y * s, v.Z * s);
        public static Vector3D operator *(double s, Vector3D v) => v * s;
        public static Vector3D operator /(Vector3D v, double s) => new(v.X / s, v.Y / s, v.Z / s);
        public static Vector3D operator -(Vector3D v) => new(-v.X, -v.Y, -v.Z);
        public static bool operator ==(Vector3D a, Vector3D b) => a.Equals(b);
        public static bool operator !=(Vector3D a, Vector3D b) => !a.Equals(b);

        public bool Equals(Vector3D other) => X == other.X && Y == other.Y && Z == other.Z;
        public override bool Equals(object? obj) => obj is Vector3D v && Equals(v);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
        public override string ToString() => $"({X}, {Y}, {Z})";

        #endregion

        #region Conversions

        public Vector3H ToVector3H() => new Vector3H(X, Y, Z);

        #endregion
    }
}
