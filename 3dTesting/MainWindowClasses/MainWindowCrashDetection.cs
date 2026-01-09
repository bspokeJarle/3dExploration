using CommonUtilities._3DHelpers;
using Domain;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting.Helpers
{
    public static class CrashDetection
    {
        public static DateTime staticOjectLastCheck { get; set; } = new DateTime();
        private static DateTime _lastStaticCheck = DateTime.MinValue;
        private static bool _skipParticles = false;

        private static List<string> LogFilter = ["Seeder", "Lazer", "Ship"];

        public static bool LocalEnableLogging = false;
        // If true: only logs collisions (and details around the collision), NOT all checks/attempts/distance spam.
        public static bool LogOnlyCollisions = true;
        // If true: include extra details WHEN a collision happens (boxes/centers/direction).
        public static bool LogCollisionDetails = true;
        // If true: skip logging particles entirely, they fill the logs
        public static bool SkipParticleLogging = true;

        public static double MaxCrashDistance = 750.0;

        private static readonly Dictionary<_3dObject, List<List<Vector3>>> RotatedBoxCache = new();
        private static int CacheHits = 0;
        private static int CacheMisses = 0;
        private static int SkippedByDistance = 0;
        private static int numFrame = 0;

        private static bool ShouldLogAny => Logger.EnableFileLogging && LocalEnableLogging;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ShouldLogPair(_3dObject a, _3dObject b)
        {
            if (!ShouldLogAny) return false;
            return CheckLogFilter(a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LogNonCollision(_3dObject a, _3dObject b, string message)
        {
            // Non-collision logs are suppressed when LogOnlyCollisions=true
            if (LogOnlyCollisions) return;
            if (!ShouldLogPair(a, b)) return;
            Logger.Log(message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LogCollision(_3dObject a, _3dObject b, string message)
        {
            // Collisions should be loggable regardless of LogOnlyCollisions
            if (!ShouldLogPair(a, b)) return;
            Logger.Log(message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LogCollisionDetail(_3dObject a, _3dObject b, string message)
        {
            if (!LogCollisionDetails) return;
            if (!ShouldLogPair(a, b)) return;
            Logger.Log(message);
        }

        public static void HandleCrashboxes(List<_3dObject> activeWorld, bool isPaused)
        {
            numFrame++;
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
                    bool isInhabitantLazer = inhabitant.ObjectName == "Lazer";
                    bool isOtherLazer = otherInhabitant.ObjectName == "Lazer";
                    bool isParticle = isInhabitantParticle || isOtherParticle;
                    bool isLazer = isInhabitantLazer || isOtherLazer;
                    bool isBothParticles = isInhabitantParticle && isOtherParticle;
                    bool isShip = inhabitant.ObjectName == "Ship" || otherInhabitant.ObjectName == "Ship";

                    // Empty objectnames should not be accepted
                    if (string.IsNullOrEmpty(inhabitant.ObjectName) || string.IsNullOrEmpty(otherInhabitant.ObjectName)) continue;
                    if (inhabitant.ObjectName == otherInhabitant.ObjectName) continue;
                    if (isInhabitantStatic && isOtherStatic) continue;
                    if ((isInhabitantStatic || isOtherStatic) && !shouldCheckStaticObjects) continue;
                    if (isParticle && isShip) continue;
                    if (isBothParticles) continue;
                    if (isParticle && _skipParticles) continue;
                    if (isLazer && isParticle || isLazer && isShip) continue;

                    if (isInhabitantStatic || isOtherStatic) _lastStaticCheck = DateTime.Now;

                    if (isPaused)
                    {
                        if (CheckLogFilter(inhabitant, otherInhabitant))
                        {
                            LogSnapShots(inhabitant, otherInhabitant);
                        }
                    }

                    // Ensure offsets exist (but do NOT rely on this for "correctness" if offsets are stale)
                    if (inhabitant.CalculatedWorldOffset == null)
                        inhabitant.CalculatedWorldOffset = new Vector3(0, 0, 0);
                    if (otherInhabitant.CalculatedWorldOffset == null)
                        otherInhabitant.CalculatedWorldOffset = new Vector3(0, 0, 0);

                    var centerA = GetCenterOfBox(
                        inhabitant.CrashBoxes
                        .SelectMany(cb => cb)
                        .Select(p => new Vector3
                        {
                            x = p.x + inhabitant.CalculatedWorldOffset.x,
                            y = p.y + inhabitant.CalculatedWorldOffset.y,
                            z = p.z + inhabitant.CalculatedWorldOffset.z
                        })
                        .ToList());

                    var centerB = GetCenterOfBox(
                        otherInhabitant.CrashBoxes
                        .SelectMany(cb => cb)
                        .Select(p => new Vector3
                        {
                            x = p.x + otherInhabitant.CalculatedWorldOffset.x,
                            y = p.y + otherInhabitant.CalculatedWorldOffset.y,
                            z = p.z + otherInhabitant.CalculatedWorldOffset.z
                        })
                        .ToList());

                    double distance = _3dObjectHelpers.GetDistance(centerA, centerB);

                    LogNonCollision(inhabitant, otherInhabitant,
                        $"[DISTANCE CHECK] [FRAME:{numFrame}] {inhabitant.ObjectName} vs {otherInhabitant.ObjectName} = {distance:F2}");

                    if (isLazer || isOtherLazer)
                    {
                        if (!LogOnlyCollisions && ShouldLogPair(inhabitant, otherInhabitant))
                        {
                            var inhabitantCrashText = string.Join(" | ",
                                inhabitant.CrashBoxes.Select((box, idx) =>
                                    $"Box{idx}: " + string.Join(", ", box.Select(v => $"({v.x:F2},{v.y:F2},{v.z:F2})"))));

                            var otherCrashText = string.Join(" | ",
                                otherInhabitant.CrashBoxes.Select((box, idx) =>
                                    $"Box{idx}: " + string.Join(", ", box.Select(v => $"({v.x:F2},{v.y:F2},{v.z:F2})"))));

                            Logger.Log($"[CHECKLAZER] {inhabitant.ObjectName} CrashBox: {inhabitantCrashText} and {otherInhabitant.ObjectName} WorldPos: {otherCrashText}");
                        }
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

            if (ShouldLogAny && !LogOnlyCollisions)
            {
                Logger.Log($"[CACHE] Hits: {CacheHits}, Misses: {CacheMisses}, Efficiency: {(CacheHits + CacheMisses == 0 ? 0 : (int)(100.0 * CacheHits / (CacheHits + CacheMisses)))}%");
                Logger.Log($"[DISTANCE SKIP] Skipped {SkippedByDistance} pairs due to distance > {MaxCrashDistance}");
            }
        }

        private static bool CheckLogFilter(I3dObject activOobject, I3dObject otherObject)
        {
            if (LogFilter.Count == 0) return true;
            if (LogFilter.Contains(activOobject.ObjectName) || LogFilter.Contains(otherObject.ObjectName)) return true;
            return false;
        }

        // -----------------------------
        // Particle collision (world offset + readable logging)
        // -----------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HandleParticleCollision(_3dObject a, _3dObject b)
        {
            var particle = a.ObjectName == "Particle" ? a : b;
            var other = particle == a ? b : a;

            var particleOffset = particle.CalculatedWorldOffset ?? new Vector3(0, 0, 0);
            var otherOffset = other.CalculatedWorldOffset ?? new Vector3(0, 0, 0);

            for (int pb = 0; pb < particle.CrashBoxes.Count; pb++)
            {
                var particleBox = particle.CrashBoxes[pb];

                // world points for particle box (so we can compute center cleanly and also log points)
                var worldParticlePoints = particleBox.Select(v => new Vector3
                {
                    x = v.x + particleOffset.x,
                    y = v.y + particleOffset.y,
                    z = v.z + particleOffset.z
                }).ToList();

                var center = GetCenterOfBox(worldParticlePoints);

                for (int ob = 0; ob < other.CrashBoxes.Count; ob++)
                {
                    var otherBox = other.CrashBoxes[ob];

                    // Other AABB in WORLD
                    float oMinX = float.MaxValue, oMinY = float.MaxValue, oMinZ = float.MaxValue;
                    float oMaxX = float.MinValue, oMaxY = float.MinValue, oMaxZ = float.MinValue;

                    for (int i = 0; i < otherBox.Count; i++)
                    {
                        var p = otherBox[i];
                        float x = p.x + otherOffset.x;
                        float y = p.y + otherOffset.y;
                        float z = p.z + otherOffset.z;

                        if (x < oMinX) oMinX = x; if (x > oMaxX) oMaxX = x;
                        if (y < oMinY) oMinY = y; if (y > oMaxY) oMaxY = y;
                        if (z < oMinZ) oMinZ = z; if (z > oMaxZ) oMaxZ = z;
                    }

                    if (center.x >= oMinX && center.x <= oMaxX &&
                        center.y >= oMinY && center.y <= oMaxY &&
                        center.z >= oMinZ && center.z <= oMaxZ)
                    {
                        var min = new Vector3(oMinX, oMinY, oMinZ);
                        var max = new Vector3(oMaxX, oMaxY, oMaxZ);

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

                        // Always log collision if logging is enabled (not depending on LogOnlyCollisions)
                        if (!SkipParticleLogging) LogCollision(a, b, $"[FRAME:{numFrame}] [PARTICLE COLLISION] {particle.ObjectName} <-> {other.ObjectName} | Dir:{direction} | ParticleBox:{pb} OtherBox:{ob}");

                        if (LogCollisionDetails)
                        {
                            if (!SkipParticleLogging) LogCollisionDetail(a, b, $"[COLLISION OFFSETS] ParticleOffset=({particleOffset.x:0.##},{particleOffset.y:0.##},{particleOffset.z:0.##}) OtherOffset=({otherOffset.x:0.##},{otherOffset.y:0.##},{otherOffset.z:0.##})");
                            if (!SkipParticleLogging) LogCollisionDetail(a, b, $"[PARTICLE CENTER] ({center.x:0.##},{center.y:0.##},{center.z:0.##})");
                            if (!SkipParticleLogging) LogCollisionDetail(a, b, $"[OTHER AABB] Min=({min.x:0.##},{min.y:0.##},{min.z:0.##}) Max=({max.x:0.##},{max.y:0.##},{max.z:0.##})");

                            // Log particle box (world)
                            if (!SkipParticleLogging) LogCrashBoxWorldPoints($"[PARTICLE BOX WORLD] {particle.ObjectName} Box[{pb}]", worldParticlePoints);

                            // Log other box (world)
                            var worldOtherPoints = otherBox.Select(v => new Vector3
                            {
                                x = v.x + otherOffset.x,
                                y = v.y + otherOffset.y,
                                z = v.z + otherOffset.z
                            }).ToList();

                            if (!SkipParticleLogging) LogCrashBoxWorldPoints($"[OTHER BOX WORLD] {other.ObjectName} Box[{ob}]", worldOtherPoints);
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        // -----------------------------
        // General collision (NO early overlap test, world offset + readable logging)
        // -----------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HandleGeneralCollision(_3dObject a, _3dObject b)
        {
            var offsetA = a.CalculatedWorldOffset ?? new Vector3(0, 0, 0);
            var offsetB = b.CalculatedWorldOffset ?? new Vector3(0, 0, 0);

            for (int ai = 0; ai < a.CrashBoxes.Count; ai++)
            {
                var boxA = a.CrashBoxes[ai];

                for (int bi = 0; bi < b.CrashBoxes.Count; bi++)
                {
                    var boxB = b.CrashBoxes[bi];

                    // Build world-space boxes (as before, but with CalculatedWorldOffset)
                    var safeBoxA = boxA.Select(v => new Vector3
                    {
                        x = v.x + offsetA.x,
                        y = v.y + offsetA.y,
                        z = v.z + offsetA.z
                    }).ToList();

                    var safeBoxB = boxB.Select(v => new Vector3
                    {
                        x = v.x + offsetB.x,
                        y = v.y + offsetB.y,
                        z = v.z + offsetB.z
                    }).ToList();

                    if (_3dObjectHelpers.CheckCollisionBoxVsBox(safeBoxA, safeBoxB))
                    {
                        a.ImpactStatus.HasCrashed = true;
                        b.ImpactStatus.HasCrashed = true;
                        a.ImpactStatus.ObjectName = b.ObjectName;
                        b.ImpactStatus.ObjectName = a.ObjectName;

                        var centerA = GetCenterOfBox(safeBoxA);
                        var centerB = GetCenterOfBox(safeBoxB);

                        a.ImpactStatus.ImpactDirection = EstimateDirection(centerA, centerB);
                        b.ImpactStatus.ImpactDirection = EstimateDirection(centerB, centerA);

                        LogCollision(a, b, $"[FRAME:{numFrame}] [GENERAL COLLISION] {a.ObjectName} <-> {b.ObjectName} | ABox:{ai} BBox:{bi}");

                        if (LogCollisionDetails)
                        {
                            LogCollisionDetail(a, b, $"[COLLISION OFFSETS] AOffset=({offsetA.x:0.##},{offsetA.y:0.##},{offsetA.z:0.##}) BOffset=({offsetB.x:0.##},{offsetB.y:0.##},{offsetB.z:0.##})");
                            LogCollisionDetail(a, b, $"[COLLISION CENTERS] A=({centerA.x:0.##},{centerA.y:0.##},{centerA.z:0.##}) B=({centerB.x:0.##},{centerB.y:0.##},{centerB.z:0.##})");
                            LogCollisionDetail(a, b, $"[COLLISION DIR] {a.ObjectName}->{b.ObjectName}:{a.ImpactStatus.ImpactDirection} | {b.ObjectName}->{a.ObjectName}:{b.ImpactStatus.ImpactDirection}");

                            LogCrashBoxWorldPoints($"[A BOX WORLD] {a.ObjectName} Box[{ai}]", safeBoxA);
                            LogCrashBoxWorldPoints($"[B BOX WORLD] {b.ObjectName} Box[{bi}]", safeBoxB);
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        // -----------------------------
        // Crashbox logging helpers (overview + points)
        // -----------------------------
        private static void LogCrashBoxWorldPoints(string title, List<Vector3> points)
        {
            if (!ShouldLogAny || points == null || points.Count == 0) return;

            // AABB summary
            float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;

            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                if (p.x < minX) minX = p.x; if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y; if (p.y > maxY) maxY = p.y;
                if (p.z < minZ) minZ = p.z; if (p.z > maxZ) maxZ = p.z;
            }

            var center = new Vector3(
                (minX + maxX) / 2f,
                (minY + maxY) / 2f,
                (minZ + maxZ) / 2f
            );

            static string F(float v) =>
                v.ToString("0.##", CultureInfo.InvariantCulture);

            Logger.Log(title);
            Logger.Log(
                $"  AABB Min=({F(minX)},{F(minY)},{F(minZ)}) " +
                $"Max=({F(maxX)},{F(maxY)},{F(maxZ)}) " +
                $"Center=({F(center.x)},{F(center.y)},{F(center.z)})"
            );

            // Points
            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                Logger.Log($"  P{i}: ({F(p.x)},{F(p.y)},{F(p.z)})");
            }
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

        public static void LogSnapShots(_3dObject inhabitant, _3dObject otherInhabitant)
        {
            if (inhabitant == null || otherInhabitant == null)
            {
                Logger.Log("[SNAPSHOT] One or both objects are null.");
                return;
            }

            LogObject("Inhabitant", inhabitant);
            LogObject("OtherInhabitant", otherInhabitant);
            //Make sure to flush immediately
            Logger.Flush();
        }

        private static void LogObject(string role, _3dObject obj)
        {
            Logger.Log($"[SNAPSHOT] --- {role}: {obj.ObjectName} ---");

            // Basic positional info (only the essentials)
            if (obj.ObjectOffsets != null)
                Logger.Log($"[SNAPSHOT] ObjectOffsets: (x={obj.ObjectOffsets.x:0.##}, y={obj.ObjectOffsets.y:0.##}, z={obj.ObjectOffsets.z:0.##})");

            if (obj.ParentSurface?.GlobalMapPosition != null)
                Logger.Log($"[SNAPSHOT] GlobalMapPosition: (x={obj.ParentSurface.GlobalMapPosition.x:0.##}, z={obj.ParentSurface.GlobalMapPosition.z:0.##})");

            // This is the key missing piece for debugging
            var offset = obj.CalculatedWorldOffset ?? new Vector3(0, 0, 0);
            Logger.Log($"[SNAPSHOT] CalculatedWorldOffset: (x={offset.x:0.##}, y={offset.y:0.##}, z={offset.z:0.##})");

            var crashBoxes = obj.CrashBoxes;
            if (crashBoxes == null || crashBoxes.Count == 0)
            {
                Logger.Log("[SNAPSHOT] CrashBoxes: <none>");
                return;
            }

            Logger.Log($"[SNAPSHOT] CrashBoxes count: {crashBoxes.Count}");

            for (int i = 0; i < crashBoxes.Count; i++)
            {
                var box = crashBoxes[i];
                if (box == null)
                {
                    Logger.Log($"[SNAPSHOT] CrashBox[{i}]: <null>");
                    continue;
                }

                // --- LOCAL (as stored) ---
                // This is what you currently log today. Keep it, but label it clearly.
                Logger.Log($"[SNAPSHOT] CrashBox[{i}] LOCAL:");
                ObjectPlacementHelpers.LogCrashboxAnalysis($"[SNAPSHOT] [FRAME:{numFrame}] {role}:{obj.ObjectName} Box[{i}] LOCAL",
                    box.Select(v => new Vector3 { x = v.x, y = v.y, z = v.z }).ToList()
                );

                // --- WORLD/EFFECTIVE (what collision uses) ---
                // This is what you actually need for debugging offsets.
                var worldBox = box.Select(v => new Vector3
                {
                    x = v.x + offset.x,
                    y = v.y + offset.y,
                    z = v.z + offset.z
                }).ToList();

                ObjectPlacementHelpers.LogCrashboxAnalysis($"[SNAPSHOT] [FRAME:{numFrame}] {role}:{obj.ObjectName} Box[{i}] WORLD (LOCAL+Offset)", worldBox);

                // Optional: quick center line (uses your existing GetCenterOfBox)
                var center = GetCenterOfBox(worldBox);
                Logger.Log($"[SNAPSHOT] CrashBox[{i}] WORLD Center: (x={center.x:0.##}, y={center.y:0.##}, z={center.z:0.##})");
            }
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
