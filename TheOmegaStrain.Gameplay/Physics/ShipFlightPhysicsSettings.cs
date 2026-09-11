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
    }
}
