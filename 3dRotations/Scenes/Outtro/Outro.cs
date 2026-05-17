using _3dRotations.World.Objects.EarthObject;
using CommonUtilities.CommonGlobalState;
using Domain;
using GameAiAndControls.Controls;

namespace _3dRotations.Scenes.Outro
{
    public class Outro : IScene
    {
        public GameModes GameMode { get; } = GameModes.Live;
        public string SceneMusic { get; } = "music_outro";
        public SceneTypes SceneType { get; } = SceneTypes.Outro;
        public SceneBiomeTypes SceneBiome { get; } = SceneBiomeTypes.HillsWoods;

        public void SetupScene(I3dWorld world)
        {
            var earth = EarthObject.CreateEarth();
            earth.Movement = new EarthObjectControls();
            world.WorldInhabitants.Add(earth);
        }

        public void SetupSceneOverlay()
        {
            GameState.ScreenOverlayState.ResetToDefaults();
            var o = GameState.ScreenOverlayState;
            o.Type = ScreenOverlayType.Outro;
            o.ShowOverlay = false;
            o.AutoHide = false;
            o.ShowDebugOverlay = false;
        }

        public void SetupGameOverlay() { }

        public void SetupVideoOverlay(string fileName) { }
    }
}
