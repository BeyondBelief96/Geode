using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Geode.Core
{
    /// <summary>
    /// A 4×4 single-precision matrix in <em>column-major memory layout</em> with
    /// <em>column-vector mathematical convention</em> — i.e. translation lives at
    /// (row 0, col 3), (row 1, col 3), (row 2, col 3), and matrices act on column
    /// vectors via <c>M * v</c>. This is OpenGL's native convention; pointer-cast
    /// the struct and feed it to <c>glUniformMatrix4fv</c> with <c>transpose = false</c>
    /// — no per-upload transpose, no flag gymnastics.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The constructor takes its arguments in <em>visual row-by-row order</em>
    /// (first row first), which makes it easy to read off a math matrix and copy
    /// it into code. The struct's fields are then laid out column-by-column in
    /// memory thanks to <see cref="LayoutKind.Sequential"/>, so a <c>float*</c>
    /// pointing at the struct yields the bytes <c>glUniformMatrix4fv</c> expects.
    /// </para>
    /// <para>
    /// Modeled after OpenGlobe's <c>Matrix4F</c>. Composition is column-vector:
    /// to "translate then rotate", write <c>rotation * translation</c>. The
    /// associated transformation is then <c>(rotation * translation) * v</c>,
    /// which is read right-to-left.
    /// </para>
    /// <para>
    /// For converting from <see cref="Matrix4x4"/> (row-vector convention,
    /// row-major memory), use <see cref="FromSystemNumerics(Matrix4x4)"/>. The
    /// conversion is a transpose because the two conventions differ.
    /// </para>
    /// </remarks>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Matrix4F : IEquatable<Matrix4F>
    {
        // Field declaration order = memory order. Sequential layout guarantees
        // these float fields are contiguous and column-major (column 0 first).
        public readonly float Col0Row0;
        public readonly float Col0Row1;
        public readonly float Col0Row2;
        public readonly float Col0Row3;

        public readonly float Col1Row0;
        public readonly float Col1Row1;
        public readonly float Col1Row2;
        public readonly float Col1Row3;

        public readonly float Col2Row0;
        public readonly float Col2Row1;
        public readonly float Col2Row2;
        public readonly float Col2Row3;

        public readonly float Col3Row0;
        public readonly float Col3Row1;
        public readonly float Col3Row2;
        public readonly float Col3Row3;

        /// <summary>
        /// Constructs a matrix from its 16 entries in <em>visual row-by-row order</em>.
        /// The first four arguments are the entries of row 0 (left-to-right),
        /// the next four are row 1, etc.
        /// </summary>
        public Matrix4F(
            float c0r0, float c1r0, float c2r0, float c3r0,
            float c0r1, float c1r1, float c2r1, float c3r1,
            float c0r2, float c1r2, float c2r2, float c3r2,
            float c0r3, float c1r3, float c2r3, float c3r3)
        {
            Col0Row0 = c0r0; Col1Row0 = c1r0; Col2Row0 = c2r0; Col3Row0 = c3r0;
            Col0Row1 = c0r1; Col1Row1 = c1r1; Col2Row1 = c2r1; Col3Row1 = c3r1;
            Col0Row2 = c0r2; Col1Row2 = c1r2; Col2Row2 = c2r2; Col3Row2 = c3r2;
            Col0Row3 = c0r3; Col1Row3 = c1r3; Col2Row3 = c2r3; Col3Row3 = c3r3;
        }

        /// <summary>The 4×4 identity matrix.</summary>
        public static Matrix4F Identity => new(
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1);

        /// <summary>
        /// Translation matrix. Translation lives in column 3 (column-vector
        /// convention), so applying via <c>M * v</c> shifts a position vector
        /// by <c>(x, y, z)</c>.
        /// </summary>
        public static Matrix4F Translation(float x, float y, float z) => new(
            1, 0, 0, x,
            0, 1, 0, y,
            0, 0, 1, z,
            0, 0, 0, 1);

        /// <summary>Rotation around the X axis by <paramref name="radians"/> (right-hand rule).</summary>
        public static Matrix4F RotationX(float radians)
        {
            float c = MathF.Cos(radians);
            float s = MathF.Sin(radians);
            return new Matrix4F(
                1, 0,  0, 0,
                0, c, -s, 0,
                0, s,  c, 0,
                0, 0,  0, 1);
        }

        /// <summary>Rotation around the Y axis by <paramref name="radians"/> (right-hand rule).</summary>
        public static Matrix4F RotationY(float radians)
        {
            float c = MathF.Cos(radians);
            float s = MathF.Sin(radians);
            return new Matrix4F(
                 c, 0, s, 0,
                 0, 1, 0, 0,
                -s, 0, c, 0,
                 0, 0, 0, 1);
        }

        /// <summary>Rotation around the Z axis by <paramref name="radians"/> (right-hand rule).</summary>
        public static Matrix4F RotationZ(float radians)
        {
            float c = MathF.Cos(radians);
            float s = MathF.Sin(radians);
            return new Matrix4F(
                c, -s, 0, 0,
                s,  c, 0, 0,
                0,  0, 1, 0,
                0,  0, 0, 1);
        }

        /// <summary>Uniform scale by <paramref name="s"/> on all three axes.</summary>
        public static Matrix4F Scale(float s) => Scale(s, s, s);

        /// <summary>Non-uniform scale.</summary>
        public static Matrix4F Scale(float x, float y, float z) => new(
            x, 0, 0, 0,
            0, y, 0, 0,
            0, 0, z, 0,
            0, 0, 0, 1);

        /// <summary>
        /// Converts a <see cref="Matrix4x4"/> (row-vector convention) into a
        /// <see cref="Matrix4F"/> (column-vector convention) by transposing.
        /// Use this when you need to feed a System.Numerics matrix into the
        /// rendering pipeline — e.g. a model matrix built with
        /// <c>Matrix4x4.CreateRotationY</c>.
        /// </summary>
        public static Matrix4F FromSystemNumerics(Matrix4x4 m) => new(
            m.M11, m.M21, m.M31, m.M41,
            m.M12, m.M22, m.M32, m.M42,
            m.M13, m.M23, m.M33, m.M43,
            m.M14, m.M24, m.M34, m.M44);

        /// <summary>
        /// Converts back to <see cref="Matrix4x4"/> (row-vector convention) by
        /// transposing. Lossy only in the sense that it changes convention; the
        /// transformation represented is the same.
        /// </summary>
        public Matrix4x4 ToSystemNumerics() => new(
            Col0Row0, Col0Row1, Col0Row2, Col0Row3,
            Col1Row0, Col1Row1, Col1Row2, Col1Row3,
            Col2Row0, Col2Row1, Col2Row2, Col2Row3,
            Col3Row0, Col3Row1, Col3Row2, Col3Row3);

        /// <summary>
        /// Column-vector matrix multiplication: <c>(a * b) * v == a * (b * v)</c>.
        /// Composition reads right-to-left — to "translate then rotate", write
        /// <c>rotation * translation</c>.
        /// </summary>
        public static Matrix4F operator *(Matrix4F a, Matrix4F b) => new(
            // Row 0 of result
            a.Col0Row0 * b.Col0Row0 + a.Col1Row0 * b.Col0Row1 + a.Col2Row0 * b.Col0Row2 + a.Col3Row0 * b.Col0Row3,
            a.Col0Row0 * b.Col1Row0 + a.Col1Row0 * b.Col1Row1 + a.Col2Row0 * b.Col1Row2 + a.Col3Row0 * b.Col1Row3,
            a.Col0Row0 * b.Col2Row0 + a.Col1Row0 * b.Col2Row1 + a.Col2Row0 * b.Col2Row2 + a.Col3Row0 * b.Col2Row3,
            a.Col0Row0 * b.Col3Row0 + a.Col1Row0 * b.Col3Row1 + a.Col2Row0 * b.Col3Row2 + a.Col3Row0 * b.Col3Row3,
            // Row 1
            a.Col0Row1 * b.Col0Row0 + a.Col1Row1 * b.Col0Row1 + a.Col2Row1 * b.Col0Row2 + a.Col3Row1 * b.Col0Row3,
            a.Col0Row1 * b.Col1Row0 + a.Col1Row1 * b.Col1Row1 + a.Col2Row1 * b.Col1Row2 + a.Col3Row1 * b.Col1Row3,
            a.Col0Row1 * b.Col2Row0 + a.Col1Row1 * b.Col2Row1 + a.Col2Row1 * b.Col2Row2 + a.Col3Row1 * b.Col2Row3,
            a.Col0Row1 * b.Col3Row0 + a.Col1Row1 * b.Col3Row1 + a.Col2Row1 * b.Col3Row2 + a.Col3Row1 * b.Col3Row3,
            // Row 2
            a.Col0Row2 * b.Col0Row0 + a.Col1Row2 * b.Col0Row1 + a.Col2Row2 * b.Col0Row2 + a.Col3Row2 * b.Col0Row3,
            a.Col0Row2 * b.Col1Row0 + a.Col1Row2 * b.Col1Row1 + a.Col2Row2 * b.Col1Row2 + a.Col3Row2 * b.Col1Row3,
            a.Col0Row2 * b.Col2Row0 + a.Col1Row2 * b.Col2Row1 + a.Col2Row2 * b.Col2Row2 + a.Col3Row2 * b.Col2Row3,
            a.Col0Row2 * b.Col3Row0 + a.Col1Row2 * b.Col3Row1 + a.Col2Row2 * b.Col3Row2 + a.Col3Row2 * b.Col3Row3,
            // Row 3
            a.Col0Row3 * b.Col0Row0 + a.Col1Row3 * b.Col0Row1 + a.Col2Row3 * b.Col0Row2 + a.Col3Row3 * b.Col0Row3,
            a.Col0Row3 * b.Col1Row0 + a.Col1Row3 * b.Col1Row1 + a.Col2Row3 * b.Col1Row2 + a.Col3Row3 * b.Col1Row3,
            a.Col0Row3 * b.Col2Row0 + a.Col1Row3 * b.Col2Row1 + a.Col2Row3 * b.Col2Row2 + a.Col3Row3 * b.Col2Row3,
            a.Col0Row3 * b.Col3Row0 + a.Col1Row3 * b.Col3Row1 + a.Col2Row3 * b.Col3Row2 + a.Col3Row3 * b.Col3Row3);

        public bool Equals(Matrix4F other) =>
            Col0Row0 == other.Col0Row0 && Col0Row1 == other.Col0Row1 && Col0Row2 == other.Col0Row2 && Col0Row3 == other.Col0Row3 &&
            Col1Row0 == other.Col1Row0 && Col1Row1 == other.Col1Row1 && Col1Row2 == other.Col1Row2 && Col1Row3 == other.Col1Row3 &&
            Col2Row0 == other.Col2Row0 && Col2Row1 == other.Col2Row1 && Col2Row2 == other.Col2Row2 && Col2Row3 == other.Col2Row3 &&
            Col3Row0 == other.Col3Row0 && Col3Row1 == other.Col3Row1 && Col3Row2 == other.Col3Row2 && Col3Row3 == other.Col3Row3;

        public override bool Equals(object? obj) => obj is Matrix4F m && Equals(m);

        public override int GetHashCode() => HashCode.Combine(
            HashCode.Combine(Col0Row0, Col0Row1, Col0Row2, Col0Row3),
            HashCode.Combine(Col1Row0, Col1Row1, Col1Row2, Col1Row3),
            HashCode.Combine(Col2Row0, Col2Row1, Col2Row2, Col2Row3),
            HashCode.Combine(Col3Row0, Col3Row1, Col3Row2, Col3Row3));

        public static bool operator ==(Matrix4F a, Matrix4F b) => a.Equals(b);
        public static bool operator !=(Matrix4F a, Matrix4F b) => !a.Equals(b);
    }
}
