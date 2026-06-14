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
    public void SaveGameState_WhenCheckpointExists_PersistsCheckpointAsResumableState()
    {
        var gps = GameState.GamePlayState;

        gps.PlayerName = "CharlieB";
        gps.SceneIndex = 6;
        gps.SimulationRound = 2;
        gps.CurrentSceneBiome = SceneBiomeTypes.Desert;
        gps.MaxHealth = 100f;
        gps.TotalBioTiles = 500;

        gps.Score = 9000;
        gps.PlanetStyleBonusScore = 1300;
        gps.PlanetStyleBonusSceneIndex = 6;
        gps.Lives = 1;
        gps.Health = 12f;
        gps.WaveNumber = 8;
        gps.PowerUpsCollected = 2;
        gps.InfectionLevel = 140f;
        gps.SeedersRemaining = 1;
        gps.DronesRemaining = 0;
        gps.MotherShipsRemaining = 1;
        gps.InitialSeeders = 20;
        gps.InitialDrones = 4;
        gps.InitialMotherShips = 1;
        gps.TotalShotsFired = 200;
        gps.TotalKills = 35;
        gps.TotalDeaths = 4;

        gps.HasCheckpoint = true;
        gps.CheckpointScore = 4200;
        gps.CheckpointPlanetStyleBonusScore = 500;
        gps.CheckpointPlanetStyleBonusSceneIndex = 6;
        gps.CheckpointLives = 3;
        gps.CheckpointHealth = 88f;
        gps.CheckpointWaveNumber = 4;
        gps.CheckpointPowerUpsCollected = 1;
        gps.CheckpointInfectionLevel = 40f;
        gps.CheckpointSeedersRemaining = 8;
        gps.CheckpointDronesRemaining = 2;
        gps.CheckpointMotherShipsRemaining = 0;
        gps.CheckpointInitialSeeders = 20;
        gps.CheckpointInitialDrones = 4;
        gps.CheckpointInitialMotherShips = 1;
        gps.CheckpointTotalShotsFired = 80;
        gps.CheckpointTotalKills = 12;
        gps.CheckpointTotalDeaths = 1;

        GameStatePersistence.SaveGameState();

        var loaded = GameStatePersistence.LoadGameState("CharlieB");

        Assert.IsNotNull(loaded);
        Assert.AreEqual(6, loaded!.SceneIndex);
        Assert.AreEqual(2, loaded.SimulationRound);
        Assert.AreEqual(SceneBiomeTypes.Desert, loaded.SceneBiome);
        Assert.AreEqual(100f, loaded.MaxHealth);
        Assert.AreEqual(500, loaded.TotalBioTiles);

        Assert.AreEqual(4200L, loaded.Score);
        Assert.AreEqual(500, loaded.PlanetStyleBonusScore);
        Assert.AreEqual(6, loaded.PlanetStyleBonusSceneIndex);
        Assert.AreEqual(3, loaded.Lives);
        Assert.AreEqual(88f, loaded.Health);
        Assert.AreEqual(4, loaded.WaveNumber);
        Assert.AreEqual(1, loaded.PowerUpsCollected);
        Assert.AreEqual(40f, loaded.InfectionLevel);
        Assert.AreEqual(8, loaded.SeedersRemaining);
        Assert.AreEqual(2, loaded.DronesRemaining);
        Assert.AreEqual(0, loaded.MotherShipsRemaining);
        Assert.AreEqual(20, loaded.InitialSeeders);
        Assert.AreEqual(4, loaded.InitialDrones);
        Assert.AreEqual(1, loaded.InitialMotherShips);
        Assert.AreEqual(80, loaded.TotalShotsFired);
        Assert.AreEqual(12, loaded.TotalKills);
        Assert.AreEqual(1, loaded.TotalDeaths);

        Assert.IsTrue(loaded.HasCheckpoint);
        Assert.AreEqual(4200L, loaded.CheckpointScore);
        Assert.AreEqual(500, loaded.CheckpointPlanetStyleBonusScore);
        Assert.AreEqual(6, loaded.CheckpointPlanetStyleBonusSceneIndex);
        Assert.AreEqual(88f, loaded.CheckpointHealth);
        Assert.AreEqual(8, loaded.CheckpointSeedersRemaining);
    }

    [TestMethod]
    public void SaveAndRestore_PersistsPlanetStartSnapshot()
    {
        var gps = GameState.GamePlayState;

        gps.PlayerName = "CharlieB";
        gps.SceneIndex = 6;
        gps.CurrentSceneBiome = SceneBiomeTypes.Desert;
        gps.Score = 5000;
        gps.PlanetStyleBonusScore = 200;
        gps.PlanetStyleBonusSceneIndex = 6;
        gps.Lives = 2;
        gps.Health = 91f;
        gps.PowerUpsCollected = 1;
        gps.InfectionLevel = 0f;
        gps.SeedersRemaining = 24;
        gps.DronesRemaining = 7;
        gps.MotherShipsRemaining = 1;
        gps.InitialSeeders = 24;
        gps.InitialDrones = 7;
        gps.InitialMotherShips = 1;
        gps.TotalShotsFired = 80;
        gps.TotalKills = 14;
        gps.TotalDeaths = 2;
        gps.SavePlanetStartSnapshot();

        gps.Score = 7500;
        gps.PlanetStyleBonusScore = 900;
        gps.Health = 35f;
        gps.PowerUpsCollected = 2;
        gps.InfectionLevel = 85f;
        gps.SeedersRemaining = 4;
        gps.DronesRemaining = 1;
        gps.MotherShipsRemaining = 0;
        gps.TotalShotsFired = 160;
        gps.TotalKills = 28;
        gps.TotalDeaths = 3;
        gps.SaveCheckpoint();

        GameStatePersistence.SaveGameState();

        var loaded = GameStatePersistence.LoadGameState("CharlieB");
        Assert.IsNotNull(loaded);
        Assert.IsTrue(loaded!.HasPlanetStartSnapshot);
        Assert.AreEqual(6, loaded.PlanetStartSceneIndex);
        Assert.AreEqual(5000L, loaded.PlanetStartScore);
        Assert.AreEqual(200, loaded.PlanetStartPlanetStyleBonusScore);
        Assert.AreEqual(91f, loaded.PlanetStartHealth);
        Assert.AreEqual(1, loaded.PlanetStartPowerUpsCollected);
        Assert.AreEqual(24, loaded.PlanetStartSeedersRemaining);
        Assert.AreEqual(7, loaded.PlanetStartDronesRemaining);
        Assert.AreEqual(1, loaded.PlanetStartMotherShipsRemaining);
        Assert.AreEqual(80, loaded.PlanetStartTotalShotsFired);
        Assert.AreEqual(14, loaded.PlanetStartTotalKills);
        Assert.AreEqual(2, loaded.PlanetStartTotalDeaths);

        GameState.GamePlayState = new GamePlayState();
        GameStatePersistence.RestoreToGamePlayState(loaded);

        var restored = GameState.GamePlayState;
        Assert.IsTrue(restored.HasPlanetStartSnapshot);
        Assert.AreEqual(6, restored.PlanetStartSceneIndex);
        Assert.AreEqual(5000L, restored.PlanetStartScore);
        Assert.AreEqual(24, restored.PlanetStartSeedersRemaining);
        Assert.AreEqual(7, restored.PlanetStartDronesRemaining);
        Assert.AreEqual(1, restored.PlanetStartMotherShipsRemaining);
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
