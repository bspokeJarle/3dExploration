using _3dTesting._Coordinates;
using _3dTesting.MainWindowClasses.Loops;
using Domain;
using System.Collections.Generic;

namespace _3dTesting.MainWindowClasses
{
    public class GameWorldManager
    {
        private readonly IGameLoop<_2dTriangleMesh> gameLoop;

        public GameWorldManager()
            : this(new LiveGameLoop())
        {
        }

        public GameWorldManager(IGameLoop<_2dTriangleMesh> gameLoop)
        {
            this.gameLoop = gameLoop;
        }

        public string DebugMessage
        {
            get => gameLoop.DebugMessage;
            set => gameLoop.DebugMessage = value;
        }

        public bool FadeOutWorld
        {
            get => gameLoop.FadeOutWorld;
            set => gameLoop.FadeOutWorld = value;
        }

        public bool FadeInWorld
        {
            get => gameLoop.FadeInWorld;
            set => gameLoop.FadeInWorld = value;
        }

        public I3dObject ShipCopy
        {
            get => gameLoop.ShipCopy;
            set => gameLoop.ShipCopy = value;
        }

        public I3dObject SurfaceCopy
        {
            get => gameLoop.SurfaceCopy;
            set => gameLoop.SurfaceCopy = value;
        }

        public List<_2dTriangleMesh> UpdateWorld(I3dWorld world, ref List<_2dTriangleMesh> projectedCoordinates, ref List<_2dTriangleMesh> crashBoxCoordinates)
        {
            return gameLoop.UpdateWorld(world, ref projectedCoordinates, ref crashBoxCoordinates);
        }

        public void FinalizeRecording()
        {
            gameLoop.FinalizeRecording();
        }
    }
}
