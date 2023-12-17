using _3dTesting._3dWorld;
using _3dTesting._Coordinates;
using _3dTesting.Helpers;
//using _3dTesting._Models;
using System;
using System.Collections.Generic;

namespace _3dTesting._3dRotation
{
    public class _3dTo2d
    {
        List<_2dTriangleMesh> screenCoordinates { get; set; }
        const int perspectiveAdjustment = 1000;
        const int objectOffset = 200;
        int defaultObjectZoom = 1500;
        const int screenCenterX = 750;
        const int screenCenterY = 512;
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
                    foreach (var triangles in parts.Triangles)
                    {
                        var xc = (triangles.vert1.x / ((triangles.vert1.z / perspectiveAdjustment) + objectOffset)) * objectZoom + (screenCenterX + obj.Position.x) + obj.Position.x;
                        var yc = (triangles.vert1.y / ((triangles.vert1.z / perspectiveAdjustment) + objectOffset)) * objectZoom + (screenCenterY + obj.Position.y) + obj.Position.y;

                        var xc2 = (triangles.vert2.x / ((triangles.vert2.z / perspectiveAdjustment) + objectOffset)) * objectZoom + (screenCenterX + obj.Position.x) + obj.Position.x;
                        var yc2 = (triangles.vert2.y / ((triangles.vert2.z / perspectiveAdjustment) + objectOffset)) * objectZoom + (screenCenterY + obj.Position.y) + obj.Position.y;

                        var xc3 = (triangles.vert3.x / ((triangles.vert3.z / perspectiveAdjustment) + objectOffset)) * objectZoom + (screenCenterX + obj.Position.x) + obj.Position.x;
                        var yc3 = (triangles.vert3.y / ((triangles.vert3.z / perspectiveAdjustment) + objectOffset)) * objectZoom + (screenCenterY + obj.Position.y) + obj.Position.y;

                        //If the triangle is outside the screen, don't draw it
                        var xFactor = (xc + xc2 + xc3) / 3;
                        if (xFactor > 2000) continue;
                        if (xFactor < 0) continue;

                        //If the triangle is outside the screen, don't draw it
                        var yFactor = (yc + yc2 + yc3) / 3;
                        if (yFactor > 1024) continue;
                        if (yFactor < 0) continue;

                        //if (triangles.normal1.z > 0) screenCoordinates.Add(new _2dTriangleMesh { X1 = Convert.ToInt32(xc), Y1 = Convert.ToInt32(yc), X2 = Convert.ToInt32(xc2), Y2 = Convert.ToInt32(yc2), X3 = Convert.ToInt32(xc3), Y3 = Convert.ToInt32(yc3), CalculatedZ = (((triangles.vert1.z + triangles.vert2.z + triangles.vert3.z) / 3) + obj.Position.z), Normal = triangles.normal1.z, TriangleAngle = triangles.angle, Color = triangles.Color });
                        //Try new method to sort triangles                                                
                        if (triangles.normal1.z > 0) screenCoordinates.Add(new _2dTriangleMesh
                        {
                            X1 = Convert.ToInt32(xc),
                            Y1 = Convert.ToInt32(yc),
                            X2 = Convert.ToInt32(xc2),
                            Y2 = Convert.ToInt32(yc2),
                            X3 = Convert.ToInt32(xc3),
                            Y3 = Convert.ToInt32(yc3),
                            CalculatedZ = ((triangles.vert1.z + triangles.vert2.z + triangles.vert3.z) / 3) + (_3dObjectHelpers.GetDeepestZ(triangles) + obj.Position.z),
                            Normal = triangles.normal1.z,
                            TriangleAngle = triangles.angle,
                            Color = triangles.Color,
                            PartName = parts.PartName
                        });
                    }
                }
            }
            return screenCoordinates;
        }


    }
}
