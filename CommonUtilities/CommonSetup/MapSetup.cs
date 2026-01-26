using System;
using System.Collections.Generic;
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
    }
}
