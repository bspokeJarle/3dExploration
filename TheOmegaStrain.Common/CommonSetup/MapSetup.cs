using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheOmegaStrain.Common.CommonSetup
{
    //TODO: Expand this as needed, common map properties can go here
    public static class MapSetup
    {
        public const int globalMapSize = 2500 + SurfaceSetup.DefaultViewPortSize;
        public static int tileSize => SurfaceSetup.tileSize;
        public static int maxHeight = 75; //Height elevation for the map
        public const int bitmapSize = 72; //Size of the bitmap on screen

        // GlobalMapPosition is the viewport corner: Surface.CenterViewportOnLandingPlatform sets it
        // to (shipTile * tileSize) - viewPortCenterOffsetX, using the SAME offset on both axes.
        // The ship is therefore at GlobalMapPosition + viewPortCenterOffsetX, and the crop must be
        // centred on that point, not on GlobalMapPosition itself.
        public static int viewPortCenterOffsetX => (SurfaceSetup.viewPortSize * tileSize) / 2;
        public static int bitmapMapCenterOffsetX => (bitmapSize * tileSize) - viewPortCenterOffsetX;
        public static int bitmapMapCenterOffsetY => ((bitmapSize / 2) * tileSize) - viewPortCenterOffsetX;
        public static int screensPrMap => globalMapSize / SurfaceSetup.DefaultViewPortSize;
    }
}
