using System;

namespace Geode.Core.Tessellation
{
    /// <summary>
    /// Shared helpers used by the subdivision-surface sphere tessellators
    /// (e.g. <see cref="SubdivisionSphereTessellatorSimple"/>). The tessellators
    /// recursively split each triangle of a base polyhedron into four smaller
    /// triangles and project the new midpoints onto the unit sphere.
    /// </summary>
    internal static class SubdivisionUtility
    {
        /// <summary>
        /// Returns the number of triangles produced after <paramref name="numSubdivisions"/>
        /// levels of 4-way triangle subdivision over a 4-triangle base shape.
        /// </summary>
        /// <remarks>
        /// Each subdivision pass replaces every triangle with four smaller triangles,
        /// so level <c>i</c> contributes <c>4 * 4^i</c> triangles. Summing every level
        /// from 0 through <paramref name="numSubdivisions"/> gives the total size to
        /// preallocate for an index buffer that stores all triangles built during the
        /// recursion (not just the leaves). Callers use this to size the output buffer
        /// up front so no reallocation is needed while subdividing.
        /// </remarks>
        /// <param name="numSubdivisions">Number of recursive subdivision passes (&gt;= 0).</param>
        public static int NumberOfTriangles(int numSubdivisions)
        {
            int numberOfTriangles = 0;
            for(int i = 0; i <= numSubdivisions; ++i)
            {
                numberOfTriangles += Convert.ToInt32(Math.Pow(4, i));
            }

            numberOfTriangles *= 4;
            return numberOfTriangles;
        }

        /// <summary>
        /// Returns the number of vertex positions produced after
        /// <paramref name="numSubdivisions"/> levels of 4-way triangle subdivision.
        /// </summary>
        /// <remarks>
        /// Starts from the 4 vertices of the base shape, then at each level adds three
        /// new edge-midpoint vertices per triangle. The "simple" tessellator does not
        /// deduplicate midpoints shared between adjacent triangles, so each level
        /// contributes <c>12 * 4^(i-1)</c> fresh vertices, giving the closed form
        /// <c>4 + 12 * sum_{i=0..n-1} 4^i</c>. Used to preallocate the position
        /// vertex attribute before subdivision begins.
        /// </remarks>
        /// <param name="numSubdivisions">Number of recursive subdivision passes (&gt;= 0).</param>
        public static int NumberOfVertices(int numSubdivisions)
        {
            int numberOfVertices = 0;
            for (int i = 0; i < numSubdivisions; ++i)
            {
                numberOfVertices += Convert.ToInt32(Math.Pow(4, i));
            }
            numberOfVertices = 4 + (12 * numberOfVertices);

            return numberOfVertices;
        }

        /// <summary>
        /// Maps a point on the unit sphere to a 2D texture coordinate in [0, 1]
        /// using an equirectangular (longitude/latitude) projection.
        /// </summary>
        /// <remarks>
        /// <c>U = atan2(y, x) / (2*pi) + 0.5</c> wraps longitude from [-pi, pi] into [0, 1].
        /// <c>V = asin(z) / pi + 0.5</c> maps latitude from [-pi/2, pi/2] into [0, 1].
        /// The input is assumed to already lie on the unit sphere; callers are
        /// responsible for normalizing positions before calling this.
        /// </remarks>
        /// <param name="position">A point on the unit sphere.</param>
        /// <returns>The (u, v) texture coordinate for the point.</returns>
        public static Vector2H ComputeTextureCoordinate(Vector3D position)
        {
            return new Vector2H(Math.Atan2(position.Y, position.X) / Trigonometry.TwoPi + 0.5, Math.Asin(position.Z) / Math.PI + 0.5);
        }
    }
}
