using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using CommonUtilities.Persistence;
using Domain;
using System;
using System.IO;

namespace _3DSpesificsUnitTests.Persistence;

[TestClass]
public class GameStatePersistenceIsolationTests
{
    private string _originalLocalFolder = string.Empty;
    private string _testLocalFolder = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _originalLocalFolder = PersistenceSetup.LocalFolder;
        _testLocalFolder = Path.Combine(Path.GetTempPath(), "OmegaStrainTests", Guid.NewGuid().ToString("N"));
        PersistenceSetup.LocalFolder = _testLocalFolder;
        PersistenceSetup.Initialize();

        GameState.GamePlayState = new GamePlayState();
        GameState.SurfaceState = new SurfaceState();
        GameState.ScreenOverlayState = new ScreenOverlayState();
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
    public void SaveAndLoad_UsesPerPlayerFiles_AndDoesNotMixUsers()
    {
        var gps = GameState.GamePlayState;

        gps.PlayerName = "Jarle";
        gps.SceneIndex = 3;
        gps.Score = 1111;
        gps.PowerUpsCollected = 2;
        GameStatePersistence.SaveGameState();

        gps.PlayerName = "Anna";
        gps.SceneIndex = 1;
        gps.Score = 2222;
        gps.PowerUpsCollected = 0;
        GameStatePersistence.SaveGameState();

        var jarle = GameStatePersistence.LoadGameState("Jarle");
        var anna = GameStatePersistence.LoadGameState("Anna");

        Assert.IsNotNull(jarle);
        Assert.IsNotNull(anna);
        Assert.AreEqual(3, jarle!.SceneIndex);
        Assert.AreEqual(1111L, jarle.Score);
        Assert.AreEqual(2, jarle.PowerUpsCollected);

        Assert.AreEqual(1, anna!.SceneIndex);
        Assert.AreEqual(2222L, anna.Score);
        Assert.AreEqual(0, anna.PowerUpsCollected);
    }

    [TestMethod]
    public void ResetPlayerToScene1_AffectsOnlyTargetPlayer()
    {
        var gps = GameState.GamePlayState;

        gps.PlayerName = "Jarle";
        gps.SceneIndex = 5;
        gps.Score = 9999;
        gps.PowerUpsCollected = 3;
        GameStatePersistence.SaveGameState();

        gps.PlayerName = "Anna";
        gps.SceneIndex = 4;
        gps.Score = 4444;
        gps.PowerUpsCollected = 1;
        GameStatePersistence.SaveGameState();

        GameStatePersistence.ResetPlayerToScene1("Jarle");

        var jarle = GameStatePersistence.LoadGameState("Jarle");
        var anna = GameStatePersistence.LoadGameState("Anna");

        Assert.IsNotNull(jarle);
        Assert.IsNotNull(anna);

        Assert.AreEqual(1, jarle!.SceneIndex);
        Assert.AreEqual(0L, jarle.Score);
        Assert.AreEqual(0, jarle.PowerUpsCollected);
        Assert.IsFalse(jarle.HasCheckpoint);

        Assert.AreEqual(4, anna!.SceneIndex);
        Assert.AreEqual(4444L, anna.Score);
        Assert.AreEqual(1, anna.PowerUpsCollected);
    }
}
