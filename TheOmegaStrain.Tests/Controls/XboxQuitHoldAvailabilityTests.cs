using TheOmegaStrain.Common.CommonGlobalState.States;
using TheOmegaStrain.Domain;
using TheOmegaStrain.Wpf.Input;

namespace TheOmegaStrain.Tests.Controls;

[TestClass]
public class XboxQuitHoldAvailabilityTests
{
    [TestMethod]
    public void CanStart_AllowsXboxMainMenu()
    {
        var overlay = CreateMainMenuOverlay();

        Assert.IsTrue(XboxQuitHoldAvailability.CanStart(
            overlay,
            SceneTypes.Intro,
            ControlInputMode.XboxController));
    }

    [TestMethod]
    public void CanStart_RejectsOtherDialogsScenesAndControlModes()
    {
        var overlay = CreateMainMenuOverlay();

        overlay.ChoiceAction = ScreenOverlayChoiceAction.QuitGameConfirmation;
        Assert.IsFalse(XboxQuitHoldAvailability.CanStart(
            overlay,
            SceneTypes.Intro,
            ControlInputMode.XboxController));

        overlay.ChoiceAction = ScreenOverlayChoiceAction.IntroMainMenu;
        Assert.IsFalse(XboxQuitHoldAvailability.CanStart(
            overlay,
            SceneTypes.Game,
            ControlInputMode.XboxController));
        Assert.IsFalse(XboxQuitHoldAvailability.CanStart(
            overlay,
            SceneTypes.Intro,
            ControlInputMode.Keyboard));
    }

    private static ScreenOverlayState CreateMainMenuOverlay() => new()
    {
        ShowOverlay = true,
        Type = ScreenOverlayType.Intro,
        CurrentPage = 0,
        ChoiceAction = ScreenOverlayChoiceAction.IntroMainMenu
    };
}
