using _3dTesting._3dWorld;
using STL_Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTesting.Helpers
{
    public static class _3dObjectHelpers
    {
        public static List<_3dObject> DeepCopy3dObjects(List<_3dObject> inhabitants)
        {
            //Copy all inhabitants to a new list with no references to the original inhabitants
            var theobjects = new List<_3dObject>();
            foreach (var inhabitant in inhabitants)
            {
                var Triangles = new List<TriangleMesh>();
                foreach (var triangle in inhabitant.Triangles)
                {
                    Triangles.Add(new TriangleMesh
                    {
                        vert1 = new Vector3 { x = triangle.vert1.x, y = triangle.vert1.y, z = triangle.vert1.z },
                        vert2 = new Vector3 { x = triangle.vert2.x, y = triangle.vert2.y, z = triangle.vert2.z },
                        vert3 = new Vector3 { x = triangle.vert3.x, y = triangle.vert3.y, z = triangle.vert3.z },
                        normal1 = new Vector3 { x = triangle.normal1.x, y = triangle.normal1.y, z = triangle.normal1.z },
                        normal2 = new Vector3 { x = triangle.normal2.x, y = triangle.normal2.y, z = triangle.normal2.z },
                        normal3 = new Vector3 { x = triangle.normal3.x, y = triangle.normal3.y, z = triangle.normal3.z },
                        angle = triangle.angle
                    });
                }

                theobjects.Add(new _3dObject
                {
                    Position = new Vector3 { x = inhabitant.Position.x, y = inhabitant.Position.y, z = inhabitant.Position.z },
                    Rotation = new Vector3 { x = inhabitant.Rotation.x, y = inhabitant.Rotation.y, z = inhabitant.Rotation.z },
                    Triangles = Triangles
                });
            }                    
            return theobjects;
        }
    }
}
