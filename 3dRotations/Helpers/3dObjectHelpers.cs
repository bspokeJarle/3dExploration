using _3dTesting._3dRotation;
using _3dTesting._3dWorld;
using Domain;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Nodes;
using System.Windows.Controls;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting.Helpers
{
    public static class _3dObjectHelpers
    {
        public static IVector3 GetLocalWorldPosition(this _3dObject inhabitant)
        {
            var globalMapPosition = inhabitant.ParentSurface.GlobalMapPosition;
            //Some objects will always be in location, they have no world position, just return
            if (inhabitant.WorldPosition.x == 0 && inhabitant.WorldPosition.y == 0 && inhabitant.WorldPosition.z == 0) return null;
            //Some objects fly around, they have this world position, so they appear when you are at that location in the map
            var localWorldPosition = new Vector3
            {
                x = globalMapPosition.x - inhabitant.WorldPosition.x,
                y = globalMapPosition.y - inhabitant.WorldPosition.y,
                z = globalMapPosition.z - inhabitant.WorldPosition.z
            };
            return localWorldPosition;
        }   
        public static bool CheckInhabitantVisibility(this _3dObject inhabitant)
        {
            //All of the onscreen objects have no world position, they are either visible all the time or landbased
            if (inhabitant.WorldPosition.x == 0 && inhabitant.WorldPosition.y == 0 && inhabitant.WorldPosition.z == 0) return true;
 
            var globalMapPosition = inhabitant.ParentSurface.GlobalMapPosition;
            var inhabitantPosition = inhabitant.WorldPosition;

            var distance = GetDistance(globalMapPosition, (Vector3)inhabitantPosition);
            //if (inhabitant.ObjectName=="Seeder") Debug.WriteLine($"Distance: {distance} globalMapPosition: {globalMapPosition.x} {globalMapPosition.y} {globalMapPosition.z} worldPosition: {inhabitantPosition.x} {inhabitantPosition.y} {inhabitantPosition.z} Inhabitant: {inhabitant.ObjectName} ");
            if (distance > 1400 || distance < -1400) return false;
            return true;
        }

        public static double GetDistance(Vector3 point1, Vector3 point2)
        {
            return Math.Sqrt(Math.Pow(point1.x - point2.x, 2) + Math.Pow(point1.y - point2.y, 2) + Math.Pow(point1.z - point2.z, 2));
        }

        public static void CenterObjectAt(I3dObject obj, IVector3 targetPosition)
        {
            if (obj == null || targetPosition == null)
                return;

            //Use offset from specified in the Scene
            var offset = obj.Position;

            // Calculate the geometric center of the object
            IVector3 objectCenter = GetObjectGeometricCenter(obj);

            // Compute the shift required
            float shiftX = targetPosition.x + offset.x - objectCenter.x;
            float shiftY = targetPosition.y + offset.y - objectCenter.y;
            float shiftZ = targetPosition.z + offset.z - objectCenter.z;

            // Apply the shift to the object's position
            obj.Position.x += shiftX;
            obj.Position.y += shiftY;
            obj.Position.z += shiftZ;

            // Move all object parts accordingly
            foreach (var part in obj.ObjectParts)
            {
                foreach (var triangle in part.Triangles)
                {
                    triangle.vert1.x += shiftX;
                    triangle.vert1.y += shiftY;
                    triangle.vert1.z += shiftZ;

                    triangle.vert2.x += shiftX;
                    triangle.vert2.y += shiftY;
                    triangle.vert2.z += shiftZ;

                    triangle.vert3.x += shiftX;
                    triangle.vert3.y += shiftY;
                    triangle.vert3.z += shiftZ;
                }
            }

            // Update world position
            obj.WorldPosition = new Vector3
            {
                x = targetPosition.x + offset.x,
                y = targetPosition.y + offset.y,
                z = targetPosition.z + offset.z
            };
        }

        private static IVector3 GetObjectGeometricCenter(I3dObject obj)
        {
            if (obj.ObjectParts == null || obj.ObjectParts.Count == 0)
                return new Vector3 { x = obj.Position.x, y = obj.Position.y, z = obj.Position.z };

            float sumX = 0, sumY = 0, sumZ = 0;
            int vertexCount = 0;

            foreach (var part in obj.ObjectParts)
            {
                foreach (var triangle in part.Triangles)
                {
                    sumX += triangle.vert1.x + triangle.vert2.x + triangle.vert3.x;
                    sumY += triangle.vert1.y + triangle.vert2.y + triangle.vert3.y;
                    sumZ += triangle.vert1.z + triangle.vert2.z + triangle.vert3.z;
                    vertexCount += 3;
                }
            }

            return new Vector3
            {
                x = vertexCount > 0 ? sumX / vertexCount : obj.Position.x,
                y = vertexCount > 0 ? sumY / vertexCount : obj.Position.y,
                z = vertexCount > 0 ? sumZ / vertexCount : obj.Position.z
            };
        }

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
                            landBasedPosition = triangle.landBasedPosition,
                            angle = triangle.angle,
                            Color = triangle.Color,
                            noHidden = triangle.noHidden
                        });
                    }
                    objectparts.Add(new _3dObjectPart { PartName = part.PartName, Triangles = Triangles, IsVisible = part.IsVisible });
                }

                theInhabitants.Add(new _3dObject
                {
                    Position = new Vector3 { x = inhabitant.Position.x, y = inhabitant.Position.y, z = inhabitant.Position.z },
                    Rotation = new Vector3 { x = inhabitant.Rotation.x, y = inhabitant.Rotation.y, z = inhabitant.Rotation.z },
                    WorldPosition = new Vector3 { x = inhabitant.WorldPosition.x, y = inhabitant.WorldPosition.y, z = inhabitant.WorldPosition.z },
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
                    RotationOffsetZ = inhabitant.RotationOffsetZ,
                    SurfaceBasedId = inhabitant.SurfaceBasedId
                });
            }
            return theInhabitants;
        }
    }  
}
