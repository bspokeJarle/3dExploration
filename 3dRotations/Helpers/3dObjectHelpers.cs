using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting.Helpers
{
    public static class _3dObjectHelpers
    {
        public static bool _localLoggingEnabled = false;
        public static void ApplyScaleToTriangles(List<ITriangleMeshWithColor> triangles, float scale)
        {
            if (triangles == null || triangles.Count == 0) return;

            foreach (var tri in triangles)
            {
                // Assumes that vert1/vert2/vert3 are IVector3 with settable x/y/z
                tri.vert1.x *= scale;
                tri.vert1.y *= scale;
                tri.vert1.z *= scale;

                tri.vert2.x *= scale;
                tri.vert2.y *= scale;
                tri.vert2.z *= scale;

                tri.vert3.x *= scale;
                tri.vert3.y *= scale;
                tri.vert3.z *= scale;
            }
        }
        public static void ApplyScaleToObject(I3dObject actualObject, float scale)
        {
            if (actualObject == null || actualObject.ObjectParts.Count == 0) return;

            foreach (var part in actualObject.ObjectParts)
            {
                ApplyScaleToTriangles(part.Triangles, scale);
            }
            foreach (var crashBox in actualObject.CrashBoxes)
            {
                for (int i = 0; i < crashBox.Count; i++)
                {
                    crashBox[i] = new Vector3
                    {
                        x = crashBox[i].x * scale,
                        y = crashBox[i].y * scale,
                        z = crashBox[i].z * scale
                    };
                }
            }
        }
        public static List<IVector3> GenerateAabbCrashBoxFromRotated(List<IVector3> rotatedPoints)
        {
            if (rotatedPoints == null || rotatedPoints.Count < 2)
                return new List<IVector3>();

            var min = new Vector3
            {
                x = rotatedPoints.Min(p => p.x),
                y = rotatedPoints.Min(p => p.y),
                z = rotatedPoints.Min(p => p.z)
            };

            var max = new Vector3
            {
                x = rotatedPoints.Max(p => p.x),
                y = rotatedPoints.Max(p => p.y),
                z = rotatedPoints.Max(p => p.z)
            };

            return GenerateCrashBoxCorners(min, max);
        }
        public static List<IVector3> GenerateCrashBoxCorners(Vector3 min, Vector3 max)
        {
            return new List<IVector3>
            {
                new Vector3 { x = min.x, y = max.y, z = min.z }, // Corner 0
                new Vector3 { x = max.x, y = max.y, z = min.z }, // Corner 1
                new Vector3 { x = max.x, y = min.y, z = min.z }, // Corner 2
                new Vector3 { x = min.x, y = min.y, z = min.z }, // Corner 3
                new Vector3 { x = min.x, y = max.y, z = max.z }, // Corner 4
                new Vector3 { x = max.x, y = max.y, z = max.z }, // Corner 5
                new Vector3 { x = max.x, y = min.y, z = max.z }, // Corner 6
                new Vector3 { x = min.x, y = min.y, z = max.z }  // Corner 7
            };
        }
 
        public static double GetDistance(Vector3 point1, Vector3 point2)
        {
            float dx = point1.x - point2.x;
            float dy = point1.y - point2.y;
            float dz = point1.z - point2.z;

            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public struct CosSin
        {
            public float CosRes { get; set; }
            public float SinRes { get; set; }
        }

        public static bool CheckCollisionBoxVsBox(
            List<Vector3> boxA,
            List<Vector3> boxB,
            string? nameA = null,
            string? nameB = null
        )
        {
            var minA = new Vector3(boxA.Min(p => p.x), boxA.Min(p => p.y), boxA.Min(p => p.z));
            var maxA = new Vector3(boxA.Max(p => p.x), boxA.Max(p => p.y), boxA.Max(p => p.z));

            var minB = new Vector3(boxB.Min(p => p.x), boxB.Min(p => p.y), boxB.Min(p => p.z));
            var maxB = new Vector3(boxB.Max(p => p.x), boxB.Max(p => p.y), boxB.Max(p => p.z));

            const float margin = 5f;

            bool overlapX = (maxA.x + margin) >= (minB.x - margin) && (minA.x - margin) <= (maxB.x + margin);
            bool overlapY = (maxA.y + margin) >= (minB.y - margin) && (minA.y - margin) <= (maxB.y + margin);
            bool overlapZ = (maxA.z + margin) >= (minB.z - margin) && (minA.z - margin) <= (maxB.z + margin);

            if (_localLoggingEnabled && nameA != null && nameB != null)
            {
                Logger.Log(
                    $"AABBCHK {nameA} vs {nameB} | " +
                    $"X:{overlapX} Y:{overlapY} Z:{overlapZ} | " +
                    $"A[min=({minA.x:0.#},{minA.y:0.#},{minA.z:0.#}) max=({maxA.x:0.#},{maxA.y:0.#},{maxA.z:0.#})] " +
                    $"B[min=({minB.x:0.#},{minB.y:0.#},{minB.z:0.#}) max=({maxB.x:0.#},{maxB.y:0.#},{maxB.z:0.#})]"
                );
            }

            return overlapX && overlapY && overlapZ;
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
    }
}
