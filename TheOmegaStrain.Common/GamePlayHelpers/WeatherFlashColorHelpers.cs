using System;

namespace TheOmegaStrain.Common.GamePlayHelpers
{
    /// <summary>
    /// Shared background-flash colour math for lightning and explosion impact flashes.
    /// Both the WPF renderer (background brush) and the Direct3D 11 renderer (clear colour)
    /// use this, so the sky flashes identically regardless of the active backend.
    /// </summary>
    public static class WeatherFlashColorHelpers
    {
        public static float CalculateImpactWarmth(float lightning, float impact)
        {
            float total = lightning + impact;
            return total <= 0.001f ? 0f : Math.Clamp(impact / total, 0f, 1f);
        }

        /// <summary>
        /// Returns the background colour bytes for the given flash intensities.
        /// Returns black when no flash is active.
        /// </summary>
        public static (byte Red, byte Green, byte Blue) GetBackgroundColor(float lightning, float impact)
        {
            float intensity = Math.Max(lightning, impact);
            if (intensity <= 0.005f)
                return (0, 0, 0);

            // Quantized so callers can cache brushes per step without visible banding.
            int key = Math.Clamp((int)MathF.Round(intensity * 16f), 0, 16);
            int warmthKey = Math.Clamp((int)MathF.Round(CalculateImpactWarmth(lightning, impact) * 16f), 0, 16);

            float t = key / 16f;
            float warmth = warmthKey / 16f;

            byte lightningRed = (byte)(2 + 38 * t);
            byte lightningGreen = (byte)(4 + 56 * t);
            byte lightningBlue = (byte)(9 + 92 * t);

            byte impactRed = (byte)(12 + 120 * t);
            byte impactGreen = (byte)(4 + 54 * t);
            byte impactBlue = (byte)(2 + 20 * t);

            return (
                Mix(lightningRed, impactRed, warmth),
                Mix(lightningGreen, impactGreen, warmth),
                Mix(lightningBlue, impactBlue, warmth));
        }

        public static byte Mix(byte a, byte b, float t)
        {
            return ClampByte(a + (b - a) * Math.Clamp(t, 0f, 1f));
        }

        public static byte ClampByte(float value)
        {
            if (value <= 0f) return 0;
            if (value >= 255f) return 255;
            return (byte)value;
        }
    }
}
