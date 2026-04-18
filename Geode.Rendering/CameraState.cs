using Geode.Core;

namespace Geode.Rendering
{
    /// <summary>
    /// Defines the position, orientation, and projection parameters of a virtual camera.
    /// Together these describe both the view transform (eye/target/up) and the
    /// perspective projection (field of view, aspect ratio, near/far planes).
    /// </summary>
    public class CameraState
    {
        /// <summary>
        /// Camera position in world-space (ECEF) coordinates.
        /// Default: (0, 0, 10).
        /// </summary>
        public Vector3D Eye { get; set; } = new(0, 0, 10);

        /// <summary>
        /// The point the camera is looking at, in world-space (ECEF) coordinates.
        /// The view direction is implicitly <c>Target - Eye</c>.
        /// Default: the origin (0, 0, 0).
        /// </summary>
        public Vector3D Target { get; set; } = new(0, 0, 0);

        /// <summary>
        /// The up direction used to orient the camera's view basis.
        /// Should be linearly independent from the view direction; the renderer
        /// re-orthogonalizes the basis when constructing the view matrix.
        /// Default: world +Y (0, 1, 0).
        /// </summary>
        /// <remarks>
        /// For an ECEF camera, this is typically the local geodetic "up" at the
        /// camera's position rather than world +Y.
        /// </remarks>
        public Vector3D Up { get; set; } = new(0, 1, 0);

        /// <summary>
        /// Vertical field of view, in radians, used by the perspective projection.
        /// Default: 60 degrees.
        /// </summary>
        public double FieldOfViewY { get; set; } = Trigonometry.ToRadians(60.0);

        /// <summary>
        /// Viewport aspect ratio (width / height) used by the perspective projection.
        /// Default: 16:9.
        /// </summary>
        public double AspectRatio { get; set; } = 16.0 / 9.0;

        /// <summary>
        /// Distance from the eye to the near clipping plane, in world units.
        /// Fragments closer than this are clipped. Must be greater than zero and
        /// less than <see cref="FarPlane"/>. Default: 0.1.
        /// </summary>
        public double NearPlane { get; set; } = 0.1;

        /// <summary>
        /// Distance from the eye to the far clipping plane, in world units.
        /// Fragments farther than this are clipped. Must be greater than
        /// <see cref="NearPlane"/>. Default: 1000.0.
        /// </summary>
        public double FarPlane { get; set; } = 1000.0;
    }
}
