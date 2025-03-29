using _3dTesting._3dWorld;
using _3dTesting._Coordinates;
using _3dTesting.Helpers;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting._3dRotation
{
    public class _3dTo2d
    {
        private const int perspectiveAdjustment = 1500;
        private const int defaultObjectZoom = 2;

        private const int screenSizeX = 1500;
        private const int screenSizeY = 1024;
        private const int screenCenterX = screenSizeX / 2;
        private const int screenCenterY = screenSizeY / 2;

        public List<_2dTriangleMesh> convertTo2dFromObjects(List<_3dObject> inhabitants)
        {
            var screenCoordinates = new ConcurrentBag<_2dTriangleMesh>();

            Parallel.ForEach(inhabitants, obj =>
            {
                if (obj == null || !obj.CheckInhabitantVisibility()) return;

                if (!ObjectPlacementHelpers.TryGetRenderPosition(obj, screenCenterX, screenCenterY, out double screenX, out double screenY, out double screenZ))
                    return;


                var projected = ConvertObjectTo2d(obj, screenX, screenY, screenZ);
                foreach (var tri in projected)
                    screenCoordinates.Add(tri);
            });

            return screenCoordinates.ToList();
        }
         
        private List<_2dTriangleMesh> ConvertObjectTo2d(_3dObject obj, double objPosX, double objPosY, double objPosZ)
        {
            var result = new List<_2dTriangleMesh>();

            foreach (var part in obj.ObjectParts)
            {
                if (!part.IsVisible) continue;

                foreach (var triangle in part.Triangles)
                {
                    var (x1, y1) = ProjectVertex((Vector3)triangle.vert1, objPosX, objPosY, objPosZ);
                    var (x2, y2) = ProjectVertex((Vector3)triangle.vert2, objPosX, objPosY, objPosZ);
                    var (x3, y3) = ProjectVertex((Vector3)triangle.vert3, objPosX, objPosY, objPosZ);

                    double xFactor = (x1 + x2 + x3) / 3;
                    double yFactor = (y1 + y2 + y3) / 3;

                    if (!IsOnScreen(xFactor, yFactor)) continue;

                    if (triangle.normal1.z > 0 || (triangle.noHidden ?? false))
                    {
                        result.Add(new _2dTriangleMesh
                        {
                            X1 = Convert.ToInt32(x1),
                            Y1 = Convert.ToInt32(y1),
                            X2 = Convert.ToInt32(x2),
                            Y2 = Convert.ToInt32(y2),
                            X3 = Convert.ToInt32(x3),
                            Y3 = Convert.ToInt32(y3),
                            CalculatedZ = ((triangle.vert1.z + triangle.vert2.z + triangle.vert3.z) / 3)
                                         + (_3dObjectHelpers.GetDeepestZ(triangle) + obj.Position.z),
                            Normal = triangle.normal1.z,
                            TriangleAngle = triangle.angle,
                            Color = triangle.Color,
                            PartName = part.PartName
                        });
                    }
                }
            }

            return result;
        }

        private (double x, double y) ProjectVertex(Vector3 v, double objPosX, double objPosY, double objPosZ)
        {
            double factor = perspectiveAdjustment / (-v.z + objPosZ + perspectiveAdjustment);
            double x = (v.x * factor * defaultObjectZoom) + objPosX;
            double y = (v.y * factor * defaultObjectZoom) + objPosY;
            return (x, y);
        }

        private static bool IsOnScreen(double x, double y)
        {
            return x >= -(screenSizeX * 0.2) && x <= (screenSizeX * 1.2)
                && y >= -(screenSizeY * 0.2) && y <= (screenSizeY * 1.2);
        }
    }
}
