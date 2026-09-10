using TheOmegaStrain.Common.CommonGlobalState.States;
using TheOmegaStrain.Domain;

namespace TheOmegaStrain.Wpf.Input
{
    public static class XboxQuitHoldAvailability
    {
        public static bool CanStart(
            ScreenOverlayState? overlay,
            SceneTypes sceneType,
            ControlInputMode activeControlScheme)
        {
            return overlay is
                   {
                       ShowOverlay: true,
                       Type: ScreenOverlayType.Intro,
                       CurrentPage: 0,
                       ChoiceAction: ScreenOverlayChoiceAction.IntroMainMenu
                   } &&
                   sceneType == SceneTypes.Intro &&
                   activeControlScheme == ControlInputMode.XboxController;
        }
    }
}
