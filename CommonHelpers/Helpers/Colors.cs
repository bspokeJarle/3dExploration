using System;
using System.Runtime.CompilerServices;

namespace _3dTesting.Helpers
{
    public class Colors
    {
        private static readonly char[] HexChars = "0123456789ABCDEF".ToCharArray();

        public static System.Windows.Media.Color getShadeOfColorFromNormal(float normal, string color)
        {
            ReadOnlySpan<char> span = string.IsNullOrEmpty(color)
                ? "000000".AsSpan()
                : color.AsSpan();

            if (span.Length > 0 && span[0] == '#')
                span = span[1..];

            if (span.Length < 6)
                return System.Windows.Media.Colors.Black;

            byte r = (byte)Math.Clamp((int)(ParseHexByte(span[0], span[1]) * normal), 0, 255);
            byte g = (byte)Math.Clamp((int)(ParseHexByte(span[2], span[3]) * normal), 0, 255);
            byte b = (byte)Math.Clamp((int)(ParseHexByte(span[4], span[5]) * normal), 0, 255);

            return System.Windows.Media.Color.FromArgb(255, r, g, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ParseHexByte(char hi, char lo)
            => (HexDigit(hi) << 4) | HexDigit(lo);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int HexDigit(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            return 0;
        }
    }
}
