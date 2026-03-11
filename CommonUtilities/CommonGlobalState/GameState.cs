using CommonUtilities.CommonGlobalState.States;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CommonUtilities.CommonGlobalState
{
    // This class holds global state information for the game, expand as needed
    // No deep copy for this class, it's intended to be a singleton-like static holder of state
    public static class GameState
    {
        public static ShipState ShipState = new ShipState();
        public static SurfaceState SurfaceState = new SurfaceState();
        public static ScreenOverlayState ScreenOverlayState = new ScreenOverlayState();
        public static GamePlayState GamePlayState = new GamePlayState();
        public static long FrameCount { get; set; } = 0;
        public static int ObjectIdCounter { get; set; } = 0;
    }
}
