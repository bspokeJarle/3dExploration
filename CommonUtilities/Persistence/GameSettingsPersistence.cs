using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using System;
using System.IO;
using System.Text.Json;

namespace CommonUtilities.Persistence
{
    public static class GameSettingsPersistence
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        public static GameSettingsState LoadSettings()
        {
            try
            {
                if (!File.Exists(PersistenceSetup.LocalSettingsFilePath))
                    return CreateDefaultSettings();

                var json = File.ReadAllText(PersistenceSetup.LocalSettingsFilePath);
                var state = JsonSerializer.Deserialize<GameSettingsState>(json, JsonOptions) ?? CreateDefaultSettings();
                state.Normalize();
                return state;
            }
            catch
            {
                return CreateDefaultSettings();
            }
        }

        public static void LoadIntoGameState()
        {
            GameState.SettingsState = LoadSettings();
        }

        public static void SaveSettings(GameSettingsState settings)
        {
            if (settings == null)
                return;

            try
            {
                Directory.CreateDirectory(PersistenceSetup.LocalFolder);
                settings.Normalize();
                var json = JsonSerializer.Serialize(settings, JsonOptions);
                File.WriteAllText(PersistenceSetup.LocalSettingsFilePath, json);
            }
            catch
            {
            }
        }

        private static GameSettingsState CreateDefaultSettings()
        {
            var settings = new GameSettingsState();
            settings.Normalize();
            return settings;
        }
    }
}
