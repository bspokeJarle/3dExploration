using _3dTesting._Coordinates;
using Domain;
using System.Collections.Generic;

namespace _3dTesting.MainWindowClasses.Loops
{
    public class PlaybackGameLoop : IGameLoop<_2dTriangleMesh>
    {
        public string DebugMessage { get; set; } = string.Empty;
        public bool FadeOutWorld { get; set; }
        public bool FadeInWorld { get; set; }
        public I3dObject ShipCopy { get; set; }
        public I3dObject SurfaceCopy { get; set; }

        public List<_2dTriangleMesh> UpdateWorld(I3dWorld world, ref List<_2dTriangleMesh> projectedCoordinates, ref List<_2dTriangleMesh> crashBoxCoordinates)
        {
            return projectedCoordinates ?? [];
        }

        public void FinalizeRecording()
        {
        }
    }
}
