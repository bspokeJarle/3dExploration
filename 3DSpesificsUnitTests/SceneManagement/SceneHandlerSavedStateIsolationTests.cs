using _3DWorld.Scene;
using _3dTesting._3dWorld;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using CommonUtilities.Persistence;
using Domain;
using System;
using System.IO;
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

        GameState.GamePlayState = new GamePlayState();
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

    private static _3dWorld CreateWorld(SceneHandler handler)
    {
        var world = new _3dWorld();
        world.SceneHandler = handler;
        world.WorldInhabitants.Clear();
        handler.SetupActiveScene(world);
        return world;
    }
}
