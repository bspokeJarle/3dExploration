using _3dTesting._Coordinates;
using Domain;
using System.Collections.Generic;
using World3d = _3dTesting._3dWorld._3dWorld;

namespace _3dTesting.MainWindowClasses.Loops
{
    public interface IGameLoop
    {
        string DebugMessage { get; set; }
        bool FadeOutWorld { get; set; }
        bool FadeInWorld { get; set; }
        I3dObject ShipCopy { get; set; }
        I3dObject SurfaceCopy { get; set; }

        List<_2dTriangleMesh> UpdateWorld(
            World3d world,
            ref List<_2dTriangleMesh> projectedCoordinates,
            ref List<_2dTriangleMesh> crashBoxCoordinates);
    }
}
