using System;
using Xunit;

namespace Geode.Core.Tests;

public class EllipsoidTests
{
    private const double Tolerance = 1e-6;

    // ───────────────────────────────────────────────
    //  Constructor & precomputed properties
    // ───────────────────────────────────────────────

    [Fact]
    public void Constructor_SetsRadii()
    {
        var e = new Ellipsoid(10, 20, 30);

        Assert.Equal(10, e.Radii.X);
        Assert.Equal(20, e.Radii.Y);
        Assert.Equal(30, e.Radii.Z);
    }

    [Fact]
    public void Constructor_ComputesRadiiSquared()
    {
        var e = new Ellipsoid(3, 4, 5);

        Assert.Equal(9, e.RadiiSquared.X);
        Assert.Equal(16, e.RadiiSquared.Y);
        Assert.Equal(25, e.RadiiSquared.Z);
    }

    [Fact]
    public void Constructor_ComputesRadiiToTheFourth()
    {
        var e = new Ellipsoid(3, 4, 5);

        Assert.Equal(81, e.RadiiToTheFourth.X);
        Assert.Equal(256, e.RadiiToTheFourth.Y);
        Assert.Equal(625, e.RadiiToTheFourth.Z);
    }

    [Fact]
    public void Constructor_ComputesOneOverRadiiSquared()
    {
        var e = new Ellipsoid(2, 4, 5);

        Assert.Equal(1.0 / 4, e.OneOverRadiiSquared.X);
        Assert.Equal(1.0 / 16, e.OneOverRadiiSquared.Y);
        Assert.Equal(1.0 / 25, e.OneOverRadiiSquared.Z);
    }

    // ───────────────────────────────────────────────
    //  Standard ellipsoid instances
    // ───────────────────────────────────────────────

    [Fact]
    public void Wgs84_HasCorrectRadii()
    {
        var wgs = Ellipsoid.Wgs84;

        Assert.Equal(Constants.Wgs84SemiMajorAxis, wgs.Radii.X);
        Assert.Equal(Constants.Wgs84SemiMajorAxis, wgs.Radii.Y);
        Assert.Equal(Constants.Wgs84SemiMinorAxis, wgs.Radii.Z);
    }

    [Fact]
    public void UnitSphere_HasRadiiOfOne()
    {
        var sphere = Ellipsoid.UnitSphere;

        Assert.Equal(1.0, sphere.Radii.X);
        Assert.Equal(1.0, sphere.Radii.Y);
        Assert.Equal(1.0, sphere.Radii.Z);
    }

    // ───────────────────────────────────────────────
    //  GeodeticSurfaceNormal(Vector3D)
    // ───────────────────────────────────────────────

    [Fact]
    public void GeodeticSurfaceNormal_FromCartesian_UnitSphere_ReturnsNormalizedInput()
    {
        var sphere = Ellipsoid.UnitSphere;
        var point = new Vector3D(1, 0, 0);

        var normal = sphere.GeodeticSurfaceNormal(point);

        Assert.Equal(1.0, normal.X, Tolerance);
        Assert.Equal(0.0, normal.Y, Tolerance);
        Assert.Equal(0.0, normal.Z, Tolerance);
    }

    [Fact]
    public void GeodeticSurfaceNormal_FromCartesian_IsUnitLength()
    {
        var e = Ellipsoid.Wgs84;
        // A point roughly on the WGS84 surface at 45° lat
        var geodetic = new Geodetic3D(0, Math.PI / 4);
        var point = e.ToVector3D(geodetic);

        var normal = e.GeodeticSurfaceNormal(point);

        Assert.Equal(1.0, normal.Magnitude, Tolerance);
    }

    // ───────────────────────────────────────────────
    //  GeodeticSurfaceNormal(Geodetic3D)
    // ───────────────────────────────────────────────

    [Fact]
    public void GeodeticSurfaceNormal_FromGeodetic_AtEquatorPrimeMeridian()
    {
        var e = Ellipsoid.Wgs84;
        var geodetic = new Geodetic3D(0, 0); // lon=0, lat=0

        var normal = e.GeodeticSurfaceNormal(geodetic);

        Assert.Equal(1.0, normal.X, Tolerance);
        Assert.Equal(0.0, normal.Y, Tolerance);
        Assert.Equal(0.0, normal.Z, Tolerance);
    }

    [Fact]
    public void GeodeticSurfaceNormal_FromGeodetic_AtNorthPole()
    {
        var e = Ellipsoid.Wgs84;
        var geodetic = new Geodetic3D(0, Math.PI / 2); // lat=90°

        var normal = e.GeodeticSurfaceNormal(geodetic);

        Assert.Equal(0.0, normal.X, Tolerance);
        Assert.Equal(0.0, normal.Y, Tolerance);
        Assert.Equal(1.0, normal.Z, Tolerance);
    }

    [Fact]
    public void GeodeticSurfaceNormal_FromGeodetic_AtSouthPole()
    {
        var e = Ellipsoid.Wgs84;
        var geodetic = new Geodetic3D(0, -Math.PI / 2); // lat=-90°

        var normal = e.GeodeticSurfaceNormal(geodetic);

        Assert.Equal(0.0, normal.X, Tolerance);
        Assert.Equal(0.0, normal.Y, Tolerance);
        Assert.Equal(-1.0, normal.Z, Tolerance);
    }

    [Fact]
    public void GeodeticSurfaceNormal_FromGeodetic_IsAlwaysUnitLength()
    {
        var e = Ellipsoid.Wgs84;
        var geodetic = new Geodetic3D(Math.PI / 3, Math.PI / 6); // lon=60°, lat=30°

        var normal = e.GeodeticSurfaceNormal(geodetic);

        Assert.Equal(1.0, normal.Magnitude, Tolerance);
    }

    // ───────────────────────────────────────────────
    //  ToVector3D  (geodetic → cartesian)
    // ───────────────────────────────────────────────

    [Fact]
    public void ToVector3D_EquatorPrimeMeridian_ZeroHeight()
    {
        var e = Ellipsoid.Wgs84;
        var geodetic = new Geodetic3D(0, 0, 0);

        var cart = e.ToVector3D(geodetic);

        // At lon=0, lat=0, h=0: x = a, y = 0, z = 0
        Assert.Equal(Constants.Wgs84SemiMajorAxis, cart.X, Tolerance);
        Assert.Equal(0.0, cart.Y, Tolerance);
        Assert.Equal(0.0, cart.Z, Tolerance);
    }

    [Fact]
    public void ToVector3D_NorthPole_ZeroHeight()
    {
        var e = Ellipsoid.Wgs84;
        var geodetic = new Geodetic3D(0, Math.PI / 2, 0);

        var cart = e.ToVector3D(geodetic);

        // At north pole: x ≈ 0, y = 0, z = b
        Assert.Equal(0.0, cart.X, Tolerance);
        Assert.Equal(0.0, cart.Y, Tolerance);
        Assert.Equal(Constants.Wgs84SemiMinorAxis, cart.Z, Tolerance);
    }

    [Fact]
    public void ToVector3D_SouthPole_ZeroHeight()
    {
        var e = Ellipsoid.Wgs84;
        var geodetic = new Geodetic3D(0, -Math.PI / 2, 0);

        var cart = e.ToVector3D(geodetic);

        Assert.Equal(0.0, cart.X, Tolerance);
        Assert.Equal(0.0, cart.Y, Tolerance);
        Assert.Equal(-Constants.Wgs84SemiMinorAxis, cart.Z, Tolerance);
    }

    [Fact]
    public void ToVector3D_Equator90Degrees_ZeroHeight()
    {
        var e = Ellipsoid.Wgs84;
        var geodetic = new Geodetic3D(Math.PI / 2, 0, 0); // lon=90°, lat=0

        var cart = e.ToVector3D(geodetic);

        Assert.Equal(0.0, cart.X, Tolerance);
        Assert.Equal(Constants.Wgs84SemiMajorAxis, cart.Y, Tolerance);
        Assert.Equal(0.0, cart.Z, Tolerance);
    }

    [Fact]
    public void ToVector3D_WithHeight_IncreasesDistanceFromCenter()
    {
        var e = Ellipsoid.Wgs84;
        double height = 10000; // 10 km
        var onSurface = e.ToVector3D(new Geodetic3D(0, 0, 0));
        var above = e.ToVector3D(new Geodetic3D(0, 0, height));

        double surfaceDist = onSurface.Magnitude;
        double aboveDist = above.Magnitude;

        Assert.True(aboveDist > surfaceDist);
        Assert.Equal(height, aboveDist - surfaceDist, Tolerance);
    }

    [Fact]
    public void ToVector3D_UnitSphere_MatchesTrigIdentities()
    {
        var sphere = Ellipsoid.UnitSphere;
        double lon = Math.PI / 4; // 45°
        double lat = Math.PI / 6; // 30°
        var geodetic = new Geodetic3D(lon, lat, 0);

        var cart = sphere.ToVector3D(geodetic);

        // For a unit sphere: x = cosφ cosλ, y = cosφ sinλ, z = sinφ
        double expectedX = Math.Cos(lat) * Math.Cos(lon);
        double expectedY = Math.Cos(lat) * Math.Sin(lon);
        double expectedZ = Math.Sin(lat);

        Assert.Equal(expectedX, cart.X, Tolerance);
        Assert.Equal(expectedY, cart.Y, Tolerance);
        Assert.Equal(expectedZ, cart.Z, Tolerance);
    }

    [Fact]
    public void ToVector3D_UnitSphere_ZeroHeight_PointLiesOnSurface()
    {
        var sphere = Ellipsoid.UnitSphere;
        var geodetic = new Geodetic3D(1.2, 0.7, 0);

        var cart = sphere.ToVector3D(geodetic);

        Assert.Equal(1.0, cart.Magnitude, Tolerance);
    }

    [Fact]
    public void ToVector3D_Wgs84_ZeroHeight_PointLiesOnEllipsoid()
    {
        var e = Ellipsoid.Wgs84;
        double lon = 0.5;
        double lat = 0.8;
        var cart = e.ToVector3D(new Geodetic3D(lon, lat, 0));

        // Verify the point satisfies x²/a² + y²/a² + z²/b² = 1
        double a = Constants.Wgs84SemiMajorAxis;
        double b = Constants.Wgs84SemiMinorAxis;
        double result = (cart.X * cart.X + cart.Y * cart.Y) / (a * a)
                      + (cart.Z * cart.Z) / (b * b);

        Assert.Equal(1.0, result, Tolerance);
    }

    // ───────────────────────────────────────────────
    //  ToGeodetic2D  (cartesian → geodetic)
    // ───────────────────────────────────────────────

    [Fact]
    public void ToGeodetic2D_EquatorPrimeMeridian()
    {
        var e = Ellipsoid.Wgs84;
        var point = new Vector3D(Constants.Wgs84SemiMajorAxis, 0, 0);

        var geo = e.ToGeodetic2D(point);

        Assert.Equal(0.0, geo.Longitude, Tolerance);
        Assert.Equal(0.0, geo.Latitude, Tolerance);
    }

    [Fact]
    public void ToGeodetic2D_NorthPole()
    {
        var e = Ellipsoid.Wgs84;
        var point = new Vector3D(0, 0, Constants.Wgs84SemiMinorAxis);

        var geo = e.ToGeodetic2D(point);

        Assert.Equal(Math.PI / 2, geo.Latitude, Tolerance);
    }

    [Fact]
    public void ToGeodetic2D_Equator90DegreesLon()
    {
        var e = Ellipsoid.Wgs84;
        var point = new Vector3D(0, Constants.Wgs84SemiMajorAxis, 0);

        var geo = e.ToGeodetic2D(point);

        Assert.Equal(Math.PI / 2, geo.Longitude, Tolerance);
        Assert.Equal(0.0, geo.Latitude, Tolerance);
    }

    // ───────────────────────────────────────────────
    //  Round-trip consistency
    // ───────────────────────────────────────────────

    [Theory]
    [InlineData(0, 0)]
    [InlineData(Math.PI / 4, Math.PI / 6)]
    [InlineData(-Math.PI / 3, Math.PI / 4)]
    [InlineData(Math.PI, -Math.PI / 3)]
    [InlineData(-Math.PI / 2, 0)]
    public void ToVector3D_ThenToGeodetic2D_RecoverOriginalCoords(double lon, double lat)
    {
        var e = Ellipsoid.Wgs84;
        var original = new Geodetic3D(lon, lat, 0);

        var cartesian = e.ToVector3D(original);
        var recovered = e.ToGeodetic2D(cartesian);

        Assert.Equal(lon, recovered.Longitude, Tolerance);
        Assert.Equal(lat, recovered.Latitude, Tolerance);
    }

    // ───────────────────────────────────────────────
    //  Constructor – Vector3D overload
    // ───────────────────────────────────────────────

    [Fact]
    public void Constructor_Vector3DOverload_MatchesScalarOverload()
    {
        var fromScalars = new Ellipsoid(3, 4, 5);
        var fromVector = new Ellipsoid(new Vector3D(3, 4, 5));

        Assert.Equal(fromScalars.Radii.X, fromVector.Radii.X);
        Assert.Equal(fromScalars.Radii.Y, fromVector.Radii.Y);
        Assert.Equal(fromScalars.Radii.Z, fromVector.Radii.Z);
        Assert.Equal(fromScalars.RadiiSquared.X, fromVector.RadiiSquared.X);
        Assert.Equal(fromScalars.OneOverRadiiSquared.Z, fromVector.OneOverRadiiSquared.Z);
    }

    // ───────────────────────────────────────────────
    //  GeodeticSurfaceNormal – additional cases
    // ───────────────────────────────────────────────

    [Fact]
    public void GeodeticSurfaceNormal_FromCartesian_Wgs84_NorthPole()
    {
        var e = Ellipsoid.Wgs84;
        var pole = e.ToVector3D(new Geodetic3D(0, Math.PI / 2, 0));

        var normal = e.GeodeticSurfaceNormal(pole);

        Assert.Equal(0.0, normal.X, Tolerance);
        Assert.Equal(0.0, normal.Y, Tolerance);
        Assert.Equal(1.0, normal.Z, Tolerance);
    }

    [Fact]
    public void GeodeticSurfaceNormal_FromCartesian_NonSphere_DiffersFromPosition()
    {
        // On a non-spherical ellipsoid, the geodetic normal differs from
        // the geocentric (position) direction except at special points
        var e = new Ellipsoid(2, 2, 1); // oblate
        var point = e.ToVector3D(new Geodetic3D(0, Math.PI / 4, 0)); // 45° lat

        var normal = e.GeodeticSurfaceNormal(point);
        var geocentricDir = point.Normalize();

        // They should not be equal for an oblate ellipsoid at 45° lat
        Assert.NotEqual(geocentricDir.X, normal.X, Tolerance);
    }

    [Fact]
    public void GeodeticSurfaceNormal_FromGeodetic_Equator180()
    {
        var e = Ellipsoid.Wgs84;
        var geodetic = new Geodetic3D(Math.PI, 0); // lon=180°, lat=0

        var normal = e.GeodeticSurfaceNormal(geodetic);

        Assert.Equal(-1.0, normal.X, Tolerance);
        Assert.Equal(0.0, normal.Y, Tolerance);
        Assert.Equal(0.0, normal.Z, Tolerance);
    }

    [Fact]
    public void GeodeticSurfaceNormal_FromGeodetic_EquatorNeg90()
    {
        var e = Ellipsoid.Wgs84;
        var geodetic = new Geodetic3D(-Math.PI / 2, 0); // lon=-90°, lat=0

        var normal = e.GeodeticSurfaceNormal(geodetic);

        Assert.Equal(0.0, normal.X, Tolerance);
        Assert.Equal(-1.0, normal.Y, Tolerance);
        Assert.Equal(0.0, normal.Z, Tolerance);
    }

    // ───────────────────────────────────────────────
    //  ToVector3D – additional cases
    // ───────────────────────────────────────────────

    [Fact]
    public void ToVector3D_NegativeHeight_CloserToCenter()
    {
        var e = Ellipsoid.Wgs84;
        var onSurface = e.ToVector3D(new Geodetic3D(0.5, 0.3, 0));
        var below = e.ToVector3D(new Geodetic3D(0.5, 0.3, -1000));

        Assert.True(below.Magnitude < onSurface.Magnitude);
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(1.0, 0.5)]
    [InlineData(-2.0, -1.0)]
    [InlineData(Math.PI, Math.PI / 2)]
    public void ToVector3D_Wgs84_VariousCoords_PointLiesOnEllipsoid(double lon, double lat)
    {
        var e = Ellipsoid.Wgs84;
        var cart = e.ToVector3D(new Geodetic3D(lon, lat, 0));

        double a = Constants.Wgs84SemiMajorAxis;
        double b = Constants.Wgs84SemiMinorAxis;
        double result = (cart.X * cart.X + cart.Y * cart.Y) / (a * a)
                      + (cart.Z * cart.Z) / (b * b);

        Assert.Equal(1.0, result, Tolerance);
    }

    // ───────────────────────────────────────────────
    //  ToGeodetic2D – additional cases
    // ───────────────────────────────────────────────

    [Fact]
    public void ToGeodetic2D_SouthPole()
    {
        var e = Ellipsoid.Wgs84;
        var point = new Vector3D(0, 0, -Constants.Wgs84SemiMinorAxis);

        var geo = e.ToGeodetic2D(point);

        Assert.Equal(-Math.PI / 2, geo.Latitude, Tolerance);
    }

    [Fact]
    public void ToGeodetic2D_NegativeLongitude()
    {
        var e = Ellipsoid.Wgs84;
        var point = new Vector3D(0, -Constants.Wgs84SemiMajorAxis, 0);

        var geo = e.ToGeodetic2D(point);

        Assert.Equal(-Math.PI / 2, geo.Longitude, Tolerance);
        Assert.Equal(0.0, geo.Latitude, Tolerance);
    }

    [Fact]
    public void ToGeodetic2D_UnitSphere_ArbitraryPoint()
    {
        var sphere = Ellipsoid.UnitSphere;
        double lon = 0.7;
        double lat = 0.4;
        var cart = sphere.ToVector3D(new Geodetic3D(lon, lat, 0));

        var geo = sphere.ToGeodetic2D(cart);

        Assert.Equal(lon, geo.Longitude, Tolerance);
        Assert.Equal(lat, geo.Latitude, Tolerance);
    }

    // ───────────────────────────────────────────────
    //  ScaleToSurfaceGeocentric
    // ───────────────────────────────────────────────

    [Fact]
    public void ScaleToSurfaceGeocentric_UnitSphere_NormalizesVector()
    {
        var sphere = Ellipsoid.UnitSphere;
        var point = new Vector3D(3, 4, 0);

        var result = sphere.ScaleToSurfaceGeocentric(point);

        Assert.Equal(1.0, result.Magnitude, Tolerance);
        // Direction should be preserved
        Assert.Equal(3.0 / 5.0, result.X, Tolerance);
        Assert.Equal(4.0 / 5.0, result.Y, Tolerance);
        Assert.Equal(0.0, result.Z, Tolerance);
    }

    [Fact]
    public void ScaleToSurfaceGeocentric_PointAlreadyOnSurface_ReturnsSamePoint()
    {
        var e = Ellipsoid.Wgs84;
        var surfacePoint = e.ToVector3D(new Geodetic3D(0.5, 0.3, 0));

        var result = e.ScaleToSurfaceGeocentric(surfacePoint);

        Assert.Equal(surfacePoint.X, result.X, Tolerance);
        Assert.Equal(surfacePoint.Y, result.Y, Tolerance);
        Assert.Equal(surfacePoint.Z, result.Z, Tolerance);
    }

    [Fact]
    public void ScaleToSurfaceGeocentric_PointAboveSurface_ProjectsOntoEllipsoid()
    {
        var e = Ellipsoid.Wgs84;
        var above = e.ToVector3D(new Geodetic3D(0, 0, 50000)); // 50 km above

        var result = e.ScaleToSurfaceGeocentric(above);

        double a = Constants.Wgs84SemiMajorAxis;
        double b = Constants.Wgs84SemiMinorAxis;
        double ellipsoidEq = (result.X * result.X + result.Y * result.Y) / (a * a)
                           + (result.Z * result.Z) / (b * b);

        Assert.Equal(1.0, ellipsoidEq, Tolerance);
    }

    [Fact]
    public void ScaleToSurfaceGeocentric_PointBelowSurface_ProjectsOntoEllipsoid()
    {
        var e = Ellipsoid.Wgs84;
        // A point inside the ellipsoid (half the semi-major axis along X)
        var inside = new Vector3D(Constants.Wgs84SemiMajorAxis * 0.5, 0, 0);

        var result = e.ScaleToSurfaceGeocentric(inside);

        double a = Constants.Wgs84SemiMajorAxis;
        double b = Constants.Wgs84SemiMinorAxis;
        double ellipsoidEq = (result.X * result.X + result.Y * result.Y) / (a * a)
                           + (result.Z * result.Z) / (b * b);

        Assert.Equal(1.0, ellipsoidEq, Tolerance);
    }

    [Fact]
    public void ScaleToSurfaceGeocentric_AlongXAxis_ReturnsEquator()
    {
        var e = Ellipsoid.Wgs84;
        var point = new Vector3D(1e7, 0, 0);

        var result = e.ScaleToSurfaceGeocentric(point);

        Assert.Equal(Constants.Wgs84SemiMajorAxis, result.X, Tolerance);
        Assert.Equal(0.0, result.Y, Tolerance);
        Assert.Equal(0.0, result.Z, Tolerance);
    }

    [Fact]
    public void ScaleToSurfaceGeocentric_AlongZAxis_ReturnsPole()
    {
        var e = Ellipsoid.Wgs84;
        var point = new Vector3D(0, 0, 1e7);

        var result = e.ScaleToSurfaceGeocentric(point);

        Assert.Equal(0.0, result.X, Tolerance);
        Assert.Equal(0.0, result.Y, Tolerance);
        Assert.Equal(Constants.Wgs84SemiMinorAxis, result.Z, Tolerance);
    }

    [Fact]
    public void ScaleToSurfaceGeocentric_PreservesDirection()
    {
        var e = Ellipsoid.Wgs84;
        var point = new Vector3D(5e6, 3e6, 2e6);

        var result = e.ScaleToSurfaceGeocentric(point);

        // The direction (ratios) should be preserved
        var pointDir = point.Normalize();
        var resultDir = result.Normalize();

        Assert.Equal(pointDir.X, resultDir.X, Tolerance);
        Assert.Equal(pointDir.Y, resultDir.Y, Tolerance);
        Assert.Equal(pointDir.Z, resultDir.Z, Tolerance);
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(Math.PI / 4, Math.PI / 6)]
    [InlineData(-Math.PI / 3, -Math.PI / 4)]
    [InlineData(Math.PI / 2, Math.PI / 3)]
    public void ScaleToSurfaceGeocentric_VariousDirections_ResultLiesOnEllipsoid(double lon, double lat)
    {
        var e = Ellipsoid.Wgs84;
        // Create a point above the surface and project it
        var point = e.ToVector3D(new Geodetic3D(lon, lat, 100000));

        var result = e.ScaleToSurfaceGeocentric(point);

        double a = Constants.Wgs84SemiMajorAxis;
        double b = Constants.Wgs84SemiMinorAxis;
        double ellipsoidEq = (result.X * result.X + result.Y * result.Y) / (a * a)
                           + (result.Z * result.Z) / (b * b);

        Assert.Equal(1.0, ellipsoidEq, Tolerance);
    }

    [Fact]
    public void ScaleToSurfaceGeocentric_NonEarthEllipsoid_ResultLiesOnSurface()
    {
        var e = new Ellipsoid(10, 20, 30);
        var point = new Vector3D(50, 100, 150);

        var result = e.ScaleToSurfaceGeocentric(point);

        double ellipsoidEq = (result.X * result.X) / (10.0 * 10.0)
                           + (result.Y * result.Y) / (20.0 * 20.0)
                           + (result.Z * result.Z) / (30.0 * 30.0);

        Assert.Equal(1.0, ellipsoidEq, Tolerance);
    }

    // ───────────────────────────────────────────────
    //  ScaleToSurfaceGeodetic
    // ───────────────────────────────────────────────

    [Fact]
    public void ScaleToSurfaceGeodetic_UnitSphere_PointAboveSurface()
    {
        var sphere = Ellipsoid.UnitSphere;
        var point = new Vector3D(5, 0, 0);

        var result = sphere.ScaleToSurfaceGeodetic(point);

        // On a sphere, geodetic == geocentric, so result should be the surface point
        Assert.Equal(1.0, result.X, Tolerance);
        Assert.Equal(0.0, result.Y, Tolerance);
        Assert.Equal(0.0, result.Z, Tolerance);
    }

    [Fact]
    public void ScaleToSurfaceGeodetic_PointOnSurface_ReturnsSamePoint()
    {
        var e = Ellipsoid.Wgs84;
        var surfacePoint = e.ToVector3D(new Geodetic3D(0.5, 0.3, 0));

        var result = e.ScaleToSurfaceGeodetic(surfacePoint);

        Assert.Equal(surfacePoint.X, result.X, Tolerance);
        Assert.Equal(surfacePoint.Y, result.Y, Tolerance);
        Assert.Equal(surfacePoint.Z, result.Z, Tolerance);
    }

    [Fact]
    public void ScaleToSurfaceGeodetic_ResultLiesOnEllipsoid()
    {
        var e = Ellipsoid.Wgs84;
        var point = e.ToVector3D(new Geodetic3D(1.0, 0.5, 30000));

        var result = e.ScaleToSurfaceGeodetic(point);

        double a = Constants.Wgs84SemiMajorAxis;
        double b = Constants.Wgs84SemiMinorAxis;
        double ellipsoidEq = (result.X * result.X + result.Y * result.Y) / (a * a)
                           + (result.Z * result.Z) / (b * b);

        Assert.Equal(1.0, ellipsoidEq, Tolerance);
    }

    [Fact]
    public void ScaleToSurfaceGeodetic_AlongXAxis_ReturnsEquator()
    {
        var e = Ellipsoid.Wgs84;
        var point = new Vector3D(1e7, 0, 0);

        var result = e.ScaleToSurfaceGeodetic(point);

        Assert.Equal(Constants.Wgs84SemiMajorAxis, result.X, Tolerance);
        Assert.Equal(0.0, result.Y, Tolerance);
        Assert.Equal(0.0, result.Z, Tolerance);
    }

    [Fact]
    public void ScaleToSurfaceGeodetic_AlongZAxis_ReturnsPole()
    {
        var e = Ellipsoid.Wgs84;
        var point = new Vector3D(0, 0, 1e7);

        var result = e.ScaleToSurfaceGeodetic(point);

        Assert.Equal(0.0, result.X, Tolerance);
        Assert.Equal(0.0, result.Y, Tolerance);
        Assert.Equal(Constants.Wgs84SemiMinorAxis, result.Z, Tolerance);
    }

    [Fact]
    public void ScaleToSurfaceGeodetic_PointInsideEllipsoid_ProjectsOntoSurface()
    {
        var e = Ellipsoid.Wgs84;
        var inside = new Vector3D(Constants.Wgs84SemiMajorAxis * 0.5, 0, 0);

        var result = e.ScaleToSurfaceGeodetic(inside);

        double a = Constants.Wgs84SemiMajorAxis;
        double b = Constants.Wgs84SemiMinorAxis;
        double ellipsoidEq = (result.X * result.X + result.Y * result.Y) / (a * a)
                           + (result.Z * result.Z) / (b * b);

        Assert.Equal(1.0, ellipsoidEq, Tolerance);
    }

    [Theory]
    [InlineData(0.0, 0.0, 0.0)]
    [InlineData(0.0, 0.0, 50000.0)]
    [InlineData(Math.PI / 4, Math.PI / 6, 10000.0)]
    [InlineData(-Math.PI / 3, -Math.PI / 4, 100000.0)]
    [InlineData(Math.PI / 2, Math.PI / 3, 5000.0)]
    [InlineData(0.0, Math.PI / 2, 20000.0)]   // north pole
    [InlineData(0.0, -Math.PI / 2, 20000.0)]  // south pole
    public void ScaleToSurfaceGeodetic_VariousPositions_ResultLiesOnEllipsoid(double lon, double lat, double height)
    {
        var e = Ellipsoid.Wgs84;
        var point = e.ToVector3D(new Geodetic3D(lon, lat, height));

        var result = e.ScaleToSurfaceGeodetic(point);

        double a = Constants.Wgs84SemiMajorAxis;
        double b = Constants.Wgs84SemiMinorAxis;
        double ellipsoidEq = (result.X * result.X + result.Y * result.Y) / (a * a)
                           + (result.Z * result.Z) / (b * b);

        Assert.Equal(1.0, ellipsoidEq, Tolerance);
    }

    [Fact]
    public void ScaleToSurfaceGeodetic_NonEarthEllipsoid_ResultLiesOnSurface()
    {
        var e = new Ellipsoid(10, 20, 30);
        var point = new Vector3D(50, 100, 150);

        var result = e.ScaleToSurfaceGeodetic(point);

        double ellipsoidEq = (result.X * result.X) / (10.0 * 10.0)
                           + (result.Y * result.Y) / (20.0 * 20.0)
                           + (result.Z * result.Z) / (30.0 * 30.0);

        Assert.Equal(1.0, ellipsoidEq, Tolerance);
    }

    [Fact]
    public void ScaleToSurfaceGeodetic_UnitSphere_MatchesGeocentric()
    {
        // On a sphere, geodetic and geocentric projections should agree
        var sphere = Ellipsoid.UnitSphere;
        var point = new Vector3D(3, 4, 5);

        var geodetic = sphere.ScaleToSurfaceGeodetic(point);
        var geocentric = sphere.ScaleToSurfaceGeocentric(point);

        Assert.Equal(geocentric.X, geodetic.X, Tolerance);
        Assert.Equal(geocentric.Y, geodetic.Y, Tolerance);
        Assert.Equal(geocentric.Z, geodetic.Z, Tolerance);
    }

    [Fact]
    public void ScaleToSurfaceGeodetic_OblateEllipsoid_DiffersFromGeocentric()
    {
        // On a non-spherical ellipsoid, geodetic and geocentric projections
        // should generally differ for off-axis points
        var e = Ellipsoid.Wgs84;
        var point = e.ToVector3D(new Geodetic3D(0.5, 0.5, 50000));

        var geodetic = e.ScaleToSurfaceGeodetic(point);
        var geocentric = e.ScaleToSurfaceGeocentric(point);

        // Both should lie on the ellipsoid, but at different positions
        double a = Constants.Wgs84SemiMajorAxis;
        double b = Constants.Wgs84SemiMinorAxis;

        double geoEq = (geodetic.X * geodetic.X + geodetic.Y * geodetic.Y) / (a * a)
                     + (geodetic.Z * geodetic.Z) / (b * b);
        double cenEq = (geocentric.X * geocentric.X + geocentric.Y * geocentric.Y) / (a * a)
                     + (geocentric.Z * geocentric.Z) / (b * b);

        Assert.Equal(1.0, geoEq, Tolerance);
        Assert.Equal(1.0, cenEq, Tolerance);

        // The two projections should not be identical (except on axes/equator)
        double diff = Math.Sqrt(
            Math.Pow(geodetic.X - geocentric.X, 2) +
            Math.Pow(geodetic.Y - geocentric.Y, 2) +
            Math.Pow(geodetic.Z - geocentric.Z, 2));
        Assert.True(diff > 0.01, "Geodetic and geocentric projections should differ on oblate ellipsoid");
    }

    // ───────────────────────────────────────────────
    //  Round-trip consistency – extended
    // ───────────────────────────────────────────────

    [Theory]
    [InlineData(0, 0)]
    [InlineData(Math.PI / 4, Math.PI / 6)]
    [InlineData(-Math.PI / 3, Math.PI / 4)]
    [InlineData(Math.PI, -Math.PI / 3)]
    [InlineData(-Math.PI / 2, 0)]
    public void ScaleToSurfaceGeodetic_ThenToGeodetic2D_RecoverCoords(double lon, double lat)
    {
        var e = Ellipsoid.Wgs84;
        var point = e.ToVector3D(new Geodetic3D(lon, lat, 50000));

        var projected = e.ScaleToSurfaceGeodetic(point);
        var recovered = e.ToGeodetic2D(projected);

        Assert.Equal(lon, recovered.Longitude, Tolerance);
        Assert.Equal(lat, recovered.Latitude, Tolerance);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(Math.PI / 4, Math.PI / 6, 10000)]
    [InlineData(-1.5, 0.8, 50000)]
    public void ScaleToSurfaceGeocentric_ThenToGeodetic2D_RecoversLonLat(double lon, double lat, double height)
    {
        var e = Ellipsoid.Wgs84;
        var point = e.ToVector3D(new Geodetic3D(lon, lat, height));

        var projected = e.ScaleToSurfaceGeocentric(point);
        var recovered = e.ToGeodetic2D(projected);

        // Geocentric projection preserves direction, so lon should match.
        // Latitude may differ slightly on an oblate ellipsoid (geocentric vs geodetic lat),
        // but for zero-height points the round-trip should be exact.
        if (height == 0)
        {
            Assert.Equal(lon, recovered.Longitude, Tolerance);
            Assert.Equal(lat, recovered.Latitude, Tolerance);
        }
        else
        {
            Assert.Equal(lon, recovered.Longitude, Tolerance);
        }
    }

    // ───────────────────────────────────────────────
    //  ToGeodetic3D  (cartesian → geodetic with height)
    // ───────────────────────────────────────────────

    [Fact]
    public void ToGeodetic3D_PointOnSurface_HeightIsZero()
    {
        var e = Ellipsoid.Wgs84;
        var cart = e.ToVector3D(new Geodetic3D(0.5, 0.3, 0));

        var geo = e.ToGeodetic3D(cart);

        Assert.Equal(0.0, geo.Height, Tolerance);
    }

    [Fact]
    public void ToGeodetic3D_PointAboveSurface_HeightIsPositive()
    {
        var e = Ellipsoid.Wgs84;
        double height = 10000;
        var cart = e.ToVector3D(new Geodetic3D(0.5, 0.3, height));

        var geo = e.ToGeodetic3D(cart);

        Assert.True(geo.Height > 0);
        Assert.Equal(height, geo.Height, Tolerance);
    }

    [Fact]
    public void ToGeodetic3D_PointBelowSurface_HeightIsNegative()
    {
        var e = Ellipsoid.Wgs84;
        double height = -5000;
        var cart = e.ToVector3D(new Geodetic3D(0.5, 0.3, height));

        var geo = e.ToGeodetic3D(cart);

        Assert.True(geo.Height < 0);
        Assert.Equal(height, geo.Height, Tolerance);
    }

    [Fact]
    public void ToGeodetic3D_EquatorPrimeMeridian_ZeroHeight()
    {
        var e = Ellipsoid.Wgs84;
        var cart = new Vector3D(Constants.Wgs84SemiMajorAxis, 0, 0);

        var geo = e.ToGeodetic3D(cart);

        Assert.Equal(0.0, geo.Longitude, Tolerance);
        Assert.Equal(0.0, geo.Latitude, Tolerance);
        Assert.Equal(0.0, geo.Height, Tolerance);
    }

    [Fact]
    public void ToGeodetic3D_NorthPole_ZeroHeight()
    {
        var e = Ellipsoid.Wgs84;
        var cart = new Vector3D(0, 0, Constants.Wgs84SemiMinorAxis);

        var geo = e.ToGeodetic3D(cart);

        Assert.Equal(Math.PI / 2, geo.Latitude, Tolerance);
        Assert.Equal(0.0, geo.Height, Tolerance);
    }

    [Fact]
    public void ToGeodetic3D_SouthPole_ZeroHeight()
    {
        var e = Ellipsoid.Wgs84;
        var cart = new Vector3D(0, 0, -Constants.Wgs84SemiMinorAxis);

        var geo = e.ToGeodetic3D(cart);

        Assert.Equal(-Math.PI / 2, geo.Latitude, Tolerance);
        Assert.Equal(0.0, geo.Height, Tolerance);
    }

    [Theory]
    [InlineData(0.0, 0.0, 0.0)]
    [InlineData(0.0, 0.0, 50000.0)]
    [InlineData(Math.PI / 4, Math.PI / 6, 10000.0)]
    [InlineData(-Math.PI / 3, -Math.PI / 4, 100000.0)]
    [InlineData(Math.PI / 2, Math.PI / 3, 5000.0)]
    [InlineData(0.0, Math.PI / 2, 20000.0)]
    [InlineData(0.0, -Math.PI / 2, 20000.0)]
    [InlineData(1.5, -0.8, -3000.0)]
    public void ToGeodetic3D_RoundTrip_RecoverAllCoordinates(double lon, double lat, double height)
    {
        var e = Ellipsoid.Wgs84;
        var original = new Geodetic3D(lon, lat, height);
        var cart = e.ToVector3D(original);

        var recovered = e.ToGeodetic3D(cart);

        Assert.Equal(lon, recovered.Longitude, Tolerance);
        Assert.Equal(lat, recovered.Latitude, Tolerance);
        Assert.Equal(height, recovered.Height, Tolerance);
    }

    [Fact]
    public void ToGeodetic3D_UnitSphere_RoundTrip()
    {
        var sphere = Ellipsoid.UnitSphere;
        var original = new Geodetic3D(0.7, 0.4, 0.5);
        var cart = sphere.ToVector3D(original);

        var recovered = sphere.ToGeodetic3D(cart);

        Assert.Equal(original.Longitude, recovered.Longitude, Tolerance);
        Assert.Equal(original.Latitude, recovered.Latitude, Tolerance);
        Assert.Equal(original.Height, recovered.Height, Tolerance);
    }

    [Fact]
    public void ToGeodetic3D_HighAltitude_RecoverHeight()
    {
        var e = Ellipsoid.Wgs84;
        double height = 35786000; // geostationary orbit ~35,786 km
        var cart = e.ToVector3D(new Geodetic3D(0, 0, height));

        var geo = e.ToGeodetic3D(cart);

        Assert.Equal(height, geo.Height, 1.0); // 1m tolerance at orbital altitude
    }

    // ───────────────────────────────────────────────
    //  ComputeCurve
    // ───────────────────────────────────────────────

    [Fact]
    public void ComputeCurve_ReturnsStartAndEnd()
    {
        var e = Ellipsoid.UnitSphere;
        var start = new Vector3D(1, 0, 0);
        var end = new Vector3D(0, 1, 0);

        var points = e.ComputeCurve(start, end, Math.PI);

        Assert.Equal(start, points[0]);
        Assert.Equal(end, points[points.Count - 1]);
    }

    [Fact]
    public void ComputeCurve_GranularityLargerThanAngle_ReturnsOnlyStartAndEnd()
    {
        var e = Ellipsoid.UnitSphere;
        var start = new Vector3D(1, 0, 0);
        var end = new Vector3D(0, 1, 0);
        // Angle between them is π/2; granularity > π/2
        double granularity = Math.PI;

        var points = e.ComputeCurve(start, end, granularity);

        Assert.Equal(2, points.Count);
        Assert.Equal(start, points[0]);
        Assert.Equal(end, points[1]);
    }

    [Fact]
    public void ComputeCurve_IntermediatePointCount_MatchesFormula()
    {
        var e = Ellipsoid.UnitSphere;
        var start = new Vector3D(1, 0, 0);
        var end = new Vector3D(0, 1, 0);
        // θ = π/2 ≈ 1.5708, granularity = 0.3
        // n = floor(1.5708 / 0.3) - 1 = 5 - 1 = 4 intermediate points
        // total = 4 + 2 = 6
        double granularity = 0.3;

        var points = e.ComputeCurve(start, end, granularity);

        Assert.Equal(6, points.Count);
    }

    [Fact]
    public void ComputeCurve_SmallerGranularity_ProducesMorePoints()
    {
        var e = Ellipsoid.UnitSphere;
        var start = new Vector3D(1, 0, 0);
        var end = new Vector3D(0, 1, 0);

        var coarse = e.ComputeCurve(start, end, 0.5);
        var fine = e.ComputeCurve(start, end, 0.1);

        Assert.True(fine.Count > coarse.Count);
    }

    [Fact]
    public void ComputeCurve_UnitSphere_IntermediatePointsOnSurface()
    {
        var sphere = Ellipsoid.UnitSphere;
        var start = new Vector3D(1, 0, 0);
        var end = new Vector3D(0, 1, 0);

        var points = sphere.ComputeCurve(start, end, 0.1);

        // On a unit sphere, Rodrigues rotation preserves magnitude,
        // so all intermediate points should lie on the surface
        for (int i = 1; i < points.Count - 1; i++)
        {
            Assert.Equal(1.0, points[i].Magnitude, Tolerance);
        }
    }

    [Fact]
    public void ComputeCurve_AllPointsLieInPlane()
    {
        var e = Ellipsoid.UnitSphere;
        var start = new Vector3D(1, 0, 0);
        var end = new Vector3D(0, 0, 1);
        var planeNormal = start.Cross(end).Normalize();

        var points = e.ComputeCurve(start, end, 0.2);

        // Every point should be perpendicular to the plane normal
        foreach (var p in points)
        {
            Assert.Equal(0.0, p.Dot(planeNormal), Tolerance);
        }
    }

    [Fact]
    public void ComputeCurve_PointsAreOrderedByAngle()
    {
        var e = Ellipsoid.UnitSphere;
        var start = new Vector3D(1, 0, 0);
        var end = new Vector3D(0, 1, 0);

        var points = e.ComputeCurve(start, end, 0.2);

        // Each successive point should have a larger angle from start
        for (int i = 1; i < points.Count; i++)
        {
            double prevAngle = start.AngleBetween(points[i - 1]);
            double currAngle = start.AngleBetween(points[i]);
            Assert.True(currAngle >= prevAngle,
                $"Point {i} angle {currAngle} should be >= point {i - 1} angle {prevAngle}");
        }
    }

    [Fact]
    public void ComputeCurve_Wgs84_PointsLieInPlane()
    {
        var e = Ellipsoid.Wgs84;
        // Two points on the WGS84 surface
        var start = e.ToVector3D(new Geodetic3D(0, 0, 0));
        var end = e.ToVector3D(new Geodetic3D(Math.PI / 4, Math.PI / 6, 0));
        var planeNormal = start.Cross(end).Normalize();

        var points = e.ComputeCurve(start, end, Trigonometry.ToRadians(1));

        foreach (var p in points)
        {
            Assert.Equal(0.0, p.Dot(planeNormal), 0.1); // relaxed tolerance for planetary scale
        }
    }

    [Fact]
    public void ComputeCurve_ClosePoints_ReturnsStartAndEnd()
    {
        var e = Ellipsoid.UnitSphere;
        var start = new Vector3D(1, 0, 0);
        // A point very close to start
        var end = start.RotateAroundAxis(new Vector3D(0, 0, 1), 0.001);
        double granularity = 0.01; // larger than the angle between them

        var points = e.ComputeCurve(start, end, granularity);

        Assert.Equal(2, points.Count);
    }

    [Fact]
    public void ComputeCurve_IntermediatePointsHaveUniformAngularSpacing()
    {
        var e = Ellipsoid.UnitSphere;
        var start = new Vector3D(1, 0, 0);
        var end = new Vector3D(0, 1, 0);
        double granularity = 0.2;

        var points = e.ComputeCurve(start, end, granularity);

        // Check angular spacing between consecutive intermediate points
        for (int i = 1; i < points.Count - 2; i++)
        {
            double angle = start.AngleBetween(points[i]);
            double expectedAngle = i * granularity;
            Assert.Equal(expectedAngle, angle, Tolerance);
        }
    }

    [Fact]
    public void ComputeCurve_3DPath_AllPointsInPlane()
    {
        var e = Ellipsoid.UnitSphere;
        // Two points not on any axis plane
        var start = new Vector3D(1, 1, 0).Normalize();
        var end = new Vector3D(0, 1, 1).Normalize();
        var planeNormal = start.Cross(end).Normalize();

        var points = e.ComputeCurve(start, end, 0.1);

        foreach (var p in points)
        {
            Assert.Equal(0.0, p.Dot(planeNormal), Tolerance);
        }
    }
}
