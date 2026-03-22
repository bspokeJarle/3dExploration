using System;
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
        // Infection / lose condition (core to Omega Strain)
        // -----------------------------
        /// <summary>
        /// 0..1. When reaching CriticalMass, the game is lost.
        /// </summary>
        public float InfectionLevel { get; set; } = 0f;

        /// <summary>
        /// 0..1. Default 1.0 means "full infection = lose".
        /// You can set e.g. 0.85f if you want earlier fail.
        /// </summary>
        public float InfectionCriticalMass { get; set; } = 100f;

        public bool IsInfectionCritical => InfectionLevel >= InfectionCriticalMass;

        // -----------------------------
        // Weapons (simple, but practical)
        // -----------------------------
        public WeaponType SelectedWeapon { get; set; } = WeaponType.Lazer;
        public string ActivePowerup { get; set; } = "LAZER";

        public int LaserAmmo { get; set; } = -1;   // -1 means infinite
        public int RocketAmmo { get; set; } = 10;

        // Cooldowns (seconds)
        public float LaserCooldownLeft { get; set; } = 0f;
        public float RocketCooldownLeft { get; set; } = 0f;

        public float LaserCooldownSeconds { get; set; } = 0.08f;
        public float RocketCooldownSeconds { get; set; } = 0.65f;

        public bool CanFireLaser => !IsPaused && LaserCooldownLeft <= 0f && (LaserAmmo != 0);
        public bool CanFireRocket => !IsPaused && RocketCooldownLeft <= 0f && RocketAmmo > 0;

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
            if (Lives > 0) Lives--;

            // Simple respawn defaults (tweak later)
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
                return true;
            }

            if (SelectedWeapon == WeaponType.Rocket && CanFireRocket)
            {
                RocketCooldownLeft = RocketCooldownSeconds;
                RocketAmmo--;
                if (RocketAmmo < 0) RocketAmmo = 0;
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

            InfectionLevel = 0f;
            InfectionCriticalMass = 1.0f;

            SelectedWeapon = WeaponType.Lazer;
            ActivePowerup = "LAZER";
            LaserAmmo = -1;
            RocketAmmo = 10;

            LaserCooldownLeft = 0f;
            RocketCooldownLeft = 0f;
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