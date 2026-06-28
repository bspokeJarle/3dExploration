using System;

namespace CommonUtilities.CommonGlobalState.States
{
    public enum GraphicsQualityPreset
    {
        Low = 0,
        Balanced = 1,
        High = 2
    }

    public enum AudioSettingsField
    {
        MasterVolume = 0,
        MusicVolume = 1,
        EffectsVolume = 2,
        VoiceVolume = 3
    }

    public enum GraphicsSettingsField
    {
        QualityPreset = 0,
        ParticleDensity = 1,
        GlowEffects = 2,
        EnhancedWeather = 3,
        EnhancedShadows = 4
    }

    public sealed class GameSettingsState
    {
        public const int VolumeStepPercent = 5;
        public const int ParticleDensityStepPercent = 10;

        public int MasterVolumePercent { get; set; } = 100;
        public int MusicVolumePercent { get; set; } = 100;
        public int EffectsVolumePercent { get; set; } = 100;
        public int VoiceVolumePercent { get; set; } = 100;

        public GraphicsQualityPreset GraphicsQuality { get; set; } = GraphicsQualityPreset.Balanced;
        public int ParticleDensityPercent { get; set; } = 100;
        public bool GlowEffectsEnabled { get; set; } = false;
        public bool EnhancedWeatherEnabled { get; set; } = true;
        public bool EnhancedShadowsEnabled { get; set; } = true;

        public long Version { get; private set; } = 0;

        public float MasterVolumeMultiplier => PercentToMultiplier(MasterVolumePercent);
        public float MusicVolumeMultiplier => MasterVolumeMultiplier * PercentToMultiplier(MusicVolumePercent);
        public float EffectsVolumeMultiplier => MasterVolumeMultiplier * PercentToMultiplier(EffectsVolumePercent);
        public float VoiceVolumeMultiplier => MasterVolumeMultiplier * PercentToMultiplier(VoiceVolumePercent);
        public float ParticleDensityMultiplier => Math.Clamp(ParticleDensityPercent, 50, 200) / 100f;

        public void Normalize()
        {
            MasterVolumePercent = ClampPercent(MasterVolumePercent);
            MusicVolumePercent = ClampPercent(MusicVolumePercent);
            EffectsVolumePercent = ClampPercent(EffectsVolumePercent);
            VoiceVolumePercent = ClampPercent(VoiceVolumePercent);
            ParticleDensityPercent = Math.Clamp(ParticleDensityPercent, 50, 200);

            if (!Enum.IsDefined(typeof(GraphicsQualityPreset), GraphicsQuality))
                GraphicsQuality = GraphicsQualityPreset.Balanced;
        }

        public void AdjustAudio(AudioSettingsField field, int direction)
        {
            if (direction == 0)
                return;

            int delta = direction > 0 ? VolumeStepPercent : -VolumeStepPercent;

            switch (field)
            {
                case AudioSettingsField.MasterVolume:
                    MasterVolumePercent = ClampPercent(MasterVolumePercent + delta);
                    break;
                case AudioSettingsField.MusicVolume:
                    MusicVolumePercent = ClampPercent(MusicVolumePercent + delta);
                    break;
                case AudioSettingsField.EffectsVolume:
                    EffectsVolumePercent = ClampPercent(EffectsVolumePercent + delta);
                    break;
                case AudioSettingsField.VoiceVolume:
                    VoiceVolumePercent = ClampPercent(VoiceVolumePercent + delta);
                    break;
            }

            Version++;
        }

        public void AdjustGraphics(GraphicsSettingsField field, int direction)
        {
            if (direction == 0)
                return;

            switch (field)
            {
                case GraphicsSettingsField.QualityPreset:
                    GraphicsQuality = AdjustQuality(GraphicsQuality, direction);
                    ApplyPresetDefaults(GraphicsQuality);
                    break;
                case GraphicsSettingsField.ParticleDensity:
                    int delta = direction > 0 ? ParticleDensityStepPercent : -ParticleDensityStepPercent;
                    ParticleDensityPercent = Math.Clamp(ParticleDensityPercent + delta, 50, 200);
                    break;
                case GraphicsSettingsField.GlowEffects:
                    GlowEffectsEnabled = !GlowEffectsEnabled;
                    break;
                case GraphicsSettingsField.EnhancedWeather:
                    EnhancedWeatherEnabled = !EnhancedWeatherEnabled;
                    break;
                case GraphicsSettingsField.EnhancedShadows:
                    EnhancedShadowsEnabled = !EnhancedShadowsEnabled;
                    break;
            }

            Version++;
        }

        public float ApplyMusicVolume(float baseVolume) => Clamp01(baseVolume * MusicVolumeMultiplier);
        public float ApplyEffectsVolume(float baseVolume) => SanitizeVolume(baseVolume * EffectsVolumeMultiplier);
        public float ApplyVoiceVolume(float baseVolume) => SanitizeVolume(baseVolume * VoiceVolumeMultiplier);

        public int ScaleParticleCount(int baseCount)
        {
            if (!EnhancedWeatherEnabled)
                return 0;

            return Math.Max(0, (int)MathF.Round(baseCount * ParticleDensityMultiplier));
        }

        public bool IsVoiceSound(string? soundId, string? usage)
        {
            return StartsWithIgnoreCase(soundId, "ship_ai_") ||
                   StartsWithIgnoreCase(soundId, "ship_collision_warning") ||
                   ContainsIgnoreCase(usage, "ShipAiVoice") ||
                   ContainsIgnoreCase(usage, "Voice") ||
                   ContainsIgnoreCase(usage, "Warning");
        }

        private static GraphicsQualityPreset AdjustQuality(GraphicsQualityPreset current, int direction)
        {
            int next = (int)current + (direction > 0 ? 1 : -1);
            next = Math.Clamp(next, (int)GraphicsQualityPreset.Low, (int)GraphicsQualityPreset.High);
            return (GraphicsQualityPreset)next;
        }

        private void ApplyPresetDefaults(GraphicsQualityPreset preset)
        {
            switch (preset)
            {
                case GraphicsQualityPreset.Low:
                    ParticleDensityPercent = 70;
                    GlowEffectsEnabled = false;
                    EnhancedWeatherEnabled = false;
                    EnhancedShadowsEnabled = false;
                    break;
                case GraphicsQualityPreset.High:
                    ParticleDensityPercent = 180;
                    GlowEffectsEnabled = true;
                    EnhancedWeatherEnabled = true;
                    EnhancedShadowsEnabled = true;
                    break;
                default:
                    ParticleDensityPercent = 100;
                    GlowEffectsEnabled = false;
                    EnhancedWeatherEnabled = true;
                    EnhancedShadowsEnabled = true;
                    break;
            }
        }

        private static int ClampPercent(int value) => Math.Clamp(value, 0, 100);

        private static float PercentToMultiplier(int value) => ClampPercent(value) / 100f;

        private static float Clamp01(float value) => Math.Clamp(value, 0f, 1f);

        private static float SanitizeVolume(float value) =>
            float.IsFinite(value) ? Math.Max(0f, value) : 0f;

        private static bool StartsWithIgnoreCase(string? value, string prefix) =>
            !string.IsNullOrWhiteSpace(value) &&
            value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

        private static bool ContainsIgnoreCase(string? value, string needle) =>
            !string.IsNullOrWhiteSpace(value) &&
            value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
