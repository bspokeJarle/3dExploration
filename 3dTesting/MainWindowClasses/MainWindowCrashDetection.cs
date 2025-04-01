using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using _3dTesting._3dWorld;
using Domain;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting.Helpers
{
    public static class CrashDetection
    {
        public static DateTime staticOjectLastCheck { get; set; } = new DateTime();
        private static DateTime _lastStaticCheck = DateTime.MinValue;

        public static void HandleCrashboxes(List<_3dObject> activeWorld)
        {
            int count = activeWorld.Count;
            bool shouldCheckStaticObjects = (DateTime.Now - _lastStaticCheck).TotalMilliseconds > 100;

            for (int i = 0; i < count; i++)
            {
                var inhabitant = activeWorld[i];
                if (inhabitant == null || inhabitant.CrashBoxes == null) continue;

                for (int j = i + 1; j < count; j++)
                {
                    var otherInhabitant = activeWorld[j];
                    if (otherInhabitant == null || otherInhabitant.CrashBoxes == null) continue;

                    bool isInhabitantStatic = IsStatic(inhabitant.ObjectName);
                    bool isOtherStatic = IsStatic(otherInhabitant.ObjectName);

                    if (inhabitant.ObjectName == otherInhabitant.ObjectName) continue;
                    if (isInhabitantStatic && isOtherStatic) continue;
                    if ((isInhabitantStatic || isOtherStatic) && !shouldCheckStaticObjects) continue;

                    if (isInhabitantStatic || isOtherStatic) _lastStaticCheck = DateTime.Now;

                    // Handle particles separately
                    if (inhabitant.ObjectName == "Particle" || otherInhabitant.ObjectName == "Particle")
                    {
                        if (HandleParticleCollision(inhabitant, otherInhabitant)) return;
                        continue;
                    }

                    if (HandleGeneralCollision(inhabitant, otherInhabitant)) return;
                }
            }
        }

        private static bool HandleParticleCollision(_3dObject a, _3dObject b)
        {
            var particle = a.ObjectName == "Particle" ? a : b;
            var other = particle == a ? b : a;

            foreach (var particleBox in particle.CrashBoxes)
            {
                var center = new Vector3
                {
                    x = particleBox.Average(p => p.x),
                    y = particleBox.Average(p => p.y),
                    z = particleBox.Average(p => p.z)
                };

                foreach (var otherBox in other.CrashBoxes)
                {
                    var min = new Vector3(otherBox.Min(p => p.x), otherBox.Min(p => p.y), otherBox.Min(p => p.z));
                    var max = new Vector3(otherBox.Max(p => p.x), otherBox.Max(p => p.y), otherBox.Max(p => p.z));

                    bool overlapX = center.x >= min.x && center.x <= max.x;
                    bool overlapY = center.y >= min.y && center.y <= max.y;
                    bool overlapZ = center.z >= min.z && center.z <= max.z;

                    if (overlapX && overlapY && overlapZ)
                    {
                        Logger.Log($"[PARTICLE COLLISION] {particle.ObjectName} <-> {other.ObjectName}");

                        var direction = EstimateDirection(center, min, max);

                        particle.ImpactStatus.HasCrashed = true;
                        particle.ImpactStatus.ImpactDirection = direction;

                        if (particle.ImpactStatus.SourceParticle?.ImpactStatus != null)
                        {
                            particle.ImpactStatus.SourceParticle.ImpactStatus.HasCrashed = true;
                            particle.ImpactStatus.SourceParticle.ImpactStatus.ImpactDirection = direction;
                        }

                        if (other.ImpactStatus != null)
                        {
                            other.ImpactStatus.HasCrashed = true;
                            other.ImpactStatus.ImpactDirection = direction;
                        }

                        return true;
                    }
                }
            }
            return false;
        }

        private static bool HandleGeneralCollision(_3dObject a, _3dObject b)
        {
            Logger.Log("----------------------------------------------------");
            Logger.Log($"[CrashCheck] Checking Start {a.ObjectName} vs {b.ObjectName}");

            ObjectPlacementHelpers.TryGetCrashboxWorldPosition(a, out var worldA);
            ObjectPlacementHelpers.TryGetCrashboxWorldPosition(b, out var worldB);

            var rotatedA = RotateAllCrashboxes(a.CrashBoxes, (Vector3)a.Rotation, (Vector3)a.ObjectOffsets, worldA, a.ObjectName);
            var rotatedB = RotateAllCrashboxes(b.CrashBoxes, (Vector3)b.Rotation, (Vector3)b.ObjectOffsets, worldB, b.ObjectName);

            foreach (var boxA in rotatedA)
            {
                foreach (var boxB in rotatedB)
                {
                    CenterCrashBoxIfSurfaceBased(a, boxA);
                    CenterCrashBoxIfSurfaceBased(b, boxB);

                    if (Logger.EnableFileLogging)
                    {
                        ObjectPlacementHelpers.LogCrashboxContact(a.ObjectName, a, boxA, b, boxB);
                        ObjectPlacementHelpers.LogCrashboxAnalysis($"{a.ObjectName} CrashBox", boxA);
                        ObjectPlacementHelpers.LogCrashboxAnalysis($"{b.ObjectName} CrashBox", boxB);
                    }

                    if (_3dObjectHelpers.CheckCollisionBoxVsBox(boxA, boxB))
                    {
                        Logger.Log($"[COLLISION] {a.ObjectName} <-> {b.ObjectName}");
                        a.ImpactStatus.HasCrashed = true;
                        b.ImpactStatus.HasCrashed = true;

                        var centerA = GetCenterOfBox(boxA);
                        var centerB = GetCenterOfBox(boxB);

                        a.ImpactStatus.ImpactDirection = EstimateDirection(centerA, centerB);
                        b.ImpactStatus.ImpactDirection = EstimateDirection(centerB, centerA);

                        return true;
                    }
                }
            }

            Logger.Log($"[CrashCheck] Checking End {a.ObjectName} vs {b.ObjectName}");
            return false;
        }

        private static ImpactDirection EstimateDirection(Vector3 from, Vector3 min, Vector3 max)
        {
            var center = new Vector3
            {
                x = (min.x + max.x) / 2,
                y = (min.y + max.y) / 2,
                z = (min.z + max.z) / 2
            };

            return EstimateDirection(from, center);
        }

        private static ImpactDirection EstimateDirection(Vector3 from, Vector3 to)
        {
            float dx = from.x - to.x;
            float dy = from.y - to.y;
            float dz = from.z - to.z;

            if (Math.Abs(dx) > Math.Abs(dy) && Math.Abs(dx) > Math.Abs(dz))
                return dx > 0 ? ImpactDirection.Right : ImpactDirection.Left;
            else if (Math.Abs(dy) > Math.Abs(dz))
                return dy > 0 ? ImpactDirection.Top : ImpactDirection.Bottom;
            else
                return ImpactDirection.Center;
        }

        private static Vector3 GetCenterOfBox(List<Vector3> box)
        {
            return new Vector3
            {
                x = box.Average(p => p.x),
                y = box.Average(p => p.y),
                z = box.Average(p => p.z)
            };
        }

        private static void CenterCrashBoxIfSurfaceBased(_3dObject obj, List<Vector3> box)
        {
            if (obj?.SurfaceBasedId > 0)
            {
                var tri = obj.ParentSurface?.RotatedSurfaceTriangles.Find(t => t.landBasedPosition == obj.SurfaceBasedId);
                if (tri != null)
                    ObjectPlacementHelpers.CenterCrashBoxAt(box, tri.vert1, obj.CrashboxOffsets);
            }
        }

        public static bool IsStatic(string objectName) =>
            objectName == "Tree" || objectName == "Surface" || objectName == "House";

        private static List<List<Vector3>> RotateAllCrashboxes(List<List<IVector3>> crashboxes, Vector3 rotation, Vector3 objectOffsets, Vector3 worldPosition, string objectName)
        {
            var rotatedCrashboxes = new List<List<Vector3>>(crashboxes.Count);
            foreach (var crashbox in crashboxes)
            {
                var rotated = new List<Vector3>(crashbox.Count);
                foreach (var point in crashbox)
                {
                    rotated.Add(RotatePoint(point, rotation, objectOffsets, worldPosition, objectName));
                }
                rotatedCrashboxes.Add(rotated);
            }
            return rotatedCrashboxes;
        }

        private static Vector3 RotatePoint(IVector3 point, Vector3 rotation, Vector3 objectOffsets, Vector3 worldPosition, string objectName)
        {
            var singleTriangle = new List<ITriangleMeshWithColor>
            {
                new TriangleMeshWithColor
                {
                    vert1 = new Vector3 { x = point.x, y = point.y, z = point.z },
                    vert2 = new Vector3 { x = point.x, y = point.y, z = point.z },
                    vert3 = new Vector3 { x = point.x, y = point.y, z = point.z }
                }
            };

            var rotatedTriangle = GameHelpers.RotateMesh(singleTriangle, rotation);
            var rotatedPoint = rotatedTriangle[0].vert1;

            rotatedPoint.x += worldPosition.x + objectOffsets.x;
            rotatedPoint.y += worldPosition.y + objectOffsets.y;
            rotatedPoint.z += worldPosition.z + objectOffsets.z;

            return (Vector3)rotatedPoint;
        }
    }
}
