using _3dRotations.Helpers;
using _3dTesting._3dWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dRotations.World.Objects
{
    public static class Surface
    {
        public static _3dObject CreateSurface()
        {
            var surface = new _3dObject();
            //make code that calls the surface creator
            var surfaceTriangles = SurfaceGeneration.Generate();
            surface.ObjectParts.Add(new _3dObjectPart { PartName = "Surface", Triangles = surfaceTriangles, IsVisible=true });
            return surface;
        }
    }
}
