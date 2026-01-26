using CommonUtilities.CommonSetup;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace CommonUtilities.CommonGlobalState.States
{
    // This class holds global state information for the ship, expand as needed
    // No deep copy for this class, it's intended to be a singleton-like static holder of state
    public class SurfaceState
    {
        public SurfaceData[,]? Global2DMap { get; set; } = new SurfaceData[MapSetup.globalMapSize, MapSetup.globalMapSize];
        public BitmapSource? GlobalMapBitmap { get; set; }
    }
}
