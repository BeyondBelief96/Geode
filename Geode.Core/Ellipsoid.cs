using System;
using System.Collections;
using System.Collections.Generic;

namespace Geode.Core;

/// <summary>
/// An ellipsoid defined by three radii (a, b, c) centered at the origin.
/// Provides surface normals, coordinate transforms, ray intersection,
/// and curve computation.
/// </summary>
public class Ellipsoid
{
    /// <summary>
    /// Represents the WGS84 reference ellipsoid used for global geodetic calculations.
    /// </summary>
    /// <remarks>The WGS84 ellipsoid is the standard reference for GPS and many mapping applications. It
    /// defines the size and shape of the Earth as used in the World Geodetic System 1984.</remarks>
    public static readonly Ellipsoid Wgs84 = new Ellipsoid(Constants.Wgs84SemiMajorAxis, Constants.Wgs84SemiMajorAxis, Constants.Wgs84SemiMinorAxis);

    /// <summary>
    /// Represents an ellipsoid with unit radii along all axes.
    /// </summary>
    /// <remarks>This instance defines a sphere with a radius of 1.0 for the X, Y, and Z axes. It can be used
    /// as a standard reference for operations requiring a unit sphere.</remarks>
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

    /// <summary>
    /// Projects the specified position vector onto the surface of the ellipsoid using a geodetic (surface-normal)
    /// approach.
    /// </summary>
    /// <remarks>This method uses Newton's iterative algorithm to find the intersection of the geodetic normal with
    /// the ellipsoid surface. The result is the point on the ellipsoid surface that is closest to the input position
    /// along the surface normal. The input position does not need to be on or above the surface. The method is suitable
    /// for ellipsoids with arbitrary radii.</remarks>
    /// <param name="position">The position vector, in the ellipsoid's coordinate system, to be projected onto the ellipsoid surface.</param>
    /// <returns>A new Vector3D representing the closest point on the ellipsoid surface to the input position, following the
    /// geodetic normal direction.</returns>
    public Vector3D ScaleToSurfaceGeodetic(Vector3D position)
    {
        double beta = 1.0 / Math.Sqrt(
                (position.X * position.X) * _oneOverRadiiSquared.X +
                (position.Y * position.Y) * _oneOverRadiiSquared.Y +
                (position.Z * position.Z) * _oneOverRadiiSquared.Z
            );

        double geocentricNormalMagnitude = new Vector3D(
            beta * position.X * _oneOverRadiiSquared.X,
            beta * position.Y * _oneOverRadiiSquared.Y,
            beta * position.Z * _oneOverRadiiSquared.Z).Magnitude;

        // initializing alpha to our initial guess based on the geocentric normal.
        double alpha = (1.0 - beta) * (position.Magnitude / geocentricNormalMagnitude);

        double xSquared = (position.X * position.X);
        double ySquared = (position.Y * position.Y);
        double zSquared = (position.Z * position.Z);

        double da = 0.0;
        double db = 0.0;
        double dc = 0.0;

        // Initial value for the root of S(α) = 0,
        // which we will iteratively solve for using Newton's method.
        // We start with a large value to ensure the loop runs at least once.
        double s = double.MaxValue; 

        // iteratively approximate the root of S(α) using Newton's method until we find a solution with sufficient precision
        while (Math.Abs(s) > 1e-10)
        {
            da = 1.0 + (alpha * _oneOverRadiiSquared.X);
            db = 1.0 + (alpha * _oneOverRadiiSquared.Y);
            dc = 1.0 + (alpha * _oneOverRadiiSquared.Z);

            double daSquared = da * da;
            double dbSquared = db * db;
            double dcSquared = dc * dc;

            double daCubed = daSquared * da;
            double dbCubed = dbSquared * db;
            double dcCubed = dcSquared * dc;

            s = xSquared / (_radiiSquared.X * daSquared) +
                ySquared / (_radiiSquared.Y * dbSquared) +
                zSquared / (_radiiSquared.Z * dcSquared) - 1.0;

            double dSdA = -2.0 * (xSquared / (_radiiToTheFourth.X * daCubed) + ySquared / (_radiiToTheFourth.Y * dbCubed) + zSquared / (_radiiToTheFourth.Z * dcCubed));
            alpha -= (s / dSdA);
        }

        return new Vector3D(position.X / da, position.Y / db, position.Z / dc);
    }

    /// <summary>
    /// Converts a 3D Cartesian position to its geodetic representation, including latitude, longitude, and height above
    /// or below the reference ellipsoid.
    /// </summary>
    /// <remarks>The returned height is positive if the position is above the ellipsoid surface and negative
    /// if below. This method assumes the input position is referenced to the same ellipsoid model used by the
    /// conversion.</remarks>
    /// <param name="position">The 3D Cartesian position to convert, expressed in the same coordinate system as the reference ellipsoid.</param>
    /// <returns>A Geodetic3D structure representing the latitude, longitude, and height (in meters) corresponding to the
    /// specified position.</returns>
    public Geodetic3D ToGeodetic3D(Vector3D position)
    {
        Vector3D r_s = ScaleToSurfaceGeodetic(position);
        Vector3D h = position - r_s;

        // We take the dot product of the surface normal with the position vector to determine if the height is positive or negative.
        // If the dot product is positive, the position is above the surface and the height is positive. If the dot product is negative, the position is below the surface and the height is negative.
        double height = Math.Sign(h.Dot(position)) * h.Magnitude;

        return new Geodetic3D(ToGeodetic2D(r_s), height);
    }

    /// <summary>
    /// Generates a sequence of points representing a curve between two vectors in 3D space, with the specified angular
    /// granularity.
    /// </summary>
    /// <remarks>The curve is generated by rotating the start vector towards the end vector around the axis
    /// defined by their cross product. If the angle between the start and end vectors is less than the specified
    /// granularity, only the start and end points are returned. The method does not validate whether the input vectors
    /// are normalized.</remarks>
    /// <param name="start">The starting point of the curve as a 3D vector.</param>
    /// <param name="end">The ending point of the curve as a 3D vector.</param>
    /// <param name="granularity">The angular distance, in radians, between consecutive points along the curve. Must be greater than zero.</param>
    /// <returns>A list of 3D vectors representing the points along the curve, including the start and end points. The number of
    /// points depends on the angle between the start and end vectors and the specified granularity.</returns>
    public IList<Vector3D> ComputeCurve(Vector3D start, Vector3D end, double granularity)
    {
        Vector3D planeNormal = start.Cross(end).Normalize();
        double theta = start.AngleBetween(end);

        // Number of intermediate points to generate along the curve, based on the angle between the start and end points and the specified granularity.
        // We subtract 1 from the result to account for the fact that we will be including the start and end points in the final list of points.
        int numPoints = Math.Max((int)(theta / granularity) - 1, 0);

        // We initialize the list with a capacity of 2 + numPoints to accommodate the start and end points, as well as the intermediate points along the curve.
        List<Vector3D> points = new List<Vector3D>(2 + numPoints) { start };

        for(int i = 1; i <= numPoints; i++)
        {
            double phi = (i * granularity);
            Vector3D rotatedPoint = start.RotateAroundAxis(planeNormal, phi);
            points.Add(rotatedPoint);
        }

        points.Add(end);
        return points;  
    }
}