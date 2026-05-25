using _3DWorld.Scene;
using _3dTesting._3dWorld;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using CommonUtilities.Persistence;
using Domain;
using System;
using System.IO;
using System.Reflection;
using static Domain._3dSpecificsImplementations;

namespace _3DSpesificsUnitTests.SceneManagement;

[TestClass]
public class SceneHandlerSavedStateIsolationTests
{
    private string _originalLocalFolder = string.Empty;
    private string _testLocalFolder = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _originalLocalFolder = PersistenceSetup.LocalFolder;
        _testLocalFolder = Path.Combine(Path.GetTempPath(), "OmegaStrainSceneTests", Guid.NewGuid().ToString("N"));
        PersistenceSetup.LocalFolder = _testLocalFolder;
        PersistenceSetup.Initialize();

        GameState.GamePlayState = new GamePlayState { SceneIndex = 0 };
        GameState.SurfaceState = new SurfaceState();
        GameState.ScreenOverlayState = new ScreenOverlayState();
        GameState.ObjectIdCounter = 0;
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
    public void ReturningPlayer_LoadsOnlyOwnSavedSceneIndex()
    {
        // Save Jarle at Scene4
        GameState.GamePlayState = new GamePlayState
        {
            PlayerName = "Jarle",
            SceneIndex = 4,
            Score = 1000
        };
        GameStatePersistence.SaveGameState();

        // Save Anna at Scene1
        GameState.GamePlayState = new GamePlayState
        {
            PlayerName = "Anna",
            SceneIndex = 1,
            Score = 2000
        };
        GameStatePersistence.SaveGameState();

        // Simulate login flow for Anna
        var loadedAnna = GameStatePersistence.LoadGameState("Anna");
        Assert.IsNotNull(loadedAnna);

        // SceneHandler logic should only target Anna's scene index
        var handler = new SceneHandler();
        var world = CreateWorld(handler);

        // emulate what HandleNameEntryKey would set for Anna
        GameState.GamePlayState.PlayerName = "Anna";
        GameState.GamePlayState.SceneIndex = 0;

        // We cannot call private handler methods directly; verify persistence source is correct.
        Assert.AreEqual(1, loadedAnna!.SceneIndex);

        var loadedJarle = GameStatePersistence.LoadGameState("Jarle");
        Assert.IsNotNull(loadedJarle);
        Assert.AreEqual(4, loadedJarle!.SceneIndex);

        // Ensure world/handler still starts from intro and can progress deterministically.
        Assert.AreEqual("Intro", handler.GetActiveScene().GetType().Name);
        handler.NextScene(world);
        Assert.AreEqual("Scene1", handler.GetActiveScene().GetType().Name);
    }

    [TestMethod]
    public void NextScene_DoesNotPersistPreviousSceneCheckpointUnderNewSceneIndex()
    {
        var handler = new SceneHandler();
        var world = CreateWorld(handler);

        var gps = GameState.GamePlayState;
        gps.PlayerName = "Jarle";
        gps.SceneIndex = 0;
        gps.Score = 1234;
        gps.PowerUpsCollected = 2;
        gps.SeedersRemaining = 0;
        gps.DronesRemaining = 0;
        gps.MotherShipsRemaining = 1;
        gps.InitialSeeders = 7;
        gps.InitialDrones = 4;
        gps.InitialMotherShips = 1;
        gps.SaveCheckpoint();

        handler.NextScene(world);

        var saved = GameStatePersistence.LoadGameState("Jarle");

        Assert.IsNotNull(saved);
        Assert.AreEqual(1, saved!.SceneIndex);
        Assert.IsFalse(saved.HasCheckpoint,
            "Entering a new scene should save fresh scene progress, not the previous scene's checkpoint.");
        Assert.AreEqual(1234, saved.Score);
        Assert.AreEqual(2, saved.PowerUpsCollected);
    }

    [TestMethod]
    public void ResetActiveScene_CheckpointWithZeroMotherShips_PreservesSceneMotherShipCandidate()
    {
        var handler = new SceneHandler();
        var world = CreateWorld(handler);

        handler.NextScene(world); // Scene1
        handler.NextScene(world); // Scene2
        handler.NextScene(world); // Scene3

        var gps = GameState.GamePlayState;
        gps.PowerUpsCollected = 1;
        gps.SeedersRemaining = 5;
        gps.DronesRemaining = 3;
        gps.MotherShipsRemaining = 0;
        gps.InitialSeeders = 12;
        gps.InitialDrones = 8;
        gps.InitialMotherShips = 1;
        gps.SaveCheckpoint();

        handler.ResetActiveScene(world);

        int motherShipCount = GameState.SurfaceState.AiObjects.Count(o => o.ObjectName == "MotherShipSmall");
        Assert.IsTrue(motherShipCount > 0,
            "Checkpoint restore should keep at least one Scene3 mothership candidate even when checkpoint mother ship count is zero.");
    }

    [TestMethod]
    public void UpdateFrame_LoadedCheckpointWithZeroMotherShips_PreservesSceneMotherShipCandidate()
    {
        var handler = new SceneHandler();
        var world = CreateWorld(handler);

        var loaded = new SavedGameState
        {
            PlayerName = "Jarle",
            SceneIndex = 3,
            Score = 3456,
            TotalKills = 22,
            TotalShotsFired = 100,
            TotalDeaths = 1,
            PowerUpsCollected = 1,
            HasCheckpoint = true,
            CheckpointSeedersRemaining = 5,
            CheckpointDronesRemaining = 3,
            CheckpointMotherShipsRemaining = 0,
            CheckpointInitialSeeders = 12,
            CheckpointInitialDrones = 8,
            CheckpointInitialMotherShips = 1,
            CheckpointTotalKills = 10
        };

        SetPrivateField(handler, "_pendingSavedState", loaded);
        SetPrivateField(handler, "_targetSceneIndex", 3);
        SetPrivateField(handler, "_pendingSceneAdvance", true);
        SetPrivateField(handler, "_pendingSceneAdvanceFramesLeft", 0);

        handler.UpdateFrame(world);

        int motherShipCount = GameState.SurfaceState.AiObjects.Count(o => o.ObjectName == "MotherShipSmall");
        Assert.AreEqual(1, motherShipCount,
            "Loaded checkpoint restore should keep Scene3 mothership candidate even when checkpoint mother ship count is zero.");
    }

    private static void SetPrivateField<T>(SceneHandler handler, string fieldName, T value)
    {
        var field = typeof(SceneHandler).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing private field: {fieldName}");
        field!.SetValue(handler, value);
    }

    private static _3dWorld CreateWorld(SceneHandler handler)
    {
        var world = new _3dWorld();
        world.SceneHandler = handler;
        world.WorldInhabitants.Clear();
        handler.SetupActiveScene(world);
        return world;
    }
}
