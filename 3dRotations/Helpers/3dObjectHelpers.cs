using CommonUtilities.CommonSetup;
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

            // Track already-scaled vertices to avoid scaling shared Vector3 instances multiple times
            var scaled = new HashSet<IVector3>(ReferenceEqualityComparer.Instance);

            foreach (var part in actualObject.ObjectParts)
            {
                if (part.Triangles == null || part.Triangles.Count == 0) continue;

                foreach (var tri in part.Triangles)
                {
                    if (scaled.Add(tri.vert1)) { tri.vert1.x *= scale; tri.vert1.y *= scale; tri.vert1.z *= scale; }
                    if (scaled.Add(tri.vert2)) { tri.vert2.x *= scale; tri.vert2.y *= scale; tri.vert2.z *= scale; }
                    if (scaled.Add(tri.vert3)) { tri.vert3.x *= scale; tri.vert3.y *= scale; tri.vert3.z *= scale; }
                }
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

            float marginX = -GameSetup.CollisionMarginX;
            float marginY = GameSetup.CollisionMarginY;
            float marginZ = GameSetup.CollisionMarginZ;

            bool overlapX = (maxA.x + marginX) >= (minB.x - marginX) && (minA.x - marginX) <= (maxB.x + marginX);
            bool overlapY = (maxA.y + marginY) >= (minB.y - marginY) && (minA.y - marginY) <= (maxB.y + marginY);
            bool overlapZ = (maxA.z + marginZ) >= (minB.z - marginZ) && (minA.z - marginZ) <= (maxB.z + marginZ);

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

        // ----------------------------------------------------
        //  RIGHT-HAND RULE GEOMETRY HELPERS
        // ----------------------------------------------------

        public static void AddQuadOutward(
            List<ITriangleMeshWithColor> tris,
            Vector3 v1,
            Vector3 v2,
            Vector3 v3,
            Vector3 v4,
            Vector3 center,
            string color,
            bool noHidden = false)
        {
            tris.Add(CreateTriangleOutward(v1, v2, v3, center, color, noHidden));
            tris.Add(CreateTriangleOutward(v1, v3, v4, center, color, noHidden));
        }

        public static TriangleMeshWithColor CreateTriangleOutward(
            Vector3 v1,
            Vector3 v2,
            Vector3 v3,
            Vector3 center,
            string color,
            bool noHidden = false)
        {
            var edge1 = Subtract(v2, v1);
            var edge2 = Subtract(v3, v1);
            var normal = Normalize(Cross(edge1, edge2));

            var mid = new Vector3
            {
                x = (v1.x + v2.x + v3.x) / 3f,
                y = (v1.y + v2.y + v3.y) / 3f,
                z = (v1.z + v2.z + v3.z) / 3f
            };

            var desired = Normalize(Subtract(mid, center));
            float dot = Dot(normal, desired);

            if (dot < 0f)
            {
                var temp = v2;
                v2 = v3;
                v3 = temp;
            }

            return new TriangleMeshWithColor
            {
                Color = color,
                vert1 = v1,
                vert2 = v2,
                vert3 = v3,
                noHidden = noHidden
            };
        }

        public static Vector3 Subtract(Vector3 a, Vector3 b)
        {
            return new Vector3
            {
                x = a.x - b.x,
                y = a.y - b.y,
                z = a.z - b.z
            };
        }

        public static Vector3 Add(Vector3 a, Vector3 b)
        {
            return new Vector3
            {
                x = a.x + b.x,
                y = a.y + b.y,
                z = a.z + b.z
            };
        }

        public static Vector3 Scale(Vector3 v, float s)
        {
            return new Vector3
            {
                x = v.x * s,
                y = v.y * s,
                z = v.z * s
            };
        }

        public static Vector3 Cross(Vector3 a, Vector3 b)
        {
            return new Vector3
            {
                x = a.y * b.z - a.z * b.y,
                y = a.z * b.x - a.x * b.z,
                z = a.x * b.y - a.y * b.x
            };
        }

        public static float Dot(Vector3 a, Vector3 b)
        {
            return a.x * b.x + a.y * b.y + a.z * b.z;
        }

        public static Vector3 Normalize(Vector3 v)
        {
            float lenSq = v.x * v.x + v.y * v.y + v.z * v.z;
            if (lenSq <= 1e-6f)
                return new Vector3 { x = 0, y = 0, z = 0 };

            float invLen = 1.0f / (float)Math.Sqrt(lenSq);
            return new Vector3
            {
                x = v.x * invLen,
                y = v.y * invLen,
                z = v.z * invLen
            };
        }
    }
}
