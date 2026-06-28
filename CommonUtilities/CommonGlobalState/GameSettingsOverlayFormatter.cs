using CommonUtilities.CommonGlobalState.States;
using System;
using System.Collections.Generic;

namespace CommonUtilities.CommonGlobalState
{
    public static class GameSettingsOverlayFormatter
    {
        public const string Footer = "UP/DOWN SELECT  //  LEFT/RIGHT ADJUST  //  ENTER OR ESC TO CLOSE";

        public static string BuildAudioBody(GameSettingsState settings, int selectedIndex)
        {
            settings.Normalize();

            var lines = new List<string>
            {
                "Adjust the shipboard audio mix. Changes are saved locally.",
                ""
            };

            AddPercentLine(lines, selectedIndex, (int)AudioSettingsField.MasterVolume, "MASTER", settings.MasterVolumePercent);
            AddPercentLine(lines, selectedIndex, (int)AudioSettingsField.MusicVolume, "MUSIC", settings.MusicVolumePercent);
            AddPercentLine(lines, selectedIndex, (int)AudioSettingsField.EffectsVolume, "EFFECTS", settings.EffectsVolumePercent);
            AddPercentLine(lines, selectedIndex, (int)AudioSettingsField.VoiceVolume, "HAL-E / VOICE", settings.VoiceVolumePercent);

            return string.Join("\n", lines);
        }

        public static string BuildGraphicsBody(GameSettingsState settings, int selectedIndex)
        {
            settings.Normalize();

            var lines = new List<string>
            {
                "Tune visual detail for this machine. Changes are saved locally.",
                ""
            };

            AddValueLine(lines, selectedIndex, (int)GraphicsSettingsField.QualityPreset, "QUALITY", settings.GraphicsQuality.ToString().ToUpperInvariant());
            AddPercentLine(lines, selectedIndex, (int)GraphicsSettingsField.ParticleDensity, "PARTICLES", settings.ParticleDensityPercent);
            AddValueLine(lines, selectedIndex, (int)GraphicsSettingsField.GlowEffects, "GLOW", OnOff(settings.GlowEffectsEnabled));
            AddValueLine(lines, selectedIndex, (int)GraphicsSettingsField.EnhancedWeather, "WEATHER FX", OnOff(settings.EnhancedWeatherEnabled));
            AddValueLine(lines, selectedIndex, (int)GraphicsSettingsField.EnhancedShadows, "SHADOWS", OnOff(settings.EnhancedShadowsEnabled));

            return string.Join("\n", lines);
        }

        private static void AddPercentLine(List<string> lines, int selectedIndex, int index, string label, int percent)
        {
            AddValueLine(lines, selectedIndex, index, label, $"{BuildBar(percent)} {percent,3}%");
        }

        private static void AddValueLine(List<string> lines, int selectedIndex, int index, string label, string value)
        {
            string marker = selectedIndex == index ? ">" : " ";
            lines.Add($"{marker} {label,-14} {value}");
        }

        private static string BuildBar(int percent)
        {
            int blocks = Math.Clamp((int)MathF.Round(percent / 10f), 0, 10);
            return "[" + new string('#', blocks) + new string('-', 10 - blocks) + "]";
        }

        private static string OnOff(bool value) => value ? "ON" : "OFF";
    }
}
