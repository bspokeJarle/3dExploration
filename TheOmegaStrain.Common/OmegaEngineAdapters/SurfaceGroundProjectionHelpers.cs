using RetroMesh.Engine;
using TheOmegaStrain.Domain;

namespace TheOmegaStrain.Common.OmegaEngineAdapters
{
    public static class SurfaceGroundProjectionHelpers
    {
        public const float DefaultShadowStaticOffsetX = GroundProjectionMath.DefaultShadowStaticOffsetX;
        public const float DefaultShadowStaticOffsetY = GroundProjectionMath.DefaultShadowStaticOffsetY;
        public const float DefaultShadowStaticOffsetZ = GroundProjectionMath.DefaultShadowStaticOffsetZ;
        public const float DefaultShadowBaseScale = GroundProjectionMath.DefaultShadowBaseScale;
        public const float DefaultShadowMinScale = GroundProjectionMath.DefaultShadowMinScale;
        public const float DefaultShadowAltitudeShrinkFactor = GroundProjectionMath.DefaultShadowAltitudeShrinkFactor;
        public const float DefaultShadowSlopeX = GroundProjectionMath.DefaultShadowSlopeX;
        public const float DefaultShadowSlopeY = GroundProjectionMath.DefaultShadowSlopeY;
        public const float DefaultShadowVertexStretchBoost = GroundProjectionMath.DefaultShadowVertexStretchBoost;

        public static bool TryGetSurfaceGroundPoint(
            IReadOnlyList<ITriangleMeshWithColorAndTexture>? rotatedTiles,
            float targetX,
            float targetZ,
            out float groundX,
            out float groundY,
            out float groundZ)
        {
            return GroundProjectionMath.TryGetSurfaceGroundPoint(
                rotatedTiles,
                targetX,
                targetZ,
                out groundX,
                out groundY,
                out groundZ);
        }

        public static bool TryGetFrontmostSurfaceGroundPoint(
            IReadOnlyList<ITriangleMeshWithColorAndTexture>? rotatedTiles,
            float targetX,
            out float groundX,
            out float groundY,
            out float groundZ)
        {
            return GroundProjectionMath.TryGetFrontmostSurfaceGroundPoint(
                rotatedTiles,
                targetX,
                out groundX,
                out groundY,
                out groundZ);
        }

        public static bool IsWithinSurfaceBounds(
            IReadOnlyList<ITriangleMeshWithColorAndTexture>? rotatedTiles,
            float targetX,
            float targetZ)
        {
            if (rotatedTiles == null || rotatedTiles.Count == 0)
                return false;

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minZ = float.MaxValue;
            float maxZ = float.MinValue;

            for (int i = 0; i < rotatedTiles.Count; i++)
            {
                AccumulateBounds(rotatedTiles[i].vert1);
                AccumulateBounds(rotatedTiles[i].vert2);
                AccumulateBounds(rotatedTiles[i].vert3);
            }

            return targetX >= minX && targetX <= maxX
                && targetZ >= minZ && targetZ <= maxZ;

            void AccumulateBounds(IVector3 vertex)
            {
                minX = MathF.Min(minX, vertex.x);
                maxX = MathF.Max(maxX, vertex.x);
                minZ = MathF.Min(minZ, vertex.z);
                maxZ = MathF.Max(maxZ, vertex.z);
            }
        }
    }
}
