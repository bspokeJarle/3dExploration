namespace CommonUtilities.CommonSetup
{
    public static class GameSetup
    {
        public static float CollisionMarginX { get; set; } = 10f;
        public static float CollisionMarginY { get; set; } = 10f;
        public static float CollisionMarginZ { get; set; } = 20f;
        public static float MaxKamikazeShipCenterCollisionDistance { get; set; } = 150f;
        public static int MaxActiveDecoys { get; set; } = 3;
        public static int ShipShadowSubdivisionLevels { get; set; } = 2;

        // Kamikaze drone hunt timing
        public static int KamikazeDroneMinHuntDelay { get; set; } = 15;
        public static int KamikazeDroneMaxHuntDelay { get; set; } = 165;
        public static float KamikazeDroneProximityHuntDistance { get; set; } = 10_000f;

        // Decoy blast radius: exploding decoys damage nearby objects within this distance.
        // Viewport diagonal is ~960 world units; 850 covers most of the visible screen.
        public static float DecoyBlastRadius { get; set; } = 850f;

        // Scoring: base points awarded per enemy kill
        public static int SeederKillScore { get; set; } = 100;
        public static int KamikazeDroneKillScore { get; set; } = 50;
        public static int MotherShipSmallKillScore { get; set; } = 500;
        public static int DefaultKillScore { get; set; } = 25;

        // Score penalty deducted each time the player dies
        public static int DeathScorePenalty { get; set; } = 200;

        // End-of-game accuracy bonus: finalBonus = baseKillScore * accuracy * this multiplier
        public static float AccuracyBonusMultiplier { get; set; } = 2.0f;

        /// <summary>
        /// Returns the score awarded for killing an enemy of the given type.
        /// </summary>
        public static int GetKillScore(string enemyType) => enemyType switch
        {
            "Seeder" => SeederKillScore,
            "KamikazeDrone" => KamikazeDroneKillScore,
            "MotherShipSmall" => MotherShipSmallKillScore,
            _ => DefaultKillScore
        };

        /// <summary>
        /// Returns true if the killed enemy should trigger a checkpoint auto-save.
        /// Checkpoints are tied to enemies with powerups and all MotherShip types.
        /// </summary>
        public static bool IsCheckpointEnemy(string objectName, bool hasPowerUp)
        {
            if (objectName.StartsWith("MotherShip")) return true;
            return hasPowerUp;
        }
    }
}
