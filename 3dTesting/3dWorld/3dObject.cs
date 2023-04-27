using STL_Tools;
using System.Collections.Generic;

namespace _3dTesting._3dWorld
{
    public class _3dObject
    {
        public List<TriangleMesh> Triangles = new();
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set;}
        //todo add object relative properties, colors, ai etc    
    }
}
