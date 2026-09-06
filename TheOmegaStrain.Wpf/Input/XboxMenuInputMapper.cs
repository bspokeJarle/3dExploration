using System;

namespace TheOmegaStrain.Wpf.Input
{
    public static class XboxMenuInputMapper
    {
        private const float StickNavigationThreshold = 0.45f;

        public static GameInputKey ToGameInputKey(XboxControllerSnapshot state)
        {
            if (XboxControllerInput.IsControlPressed(state, XboxControlButton.B) ||
                XboxControllerInput.IsControlPressed(state, XboxControlButton.View))
                return GameInputKey.Escape;

            if (XboxControllerInput.IsControlPressed(state, XboxControlButton.A) ||
                XboxControllerInput.IsControlPressed(state, XboxControlButton.Menu))
                return GameInputKey.Return;

            if (XboxControllerInput.IsControlPressed(state, XboxControlButton.DPadUp))
                return GameInputKey.Up;
            if (XboxControllerInput.IsControlPressed(state, XboxControlButton.DPadDown))
                return GameInputKey.Down;
            if (XboxControllerInput.IsControlPressed(state, XboxControlButton.DPadLeft))
                return GameInputKey.Left;
            if (XboxControllerInput.IsControlPressed(state, XboxControlButton.DPadRight))
                return GameInputKey.Right;

            float up = XboxControllerInput.GetControlStrength(state, XboxControlButton.LeftStickUp);
            float down = XboxControllerInput.GetControlStrength(state, XboxControlButton.LeftStickDown);
            float left = XboxControllerInput.GetControlStrength(state, XboxControlButton.LeftStickLeft);
            float right = XboxControllerInput.GetControlStrength(state, XboxControlButton.LeftStickRight);

            float vertical = up - down;
            float horizontal = right - left;

            if (MathF.Max(MathF.Abs(vertical), MathF.Abs(horizontal)) < StickNavigationThreshold)
                return GameInputKey.None;

            if (MathF.Abs(vertical) >= MathF.Abs(horizontal))
                return vertical > 0f ? GameInputKey.Up : GameInputKey.Down;

            return horizontal > 0f ? GameInputKey.Right : GameInputKey.Left;
        }

        public static GameInputKey ToShortcutGameInputKey(XboxControllerSnapshot state)
        {
            if (XboxControllerInput.IsControlPressed(state, XboxControlButton.Y))
                return GameInputKey.T;
            if (XboxControllerInput.IsControlPressed(state, XboxControlButton.X))
                return GameInputKey.C;
            if (XboxControllerInput.IsControlPressed(state, XboxControlButton.LeftShoulder))
                return GameInputKey.S;
            if (XboxControllerInput.IsControlPressed(state, XboxControlButton.RightShoulder))
                return GameInputKey.G;

            return GameInputKey.None;
        }

        public static GameInputKey ToNameEntryGameInputKey(XboxControllerSnapshot state)
        {
            if (XboxControllerInput.IsControlPressed(state, XboxControlButton.X))
                return GameInputKey.Right;
            if (XboxControllerInput.IsControlPressed(state, XboxControlButton.Y))
                return GameInputKey.Down;

            return ToGameInputKey(state);
        }

        public static GameInputKey ToIntroGameInputKey(XboxControllerSnapshot state)
        {
            if (XboxControllerInput.IsControlPressed(state, XboxControlButton.B) ||
                XboxControllerInput.IsControlPressed(state, XboxControlButton.View))
                return GameInputKey.Left;

            return ToGameInputKey(state);
        }

        public static bool IsPauseTogglePressed(XboxControllerSnapshot state) =>
            XboxControllerInput.IsControlPressed(state, XboxControlButton.Menu);

        public static bool IsExitToMenuPressed(XboxControllerSnapshot state) =>
            XboxControllerInput.IsControlPressed(state, XboxControlButton.View);

        public static bool IsQuitConfirmationShortcutPressed(XboxControllerSnapshot state) =>
            XboxControllerInput.IsControlPressed(state, XboxControlButton.View) &&
            XboxControllerInput.IsControlPressed(state, XboxControlButton.Menu);
    }
}
