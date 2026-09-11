using System;
using TheOmegaStrain.Common.CommonGlobalState.States;
using TheOmegaStrain.Domain;

namespace TheOmegaStrain.Gameplay.Physics
{
    /// <summary>
    /// Applies player-selected handling values to the player's physics instance.
    /// This deliberately leaves biome modifiers and every non-player object unchanged.
    /// </summary>
    public static class ShipFlightPhysicsSettings
    {
        private const float MinimumHoverDurationFactor = 0.25f;

        public static void Apply(IPhysics physics, GameSettingsState settings)
        {
            settings.Normalize();
            physics.CoastingRetention = settings.ShipCoastingRetention;
            physics.ThrustRampRate = settings.ShipThrustRampRate;
            physics.ThrustSpeedMultiplier = settings.ShipThrustSpeedMultiplier;
            physics.GravityPullMultiplier = settings.ShipGravityPullMultiplier;
            physics.HoverFloatDuration = settings.ShipHoverFloatDuration;
            physics.HoverMinGravityScale = 0f;
        }

        public static float CalculateReleaseHoverDuration(
            float configuredMaximumSeconds,
            float inertiaX,
            float inertiaZ,
            float maxInertia)
        {
            float horizontalSpeed = MathF.Sqrt(inertiaX * inertiaX + inertiaZ * inertiaZ);
            float speedRatio = maxInertia <= 0f
                ? 0f
                : Math.Clamp(horizontalSpeed / maxInertia, 0f, 1f);

            // Preserve a brief transition at low speed, while fast flight gets
            // the full configured coast/hover window.
            float durationFactor = MinimumHoverDurationFactor
                + (1f - MinimumHoverDurationFactor) * speedRatio;
            return MathF.Max(0f, configuredMaximumSeconds) * durationFactor;
        }
    }
}
