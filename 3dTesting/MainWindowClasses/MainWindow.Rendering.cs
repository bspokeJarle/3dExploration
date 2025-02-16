using _3dTesting._Coordinates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace _3dTesting.Rendering
{
    public class WorldRenderer
    {
        private DrawingVisualHost visualHost;
        private DrawingVisual visual = new DrawingVisual();
        private DrawingContext drawingContext;
        private int renderingTriangleCount = 0;

        public int GetRenderingTriangleCount()
        {
            return renderingTriangleCount;
        }

        public WorldRenderer(DrawingVisualHost host)
        {
            visualHost = host;
        }

        public void RenderTriangles(List<_2dTriangleMesh> screenCoordinates)
        {
            using (drawingContext = visual.RenderOpen())
            {
                drawingContext.DrawRectangle(Brushes.Black, null, new Rect(0, 0, 1920, 1080));

                foreach (var triangle in screenCoordinates.OrderBy(z => z.CalculatedZ))
                {
                    if (triangle.CalculatedZ > 1000 || triangle.CalculatedZ < -1000)
                        continue;

                    float zcolorCalculation = ((triangle.CalculatedZ + 1000 + 500) / 2000);
                    Color color = (Color)ColorConverter.ConvertFromString(
                        Helpers.Colors.getShadeOfColorFromNormal(zcolorCalculation, triangle.Color));

                    DrawTriangle(triangle, color);
                }
            }
            visualHost.AddVisual(visual);
        }

        public void DrawTriangle(_2dTriangleMesh triangle, Color color)
        {
            Point p1 = new Point(triangle.X1, triangle.Y1);
            Point p2 = new Point(triangle.X2, triangle.Y2);
            Point p3 = new Point(triangle.X3, triangle.Y3);

            StreamGeometry geometry = new StreamGeometry();
            using (StreamGeometryContext ctx = geometry.Open())
            {
                ctx.BeginFigure(p1, true, true);
                ctx.LineTo(p2, true, false);
                ctx.LineTo(p3, true, false);
                ctx.LineTo(p1, true, false);
            }

            drawingContext.DrawGeometry(new SolidColorBrush(color), null, geometry);
        }
    }
}
