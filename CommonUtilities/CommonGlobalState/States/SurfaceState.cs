using CommonUtilities.CommonSetup;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using static Domain._3dSpecificsImplementations;

namespace CommonUtilities.CommonGlobalState.States
{
    // This class holds global state information for the ship, expand as needed
    // No deep copy for this class, it's intended to be a singleton-like static holder of state
    public class SurfaceState
    {
        //Meta information for surface ecology, for AI behavior etc
        public ScreenEcoMeta[,] ScreenEcoMetas {get;set;} = new ScreenEcoMeta[MapSetup.globalMapSize, MapSetup.globalMapSize];
        public SurfaceData[,]? Global2DMap { get; set; } = new SurfaceData[MapSetup.globalMapSize, MapSetup.globalMapSize];
        public BitmapSource? GlobalMapBitmap { get; set; }
        public Vector3 GlobalMapPosition { get; set; } = new Vector3 { x = SurfaceSetup.DefaultMapPosition.x, y = SurfaceSetup.DefaultMapPosition.y, z = SurfaceSetup.DefaultMapPosition.z };
    }
}
