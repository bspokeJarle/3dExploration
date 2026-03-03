using System.Collections.Generic;

namespace Domain
{
    public interface IGameLoop<TTriangle>
    {
        string DebugMessage { get; set; }
        bool FadeOutWorld { get; set; }
        bool FadeInWorld { get; set; }
        I3dObject ShipCopy { get; set; }
        I3dObject SurfaceCopy { get; set; }

        List<TTriangle> UpdateWorld(
            I3dWorld world,
            ref List<TTriangle> projectedCoordinates,
            ref List<TTriangle> crashBoxCoordinates);

        void FinalizeRecording();
    }
}
