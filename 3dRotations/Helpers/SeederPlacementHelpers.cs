using _3dRotations.World.Objects;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonSetup;
using Domain;
using GameAiAndControls.Controls.SeederControls;
using System;
using System.Collections.Generic;
using static Domain._3dSpecificsImplementations;

namespace _3dRotations.Helpers
{
    public static class SeederPlacementHelpers
    {
        public static List<Vector3> CreateRingSeederPositions(
            int count,
            Vector3 center,
            int seed,
            int nearSeederCount = 4,
            float firstRingRadius = 8000f,
            float ringRadiusStep = 11500f,
            float radiusJitter = 1800f,
            float angleJitterDegrees = 12f)
        {
            var positions = new List<Vector3>(Math.Max(0, count));
            if (count <= 0)
                return positions;

            var random = new Random(seed);
            int nearCount = Math.Min(count, Math.Max(1, nearSeederCount));
            int farCount = count - nearCount;

            AddRing(positions, center, nearCount, firstRingRadius, random, radiusJitter, angleJitterDegrees, angleOffsetDegrees: -18f);

            const int farRingCapacity = 6;
            int placedFar = 0;
            int ringIndex = 1;
            while (placedFar < farCount)
            {
                int ringCount = Math.Min(farRingCapacity + ringIndex, farCount - placedFar);
                float radius = firstRingRadius + (ringRadiusStep * ringIndex);
                AddRing(positions, center, ringCount, radius, random, radiusJitter, angleJitterDegrees, angleOffsetDegrees: ringIndex * 23f);
                placedFar += ringCount;
                ringIndex++;
            }

            return positions;
        }

        public static List<Vector3> CreateRandomSeederPositions(
            int count,
            Vector3 center,
            int seed,
            float minRadius = 12000f,
            float maxRadius = 38000f,
            IReadOnlyCollection<Vector3>? avoidPositions = null,
            float minDistance = 3500f)
        {
            var positions = new List<Vector3>(Math.Max(0, count));
            if (count <= 0)
                return positions;

            var occupied = avoidPositions != null
                ? new List<Vector3>(avoidPositions)
                : new List<Vector3>();
            float scaledMinDistance = Math.Max(0f, minDistance) * SurfaceSetup.WorldScale;
            var random = new Random(seed);
            int attempts = 0;
            int maxAttempts = Math.Max(count * 80, 100);
            while (positions.Count < count && attempts < maxAttempts)
            {
                attempts++;
                double angle = random.NextDouble() * Math.PI * 2.0;
                float radius = minRadius + ((float)random.NextDouble() * (maxRadius - minRadius));
                var candidate = CreatePosition(center, radius * SurfaceSetup.WorldScale, angle);
                if (HasPositionWithinDistance(occupied, candidate, scaledMinDistance))
                    continue;

                positions.Add(candidate);
                occupied.Add(candidate);
            }

            int fallbackAttempt = 0;
            int fallbackSlotsPerRing = Math.Max(1, count);
            float fallbackRingStep = Math.Max(2500f, minDistance);
            while (positions.Count < count)
            {
                int fallbackRing = fallbackAttempt / fallbackSlotsPerRing;
                int fallbackSlot = fallbackAttempt % fallbackSlotsPerRing;
                float radius = (maxRadius + (fallbackRing * fallbackRingStep)) * SurfaceSetup.WorldScale;
                double angle = ((Math.PI * 2.0) / fallbackSlotsPerRing) * fallbackSlot + (seed * 0.017) + (fallbackRing * 0.37);
                var fallback = CreatePosition(center, radius, angle);
                fallbackAttempt++;
                if (HasPositionWithinDistance(occupied, fallback, scaledMinDistance))
                    continue;

                positions.Add(fallback);
                occupied.Add(fallback);
            }

            return positions;
        }

        public static List<Vector3> CreateCheckpointSeederPositions(
            int count,
            Vector3 center,
            int seed,
            float firstRingRadius = 7500f,
            float ringRadiusStep = 11500f,
            IReadOnlyCollection<Vector3>? avoidPositions = null,
            float minDistance = 4500f)
        {
            float minRadius = firstRingRadius + Math.Max(4500f, ringRadiusStep * 0.45f);
            float maxRadius = firstRingRadius + Math.Max(minRadius - firstRingRadius + 2500f, ringRadiusStep * 1.45f);

            return CreateRandomSeederPositions(
                count,
                center,
                seed,
                minRadius,
                maxRadius,
                avoidPositions,
                minDistance);
        }

        public static void AddSeederGroup(
            I3dWorld world,
            ISurface surface,
            Vector3 center,
            int regularCount,
            int powerUpCount,
            int regularSeed,
            int powerUpSeed,
            int nearSeederCount,
            float firstRingRadius = 7500f,
            float ringRadiusStep = 11500f,
            float powerUpMinRadius = 0f,
            float powerUpMaxRadius = 0f)
        {
            var regularPositions = CreateRingSeederPositions(
                regularCount,
                center,
                regularSeed,
                nearSeederCount,
                firstRingRadius,
                ringRadiusStep);

            foreach (var seederPosition in regularPositions)
            {
                AddSeeder(world, surface, seederPosition, hasPowerUp: false);
            }

            var powerUpPositions = powerUpMinRadius > 0f && powerUpMaxRadius > powerUpMinRadius
                ? CreateRandomSeederPositions(
                    powerUpCount,
                    center,
                    powerUpSeed,
                    powerUpMinRadius,
                    powerUpMaxRadius,
                    avoidPositions: regularPositions,
                    minDistance: 4500f)
                : CreateCheckpointSeederPositions(
                    powerUpCount,
                    center,
                    powerUpSeed,
                    firstRingRadius,
                    ringRadiusStep,
                    avoidPositions: regularPositions,
                    minDistance: 4500f);

            foreach (var seederPosition in powerUpPositions)
            {
                AddSeeder(world, surface, seederPosition, hasPowerUp: true);
            }
        }

        private static void AddRing(
            List<Vector3> positions,
            Vector3 center,
            int count,
            float radius,
            Random random,
            float radiusJitter,
            float angleJitterDegrees,
            float angleOffsetDegrees)
        {
            if (count <= 0)
                return;

            double angleOffset = angleOffsetDegrees * Math.PI / 180.0;
            double angleJitter = angleJitterDegrees * Math.PI / 180.0;

            for (int i = 0; i < count; i++)
            {
                double baseAngle = angleOffset + (Math.PI * 2.0 * i / count);
                double angle = baseAngle + ((random.NextDouble() * 2.0 - 1.0) * angleJitter);
                float jitteredRadius = radius + ((float)random.NextDouble() * 2f - 1f) * radiusJitter;
                positions.Add(CreatePosition(center, Math.Max(1000f, jitteredRadius) * SurfaceSetup.WorldScale, angle));
            }
        }

        private static Vector3 CreatePosition(Vector3 center, float radius, double angle)
        {
            return new Vector3
            {
                x = center.x + (MathF.Cos((float)angle) * radius),
                y = center.y,
                z = center.z + (MathF.Sin((float)angle) * radius)
            };
        }

        private static bool HasPositionWithinDistance(List<Vector3> positions, Vector3 candidate, float minDistance)
        {
            if (minDistance <= 0f)
                return false;

            float minDistanceSquared = minDistance * minDistance;
            foreach (var position in positions)
            {
                float dx = position.x - candidate.x;
                float dz = position.z - candidate.z;
                if ((dx * dx) + (dz * dz) < minDistanceSquared)
                    return true;
            }

            return false;
        }

        private static void AddSeeder(I3dWorld world, ISurface surface, Vector3 worldPosition, bool hasPowerUp)
        {
            var seeder = Seeder.CreateSeeder(surface);
            seeder.Rotation = new Vector3 { };
            seeder.WorldPosition = worldPosition;
            seeder.ObjectOffsets = new Vector3 { x = 0, y = -200, z = 600 };
            seeder.ObjectName = "Seeder";
            seeder.Movement = new SeederControls();
            seeder.CrashBoxDebugMode = false;
            seeder.ImpactStatus = new ImpactStatus { ObjectHealth = EnemySetup.SeederHealth };
            seeder.HasPowerUp = hasPowerUp;
            world.WorldInhabitants.Add(seeder);
            GameState.SurfaceState.AiObjects.Add(seeder);
        }
    }
}
