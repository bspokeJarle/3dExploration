using System;

namespace CommonUtilities.CommonGlobalState.States
{
    public sealed class WeatherVisualState
    {
        public float LightningFlashIntensity { get; private set; }

        public void RaiseLightningFlash(float intensity)
        {
            LightningFlashIntensity = Math.Max(LightningFlashIntensity, Math.Clamp(intensity, 0f, 1f));
        }

        public void DecayLightningFlash(float amount)
        {
            LightningFlashIntensity = Math.Max(0f, LightningFlashIntensity - Math.Max(0f, amount));
        }

        public void ClearLightningFlash()
        {
            LightningFlashIntensity = 0f;
        }
    }
}
