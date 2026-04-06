using System;

namespace Geode.Core;

/// <summary>
/// An ellipsoid defined by three radii (a, b, c) centered at the origin.
/// Provides surface normals, coordinate transforms, ray intersection,
/// and curve computation.
/// </summary>
public class Ellipsoid
{
    // Standard Ellipsoids
    public static readonly Ellipsoid Wgs84 = new Ellipsoid(Constants.Wgs84SemiMajorAxis, Constants.Wgs84SemiMajorAxis, Constants.Wgs84SemiMinorAxis);
    public static readonly Ellipsoid UnitSphere = new Ellipsoid(1.0, 1.0, 1.0);

    // Precomputed values
    private readonly Vector3D _radii;
    private readonly Vector3D _radiiSquared;
    private readonly Vector3D _radiiToTheFourth;
    private readonly Vector3D _oneOverRadiiSquared;

    /// <summary>
    /// Gets the radii of the ellipsoid along the X, Y, and Z axes.
    /// </summary>
    public Vector3D Radii => _radii;

    /// <summary>
    /// Gets the squared values of the ellipsoid's radii for each axis.
    /// </summary>
    /// <remarks>Use this property when calculations require the squared radii, such as in geometric or
    /// spatial computations involving the ellipsoid. The values correspond to the X, Y, and Z axes.</remarks>
    public Vector3D RadiiSquared => _radiiSquared;

    /// <summary>
    /// Gets the component-wise fourth power of the ellipsoid's radii.
    /// </summary>
    /// <remarks>This property is typically used in geometric or physics calculations where the fourth power
    /// of each radius is required, such as in certain volume or inertia computations. The values are precomputed for
    /// efficiency.</remarks>
    public Vector3D RadiiToTheFourth => _radiiToTheFourth;

    /// <summary>
    /// Gets the component-wise reciprocal of the squared radii of the ellipsoid represented by this instance.
    /// </summary>
    /// <remarks>This value is commonly used in geometric and mathematical calculations involving ellipsoids,
    /// such as intersection tests or coordinate transformations. The vector contains the reciprocals of the squared
    /// radii for each axis (X, Y, Z).</remarks>
    public Vector3D OneOverRadiiSquared => _oneOverRadiiSquared;

    public Ellipsoid(double a, double b, double c) : this(new Vector3D(a, b, c)) { }

    public Ellipsoid(Vector3D radii)
    {
        _radii = radii;
        _radiiSquared = new Vector3D(radii.X * radii.X, radii.Y * radii.Y, radii.Z * radii.Z);
        _radiiToTheFourth = new Vector3D(_radiiSquared.X * _radiiSquared.X, _radiiSquared.Y * _radiiSquared.Y, _radiiSquared.Z * _radiiSquared.Z);
        _oneOverRadiiSquared = new Vector3D(1.0 / _radiiSquared.X, 1.0 / _radiiSquared.Y, 1.0 / _radiiSquared.Z);
    }

    /// <summary>
    /// Calculates the geodetic surface normal at the specified position on the ellipsoid.
    /// </summary>
    /// <remarks>The returned vector is normalized and points outward from the ellipsoid surface. This method
    /// assumes the input position lies on or near the ellipsoid surface for accurate results.</remarks>
    /// <param name="positionOnEllipsoid">The position on the ellipsoid, specified as a 3D Cartesian coordinate. The coordinate should be expressed in the
    /// same reference frame and units as the ellipsoid's axes.</param>
    /// <returns>A unit vector representing the geodetic surface normal at the specified position on the ellipsoid.</returns>
    public Vector3D GeodeticSurfaceNormal(Vector3D positionOnEllipsoid) => positionOnEllipsoid.MultiplyComponents(_oneOverRadiiSquared).Normalize();

    /// <summary>
    /// Calculates the unit vector normal to the reference ellipsoid at the specified geodetic coordinates.
    /// </summary>
    /// <remarks>The returned vector is normalized and points outward from the center of the ellipsoid. This
    /// method assumes the input coordinates are referenced to the same ellipsoid as the containing type.</remarks>
    /// <param name="geodetic">The geodetic coordinates, including latitude and longitude, for which to compute the surface normal.</param>
    /// <returns>A unit vector representing the surface normal at the specified geodetic position.</returns>
    public Vector3D GeodeticSurfaceNormal(Geodetic3D geodetic)
    {
        double cosLatitude = Math.Cos(geodetic.Latitude);
        return new Vector3D(
            cosLatitude * Math.Cos(geodetic.Longitude),
            cosLatitude * Math.Sin(geodetic.Longitude),
            Math.Sin(geodetic.Latitude));
    }

    /// <summary>
    /// Converts the specified geodetic coordinates to a 3D Cartesian vector representing a position in space.
    /// </summary>
    /// <remarks>The conversion accounts for the ellipsoidal shape of the reference body, mapping latitude,
    /// longitude, and height to a position in 3D space. The resulting vector is relative to the ellipsoid's
    /// center.</remarks>
    /// <param name="geodetic">The geodetic coordinates, including latitude, longitude, and height, to convert to a 3D Cartesian position.</param>
    /// <returns>A Vector3D representing the position in Cartesian coordinates corresponding to the specified geodetic location.</returns>
    public Vector3D ToVector3D(Geodetic3D geodetic)
    {
        // Unit vector perpendicular to the ellipsoid surface: n = (cosφ cosλ, cosφ sinλ, sinφ)
        Vector3D n = GeodeticSurfaceNormal(geodetic);

        // Scale by (a², a², b²) to get an unnormalized surface position vector
        Vector3D k = _radiiSquared.MultiplyComponents(n);

        // γ = √(a² cos²φ + b² sin²φ), the denominator of N(φ) = a²/γ
        double gamma = Math.Sqrt(k.X * n.X + k.Y * n.Y + k.Z * n.Z);

        // k/γ = the point on the ellipsoid surface, equivalent to (N cosφ cosλ, N cosφ sinλ, N(1−e²) sinφ)
        Vector3D rSurface = k / gamma;

        // Offset along the surface normal by height h
        return rSurface + (geodetic.Height * n);
    }

    /// <summary>
    /// Converts a position on the ellipsoid to its corresponding geodetic coordinates (latitude and longitude) in two
    /// dimensions.
    /// </summary>
    /// <param name="positionOnEllipsoid">The position on the ellipsoid, specified as a three-dimensional Cartesian vector. The vector should represent a
    /// point on the surface of the ellipsoid.</param>
    /// <returns>A Geodetic2D structure containing the latitude and longitude corresponding to the specified position on the
    /// ellipsoid.</returns>
    public Geodetic2D ToGeodetic2D(Vector3D positionOnEllipsoid)
    {
        Vector3D n = GeodeticSurfaceNormal(positionOnEllipsoid);
        return new Geodetic2D(
            Math.Atan2(n.Y, n.X), // Longitude
            Math.Asin(n.Z)        // Latitude
        );
    }

    /// <summary>
    /// Scales the specified geocentric position vector so that it lies on the surface of the ellipsoid defined by the
    /// current radii.
    /// </summary>
    /// <remarks>This method assumes the input position is specified in the same coordinate system as the
    /// ellipsoid. The resulting vector will have the same direction as the input but will be scaled to intersect the
    /// ellipsoid surface. This is commonly used for projecting points onto the surface of a planet or celestial body
    /// modeled as an ellipsoid.</remarks>
    /// <param name="position">The geocentric position vector to scale to the ellipsoid surface.</param>
    /// <returns>A new vector representing the point on the ellipsoid surface in the same direction as the input position.</returns>
    public Vector3D ScaleToSurfaceGeocentric(Vector3D position)
    {
        double beta = 1.0 / Math.Sqrt(
                (position.X * position.X) * _oneOverRadiiSquared.X +
                (position.Y * position.Y) * _oneOverRadiiSquared.Y +
                (position.Z * position.Z) * _oneOverRadiiSquared.Z
            );

        return new Vector3D(position.X * beta, position.Y * beta, position.Z * beta);
    }
}