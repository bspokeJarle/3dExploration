using CommonUtilities.CommonGlobalState;
using System;
using System.IO;
using System.Text.Json;

namespace CommonUtilities.Persistence
{
    /// <summary>
    /// Saves and restores the local game state as an encrypted JSON file.
    /// The file and its key are stored in the local data folder
    /// (<see cref="PersistenceSetup.LocalFolder"/>).
    /// </summary>
    public static class GameStatePersistence
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Takes a snapshot of the current <see cref="GameState.GamePlayState"/>
        /// and writes it as an encrypted per-player file.
        /// </summary>
        public static void SaveGameState()
        {
            var state = GameState.GamePlayState;
            if (string.IsNullOrWhiteSpace(state.PlayerName)) return;

            var saved = new SavedGameState
            {
                PlayerName = state.PlayerName,
                SceneIndex = state.SceneIndex,
                Score = state.Score,
                Lives = state.Lives,
                Health = state.Health,
                MaxHealth = state.MaxHealth,
                WaveNumber = state.WaveNumber,
                PowerUpsCollected = state.PowerUpsCollected,
                InfectionLevel = state.InfectionLevel,
                TotalBioTiles = state.TotalBioTiles,
                SeedersRemaining = state.SeedersRemaining,
                DronesRemaining = state.DronesRemaining,
                MotherShipsRemaining = state.MotherShipsRemaining,
                TotalShotsFired = state.TotalShotsFired,
                TotalKills = state.TotalKills,
                TotalDeaths = state.TotalDeaths,
                HasCheckpoint = state.HasCheckpoint,
                CheckpointScore = state.CheckpointScore,
                CheckpointLives = state.CheckpointLives,
                CheckpointHealth = state.CheckpointHealth,
                CheckpointPowerUpsCollected = state.CheckpointPowerUpsCollected,
                CheckpointSeedersRemaining = state.CheckpointSeedersRemaining,
                CheckpointDronesRemaining = state.CheckpointDronesRemaining,
                CheckpointMotherShipsRemaining = state.CheckpointMotherShipsRemaining,
                CheckpointTotalShotsFired = state.CheckpointTotalShotsFired,
                CheckpointTotalKills = state.CheckpointTotalKills,
                CheckpointTotalDeaths = state.CheckpointTotalDeaths,
                CheckpointInfectionLevel = state.CheckpointInfectionLevel,
                CheckpointWaveNumber = state.CheckpointWaveNumber,
                CheckpointInitialSeeders = state.CheckpointInitialSeeders,
                CheckpointInitialDrones = state.CheckpointInitialDrones,
                CheckpointInitialMotherShips = state.CheckpointInitialMotherShips,
                SavedAtUtc = DateTime.UtcNow.ToString("o")
            };

            var filePath = PersistenceSetup.GetPlayerGameStateFilePath(state.PlayerName);
            Directory.CreateDirectory(PersistenceSetup.LocalFolder);
            EncryptionHelper.EnsureKeyFile(PersistenceSetup.LocalKeyFilePath);

            var json = JsonSerializer.Serialize(saved, JsonOptions);
            EncryptionHelper.EncryptToFile(
                filePath,
                json,
                PersistenceSetup.LocalKeyFilePath);
        }

        /// <summary>
        /// Loads the saved game state for a specific player from disk.
        /// Returns null if no save exists or if decryption fails.
        /// </summary>
        public static SavedGameState? LoadGameState(string playerName)
        {
            try
            {
                var filePath = PersistenceSetup.GetPlayerGameStateFilePath(playerName);
                if (!File.Exists(filePath))
                    return null;

                var json = EncryptionHelper.DecryptFromFile(
                    filePath,
                    PersistenceSetup.LocalKeyFilePath);

                if (json == null) return null;

                return JsonSerializer.Deserialize<SavedGameState>(json, JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Applies a previously loaded save onto the live
        /// <see cref="GameState.GamePlayState"/>.
        /// </summary>
        public static void RestoreToGamePlayState(SavedGameState saved)
        {
            var state = GameState.GamePlayState;

            state.Score = saved.Score;
            state.SceneIndex = saved.SceneIndex;
            state.Lives = saved.Lives;
            state.Health = saved.Health;
            state.MaxHealth = saved.MaxHealth;
            state.WaveNumber = saved.WaveNumber;
            state.PowerUpsCollected = saved.PowerUpsCollected;
            state.InfectionLevel = saved.InfectionLevel;
            state.TotalBioTiles = saved.TotalBioTiles;
            state.SeedersRemaining = saved.SeedersRemaining;
            state.DronesRemaining = saved.DronesRemaining;
            state.MotherShipsRemaining = saved.MotherShipsRemaining;
            state.TotalShotsFired = saved.TotalShotsFired;
            state.TotalKills = saved.TotalKills;
            state.TotalDeaths = saved.TotalDeaths;
            state.HasCheckpoint = saved.HasCheckpoint;
            state.CheckpointScore = saved.CheckpointScore;
            state.CheckpointLives = saved.CheckpointLives;
            state.CheckpointHealth = saved.CheckpointHealth;
            state.CheckpointPowerUpsCollected = saved.CheckpointPowerUpsCollected;
            state.CheckpointSeedersRemaining = saved.CheckpointSeedersRemaining;
            state.CheckpointDronesRemaining = saved.CheckpointDronesRemaining;
            state.CheckpointMotherShipsRemaining = saved.CheckpointMotherShipsRemaining;
            state.CheckpointTotalShotsFired = saved.CheckpointTotalShotsFired;
            state.CheckpointTotalKills = saved.CheckpointTotalKills;
            state.CheckpointTotalDeaths = saved.CheckpointTotalDeaths;
            state.CheckpointInfectionLevel = saved.CheckpointInfectionLevel;
            state.CheckpointWaveNumber = saved.CheckpointWaveNumber;
        }

        /// <summary>
        /// Returns true if a saved game file exists for the given player.
        /// </summary>
        public static bool HasSavedGame(string playerName) =>
            PersistenceSetup.HasPlayerSaveFile(playerName);

        /// <summary>
        /// Deletes the saved game file for the given player.
        /// </summary>
        public static void DeleteSave(string playerName)
        {
            var path = PersistenceSetup.GetPlayerGameStateFilePath(playerName);
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
