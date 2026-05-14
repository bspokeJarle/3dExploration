using System.Collections.Generic;

namespace CommonUtilities.CommonSetup
{
    public static class TerrainAvoidanceSetup
    {
        public const float DefaultRecoveryDurationSeconds = 0.75f;
        public const float DefaultLiftSpeed = 220f;
        public const float DefaultHorizontalSpeed = 320f;

        public const float HeavyRecoveryDurationSeconds = 1.15f;
        public const float HeavyLiftSpeed = 170f;
        public const float HeavyHorizontalSpeed = 220f;

        private static readonly HashSet<string> TerrainObstacleNames = new()
        {
            "Surface",
            "Tower",
            "Tree",
            "LargePalm",
            "SmallPalm",
            "BambooHut",
            "House"
        };

        private static readonly HashSet<string> AvoidanceCapableAiNames = new()
        {
            "Seeder",
            "KamikazeDrone",
            "ZeppelinBomber",
            "MotherShipSmall",
            "MotherShipMedium",
            "MotherShipLarge",
            "SpaceSwan"
        };

        private static readonly HashSet<string> HeavyAiNames = new()
        {
            "MotherShipSmall",
            "MotherShipMedium",
            "MotherShipLarge"
        };

        public static bool IsTerrainObstacle(string? objectName) =>
            !string.IsNullOrEmpty(objectName) && TerrainObstacleNames.Contains(objectName);

        public static bool IsAvoidanceCapableAi(string? objectName) =>
            !string.IsNullOrEmpty(objectName) && AvoidanceCapableAiNames.Contains(objectName);

        public static bool IsHeavyAvoidanceAi(string? objectName) =>
            !string.IsNullOrEmpty(objectName) && HeavyAiNames.Contains(objectName);
    }
}
