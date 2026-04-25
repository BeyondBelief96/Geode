using System;
using System.Numerics;
using Geode.Core;
// Alias to avoid a name clash with Silk.NET.Maths.Vector3D<T>.
using Vector3D = Geode.Core.Vector3D;
using Geode.Rendering.State;

namespace Geode.Rendering
{
    /// <summary>
    /// Per-frame scene state consumed by the renderer. Holds the camera, sun
    /// position, material parameters, and derived quantities the automatic
    /// uniform subsystem pulls from (matrices, viewport, etc.).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Many of the properties here are <em>derived</em> from <see cref="Camera"/>
    /// and <see cref="ModelMatrix"/>. They are implemented as computed properties
    /// rather than cached values so that mutating the camera / model / sun
    /// reflects in the next draw without explicit invalidation bookkeeping.
    /// If profiling later shows matrix recomputation is hot, cache them behind
    /// a dirty flag -- but keep the property surface identical.
    /// </para>
    /// <para>
    /// All matrix-typed properties (<see cref="ViewMatrix"/>,
    /// <see cref="PerspectiveMatrix"/>, <see cref="ModelViewPerspectiveMatrix"/>,
    /// <see cref="NormalMatrix"/>) return Geode's column-major / column-vector
    /// matrix types (<see cref="Matrix4F"/>, <see cref="Matrix3F"/>). These
    /// upload directly to OpenGL with <c>transpose = GL_FALSE</c>. The user-
    /// settable <see cref="ModelMatrix"/> is still <see cref="Matrix4x4"/>
    /// (row-vector) for ergonomics — call sites can keep using
    /// <c>Matrix4x4.CreateRotationY</c> etc. The conversion to column-vector
    /// happens once per frame inside <see cref="ModelMatrixF"/>.
    /// </para>
    /// </remarks>
    public class SceneState
    {
        #region Primary state

        /// <summary>The camera (position, orientation, projection parameters).</summary>
        public CameraState Camera { get; set; } = new();

        /// <summary>
        /// Sun position in world (ECEF) coordinates. Used for directional lighting
        /// on the globe surface. Default: far above the equator on +Y.
        /// </summary>
        public Vector3D SunPosition { get; set; } = new(0, 100_000_000, 0);

        /// <summary>
        /// Model matrix for the object being drawn. Applied first in the
        /// transform chain. Default: identity (object is in world space).
        /// </summary>
        /// <remarks>
        /// Stored as <see cref="Matrix4x4"/> (row-vector convention) so that
        /// callers can compose with <c>Matrix4x4.CreateRotationY</c> etc.
        /// Internally the renderer uses <see cref="ModelMatrixF"/>, the
        /// column-vector form, in matrix products with <see cref="ViewMatrix"/>
        /// and <see cref="PerspectiveMatrix"/>.
        /// </remarks>
        public Matrix4x4 ModelMatrix { get; set; } = Matrix4x4.Identity;

        /// <summary>
        /// World-space position of a light attached to the camera ("headlamp").
        /// Default: world origin. For most scenes this is set to <see cref="CameraEyeFloat"/>.
        /// </summary>
        public Vector3 CameraLightPosition { get; set; } = Vector3.Zero;

        /// <summary>
        /// Current viewport in window coordinates as <c>(x, y, width, height)</c>.
        /// Must be kept in sync with the OpenGL viewport state (see
        /// <c>glViewport</c> in the application's resize handler).
        /// Default: (0, 0, 800, 600).
        /// </summary>
        public Vector4 Viewport { get; set; } = new(0, 0, 800, 600);

        /// <summary>
        /// Screen-space size of one world unit at unit distance from the camera.
        /// Used by LOD selection shaders. Default: 1.
        /// </summary>
        public float PixelSizePerDistance { get; set; } = 1.0f;

        /// <summary>
        /// Sub-pixel snap factor for techniques that require pixel-aligned
        /// geometry (e.g. high-quality billboard text). Default: 1.
        /// </summary>
        public float HighResolutionSnapScale { get; set; } = 1.0f;

        #endregion

        #region Material / lighting scalars

        /// <summary>Diffuse lighting coefficient. Default: 0.8.</summary>
        public float DiffuseIntensity { get; set; } = 0.8f;

        /// <summary>Specular lighting coefficient. Default: 0.5.</summary>
        public float SpecularIntensity { get; set; } = 0.5f;

        /// <summary>Ambient lighting coefficient. Default: 0.1.</summary>
        public float AmbientIntensity { get; set; } = 0.1f;

        /// <summary>Phong shininess exponent. Default: 32.</summary>
        public float Shininess { get; set; } = 32.0f;

        #endregion

        #region Derived vectors (for automatic uniforms)

        /// <summary>
        /// Camera eye position cast to single precision. Suitable for
        /// small-scene (non-planetary) rendering; for planetary scale,
        /// use the RTE/DSFP variants (Section 27).
        /// </summary>
        public Vector3 CameraEyeFloat =>
            new Vector3((float)Camera.Eye.X, (float)Camera.Eye.Y, (float)Camera.Eye.Z);

        /// <summary>Sun position cast to single precision.</summary>
        public Vector3 SunPositionFloat =>
            new Vector3((float)SunPosition.X, (float)SunPosition.Y, (float)SunPosition.Z);

        /// <summary>
        /// Material parameters packed as <c>(diffuse, specular, ambient, shininess)</c>.
        /// Book convention: one vec4 uniform instead of four scalars.
        /// </summary>
        public Vector4 DiffuseSpecularAmbientShininess =>
            new Vector4(DiffuseIntensity, SpecularIntensity, AmbientIntensity, Shininess);

        /// <summary>
        /// Reciprocal viewport -- <c>(1/x, 1/y, 1/width, 1/height)</c> -- so shaders
        /// can avoid division. Zero components are preserved as zero.
        /// </summary>
        public Vector4 InverseViewport => new Vector4(
            Viewport.X == 0 ? 0 : 1f / Viewport.X,
            Viewport.Y == 0 ? 0 : 1f / Viewport.Y,
            Viewport.Z == 0 ? 0 : 1f / Viewport.Z,
            Viewport.W == 0 ? 0 : 1f / Viewport.W);

        #endregion

        #region Derived matrices

        /// <summary>
        /// <see cref="ModelMatrix"/> converted to column-vector convention
        /// (transposed). This is the form used in matrix products with
        /// <see cref="ViewMatrix"/> and <see cref="PerspectiveMatrix"/>, and
        /// the form uploaded to <c>geode_modelMatrix</c>.
        /// </summary>
        public Matrix4F ModelMatrixF => Matrix4F.FromSystemNumerics(ModelMatrix);

        /// <summary>
        /// View matrix in column-vector convention. Computed from the camera
        /// each access. Maps world space → eye space.
        /// </summary>
        public Matrix4F ViewMatrix => ComputeViewMatrix();

        /// <summary>
        /// Perspective projection matrix in column-vector convention. Uses
        /// <c>[0, 1]</c> depth range (for
        /// <c>glClipControl(GL_LOWER_LEFT, GL_ZERO_TO_ONE)</c>).
        /// </summary>
        public Matrix4F PerspectiveMatrix => ComputePerspectiveMatrix();

        /// <summary>
        /// Combined model-view-perspective matrix in column-vector convention.
        /// Composition reads right-to-left: <c>P * V * M</c>, so applying
        /// to a column vector first transforms by Model, then View, then
        /// Perspective.
        /// </summary>
        public Matrix4F ModelViewPerspectiveMatrix =>
            PerspectiveMatrix * ViewMatrix * ModelMatrixF;

        /// <summary>
        /// Normal matrix -- the inverse-transpose of the upper 3×3 of the
        /// model-view matrix. Transforms normals from model space to eye
        /// space without introducing scale distortion when the model-view
        /// includes non-uniform scale.
        /// </summary>
        public Matrix3F NormalMatrix
        {
            get
            {
                Matrix4F mv = ViewMatrix * ModelMatrixF;
                return InverseTransposeUpper3x3(mv);
            }
        }

        #endregion

        #region Matrix construction (column-vector)

        /// <summary>
        /// Builds the column-vector view matrix from the current camera.
        /// </summary>
        /// <remarks>
        /// In column-vector form the view matrix is
        /// <code>
        ///   [  right.x      right.y      right.z     -right·eye  ]
        ///   [  trueUp.x     trueUp.y     trueUp.z    -trueUp·eye ]
        ///   [ -forward.x   -forward.y   -forward.z    forward·eye]
        ///   [  0            0            0            1          ]
        /// </code>
        /// The third row uses <c>-forward</c> because OpenGL's eye space looks
        /// down <c>-Z</c> (objects in front of the camera have negative eye-space z).
        /// Computation runs in double precision (<see cref="Vector3D"/>) and
        /// downcasts to <see cref="float"/> when populating the matrix; this
        /// keeps geodetic camera math accurate at planetary scale.
        /// </remarks>
        public Matrix4F ComputeViewMatrix()
        {
            Vector3D eye = Camera.Eye;
            Vector3D target = Camera.Target;
            Vector3D up = Camera.Up;

            Vector3D forward = (target - eye).Normalize();
            Vector3D right = forward.Cross(up).Normalize();
            Vector3D trueUp = right.Cross(forward);

            return new Matrix4F(
                (float)right.X,     (float)right.Y,     (float)right.Z,     (float)(-right.Dot(eye)),
                (float)trueUp.X,    (float)trueUp.Y,    (float)trueUp.Z,    (float)(-trueUp.Dot(eye)),
                (float)(-forward.X),(float)(-forward.Y),(float)(-forward.Z),(float)forward.Dot(eye),
                0,                  0,                  0,                  1);
        }

        /// <summary>
        /// Builds the column-vector perspective projection matrix from the
        /// current camera. <c>[0, 1]</c> depth range; matches what
        /// <c>glClipControl(GL_LOWER_LEFT, GL_ZERO_TO_ONE)</c> expects.
        /// </summary>
        public Matrix4F ComputePerspectiveMatrix()
        {
            double fovY = Camera.FieldOfViewY;
            double aspect = Camera.AspectRatio;
            double near = Camera.NearPlane;
            double far = Camera.FarPlane;

            double tanHalfFov = Math.Tan(fovY / 2.0);

            // Perspective matrix for [0, 1] depth range. Differs from the
            // standard [-1, 1] form in the row-2 entries:
            //   Standard: -(f+n)/(f-n)  and  -2fn/(f-n)
            //   [0, 1]:   -f/(f-n)      and  -fn/(f-n)
            return new Matrix4F(
                (float)(1.0 / (aspect * tanHalfFov)), 0,                          0,                              0,
                0,                                    (float)(1.0 / tanHalfFov),  0,                              0,
                0,                                    0,                          (float)(-far / (far - near)),   (float)(-(far * near) / (far - near)),
                0,                                    0,                          -1,                             0);
        }

        /// <summary>
        /// Computes the inverse-transpose of the upper 3×3 of a column-vector
        /// 4×4 matrix. Used by <see cref="NormalMatrix"/>.
        /// </summary>
        /// <remarks>
        /// Operates in float; for planetary-scale model-view matrices this is
        /// fine because the normal matrix only depends on rotation/scale, not
        /// translation. Returns identity if the matrix is singular.
        /// </remarks>
        private static Matrix3F InverseTransposeUpper3x3(Matrix4F m)
        {
            float a = m.Col0Row0, b = m.Col1Row0, c = m.Col2Row0;
            float d = m.Col0Row1, e = m.Col1Row1, f = m.Col2Row1;
            float g = m.Col0Row2, h = m.Col1Row2, i = m.Col2Row2;

            // Cofactor matrix entries (also the transpose of the adjugate).
            float A =  (e * i - f * h);
            float B = -(d * i - f * g);
            float C =  (d * h - e * g);
            float D = -(b * i - c * h);
            float E =  (a * i - c * g);
            float F = -(a * h - b * g);
            float G =  (b * f - c * e);
            float H = -(a * f - c * d);
            float I =  (a * e - b * d);

            float det = a * A + b * B + c * C;
            if (det == 0) return Matrix3F.Identity;
            float inv = 1f / det;

            // (1/det) * cofactor matrix = inverse-transpose. Filling row-by-row
            // in the constructor (Matrix3F constructor takes visual rows).
            return new Matrix3F(
                A * inv, B * inv, C * inv,
                D * inv, E * inv, F * inv,
                G * inv, H * inv, I * inv);
        }

        #endregion
    }
}
