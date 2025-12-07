using _3dTesting._3dWorld;
using Domain;
using GameAiAndControls.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Nodes;
using System.Windows.Documents;
using static Domain._3dSpecificsImplementations;
using CommonUtilities._3DHelpers;

namespace _3dTesting.Helpers
{
    public static class _3dObjectHelpers
    {
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
        public static CosSin ConvertFromAngleToCosSin(this float angle)
        {
            var radian = Math.PI * angle / 180.0;
            var sinRes = Math.Sin(radian);
            var cosRes = Math.Cos(radian);
            return new CosSin { CosRes = (float)cosRes, SinRes = (float)sinRes };
        }

        public static bool CheckCollisionBoxVsBox(List<Vector3> boxA, List<Vector3> boxB)
        {
            var minA = new Vector3(boxA.Min(p => p.x), boxA.Min(p => p.y), boxA.Min(p => p.z));
            var maxA = new Vector3(boxA.Max(p => p.x), boxA.Max(p => p.y), boxA.Max(p => p.z));

            var minB = new Vector3(boxB.Min(p => p.x), boxB.Min(p => p.y), boxB.Min(p => p.z));
            var maxB = new Vector3(boxB.Max(p => p.x), boxB.Max(p => p.y), boxB.Max(p => p.z));

            const float margin = 10f;

            bool overlapX = (maxA.x + margin) >= (minB.x - margin) && (minA.x - margin) <= (maxB.x + margin);
            bool overlapY = (maxA.y + margin) >= (minB.y - margin) && (minA.y - margin) <= (maxB.y + margin);
            bool overlapZ = (maxA.z + margin) >= (minB.z - margin) && (minA.z - margin) <= (maxB.z + margin);

            return overlapX && overlapY && overlapZ;
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

        public static ParticlesAI DeepCopyParticlesAI(ParticlesAI original)
        {
            var copy = new ParticlesAI
            {
                ParentShip = original.ParentShip, // Keeping reference, change if deep copy is needed
                Visible = original.Visible,
                Particles = DeepCopyParticles(original.Particles)
            };

            return copy;
        }

        public static List<IParticle> DeepCopyParticles(List<IParticle> originalParticles)
        {
            List<IParticle> copiedParticles = new List<IParticle>();

            foreach (var original in originalParticles)
            {
                var copy = new Particle // Ensure `Particle` is the concrete class implementing `IParticle`
                {
                    ParticleTriangle = new TriangleMeshWithColor
                    {
                        Color = original.ParticleTriangle.Color,
                        normal1 = new Vector3(original.ParticleTriangle.normal1.x, original.ParticleTriangle.normal1.y, original.ParticleTriangle.normal1.z),
                        normal2 = new Vector3(original.ParticleTriangle.normal2.x, original.ParticleTriangle.normal2.y, original.ParticleTriangle.normal2.z),
                        normal3 = new Vector3(original.ParticleTriangle.normal3.x, original.ParticleTriangle.normal3.y, original.ParticleTriangle.normal3.z),
                        vert1 = new Vector3(original.ParticleTriangle.vert1.x, original.ParticleTriangle.vert1.y, original.ParticleTriangle.vert1.z),
                        vert2 = new Vector3(original.ParticleTriangle.vert2.x, original.ParticleTriangle.vert2.y, original.ParticleTriangle.vert2.z),
                        vert3 = new Vector3(original.ParticleTriangle.vert3.x, original.ParticleTriangle.vert3.y, original.ParticleTriangle.vert3.z),
                        landBasedPosition = original.ParticleTriangle.landBasedPosition,
                        angle = original.ParticleTriangle.angle,
                        noHidden = original.ParticleTriangle.noHidden
                    },
                    Velocity = new Vector3(original.Velocity.x, original.Velocity.y, original.Velocity.z),
                    Acceleration = new Vector3(original.Acceleration.x, original.Acceleration.y, original.Acceleration.z),
                    VariedStart = original.VariedStart,
                    Life = original.Life,
                    Size = original.Size,
                    Color = original.Color,
                    BirthTime = original.BirthTime,
                    IsRotated = original.IsRotated,
                    Position = new Vector3(original.Position.x, original.Position.y, original.Position.z),
                    WorldPosition = new Vector3(original.WorldPosition.x, original.WorldPosition.y, original.WorldPosition.z),
                    Rotation = original.Rotation != null ? new Vector3(original.Rotation.x, original.Rotation.y, original.Rotation.z) : null,
                    RotationSpeed = original.RotationSpeed != null ? new Vector3(original.RotationSpeed.x, original.RotationSpeed.y, original.RotationSpeed.z) : null,
                    NoShading = original.NoShading,
                    Visible = original.Visible
                };
                copiedParticles.Add(copy);
            }

            return copiedParticles;
        }

        public static List<List<IVector3>> CopyCrashboxes(List<List<IVector3>> original)
        {
            var result = new List<List<IVector3>>(original.Count);
            foreach (var box in original)
            {
                var copiedBox = new List<IVector3>(box.Count);
                foreach (var point in box)
                {
                    copiedBox.Add(new Vector3
                    {
                        x = point.x,
                        y = point.y,
                        z = point.z
                    });
                }
                result.Add(copiedBox);
            }
            return result;
        }

    }
}
