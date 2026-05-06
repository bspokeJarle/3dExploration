using CommonUtilities.CommonSetup;
using Domain;

namespace CommonUtilities.GamePlayHelpers
{
    public static class MapCoordinateHelpers
    {
        public static int WrapIndex(int index, int size)
        {
            if (size <= 0) return 0;

            int wrapped = index % size;
            return wrapped < 0 ? wrapped + size : wrapped;
        }

        public static int GetWrappedRelativeIndex(int index, int origin, int size)
        {
            return WrapIndex(index - origin, size);
        }

        public static int WorldToTileIndex(float worldCoordinate, int tileSize, int tileCount)
        {
            if (tileSize <= 0) return 0;

            int tileIndex = (int)MathF.Floor(worldCoordinate / tileSize);
            return WrapIndex(tileIndex, tileCount);
        }

        public static int WorldXToTileIndex(float worldX, SurfaceData[,] map)
        {
            return WorldToTileIndex(worldX, SurfaceSetup.tileSize, map.GetLength(1));
        }

        public static int WorldZToTileIndex(float worldZ, SurfaceData[,] map)
        {
            return WorldToTileIndex(worldZ, SurfaceSetup.tileSize, map.GetLength(0));
        }
    }
}
