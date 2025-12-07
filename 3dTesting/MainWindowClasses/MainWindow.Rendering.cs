using _3dTesting._Coordinates;
using System;
using System.Collections.Generic;
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

        public int GetRenderingTriangleCount() => renderingTriangleCount;

        public WorldRenderer(DrawingVisualHost host, bool enableLocalLogging = false)
        {
            visualHost = host;
            _localLoggingEnabled = enableLocalLogging;

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
            string[] commonColors = new[] { "red", "green", "blue", "gray", "white", "black", "yellow", "orange", "brown" };
            int generatedCount = 0;

            for (float z = 0.2f; z <= 0.9f; z += 0.1f)
            {
                float roundedZ = (float)Math.Round(z, 2, MidpointRounding.AwayFromZero);

                foreach (var baseColor in commonColors)
                {
                    string normalized = baseColor.ToLowerInvariant();

                    var cacheKey = (roundedZ, normalized);
                    if (colorCache.ContainsKey(cacheKey))
                        continue;

                    try
                    {
                        string formattedZ = roundedZ.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

                        Color color = (Color)ColorConverter.ConvertFromString(
                            Helpers.Colors.getShadeOfColorFromNormal(roundedZ, normalized));
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
                            Logger.Log($"[WorldRenderer] ⚠️ Failed to prewarm color ({roundedZ}, {baseColor}): {ex.Message}");
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

                    if (triangle.X1 < 0 && triangle.X2 < 0 && triangle.X3 < 0) continue;
                    if (triangle.X1 > 1920 && triangle.X2 > 1920 && triangle.X3 > 1920) continue;
                    if (triangle.Y1 < 0 && triangle.Y2 < 0 && triangle.Y3 < 0) continue;
                    if (triangle.Y1 > 1080 && triangle.Y2 > 1080 && triangle.Y3 > 1080) continue;

                    float zKey = (float)Math.Round((triangle.CalculatedZ + 1050) / 3000f, 2);
                    string baseColor = triangle.Color.ToLowerInvariant();

                    if (!colorCache.TryGetValue((zKey, baseColor), out Color color))
                    {
                        color = (Color)ColorConverter.ConvertFromString(
                            Helpers.Colors.getShadeOfColorFromNormal(zKey, baseColor));
                        colorCache[(zKey, baseColor)] = color;
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

            var geometry = new StreamGeometry();
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

    }
}
