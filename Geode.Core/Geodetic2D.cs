using System;

namespace Geode.Core
{
    /// <summary>
    /// A position on the ellipsoid surface: longitude + latitude in radians.
    /// </summary>
    public readonly struct Geodetic2D
    {
        public readonly double Longitude; // radians [-π, π]
        public readonly double Latitude; // radians [-π/2, π/2]

        public Geodetic2D(double longitude, double latitude)
        {
            Longitude = longitude;
            Latitude = latitude;
        }

        public bool Equals(Geodetic2D other) => 
            Longitude == other.Longitude && Latitude == other.Latitude;

        public override bool Equals(object? obj) => obj is Geodetic2D g && Equals(g);
        public override int GetHashCode() => HashCode.Combine(Longitude, Latitude);

        public static bool operator ==(Geodetic2D a, Geodetic2D b) => a.Equals(b);
        public static bool operator !=(Geodetic2D a, Geodetic2D b) => !a.Equals(b);
    }
}
