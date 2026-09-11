using TheOmegaStrain.Common.CommonGlobalState;
using TheOmegaStrain.Common.CommonSetup;
using TheOmegaStrain.Common.OmegaEngineAdapters;
using TheOmegaStrain.Domain;

namespace TheOmegaStrain.Gameplay.Helpers
{
    internal static class KamikazeDroneMovementHelpers
    {
        // Keep the complete drone body clear of the rendered terrain. This is a
        // navigation floor, not a visual offset: homing remains free above it.
        internal const float MinimumSurfaceClearance = 120f;

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

        internal static void KeepAboveVisibleSurface(I3dObject obj)
        {
            if (!obj.IsOnScreen || obj is not OmegaObject3D omegaObject || obj.WorldPosition == null)
                return;

            var surface = obj.ParentSurface;
            var surfaceViewport = GameState.SurfaceState.SurfaceViewportObject;
            var surfaceOffsets = surfaceViewport?.ObjectOffsets;
            var objectOffsets = obj.ObjectOffsets;
            var rotatedTiles = surface?.RotatedSurfaceTriangles;
            var localWorld = omegaObject.GetLocalWorldPosition();

            if (surfaceOffsets == null || objectOffsets == null || localWorld == null ||
                rotatedTiles == null || rotatedTiles.Count == 0)
                return;

            float objectScreenX = -localWorld.x + objectOffsets.x;
            float objectScreenZ = localWorld.z + objectOffsets.z;
            float surfaceLocalX = objectScreenX - surfaceOffsets.x;
            float surfaceLocalZ = objectScreenZ - surfaceOffsets.z;

            if (!SurfaceGroundProjectionHelpers.IsWithinSurfaceBounds(
                    rotatedTiles,
                    surfaceLocalX,
                    surfaceLocalZ))
                return;

            if (!SurfaceGroundProjectionHelpers.TryGetSurfaceGroundPoint(
                    rotatedTiles,
                    surfaceLocalX,
                    surfaceLocalZ,
                    out _,
                    out float groundY,
                    out _))
                return;

            // ObjectOffsets has not received the camera-angle correction yet.
            // Include it here so the AI floor agrees with the final rendered Y.
            float pitchCorrection = SurfacePositionSyncHelpers.GetSurfacePitchHeightCorrectionY(
                obj,
                WorldViewSetup.SurfacePitchDegrees);
            float objectScreenY = -localWorld.y + objectOffsets.y + pitchCorrection;
            float groundScreenY = surfaceOffsets.y + groundY;
            float clearance = groundScreenY - objectScreenY;

            if (clearance >= MinimumSurfaceClearance)
                return;

            // In Omega, smaller world Y renders higher on screen. Correct only
            // the proposed homing result; never replace or accumulate offsets.
            float requiredLift = MinimumSurfaceClearance - clearance;
            obj.WorldPosition.y -= requiredLift;
        }
    }
}
