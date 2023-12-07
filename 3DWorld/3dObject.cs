using STL_Tools;
using System;
using System.Collections.Generic;

namespace _3dTesting._3dWorld
{    
    public class _3dObject
    {        
        public List<_3dObjectPart> ObjectParts = new();
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set;}
        //todo add object relative properties, colors, ai etc    
    }
    public class _3dObjectPart
    {
        public List<TriangleMeshWithColor> Triangles = new();
        public string PartName { get; set; }           
    }
    public class TriangleMeshWithColor: TriangleMesh
    {
        public string Color { get; set; }
    }
}
