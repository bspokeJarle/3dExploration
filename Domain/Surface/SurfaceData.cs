using System.Collections.Generic;

namespace Domain
{
    public struct SurfaceData
    {
        public int mapDepth;
        public int mapId;
        public bool hasLandbasedObject;
        public required bool isInfected;

        public CrashBoxData? crashBox;
        public struct CrashBoxData
        {
            public int width;
            public int height;
            public int boxDepth;
        }
    }

    public struct ScreenEcoMeta
    {
        public int BioTileCount;
        public int ScreenCount;
        public List<TileCoord> BioTiles;
    }

    public struct TileCoord
    {
        public int Y;
        public int X;
    }
}
