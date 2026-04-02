using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonSetup;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting.Helpers
{
    public static partial class CrashDetection
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HandleParticleCollision(_3dObject a, _3dObject b)
        {
            var particle = a.ObjectName == "Particle" ? a : b;
            var other = particle == a ? b : a;

            for (int pb = 0; pb < particle.CrashBoxes.Count; pb++)
            {
                var particleBox = particle.CrashBoxes[pb];

                var worldParticlePoints = GetWorldBoxPointsCached(particle, pb, particleBox);
                if (worldParticlePoints.Count == 0) continue;

                var center = GetCenterOfBox(worldParticlePoints);

                for (int ob = 0; ob < other.CrashBoxes.Count; ob++)
                {
                    var otherBox = other.CrashBoxes[ob];

                    var worldOtherPoints = GetWorldBoxPointsCached(other, ob, otherBox);
                    if (worldOtherPoints.Count == 0) continue;

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
                            // Tell the source what it hit (the other object's name)
                            particle.ImpactStatus.SourceParticle.ImpactStatus.ObjectName = other.ObjectName;
                        }

                        if (other.ImpactStatus != null)
                        {
                            other.ImpactStatus.HasCrashed = true;
                            other.ImpactStatus.ImpactDirection = direction;
                            // Tell the other object what hit it (the weapon name stored on the particle)
                            other.ImpactStatus.ObjectName = particle.ImpactStatus?.ObjectName
                                                            ?? particle.ObjectName;
                        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HandleGeneralCollision(_3dObject a, _3dObject b)
        {
            for (int ai = 0; ai < a.CrashBoxes.Count; ai++)
            {
                var boxA = a.CrashBoxes[ai];

                var safeBoxA = GetWorldBoxPointsCached(a, ai, boxA);
                if (safeBoxA.Count == 0) continue;

                for (int bi = 0; bi < b.CrashBoxes.Count; bi++)
                {
                    var boxB = b.CrashBoxes[bi];

                    var safeBoxB = GetWorldBoxPointsCached(b, bi, boxB);

                    if (ShouldLogAny && !LogOnlyCollisions && CheckLogFilter(a, b) && LogCollisionDetails)
                    {
                        Logger.Log($"PTS {a.ObjectName}[{ai}] vs {b.ObjectName}[{bi}] A:{string.Join(";", safeBoxA.Select(p => $"({p.x:0.#},{p.y:0.#},{p.z:0.#})"))} | B:{string.Join(";", safeBoxB.Select(p => $"({p.x:0.#},{p.y:0.#},{p.z:0.#})"))}");
                    }

                    if (safeBoxB.Count == 0) continue;

                    if (_3dObjectHelpers.CheckCollisionBoxVsBox(safeBoxA, safeBoxB, a.ObjectName, b.ObjectName))
                    {
                        var centerA = GetCenterOfBox(safeBoxA);
                        var centerB = GetCenterOfBox(safeBoxB);
                        var centerDistance = (float)_3dObjectHelpers.GetDistance(centerA, centerB);
                        bool isKamikazeShipPair =
                            (a.ObjectName == "KamikazeDrone" && b.ObjectName == "Ship") ||
                            (a.ObjectName == "Ship" && b.ObjectName == "KamikazeDrone");

                        if (isKamikazeShipPair && centerDistance > GameSetup.MaxKamikazeShipCenterCollisionDistance)
                        {
                            if (LogSkippedCollisions)
                            {
                                LogCollisionDetail(a, b,
                                    $"[COLLISION SKIPPED] {a.ObjectName} <-> {b.ObjectName} | CenterDistance:{centerDistance:0.##} | Max:{GameSetup.MaxKamikazeShipCenterCollisionDistance:0.##}");
                            }
                            continue;
                        }

                        a.ImpactStatus.HasCrashed = true;
                        b.ImpactStatus.HasCrashed = true;
                        a.ImpactStatus.ObjectName = b.ObjectName;
                        b.ImpactStatus.ObjectName = a.ObjectName;

                        a.ImpactStatus.ImpactDirection = EstimateDirection(centerA, centerB);
                        b.ImpactStatus.ImpactDirection = EstimateDirection(centerB, centerA);

                        LogCollision(a, b,
                            $"[FRAME:{numFrame}] [GENERAL COLLISION] {a.ObjectName} <-> {b.ObjectName} | ABox:{ai} BBox:{bi} | CenterDistance:{centerDistance:0.##}");

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
                                $"[COLLISION DISTANCE] CenterToCenter={centerDistance:0.##}");

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

        private static void HandleDecoyBlastDamage(List<_3dObject> activeWorld)
        {
            float blastRadius = CommonUtilities.CommonSetup.GameSetup.DecoyBlastRadius;
            int count = activeWorld.Count;

            for (int i = 0; i < count; i++)
            {
                var candidate = activeWorld[i];
                if (candidate == null) continue;
                if (candidate.ObjectName != "DroneDecoy") continue;
                if (candidate.ImpactStatus?.HasExploded == true) continue;
                if (candidate.CrashBoxes != null && candidate.CrashBoxes.Count > 0) continue;
                if (candidate.ObjectParts == null || candidate.ObjectParts.Count == 0) continue;

                if (!_processedDecoyBlasts.Add(candidate.ObjectId)) continue;

                // Use WorldPosition for blast distance so objects with different
                // ObjectOffsets are compared in the same coordinate space.
                var blastCenter = candidate.WorldPosition as Vector3;
                if (blastCenter == null) continue;

                for (int j = 0; j < count; j++)
                {
                    if (j == i) continue;
                    var target = activeWorld[j];
                    if (target == null) continue;
                    if (target.CrashBoxes == null || target.CrashBoxes.Count == 0) continue;
                    if (target.ImpactStatus?.HasExploded == true) continue;
                    if (target.ImpactStatus?.HasCrashed == true) continue;

                    var flags = GetTypeFlagsCached(target);
                    if (flags.IsShip || flags.IsSurface || flags.IsParticle || flags.IsLazer || flags.IsStatic) continue;

                    var targetPos = target.WorldPosition as Vector3;
                    if (targetPos == null) continue;
                    float distance = (float)_3dObjectHelpers.GetDistance(blastCenter, targetPos);

                    if (distance <= blastRadius)
                    {
                        if (target.ImpactStatus != null)
                        {
                            target.ImpactStatus.HasCrashed = true;
                            target.ImpactStatus.ObjectName = "DroneDecoy";
                        }

                        LogCollision(candidate, target,
                            $"[FRAME:{numFrame}] [DECOY BLAST] {candidate.ObjectName} -> {target.ObjectName} | Distance:{distance:0.##} | BlastRadius:{blastRadius:0.##}");
                    }
                }
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
    }
}
