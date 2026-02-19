using _3dTesting._Coordinates;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace _3dTesting.Rendering
{
    public class WorldRenderer
    {
        private readonly DrawingVisualHost visualHost;
        private readonly DrawingVisual visual = new DrawingVisual();
        private readonly bool _localLoggingEnabled =  false;

        private int renderingTriangleCount = 0;

        private readonly Dictionary<(float, string), Color> colorCache = new();
        private readonly Dictionary<Color, SolidColorBrush> brushCache = new();
        private readonly Dictionary<Color, Pen> penCache = new();

        private readonly List<StreamGeometry> geometryPool = new();
        private int geometryPoolIndex = 0;

        private const float FarZ = 2000f;
        private const float NearZ = -2000f;

        public int GetRenderingTriangleCount() => renderingTriangleCount;

        public WorldRenderer(DrawingVisualHost host)
        {
            visualHost = host;
  
            if (ShouldLog())
            {
                int tier = (RenderCapability.Tier >> 16);
                string tierText = tier switch
                {
                    2 => "Render Tier 2 - Full hardware acceleration",
                    1 => "Render Tier 1 - Partial hardware acceleration",
                    0 => "Render Tier 0 - Software rendering only",
                    _ => $"Unknown Render Tier ({tier})"
                };

                Logger.Log($"[WorldRenderer] WPF Render Tier: {tierText}");
                if (tier < 2)
                    Logger.Log("[WorldRenderer] ⚠️ Performance Warning: Not using full hardware acceleration!");
            }

            PrewarmColorCache();
        }

        private bool ShouldLog() => _localLoggingEnabled && Logger.EnableFileLogging;

        private void PrewarmColorCache()
        {
            string[] commonColors = ["red", "green", "blue", "gray", "white", "black", "yellow", "orange", "brown"];
            int generatedCount = 0;

            // Choose how dense you want the prewarm. 100 steps -> about 101 * 9 = 909 entries before rounding/cache hits.
            const int steps = 500;
            float stepSize = (FarZ - NearZ) / steps;

            for (int i = 0; i <= steps; i++)
            {
                float calculatedZ = NearZ + (i * stepSize);

                // Map to 0..1 using the new helper
                float factor01 = GetDepthFactor01(calculatedZ);

                // Quantize for stable cache keys (same concept as runtime shading key)
                float roundedFactor01 = (float)Math.Round(factor01, 2, MidpointRounding.AwayFromZero);

                foreach (var baseColor in commonColors)
                {
                    string normalized = baseColor.ToLowerInvariant();

                    var cacheKey = (roundedFactor01, normalized);
                    if (colorCache.ContainsKey(cacheKey))
                        continue;

                    try
                    {
                        // NOTE: We keep the call signature the same for now,
                        // since you said you're aligned with renaming later.
                        Color color = (Color)ColorConverter.ConvertFromString(
                            Helpers.Colors.getShadeOfColorFromNormal(roundedFactor01, normalized));

                        colorCache[cacheKey] = color;

                        if (!brushCache.ContainsKey(color))
                        {
                            SolidColorBrush brush = new SolidColorBrush(color);
                            brush.Freeze();
                            brushCache[color] = brush;
                        }

                        if (!penCache.ContainsKey(color))
                        {
                            Pen pen = new Pen(brushCache[color], 1);
                            pen.Freeze();
                            penCache[color] = pen;
                        }

                        generatedCount++;
                    }
                    catch (Exception ex)
                    {
                        if (ShouldLog())
                            Logger.Log($"[WorldRenderer] ⚠️ Failed to prewarm color (factor={roundedFactor01:0.00}, baseColor={baseColor}, calcZ={calculatedZ:0.00}): {ex.Message}");
                    }
                }
            }

            if (ShouldLog())
                Logger.Log($"[WorldRenderer] ✅ Prewarmed {generatedCount} colors + brushes + pens.");
        }


        public void RenderTriangles(List<_2dTriangleMesh> screenCoordinates)
        {
            renderingTriangleCount = screenCoordinates.Count;
            var triangleArray = screenCoordinates.ToArray();
            Array.Sort(triangleArray, (a, b) => a.CalculatedZ.CompareTo(b.CalculatedZ));

            geometryPoolIndex = 0;

            bool trackStats = ShouldLog();
            int colorHits = 0, colorMisses = 0;
            int brushHits = 0, brushMisses = 0;
            int penHits = 0, penMisses = 0;

            using (var dc = visual.RenderOpen())
            {
                dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, 1920, 1080));

                foreach (var triangle in triangleArray)
                {
                    if (triangle.CalculatedZ > 1200 || triangle.CalculatedZ < -2000)
                        continue;

                    float depthFactor01 = GetDepthFactor01(triangle.CalculatedZ);
                    float angleFactor01 = NormalizeAngleTo01(triangle.TriangleAngle);

                    // First shade by angle, then by depth => combined factor
                    float combinedFactor01 = Math.Clamp(angleFactor01 * depthFactor01, 0f, 1f);
                    float shadeKey = (float)Math.Round(combinedFactor01, 2, MidpointRounding.AwayFromZero);

                    // Normalize baseColor so "#FF8B00" and "ff8b00" become the same key
                    string baseColor = triangle.Color?.Trim().ToLowerInvariant() ?? "000000";

                    if (baseColor.StartsWith("#"))
                        baseColor = baseColor.Substring(1);

                    if (!colorCache.TryGetValue((shadeKey, baseColor), out Color color))
                    {
                        if (ShouldLog()) Logger.Log($"[WorldRenderer] ⚠️ Color cache miss for key ({shadeKey}, {baseColor}). CalculatedZ:{triangle.CalculatedZ} Angle:{triangle.TriangleAngle:0.00}");
                        string hex = Helpers.Colors.getShadeOfColorFromNormal(shadeKey, baseColor);
                        color = HexToColor(hex);
                        colorCache[(shadeKey, baseColor)] = color;
                        if (trackStats) colorMisses++;
                    }
                    else if (trackStats)
                    {
                        colorHits++;
                    }

                    if (!brushCache.TryGetValue(color, out SolidColorBrush brush))
                    {
                        brush = new SolidColorBrush(color);
                        brush.Freeze();
                        brushCache[color] = brush;
                        if (trackStats) brushMisses++;
                    }
                    else if (trackStats)
                    {
                        brushHits++;
                    }

                    if (!penCache.TryGetValue(color, out Pen pen))
                    {
                        pen = new Pen(brush, 1);
                        pen.Freeze();
                        penCache[color] = pen;
                        if (trackStats) penMisses++;
                    }
                    else if (trackStats)
                    {
                        penHits++;
                    }

                    DrawTriangle(dc, triangle, brush, pen);
                }
            }

            visualHost.AddVisual(visual);

            if (trackStats)
            {
                Logger.Log($"[WorldRenderer] Caching stats - Colors: {colorHits} hits / {colorMisses} misses, " +
                           $"Brushes: {brushHits} hits / {brushMisses} misses, Pens: {penHits} hits / {penMisses} misses");
            }
        }

        private void DrawTriangle(DrawingContext dc, _2dTriangleMesh triangle, SolidColorBrush brush, Pen pen)
        {
            var p1 = new Point(triangle.X1, triangle.Y1);
            var p2 = new Point(triangle.X2, triangle.Y2);
            var p3 = new Point(triangle.X3, triangle.Y3);

            if (geometryPoolIndex >= geometryPool.Count)
                geometryPool.Add(new StreamGeometry());

            var geometry = geometryPool[geometryPoolIndex++];
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(p1, true, true);
                ctx.LineTo(p2, true, false);
                ctx.LineTo(p3, true, false);
            }

            if (triangle.PartName != null && triangle.PartName.StartsWith("CrashBox-"))
            {
                // If rendering CrashBoxes -> use semi-transparent brush
                var transparentBrush = new SolidColorBrush(brush.Color) { Opacity = 0.25 };
                transparentBrush.Freeze();
                dc.DrawGeometry(transparentBrush, pen, geometry);
            }
            else
            {
                // Normal rendering
                dc.DrawGeometry(brush, pen, geometry);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetDepthFactor01(float calculatedZ)
        {
            float near = NearZ; // lav Z -> mørkere (0)
            float far = FarZ;  // høy Z -> lysere (1)

            if (calculatedZ <= near) return 0f;
            if (calculatedZ >= far) return 1f;

            return (calculatedZ - near) / (far - near);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float NormalizeAngleTo01(float angle)
        {
            // Angle is typically a dot product in [-1, 1]; map to [0, 1]
            float normalized = (angle + 1f) * 0.5f;
            return Math.Clamp(normalized, 0f, 1f);
        }

        /*private void DrawTriangle(DrawingContext dc, _2dTriangleMesh triangle, SolidColorBrush brush, Pen pen)
        {
            var p1 = new Point(triangle.X1, triangle.Y1);
            var p2 = new Point(triangle.X2, triangle.Y2);
            var p3 = new Point(triangle.X3, triangle.Y3);

            if (geometryPoolIndex >= geometryPool.Count)
                geometryPool.Add(new StreamGeometry());

            var geometry = geometryPool[geometryPoolIndex++];
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(p1, true, true);
                ctx.LineTo(p2, true, false);
                ctx.LineTo(p3, true, false);
            }

            if (triangle.PartName != null && triangle.PartName.StartsWith("CrashBox-"))
            {
                // If rendering CrashBoxes -> use semi-transparent brush
                var transparentBrush = new SolidColorBrush(brush.Color) { Opacity = 0.25 };
                transparentBrush.Freeze();
                dc.DrawGeometry(transparentBrush, pen, geometry);
            }
            else
            {
                // Normal rendering
                dc.DrawGeometry(brush, pen, geometry);
            }
        }*/

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Color HexToColor(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return Colors.Black;
            if (hex[0] == '#') hex = hex.Substring(1);
            if (hex.Length < 6) return Colors.Black;

            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);

            return Color.FromArgb(255, r, g, b);
        }
    }
}
