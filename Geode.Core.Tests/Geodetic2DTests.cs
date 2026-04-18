using System;
using Xunit;

namespace Geode.Core.Tests;

public class Geodetic2DTests
{
    // ───────────────────────────────────────────────
    //  Constructor
    // ───────────────────────────────────────────────

    [Fact]
    public void Constructor_SetsLongitudeAndLatitude()
    {
        var g = new Geodetic2D(1.5, 0.8);

        Assert.Equal(1.5, g.Longitude);
        Assert.Equal(0.8, g.Latitude);
    }

    [Fact]
    public void Constructor_ZeroValues()
    {
        var g = new Geodetic2D(0, 0);

        Assert.Equal(0.0, g.Longitude);
        Assert.Equal(0.0, g.Latitude);
    }

    [Fact]
    public void Constructor_NegativeValues()
    {
        var g = new Geodetic2D(-Math.PI, -Math.PI / 2);

        Assert.Equal(-Math.PI, g.Longitude);
        Assert.Equal(-Math.PI / 2, g.Latitude);
    }

    [Fact]
    public void Constructor_BoundaryValues()
    {
        var g = new Geodetic2D(Math.PI, Math.PI / 2);

        Assert.Equal(Math.PI, g.Longitude);
        Assert.Equal(Math.PI / 2, g.Latitude);
    }

    // ───────────────────────────────────────────────
    //  Equality
    // ───────────────────────────────────────────────

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        var a = new Geodetic2D(1.0, 0.5);
        var b = new Geodetic2D(1.0, 0.5);

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Equals_DifferentLongitude_ReturnsFalse()
    {
        var a = new Geodetic2D(1.0, 0.5);
        var b = new Geodetic2D(2.0, 0.5);

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_DifferentLatitude_ReturnsFalse()
    {
        var a = new Geodetic2D(1.0, 0.5);
        var b = new Geodetic2D(1.0, 0.6);

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_ObjectOverload_SameValues()
    {
        var a = new Geodetic2D(1.0, 0.5);
        object b = new Geodetic2D(1.0, 0.5);

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Equals_ObjectOverload_DifferentType()
    {
        var g = new Geodetic2D(1.0, 0.5);

        Assert.False(g.Equals("not a geodetic"));
        Assert.False(g.Equals(null));
    }

    // ───────────────────────────────────────────────
    //  Operators
    // ───────────────────────────────────────────────

    [Fact]
    public void EqualityOperator_SameValues_ReturnsTrue()
    {
        var a = new Geodetic2D(1.0, 0.5);
        var b = new Geodetic2D(1.0, 0.5);

        Assert.True(a == b);
    }

    [Fact]
    public void InequalityOperator_DifferentValues_ReturnsTrue()
    {
        var a = new Geodetic2D(1.0, 0.5);
        var b = new Geodetic2D(1.0, 0.6);

        Assert.True(a != b);
    }

    [Fact]
    public void InequalityOperator_SameValues_ReturnsFalse()
    {
        var a = new Geodetic2D(1.0, 0.5);
        var b = new Geodetic2D(1.0, 0.5);

        Assert.False(a != b);
    }

    // ───────────────────────────────────────────────
    //  GetHashCode
    // ───────────────────────────────────────────────

    [Fact]
    public void GetHashCode_EqualInstances_SameHash()
    {
        var a = new Geodetic2D(1.0, 0.5);
        var b = new Geodetic2D(1.0, 0.5);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentInstances_LikelyDifferentHash()
    {
        var a = new Geodetic2D(1.0, 0.5);
        var b = new Geodetic2D(2.0, 0.5);

        // Not guaranteed, but highly likely for distinct values
        Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
    }
}
