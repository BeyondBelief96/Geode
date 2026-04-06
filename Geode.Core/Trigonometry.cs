using System;

namespace Geode.Core
{
    /// <summary>
    /// Degree/Radian conversion and common constants for trigonometric calculations.
    /// </summary>
    public static class Trigonometry
    {
        public const double TwoPi = 2 * Math.PI;
        public const double HalfPi = Math.PI / 2;

        public static double ToRadians(double degrees) => degrees * (Math.PI / 180);
        public static double ToDegrees(double radians) => radians * (180 / Math.PI);
    }
}
