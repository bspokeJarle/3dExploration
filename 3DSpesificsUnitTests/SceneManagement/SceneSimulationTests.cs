using _3DWorld.Scene;
using _3dRotations.Scenes.SceneSimulation;
using _3dTesting._3dWorld;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using CommonUtilities.Events;
using CommonUtilities.Persistence;
using Domain;
using System.Reflection;

namespace _3DSpesificsUnitTests.SceneManagement;

[TestClass]
public class SceneSimulationTests
{
    private string _originalLocalFolder = "";
    private string _testLocalFolder = "";

    [TestInitialize]
    public void Setup()
    {
        _originalLocalFolder = PersistenceSetup.LocalFolder;
        _testLocalFolder = Path.Combine(Path.GetTempPath(), "OmegaStrainSimulationTests", Guid.NewGuid().ToString("N"));
        PersistenceSetup.LocalFolder = _testLocalFolder;
        PersistenceSetup.Initialize();

        GameState.GamePlayState = new GamePlayState { SceneIndex = 0 };
        GameState.SurfaceState = new SurfaceState();
        GameState.ScreenOverlayState = new ScreenOverlayState();
        GameState.WeatherVisualState = new WeatherVisualState();
        GameState.WorldFade = new WorldFadeState();
        GameState.ObjectIdCounter = 0;
        GameState.DeltaTime = 0f;
    }

    [TestCleanup]
    public void Cleanup()
    {
        PersistenceSetup.LocalFolder = _originalLocalFolder;

        try
        {
            if (Directory.Exists(_testLocalFolder))
                Directory.Delete(_testLocalFolder, recursive: true);
        }
        catch
        {
        }
    }

    [TestMethod]
    public void SceneSimulation_BiomeAndMusicCycleByRound()
    {
        AssertSimulationRound(0, SceneBiomeTypes.HillsWoods, "music_flight");
        AssertSimulationRound(1, SceneBiomeTypes.Winter, "music_battle");
        AssertSimulationRound(2, SceneBiomeTypes.Rainforrest, "music_kanpai");
        AssertSimulationRound(3, SceneBiomeTypes.Desert, "music_dontstop");
        AssertSimulationRound(4, SceneBiomeTypes.HillsWoods, "music_flight");
    }

    [TestMethod]
    public void SceneSimulation_WinterBiome_UsesWinterObjects()
    {
        var scene = CreateSimulationForBiome(SceneBiomeTypes.Winter);
        var world = new TestWorld();

        scene.SetupScene(world);

        Assert.IsTrue(world.WorldInhabitants.Any(o => o.ObjectName == "SnowTower"),
            "Winter simulation should use snow towers instead of regular towers.");
        Assert.IsTrue(world.WorldInhabitants.Any(o => o.ObjectName == "SmallIgloo" || o.ObjectName == "LargeIgloo"),
            "Winter simulation should place igloos.");
        Assert.IsTrue(world.WorldInhabitants.Any(o => o.ObjectName == "Tree"),
            "Winter simulation may keep trees/firs as biome-appropriate landmarks.");
        Assert.IsTrue(world.WorldInhabitants.Any(o => o.ObjectName == "PolarBear"));
        Assert.IsTrue(world.WorldInhabitants.Any(o => o.ObjectName == "Seal"));
        Assert.IsTrue(world.WorldInhabitants.Any(o => o.ObjectName == "SnowEmitter"));
        Assert.IsFalse(world.WorldInhabitants.Any(o => o.ObjectName == "Tower"),
            "Winter simulation should not use regular towers.");
        Assert.IsFalse(world.WorldInhabitants.Any(o => o.ObjectName == "House"),
            "Winter simulation should not use temperate houses.");
        Assert.IsFalse(world.WorldInhabitants.Any(o => o.ObjectName == "JumpingFish"),
            "Winter simulation should use seals instead of jumping fish.");
    }

    [TestMethod]
    public void SceneSimulation_RainforestBiome_UsesRainforestObjects()
    {
        var scene = CreateSimulationForBiome(SceneBiomeTypes.Rainforrest);
        var world = new TestWorld();

        scene.SetupScene(world);

        Assert.IsTrue(world.WorldInhabitants.Any(o => o.ObjectName == "RainEmitter"));
        Assert.IsTrue(world.WorldInhabitants.Any(o => o.ObjectName == "LightningEmitter"));
        Assert.IsTrue(world.WorldInhabitants.Any(o =>
            o.ObjectName == "LargePalm" ||
            o.ObjectName == "SmallPalm" ||
            o.ObjectName == "LargeAlienPlant" ||
            o.ObjectName == "SmallAlienPlant"),
            "Rainforest simulation should use rainforest vegetation.");
        Assert.IsTrue(world.WorldInhabitants.Any(o => o.ObjectName == "BambooHut"),
            "Rainforest simulation should use bamboo huts.");
        Assert.IsFalse(world.WorldInhabitants.Any(o => o.ObjectName == "SnowEmitter"));
        Assert.IsFalse(world.WorldInhabitants.Any(o => o.ObjectName == "SnowTower"));
        Assert.IsFalse(world.WorldInhabitants.Any(o => o.ObjectName == "House"),
            "Rainforest simulation should not use temperate houses.");
    }

    [TestMethod]
    public void SceneSimulation_DifficultyScalesWithRound()
    {
        var early = CreateSimulationForRound(0);
        var late = CreateSimulationForRound(5);

        Assert.IsTrue(GetPrivateInt(late, "_seeders") > GetPrivateInt(early, "_seeders"));
        Assert.IsTrue(GetPrivateInt(late, "_drones") > GetPrivateInt(early, "_drones"));
        Assert.IsTrue(GetPrivateInt(late, "_bombers") > GetPrivateInt(early, "_bombers"));
        Assert.IsTrue(GetPrivateFloat(late, "_infectionThreshold") < GetPrivateFloat(early, "_infectionThreshold"));
        Assert.IsTrue(GetPrivateFloat(late, "_motherShipAggression") > GetPrivateFloat(early, "_motherShipAggression"));
    }

    [TestMethod]
    public void GameStatePersistence_SavesSimulationBiomeAndEnemyCounts()
    {
        var gps = GameState.GamePlayState;
        gps.PlayerName = "Pilot";
        gps.SceneIndex = 10;
        gps.SimulationRound = 2;
        gps.CurrentSceneBiome = SceneBiomeTypes.Rainforrest;
        gps.SeedersRemaining = 9;
        gps.DronesRemaining = 4;
        gps.MotherShipsRemaining = 1;
        gps.InitialSeeders = 14;
        gps.InitialDrones = 16;
        gps.InitialMotherShips = 1;

        GameStatePersistence.SaveGameState();

        var saved = GameStatePersistence.LoadGameState("Pilot");
        Assert.IsNotNull(saved);
        Assert.AreEqual(10, saved!.SceneIndex);
        Assert.AreEqual(2, saved.SimulationRound);
        Assert.AreEqual(SceneBiomeTypes.Rainforrest, saved.SceneBiome);
        Assert.AreEqual(9, saved.SeedersRemaining);
        Assert.AreEqual(4, saved.DronesRemaining);
        Assert.AreEqual(1, saved.MotherShipsRemaining);
        Assert.AreEqual(14, saved.InitialSeeders);
        Assert.AreEqual(16, saved.InitialDrones);

        GameState.GamePlayState = new GamePlayState();
        GameStatePersistence.RestoreToGamePlayState(saved);

        Assert.AreEqual(SceneBiomeTypes.Rainforrest, GameState.GamePlayState.CurrentSceneBiome);
        Assert.AreEqual(2, GameState.GamePlayState.SimulationRound);
        Assert.AreEqual(9, GameState.GamePlayState.SeedersRemaining);
    }

    [TestMethod]
    public void SceneHandler_CanTargetSavedSimulationScene()
    {
        var handler = new SceneHandler();
        var saved = new SavedGameState
        {
            PlayerName = "Pilot",
            SceneIndex = 10,
            SimulationRound = 2,
            SceneBiome = SceneBiomeTypes.Rainforrest
        };

        var method = typeof(SceneHandler).GetMethod("CanTargetSavedScene", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);

        var result = (bool)method!.Invoke(handler, new object[] { saved })!;
        Assert.IsTrue(result, "Saved simulation progress should target the simulation scene on login.");
    }

    [TestMethod]
    public void SceneHandler_LoadedSimulationSave_RebuildsSavedRound()
    {
        var handler = new SceneHandler();
        var world = new _3dWorld();
        world.SceneHandler = handler;
        world.WorldInhabitants.Clear();

        var saved = new SavedGameState
        {
            PlayerName = "Pilot",
            SceneIndex = 10,
            SimulationRound = 2,
            SceneBiome = SceneBiomeTypes.Rainforrest,
            Score = 45000,
            TotalKills = 77,
            TotalShotsFired = 300,
            PowerUpsCollected = 2
        };

        SetPrivateField(handler, "_pendingSavedState", saved);
        SetPrivateField(handler, "_targetSceneIndex", 10);
        SetPrivateField(handler, "_pendingSceneAdvance", true);
        SetPrivateField(handler, "_pendingSceneAdvanceFramesLeft", 0);

        handler.UpdateFrame(world);

        Assert.AreEqual(SceneTypes.Simulation, handler.GetActiveScene().SceneType);
        Assert.AreEqual(SceneBiomeTypes.Rainforrest, handler.GetActiveScene().SceneBiome);
        Assert.AreEqual(10, GameState.GamePlayState.SceneIndex);
        Assert.AreEqual(2, GameState.GamePlayState.SimulationRound);
        Assert.AreEqual(SceneBiomeTypes.Rainforrest, GameState.GamePlayState.CurrentSceneBiome);
        Assert.AreEqual(45000, GameState.GamePlayState.Score);
        Assert.IsTrue(world.WorldInhabitants.Any(o => o.ObjectName == "RainEmitter"));
    }

    private static void AssertSimulationRound(int round, SceneBiomeTypes expectedBiome, string expectedMusic)
    {
        var scene = CreateSimulationForRound(round);
        Assert.AreEqual(expectedBiome, scene.SceneBiome);
        Assert.AreEqual(expectedMusic, scene.SceneMusic);
    }

    private static SceneSimulation CreateSimulationForBiome(SceneBiomeTypes biome)
    {
        for (int round = 0; round < 16; round++)
        {
            var scene = CreateSimulationForRound(round);
            if (scene.SceneBiome == biome)
                return scene;
        }

        Assert.Fail($"Could not find simulation round for biome {biome}.");
        return null!;
    }

    private static SceneSimulation CreateSimulationForRound(int round)
    {
        GameState.GamePlayState.SimulationRound = round;
        return new SceneSimulation();
    }

    private static int GetPrivateInt(SceneSimulation scene, string fieldName) =>
        (int)GetPrivateField(scene, fieldName);

    private static float GetPrivateFloat(SceneSimulation scene, string fieldName) =>
        (float)GetPrivateField(scene, fieldName);

    private static object GetPrivateField(SceneSimulation scene, string fieldName)
    {
        var field = typeof(SceneSimulation).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing private field {fieldName}.");
        return field!.GetValue(scene)!;
    }

    private static void SetPrivateField<T>(SceneHandler handler, string fieldName, T value)
    {
        var field = typeof(SceneHandler).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing private field: {fieldName}");
        field!.SetValue(handler, value);
    }

    private sealed class TestWorld : I3dWorld
    {
        public List<I3dObject> WorldInhabitants { get; set; } = new();
        public ISceneHandler SceneHandler { get; set; } = null!;
        public IGameEventBus? EventBus { get; set; } = new GameEventBus();
        public bool IsPaused { get; set; }
    }
}
