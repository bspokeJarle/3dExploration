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
        public static int bitmapMapCenterOffsetX => bitmapSize * tileSize;
        public static int bitmapMapCenterOffsetY => (bitmapSize / 2) * tileSize;
        public static int screensPrMap => globalMapSize / SurfaceSetup.DefaultViewPortSize;
    }
}
