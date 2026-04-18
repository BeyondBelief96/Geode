using System;
using Xunit;

namespace Geode.Core.Tests;

public class Vector3DTests
{
    private const double Tolerance = 1e-6;

    // ───────────────────────────────────────────────
    //  Constructor & properties
    // ───────────────────────────────────────────────

    [Fact]
    public void Constructor_SetsComponents()
    {
        var v = new Vector3D(1.5, -2.3, 4.7);

        Assert.Equal(1.5, v.X);
        Assert.Equal(-2.3, v.Y);
        Assert.Equal(4.7, v.Z);
    }

    [Fact]
    public void Default_IsZeroVector()
    {
        var v = default(Vector3D);

        Assert.Equal(0.0, v.X);
        Assert.Equal(0.0, v.Y);
        Assert.Equal(0.0, v.Z);
    }

    // ───────────────────────────────────────────────
    //  Magnitude
    // ───────────────────────────────────────────────

    [Fact]
    public void MagnitudeSquared_ReturnsSquaredLength()
    {
        var v = new Vector3D(3, 4, 0);

        Assert.Equal(25.0, v.MagnitudeSquared, Tolerance);
    }

    [Fact]
    public void Magnitude_ReturnsLength()
    {
        var v = new Vector3D(3, 4, 0);

        Assert.Equal(5.0, v.Magnitude, Tolerance);
    }

    [Fact]
    public void Magnitude_3D_ReturnsCorrectLength()
    {
        var v = new Vector3D(1, 2, 2);

        Assert.Equal(3.0, v.Magnitude, Tolerance);
    }

    [Fact]
    public void Magnitude_ZeroVector_ReturnsZero()
    {
        var v = new Vector3D(0, 0, 0);

        Assert.Equal(0.0, v.Magnitude);
    }

    [Fact]
    public void Magnitude_UnitVectors_ReturnOne()
    {
        Assert.Equal(1.0, new Vector3D(1, 0, 0).Magnitude, Tolerance);
        Assert.Equal(1.0, new Vector3D(0, 1, 0).Magnitude, Tolerance);
        Assert.Equal(1.0, new Vector3D(0, 0, 1).Magnitude, Tolerance);
    }

    // ───────────────────────────────────────────────
    //  Normalize
    // ───────────────────────────────────────────────

    [Fact]
    public void Normalize_ReturnsUnitVector()
    {
        var v = new Vector3D(3, 4, 0);

        var n = v.Normalize();

        Assert.Equal(1.0, n.Magnitude, Tolerance);
    }

    [Fact]
    public void Normalize_PreservesDirection()
    {
        var v = new Vector3D(3, 4, 0);

        var n = v.Normalize();

        Assert.Equal(0.6, n.X, Tolerance);
        Assert.Equal(0.8, n.Y, Tolerance);
        Assert.Equal(0.0, n.Z, Tolerance);
    }

    [Fact]
    public void Normalize_AlreadyUnit_ReturnsSame()
    {
        var v = new Vector3D(1, 0, 0);

        var n = v.Normalize();

        Assert.Equal(1.0, n.X, Tolerance);
        Assert.Equal(0.0, n.Y, Tolerance);
        Assert.Equal(0.0, n.Z, Tolerance);
    }

    [Fact]
    public void Normalize_NegativeComponents()
    {
        var v = new Vector3D(-3, -4, 0);

        var n = v.Normalize();

        Assert.Equal(-0.6, n.X, Tolerance);
        Assert.Equal(-0.8, n.Y, Tolerance);
    }

    // ───────────────────────────────────────────────
    //  Dot product
    // ───────────────────────────────────────────────

    [Fact]
    public void Dot_PerpendicularVectors_ReturnsZero()
    {
        var a = new Vector3D(1, 0, 0);
        var b = new Vector3D(0, 1, 0);

        Assert.Equal(0.0, a.Dot(b), Tolerance);
    }

    [Fact]
    public void Dot_ParallelVectors_ReturnsProduct()
    {
        var a = new Vector3D(2, 0, 0);
        var b = new Vector3D(3, 0, 0);

        Assert.Equal(6.0, a.Dot(b), Tolerance);
    }

    [Fact]
    public void Dot_AntiParallelVectors_ReturnsNegative()
    {
        var a = new Vector3D(1, 0, 0);
        var b = new Vector3D(-1, 0, 0);

        Assert.Equal(-1.0, a.Dot(b), Tolerance);
    }

    [Fact]
    public void Dot_GeneralCase()
    {
        var a = new Vector3D(1, 2, 3);
        var b = new Vector3D(4, 5, 6);

        // 1*4 + 2*5 + 3*6 = 32
        Assert.Equal(32.0, a.Dot(b), Tolerance);
    }

    [Fact]
    public void Dot_IsCommutative()
    {
        var a = new Vector3D(1, 2, 3);
        var b = new Vector3D(4, 5, 6);

        Assert.Equal(a.Dot(b), b.Dot(a), Tolerance);
    }

    // ───────────────────────────────────────────────
    //  Cross product
    // ───────────────────────────────────────────────

    [Fact]
    public void Cross_XCrossY_ReturnsZ()
    {
        var x = new Vector3D(1, 0, 0);
        var y = new Vector3D(0, 1, 0);

        var result = x.Cross(y);

        Assert.Equal(0.0, result.X, Tolerance);
        Assert.Equal(0.0, result.Y, Tolerance);
        Assert.Equal(1.0, result.Z, Tolerance);
    }

    [Fact]
    public void Cross_YCrossX_ReturnsNegZ()
    {
        var x = new Vector3D(1, 0, 0);
        var y = new Vector3D(0, 1, 0);

        var result = y.Cross(x);

        Assert.Equal(0.0, result.X, Tolerance);
        Assert.Equal(0.0, result.Y, Tolerance);
        Assert.Equal(-1.0, result.Z, Tolerance);
    }

    [Fact]
    public void Cross_ParallelVectors_ReturnsZero()
    {
        var a = new Vector3D(1, 2, 3);
        var b = new Vector3D(2, 4, 6);

        var result = a.Cross(b);

        Assert.Equal(0.0, result.X, Tolerance);
        Assert.Equal(0.0, result.Y, Tolerance);
        Assert.Equal(0.0, result.Z, Tolerance);
    }

    [Fact]
    public void Cross_ResultIsPerpendicularToBothInputs()
    {
        var a = new Vector3D(1, 2, 3);
        var b = new Vector3D(4, 5, 6);

        var cross = a.Cross(b);

        Assert.Equal(0.0, cross.Dot(a), Tolerance);
        Assert.Equal(0.0, cross.Dot(b), Tolerance);
    }

    [Fact]
    public void Cross_GeneralCase()
    {
        var a = new Vector3D(1, 2, 3);
        var b = new Vector3D(4, 5, 6);

        var result = a.Cross(b);

        // (2*6 - 3*5, 3*4 - 1*6, 1*5 - 2*4) = (-3, 6, -3)
        Assert.Equal(-3.0, result.X, Tolerance);
        Assert.Equal(6.0, result.Y, Tolerance);
        Assert.Equal(-3.0, result.Z, Tolerance);
    }

    // ───────────────────────────────────────────────
    //  MultiplyComponents
    // ───────────────────────────────────────────────

    [Fact]
    public void MultiplyComponents_ElementWise()
    {
        var a = new Vector3D(2, 3, 4);
        var b = new Vector3D(5, 6, 7);

        var result = a.MultiplyComponents(b);

        Assert.Equal(10.0, result.X, Tolerance);
        Assert.Equal(18.0, result.Y, Tolerance);
        Assert.Equal(28.0, result.Z, Tolerance);
    }

    [Fact]
    public void MultiplyComponents_ByOnes_ReturnsSame()
    {
        var v = new Vector3D(2, 3, 4);
        var ones = new Vector3D(1, 1, 1);

        var result = v.MultiplyComponents(ones);

        Assert.Equal(v.X, result.X);
        Assert.Equal(v.Y, result.Y);
        Assert.Equal(v.Z, result.Z);
    }

    [Fact]
    public void MultiplyComponents_ByZeros_ReturnsZero()
    {
        var v = new Vector3D(2, 3, 4);
        var zeros = new Vector3D(0, 0, 0);

        var result = v.MultiplyComponents(zeros);

        Assert.Equal(0.0, result.X);
        Assert.Equal(0.0, result.Y);
        Assert.Equal(0.0, result.Z);
    }

    // ───────────────────────────────────────────────
    //  AngleBetween
    // ───────────────────────────────────────────────

    [Fact]
    public void AngleBetween_PerpendicularVectors_ReturnsHalfPi()
    {
        var a = new Vector3D(1, 0, 0);
        var b = new Vector3D(0, 1, 0);

        Assert.Equal(Math.PI / 2, a.AngleBetween(b), Tolerance);
    }

    [Fact]
    public void AngleBetween_SameDirection_ReturnsZero()
    {
        var a = new Vector3D(1, 0, 0);
        var b = new Vector3D(5, 0, 0);

        Assert.Equal(0.0, a.AngleBetween(b), Tolerance);
    }

    [Fact]
    public void AngleBetween_OppositeDirection_ReturnsPi()
    {
        var a = new Vector3D(1, 0, 0);
        var b = new Vector3D(-1, 0, 0);

        Assert.Equal(Math.PI, a.AngleBetween(b), Tolerance);
    }

    [Fact]
    public void AngleBetween_45Degrees()
    {
        var a = new Vector3D(1, 0, 0);
        var b = new Vector3D(1, 1, 0);

        Assert.Equal(Math.PI / 4, a.AngleBetween(b), Tolerance);
    }

    [Fact]
    public void AngleBetween_IsCommutative()
    {
        var a = new Vector3D(1, 2, 3);
        var b = new Vector3D(4, 5, 6);

        Assert.Equal(a.AngleBetween(b), b.AngleBetween(a), Tolerance);
    }

    [Fact]
    public void AngleBetween_IndependentOfMagnitude()
    {
        var a = new Vector3D(1, 0, 0);
        var b = new Vector3D(0, 1, 0);
        var bScaled = new Vector3D(0, 100, 0);

        Assert.Equal(a.AngleBetween(b), a.AngleBetween(bScaled), Tolerance);
    }

    // ───────────────────────────────────────────────
    //  RotateAroundAxis
    // ───────────────────────────────────────────────

    [Fact]
    public void RotateAroundAxis_90DegreesAroundZ()
    {
        var v = new Vector3D(1, 0, 0);
        var axis = new Vector3D(0, 0, 1);

        var result = v.RotateAroundAxis(axis, Math.PI / 2);

        Assert.Equal(0.0, result.X, Tolerance);
        Assert.Equal(1.0, result.Y, Tolerance);
        Assert.Equal(0.0, result.Z, Tolerance);
    }

    [Fact]
    public void RotateAroundAxis_180DegreesAroundZ()
    {
        var v = new Vector3D(1, 0, 0);
        var axis = new Vector3D(0, 0, 1);

        var result = v.RotateAroundAxis(axis, Math.PI);

        Assert.Equal(-1.0, result.X, Tolerance);
        Assert.Equal(0.0, result.Y, Tolerance);
        Assert.Equal(0.0, result.Z, Tolerance);
    }

    [Fact]
    public void RotateAroundAxis_360Degrees_ReturnsSame()
    {
        var v = new Vector3D(1, 2, 3);
        var axis = new Vector3D(0, 0, 1);

        var result = v.RotateAroundAxis(axis, 2 * Math.PI);

        Assert.Equal(v.X, result.X, Tolerance);
        Assert.Equal(v.Y, result.Y, Tolerance);
        Assert.Equal(v.Z, result.Z, Tolerance);
    }

    [Fact]
    public void RotateAroundAxis_ZeroAngle_ReturnsSame()
    {
        var v = new Vector3D(1, 2, 3);
        var axis = new Vector3D(0, 0, 1);

        var result = v.RotateAroundAxis(axis, 0);

        Assert.Equal(v.X, result.X, Tolerance);
        Assert.Equal(v.Y, result.Y, Tolerance);
        Assert.Equal(v.Z, result.Z, Tolerance);
    }

    [Fact]
    public void RotateAroundAxis_PreservesMagnitude()
    {
        var v = new Vector3D(3, 4, 5);
        var axis = new Vector3D(1, 1, 1);

        var result = v.RotateAroundAxis(axis, 1.23);

        Assert.Equal(v.Magnitude, result.Magnitude, Tolerance);
    }

    [Fact]
    public void RotateAroundAxis_AlongAxis_ReturnsSame()
    {
        var v = new Vector3D(0, 0, 5);
        var axis = new Vector3D(0, 0, 1);

        var result = v.RotateAroundAxis(axis, Math.PI / 3);

        Assert.Equal(0.0, result.X, Tolerance);
        Assert.Equal(0.0, result.Y, Tolerance);
        Assert.Equal(5.0, result.Z, Tolerance);
    }

    [Fact]
    public void RotateAroundAxis_90DegreesAroundX()
    {
        var v = new Vector3D(0, 1, 0);
        var axis = new Vector3D(1, 0, 0);

        var result = v.RotateAroundAxis(axis, Math.PI / 2);

        Assert.Equal(0.0, result.X, Tolerance);
        Assert.Equal(0.0, result.Y, Tolerance);
        Assert.Equal(1.0, result.Z, Tolerance);
    }

    [Fact]
    public void RotateAroundAxis_UnnormalizedAxis_StillWorks()
    {
        var v = new Vector3D(1, 0, 0);
        var axis = new Vector3D(0, 0, 10); // not unit length

        var result = v.RotateAroundAxis(axis, Math.PI / 2);

        Assert.Equal(0.0, result.X, Tolerance);
        Assert.Equal(1.0, result.Y, Tolerance);
        Assert.Equal(0.0, result.Z, Tolerance);
    }

    // ───────────────────────────────────────────────
    //  Arithmetic operators
    // ───────────────────────────────────────────────

    [Fact]
    public void Add_TwoVectors()
    {
        var a = new Vector3D(1, 2, 3);
        var b = new Vector3D(4, 5, 6);

        var result = a + b;

        Assert.Equal(5.0, result.X);
        Assert.Equal(7.0, result.Y);
        Assert.Equal(9.0, result.Z);
    }

    [Fact]
    public void Subtract_TwoVectors()
    {
        var a = new Vector3D(4, 5, 6);
        var b = new Vector3D(1, 2, 3);

        var result = a - b;

        Assert.Equal(3.0, result.X);
        Assert.Equal(3.0, result.Y);
        Assert.Equal(3.0, result.Z);
    }

    [Fact]
    public void Multiply_VectorByScalar()
    {
        var v = new Vector3D(1, 2, 3);

        var result = v * 2;

        Assert.Equal(2.0, result.X);
        Assert.Equal(4.0, result.Y);
        Assert.Equal(6.0, result.Z);
    }

    [Fact]
    public void Multiply_ScalarByVector()
    {
        var v = new Vector3D(1, 2, 3);

        var result = 2 * v;

        Assert.Equal(2.0, result.X);
        Assert.Equal(4.0, result.Y);
        Assert.Equal(6.0, result.Z);
    }

    [Fact]
    public void Divide_VectorByScalar()
    {
        var v = new Vector3D(2, 4, 6);

        var result = v / 2;

        Assert.Equal(1.0, result.X);
        Assert.Equal(2.0, result.Y);
        Assert.Equal(3.0, result.Z);
    }

    [Fact]
    public void Negate_Vector()
    {
        var v = new Vector3D(1, -2, 3);

        var result = -v;

        Assert.Equal(-1.0, result.X);
        Assert.Equal(2.0, result.Y);
        Assert.Equal(-3.0, result.Z);
    }

    // ───────────────────────────────────────────────
    //  Equality
    // ───────────────────────────────────────────────

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        var a = new Vector3D(1, 2, 3);
        var b = new Vector3D(1, 2, 3);

        Assert.True(a.Equals(b));
        Assert.True(a == b);
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        var a = new Vector3D(1, 2, 3);
        var b = new Vector3D(1, 2, 4);

        Assert.False(a.Equals(b));
        Assert.True(a != b);
    }

    [Fact]
    public void Equals_ObjectOverload_Works()
    {
        var a = new Vector3D(1, 2, 3);
        object b = new Vector3D(1, 2, 3);

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Equals_NonVector3D_ReturnsFalse()
    {
        var v = new Vector3D(1, 2, 3);

        Assert.False(v.Equals("not a vector"));
        Assert.False(v.Equals(null));
    }

    [Fact]
    public void GetHashCode_EqualVectors_SameHash()
    {
        var a = new Vector3D(1, 2, 3);
        var b = new Vector3D(1, 2, 3);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        var v = new Vector3D(1, 2, 3);

        Assert.Equal("(1, 2, 3)", v.ToString());
    }
}
