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
}
