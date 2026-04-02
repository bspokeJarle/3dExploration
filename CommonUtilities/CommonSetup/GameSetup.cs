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
    }
}
