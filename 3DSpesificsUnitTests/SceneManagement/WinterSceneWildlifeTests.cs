using _3dRotations.Scene.Scene7;
using _3dRotations.Scenes.SceneSimulation;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using CommonUtilities.Events;
using Domain;
using System.Linq;

namespace _3DSpesificsUnitTests.SceneManagement;

[TestClass]
public class WinterSceneWildlifeTests
{
    [TestInitialize]
    public void Setup()
    {
        GameState.GamePlayState = new GamePlayState();
        GameState.SurfaceState = new SurfaceState();
        GameState.ScreenOverlayState = new ScreenOverlayState();
        GameState.ObjectIdCounter = 0;
    }

    [TestMethod]
    public void Scene7_WinterScene_SpawnsPolarBearsAndSeals()
    {
        var scene = new Scene7();
        var world = new TestWorld();

        scene.SetupScene(world);

        Assert.AreEqual(Scene7.TargetPatrolPolarBearCount + 1,
            world.WorldInhabitants.Count(o => o.ObjectName == "PolarBear"),
            "Scene7 should spawn one guaranteed polar bear plus the configured patrol target.");
        Assert.IsTrue(world.WorldInhabitants.Any(o => o.ObjectName == "Seal"),
            "Scene7 is a winter scene and should use seals as water wildlife.");
        Assert.IsFalse(world.WorldInhabitants.Any(o => o.ObjectName == "JumpingFish"),
            "Winter scenes should not use the temperate/jungle fish wildlife pass.");
    }

    [TestMethod]
    public void SceneSimulation_WhenBiomeIsWinter_SpawnsPolarBearsAndSeals()
    {
        GameState.GamePlayState.SimulationRound = 1;
        var scene = new SceneSimulation();
        var world = new TestWorld();

        Assert.AreEqual(SceneBiomeTypes.Winter, scene.SceneBiome,
            "Simulation round 1 is expected to pick the deterministic winter biome for this regression.");

        scene.SetupScene(world);

        Assert.IsTrue(world.WorldInhabitants.Any(o => o.ObjectName == "PolarBear"),
            "Winter simulation scenes should include polar bears.");
        Assert.IsTrue(world.WorldInhabitants.Any(o => o.ObjectName == "Seal"),
            "Winter simulation scenes should include seals.");
        Assert.IsTrue(world.WorldInhabitants.Any(o => o.ObjectName == "SnowEmitter"),
            "Winter simulation scenes should include snow.");
        Assert.IsFalse(world.WorldInhabitants.Any(o => o.ObjectName == "JumpingFish"),
            "Winter simulation scenes should not use jumping fish.");
    }

    private sealed class TestWorld : I3dWorld
    {
        public List<I3dObject> WorldInhabitants { get; set; } = new();
        public ISceneHandler SceneHandler { get; set; } = null!;
        public IGameEventBus? EventBus { get; set; } = new GameEventBus();
        public bool IsPaused { get; set; }
    }
}
