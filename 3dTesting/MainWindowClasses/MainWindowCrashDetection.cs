using System;
using System.Collections.Generic;
using System.Linq;
using _3dTesting._3dWorld;
using Domain;
using static Domain._3dSpecificsImplementations;
using System.Runtime.CompilerServices;

namespace _3dTesting.Helpers
{
    public static class CrashDetection
    {
        public static DateTime staticOjectLastCheck { get; set; } = new DateTime();
        private static DateTime _lastStaticCheck = DateTime.MinValue;
        private static bool _skipParticles = true;

        public static bool LocalEnableLogging = false;
        public static double MaxCrashDistance = 750.0;

        private static readonly Dictionary<_3dObject, List<List<Vector3>>> RotatedBoxCache = new();
        private static int CacheHits = 0;
        private static int CacheMisses = 0;
        private static int SkippedByDistance = 0;

        private static bool ShouldLog => Logger.EnableFileLogging && LocalEnableLogging;

        public static void HandleCrashboxes(List<_3dObject> activeWorld)
        {
            int count = activeWorld.Count;
            bool shouldCheckStaticObjects = (DateTime.Now - _lastStaticCheck).TotalMilliseconds > 100;
            _skipParticles = !_skipParticles;

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
                    bool isInhabitantParticle = inhabitant.ObjectName == "Particle";
                    bool isOtherParticle = otherInhabitant.ObjectName == "Particle";
                    bool isParticle = isInhabitantParticle || isOtherParticle;
                    bool isBothParticles = isInhabitantParticle && isOtherParticle;
                    bool isShip = inhabitant.ObjectName == "Ship" || otherInhabitant.ObjectName == "Ship";

                    if (inhabitant.ObjectName == otherInhabitant.ObjectName) continue;
                    if (isInhabitantStatic && isOtherStatic) continue;
                    if ((isInhabitantStatic || isOtherStatic) && !shouldCheckStaticObjects) continue;
                    if (isParticle && isShip) continue;
                    if (isBothParticles) continue;
                    if (isParticle && _skipParticles) continue;

                    if (isInhabitantStatic || isOtherStatic) _lastStaticCheck = DateTime.Now;

                    var rotatedA = GetOrCacheRotatedBoxes(inhabitant);
                    var rotatedB = GetOrCacheRotatedBoxes(otherInhabitant);

                    var centerA = GetCenterOfBox(rotatedA[0]);
                    var centerB = GetCenterOfBox(rotatedB[0]);

                    double distance = _3dObjectHelpers.GetDistance(centerA, centerB);
                    if (ShouldLog)
                    {
                        Logger.Log($"[DISTANCE CHECK] {inhabitant.ObjectName} vs {otherInhabitant.ObjectName} = {distance:F2}");
                    }

                    if (distance > MaxCrashDistance)
                    {
                        SkippedByDistance++;
                        continue;
                    }

                    if (isParticle)
                    {
                        if (HandleParticleCollision(inhabitant, otherInhabitant))
                            continue;
                    }
                    else
                    {
                        if (HandleGeneralCollision(inhabitant, otherInhabitant))
                            continue;
                    }
                }
            }

            if (ShouldLog)
            {
                Logger.Log($"[CACHE] Hits: {CacheHits}, Misses: {CacheMisses}, Efficiency: {(CacheHits + CacheMisses == 0 ? 0 : (int)(100.0 * CacheHits / (CacheHits + CacheMisses)))}%");
                Logger.Log($"[DISTANCE SKIP] Skipped {SkippedByDistance} pairs due to distance > {MaxCrashDistance}");
            }
        }

        private static List<List<Vector3>> GetOrCacheRotatedBoxes(_3dObject obj)
        {
            if (RotatedBoxCache.TryGetValue(obj, out var cached))
            {
                CacheHits++;
                return cached.Select(box => box.Select(p => new Vector3 { x = p.x, y = p.y, z = p.z }).ToList()).ToList();
            }

            ObjectPlacementHelpers.TryGetCrashboxWorldPosition(obj, out var world);
            var rotated = RotateAllCrashboxes(obj.CrashBoxes, (Vector3)obj.Rotation, (Vector3)obj.ObjectOffsets, world, obj.ObjectName);
            RotatedBoxCache[obj] = rotated;
            CacheMisses++;
            return rotated.Select(box => box.Select(p => new Vector3 { x = p.x, y = p.y, z = p.z }).ToList()).ToList();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HandleParticleCollision(_3dObject a, _3dObject b)
        {
            var particle = a.ObjectName == "Particle" ? a : b;
            var other = particle == a ? b : a;

            foreach (var particleBox in particle.CrashBoxes)
            {
                var center = GetCenterOfBox(particleBox.Cast<Vector3>().ToList());

                foreach (var otherBox in other.CrashBoxes)
                {
                    var min = new Vector3(otherBox.Min(p => p.x), otherBox.Min(p => p.y), otherBox.Min(p => p.z));
                    var max = new Vector3(otherBox.Max(p => p.x), otherBox.Max(p => p.y), otherBox.Max(p => p.z));

                    if (center.x >= min.x && center.x <= max.x &&
                        center.y >= min.y && center.y <= max.y &&
                        center.z >= min.z && center.z <= max.z)
                    {
                        var direction = EstimateDirectionFromSurface(center, min, max);

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

                        if (ShouldLog)
                        {
                            Logger.Log($"[PARTICLE COLLISION] {particle.ObjectName} <-> {other.ObjectName} | Direction: {direction}");
                        }

                        return true;
                    }
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HandleGeneralCollision(_3dObject a, _3dObject b)
        {
            if (ShouldLog)
            {
                Logger.Log($"[CHECK] Trying GeneralCollision: {a.ObjectName} vs {b.ObjectName}");
            }

            var rotatedA = GetOrCacheRotatedBoxes(a);
            var rotatedB = GetOrCacheRotatedBoxes(b);

            foreach (var boxA in rotatedA)
            {
                foreach (var boxB in rotatedB)
                {
                    CenterCrashBoxIfSurfaceBased(a, boxA);
                    CenterCrashBoxIfSurfaceBased(b, boxB);

                    if (_3dObjectHelpers.CheckCollisionBoxVsBox(boxA, boxB))
                    {
                        a.ImpactStatus.HasCrashed = true;
                        b.ImpactStatus.HasCrashed = true;

                        var centerA = GetCenterOfBox(boxA);
                        var centerB = GetCenterOfBox(boxB);

                        a.ImpactStatus.ImpactDirection = EstimateDirection(centerA, centerB);
                        b.ImpactStatus.ImpactDirection = EstimateDirection(centerB, centerA);

                        if (ShouldLog)
                        {
                            Logger.Log($"[GENERAL COLLISION] {a.ObjectName} <-> {b.ObjectName}");
                        }

                        return true;
                    }
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3 GetCenterOfBox(List<Vector3> box)
        {
            return new Vector3
            {
                x = box.Average(p => p.x),
                y = box.Average(p => p.y),
                z = box.Average(p => p.z)
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ImpactDirection EstimateDirectionFromSurface(Vector3 point, Vector3 min, Vector3 max)
        {
            var center = new Vector3
            {
                x = (min.x + max.x) / 2,
                y = (min.y + max.y) / 2,
                z = (min.z + max.z) / 2
            };
            float dx = point.x - center.x;
            float dy = point.y - center.y;
            float dz = point.z - center.z;

            if (Math.Abs(dy) > Math.Abs(dx) && Math.Abs(dy) > Math.Abs(dz))
                return dy < 0 ? ImpactDirection.Top : ImpactDirection.Bottom;
            else if (Math.Abs(dx) > Math.Abs(dz))
                return dx > 0 ? ImpactDirection.Right : ImpactDirection.Left;
            else
                return ImpactDirection.Center;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ImpactDirection EstimateDirection(Vector3 from, Vector3 to)
        {
            float dx = from.x - to.x;
            float dy = from.y - to.y;
            float dz = from.z - to.z;

            if (Math.Abs(dy) > Math.Abs(dx) && Math.Abs(dy) > Math.Abs(dz))
                return dy < 0 ? ImpactDirection.Top : ImpactDirection.Bottom;
            else if (Math.Abs(dx) > Math.Abs(dz))
                return dx > 0 ? ImpactDirection.Right : ImpactDirection.Left;
            else
                return ImpactDirection.Center;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
