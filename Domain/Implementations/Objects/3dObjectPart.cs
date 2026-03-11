using System.Collections.Generic;

namespace Domain
{
    public partial class _3dSpecificsImplementations
    {
        public class _3dObjectPart : I3dObjectPart
        {
            public List<ITriangleMeshWithColor> Triangles { get; set; } = new();
            public string? PartName { get; set; }
            public bool IsVisible { get; set; }
        }
    }
}
