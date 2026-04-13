using System.Collections.Generic;

namespace Domain
{
    public interface I3dWorld
    {
        List<I3dObject> WorldInhabitants { get; set; }
        ISceneHandler SceneHandler { get; set; }
        IGameEventBus? EventBus { get; set; }
        bool IsPaused { get; set; }
    }
}
