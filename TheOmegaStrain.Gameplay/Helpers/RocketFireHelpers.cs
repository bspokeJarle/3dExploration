using System;

namespace TheOmegaStrain.Gameplay.Helpers;

public static class RocketFireHelpers
{
    public const float MinimumCooldownSeconds = 10f;

    public static bool CanFire(
        bool isAttackShipVisible,
        float distanceToShip,
        float maxRange,
        float secondsSinceSuccessfulLaunch,
        float configuredCooldownSeconds,
        int activeRocketCount)
    {
        float requiredCooldown = MathF.Max(MinimumCooldownSeconds, configuredCooldownSeconds);

        return isAttackShipVisible &&
               maxRange > 0f &&
               distanceToShip >= 0f &&
               distanceToShip <= maxRange &&
               secondsSinceSuccessfulLaunch >= requiredCooldown &&
               activeRocketCount == 0;
    }
}
