using TheOmegaStrain.Common.CommonGlobalState;
using TheOmegaStrain.Common.CommonSetup;
using TheOmegaStrain.Common.OmegaEngineAdapters;
using TheOmegaStrain.Domain;

namespace TheOmegaStrain.Gameplay.Helpers
{
    internal static class KamikazeDroneMovementHelpers
    {
        internal static Vector3 ToVector3(IVector3? v)
        {
            if (v is null)
            {
                return new Vector3();
            }

            return new Vector3
            {
                x = v.x,
                y = v.y,
                z = v.z
            };
        }

        internal static Vector3 GetDroneCrashCenterWorldPosition(I3dObject obj)
        {
            return ToVector3(ObjectCollisionGeometry.GetObjectCrashCenterWorldPosition(
                obj,
                includeObjectOffsets: true));
        }

        internal static Vector3 GetNavigationCrashCenterWorldPosition(I3dObject obj)
        {
            return ToVector3(ObjectCollisionGeometry.GetObjectCrashCenterWorldPosition(
                obj,
                includeObjectOffsets: false));
        }

        internal static Vector3 GetCompensatedHuntTargetWorldPosition(I3dObject hunter, I3dObject target)
        {
            return ToVector3(ObjectCollisionGeometry.GetCompensatedHuntTargetWorldPosition(hunter, target));
        }

        internal static Vector3? GetShipCrashCenterWorldPosition()
        {
            if (GameState.ShipState?.ShipCrashCenterWorldPosition is Vector3 shipCrashCenter)
            {
                return new Vector3
                {
                    x = shipCrashCenter.x - (TheOmegaStrain.Common.CommonSetup.ScreenSetup.screenSizeX / 2f),
                    y = shipCrashCenter.y,
                    z = shipCrashCenter.z - (TheOmegaStrain.Common.CommonSetup.ScreenSetup.screenSizeY / 2f)
                };
            }

            if (GameState.ShipState?.ShipWorldPosition is Vector3 shipWorldPosition)
            {
                return new Vector3
                {
                    x = shipWorldPosition.x - (TheOmegaStrain.Common.CommonSetup.ScreenSetup.screenSizeX / 2f),
                    y = shipWorldPosition.y,
                    z = shipWorldPosition.z - (TheOmegaStrain.Common.CommonSetup.ScreenSetup.screenSizeY / 2f)
                };
            }

            return null;
        }

        internal static float GetApproximateCrashRadius(I3dObject obj)
        {
            return ObjectCollisionGeometry.GetApproximateCrashRadius(obj);
        }

    }
}
