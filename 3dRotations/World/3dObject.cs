using Domain;
using System.Collections.Generic;

namespace _3dTesting._3dWorld
{
    public class _3dObject : I3dObject
    {
        public List<I3dObjectPart> ObjectParts { get; set; } = new();
        public IVector3? Position { get; set; }
        public IVector3? Rotation { get; set; }
        public IObjectMovement? Movement { get; set; }
        public IParticles? Particles { get; set; }
        public List<List<IVector3>> CrashBoxes { get; set; }
        public bool HasCrashed { get; set; } = false;
        public int? Mass { get; set; }
        public string ObjectName { get; set; }
        //todo add object relative properties, colors, ai etc    
    }
    public class _3dObjectPart : I3dObjectPart
    {
        public List<ITriangleMeshWithColor> Triangles { get; set; } = new();
        public string? PartName { get; set; }
        public bool IsVisible { get; set; }
    }
}
