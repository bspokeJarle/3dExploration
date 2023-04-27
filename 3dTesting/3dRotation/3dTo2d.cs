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
        const int perspectiveAdjustment = 4;
        const int objectOffset = 200;
        const int objectZoom = 1500;
        const int screenCenter = 500;
        //Convert 3d coordinates to 2d coordinates with perspective
        public List<_2dTriangleMesh> convertTo2d(List<TriangleMesh> coordinates)
        {
            screenCoordinates = new List<_2dTriangleMesh>();
            foreach (var coor in coordinates)
            {
                var xc = (coor.vert1.x / ((coor.vert1.z/perspectiveAdjustment)+objectOffset)) * objectZoom+screenCenter;
                var yc = (coor.vert1.y / ((coor.vert1.z/perspectiveAdjustment)+objectOffset)) * objectZoom+screenCenter;

                var xc2 = (coor.vert2.x / ((coor.vert2.z / perspectiveAdjustment) + objectOffset)) * objectZoom + screenCenter;
                var yc2 = (coor.vert2.y / ((coor.vert2.z / perspectiveAdjustment) + objectOffset)) * objectZoom + screenCenter;

                var xc3 = (coor.vert3.x / ((coor.vert3.z / perspectiveAdjustment) + objectOffset)) * objectZoom + screenCenter;
                var yc3 = (coor.vert3.y / ((coor.vert3.z / perspectiveAdjustment) + objectOffset)) * objectZoom + screenCenter;
                
                if (coor.normal1.z>0) screenCoordinates.Add(new _2dTriangleMesh { X1 = Convert.ToInt32(xc), Y1 = Convert.ToInt32(yc), X2 = Convert.ToInt32(xc2), Y2 = Convert.ToInt32(yc2), X3 = Convert.ToInt32(xc3), Y3 = Convert.ToInt32(yc3), CalculatedZ=((coor.vert1.z+coor.vert2.z+coor.vert3.z)/3),Normal = coor.normal1.z, TriangleAngle = coor.angle });
                               
                //Without perspective
                //screenCoordinates.Add(new _2dTriangleMesh { X1 = Convert.ToInt32(coor.vert1.x), Y1 = Convert.ToInt32(coor.vert1.y), X2 = Convert.ToInt32(coor.vert2.x), Y2 = Convert.ToInt32(coor.vert2.y), X3 = Convert.ToInt32(coor.vert3.x), Y3 = Convert.ToInt32(coor.vert3.y) });
            }
            return screenCoordinates;
        }

    }
}
