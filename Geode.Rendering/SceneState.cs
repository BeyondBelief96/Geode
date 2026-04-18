using Geode.Core;

namespace Geode.Rendering
{
    public class SceneState
    {
        #region Properties

        public CameraState Camera { get; set; } = new();

        public Vector3D SunPosition { get; set; } = new(0, 100_000_000, 0);

        public float DiffuseIntensity { get; set; } = 0.8f;

        public float SpecularIntensity { get; set; } = 0.5f;

        public float AmbientIntensity { get; set; } = 0.1f;

        public float Shininess { get; set; } = 32.0f;

        #endregion

        #region Computed Matrices

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

        #endregion
    }
}
