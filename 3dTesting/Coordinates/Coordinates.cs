using System;
using System.Collections.Generic;
using System.Text;

namespace _3dTesting._Coordinates
{
    public struct _3dCoordinate
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }
    public struct _2dCoordinate
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
    public struct _2dTriangleMesh
    {
        public float CalculatedZ { get; set; }
        public float Normal { get; set; }
        public float TriangleAngle { get; set; }
        public int X1 { get; set; }
        public int Y1 { get; set; }

        public int X2 { get; set; }
        public int Y2 { get; set; }

        public int X3 { get; set; }
        public int Y3 { get; set; }
    }
}
