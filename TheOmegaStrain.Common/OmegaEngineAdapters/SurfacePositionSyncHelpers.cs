using TheOmegaStrain.Common.CommonGlobalState;
using TheOmegaStrain.Common.CommonSetup;
using TheOmegaStrain.Domain;

using RetroMesh.Engine;

namespace TheOmegaStrain.Common.OmegaEngineAdapters
{
    public static class SurfacePositionSyncHelpers
    {
        public const float DefaultEnemySurfaceSyncFactorY = 2.5f;

        private static Vector3 CreateVector(float x, float y, float z) => new(x, y, z);

        public static Vector3 GetSurfaceAlignedWorldPosition(I3dObject obj)
        {
            var surfaceOffsets = GameState.SurfaceState.SurfaceViewportObject?.ObjectOffsets;
            return WorldPositionMath.GetSurfaceAlignedWorldPosition(obj, surfaceOffsets, CreateVector);
        }

        public static Vector3 GetSurfaceSyncedObjectOffsets(I3dObject obj, float initialOffsetY, float syncFactorY = DefaultEnemySurfaceSyncFactorY)
        {
            return WorldPositionMath.GetSurfaceSyncedObjectOffsets(
                obj.ObjectOffsets,
                GameState.SurfaceState.GlobalMapPosition.y,
                initialOffsetY,
                syncFactorY,
                CreateVector);
        }

        /// <summary>
        /// Returns the extra screen-Y correction relative to Omega's original 70°
        /// presentation. Existing object offsets were authored for that angle, so
        /// applying the complete plane slope would count the original tilt twice.
        /// The viewport centre (surface-local Z = 0) is the neutral point, and
        /// terrain height is deliberately excluded so flying objects do not bob.
        /// </summary>
        public static float GetSurfacePitchHeightCorrectionY(float surfaceLocalZ, float pitchDegrees)
        {
            float pitchRadians = pitchDegrees * (MathF.PI / 180f);
            float originalPitchRadians = OmegaWorldViewSetup.OriginalWorldPitchDegrees * (MathF.PI / 180f);
            float tangent = MathF.Tan(pitchRadians);
            float originalTangent = MathF.Tan(originalPitchRadians);

            if (!float.IsFinite(surfaceLocalZ)
                || !float.IsFinite(tangent)
                || !float.IsFinite(originalTangent)
                || MathF.Abs(tangent) < 0.0001f
                || MathF.Abs(originalTangent) < 0.0001f)
                return 0f;

            // A flat X-rotated plane has y/z = cot(pitch). Subtract the
            // original cotangent because that slope is already represented by
            // the offsets authored for the original Omega camera.
            return surfaceLocalZ * ((1f / tangent) - (1f / originalTangent));
        }

        /// <summary>
        /// Calculates an object's Z relative to the visible surface, then converts
        /// that local position to the matching ground-plane Y correction.
        /// </summary>
        public static float GetSurfacePitchHeightCorrectionY(I3dObject obj, float pitchDegrees)
        {
            var surfaceOffsets = GameState.SurfaceState.SurfaceViewportObject?.ObjectOffsets;
            var worldPosition = obj.WorldPosition;
            float localWorldZ = worldPosition == null || WorldPositionMath.IsOrigin(worldPosition)
                ? 0f
                : GameState.SurfaceState.GlobalMapPosition.z - worldPosition.z;
            float surfaceLocalZ = localWorldZ
                                  + (obj.ObjectOffsets?.z ?? 0f)
                                  - (surfaceOffsets?.z ?? 0f);

            return GetSurfacePitchHeightCorrectionY(surfaceLocalZ, pitchDegrees);
        }

        public static Vector3 GetShipWorldPosition(float shipOffsetY, float zoom)
        {
            var globalMapPosition = GameState.SurfaceState.GlobalMapPosition;
            return WorldPositionMath.GetShipWorldPosition(
                globalMapPosition,
                ScreenSetup.screenSizeX,
                ScreenSetup.screenSizeY,
                shipOffsetY,
                zoom,
                CreateVector);
        }

        public static Vector3? GetMinimapMarkerWorldPosition(I3dObject obj)
        {
            int viewportCenterOffset = (SurfaceSetup.viewPortSize * SurfaceSetup.tileSize) / 2;
            return WorldPositionMath.GetWorldPositionWithXOffset(obj, viewportCenterOffset, CreateVector);
        }

        public static Vector3? GetGuidanceTargetWorldPosition(I3dObject obj)
        {
            return WorldPositionMath.GetWorldPositionWithXOffset(obj, ScreenSetup.screenSizeX / 2f, CreateVector);
        }

        public static Vector3 GetShipRamTargetWorldPosition(I3dObject enemyObject)
        {
            if (GameState.ShipState.ShipCrashCenterWorldPosition is Vector3 shipCrashCenterWorldPosition)
            {
                return shipCrashCenterWorldPosition;
            }

            var globalMapPosition = GameState.SurfaceState.GlobalMapPosition;
            var enemyOffsets = enemyObject.ObjectOffsets;
            var shipOffsets = GameState.ShipState.ShipObjectOffsets;

            return WorldPositionMath.GetShipRamTargetWorldPosition(
                globalMapPosition,
                enemyOffsets,
                shipOffsets,
                CreateVector);
        }

        public static Vector3 GetObjectCrashCenterWorldPosition(I3dObject obj)
        {
            Vector3 basePosition;
            if (obj.ObjectName == "Ship" && GameState.ShipState.ShipWorldPosition is Vector3 shipWorldPosition)
            {
                basePosition = shipWorldPosition;
            }
            else
            {
                basePosition = GetSurfaceAlignedWorldPosition(obj);
            }

            return WorldPositionMath.GetObjectCrashCenterWorldPosition(obj, basePosition, CreateVector);
        }
    }
}
