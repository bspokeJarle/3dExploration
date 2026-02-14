using CommonUtilities.CommonSetup;
using Domain;
using System.Windows.Media.Imaging;
using static Domain._3dSpecificsImplementations;

namespace CommonUtilities.CommonGlobalState.States
{
    // This class holds global state information for the ship, expand as needed
    // No deep copy for this class, it's intended to be a singleton-like static holder of state
    public class SurfaceState
    {
        //Meta information for surface ecology, for AI behavior etc
        public ScreenEcoMeta[,] ScreenEcoMetas {get;set;} = new ScreenEcoMeta[MapSetup.screensPrMap, MapSetup.screensPrMap];

        public SurfaceData[,]? Global2DMap { get; set; } = new SurfaceData[MapSetup.globalMapSize, MapSetup.globalMapSize];
        public BitmapSource? GlobalMapBitmap { get; set; }
        public Vector3 GlobalMapPosition { get; set; } = new Vector3 { x = SurfaceSetup.DefaultMapPosition.x, y = SurfaceSetup.DefaultMapPosition.y, z = SurfaceSetup.DefaultMapPosition.z };
        public List<_3dObject> AiObjects { get; set; } = new List<_3dObject>();
        public List<IVector3> DirtyTiles { get; set; } = new List<IVector3>();
        public _3dObject? SurfaceViewportObject { get; set; }
    }
}
