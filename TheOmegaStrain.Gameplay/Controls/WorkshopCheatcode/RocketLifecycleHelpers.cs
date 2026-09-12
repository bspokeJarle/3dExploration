namespace TheOmegaStrain.Gameplay.Controls;

public enum RocketLifecycleResult
{
    None,
    HitShip,
    HitSurface,
    OutOfBounds,
    MaxRange,
    LifetimeExpired
}

public static class RocketLifecycleHelpers
{
    public static RocketLifecycleResult Classify(
        bool hitShip,
        bool hitSurface,
        bool isOutOfBounds,
        float distanceTraveled,
        float maxRange,
        float elapsedLifetimeSeconds,
        float maxLifetimeSeconds)
    {
        if (hitShip)
            return RocketLifecycleResult.HitShip;
        if (hitSurface)
            return RocketLifecycleResult.HitSurface;
        if (isOutOfBounds)
            return RocketLifecycleResult.OutOfBounds;
        if (maxRange >= 0f && distanceTraveled >= maxRange)
            return RocketLifecycleResult.MaxRange;
        if (maxLifetimeSeconds >= 0f && elapsedLifetimeSeconds >= maxLifetimeSeconds)
            return RocketLifecycleResult.LifetimeExpired;

        return RocketLifecycleResult.None;
    }
}
