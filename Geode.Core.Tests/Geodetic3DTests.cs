using System;
using Xunit;

namespace Geode.Core.Tests;

public class Geodetic3DTests
{
    // ───────────────────────────────────────────────
    //  Constructor – three parameters
    // ───────────────────────────────────────────────

    [Fact]
    public void Constructor_SetsAllFields()
    {
        var g = new Geodetic3D(1.5, 0.8, 1000);

        Assert.Equal(1.5, g.Longitude);
        Assert.Equal(0.8, g.Latitude);
        Assert.Equal(1000.0, g.Height);
    }

    [Fact]
    public void Constructor_DefaultHeight_IsZero()
    {
        var g = new Geodetic3D(1.5, 0.8);

        Assert.Equal(1.5, g.Longitude);
        Assert.Equal(0.8, g.Latitude);
        Assert.Equal(0.0, g.Height);
    }

    [Fact]
    public void Constructor_NegativeHeight()
    {
        var g = new Geodetic3D(0, 0, -500);

        Assert.Equal(-500.0, g.Height);
    }

    [Fact]
    public void Constructor_NegativeCoords()
    {
        var g = new Geodetic3D(-Math.PI, -Math.PI / 2, 0);

        Assert.Equal(-Math.PI, g.Longitude);
        Assert.Equal(-Math.PI / 2, g.Latitude);
    }

    // ───────────────────────────────────────────────
    //  Constructor – from Geodetic2D
    // ───────────────────────────────────────────────

    [Fact]
    public void Constructor_FromGeodetic2D_CopiesLonLat()
    {
        var g2 = new Geodetic2D(1.0, 0.5);
        var g3 = new Geodetic3D(g2);

        Assert.Equal(1.0, g3.Longitude);
        Assert.Equal(0.5, g3.Latitude);
        Assert.Equal(0.0, g3.Height);
    }

    [Fact]
    public void Constructor_FromGeodetic2D_WithHeight()
    {
        var g2 = new Geodetic2D(1.0, 0.5);
        var g3 = new Geodetic3D(g2, 5000);

        Assert.Equal(1.0, g3.Longitude);
        Assert.Equal(0.5, g3.Latitude);
        Assert.Equal(5000.0, g3.Height);
    }

    [Fact]
    public void Constructor_FromGeodetic2D_DefaultHeight_IsZero()
    {
        var g2 = new Geodetic2D(1.0, 0.5);
        var g3 = new Geodetic3D(g2);

        Assert.Equal(0.0, g3.Height);
    }

    // ───────────────────────────────────────────────
    //  Equality
    // ───────────────────────────────────────────────

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        var a = new Geodetic3D(1.0, 0.5, 100);
        var b = new Geodetic3D(1.0, 0.5, 100);

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Equals_DifferentLongitude_ReturnsFalse()
    {
        var a = new Geodetic3D(1.0, 0.5, 100);
        var b = new Geodetic3D(2.0, 0.5, 100);

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_DifferentLatitude_ReturnsFalse()
    {
        var a = new Geodetic3D(1.0, 0.5, 100);
        var b = new Geodetic3D(1.0, 0.6, 100);

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_DifferentHeight_ReturnsFalse()
    {
        var a = new Geodetic3D(1.0, 0.5, 100);
        var b = new Geodetic3D(1.0, 0.5, 200);

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_ObjectOverload_SameValues()
    {
        var a = new Geodetic3D(1.0, 0.5, 100);
        object b = new Geodetic3D(1.0, 0.5, 100);

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Equals_ObjectOverload_DifferentType()
    {
        var g = new Geodetic3D(1.0, 0.5, 100);

        Assert.False(g.Equals("not a geodetic"));
        Assert.False(g.Equals(null));
    }

    [Fact]
    public void Equals_IEquatable_SameValues()
    {
        IEquatable<Geodetic3D> a = new Geodetic3D(1.0, 0.5, 100);
        var b = new Geodetic3D(1.0, 0.5, 100);

        Assert.True(a.Equals(b));
    }

    // ───────────────────────────────────────────────
    //  Operators
    // ───────────────────────────────────────────────

    [Fact]
    public void EqualityOperator_SameValues_ReturnsTrue()
    {
        var a = new Geodetic3D(1.0, 0.5, 100);
        var b = new Geodetic3D(1.0, 0.5, 100);

        Assert.True(a == b);
    }

    [Fact]
    public void InequalityOperator_DifferentValues_ReturnsTrue()
    {
        var a = new Geodetic3D(1.0, 0.5, 100);
        var b = new Geodetic3D(1.0, 0.5, 200);

        Assert.True(a != b);
    }

    [Fact]
    public void InequalityOperator_SameValues_ReturnsFalse()
    {
        var a = new Geodetic3D(1.0, 0.5, 100);
        var b = new Geodetic3D(1.0, 0.5, 100);

        Assert.False(a != b);
    }

    // ───────────────────────────────────────────────
    //  GetHashCode
    // ───────────────────────────────────────────────

    [Fact]
    public void GetHashCode_EqualInstances_SameHash()
    {
        var a = new Geodetic3D(1.0, 0.5, 100);
        var b = new Geodetic3D(1.0, 0.5, 100);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentInstances_LikelyDifferentHash()
    {
        var a = new Geodetic3D(1.0, 0.5, 100);
        var b = new Geodetic3D(1.0, 0.5, 200);

        Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
    }

    // ───────────────────────────────────────────────
    //  Default height consistency
    // ───────────────────────────────────────────────

    [Fact]
    public void DefaultHeight_EqualsExplicitZero()
    {
        var implicit_ = new Geodetic3D(1.0, 0.5);
        var explicit_ = new Geodetic3D(1.0, 0.5, 0.0);

        Assert.Equal(implicit_, explicit_);
    }

    [Fact]
    public void FromGeodetic2D_EqualsDirectConstruction()
    {
        var g2 = new Geodetic2D(1.0, 0.5);
        var fromG2 = new Geodetic3D(g2, 300);
        var direct = new Geodetic3D(1.0, 0.5, 300);

        Assert.Equal(fromG2, direct);
    }
}
