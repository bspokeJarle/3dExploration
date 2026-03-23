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
    }
}
