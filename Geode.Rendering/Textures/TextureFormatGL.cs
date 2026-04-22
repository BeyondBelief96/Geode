using Silk.NET.OpenGL;
using System;

namespace Geode.Rendering.Textures
{
    /// <summary>
    /// Conversions from Geode texture/pixel enums to Silk.NET GL enums.
    /// Kept in one place so the mapping tables are easy to audit against the
    /// GL spec. Book §3.6.
    /// </summary>
    internal static class TextureFormatGL
    {
        public static SizedInternalFormat ToSizedInternalFormat(TextureFormat format) => format switch
        {
            TextureFormat.RedGreenBlue8            => SizedInternalFormat.Rgb8,
            TextureFormat.RedGreenBlue16           => SizedInternalFormat.Rgb16,
            TextureFormat.RedGreenBlueAlpha8       => SizedInternalFormat.Rgba8,
            TextureFormat.RedGreenBlue10A2         => SizedInternalFormat.Rgb10A2,
            TextureFormat.RedGreenBlueAlpha16      => SizedInternalFormat.Rgba16,
            TextureFormat.Red8                     => SizedInternalFormat.R8,
            TextureFormat.Red16                    => SizedInternalFormat.R16,
            TextureFormat.RedGreen8                => SizedInternalFormat.RG8,
            TextureFormat.RedGreen16               => SizedInternalFormat.RG16,
            TextureFormat.Red16f                   => SizedInternalFormat.R16f,
            TextureFormat.Red32f                   => SizedInternalFormat.R32f,
            TextureFormat.RedGreen16f              => SizedInternalFormat.RG16f,
            TextureFormat.RedGreen32f              => SizedInternalFormat.RG32f,
            TextureFormat.Red8i                    => SizedInternalFormat.R8i,
            TextureFormat.Red8ui                   => SizedInternalFormat.R8ui,
            TextureFormat.Red16i                   => SizedInternalFormat.R16i,
            TextureFormat.Red16ui                  => SizedInternalFormat.R16ui,
            TextureFormat.Red32i                   => SizedInternalFormat.R32i,
            TextureFormat.Red32ui                  => SizedInternalFormat.R32ui,
            TextureFormat.RedGreen8i               => SizedInternalFormat.RG8i,
            TextureFormat.RedGreen8ui              => SizedInternalFormat.RG8ui,
            TextureFormat.RedGreen16i              => SizedInternalFormat.RG16i,
            TextureFormat.RedGreen16ui             => SizedInternalFormat.RG16ui,
            TextureFormat.RedGreen32i              => SizedInternalFormat.RG32i,
            TextureFormat.RedGreen32ui             => SizedInternalFormat.RG32ui,
            TextureFormat.RedGreenBlueAlpha32f     => SizedInternalFormat.Rgba32f,
            TextureFormat.RedGreenBlue32f          => (SizedInternalFormat)GLEnum.Rgb32f,
            TextureFormat.RedGreenBlueAlpha16f     => SizedInternalFormat.Rgba16f,
            TextureFormat.RedGreenBlue16f          => (SizedInternalFormat)GLEnum.Rgb16f,
            TextureFormat.Red11fGreen11fBlue10f    => (SizedInternalFormat)GLEnum.R11fG11fB10f,
            TextureFormat.RedGreenBlue9E5          => (SizedInternalFormat)GLEnum.Rgb9E5,
            TextureFormat.SRedGreenBlue8           => (SizedInternalFormat)GLEnum.Srgb8,
            TextureFormat.SRedGreenBlue8Alpha8     => SizedInternalFormat.Srgb8Alpha8,
            TextureFormat.Depth16                  => (SizedInternalFormat)GLEnum.DepthComponent16,
            TextureFormat.Depth24                  => (SizedInternalFormat)GLEnum.DepthComponent24,
            TextureFormat.Depth32f                 => (SizedInternalFormat)GLEnum.DepthComponent32f,
            TextureFormat.Depth24Stencil8          => (SizedInternalFormat)GLEnum.Depth24Stencil8,
            TextureFormat.Depth32fStencil8         => (SizedInternalFormat)GLEnum.Depth32fStencil8,
            TextureFormat.RedGreenBlueAlpha32ui    => SizedInternalFormat.Rgba32ui,
            TextureFormat.RedGreenBlue32ui         => (SizedInternalFormat)GLEnum.Rgb32ui,
            TextureFormat.RedGreenBlueAlpha16ui    => SizedInternalFormat.Rgba16ui,
            TextureFormat.RedGreenBlue16ui         => (SizedInternalFormat)GLEnum.Rgb16ui,
            TextureFormat.RedGreenBlueAlpha8ui     => SizedInternalFormat.Rgba8ui,
            TextureFormat.RedGreenBlue8ui          => (SizedInternalFormat)GLEnum.Rgb8ui,
            TextureFormat.RedGreenBlueAlpha32i     => SizedInternalFormat.Rgba32i,
            TextureFormat.RedGreenBlue32i          => (SizedInternalFormat)GLEnum.Rgb32i,
            TextureFormat.RedGreenBlueAlpha16i     => SizedInternalFormat.Rgba16i,
            TextureFormat.RedGreenBlue16i          => (SizedInternalFormat)GLEnum.Rgb16i,
            TextureFormat.RedGreenBlueAlpha8i      => SizedInternalFormat.Rgba8i,
            TextureFormat.RedGreenBlue8i           => (SizedInternalFormat)GLEnum.Rgb8i,
            _ => throw new NotSupportedException($"TextureFormat {format} has no SizedInternalFormat mapping.")
        };

        public static PixelFormat ToPixelFormat(ImageFormat format) => format switch
        {
            ImageFormat.StencilIndex                => PixelFormat.StencilIndex,
            ImageFormat.DepthComponent              => PixelFormat.DepthComponent,
            ImageFormat.Red                         => PixelFormat.Red,
            ImageFormat.Green                       => PixelFormat.Green,
            ImageFormat.Blue                        => PixelFormat.Blue,
            ImageFormat.RedGreenBlue                => PixelFormat.Rgb,
            ImageFormat.RedGreenBlueAlpha           => PixelFormat.Rgba,
            ImageFormat.BlueGreenRed                => PixelFormat.Bgr,
            ImageFormat.BlueGreenRedAlpha           => PixelFormat.Bgra,
            ImageFormat.RedGreen                    => PixelFormat.RG,
            ImageFormat.DepthStencil                => PixelFormat.DepthStencil,
            ImageFormat.RedInteger                  => PixelFormat.RedInteger,
            ImageFormat.GreenInteger                => PixelFormat.GreenInteger,
            ImageFormat.BlueInteger                 => PixelFormat.BlueInteger,
            ImageFormat.RedGreenBlueInteger         => PixelFormat.RgbInteger,
            ImageFormat.RedGreenBlueAlphaInteger    => PixelFormat.RgbaInteger,
            ImageFormat.BlueGreenRedInteger         => PixelFormat.BgrInteger,
            ImageFormat.BlueGreenRedAlphaInteger    => PixelFormat.BgraInteger,
            ImageFormat.RedGreenInteger             => PixelFormat.RGInteger,
            _ => throw new NotSupportedException($"ImageFormat {format} has no PixelFormat mapping.")
        };

        public static PixelType ToPixelType(ImageDatatype datatype) => datatype switch
        {
            ImageDatatype.Byte              => PixelType.Byte,
            ImageDatatype.UnsignedByte      => PixelType.UnsignedByte,
            ImageDatatype.Short             => PixelType.Short,
            ImageDatatype.UnsignedShort     => PixelType.UnsignedShort,
            ImageDatatype.Int               => PixelType.Int,
            ImageDatatype.UnsignedInt       => PixelType.UnsignedInt,
            ImageDatatype.HalfFloat         => PixelType.HalfFloat,
            ImageDatatype.Float             => PixelType.Float,
            ImageDatatype.UnsignedInt248    => PixelType.UnsignedInt248,
            ImageDatatype.UnsignedShort565  => PixelType.UnsignedShort565,
            ImageDatatype.UnsignedShort4444 => PixelType.UnsignedShort4444,
            ImageDatatype.UnsignedShort5551 => PixelType.UnsignedShort5551,
            ImageDatatype.UnsignedInt1010102=> PixelType.UnsignedInt1010102,
            ImageDatatype.UnsignedInt8888   => PixelType.UnsignedInt8888,
            _ => throw new NotSupportedException($"ImageDatatype {datatype} has no PixelType mapping.")
        };
    }
}
