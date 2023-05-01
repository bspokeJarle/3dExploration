using _3dTesting._3dWorld;
using _3dTesting._Coordinates;
//using _3dTesting._Models;
using STL_Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace _3dTesting._3dRotation
{    
    public class _3dTo2d
    {
        List<_2dTriangleMesh> screenCoordinates { get; set; }
        const int perspectiveAdjustment = 1000;
        const int objectOffset = 200;
        const int objectZoom = 1500;
        const int screenCenter = 500;
        public List<_2dTriangleMesh> convertTo2dFromObjects(List<_3dObject> inhabitants)
        {
            screenCoordinates = new List<_2dTriangleMesh>();
            foreach (var obj in inhabitants)
            {
                foreach (var triangles in obj.Triangles)
                {
                    var xc = (triangles.vert1.x / ((triangles.vert1.z / perspectiveAdjustment) + objectOffset)) * objectZoom + (screenCenter+obj.Position.x);
                    var yc = (triangles.vert1.y / ((triangles.vert1.z / perspectiveAdjustment) + objectOffset)) * objectZoom + (screenCenter+obj.Position.y);

                    var xc2 = (triangles.vert2.x / ((triangles.vert2.z / perspectiveAdjustment) + objectOffset)) * objectZoom + (screenCenter+obj.Position.x);
                    var yc2 = (triangles.vert2.y / ((triangles.vert2.z / perspectiveAdjustment) + objectOffset)) * objectZoom + (screenCenter+obj.Position.y);

                    var xc3 = (triangles.vert3.x / ((triangles.vert3.z / perspectiveAdjustment) + objectOffset)) * objectZoom + (screenCenter+obj.Position.x);
                    var yc3 = (triangles.vert3.y / ((triangles.vert3.z / perspectiveAdjustment) + objectOffset)) * objectZoom + (screenCenter+obj.Position.y);

                    if (triangles.normal1.z > 0) screenCoordinates.Add(new _2dTriangleMesh { X1 = Convert.ToInt32(xc), Y1 = Convert.ToInt32(yc), X2 = Convert.ToInt32(xc2), Y2 = Convert.ToInt32(yc2), X3 = Convert.ToInt32(xc3), Y3 = Convert.ToInt32(yc3), CalculatedZ = ((triangles.vert1.z + triangles.vert2.z + triangles.vert3.z) / 3), Normal = triangles.normal1.z, TriangleAngle = triangles.angle });
                }
            }
            return screenCoordinates;
        }


    }
}
