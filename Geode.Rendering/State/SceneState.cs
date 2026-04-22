using System;
using System.Numerics;
// Alias to avoid a name clash with Silk.NET.Maths.Vector3D<T>.
using Vector3D = Geode.Core.Vector3D;
using Matrix3X3 = Silk.NET.Maths.Matrix3X3<float>;
using Geode.Rendering.State;

namespace Geode.Rendering
{
    /// <summary>
    /// Per-frame scene state consumed by the renderer. Holds the camera, sun
    /// position, material parameters, and derived quantities the automatic
    /// uniform subsystem pulls from (matrices, viewport, etc.).
    /// </summary>
    /// <remarks>
    /// Many of the properties here are <em>derived</em> from <see cref="Camera"/>
    /// and <see cref="ModelMatrix"/>. They are implemented as computed properties
    /// rather than cached values so that mutating the camera / model / sun
    /// reflects in the next draw without explicit invalidation bookkeeping.
    /// If profiling later shows matrix recomputation is hot, cache them behind
    /// a dirty flag -- but keep the property surface identical.
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
        /// View matrix cast to single precision. Computed from the double-precision
        /// <see cref="ComputeViewMatrix"/> each access.
        /// </summary>
        public Matrix4x4 ViewMatrix => DoubleArrayToMatrix4x4(ComputeViewMatrix());

        /// <summary>
        /// Perspective projection matrix cast to single precision. Computed from
        /// the double-precision <see cref="ComputePerspectiveMatrix"/> each access.
        /// </summary>
        public Matrix4x4 PerspectiveMatrix => DoubleArrayToMatrix4x4(ComputePerspectiveMatrix());

        /// <summary>
        /// Combined Model-View-Perspective matrix. Uses row-major
        /// <see cref="System.Numerics"/> multiplication order:
        /// <c>Model * View * Perspective</c>. Upload with <c>transpose = true</c>
        /// so GLSL sees the column-major equivalent.
        /// </summary>
        public Matrix4x4 ModelViewPerspectiveMatrix =>
            ModelMatrix * ViewMatrix * PerspectiveMatrix;

        /// <summary>
        /// Normal matrix -- the transpose of the inverse of the upper 3x3 of
        /// the model-view matrix. Transforms normals from model space to eye
        /// space without introducing scale distortion when the model-view
        /// includes non-uniform scale.
        /// </summary>
        public Matrix3X3 NormalMatrix
        {
            get
            {
                Matrix4x4 mv = ModelMatrix * ViewMatrix;
                Matrix4x4 upper3x3 = new Matrix4x4(
                    mv.M11, mv.M12, mv.M13, 0,
                    mv.M21, mv.M22, mv.M23, 0,
                    mv.M31, mv.M32, mv.M33, 0,
                    0,      0,      0,      1);
                if (!Matrix4x4.Invert(upper3x3, out Matrix4x4 inv))
                    return new Matrix3X3(
                        1, 0, 0,
                        0, 1, 0,
                        0, 0, 1);
                // Transpose of inverse of upper 3x3.
                return new Matrix3X3(
                    inv.M11, inv.M21, inv.M31,
                    inv.M12, inv.M22, inv.M32,
                    inv.M13, inv.M23, inv.M33);
            }
        }

        #endregion

        #region Double-precision matrix computation (existing)

        /// <summary>
        /// Computes the view matrix from the current camera, in double precision.
        /// Returns a 16-element array in column-major order.
        /// </summary>
        public double[] ComputeViewMatrix()
        {
            Vector3D eye = Camera.Eye;
            Vector3D target = Camera.Target;
            Vector3D up = Camera.Up;

            // Forward direction - From eye to target
            Vector3D forward = (target - eye).Normalize();

            // Right direction - Perpendicular to forward and up
            Vector3D right = forward.Cross(up).Normalize();

            // True up direction - Perpendicular to right and forward
            Vector3D trueUp = right.Cross(forward);

            // View matrix construction
            // View matrix in column-major order.
            // The matrix simultaneously rotates and translates:
            //   - Rotation: aligns world axes to camera axes
            //   - Translation: moves origin to camera position
            // The negation of forward is because OpenGL eye space looks down -Z.
            return new double[16]
            {
                right.X,     trueUp.X,    -forward.X,   0,
                right.Y,     trueUp.Y,    -forward.Y,   0,
                right.Z,     trueUp.Z,    -forward.Z,   0,
                -right.Dot(eye), -trueUp.Dot(eye), forward.Dot(eye), 1
            };
        }

        /// <summary>
        /// Computes the perspective projection matrix in double precision.
        /// Uses [0, 1] depth range (for glClipControl(GL_LOWER_LEFT, GL_ZERO_TO_ONE)).
        /// Returns a 16-element array in column-major order.
        /// </summary>
        public double[] ComputePerspectiveMatrix()
        {
            double fovY = Camera.FieldOfViewY;
            double aspect = Camera.AspectRatio;
            double near = Camera.NearPlane;
            double far = Camera.FarPlane;

            double tanHalfFov = Math.Tan(fovY / 2.0);

            // Perspective matrix for [0, 1] depth range.
            // This differs from the standard [-1, 1] matrix in row 2:
            //   Standard:  -(f+n)/(f-n)  and  -2fn/(f-n)
            //   [0,1]:     -f/(f-n)      and  -fn/(f-n)
            return new double[16]
            {
                1.0 / (aspect * tanHalfFov), 0,                  0,                    0,
                0,                           1.0 / tanHalfFov,   0,                    0,
                0,                           0,                  -far / (far - near),  -1,
                0,                           0,                  -(far * near) / (far - near), 0
            };
        }

        /// <summary>
        /// Converts a column-major 16-element double array (produced by
        /// <see cref="ComputeViewMatrix"/> / <see cref="ComputePerspectiveMatrix"/>)
        /// to a row-major <see cref="Matrix4x4"/>. The transpose is implicit --
        /// the storage layout is row-major after this conversion, which is what
        /// <see cref="System.Numerics"/> expects and what the
        /// <c>transpose = true</c> upload in the uniform classes assumes.
        /// </summary>
        private static Matrix4x4 DoubleArrayToMatrix4x4(double[] m)
        {
            return new Matrix4x4(
                (float)m[0], (float)m[1], (float)m[2], (float)m[3],
                (float)m[4], (float)m[5], (float)m[6], (float)m[7],
                (float)m[8], (float)m[9], (float)m[10], (float)m[11],
                (float)m[12], (float)m[13], (float)m[14], (float)m[15]);
        }

        #endregion
    }
}
