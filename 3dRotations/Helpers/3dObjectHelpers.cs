using _3dTesting._3dWorld;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting.Helpers
{
    public static class _3dObjectHelpers
    {
        public static TriangleMeshWithColor MapFromVector3ToTMesh(this Vector3 coord)
        {
            return new TriangleMeshWithColor { vert1 = coord, vert2 = coord, vert3 = coord };
        }
        public struct CosSin
        {
            public float CosRes { get; set; }
            public float SinRes { get; set; }
        }
        public static CosSin ConvertFromAngleToCosSin(this float angle)
        {
            var radian = Math.PI * angle / 180.0;
            var sinRes = Math.Sin(radian);
            var cosRes = Math.Cos(radian);
            return new CosSin { CosRes = (float)cosRes, SinRes = (float)sinRes };
        }
        public static bool CheckCollisionPointVsBox(Vector3 Point, List<Vector3> CrashBox)
        {
            //Idea comes from here https://developer.mozilla.org/en-US/docs/Games/Techniques/3D_collision_detection
            var MinX = CrashBox.Select(CrashBox => CrashBox.x).Min();
            var MaxX = CrashBox.Select(CrashBox => CrashBox.x).Max();
            var MinY = CrashBox.Select(CrashBox => CrashBox.y).Min();
            var MaxY = CrashBox.Select(CrashBox => CrashBox.y).Max();
            var MinZ = CrashBox.Select(CrashBox => CrashBox.z).Min();
            var MaxZ = CrashBox.Select(CrashBox => CrashBox.z).Max();

            if (Point.x >= MinX &&
                Point.x <= MaxX &&
                Point.y >= MinY &&
                Point.y <= MaxY &&
                Point.z >= MinZ &&
                Point.z <= MaxZ) return true;
            return false;
        }

        public static bool CheckCollisionBoxVsBox(List<Vector3> CheckBox, List<Vector3> CrashBox)
        {
            //Go through all points of the CheckBox and check if they are inside the CrashBox
            foreach (var Point in CheckBox)
            {
                //Idea comes from here https://developer.mozilla.org/en-US/docs/Games/Techniques/3D_collision_detection
                var MinX = CrashBox.Select(CrashBox => CrashBox.x).Min();
                var MaxX = CrashBox.Select(CrashBox => CrashBox.x).Max();
                var MinY = CrashBox.Select(CrashBox => CrashBox.y).Min();
                var MaxY = CrashBox.Select(CrashBox => CrashBox.y).Max();
                var MinZ = CrashBox.Select(CrashBox => CrashBox.z).Min();
                var MaxZ = CrashBox.Select(CrashBox => CrashBox.z).Max();

                if (Point.x >= MinX &&
                    Point.x <= MaxX &&
                    Point.y >= MinY &&
                    Point.y <= MaxY &&
                    Point.z >= MinZ &&
                    Point.z <= MaxZ) return true;

            }
            return false;
        }

        public static float GetDeepestZ(ITriangleMeshWithColor triangle)
        {
            if (triangle.vert1.z <= triangle.vert2.z && triangle.vert1.z <= triangle.vert3.z) return triangle.vert1.z;
            if (triangle.vert2.z <= triangle.vert1.z && triangle.vert2.z <= triangle.vert3.z) return triangle.vert2.z;
            if (triangle.vert3.z <= triangle.vert1.z && triangle.vert3.z <= triangle.vert2.z) return triangle.vert3.z;
            return 0;
        }
        public static List<ITriangleMeshWithColor> ConvertToTrianglesWithColor(List<TriangleMesh> triangles, string color)
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
                            noHidden = triangle.noHidden,
                        });
                    }
                    objectparts.Add(new _3dObjectPart { PartName = part.PartName, Triangles = Triangles, IsVisible = part.IsVisible });
                }

                theInhabitants.Add(new _3dObject
                {
                    Position = new Vector3 { x = inhabitant.Position.x, y = inhabitant.Position.y, z = inhabitant.Position.z },
                    Rotation = new Vector3 { x = inhabitant.Rotation.x, y = inhabitant.Rotation.y, z = inhabitant.Rotation.z },
                    ObjectParts = objectparts,
                    Movement = inhabitant.Movement,
                    Particles = inhabitant.Particles,
                    CrashBoxes = inhabitant.CrashBoxes,
                    HasCrashed = inhabitant.HasCrashed,
                    Mass = inhabitant.Mass,
                    ObjectName = inhabitant.ObjectName,
                    ParentSurface = inhabitant.ParentSurface,
                    RotationOffsetX = inhabitant.RotationOffsetX,
                    RotationOffsetY = inhabitant.RotationOffsetY,
                    RotationOffsetZ = inhabitant.RotationOffsetZ
                });
            }
            return theInhabitants;
        }
    }
}
