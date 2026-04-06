using System;

namespace Geode.Core
{
    /// <summary>
    /// A position relative to the ellipsoid: longitude + latitude + height.
    /// Height is in meters above (positive) or below (negative) the surface.
    /// </summary>
    public readonly struct Geodetic3D : IEquatable<Geodetic3D>
    {
        public readonly double Longitude; // radians [-π, π]
        public readonly double Latitude;  // radians [-π/2, π/2]
        public readonly double Height;    // meters

        public Geodetic3D(double longitude, double latitude, double height = 0.0)
        {
            Longitude = longitude;
            Latitude = latitude;
            Height = height;
        }

        public Geodetic3D(Geodetic2D g, double height = 0.0)
            : this(g.Longitude, g.Latitude, height) { }

        public bool Equals(Geodetic3D other) =>
            Longitude == other.Longitude && Latitude == other.Latitude && Height == other.Height;
        public override bool Equals(object? obj) => obj is Geodetic3D g && Equals(g);
        public override int GetHashCode() => HashCode.Combine(Longitude, Latitude, Height);

        public static bool operator ==(Geodetic3D a, Geodetic3D b) => a.Equals(b);
        public static bool operator !=(Geodetic3D a, Geodetic3D b) => !a.Equals(b);
    }
}
