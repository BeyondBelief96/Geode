using Geode.Core.Geometry;
using System;
using System.Collections.Generic;

namespace Geode.Core.Tessellation
{
    [Flags]
    public enum CubeMapEllipsoidVertexAttributes
    {
        Position = 1,
        Normal = 2,
        TextureCoordinates = 4,
        All = Position | Normal | TextureCoordinates
    }
    public static class CubeMapEllipsoidTessellator
    {
        internal class CubeMapMesh
        {
            public Ellipsoid Ellipsoid { get; set; }
            public int NumberOfPartitions { get; set; }
            public IList<Vector3D> Positions { get; set; }
            public IList<Vector3H> Normals { get; set; }
            public IList<Vector2H> TextureCoordinates { get; set; }
            public IndicesUnsignedInt Indices { get; set; }
        }

        public static Mesh Compute(Ellipsoid ellipsoid, int numPartitions, CubeMapEllipsoidVertexAttributes vertexAttributes)
        {
            if(numPartitions < 0)
            {
                throw new ArgumentOutOfRangeException($"numSubdivisions must be non-negative, but was {numPartitions}", nameof(numPartitions));
            }

            Mesh mesh = new Mesh
            {
                PrimitiveType = PrimitiveType.Triangles,
                FrontFaceWindingOrder = WindingOrder.CounterClockwise
            };

            int numberOfVertices = NumberOfVertices(numPartitions);
            VertexAttributeDoubleVector3 positionsAttribute = new VertexAttributeDoubleVector3("position", numberOfVertices);
            mesh.Attributes.Add(positionsAttribute);

            IndicesUnsignedInt indices = new IndicesUnsignedInt(3 * NumberOfTriangles(numPartitions));
            mesh.Indices = indices;

            CubeMapMesh cubeMapMesh = new CubeMapMesh();
            cubeMapMesh.Ellipsoid = ellipsoid;
            cubeMapMesh.NumberOfPartitions = numPartitions;
            cubeMapMesh.Positions = positionsAttribute.Values;
            cubeMapMesh.Indices = indices;

            if((vertexAttributes & CubeMapEllipsoidVertexAttributes.Normal) == CubeMapEllipsoidVertexAttributes.Normal)
            {
                VertexAttributeHalfFloatVector3 normalsAttribute = new VertexAttributeHalfFloatVector3("normal", numberOfVertices);
                mesh.Attributes.Add(normalsAttribute);
                cubeMapMesh.Normals = normalsAttribute.Values;
            }

            if((vertexAttributes & CubeMapEllipsoidVertexAttributes.TextureCoordinates) == CubeMapEllipsoidVertexAttributes.TextureCoordinates)
            {
                VertexAttributeHalfFloatVector2 textureCoordinatesAttribute = new VertexAttributeHalfFloatVector2("texCoord", numberOfVertices);
                mesh.Attributes.Add(textureCoordinatesAttribute);
                cubeMapMesh.TextureCoordinates = textureCoordinatesAttribute.Values;
            }

            // Initial cube vertices, rotated by 45 degrees around the Z axis.
            cubeMapMesh.Positions.Add(new Vector3D(-1, 0, -1));
            cubeMapMesh.Positions.Add(new Vector3D(0, -1, -1));
            cubeMapMesh.Positions.Add(new Vector3D(1, 0, -1));
            cubeMapMesh.Positions.Add(new Vector3D(0, 1, -1));
            cubeMapMesh.Positions.Add(new Vector3D(-1, 0, 1));
            cubeMapMesh.Positions.Add(new Vector3D(0, -1, 1));
            cubeMapMesh.Positions.Add(new Vector3D(1, 0, 1));
            cubeMapMesh.Positions.Add(new Vector3D(0, 1, 1));

            // Add edges connecting the bottom vertices
            int[] edge0to1 = AddEdgePositions(0, 1, cubeMapMesh);
            int[] edge1to2 = AddEdgePositions(1, 2, cubeMapMesh);
            int[] edge2to3 = AddEdgePositions(2, 3, cubeMapMesh);
            int[] edge3to0 = AddEdgePositions(3, 0, cubeMapMesh);

            // Add edges connecting the top vertices
            int[] edge4to5 = AddEdgePositions(4, 5, cubeMapMesh);
            int[] edge5to6 = AddEdgePositions(5, 6, cubeMapMesh);
            int[] edge6to7 = AddEdgePositions(6, 7, cubeMapMesh);
            int[] edge7to4 = AddEdgePositions(7, 4, cubeMapMesh);

            // Add edges connecting the top and bottom vertices
            int[] edge0to4 = AddEdgePositions(0, 4, cubeMapMesh);
            int[] edge1to5 = AddEdgePositions(1, 5, cubeMapMesh);
            int[] edge2to6 = AddEdgePositions(2, 6, cubeMapMesh);
            int[] edge3to7 = AddEdgePositions(3, 7, cubeMapMesh);

            AddFaceTriangles(edge0to4, edge0to1, edge1to5, edge4to5, cubeMapMesh); // Q3 Face
            AddFaceTriangles(edge1to5, edge1to2, edge2to6, edge5to6, cubeMapMesh); // Q4 Face
            AddFaceTriangles(edge2to6, edge2to3, edge3to7, edge6to7, cubeMapMesh); // Q1 Face
            AddFaceTriangles(edge3to7, edge3to0, edge0to4, edge7to4, cubeMapMesh); // Q2 Face
            AddFaceTriangles(ReversedArray(edge7to4), edge4to5, edge5to6, ReversedArray(edge6to7), cubeMapMesh); // Plane z = 1
            AddFaceTriangles(edge1to2, ReversedArray(edge0to1), ReversedArray(edge3to0), edge2to3, cubeMapMesh); // Plane z = -1

            CubeToEllipsoid(cubeMapMesh);

            return mesh;
        }

        private static int[] AddEdgePositions(int i0, int i1, CubeMapMesh cubeMapMesh)
        {
            var positions = cubeMapMesh.Positions;
            var numPartitions = cubeMapMesh.NumberOfPartitions;
            int[] indices = new int[2 + numPartitions - 1];
            indices[0] = i0;
            indices[indices.Length - 1] = i1;

            Vector3D origin = positions[i0];
            Vector3D direction = positions[i1] - positions[i0];

            for(int i = 1; i < numPartitions; i++)
            {
                double delta = i / (double)numPartitions;
                indices[i] = positions.Count;
                positions.Add(origin + (delta * direction));
            }

            return indices;
        }

        /// <summary>
        /// Fills the interior of one cube face with a grid of vertices and emits
        /// the triangles covering it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// By the time this runs, the four edges of the face already exist in
        /// <c>positions</c>. Each edge array holds <c>numPartitions + 1</c>
        /// vertex indices (the two corners plus the partition points between
        /// them). The four edges share corners with their neighbors, so we must
        /// not regenerate those — we consume them via the index arrays.
        /// </para>
        /// <para>
        /// Picture one face in its own 2D (u, v) frame, with the bottom-left
        /// corner at the origin. The face is an (N+1) × (N+1) lattice of points
        /// where N = numPartitions:
        /// <code>
        ///   topLeftToRight[0] ───── topLeftToRight[N]
        ///         │       ·   ·   ·       │
        ///  leftBottomToTop[j]   ·   rightBottomToTop[j]      row j
        ///         │       ·   ·   ·       │
        ///   bottomLeftToRight[0] ── bottomLeftToRight[N]
        /// </code>
        /// Any point on the face is <c>origin + (i/N)·x + (j/N)·y</c>, where
        /// <c>x</c> is the vector along the bottom edge and <c>y</c> is the
        /// vector along the left edge. Those are both computed in cube-space
        /// here; the caller projects the resulting positions onto the
        /// ellipsoid afterwards.
        /// </para>
        /// <para>
        /// We sweep row-by-row from bottom to top. Each iteration <c>j</c>
        /// handles the horizontal strip between row <c>j-1</c> (its
        /// <c>bottomIndices</c>) and row <c>j</c> (its <c>topIndices</c>) and
        /// emits <c>2·N</c> triangles for that strip. The row generated as
        /// "top" on iteration <c>j</c> becomes the "bottom" on iteration
        /// <c>j+1</c> — that is why <c>topIndicesBuffer</c> is copied into
        /// <c>bottomIndicesBuffer</c> each step (a ping-pong would avoid the
        /// copy, as the original comment noted).
        /// </para>
        /// <para>
        /// Two edge cases avoid duplicating vertices that already exist:
        /// <list type="bullet">
        /// <item><description>
        /// <c>j == 1</c>: the bottom of the first strip is the face's actual
        /// bottom edge, so <c>bottomIndices</c> is the caller-supplied
        /// <c>bottomLeftToRight</c> array — no copy needed.
        /// </description></item>
        /// <item><description>
        /// <c>j == numberOfPartitions</c>: the top of the last strip is the
        /// face's actual top edge, so <c>topIndices</c> is the caller-supplied
        /// <c>topLeftToRight</c> array — we skip interior-point generation on
        /// that iteration entirely.
        /// </description></item>
        /// </list>
        /// For every interior row (<c>1 ≤ j &lt; N</c>) the endpoints come
        /// from the left/right edge arrays at height <c>j</c>, and only the
        /// <c>N-1</c> genuinely-interior points are appended to
        /// <c>positions</c>.
        /// </para>
        /// <para>
        /// Each cell of the strip is split into two triangles sharing the
        /// diagonal from <c>bottom[i]</c> to <c>top[i+1]</c>:
        /// <code>
        ///   top[i] ────── top[i+1]
        ///      │         ╱  │
        ///      │       ╱    │
        ///      │     ╱      │
        ///      │   ╱        │
        ///   bottom[i] ── bottom[i+1]
        /// </code>
        /// Both triangles are wound counter-clockwise when viewed from outside
        /// the cube (matching <c>WindingOrder.CounterClockwise</c> set on the
        /// mesh), so back-face culling works correctly after projection.
        /// </para>
        /// </remarks>
        private static void AddFaceTriangles(
            int[] leftBottomToTop,
            int[] bottomLeftToRight,
            int[] rightBottomToTop,
            int[] topLeftToRight,
            CubeMapMesh cubeMapMesh)
        {
            IList<Vector3D> positions = cubeMapMesh.Positions;
            IndicesUnsignedInt indices = cubeMapMesh.Indices;
            int numberOfPartitions = cubeMapMesh.NumberOfPartitions;

            // Parametric frame of the face: origin is the bottom-left corner,
            // x spans the bottom edge, y spans the left edge. Any interior
            // point is origin + (i/N)*x + (j/N)*y.
            Vector3D origin = positions[bottomLeftToRight[0]];
            Vector3D x = positions[bottomLeftToRight[bottomLeftToRight.Length - 1]] - origin;
            Vector3D y = positions[topLeftToRight[0]] - origin;

            // Two row-long scratch buffers. We only ever keep two rows of
            // indices live at a time (the current strip's bottom and top).
            int[] bottomIndicesBuffer = new int[numberOfPartitions + 1];
            int[] topIndicesBuffer = new int[numberOfPartitions + 1];

            // For the first strip, the bottom row IS the face's bottom edge —
            // no copying needed. topIndices gets filled in below.
            int[] bottomIndices = bottomLeftToRight;
            int[] topIndices = topIndicesBuffer;

            // Sweep strips from bottom to top. Iteration j builds the triangles
            // between row (j-1) and row j of the lattice.
            for (int j = 1; j <= numberOfPartitions; ++j)
            {
                if (j != numberOfPartitions)
                {
                    // Interior row: we have to generate its vertices.
                    if (j != 1)
                    {
                        // The row we generated as "top" last iteration becomes
                        // this iteration's "bottom". Copy the buffer so the
                        // next "top" fill-in doesn't clobber it.
                        topIndicesBuffer.CopyTo(bottomIndicesBuffer, 0);
                        bottomIndices = bottomIndicesBuffer;
                    }

                    // Row endpoints come from the left and right edges — they
                    // already exist in positions, so we just record their indices.
                    topIndicesBuffer[0] = leftBottomToTop[j];
                    topIndicesBuffer[numberOfPartitions] = rightBottomToTop[j];

                    // Interior points of this row: append them to positions
                    // using the parametric formula, and remember their indices.
                    double deltaY = j / (double)numberOfPartitions;
                    Vector3D offsetY = deltaY * y;

                    for (int i = 1; i < numberOfPartitions; ++i)
                    {
                        double deltaX = i / (double)numberOfPartitions;
                        Vector3D offsetX = deltaX * x;

                        topIndicesBuffer[i] = cubeMapMesh.Positions.Count;
                        positions.Add(origin + offsetX + offsetY);
                    }
                }
                else
                {
                    // Final strip: its top is the face's actual top edge, which
                    // already exists — no vertex generation, just wire up the
                    // index arrays for the triangle emission below.
                    if (j != 1)
                    {
                        bottomIndices = topIndicesBuffer;
                    }
                    topIndices = topLeftToRight;
                }

                // Emit 2 triangles per cell across this strip. The diagonal
                // runs from bottom[i] to top[i+1].
                for (int i = 0; i < numberOfPartitions; ++i)
                {
                    indices.AddTriangle(new TriangleIndicesUnsignedInt(
                        bottomIndices[i], bottomIndices[i + 1], topIndices[i + 1]));
                    indices.AddTriangle(new TriangleIndicesUnsignedInt(
                        bottomIndices[i], topIndices[i + 1], topIndices[i]));
                }
            }
        }

        /// <summary>
        /// Projects each cube-space position onto the ellipsoid surface, then
        /// (optionally) emits per-vertex normals and texture coordinates.
        /// </summary>
        /// <remarks>
        /// Projection is gnomonic: normalize the cube-space ray, then scale
        /// component-wise by the ellipsoid radii. For a unit-normalized point
        /// <c>(x, y, z)</c> the result <c>(a·x, b·y, c·z)</c> satisfies the
        /// ellipsoid equation <c>(X/a)² + (Y/b)² + (Z/c)² = 1</c>, so every
        /// output vertex lies on the surface. Normals are the geodetic surface
        /// normal at each projected point; texture coordinates are the
        /// equirectangular mapping of that normal.
        /// </remarks>
        private static void CubeToEllipsoid(CubeMapMesh cubeMapMesh)
        {
            IList<Vector3D> positions = cubeMapMesh.Positions;

            for (int i = 0; i < positions.Count; i++)
            {
                positions[i] = positions[i].Normalize().MultiplyComponents(cubeMapMesh.Ellipsoid.Radii);

                if (cubeMapMesh.Normals == null && cubeMapMesh.TextureCoordinates == null)
                    continue;

                Vector3D geodeticSurfaceNormal = cubeMapMesh.Ellipsoid.GeodeticSurfaceNormal(positions[i]);

                cubeMapMesh.Normals?.Add(new Vector3H(geodeticSurfaceNormal));
                cubeMapMesh.TextureCoordinates?.Add(
                    new Vector2H(SubdivisionUtility.ComputeTextureCoordinate(geodeticSurfaceNormal)));
            }
        }

        private static int NumberOfTriangles(int numPartitions)
        {
            return 6 + 2 * numPartitions * numPartitions;
        }

        private static int NumberOfVertices(int numPartitions)
        {
            int numPartitionsMinusOne = numPartitions - 1;
            int numberOfVertices = 8; // Corners of initial cube
            numberOfVertices += 12 * numPartitionsMinusOne; // Vertices added along edges per partition
            numberOfVertices += 6 * numPartitionsMinusOne * numPartitionsMinusOne; // Vertices added on faces per partition
            return numberOfVertices;
        }

        private static int[] ReversedArray(int[] array)
        {
            int[] reversed = new int[array.Length];
            int j = 0;
            for(int i = array.Length - 1; i >= 0; i--)
            {
                reversed[j++] = array[i];
            }
            return reversed;
        }
    }
}
