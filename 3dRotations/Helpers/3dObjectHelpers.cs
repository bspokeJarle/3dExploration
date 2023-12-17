using _3dTesting._3dWorld;
using System.Collections.Generic;
using Domain;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting.Helpers
{
    public static class _3dObjectHelpers
    {
        public static float GetDeepestZ(ITriangleMeshWithColor triangle)
        {
            if (triangle.vert1.z <= triangle.vert2.z && triangle.vert1.z <= triangle.vert3.z) return triangle.vert1.z;
            if (triangle.vert2.z <= triangle.vert1.z && triangle.vert2.z <= triangle.vert3.z) return triangle.vert2.z;
            if (triangle.vert3.z <= triangle.vert1.z && triangle.vert3.z <= triangle.vert2.z) return triangle.vert3.z;
            return 0;
        }
        public static List<ITriangleMeshWithColor> ConvertToTrianglesWithColor(List<TriangleMesh> triangles,string color)
        {
            var triangleswithcolor = new List<ITriangleMeshWithColor>();
            foreach (var triangle in triangles)
            {                
                triangleswithcolor.Add(new TriangleMeshWithColor
                {
                    vert1 = new Vector3 { x = triangle.vert1.x, y = triangle.vert1.y, z = triangle.vert1.z },
                    vert2 = new Vector3 { x = triangle.vert2.x, y = triangle.vert2.y, z = triangle.vert2.z },
                    vert3 = new Vector3 { x = triangle.vert3.x, y = triangle.vert3.y, z = triangle.vert3.z },
                    normal1 = new Vector3 { x = triangle.normal1.x, y = triangle.normal1.y, z = triangle.normal1.z },
                    normal2 = new Vector3 { x = triangle.normal2.x, y = triangle.normal2.y, z = triangle.normal2.z },
                    normal3 = new Vector3 { x = triangle.normal3.x, y = triangle.normal3.y, z = triangle.normal3.z },
                    angle = triangle.angle,
                    Color = color
                });
            }
            return triangleswithcolor;
        }

        public static List<_3dObject> DeepCopy3dObjects(List<_3dObject> inhabitants)
        {
            //Copy all inhabitants to a new list with no references to the original inhabitants
            var theInhabitants = new List<_3dObject>();
            foreach (var inhabitant in inhabitants)
            {                
                var objectparts = new List<I3dObjectPart>();
                foreach (var part in inhabitant.ObjectParts)
                {
                    var Triangles = new List<ITriangleMeshWithColor>();
                    foreach (var triangle in part.Triangles)
                    {
                        Triangles.Add(new TriangleMeshWithColor
                        {
                            vert1 = new Vector3 { x = triangle.vert1.x, y = triangle.vert1.y, z = triangle.vert1.z },
                            vert2 = new Vector3 { x = triangle.vert2.x, y = triangle.vert2.y, z = triangle.vert2.z },
                            vert3 = new Vector3 { x = triangle.vert3.x, y = triangle.vert3.y, z = triangle.vert3.z },
                            normal1 = new Vector3 { x = triangle.normal1.x, y = triangle.normal1.y, z = triangle.normal1.z },
                            normal2 = new Vector3 { x = triangle.normal2.x, y = triangle.normal2.y, z = triangle.normal2.z },
                            normal3 = new Vector3 { x = triangle.normal3.x, y = triangle.normal3.y, z = triangle.normal3.z },
                            angle = triangle.angle,
                            Color = triangle.Color,                            
                        });
                    }
                    objectparts.Add(new _3dObjectPart { PartName = part.PartName, Triangles = Triangles, IsVisible = part.IsVisible});
                }            

                theInhabitants.Add(new _3dObject
                {
                    Position = new Vector3 { x = inhabitant.Position.x, y = inhabitant.Position.y, z = inhabitant.Position.z },
                    Rotation = new Vector3 { x = inhabitant.Rotation.x, y = inhabitant.Rotation.y, z = inhabitant.Rotation.z },
                    ObjectParts = objectparts,
                    Movement = inhabitant.Movement,
                    Particles = inhabitant.Particles,                    
                });
            }                    
            return theInhabitants;
        }
    }
}
