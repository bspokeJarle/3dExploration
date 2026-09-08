using System;

namespace TheOmegaStrain.Common.CommonGlobalState.States
{
    // Detection only: tells the menu system which input devices are actually present.
    // The host (Wpf) updates this every frame; gameplay/menu code only reads it.
    public sealed class InputDeviceState
    {
        public bool XboxControllerConnected { get; private set; }
        public bool SteamAvailable { get; private set; }
        public int SteamInputControllerCount { get; private set; }

        public bool SteamInputControllerConnected => SteamInputControllerCount > 0;

        // True when any gamepad-style device is present, no matter the source.
        public bool AnyControllerConnected => XboxControllerConnected || SteamInputControllerConnected;

        public void SetXboxControllerConnected(bool connected)
        {
            XboxControllerConnected = connected;
        }

        public void SetSteamStatus(bool steamAvailable, int steamInputControllerCount)
        {
            SteamAvailable = steamAvailable;
            SteamInputControllerCount = steamAvailable ? Math.Max(0, steamInputControllerCount) : 0;
        }

        public void Reset()
        {
            XboxControllerConnected = false;
            SteamAvailable = false;
            SteamInputControllerCount = 0;
        }
    }
}
