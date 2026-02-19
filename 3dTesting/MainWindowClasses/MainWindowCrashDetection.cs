using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
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

        private static List<string> LogFilter = ["Lazer", "Surface"];

        public static bool LocalEnableLogging = false;
        // If true: only logs collisions (and details around the collision), NOT all checks/attempts/distance spam.
        public static bool LogOnlyCollisions = false;
        // If true: include extra details WHEN a collision happens (boxes/centers/direction).
        public static bool LogCollisionDetails = true;
        // If true: skip logging particles entirely, they fill the logs
        public static bool SkipParticleLogging = true;

        public static double MaxCrashDistance = 750.0;

        private static readonly Dictionary<_3dObject, List<List<Vector3>>> RotatedBoxCache = new();
        private static readonly Dictionary<_3dObject, Vector3> OffsetCache = new();
        private static readonly Dictionary<_3dObject, List<Vector3>> WorldPointsCache = new();
        private static readonly Dictionary<_3dObject, Vector3> CenterCache = new();
        private static readonly Dictionary<(_3dObject obj, int boxIndex), List<Vector3>> WorldBoxCache = new();
        private static readonly Dictionary<_3dObject, ObjectTypeFlags> TypeFlagCache = new();
        private static int _cacheFrame = -1;

        private static int CacheHits = 0;
        private static int CacheMisses = 0;
        private static int SkippedByDistance = 0;
        private static int numFrame = 0;

        private static bool ShouldLogAny => Logger.EnableFileLogging && LocalEnableLogging;

        private readonly struct ObjectTypeFlags
        {
            public readonly bool IsStatic;
            public readonly bool IsParticle;
            public readonly bool IsLazer;
            public readonly bool IsSeeder;
            public readonly bool IsShip;
            public readonly bool IsSurface;
            public readonly string Name;

            public ObjectTypeFlags(string name)
            {
                Name = name;
                IsStatic = IsStaticName(name);
                IsParticle = name == "Particle";
                IsLazer = name == "Lazer";
                IsSeeder = name == "Seeder";
                IsShip = name == "Ship";
                IsSurface = name == "Surface";
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ResetFrameCachesIfNeeded()
        {
            if (_cacheFrame == numFrame) return;

            _cacheFrame = numFrame;
            OffsetCache.Clear();
            WorldPointsCache.Clear();
            CenterCache.Clear();
            WorldBoxCache.Clear();
            TypeFlagCache.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ObjectTypeFlags GetTypeFlagsCached(_3dObject obj)
        {
            if (TypeFlagCache.TryGetValue(obj, out var flags))
            {
                CacheHits++;
                return flags;
            }

            CacheMisses++;
            var name = obj.ObjectName ?? string.Empty;
            flags = new ObjectTypeFlags(name);
            TypeFlagCache[obj] = flags;
            return flags;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsStaticName(string objectName) =>
            objectName == "Tree" || objectName == "Surface" || objectName == "House";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3 GetOffsetCached(_3dObject obj)
        {
            if (OffsetCache.TryGetValue(obj, out var offset))
            {
                CacheHits++;
                return offset;
            }

            CacheMisses++;
            offset = obj.GetCrashWorldOffset();
            OffsetCache[obj] = offset;
            return offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static List<Vector3> GetWorldPointsCached(_3dObject obj)
        {
            if (WorldPointsCache.TryGetValue(obj, out var points))
            {
                CacheHits++;
                return points;
            }

            CacheMisses++;
            var offset = GetOffsetCached(obj);
            points = obj.GetAllCrashPointsWorld(offset);
            WorldPointsCache[obj] = points;
            return points;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3 GetCenterCached(_3dObject obj)
        {
            if (CenterCache.TryGetValue(obj, out var center))
            {
                CacheHits++;
                return center;
            }

            CacheMisses++;
            var points = GetWorldPointsCached(obj);
            center = GetCenterOfBox(points);
            CenterCache[obj] = center;
            return center;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static List<Vector3> GetWorldBoxPointsCached(_3dObject obj, int boxIndex, List<IVector3> box)
        {
            var key = (obj, boxIndex);
            if (WorldBoxCache.TryGetValue(key, out var points))
            {
                CacheHits++;
                return points;
            }

            CacheMisses++;
            var offset = GetOffsetCached(obj);
            points = box.ToCrashWorldPoints(offset);
            WorldBoxCache[key] = points;
            return points;
        }

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
            ResetFrameCachesIfNeeded();

            int count = activeWorld.Count;
            bool shouldCheckStaticObjects = (DateTime.Now - _lastStaticCheck).TotalMilliseconds > 100;
            _skipParticles = !_skipParticles;

            // Clear previous frame best candidate list, needs to be frame-fresh and CrashDetection provides the information naturally
            CommonUtilities.CommonGlobalState.GameState.ShipState.BestCandidateStates.Clear();

            for (int i = 0; i < count; i++)
            {
                var inhabitant = activeWorld[i];
                if (inhabitant == null || inhabitant.CrashBoxes == null) continue;

                // We want the enemy whose direction is closest to perfect alignment (dot ≈ 1.0)
                var dotDistanceToPerfect = float.MaxValue;

                for (int j = i + 1; j < count; j++)
                {
                    var otherInhabitant = activeWorld[j];
                    if (otherInhabitant == null || otherInhabitant.CrashBoxes == null) continue;

                    var flagsA = GetTypeFlagsCached(inhabitant);
                    var flagsB = GetTypeFlagsCached(otherInhabitant);

                    bool isInhabitantStatic = flagsA.IsStatic;
                    bool isOtherStatic = flagsB.IsStatic;
                    bool isInhabitantParticle = flagsA.IsParticle;
                    bool isOtherParticle = flagsB.IsParticle;
                    bool isInhabitantLazer = flagsA.IsLazer;
                    bool isOtherLazer = flagsB.IsLazer;
                    bool isInhabitantSeeder = flagsA.IsSeeder;
                    bool isOtherSeeder = flagsB.IsSeeder;
                    bool isSeeder = isInhabitantSeeder || isOtherSeeder;
                    bool isParticle = isInhabitantParticle || isOtherParticle;
                    bool isLazer = isInhabitantLazer || isOtherLazer;
                    bool isBothParticles = isInhabitantParticle && isOtherParticle;
                    bool isShip = flagsA.IsShip || flagsB.IsShip;
                    bool isSurface = flagsA.IsSurface || flagsB.IsSurface;

                    // Empty objectnames should not be accepted
                    if (string.IsNullOrEmpty(flagsA.Name) || string.IsNullOrEmpty(flagsB.Name)) continue;
                    if (flagsA.Name == flagsB.Name) continue;
                    if (isInhabitantStatic && isOtherStatic) continue;
                    if ((isInhabitantStatic || isOtherStatic) && !shouldCheckStaticObjects) continue;
                    if (isParticle && isShip) continue;
                    if (isBothParticles) continue;
                    if (isParticle && _skipParticles) continue;
                    if (isLazer && isParticle || isLazer && isShip || isSeeder && isParticle) continue;

                    if (isInhabitantStatic || isOtherStatic) _lastStaticCheck = DateTime.Now;

                    if (isPaused)
                    {
                        if (CheckLogFilter(inhabitant, otherInhabitant))
                        {
                            LogSnapShots(inhabitant, otherInhabitant);
                        }
                    }

                    // Distance check should use the same effective coordinates as collision tests (cached)
                    var centerA = GetCenterCached(inhabitant);
                    var centerB = GetCenterCached(otherInhabitant);

                    double distance = _3dObjectHelpers.GetDistance(centerA, centerB);

                    if (CommonUtilities.CommonSetup.EnemySetup.IsEnemyTypeValid(inhabitant.ObjectName) && distance < MaxCrashDistance * 2)
                    {
                        CommonUtilities.CommonGlobalState.GameState.ShipState.BestCandidateStates.Add(new BestCandidateState
                        {
                            BestEnemyCandidate = new EnemyCandidateInfo
                            {
                                EnemyObject = inhabitant,
                                EnemyCenterPosition = centerA
                            },
                            TimeStampUtc = DateTime.UtcNow
                        });

                    }

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

                            Logger.Log($"[CHECKLAZER] {inhabitant.ObjectName} CrashBox: {inhabitantCrashText} and {otherInhabitant.ObjectName} LocalCrash: {otherCrashText}");
                        }
                    }

                    //This is mainly for Lazer to hit surface (Surface is big, center is many clicks away) it also logical that the Lazer may hit from further away
                    var effectiveMaxCrashDistance = isLazer ? MaxCrashDistance * 2 : MaxCrashDistance;
                    if (distance > effectiveMaxCrashDistance)
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

            for (int pb = 0; pb < particle.CrashBoxes.Count; pb++)
            {
                var particleBox = particle.CrashBoxes[pb];

                // World points for particle box (cached per-frame)
                var worldParticlePoints = GetWorldBoxPointsCached(particle, pb, particleBox);
                if (worldParticlePoints.Count == 0) continue;

                var center = GetCenterOfBox(worldParticlePoints);

                for (int ob = 0; ob < other.CrashBoxes.Count; ob++)
                {
                    var otherBox = other.CrashBoxes[ob];

                    // World points for other box (cached per-frame)
                    var worldOtherPoints = GetWorldBoxPointsCached(other, ob, otherBox);
                    if (worldOtherPoints.Count == 0) continue;

                    // Build other AABB in WORLD from worldOtherPoints
                    float oMinX = float.MaxValue, oMinY = float.MaxValue, oMinZ = float.MaxValue;
                    float oMaxX = float.MinValue, oMaxY = float.MinValue, oMaxZ = float.MinValue;

                    for (int i = 0; i < worldOtherPoints.Count; i++)
                    {
                        var p = worldOtherPoints[i];

                        float x = p.x;
                        float y = p.y;
                        float z = p.z;

                        if (x < oMinX) oMinX = x; if (x > oMaxX) oMaxX = x;
                        if (y < oMinY) oMinY = y; if (y > oMaxY) oMaxY = y;
                        if (z < oMinZ) oMinZ = z; if (z > oMaxZ) oMaxZ = z;
                    }

                    // Particle center inside other AABB
                    if (center.x >= oMinX && center.x <= oMaxX &&
                        center.y >= oMinY && center.y <= oMaxY &&
                        center.z >= oMinZ && center.z <= oMaxZ)
                    {
                        var min = new Vector3(oMinX, oMinY, oMinZ);
                        var max = new Vector3(oMaxX, oMaxY, oMaxZ);

                        var direction = EstimateDirectionFromSurface(center, min, max);

                        // Set impact flags
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
                        if (!SkipParticleLogging)
                        {
                            LogCollision(a, b,
                                $"[FRAME:{numFrame}] [PARTICLE COLLISION] {particle.ObjectName} <-> {other.ObjectName} | Dir:{direction} | ParticleBox:{pb} OtherBox:{ob}");
                        }

                        if (LogCollisionDetails)
                        {
                            if (!SkipParticleLogging) LogCollisionDetail(a, b,
                                $"[PARTICLE CENTER] ({center.x:0.##},{center.y:0.##},{center.z:0.##})");

                            if (!SkipParticleLogging) LogCollisionDetail(a, b,
                                $"[OTHER AABB] Min=({min.x:0.##},{min.y:0.##},{min.z:0.##}) Max=({max.x:0.##},{max.y:0.##},{max.z:0.##})");

                            if (!SkipParticleLogging)
                                LogCrashBoxWorldPoints($"[PARTICLE BOX WORLD] {particle.ObjectName} Box[{pb}]", worldParticlePoints);

                            if (!SkipParticleLogging)
                                LogCrashBoxWorldPoints($"[OTHER BOX WORLD] {other.ObjectName} Box[{ob}]", worldOtherPoints);
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
            for (int ai = 0; ai < a.CrashBoxes.Count; ai++)
            {
                var boxA = a.CrashBoxes[ai];

                // Build world points for A box (cached per-frame)
                var safeBoxA = GetWorldBoxPointsCached(a, ai, boxA);
                if (safeBoxA.Count == 0) continue;

                for (int bi = 0; bi < b.CrashBoxes.Count; bi++)
                {
                    var boxB = b.CrashBoxes[bi];

                    // Build world points for B box (cached per-frame)
                    var safeBoxB = GetWorldBoxPointsCached(b, bi, boxB);

                    if (ShouldLogAny && CheckLogFilter(a, b) && LogCollisionDetails)
                    {
                        Logger.Log($"PTS {a.ObjectName}[{ai}] vs {b.ObjectName}[{bi}] A:{string.Join(";", safeBoxA.Select(p => $"({p.x:0.#},{p.y:0.#},{p.z:0.#})"))} | B:{string.Join(";", safeBoxB.Select(p => $"({p.x:0.#},{p.y:0.#},{p.z:0.#})"))}");
                    }

                    if (safeBoxB.Count == 0) continue;

                    if (_3dObjectHelpers.CheckCollisionBoxVsBox(safeBoxA, safeBoxB, a.ObjectName, b.ObjectName))
                    {
                        a.ImpactStatus.HasCrashed = true;
                        b.ImpactStatus.HasCrashed = true;
                        a.ImpactStatus.ObjectName = b.ObjectName;
                        b.ImpactStatus.ObjectName = a.ObjectName;

                        var centerA = GetCenterOfBox(safeBoxA);
                        var centerB = GetCenterOfBox(safeBoxB);

                        a.ImpactStatus.ImpactDirection = EstimateDirection(centerA, centerB);
                        b.ImpactStatus.ImpactDirection = EstimateDirection(centerB, centerA);

                        LogCollision(a, b,
                            $"[FRAME:{numFrame}] [GENERAL COLLISION] {a.ObjectName} <-> {b.ObjectName} | ABox:{ai} BBox:{bi}");

                        if (LogCollisionDetails)
                        {
                            var offsetA = GetOffsetCached(a);
                            var offsetB = GetOffsetCached(b);

                            LogCollisionDetail(a, b,
                                $"[COLLISION OFFSETS] AEffective=({offsetA.x:0.##},{offsetA.y:0.##},{offsetA.z:0.##}) " +
                                $"BEffective=({offsetB.x:0.##},{offsetB.y:0.##},{offsetB.z:0.##})");

                            LogCollisionDetail(a, b,
                                $"[COLLISION CENTERS] A=({centerA.x:0.##},{centerA.y:0.##},{centerA.z:0.##}) " +
                                $"B=({centerB.x:0.##},{centerB.y:0.##},{centerB.z:0.##})");

                            LogCollisionDetail(a, b,
                                $"[COLLISION DIR] {a.ObjectName}->{b.ObjectName}:{a.ImpactStatus.ImpactDirection} | " +
                                $"{b.ObjectName}->{a.ObjectName}:{b.ImpactStatus.ImpactDirection}");

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

            if (GameState.SurfaceState.GlobalMapPosition != null)
                Logger.Log($"[SNAPSHOT] GlobalMapPosition: (x={GameState.SurfaceState.GlobalMapPosition.x:0.##}, z={GameState.SurfaceState.GlobalMapPosition.z:0.##})");

            // Keep this: WORLD/MAP offset (may be null for screen-locked objects)
            var calculated = obj.CalculatedWorldOffset ?? new Vector3(0, 0, 0);
            Logger.Log($"[SNAPSHOT] CalculatedWorldOffset: (x={calculated.x:0.##}, y={calculated.y:0.##}, z={calculated.z:0.##})");

            // NEW: this is what CrashDetection actually uses now
            var effectiveOffset = obj.GetCrashWorldOffset();
            Logger.Log($"[SNAPSHOT] EffectiveCrashOffset: (x={effectiveOffset.x:0.##}, y={effectiveOffset.y:0.##}, z={effectiveOffset.z:0.##})");

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
                Logger.Log($"[SNAPSHOT] CrashBox[{i}] LOCAL:");

                // Build a LOCAL list safely (no LINQ, supports weird list types)
                var localBox = ((System.Collections.IEnumerable)box).ToCrashWorldPoints(new Vector3(0, 0, 0));
                ObjectPlacementHelpers.LogCrashboxAnalysis(
                    $"[SNAPSHOT] [FRAME:{numFrame}] {role}:{obj.ObjectName} Box[{i}] LOCAL",
                    localBox
                );

                // --- WORLD/EFFECTIVE (what collision uses) ---
                var worldBox = ((System.Collections.IEnumerable)box).ToCrashWorldPoints(effectiveOffset);

                ObjectPlacementHelpers.LogCrashboxAnalysis(
                    $"[SNAPSHOT] [FRAME:{numFrame}] {role}:{obj.ObjectName} Box[{i}] WORLD (EffectiveCrashOffset)",
                    worldBox
                );

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
            IsStaticName(objectName);
    }
}
