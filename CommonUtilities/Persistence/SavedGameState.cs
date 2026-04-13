namespace CommonUtilities.Persistence
{
    /// <summary>
    /// Serializable snapshot of the player's game progress.
    /// Only the fields that matter for save/restore are included;
    /// runtime-only state (cooldowns, screen positions) is excluded.
    /// </summary>
    public sealed class SavedGameState
    {
        public string PlayerName { get; set; } = "";
        public int SceneIndex { get; set; }
        public long Score { get; set; }
        public int Lives { get; set; }
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public int WaveNumber { get; set; }
        public int PowerUpsCollected { get; set; }
        public float InfectionLevel { get; set; }
        public int TotalBioTiles { get; set; }
        public int SeedersRemaining { get; set; }
        public int DronesRemaining { get; set; }
        public int MotherShipsRemaining { get; set; }

        // Combat statistics
        public int TotalShotsFired { get; set; }
        public int TotalKills { get; set; }
        public int TotalDeaths { get; set; }

        // Checkpoint state
        public bool HasCheckpoint { get; set; }
        public long CheckpointScore { get; set; }
        public int CheckpointLives { get; set; }
        public float CheckpointHealth { get; set; }
        public int CheckpointPowerUpsCollected { get; set; }
        public int CheckpointSeedersRemaining { get; set; }
        public int CheckpointDronesRemaining { get; set; }
        public int CheckpointMotherShipsRemaining { get; set; }
        public int CheckpointTotalShotsFired { get; set; }
        public int CheckpointTotalKills { get; set; }
        public int CheckpointTotalDeaths { get; set; }
        public float CheckpointInfectionLevel { get; set; }
        public int CheckpointWaveNumber { get; set; }
        public int CheckpointInitialSeeders { get; set; }
        public int CheckpointInitialDrones { get; set; }
        public int CheckpointInitialMotherShips { get; set; }

        public string SavedAtUtc { get; set; } = "";
    }
}
