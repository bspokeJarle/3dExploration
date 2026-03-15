using CommonUtilities.CommonGlobalState;
using Domain;
using Gma.System.MouseKeyHook;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using _3dTesting.Helpers;
using static Domain._3dSpecificsImplementations;

namespace GameAiAndControls.Controls
{
    public class KamikazeDroneControls : IObjectMovement
    {
        private static readonly CommonUtilities._3DHelpers._3dRotationCommon Rotate3d = new();
        private const bool enableLogging = true;
        public ITriangleMeshWithColor? StartCoordinates { get; set; }
        public ITriangleMeshWithColor? GuideCoordinates { get; set; }
        public I3dObject ParentObject { get; set; }
        public IPhysics Physics { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        //Initial rotation angles for the drone, pointing towards the camera. Adjust as needed based on the drone model's default orientation.
        private float Yrotation = 0;
        private float Xrotation = 70;
        private float Zrotation = 90;
        private float TargetYrotation = 0;
        private float TargetXrotation = 70;
        private float TargetZrotation = 90;

        const int DroneSpeedScreenPrSecond = 6; //How many seconds it should take for the drone to cross the entire screen at its current speed. Adjust as needed.
        private const float RotationDegreesPerSecond = 180f;
        private const float DirectionUpdateIntervalSeconds = 1f;
        private const int LogEveryNthFrame = 10;

        private DateTime LastDirectionUpdateDateTime = DateTime.MinValue;
        private DateTime LastMovementDateTime = DateTime.MinValue;
        private IVector3 DirectionVelocity = new Vector3 { x = 0, y = 0, z = 0 }; // Initially standing still until the first direction is calculated
        private bool _syncInitialized = false;
        private float _syncY = 0;
        private int _logFrameCounter = 0;
        private int _trackedObjectId = -1;
        private bool _storedWorldPositionInitialized = false;
        private Vector3 _storedWorldPosition = new Vector3();

        private static void SafeLog(string message)
        {
            try
            {
                if (enableLogging && Logger.EnableFileLogging) Logger.Log(message, "KamikazeDrone");
            }
            catch
            {
            }
        }

        private static string FormatVector(Vector3 v)
        {
            return string.Create(CultureInfo.InvariantCulture, $"x={v.x:0.##};y={v.y:0.##};z={v.z:0.##}");
        }

        private static Vector3 ToVector3(IVector3? v)
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

        private static Vector3 Normalize(Vector3 v)
        {
            float lenSq = v.x * v.x + v.y * v.y + v.z * v.z;
            if (lenSq <= 1e-6f)
            {
                return new Vector3 { x = 0, y = 0, z = 0 };
            }

            float invLen = 1f / MathF.Sqrt(lenSq);
            return new Vector3
            {
                x = v.x * invLen,
                y = v.y * invLen,
                z = v.z * invLen
            };
        }

        private static float Length(Vector3 v)
        {
            return MathF.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
        }

        private static float Dot(Vector3 a, Vector3 b)
        {
            return (a.x * b.x) + (a.y * b.y) + (a.z * b.z);
        }

        private static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            if (angle < -180f) angle += 360f;
            return angle;
        }

        private static float MoveAngleTowards(float current, float target, float maxDelta)
        {
            float delta = NormalizeAngle(target - current);
            if (MathF.Abs(delta) <= maxDelta)
            {
                return current + delta;
            }

            return current + MathF.Sign(delta) * maxDelta;
        }

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

            return localPoints.Count > 0
                ? CommonUtilities._3DHelpers.Common3dObjectHelpers.GetCenterOfBox(localPoints)
                : new Vector3();
        }

        private static Vector3 RotateLocalPoint(Vector3 point, IVector3? rotation)
        {
            if (rotation is not Vector3 rotationVector)
            {
                return point;
            }

            var rotatedPoint = (Vector3)Rotate3d.RotatePoint(rotationVector.z, point, 'Z');
            rotatedPoint = (Vector3)Rotate3d.RotatePoint(rotationVector.y, rotatedPoint, 'Y');
            rotatedPoint = (Vector3)Rotate3d.RotatePoint(rotationVector.x, rotatedPoint, 'X');
            return rotatedPoint;
        }

        private static Vector3 GetRotatedLocalCrashCenter(I3dObject obj)
        {
            return RotateLocalPoint(GetLocalCrashCenter(obj), obj.Rotation);
        }

        private static Vector3 GetDroneCrashCenterWorldPosition(I3dObject obj)
        {
            var rotatedLocalCrashCenter = GetRotatedLocalCrashCenter(obj);

            var worldPosition = obj.WorldPosition;
            var objectOffsets = obj.ObjectOffsets;

            return new Vector3
            {
                x = (worldPosition?.x ?? 0f) + (objectOffsets?.x ?? 0f) + rotatedLocalCrashCenter.x,
                y = (worldPosition?.y ?? 0f) + (objectOffsets?.y ?? 0f) + rotatedLocalCrashCenter.y,
                z = (worldPosition?.z ?? 0f) + (objectOffsets?.z ?? 0f) + rotatedLocalCrashCenter.z
            };
        }

        private static Vector3? GetShipCrashCenterWorldPosition()
        {
            if (GameState.ShipState?.ShipCrashCenterWorldPosition is Vector3 shipCrashCenter)
            {
                return new Vector3
                {
                    x = shipCrashCenter.x - (CommonUtilities.CommonSetup.ScreenSetup.screenSizeX / 2f),
                    y = shipCrashCenter.y,
                    z = shipCrashCenter.z
                };
            }

            if (GameState.ShipState?.ShipWorldPosition is Vector3 shipWorldPosition)
            {
                return new Vector3
                {
                    x = shipWorldPosition.x - (CommonUtilities.CommonSetup.ScreenSetup.screenSizeX / 2f),
                    y = shipWorldPosition.y,
                    z = shipWorldPosition.z
                };
            }

            return null;
        }

        private void UpdateRotationTowardsTarget(Vector3 directionToTarget)
        {
            var normalizedDirection = Normalize(directionToTarget);
            if (normalizedDirection.x == 0 && normalizedDirection.y == 0 && normalizedDirection.z == 0)
            {
                return;
            }

            float headingDegrees = MathF.Atan2(normalizedDirection.x, normalizedDirection.z) * 180f / MathF.PI;
            float pitchDegrees = MathF.Atan2(-normalizedDirection.y, MathF.Sqrt(normalizedDirection.x * normalizedDirection.x + normalizedDirection.z * normalizedDirection.z)) * 180f / MathF.PI;

            TargetZrotation = 270f + headingDegrees;
            TargetXrotation = 70f + pitchDegrees;
            TargetYrotation = 0f;
        }

        private void UpdateCurrentRotation(double deltaSeconds)
        {
            float maxDelta = RotationDegreesPerSecond * (float)deltaSeconds;
            if (maxDelta <= 0f)
            {
                return;
            }

            Xrotation = MoveAngleTowards(Xrotation, TargetXrotation, maxDelta);
            Yrotation = MoveAngleTowards(Yrotation, TargetYrotation, maxDelta);
            Zrotation = MoveAngleTowards(Zrotation, TargetZrotation, maxDelta);
        }

        private void SyncMovement(I3dObject theObject)
        {
            if (!_syncInitialized)
            {
                _syncInitialized = true;
                _syncY = theObject.ObjectOffsets?.y ?? 0f;
            }

            theObject.ObjectOffsets = CommonUtilities._3DHelpers.SurfacePositionSyncHelpers.GetSurfaceSyncedObjectOffsets(theObject, _syncY);
        }

        public I3dObject MoveObject(I3dObject theObject, IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            if (_trackedObjectId != theObject.ObjectId)
            {
                _trackedObjectId = theObject.ObjectId;
                _storedWorldPosition = ToVector3(theObject.WorldPosition);
                _storedWorldPositionInitialized = theObject.WorldPosition != null;
            }
            else if (_storedWorldPositionInitialized)
            {
                theObject.WorldPosition = new Vector3
                {
                    x = _storedWorldPosition.x,
                    y = _storedWorldPosition.y,
                    z = _storedWorldPosition.z
                };
            }

            ParentObject = theObject;
            SyncMovement(theObject);

            var now = DateTime.Now;

            if (LastMovementDateTime == DateTime.MinValue)
            {
                LastMovementDateTime = now;
            }

            var deltaSeconds = (now - LastMovementDateTime).TotalSeconds;
            int logFrameNumber = ++_logFrameCounter;
            bool shouldLogThisFrame = (logFrameNumber % LogEveryNthFrame) == 0;

            double distanceToTarget = -1;
            IVector3? currentDronePosition = null;
            IVector3? currentTargetPosition = null;
            Vector3 rotatedLocalCrashCenter = new Vector3();
            Vector3 currentWorldPosition = ToVector3(theObject.WorldPosition);
            Vector3 currentObjectOffsets = ToVector3(theObject.ObjectOffsets);
            Vector3 directionToTarget = new Vector3();
            bool shouldRecalculateDirection = false;
            float speedPerSecond = 0f;
            float moveDistanceApplied = 0f;
            float distanceBeforeMove = -1f;
            float distanceAfterMove = -1f;

            if (ParentObject.WorldPosition is IVector3 && GetShipCrashCenterWorldPosition() is Vector3 targetWorldPosition)
            {
                rotatedLocalCrashCenter = GetRotatedLocalCrashCenter(ParentObject);
                var parentWorldPosition = GetDroneCrashCenterWorldPosition(ParentObject);
                currentDronePosition = parentWorldPosition;
                currentTargetPosition = targetWorldPosition;

                directionToTarget = new Vector3
                {
                    x = targetWorldPosition.x - parentWorldPosition.x,
                    y = targetWorldPosition.y - parentWorldPosition.y,
                    z = targetWorldPosition.z - parentWorldPosition.z
                };

                UpdateRotationTowardsTarget(directionToTarget);

                var distance = CommonUtilities._3DHelpers.Common3dObjectHelpers.GetDistance(parentWorldPosition, (Vector3)targetWorldPosition);

                shouldRecalculateDirection = LastDirectionUpdateDateTime == DateTime.MinValue ||
                    (now - LastDirectionUpdateDateTime).TotalSeconds >= DirectionUpdateIntervalSeconds ||
                    Dot((Vector3)DirectionVelocity, directionToTarget) <= 0f;

                if (shouldRecalculateDirection && distance > 0)
                {
                    var speed = CommonUtilities.CommonSetup.ScreenSetup.screenSizeX / (float)DroneSpeedScreenPrSecond;

                    DirectionVelocity = new Vector3
                    {
                        x = directionToTarget.x / (float)distance * speed,
                        y = directionToTarget.y / (float)distance * speed,
                        z = directionToTarget.z / (float)distance * speed
                    };
                    LastDirectionUpdateDateTime = now;
                }
                else if (distance <= 0)
                {
                    DirectionVelocity = new Vector3 { x = 0, y = 0, z = 0 };
                }

                distanceToTarget = CommonUtilities._3DHelpers.Common3dObjectHelpers.GetDistance(parentWorldPosition, (Vector3)targetWorldPosition);
                distanceBeforeMove = (float)distanceToTarget;
            }

            UpdateCurrentRotation(deltaSeconds);

            if (theObject.Rotation != null)
            {
                var rotation = theObject.Rotation;
                rotation.y = Yrotation;
                rotation.x = Xrotation;
                rotation.z = Zrotation;
            }

            if (theObject.WorldPosition is IVector3 objectWorldPosition)
            {
                if (currentDronePosition is Vector3 anchoredDronePosition)
                {
                    var anchoredRotatedLocalCrashCenter = GetRotatedLocalCrashCenter(theObject);
                    var objectOffsets = theObject.ObjectOffsets;

                    theObject.WorldPosition = new Vector3
                    {
                        x = anchoredDronePosition.x - (objectOffsets?.x ?? 0f) - anchoredRotatedLocalCrashCenter.x,
                        y = anchoredDronePosition.y - (objectOffsets?.y ?? 0f) - anchoredRotatedLocalCrashCenter.y,
                        z = anchoredDronePosition.z - (objectOffsets?.z ?? 0f) - anchoredRotatedLocalCrashCenter.z
                    };

                    objectWorldPosition = theObject.WorldPosition;
                }

                currentDronePosition = GetDroneCrashCenterWorldPosition(theObject);

                if (currentDronePosition is Vector3 dronePosition && currentTargetPosition is Vector3 targetPosition)
                {
                    var liveDirectionToTarget = new Vector3
                    {
                        x = targetPosition.x - dronePosition.x,
                        y = targetPosition.y - dronePosition.y,
                        z = targetPosition.z - dronePosition.z
                    };

                    float currentDistance = (float)CommonUtilities._3DHelpers.Common3dObjectHelpers.GetDistance(dronePosition, targetPosition);
                    speedPerSecond = Length((Vector3)DirectionVelocity);
                    distanceBeforeMove = currentDistance;

                    if (currentDistance > 0f && speedPerSecond > 0f)
                    {
                        var moveDirection = Normalize(liveDirectionToTarget);
                        float moveDistance = MathF.Min(speedPerSecond * (float)deltaSeconds, currentDistance);
                        moveDistanceApplied = moveDistance;
                        var nextDroneCenterWorldPosition = new Vector3
                        {
                            x = dronePosition.x + (moveDirection.x * moveDistance),
                            y = dronePosition.y + (moveDirection.y * moveDistance),
                            z = dronePosition.z + (moveDirection.z * moveDistance)
                        };
                        var movementRotatedLocalCrashCenter = GetRotatedLocalCrashCenter(theObject);
                        var objectOffsets = theObject.ObjectOffsets;

                        theObject.WorldPosition = new Vector3
                        {
                            x = nextDroneCenterWorldPosition.x - (objectOffsets?.x ?? 0f) - movementRotatedLocalCrashCenter.x,
                            y = nextDroneCenterWorldPosition.y - (objectOffsets?.y ?? 0f) - movementRotatedLocalCrashCenter.y,
                            z = nextDroneCenterWorldPosition.z - (objectOffsets?.z ?? 0f) - movementRotatedLocalCrashCenter.z
                        };
                    }
                    else
                    {
                        theObject.WorldPosition = new Vector3
                        {
                            x = objectWorldPosition.x,
                            y = objectWorldPosition.y,
                            z = objectWorldPosition.z
                        };
                    }
                }

                currentDronePosition = GetDroneCrashCenterWorldPosition(theObject);
                currentWorldPosition = ToVector3(theObject.WorldPosition);
                currentObjectOffsets = ToVector3(theObject.ObjectOffsets);
                rotatedLocalCrashCenter = GetRotatedLocalCrashCenter(theObject);
                if (currentTargetPosition is Vector3 latestTargetPosition)
                {
                    distanceToTarget = CommonUtilities._3DHelpers.Common3dObjectHelpers.GetDistance((Vector3)currentDronePosition, latestTargetPosition);
                    distanceAfterMove = (float)distanceToTarget;
                }
            }

            if (shouldLogThisFrame && currentDronePosition != null && currentTargetPosition != null)
            {
                float distanceDelta = distanceBeforeMove >= 0f && distanceAfterMove >= 0f
                    ? distanceBeforeMove - distanceAfterMove
                    : 0f;

                SafeLog(
                    $"frame={logFrameNumber} objectId={theObject.ObjectId} " +
                    $"world=({FormatVector(currentWorldPosition)}) offsets=({FormatVector(currentObjectOffsets)}) centerOffset=({FormatVector(rotatedLocalCrashCenter)}) " +
                    $"drone=({FormatVector((Vector3)currentDronePosition)}) target=({FormatVector((Vector3)currentTargetPosition)}) dir=({FormatVector(directionToTarget)}) velocity=({FormatVector((Vector3)DirectionVelocity)}) " +
                    $"recalc={(shouldRecalculateDirection ? 1 : 0)} dt={deltaSeconds.ToString("0.####", CultureInfo.InvariantCulture)} speed={speedPerSecond.ToString("0.##", CultureInfo.InvariantCulture)} step={moveDistanceApplied.ToString("0.##", CultureInfo.InvariantCulture)} " +
                    $"distanceBefore={distanceBeforeMove.ToString("0.##", CultureInfo.InvariantCulture)} distanceAfter={distanceAfterMove.ToString("0.##", CultureInfo.InvariantCulture)} distanceDelta={distanceDelta.ToString("0.##", CultureInfo.InvariantCulture)}"
                );
            }

            _storedWorldPosition = ToVector3(theObject.WorldPosition);
            _storedWorldPositionInitialized = theObject.WorldPosition != null;
            LastMovementDateTime = now;

            return theObject;
        }

        public void SetParticleGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void SetWeaponGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
            throw new NotImplementedException();
        }

        public void ConfigureAudio(IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            throw new NotImplementedException();
        }

        public void ReleaseParticles(I3dObject theObject)
        {
            throw new NotImplementedException();
        }
    }
}
