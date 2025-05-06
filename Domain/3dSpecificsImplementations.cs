using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public class _3dSpecificsImplementations
    {

        public class ImpactStatus : IImpactStatus
        {
            public bool HasCrashed { get; set; }
            public string ObjectName { get; set; }
            public ImpactDirection? ImpactDirection { get; set; }
            public IParticle SourceParticle { get; set; }
            public int? ObjectHealth { get; set; } = 100;
        }

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
            public override string ToString() => $"(x={x:F2}, y={y:F2}, z={z:F2})";
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
            public long? landBasedPosition { get; set; }
            public float angle { get; set; }
            public bool? noHidden { get; set; }

            public TriangleMesh()
            {
                normal1 = new Vector3();
                normal2 = new Vector3();
                normal3 = new Vector3();
                vert1 = new Vector3();
                vert2 = new Vector3();
                vert3 = new Vector3();
                landBasedPosition = 0;
                angle = 0;
            }
        }
    }
}
