using Domain;

namespace CommonUtilities.GamePlayHelpers
{
    public readonly record struct TileRect(int MinX, int MinZ, int MaxX, int MaxZ)
    {
        public bool Contains(int x, int z)
        {
            return x >= MinX && x <= MaxX &&
                   z >= MinZ && z <= MaxZ;
        }

        public TileRect Expand(int buffer)
        {
            return new TileRect(MinX - buffer, MinZ - buffer, MaxX + buffer, MaxZ + buffer);
        }
    }

    public static class LandingPlatformHelpers
    {
        public const int LandingPlatformSizeTiles = 8;

        public static TileRect GetLandingPlatformRect(SurfaceData[,] map, int bufferTiles = 0)
        {
            int sizeZ = map.GetLength(0);
            int sizeX = map.GetLength(1);
            int centerX = sizeX / 2;
            int centerZ = sizeZ / 2;
            int half = LandingPlatformSizeTiles / 2;

            var rect = new TileRect(
                Math.Max(0, centerX - half),
                Math.Max(0, centerZ - half),
                Math.Min(sizeX - 1, centerX - half + LandingPlatformSizeTiles - 1),
                Math.Min(sizeZ - 1, centerZ - half + LandingPlatformSizeTiles - 1));

            return bufferTiles <= 0
                ? rect
                : new TileRect(
                    Math.Max(0, rect.MinX - bufferTiles),
                    Math.Max(0, rect.MinZ - bufferTiles),
                    Math.Min(sizeX - 1, rect.MaxX + bufferTiles),
                    Math.Min(sizeZ - 1, rect.MaxZ + bufferTiles));
        }

        public static (int x, int z) GetLandingPlatformCenterTile(SurfaceData[,] map)
        {
            return (map.GetLength(1) / 2, map.GetLength(0) / 2);
        }

        public static bool IsLandingPlatformTile(SurfaceData[,] map, int x, int z, int bufferTiles = 0)
        {
            return GetLandingPlatformRect(map, bufferTiles).Contains(x, z);
        }

        public static bool IsSurfaceBasedOnLandingPlatform(SurfaceData[,] map, int surfaceBasedId, int bufferTiles = 0)
        {
            if (surfaceBasedId <= 0)
                return false;

            var rect = GetLandingPlatformRect(map, bufferTiles);
            for (int z = rect.MinZ; z <= rect.MaxZ; z++)
            {
                for (int x = rect.MinX; x <= rect.MaxX; x++)
                {
                    if (map[z, x].mapId == surfaceBasedId)
                        return true;
                }
            }

            return false;
        }
    }
}
