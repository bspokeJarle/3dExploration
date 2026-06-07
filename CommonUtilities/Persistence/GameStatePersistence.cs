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
        /// Writes the player's durable progress as an encrypted per-player file.
        /// When a checkpoint exists, the resumable state is the last checkpoint,
        /// not the volatile in-flight state at shutdown.
        /// </summary>
        public static void SaveGameState()
        {
            var state = GameState.GamePlayState;
            if (string.IsNullOrWhiteSpace(state.PlayerName)) return;

            bool useCheckpoint = state.HasCheckpoint;

            var saved = new SavedGameState
            {
                PlayerName = state.PlayerName,
                SceneIndex = state.SceneIndex,
                SimulationRound = state.SimulationRound,
                SceneBiome = state.CurrentSceneBiome,
                Score = useCheckpoint ? state.CheckpointScore : state.Score,
                PlanetStyleBonusScore = useCheckpoint ? state.CheckpointPlanetStyleBonusScore : state.PlanetStyleBonusScore,
                PlanetStyleBonusSceneIndex = useCheckpoint ? state.CheckpointPlanetStyleBonusSceneIndex : state.PlanetStyleBonusSceneIndex,
                Lives = useCheckpoint ? state.CheckpointLives : state.Lives,
                Health = useCheckpoint ? state.CheckpointHealth : state.Health,
                MaxHealth = state.MaxHealth,
                WaveNumber = useCheckpoint ? state.CheckpointWaveNumber : state.WaveNumber,
                PowerUpsCollected = useCheckpoint ? state.CheckpointPowerUpsCollected : state.PowerUpsCollected,
                InfectionLevel = useCheckpoint ? state.CheckpointInfectionLevel : state.InfectionLevel,
                TotalBioTiles = state.TotalBioTiles,
                SeedersRemaining = useCheckpoint ? state.CheckpointSeedersRemaining : state.SeedersRemaining,
                DronesRemaining = useCheckpoint ? state.CheckpointDronesRemaining : state.DronesRemaining,
                MotherShipsRemaining = useCheckpoint ? state.CheckpointMotherShipsRemaining : state.MotherShipsRemaining,
                InitialSeeders = useCheckpoint ? state.CheckpointInitialSeeders : state.InitialSeeders,
                InitialDrones = useCheckpoint ? state.CheckpointInitialDrones : state.InitialDrones,
                InitialMotherShips = useCheckpoint ? state.CheckpointInitialMotherShips : state.InitialMotherShips,
                TotalShotsFired = useCheckpoint ? state.CheckpointTotalShotsFired : state.TotalShotsFired,
                TotalKills = useCheckpoint ? state.CheckpointTotalKills : state.TotalKills,
                TotalDeaths = useCheckpoint ? state.CheckpointTotalDeaths : state.TotalDeaths,
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
                CheckpointPlanetStyleBonusScore = state.CheckpointPlanetStyleBonusScore,
                CheckpointPlanetStyleBonusSceneIndex = state.CheckpointPlanetStyleBonusSceneIndex,
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
            state.PlanetStyleBonusScore = saved.PlanetStyleBonusScore;
            state.PlanetStyleBonusSceneIndex = saved.PlanetStyleBonusSceneIndex;
            state.SceneIndex = saved.SceneIndex;
            state.SimulationRound = saved.SimulationRound;
            state.CurrentSceneBiome = saved.SceneBiome;
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
            state.InitialSeeders = saved.InitialSeeders > 0 ? saved.InitialSeeders : saved.CheckpointInitialSeeders;
            state.InitialDrones = saved.InitialDrones > 0 ? saved.InitialDrones : saved.CheckpointInitialDrones;
            state.InitialMotherShips = saved.InitialMotherShips > 0 ? saved.InitialMotherShips : saved.CheckpointInitialMotherShips;
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
            state.CheckpointInitialSeeders = saved.CheckpointInitialSeeders;
            state.CheckpointInitialDrones = saved.CheckpointInitialDrones;
            state.CheckpointInitialMotherShips = saved.CheckpointInitialMotherShips;
            state.CheckpointPlanetStyleBonusScore = saved.CheckpointPlanetStyleBonusScore;
            state.CheckpointPlanetStyleBonusSceneIndex = saved.CheckpointPlanetStyleBonusSceneIndex;
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

        /// <summary>
        /// Resets a player's save to Scene1 with a clean progression state.
        /// Use this when a player should restart campaign flow from the first game scene.
        /// </summary>
        public static void ResetPlayerToScene1(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                return;

            var saved = LoadGameState(playerName) ?? new SavedGameState
            {
                PlayerName = playerName,
                Lives = 3,
                Health = 100f,
                MaxHealth = 100f,
                SavedAtUtc = DateTime.UtcNow.ToString("o")
            };

            saved.PlayerName = playerName;
            saved.SceneIndex = 1;
            saved.SceneBiome = Domain.SceneBiomeTypes.HillsWoods;
            saved.Score = 0;
            saved.PlanetStyleBonusScore = 0;
            saved.PlanetStyleBonusSceneIndex = 1;
            saved.WaveNumber = 1;
            saved.PowerUpsCollected = 0;
            saved.InfectionLevel = 0f;
            saved.TotalBioTiles = 0;
            saved.TotalShotsFired = 0;
            saved.TotalKills = 0;
            saved.TotalDeaths = 0;
            saved.Lives = 3;
            saved.Health = 100f;
            saved.MaxHealth = 100f;
            saved.HasCheckpoint = false;
            saved.SeedersRemaining = 0;
            saved.DronesRemaining = 0;
            saved.MotherShipsRemaining = 0;
            saved.CheckpointScore = 0;
            saved.CheckpointLives = 3;
            saved.CheckpointHealth = 100f;
            saved.CheckpointPowerUpsCollected = 0;
            saved.CheckpointSeedersRemaining = 0;
            saved.CheckpointDronesRemaining = 0;
            saved.CheckpointMotherShipsRemaining = 0;
            saved.CheckpointTotalShotsFired = 0;
            saved.CheckpointTotalKills = 0;
            saved.CheckpointTotalDeaths = 0;
            saved.CheckpointInfectionLevel = 0f;
            saved.CheckpointWaveNumber = 1;
            saved.CheckpointInitialSeeders = 0;
            saved.CheckpointInitialDrones = 0;
            saved.CheckpointInitialMotherShips = 0;
            saved.CheckpointPlanetStyleBonusScore = 0;
            saved.CheckpointPlanetStyleBonusSceneIndex = 1;
            saved.SavedAtUtc = DateTime.UtcNow.ToString("o");

            var filePath = PersistenceSetup.GetPlayerGameStateFilePath(playerName);
            Directory.CreateDirectory(PersistenceSetup.LocalFolder);
            EncryptionHelper.EnsureKeyFile(PersistenceSetup.LocalKeyFilePath);
            var json = JsonSerializer.Serialize(saved, JsonOptions);
            EncryptionHelper.EncryptToFile(filePath, json, PersistenceSetup.LocalKeyFilePath);

            // If this player is active in-memory, reset runtime state as well so
            // scene progression does not keep stale values until next restart.
            var state = GameState.GamePlayState;
            if (string.Equals(state.PlayerName, playerName, StringComparison.OrdinalIgnoreCase))
            {
                state.SceneIndex = 1;
                state.CurrentSceneBiome = Domain.SceneBiomeTypes.HillsWoods;
                state.Score = 0;
                state.PlanetStyleBonusScore = 0;
                state.PlanetStyleBonusSceneIndex = 1;
                state.WaveNumber = 1;
                state.PowerUpsCollected = 0;
                state.InfectionLevel = 0f;
                state.TotalBioTiles = 0;
                state.TotalShotsFired = 0;
                state.TotalKills = 0;
                state.TotalDeaths = 0;
                state.Lives = 3;
                state.Health = 100f;
                state.MaxHealth = 100f;
                state.HasCheckpoint = false;
            }
        }
    }
}
