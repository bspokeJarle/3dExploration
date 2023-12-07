using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public class _3dSpecificsImplementations
    {
        public class Vector3 : IVector3
        {
            public Vector3(float xVal = 0, float yVal = 0, float zVal = 0)
            {
                this.x = xVal;
                this.y = yVal;
                this.z = zVal;
            }

            public float x { get; set; }
            public float y { get; set; }
            public float z { get; set; }
        }

        public class TriangleMeshWithColor : TriangleMesh, ITriangleMeshWithColor
        {
            public string? Color { get; set; }
        }

        public class TriangleMesh : ITriangleMesh
        {
            public Domain.IVector3 normal1 { get; set; }
            public Domain.IVector3 normal2 { get; set; }
            public Domain.IVector3 normal3 { get; set; }
            public Domain.IVector3 vert1 { get; set; }
            public Domain.IVector3 vert2 { get; set; }
            public Domain.IVector3 vert3 { get; set; }
            public float angle { get; set; }

            public TriangleMesh()
            {
                normal1 = new Vector3();
                normal2 = new Vector3();
                normal3 = new Vector3();
                vert1 = new Vector3();
                vert2 = new Vector3();
                vert3 = new Vector3();
                angle = 0;
            }
        }
    }
}
