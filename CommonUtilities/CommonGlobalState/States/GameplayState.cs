using System;
using CommonUtilities.CommonSetup;
using static CommonUtilities.WeaponHelpers.WeaponHelpers;

namespace Domain
{
    public enum GamePhase
    {
        Intro = 0,
        Playing = 1,
        Paused = 2,
        GameOver = 3,
        Outro = 4
    }

    /// <summary>
    /// TODO: Temporary gameplay state
    /// Keep this as pure data + tiny convenience methods.
    /// </summary>
    public sealed class GamePlayState
    {
        // -----------------------------
        // High-level game flow
        // -----------------------------
        public GamePhase Phase { get; set; } = GamePhase.Intro;

        public bool IsPaused => Phase == GamePhase.Paused;
        public bool IsPlaying => Phase == GamePhase.Playing;

        // -----------------------------
        // Player / ship core stats
        // -----------------------------
        public string PlayerName { get; set; } = "";
        public int Lives { get; set; } = 3;

        // Keep as float to allow smooth damage later (e.g., collision damage scaling)
        public float Health { get; set; } = 100f;
        public float Alt { get; set; } = 0f;
        public float Thrust { get; set; } = 0f;
        public float MaxHealth { get; set; } = 100f;

        public bool IsDead => Health <= 0f;

        // Optional: invulnerability after hit
        public float InvulnerableSecondsLeft { get; set; } = 0f;

        // -----------------------------
        // Score / progression
        // -----------------------------
        public long Score { get; set; } = 0;

        // Wave/level can drive seeder spawn intensity later
        public int WaveNumber { get; set; } = 1;

        // -----------------------------
        // Combat statistics
        // -----------------------------
        public int TotalShotsFired { get; set; } = 0;
        public int TotalKills { get; set; } = 0;
        public int TotalDeaths { get; set; } = 0;
        public float Accuracy => TotalShotsFired > 0 ? (float)TotalKills / TotalShotsFired : 0f;

        // -----------------------------
        // Checkpoint state (auto-saved on powerup/mothership kill)
        // -----------------------------

        /// <summary>
        /// Lightweight value type holding all checkpoint fields.
        /// Used to preserve checkpoint data across scene resets.
        /// </summary>
        public readonly record struct CheckpointSnapshot(
            long Score, int Lives, float Health, int PowerUpsCollected,
            int SeedersRemaining, int DronesRemaining, int MotherShipsRemaining,
            int TotalShotsFired, int TotalKills, int TotalDeaths,
            float InfectionLevel, int WaveNumber);

        public bool HasCheckpoint { get; set; } = false;
        public long CheckpointScore { get; set; } = 0;
        public int CheckpointLives { get; set; } = 3;
        public float CheckpointHealth { get; set; } = 100f;
        public int CheckpointPowerUpsCollected { get; set; } = 0;
        public int CheckpointSeedersRemaining { get; set; } = 0;
        public int CheckpointDronesRemaining { get; set; } = 0;
        public int CheckpointMotherShipsRemaining { get; set; } = 0;
        public int CheckpointTotalShotsFired { get; set; } = 0;
        public int CheckpointTotalKills { get; set; } = 0;
        public int CheckpointTotalDeaths { get; set; } = 0;
        public float CheckpointInfectionLevel { get; set; } = 0f;
        public int CheckpointWaveNumber { get; set; } = 1;

        /// <summary>
        /// Captures the current checkpoint fields into a snapshot value type.
        /// Call before ResetForNewGame to preserve checkpoint data.
        /// </summary>
        public CheckpointSnapshot CaptureCheckpointSnapshot() => new(
            CheckpointScore, CheckpointLives, CheckpointHealth, CheckpointPowerUpsCollected,
            CheckpointSeedersRemaining, CheckpointDronesRemaining, CheckpointMotherShipsRemaining,
            CheckpointTotalShotsFired, CheckpointTotalKills, CheckpointTotalDeaths,
            CheckpointInfectionLevel, CheckpointWaveNumber);

        /// <summary>
        /// Restores both current gameplay state and checkpoint fields from a snapshot.
        /// Deducts one life and increments TotalDeaths for the death that triggered the restart.
        /// </summary>
        public void ApplyCheckpointRestart(CheckpointSnapshot cp)
        {
            HasCheckpoint = true;

            // Restore current state from checkpoint (with death penalty)
            Score = cp.Score;
            Lives = Math.Max(0, cp.Lives - 1);
            Health = cp.Health;
            PowerUpsCollected = cp.PowerUpsCollected;
            TotalShotsFired = cp.TotalShotsFired;
            TotalKills = cp.TotalKills;
            TotalDeaths = cp.TotalDeaths + 1;
            InfectionLevel = cp.InfectionLevel;
            WaveNumber = cp.WaveNumber;

            // Preserve checkpoint fields for future deaths
            CheckpointScore = cp.Score;
            CheckpointLives = cp.Lives;
            CheckpointHealth = cp.Health;
            CheckpointPowerUpsCollected = cp.PowerUpsCollected;
            CheckpointSeedersRemaining = cp.SeedersRemaining;
            CheckpointDronesRemaining = cp.DronesRemaining;
            CheckpointMotherShipsRemaining = cp.MotherShipsRemaining;
            CheckpointTotalShotsFired = cp.TotalShotsFired;
            CheckpointTotalKills = cp.TotalKills;
            CheckpointTotalDeaths = cp.TotalDeaths;
            CheckpointInfectionLevel = cp.InfectionLevel;
            CheckpointWaveNumber = cp.WaveNumber;
        }

        // -----------------------------
        // Enemy tracking (alive counts for HUD)
        // -----------------------------
        public int DronesRemaining { get; set; } = 0;
        public int SeedersRemaining { get; set; } = 0;
        public int MotherShipsRemaining { get; set; } = 0;
        public int InitialDrones { get; set; } = 0;
        public int InitialSeeders { get; set; } = 0;
        public int InitialMotherShips { get; set; } = 0;

        // -----------------------------
        // Infection / lose condition (core to Omega Strain)
        // -----------------------------
        /// <summary>
        /// Raw count of infected tiles. Incremented by 1 per tile infected.
        /// </summary>
        public float InfectionLevel { get; set; } = 0f;

        /// <summary>
        /// Total number of bio tiles on the map (Grassland + Highlands).
        /// Set during scene loading after eco map generation.
        /// </summary>
        public int TotalBioTiles { get; set; } = 0;

        /// <summary>
        /// Current infection as a percentage (0..100).
        /// </summary>
        public float InfectionPercent => TotalBioTiles > 0 ? (InfectionLevel / TotalBioTiles) * 100f : 0f;

        /// <summary>
        /// Infection threshold percentage (0..100). When InfectionPercent
        /// reaches this value, the planet is lost. Configurable per level.
        /// </summary>
        public float InfectionCriticalMass { get; set; } = 100f;

        public bool IsInfectionCritical => TotalBioTiles > 0 && InfectionPercent >= InfectionCriticalMass;

        /// <summary>
        /// Number of tiles infected per infection event. 1 = primary tile only
        /// (no extra spread). Higher values cause each infection to also spread
        /// to neighboring edge tiles. Configurable per scene for difficulty.
        /// </summary>
        public int InfectionSpreadRate { get; set; } = 1;

        /// <summary>
        /// Offscreen seeder speed multiplier. Configured per scene.
        /// </summary>
        public int SeederOffscreenSpeedFactor { get; set; } = 6;

        /// <summary>
        /// Delay in seconds before a seeder-infected tile cascades infection
        /// to its neighbors. Configured per scene. 0 or negative = disabled.
        /// </summary>
        public float LocalInfectionSpreadDelaySec { get; set; } = 1.0f;

        /// <summary>
        /// Maximum world-unit distance from an alive seeder for cascading
        /// infection to continue. 0 or negative = unlimited range.
        /// </summary>
        public float LocalInfectionSpreadRadius { get; set; } = 10000f;

        // -----------------------------
        // Weapons (simple, but practical)
        // -----------------------------
        public WeaponType SelectedWeapon { get; set; } = WeaponType.Bullet;
        public string ActivePowerup { get; set; } = "BULLET";

        // -----------------------------
        // MotherShip health bar (in-world, follows the object)
        // -----------------------------
        public float MotherShipHealthPercent { get; set; } = 1f;
        public float MotherShipScreenX { get; set; } = 0f;
        public float MotherShipScreenY { get; set; } = 0f;
        public bool ShowMotherShipHealthBar { get; set; } = false;
        public bool MotherShipIsOnScreen { get; set; } = false;

        // MotherShip ram warning (flashing reticle before charge)
        public bool MotherShipRamWarningActive { get; set; } = false;
        public float MotherShipRamWarningScreenX { get; set; } = 0f;
        public float MotherShipRamWarningScreenY { get; set; } = 0f;

        // PowerUp progression: each collected PowerUp unlocks the next weapon tier
        public int PowerUpsCollected { get; set; } = 0;
        public bool IsDecoyUnlocked => PowerUpsCollected >= 1;
        public bool IsLazerUnlocked => PowerUpsCollected >= 2;

        public int LaserAmmo { get; set; } = -1;   // -1 means infinite
        public int RocketAmmo { get; set; } = 10;
        public int BulletAmmo { get; set; } = -1;  // -1 means infinite

        // Cooldowns (seconds)
        public float LaserCooldownLeft { get; set; } = 0f;
        public float RocketCooldownLeft { get; set; } = 0f;
        public float BulletCooldownLeft { get; set; } = 0f;

        public float LaserCooldownSeconds { get; set; } = 0.333f;
        public float RocketCooldownSeconds { get; set; } = 0.65f;
        public float BulletCooldownSeconds { get; set; } = 0.15f;

        public bool CanFireLaser => !IsPaused && LaserCooldownLeft <= 0f && (LaserAmmo != 0);
        public bool CanFireRocket => !IsPaused && RocketCooldownLeft <= 0f && RocketAmmo > 0;
        public bool CanFireBullet => !IsPaused && BulletCooldownLeft <= 0f && (BulletAmmo != 0);

        // -----------------------------
        // Convenience / deterministic update
        // -----------------------------
        public void Update(float dtSeconds)
        {
            if (dtSeconds <= 0f) return;

            if (InvulnerableSecondsLeft > 0f)
            {
                InvulnerableSecondsLeft -= dtSeconds;
                if (InvulnerableSecondsLeft < 0f) InvulnerableSecondsLeft = 0f;
            }

            if (LaserCooldownLeft > 0f)
            {
                LaserCooldownLeft -= dtSeconds;
                if (LaserCooldownLeft < 0f) LaserCooldownLeft = 0f;
            }

            if (RocketCooldownLeft > 0f)
            {
                RocketCooldownLeft -= dtSeconds;
                if (RocketCooldownLeft < 0f) RocketCooldownLeft = 0f;
            }

            if (BulletCooldownLeft > 0f)
            {
                BulletCooldownLeft -= dtSeconds;
                if (BulletCooldownLeft < 0f) BulletCooldownLeft = 0f;
            }

            // If you want infection to tick automatically during play:
            // if (Phase == GamePhase.Playing) InfectionLevel = MathF.Min(1f, InfectionLevel + dtSeconds * 0.001f);
        }

        public void ApplyDamage(float amount, float invulnerableSeconds = 0.25f)
        {
            if (amount <= 0f) return;
            if (InvulnerableSecondsLeft > 0f) return;
            if (Phase == GamePhase.GameOver || Phase == GamePhase.Outro) return;

            Health -= amount;
            if (Health < 0f) Health = 0f;

            InvulnerableSecondsLeft = Math.Max(InvulnerableSecondsLeft, invulnerableSeconds);
        }

        public void Heal(float amount)
        {
            if (amount <= 0f) return;
            Health += amount;
            if (Health > MaxHealth) Health = MaxHealth;
        }

        public void ConsumeLifeAndRespawn()
        {
            TotalDeaths++;
            Score = Math.Max(0, Score - GameSetup.DeathScorePenalty);

            if (HasCheckpoint)
            {
                RestoreCheckpoint();
            }

            if (Lives > 0) Lives--;

            Health = MaxHealth;
            InvulnerableSecondsLeft = 1.0f;

            if (Lives < 0) Lives = 0;
        }

        public void TriggerGameOver()
        {
            Phase = GamePhase.GameOver;
        }

        public bool TryFireSelectedWeapon()
        {
            if (SelectedWeapon == WeaponType.Lazer && CanFireLaser)
            {
                LaserCooldownLeft = LaserCooldownSeconds;
                if (LaserAmmo > 0) LaserAmmo--;
                TotalShotsFired++;
                return true;
            }

            if (SelectedWeapon == WeaponType.Rocket && CanFireRocket)
            {
                RocketCooldownLeft = RocketCooldownSeconds;
                RocketAmmo--;
                if (RocketAmmo < 0) RocketAmmo = 0;
                TotalShotsFired++;
                return true;
            }

            if (SelectedWeapon == WeaponType.Bullet && CanFireBullet)
            {
                BulletCooldownLeft = BulletCooldownSeconds;
                if (BulletAmmo > 0) BulletAmmo--;
                TotalShotsFired++;
                return true;
            }

            return false;
        }

        public void ResetForNewGame()
        {
            Phase = GamePhase.Intro;

            Lives = 3;
            MaxHealth = 100f;
            Health = 100f;
            InvulnerableSecondsLeft = 0f;

            Score = 0;
            WaveNumber = 1;

            TotalShotsFired = 0;
            TotalKills = 0;
            TotalDeaths = 0;

            HasCheckpoint = false;
            CheckpointScore = 0;
            CheckpointLives = 3;
            CheckpointHealth = 100f;
            CheckpointPowerUpsCollected = 0;
            CheckpointSeedersRemaining = 0;
            CheckpointDronesRemaining = 0;
            CheckpointMotherShipsRemaining = 0;
            CheckpointTotalShotsFired = 0;
            CheckpointTotalKills = 0;
            CheckpointTotalDeaths = 0;
            CheckpointInfectionLevel = 0f;
            CheckpointWaveNumber = 1;

            DronesRemaining = 0;
            SeedersRemaining = 0;
            MotherShipsRemaining = 0;
            InitialDrones = 0;
            InitialSeeders = 0;
            InitialMotherShips = 0;

            InfectionLevel = 0f;
            TotalBioTiles = 0;
            InfectionCriticalMass = 100f; // safe default; overridden by scene threshold
            InfectionSpreadRate = 1;
            SeederOffscreenSpeedFactor = 6;
            LocalInfectionSpreadDelaySec = 1.0f;
            LocalInfectionSpreadRadius = 10000f;

            MotherShipHealthPercent = 1f;
            MotherShipScreenX = 0f;
            MotherShipScreenY = 0f;
            ShowMotherShipHealthBar = false;
            MotherShipIsOnScreen = false;

            MotherShipRamWarningActive = false;
            MotherShipRamWarningScreenX = 0f;
            MotherShipRamWarningScreenY = 0f;

            SelectedWeapon = WeaponType.Bullet;
            ActivePowerup = "BULLET";
            PowerUpsCollected = 0;
            LaserAmmo = -1;
            RocketAmmo = 10;
            BulletAmmo = -1;

            LaserCooldownLeft = 0f;
            RocketCooldownLeft = 0f;
            BulletCooldownLeft = 0f;
        }

        /// <summary>
        /// Awards score for killing an enemy and increments the kill counter.
        /// </summary>
        public void RecordKill(string enemyType)
        {
            TotalKills++;
            Score += GameSetup.GetKillScore(enemyType);
        }

        /// <summary>
        /// Snapshots the current game state as a checkpoint.
        /// Triggered when a powerup enemy or MotherShip is killed.
        /// </summary>
        public void SaveCheckpoint()
        {
            HasCheckpoint = true;
            CheckpointScore = Score;
            CheckpointLives = Lives;
            CheckpointHealth = Health;
            CheckpointPowerUpsCollected = PowerUpsCollected;
            CheckpointSeedersRemaining = SeedersRemaining;
            CheckpointDronesRemaining = DronesRemaining;
            CheckpointMotherShipsRemaining = MotherShipsRemaining;
            CheckpointTotalShotsFired = TotalShotsFired;
            CheckpointTotalKills = TotalKills;
            CheckpointTotalDeaths = TotalDeaths;
            CheckpointInfectionLevel = InfectionLevel;
            CheckpointWaveNumber = WaveNumber;
        }

        /// <summary>
        /// Restores game progress to the last checkpoint.
        /// TotalDeaths is NOT restored so the death count persists.
        /// Called automatically from <see cref="ConsumeLifeAndRespawn"/>.
        /// </summary>
        public void RestoreCheckpoint()
        {
            if (!HasCheckpoint) return;

            Score = CheckpointScore;
            Lives = CheckpointLives;
            PowerUpsCollected = CheckpointPowerUpsCollected;
            SeedersRemaining = CheckpointSeedersRemaining;
            DronesRemaining = CheckpointDronesRemaining;
            MotherShipsRemaining = CheckpointMotherShipsRemaining;
            TotalShotsFired = CheckpointTotalShotsFired;
            TotalKills = CheckpointTotalKills;
            InfectionLevel = CheckpointInfectionLevel;
            WaveNumber = CheckpointWaveNumber;
        }

        /// <summary>
        /// Calculates the final score including an accuracy bonus.
        /// Call at game end (victory or final game over) for highscore submission.
        /// </summary>
        public long CalculateFinalScore()
        {
            long accuracyBonus = (long)(Score * Accuracy * GameSetup.AccuracyBonusMultiplier);
            return Math.Max(0, Score + accuracyBonus);
        }

        public void UpdateAltitude(float height, float minHeight, float maxHeight)
        {
            if (maxHeight <= minHeight)
            {
                Alt = 0f;
                return;
            }

            float normalized = (height - minHeight) / (maxHeight - minHeight);
            Alt = MathF.Min(MathF.Max(normalized, 0f), 1f);
        }
    }
}