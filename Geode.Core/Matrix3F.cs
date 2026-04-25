using System;
using System.Runtime.InteropServices;

namespace Geode.Core
{
    /// <summary>
    /// A 3×3 single-precision matrix in column-major memory layout with
    /// column-vector mathematical convention. Same conventions and rationale as
    /// <see cref="Matrix4F"/> — see that type for the long explanation.
    /// </summary>
    /// <remarks>
    /// Most commonly used as the normal matrix — the inverse-transpose of the
    /// upper 3×3 of the model-view matrix — which transforms normals from
    /// model space to eye space without scale distortion.
    /// </remarks>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Matrix3F : IEquatable<Matrix3F>
    {
        public readonly float Col0Row0;
        public readonly float Col0Row1;
        public readonly float Col0Row2;

        public readonly float Col1Row0;
        public readonly float Col1Row1;
        public readonly float Col1Row2;

        public readonly float Col2Row0;
        public readonly float Col2Row1;
        public readonly float Col2Row2;

        /// <summary>Constructs a matrix from its 9 entries in visual row-by-row order.</summary>
        public Matrix3F(
            float c0r0, float c1r0, float c2r0,
            float c0r1, float c1r1, float c2r1,
            float c0r2, float c1r2, float c2r2)
        {
            Col0Row0 = c0r0; Col1Row0 = c1r0; Col2Row0 = c2r0;
            Col0Row1 = c0r1; Col1Row1 = c1r1; Col2Row1 = c2r1;
            Col0Row2 = c0r2; Col1Row2 = c1r2; Col2Row2 = c2r2;
        }

        /// <summary>The 3×3 identity matrix.</summary>
        public static Matrix3F Identity => new(
            1, 0, 0,
            0, 1, 0,
            0, 0, 1);

        public bool Equals(Matrix3F other) =>
            Col0Row0 == other.Col0Row0 && Col0Row1 == other.Col0Row1 && Col0Row2 == other.Col0Row2 &&
            Col1Row0 == other.Col1Row0 && Col1Row1 == other.Col1Row1 && Col1Row2 == other.Col1Row2 &&
            Col2Row0 == other.Col2Row0 && Col2Row1 == other.Col2Row1 && Col2Row2 == other.Col2Row2;

        public override bool Equals(object? obj) => obj is Matrix3F m && Equals(m);

        public override int GetHashCode() => HashCode.Combine(
            HashCode.Combine(Col0Row0, Col0Row1, Col0Row2),
            HashCode.Combine(Col1Row0, Col1Row1, Col1Row2),
            HashCode.Combine(Col2Row0, Col2Row1, Col2Row2));

        public static bool operator ==(Matrix3F a, Matrix3F b) => a.Equals(b);
        public static bool operator !=(Matrix3F a, Matrix3F b) => !a.Equals(b);
    }
}
