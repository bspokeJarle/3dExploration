using System.Reflection;
using TheOmegaStrain.Common.CommonGlobalState;
using TheOmegaStrain.Common.CommonGlobalState.States;
using TheOmegaStrain.Common.Persistence;
using TheOmegaStrain.Domain;
using TheOmegaStrain.Game.SceneManagement;

namespace TheOmegaStrain.Tests.SceneManagement;

[TestClass]
public class SceneHandlerCallsignTests
{
    private string _originalLocalFolder = "";
    private string _testLocalFolder = "";
    private string? _originalSupabaseUrl;
    private string? _originalSupabaseAnonKey;
    private string _originalSupabaseTableName = "";
    private string _originalSupabaseCallsignTableName = "";

    [TestInitialize]
    public void Setup()
    {
        _originalLocalFolder = PersistenceSetup.LocalFolder;
        _originalSupabaseUrl = PersistenceSetup.SupabaseUrl;
        _originalSupabaseAnonKey = PersistenceSetup.SupabaseAnonKey;
        _originalSupabaseTableName = PersistenceSetup.SupabaseTableName;
        _originalSupabaseCallsignTableName = PersistenceSetup.SupabaseCallsignTableName;
        _testLocalFolder = Path.Combine(Path.GetTempPath(), "OmegaStrainSceneHandlerCallsignTests", Guid.NewGuid().ToString("N"));
        PersistenceSetup.LocalFolder = _testLocalFolder;
        PersistenceSetup.SupabaseUrl = null;
        PersistenceSetup.SupabaseAnonKey = null;
        PersistenceSetup.SupabaseTableName = "highscores";
        PersistenceSetup.SupabaseCallsignTableName = "player_callsigns";
        PersistenceSetup.Initialize();

        GameState.GamePlayState = new GamePlayState();
        GameState.SurfaceState = new SurfaceState();
        GameState.ScreenOverlayState = new ScreenOverlayState();
        GameState.WorldFade = new WorldFadeState();
        GameState.ObjectIdCounter = 0;
        GameState.DeltaTime = 0f;
        HighscoreService.SaveLocalHighscores(new HighscoreList());
    }

    [TestCleanup]
    public void Cleanup()
    {
        PersistenceSetup.LocalFolder = _originalLocalFolder;
        PersistenceSetup.SupabaseUrl = _originalSupabaseUrl;
        PersistenceSetup.SupabaseAnonKey = _originalSupabaseAnonKey;
        PersistenceSetup.SupabaseTableName = _originalSupabaseTableName;
        PersistenceSetup.SupabaseCallsignTableName = _originalSupabaseCallsignTableName;

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
    public void ShowNameEntryOverlay_UsesSuggestedCallsignWhenNoLastPlayerExists()
    {
        var handler = new SceneHandler();
        var overlay = GameState.ScreenOverlayState;

        InvokeShowNameEntryOverlay(handler, overlay);

        Assert.AreEqual(ScreenOverlayType.NameEntry, overlay.Type);
        Assert.IsFalse(string.IsNullOrWhiteSpace(overlay.NameEntryBuffer));
        Assert.IsTrue(overlay.NameEntryBuffer.Length <= ScreenOverlayState.MaxCallsignLength);
    }

    [TestMethod]
    public void HandleNameEntryKey_RightArrowGeneratesNewSuggestedCallsign()
    {
        var handler = new SceneHandler();
        var overlay = GameState.ScreenOverlayState;
        InvokeShowNameEntryOverlay(handler, overlay);
        string first = overlay.NameEntryBuffer;

        InvokeHandleNameEntryKey(handler, GameInputKey.Right, overlay);

        Assert.AreNotEqual(first, overlay.NameEntryBuffer);
        Assert.AreEqual(">> NEW CALLSIGN SUGGESTED", overlay.NameEntryValidationMessage);
    }

    [TestMethod]
    public void HandleNameEntryKey_TakenCallsignOffersNumberedVariant()
    {
        HighscoreService.SaveLocalHighscores(new HighscoreList
        {
            Entries = new List<HighscoreEntry> { new() { PlayerName = "RED CARMACK" } }
        });
        var handler = new SceneHandler();
        var overlay = GameState.ScreenOverlayState;
        InvokeShowNameEntryOverlay(handler, overlay);
        overlay.NameEntryBuffer = "RED CARMACK";

        InvokeHandleNameEntryKey(handler, GameInputKey.Return, overlay);

        StringAssert.StartsWith(overlay.NameEntryBuffer, "RED CARMACK ");
        Assert.IsTrue(char.IsDigit(overlay.NameEntryBuffer[^1]));
        Assert.AreEqual(
            PlayerCallsignService.NumberedCallsignSuggestedMessage,
            overlay.NameEntryValidationMessage);
        Assert.IsFalse(overlay.IsNameConfirmed);
    }

    [TestMethod]
    public void HandleNameEntryKey_EscapeRestoresVisibleIntroMenuImmediately()
    {
        var handler = new SceneHandler();
        var overlay = GameState.ScreenOverlayState;
        InvokeShowNameEntryOverlay(handler, overlay);

        InvokeHandleNameEntryKey(handler, GameInputKey.Escape, overlay);

        Assert.AreEqual(ScreenOverlayType.Intro, overlay.Type);
        Assert.AreEqual(ScreenOverlayChoiceAction.IntroMainMenu, overlay.ChoiceAction);
        Assert.IsTrue(overlay.ShowOverlay);
    }

    private static void InvokeShowNameEntryOverlay(SceneHandler handler, ScreenOverlayState overlay)
    {
        var method = typeof(SceneHandler).GetMethod("ShowNameEntryOverlay", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method, "ShowNameEntryOverlay not found.");
        method!.Invoke(handler, new object[] { overlay });
    }

    private static void InvokeHandleNameEntryKey(SceneHandler handler, GameInputKey key, ScreenOverlayState overlay)
    {
        var method = typeof(SceneHandler).GetMethod("HandleNameEntryKey", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method, "HandleNameEntryKey not found.");
        method!.Invoke(handler, new object[] { key, handler.GetActiveScene(), overlay });
    }
}
