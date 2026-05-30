using CommonUtilities.CommonSetup;
using Domain;
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
            float maxRadius = 38000f)
        {
            var positions = new List<Vector3>(Math.Max(0, count));
            if (count <= 0)
                return positions;

            var random = new Random(seed);
            for (int i = 0; i < count; i++)
            {
                double angle = random.NextDouble() * Math.PI * 2.0;
                float radius = minRadius + ((float)random.NextDouble() * (maxRadius - minRadius));
                positions.Add(CreatePosition(center, radius * SurfaceSetup.WorldScale, angle));
            }

            return positions;
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
    }
}
