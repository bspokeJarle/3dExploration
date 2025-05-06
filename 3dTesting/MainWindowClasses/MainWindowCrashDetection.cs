using System;
using System.Collections.Generic;
using System.Linq;
using _3dTesting._3dWorld;
using Domain;
using static Domain._3dSpecificsImplementations;
using System.Runtime.CompilerServices;
using System.Net.Security;
using _3dRotations.World.Objects;
using System.Windows;

namespace _3dTesting.Helpers
{
    public static class CrashDetection
    {
        public static DateTime staticOjectLastCheck { get; set; } = new DateTime();
        private static DateTime _lastStaticCheck = DateTime.MinValue;
        private static bool _skipParticles = false;

        public static bool LocalEnableLogging = true;
        public static double MaxCrashDistance = 750.0;

        private static readonly Dictionary<_3dObject, List<List<Vector3>>> RotatedBoxCache = new();
        private static int CacheHits = 0;
        private static int CacheMisses = 0;
        private static int SkippedByDistance = 0;
        private static int numFrame = 0;

        private static bool ShouldLog => Logger.EnableFileLogging && LocalEnableLogging;

        public static void HandleCrashboxes(List<_3dObject> activeWorld)
        {
            numFrame++;
            int count = activeWorld.Count;
            bool shouldCheckStaticObjects = (DateTime.Now - _lastStaticCheck).TotalMilliseconds > 200;
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

                    //Empty objectnames should not be accepted
                    if (string.IsNullOrEmpty(inhabitant.ObjectName) || string.IsNullOrEmpty(otherInhabitant.ObjectName)) continue;
                    if (inhabitant.ObjectName == otherInhabitant.ObjectName) continue;
                    if (isInhabitantStatic && isOtherStatic) continue;
                    if ((isInhabitantStatic || isOtherStatic) && !shouldCheckStaticObjects) continue;
                    if (isParticle && isShip) continue;
                    if (isBothParticles) continue;
                    if (isParticle && _skipParticles) continue;

                    if (isInhabitantStatic || isOtherStatic) _lastStaticCheck = DateTime.Now;

                    var centerA = GetCenterOfBox(
                        inhabitant.CrashBoxes
                        .SelectMany(cb => cb)
                        .Select(p => new Vector3 { x = p.x, y = p.y, z = p.z })
                        .ToList());

                    var centerB = GetCenterOfBox(
                        otherInhabitant.CrashBoxes
                        .SelectMany(cb => cb)
                        .Select(p => new Vector3 { x = p.x, y = p.y, z = p.z })
                        .ToList());

                    double distance = _3dObjectHelpers.GetDistance(centerA, centerB);
                    if (ShouldLog)
                    {
                        Logger.Log($"[DISTANCE CHECK] [FRAME:{numFrame}] {inhabitant.ObjectName} vs {otherInhabitant.ObjectName} = {distance:F2}");
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HandleParticleCollision(_3dObject a, _3dObject b)
        {
            if (ShouldLog)
            {
                  Logger.Log($"[CHECK] Trying ParticleCollision: {a.ObjectName} vs {b.ObjectName}");
            } 
            var particle  = a.ObjectName == "Particle" ? a : b;
            var other = particle == a ? b : a;
             
            foreach (var particleBox in particle.CrashBoxes)
            { 
                var safeBox = particleBox.Select(v => new Vector3 { x = v.x, y = v.y, z = v.z }).ToList();
                var center = GetCenterOfBox(safeBox);

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
                            particle.ImpactStatus.SourceParticle.ImpactStatus.ObjectName = a.ObjectName;
                        }

                        if (other.ImpactStatus != null)
                        {
                            other.ImpactStatus.HasCrashed = true;
                            other.ImpactStatus.ImpactDirection = direction;
                            other.ImpactStatus.ObjectName = b.ObjectName;
                        }

                        if (ShouldLog)
                        {
                            Logger.Log($"[PARTICLE COLLISION] {particle.ObjectName} <-> {other.ObjectName} | Direction: {direction}");
                            Logger.Log($"[PARTICLE BOX] {string.Join(", ", particleBox.Select(p => $"({p.x}, {p.y}, {p.z})"))}");
                            Logger.Log($"[OTHER BOX] {string.Join(", ", otherBox.Select(p => $"({p.x}, {p.y}, {p.z})"))}");
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
            foreach (var boxA in a.CrashBoxes)
            {
                foreach (var boxB in b.CrashBoxes)
                {
                    var safeBoxA = boxA.Select(v => new Vector3 { x = v.x, y = v.y, z = v.z }).ToList();
                    var safeBoxB = boxB.Select(v => new Vector3 { x = v.x, y = v.y, z = v.z }).ToList();
                 
                    ObjectPlacementHelpers.LogCrashboxAnalysis($"[FRAME:{numFrame}] [CrashBoxRef {boxA.GetHashCode()}] Crashbox Inhabitant:" + a.ObjectName, safeBoxA);
                    ObjectPlacementHelpers.LogCrashboxAnalysis($"[FRAME:{numFrame}] [CrashBoxRef {boxB.GetHashCode()}] Crashbox Other Inhabitant:" + b.ObjectName, safeBoxB);

                    if (_3dObjectHelpers.CheckCollisionBoxVsBox(safeBoxA,safeBoxB))
                    {
                        a.ImpactStatus.HasCrashed = true;
                        b.ImpactStatus.HasCrashed = true;
                        a.ImpactStatus.ObjectName = b.ObjectName;
                        b.ImpactStatus.ObjectName = a.ObjectName;

                        var centerA = GetCenterOfBox(safeBoxA);
                        var centerB = GetCenterOfBox(safeBoxB);

                        a.ImpactStatus.ImpactDirection = EstimateDirection(centerA, centerB);
                        b.ImpactStatus.ImpactDirection = EstimateDirection(centerB, centerA);

                        if (ShouldLog)
                        {
                            Logger.Log($"[FRAME:{numFrame}] [GENERAL COLLISION] {a.ObjectName} <-> {b.ObjectName}");
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetCenterOfBox(List<Vector3> points)
        {
            if (points == null || points.Count == 0)
                return new Vector3();

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            foreach (var p in points)
            {
                minX = Math.Min(minX, p.x);
                maxX = Math.Max(maxX, p.x);

                minY = Math.Min(minY, p.y);
                maxY = Math.Max(maxY, p.y);

                minZ = Math.Min(minZ, p.z);
                maxZ = Math.Max(maxZ, p.z);
            }

            return new Vector3
            {
                x = (minX + maxX) / 2f,
                y = (minY + maxY) / 2f,
                z = (minZ + maxZ) / 2f
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

        public static bool IsStatic(string objectName) =>
            objectName == "Tree" || objectName == "Surface" || objectName == "House";

    }
}
