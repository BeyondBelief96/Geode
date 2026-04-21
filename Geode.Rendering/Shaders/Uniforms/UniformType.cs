// Enumeration of every GLSL uniform type we handle.
// The values are the GL token numbers so casts to/from Silk.NET's
// UniformType are identity -- but we keep a Geode-side type so the
// uniform classes don't reference Silk.NET.OpenGL enums directly.

namespace Geode.Rendering.Uniforms
{
    public enum UniformType : uint
    {
        // Scalars
        Float = 0x1406,  // GL_FLOAT
        Int = 0x1404,  // GL_INT
        UnsignedInt = 0x1405,  // GL_UNSIGNED_INT
        Bool = 0x8B56,  // GL_BOOL

        // Float vectors
        FloatVector2 = 0x8B50,  // GL_FLOAT_VEC2
        FloatVector3 = 0x8B51,  // GL_FLOAT_VEC3
        FloatVector4 = 0x8B52,  // GL_FLOAT_VEC4

        // Int vectors
        IntVector2 = 0x8B53,
        IntVector3 = 0x8B54,
        IntVector4 = 0x8B55,

        // Bool vectors
        BoolVector2 = 0x8B57,
        BoolVector3 = 0x8B58,
        BoolVector4 = 0x8B59,

        // Square matrices
        FloatMatrix22 = 0x8B5A,
        FloatMatrix33 = 0x8B5B,
        FloatMatrix44 = 0x8B5C,

        // Non-square matrices (rarely used but part of GLSL)
        FloatMatrix23 = 0x8B65,
        FloatMatrix24 = 0x8B66,
        FloatMatrix32 = 0x8B67,
        FloatMatrix34 = 0x8B6C,
        FloatMatrix42 = 0x8B69,
        FloatMatrix43 = 0x8B6B,

        // Samplers (all treated as Int for upload; the value is the texture unit)
        Sampler1D = 0x8B5D,
        Sampler2D = 0x8B5E,
        Sampler3D = 0x8B5F,
        SamplerCube = 0x8B60,
        Sampler2DArray = 0x8DC1,
        Sampler2DShadow = 0x8B62,
    }
}
