using TheOmegaStrain.Common.CommonGlobalState;
using TheOmegaStrain.Common.CommonGlobalState.States;
using TheOmegaStrain.Domain;
using TheOmegaStrain.Game.SceneManagement;
using TheOmegaStrain.Game.World;

namespace TheOmegaStrain.Tests.SceneManagement;

[TestClass]
public class SceneHandlerQuitConfirmationTests
{
    [TestInitialize]
    public void Setup()
    {
        GameState.GamePlayState = new GamePlayState();
        GameState.SurfaceState = new SurfaceState();
        GameState.ScreenOverlayState = new ScreenOverlayState();
        GameState.WorldFade = new WorldFadeState();
        GameState.ObjectIdCounter = 0;
        GameState.DeltaTime = 0f;
    }

    [TestMethod]
    public void IntroEscape_ShowsQuitConfirmationWithNoSelected()
    {
        var handler = CreateIntroHandlerWithVisibleOverlay(out var world);

        handler.HandleKeyPress(GameInputKey.Escape, world);

        var overlay = GameState.ScreenOverlayState;
        Assert.AreEqual(ScreenOverlayChoiceAction.QuitGameConfirmation, overlay.ChoiceAction);
        Assert.AreEqual("NO", overlay.SelectedChoice);
        Assert.IsFalse(overlay.QuitApplicationRequested);
        StringAssert.Contains(overlay.Body, "> NO");
        StringAssert.Contains(overlay.Footer, "[B] BACK");
    }

    [TestMethod]
    public void QuitConfirmation_CancelRestoresIntroOverlay()
    {
        var handler = CreateIntroHandlerWithVisibleOverlay(out var world);

        handler.HandleKeyPress(GameInputKey.Escape, world);
        handler.HandleKeyPress(GameInputKey.Right, world);
        handler.HandleKeyPress(GameInputKey.Escape, world);

        var overlay = GameState.ScreenOverlayState;
        Assert.AreEqual(ScreenOverlayChoiceAction.None, overlay.ChoiceAction);
        Assert.AreEqual(ScreenOverlayType.Intro, overlay.Type);
        Assert.IsTrue(overlay.ShowOverlay);
        Assert.IsFalse(overlay.QuitApplicationRequested);
    }

    [TestMethod]
    public void QuitConfirmation_RequiresExplicitYesConfirmation()
    {
        var handler = CreateIntroHandlerWithVisibleOverlay(out var world);

        handler.HandleKeyPress(GameInputKey.Escape, world);
        handler.HandleKeyPress(GameInputKey.Return, world);

        Assert.IsFalse(GameState.ScreenOverlayState.QuitApplicationRequested);

        handler.HandleKeyPress(GameInputKey.Escape, world);
        handler.HandleKeyPress(GameInputKey.Right, world);
        handler.HandleKeyPress(GameInputKey.Return, world);

        Assert.IsTrue(GameState.ScreenOverlayState.QuitApplicationRequested);
    }

    private static SceneHandler CreateIntroHandlerWithVisibleOverlay(out GameWorld world)
    {
        var handler = new SceneHandler();
        world = new GameWorld { SceneHandler = handler };
        handler.GetActiveScene().SetupSceneOverlay();
        GameState.ScreenOverlayState.ShowOverlay = true;
        return handler;
    }
}
