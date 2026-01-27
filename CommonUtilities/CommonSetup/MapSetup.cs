using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonUtilities.CommonSetup
{
    //TODO: Expand this as needed, common map properties can go here
    public static class MapSetup
    {
        public const int globalMapSize = 2500 + (SurfaceSetup.surfaceWidth / tileSize);
        public const int tileSize = 75;
        public static int maxHeight = 75; //Height elevation for the map
        public const int bitmapMapCenterOffset = 2000; //Offset to center the bitmap on the map position
        public const int bitmapSize = 72; //Size of the bitmap on screen
    }
}
