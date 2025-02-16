using _3dTesting._3dWorld;
using _3dTesting._Coordinates;
using _3dTesting.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace _3dTesting._3dRotation
{
    public class _3dTo2d
    {
        List<_2dTriangleMesh> screenCoordinates { get; set; }
        const int perspectiveAdjustment = 3000;
        const int objectOffset = 200;
        int defaultObjectZoom = 1500;

        const int screenSizeX = 1500;
        const int screenSizeY = 1024;
        const int screenCenterX = screenSizeX / 2;
        const int screenCenterY = screenSizeY / 2;

        public List<_2dTriangleMesh> convertTo2dFromObjects(List<_3dObject> inhabitants)
        {
            screenCoordinates = new List<_2dTriangleMesh>();

            foreach (var obj in inhabitants)
            {
                if (obj==null) continue;
                int objPosX = screenCenterX + (int)obj.Position.x;
                int objPosY = screenCenterY + (int)obj.Position.y;
                int objectZoom = (int)(obj.Position.z != 0 ? obj.Position.z : defaultObjectZoom);

                foreach (var parts in obj.ObjectParts)
                {
                    if (!parts.IsVisible) continue;

                    foreach (var triangle in parts.Triangles)
                    {
                        var xc = (triangle.vert1.x / Math.Max(1, (triangle.vert1.z / perspectiveAdjustment) + objectOffset)) * objectZoom + objPosX + obj.Position.x;
                        var yc = (triangle.vert1.y / Math.Max(1, (triangle.vert1.z / perspectiveAdjustment) + objectOffset)) * objectZoom + objPosY + obj.Position.y;

                        var xc2 = (triangle.vert2.x / Math.Max(1, (triangle.vert2.z / perspectiveAdjustment) + objectOffset)) * objectZoom + objPosX + obj.Position.x;
                        var yc2 = (triangle.vert2.y / Math.Max(1, (triangle.vert2.z / perspectiveAdjustment) + objectOffset)) * objectZoom + objPosY + obj.Position.y;

                        var xc3 = (triangle.vert3.x / Math.Max(1, (triangle.vert3.z / perspectiveAdjustment) + objectOffset)) * objectZoom + objPosX + obj.Position.x;
                        var yc3 = (triangle.vert3.y / Math.Max(1, (triangle.vert3.z / perspectiveAdjustment) + objectOffset)) * objectZoom + objPosY + obj.Position.y;

                        var xFactor = (xc + xc2 + xc3) / 3;
                        if (xFactor < 0 || xFactor > screenSizeX) continue;

                        var yFactor = (yc + yc2 + yc3) / 3;
                        if (yFactor < 0 || yFactor > screenSizeY) continue;

                        if (triangle.normal1.z > 0 || (triangle.noHidden ?? false))
                        {
                            screenCoordinates.Add(new _2dTriangleMesh
                            {
                                X1 = Convert.ToInt32(xc),
                                Y1 = Convert.ToInt32(yc),
                                X2 = Convert.ToInt32(xc2),
                                Y2 = Convert.ToInt32(yc2),
                                X3 = Convert.ToInt32(xc3),
                                Y3 = Convert.ToInt32(yc3),
                                CalculatedZ = ((triangle.vert1.z + triangle.vert2.z + triangle.vert3.z) / 3) + (_3dObjectHelpers.GetDeepestZ(triangle) + obj.Position.z),
                                Normal = triangle.normal1.z,
                                TriangleAngle = triangle.angle,
                                Color = triangle.Color,
                                PartName = parts.PartName
                            });
                        }
                    }
                }
            }
            return screenCoordinates;
        }
    }
}
