using _3dTesting._3dWorld;
using _3dTesting._Coordinates;
using _3dTesting.Helpers;
//using _3dTesting._Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace _3dTesting._3dRotation
{
    public class _3dTo2d
    {
        List<_2dTriangleMesh> screenCoordinates { get; set; }
        const int perspectiveAdjustment = 1000;
        const int objectOffset = 200;
        int defaultObjectZoom = 1500;
        const int screenCenterX = screenSizeX/2;
        const int screenCenterY = screenSizeY/2;
        const int screenSizeX = 1500;
        const int screenSizeY = 1024;
        public List<_2dTriangleMesh> convertTo2dFromObjects(List<_3dObject> inhabitants)
        {
            screenCoordinates = new List<_2dTriangleMesh>();
            foreach (var obj in inhabitants)
            {
                var objectZoom = defaultObjectZoom;
                if (obj.Position.z != 0) objectZoom = (int)obj.Position.z;
                foreach (var parts in obj.ObjectParts)
                {
                    if (parts.IsVisible == false) continue;
                    foreach (var triangle in parts.Triangles)
                    {
                        var xc = (triangle.vert1.x / ((triangle.vert1.z / perspectiveAdjustment) + objectOffset)) * objectZoom + (screenCenterX + obj.Position.x) + obj.Position.x;
                        var yc = (triangle.vert1.y / ((triangle.vert1.z / perspectiveAdjustment) + objectOffset)) * objectZoom + (screenCenterY + obj.Position.y) + obj.Position.y;

                        var xc2 = (triangle.vert2.x / ((triangle.vert2.z / perspectiveAdjustment) + objectOffset)) * objectZoom + (screenCenterX + obj.Position.x) + obj.Position.x;
                        var yc2 = (triangle.vert2.y / ((triangle.vert2.z / perspectiveAdjustment) + objectOffset)) * objectZoom + (screenCenterY + obj.Position.y) + obj.Position.y;

                        var xc3 = (triangle.vert3.x / ((triangle.vert3.z / perspectiveAdjustment) + objectOffset)) * objectZoom + (screenCenterX + obj.Position.x) + obj.Position.x;
                        var yc3 = (triangle.vert3.y / ((triangle.vert3.z / perspectiveAdjustment) + objectOffset)) * objectZoom + (screenCenterY + obj.Position.y) + obj.Position.y;

                        //If the triangle is outside the screen, don't draw it
                        var xFactor = (xc + xc2 + xc3) / 3;
                        if (xFactor > 3400) continue;
                        if (xFactor < 0) continue;

                        //If the triangle is outside the screen, don't draw it
                        var yFactor = (yc + yc2 + yc3) / 3;
                        if (yFactor > 1024) continue;
                        if (yFactor < 0) continue;

                        //if (triangles.normal1.z > 0) screenCoordinates.Add(new _2dTriangleMesh { X1 = Convert.ToInt32(xc), Y1 = Convert.ToInt32(yc), X2 = Convert.ToInt32(xc2), Y2 = Convert.ToInt32(yc2), X3 = Convert.ToInt32(xc3), Y3 = Convert.ToInt32(yc3), CalculatedZ = (((triangles.vert1.z + triangles.vert2.z + triangles.vert3.z) / 3) + obj.Position.z), Normal = triangles.normal1.z, TriangleAngle = triangles.angle, Color = triangles.Color });
                        //Try new method to sort triangles                                                
                        if (triangle.normal1.z > 0 || triangle.noHidden==true)
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
