using System.Collections.Generic;
using static Domain._3dSpecificsImplementations;

namespace Domain
{
    public interface ISurface
    {
        Vector3 GlobalMapRotation { get; set; }
        int SurfaceWidth();
        int GlobalMapSize();
        int ViewPortSize();
        int TileSize();
        int MaxHeight();
        List<ITriangleMeshWithColor> RotatedSurfaceTriangles { get; set; }
        HashSet<long?> LandBasedIds { get; set; }
        I3dObject GetSurfaceViewPort();
        void Create2DMap(int? maxTrees, int? maxHouses, GameModes gameMode, string? recordedSurface);
    }
}
