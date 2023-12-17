using System;
using System.Collections.Generic;
using System.Text;

namespace _3dTesting._Coordinates
{
    public struct _2dTriangleMesh
    {
        public string PartName { get; set; }
        public float CalculatedZ { get; set; }
        public float Normal { get; set; }
        public float TriangleAngle { get; set; }
        public int X1 { get; set; }
        public int Y1 { get; set; }

        public int X2 { get; set; }
        public int Y2 { get; set; }

        public int X3 { get; set; }
        public int Y3 { get; set; }
        public string Color { get; set; }
    }
}
