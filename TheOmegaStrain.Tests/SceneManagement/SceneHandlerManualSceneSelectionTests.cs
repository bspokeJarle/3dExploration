using System.Reflection;
using TheOmegaStrain.Game.SceneManagement;
using TheOmegaStrain.Game.World;
using TheOmegaStrain.Common.CommonGlobalState;
using TheOmegaStrain.Common.CommonGlobalState.States;
using TheOmegaStrain.Common.Persistence;
using TheOmegaStrain.Domain;

namespace TheOmegaStrain.Tests.SceneManagement;

/// <summary>
/// Regression coverage for the manual scene selection test hook:
/// GamePlayState.SceneIndex is edited in code before running, the Intro must still
/// play first, and the manually selected scene must win over the saved game scene.
/// </summary>
[TestClass]
public class SceneHandlerManualSceneSelectionTests
{
    private string _originalLocalFolder = string.Empty;
    private string _testLocalFolder = string.Empty;
    private int _originalDevStartSceneIndex;

    [TestInitialize]
    public void Setup()
    {
        _originalLocalFolder = PersistenceSetup.LocalFolder;
        _testLocalFolder = Path.Combine(Path.GetTempPath(), "OmegaStrainManualSceneTests", Guid.NewGuid().ToString("N"));
        PersistenceSetup.LocalFolder = _testLocalFolder;
        PersistenceSetup.Initialize();

        // Tests drive the selection through GamePlayState.SceneIndex, so neutralize the
        // developer switch regardless of what value is currently checked in.
        _originalDevStartSceneIndex = SceneHandler.DevStartSceneIndex;
        SceneHandler.DevStartSceneIndex = 0;

        GameState.GamePlayState = new GamePlayState();
        GameState.SurfaceState = new SurfaceState();
        GameState.ScreenOverlayState = new ScreenOverlayState();
        GameState.ObjectIdCounter = 0;
    }

    [TestCleanup]
    public void Cleanup()
    {
        SceneHandler.DevStartSceneIndex = _originalDevStartSceneIndex;
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
    public void DevStartSceneIndex_BeatsSavedGameAndTutorial()
    {
        // Simulates the real launch path: a returning pilot with a Scene6 save and a
        // completed tutorial, where the developer has set the dev switch to Scene1.
        SaveGameForPilotAtScene(6);
        TutorialProgressService.MarkTutorialCompleted("Pilot");
        SceneHandler.DevStartSceneIndex = 1;

        GameState.GamePlayState = new GamePlayState();
        var handler = new SceneHandler();

        Assert.AreEqual("Intro", handler.GetActiveScene().GetType().Name, "Intro must always play first.");

        ConfirmNameEntry(handler, "Pilot");

        Assert.AreEqual(1, GetTargetSceneIndex(handler),
            "DevStartSceneIndex must win over the saved Scene6.");
    }

    [TestMethod]
    public void ManualTrainingRequest_BeatsDevStartSceneIndex()
    {
        // The T key (explicit training request) must always win over the dev switch.
        TutorialProgressService.MarkTutorialCompleted("Pilot");
        SceneHandler.DevStartSceneIndex = 1;

        GameState.GamePlayState = new GamePlayState();
        var handler = new SceneHandler();

        var pendingTutorial = typeof(SceneHandler).GetField("_pendingTutorialStart", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(pendingTutorial, "_pendingTutorialStart not found.");
        pendingTutorial!.SetValue(handler, true);

        ConfirmNameEntry(handler, "Pilot");

        var tutorialIndex = GetTargetSceneIndex(handler);
        Assert.AreEqual(SceneTypes.Tutorial, GetSceneTypeAt(handler, tutorialIndex),
            "Requesting training must override DevStartSceneIndex.");
    }

    private static SceneTypes GetSceneTypeAt(SceneHandler handler, int index)
    {
        var field = typeof(SceneHandler).GetField("scenes", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, "scenes not found.");
        var list = (System.Collections.Generic.List<IScene>)field!.GetValue(handler)!;
        return list[index].SceneType;
    }

    [TestMethod]
    public void ManualSceneIndex_SurvivesIntroOverwritingSceneIndex()
    {
        GameState.GamePlayState.PlayerName = "Pilot";
        GameState.GamePlayState.SceneIndex = 1;

        var handler = new SceneHandler();

        // Intro must still be the active scene (manual selection never skips the intro).
        Assert.AreEqual("Intro", handler.GetActiveScene().GetType().Name);

        // The intro overwrites GamePlayState.SceneIndex with its own index.
        GameState.GamePlayState.SceneIndex = 0;

        Assert.AreEqual(1, GetManualSceneIndexRequest(handler));
    }

    [TestMethod]
    public void ManualSceneIndex_BeatsSavedGameSceneOnNameEntry()
    {
        SaveGameForPilotAtScene(6);

        GameState.GamePlayState = new GamePlayState { PlayerName = "Pilot", SceneIndex = 1 };
        var handler = new SceneHandler();

        Assert.AreEqual("Intro", handler.GetActiveScene().GetType().Name);

        ConfirmNameEntry(handler, "Pilot");

        Assert.AreEqual(1, GetTargetSceneIndex(handler), "Manual scene selection must override the saved game scene.");
    }

    [TestMethod]
    public void SavedGameScene_IsUsedWhenNoManualSceneSelected()
    {
        SaveGameForPilotAtScene(6);
        TutorialProgressService.MarkTutorialCompleted("Pilot");

        GameState.GamePlayState = new GamePlayState { PlayerName = "Pilot", SceneIndex = 0 };
        var handler = new SceneHandler();

        ConfirmNameEntry(handler, "Pilot");

        Assert.AreEqual(6, GetTargetSceneIndex(handler), "Without a manual selection the saved scene must be used.");
    }

    [TestMethod]
    public void ManualSceneIndex_BypassesTutorialGateForUntrainedPilot()
    {
        // Save a game at Scene6 WITHOUT marking the tutorial completed, so the
        // tutorial gate would normally redirect this pilot into training.
        var gps = GameState.GamePlayState;
        gps.PlayerName = "Pilot";
        gps.SceneIndex = 6;
        gps.CurrentSceneType = SceneTypes.Game;
        GameStatePersistence.SaveGameState();
        PersistenceSetup.SaveLastPlayerName("Pilot");

        GameState.GamePlayState = new GamePlayState { PlayerName = "Pilot", SceneIndex = 1 };
        var handler = new SceneHandler();

        ConfirmNameEntry(handler, "Pilot");
        int target = GetTargetSceneIndex(handler);
        int gated = InvokeTutorialGate(handler, target);

        Assert.AreEqual(1, target, "Manual selection must target Scene1.");
        Assert.AreNotEqual(target, gated, "Sanity: the tutorial gate would otherwise redirect this pilot.");
    }

    [TestMethod]
    public void TutorialGate_StillRedirectsUntrainedPilot_WhenNoManualSelection()
    {
        // Guards the training flow: with no dev switch and no manual selection,
        // an untrained pilot must still be routed into the tutorial.
        SceneHandler.DevStartSceneIndex = 0;

        GameState.GamePlayState = new GamePlayState { PlayerName = "Rookie", SceneIndex = 0 };
        var handler = new SceneHandler();
        var world = new GameWorld { SceneHandler = handler };
        world.WorldInhabitants.Clear();

        handler.NextScene(world);

        Assert.AreEqual(SceneTypes.Tutorial, handler.GetActiveScene().SceneType,
            "Untrained pilot must be routed to the tutorial before Scene1.");
        Assert.IsFalse(TutorialProgressService.HasCompletedTutorial("Rookie"));
    }

    [TestMethod]
    public void ManualSelection_DoesNotMarkTutorialAsCompleted()
    {
        // Bypassing the gate for a dev jump must not corrupt real training progress.
        GameState.GamePlayState = new GamePlayState { PlayerName = "Rookie", SceneIndex = 1 };
        var handler = new SceneHandler();

        ConfirmNameEntry(handler, "Rookie");

        Assert.IsFalse(TutorialProgressService.HasCompletedTutorial("Rookie"),
            "A manual scene jump must not silently mark training as completed.");
    }

    private static int InvokeTutorialGate(SceneHandler handler, int candidate)
    {
        var method = typeof(SceneHandler).GetMethod("ApplyTutorialGate", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method, "ApplyTutorialGate not found.");
        return (int)method!.Invoke(handler, new object[] { candidate })!;
    }

    private static void SaveGameForPilotAtScene(int sceneIndex)
    {
        var gps = GameState.GamePlayState;
        gps.PlayerName = "Pilot";
        gps.SceneIndex = sceneIndex;
        gps.CurrentSceneType = SceneTypes.Game;
        gps.Score = 12500;
        GameStatePersistence.SaveGameState();
        TutorialProgressService.MarkTutorialCompleted("Pilot");
        PersistenceSetup.SaveLastPlayerName("Pilot");
    }

    private static void ConfirmNameEntry(SceneHandler handler, string name)
    {
        var world = new GameWorld { SceneHandler = handler };
        world.WorldInhabitants.Clear();

        GameState.ScreenOverlayState.Type = ScreenOverlayType.NameEntry;
        GameState.ScreenOverlayState.ShowOverlay = true;
        GameState.ScreenOverlayState.NameEntryBuffer = name;

        var method = typeof(SceneHandler).GetMethod("HandleNameEntryKey", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method, "HandleNameEntryKey not found.");
        method!.Invoke(handler, new object[] { GameInputKey.Return, handler.GetActiveScene(), GameState.ScreenOverlayState });
    }

    private static int GetManualSceneIndexRequest(SceneHandler handler)
    {
        var field = typeof(SceneHandler).GetField("_manualSceneIndexRequest", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, "_manualSceneIndexRequest not found.");
        return (int)field!.GetValue(handler)!;
    }

    private static int GetTargetSceneIndex(SceneHandler handler)
    {
        var field = typeof(SceneHandler).GetField("_targetSceneIndex", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, "_targetSceneIndex not found.");
        var value = (int?)field!.GetValue(handler);
        Assert.IsTrue(value.HasValue, "No target scene index was set.");
        return value!.Value;
    }
}
