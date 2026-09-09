using System;

namespace TheOmegaStrain.Wpf.Input
{
    public sealed class XboxQuitHoldTracker
    {
        public static readonly TimeSpan RequiredHoldDuration = TimeSpan.FromSeconds(2);

        private DateTime? holdStartedAtUtc;
        private bool triggered;

        public bool IsTracking => holdStartedAtUtc.HasValue;

        public bool Update(bool isPressed, DateTime nowUtc)
        {
            if (!isPressed)
            {
                Reset();
                return false;
            }

            if (!holdStartedAtUtc.HasValue)
            {
                holdStartedAtUtc = nowUtc;
                return false;
            }

            if (triggered || nowUtc - holdStartedAtUtc.Value < RequiredHoldDuration)
                return false;

            triggered = true;
            return true;
        }

        public void Reset()
        {
            holdStartedAtUtc = null;
            triggered = false;
        }
    }
}
