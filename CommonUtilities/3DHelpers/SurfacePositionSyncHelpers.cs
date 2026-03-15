using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonSetup;
using Domain;
using static Domain._3dSpecificsImplementations;

namespace CommonUtilities._3DHelpers
{
    public static class SurfacePositionSyncHelpers
    {
        private static readonly _3dRotationCommon Rotate3d = new();
        public const float DefaultEnemySurfaceSyncFactorY = 2.5f;

        private static Vector3 GetLocalCrashCenter(I3dObject obj)
        {
            if (obj.CrashBoxes == null || obj.CrashBoxes.Count == 0)
            {
                return new Vector3();
            }

            var localPoints = new List<Vector3>();
            foreach (var box in obj.CrashBoxes)
            {
                foreach (var point in box)
                {
                    localPoints.Add((Vector3)point);
                }
            }

            return localPoints.Count > 0 ? Common3dObjectHelpers.GetCenterOfBox(localPoints) : new Vector3();
        }

        private static Vector3 RotateLocalCrashCenter(Vector3 localCrashCenter, IVector3? rotation)
        {
            if (rotation is not Vector3 rotationVector)
            {
                return localCrashCenter;
            }

            var rotatedPoint = (Vector3)Rotate3d.RotatePoint(rotationVector.z, localCrashCenter, 'Z');
            rotatedPoint = (Vector3)Rotate3d.RotatePoint(rotationVector.y, rotatedPoint, 'Y');
            rotatedPoint = (Vector3)Rotate3d.RotatePoint(rotationVector.x, rotatedPoint, 'X');
            return rotatedPoint;
        }

        public static Vector3 GetSurfaceAlignedWorldPosition(I3dObject obj)
        {
            var worldPosition = obj.WorldPosition;
            if (worldPosition == null)
            {
                return new Vector3();
            }

            var objectOffsets = obj.ObjectOffsets;
            var surfaceOffsets = GameState.SurfaceState.SurfaceViewportObject?.ObjectOffsets;

            float deltaX = (surfaceOffsets?.x ?? 0f) - (objectOffsets?.x ?? 0f);
            float deltaZ = (surfaceOffsets?.z ?? 0f) - (objectOffsets?.z ?? 0f);

            return new Vector3
            {
                x = worldPosition.x - deltaX,
                y = worldPosition.y,
                z = worldPosition.z - deltaZ
            };
        }

        public static Vector3 GetSurfaceSyncedObjectOffsets(I3dObject obj, float initialOffsetY, float syncFactorY = DefaultEnemySurfaceSyncFactorY)
        {
            var objectOffsets = obj.ObjectOffsets;

            return new Vector3
            {
                x = objectOffsets?.x ?? 0f,
                y = GameState.SurfaceState.GlobalMapPosition.y * syncFactorY + initialOffsetY,
                z = objectOffsets?.z ?? 0f
            };
        }

        public static Vector3 GetShipWorldPosition(float shipOffsetY, float zoom)
        {
            var globalMapPosition = GameState.SurfaceState.GlobalMapPosition;

            return new Vector3
            {
                x = globalMapPosition.x + (ScreenSetup.screenSizeX / 2f),
                y = globalMapPosition.y + shipOffsetY,
                z = globalMapPosition.z + (ScreenSetup.screenSizeY / 2f) + zoom
            };
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

            return new Vector3
            {
                x = globalMapPosition.x + (shipOffsets?.x ?? 0f) - (enemyOffsets?.x ?? 0f),
                y = globalMapPosition.y + (shipOffsets?.y ?? 0f) - (enemyOffsets?.y ?? 0f),
                z = globalMapPosition.z + (enemyOffsets?.z ?? 0f) - (shipOffsets?.z ?? 0f)
            };
        }

        public static Vector3 GetObjectCrashCenterWorldPosition(I3dObject obj)
        {
            var localCrashCenter = RotateLocalCrashCenter(GetLocalCrashCenter(obj), obj.Rotation);

            Vector3 basePosition;
            if (obj.ObjectName == "Ship" && GameState.ShipState.ShipWorldPosition is Vector3 shipWorldPosition)
            {
                basePosition = shipWorldPosition;
            }
            else
            {
                basePosition = GetSurfaceAlignedWorldPosition(obj);
            }

            return new Vector3
            {
                x = basePosition.x + localCrashCenter.x,
                y = basePosition.y + localCrashCenter.y,
                z = basePosition.z + localCrashCenter.z
            };
        }
    }
}
