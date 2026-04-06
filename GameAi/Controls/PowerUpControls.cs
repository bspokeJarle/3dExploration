using CommonUtilities._3DHelpers;
using CommonUtilities.CommonGlobalState;
using Domain;
using System;
using System.Collections.Generic;
using System.Globalization;
using static Domain._3dSpecificsImplementations;

namespace GameAiAndControls.Controls
{
    public class PowerUpControls : IObjectMovement
    {
        // Visual rotation:
        // - BaseXRotation tilts to face the camera (same approach as SeederControls).
        // - Yrotation increments each frame for a slow spin around the Y axis.
        private const float BaseXRotation = 90f;
        private const float BaseYRotation = 0f;
        private const float BaseZRotation = 0f;
        private const float BaseYRotationIncrementPerFrame = 0.8f;

        // Sync offsets:
        // - SyncFactorY: scales how much the Y-offset follows the surface's GlobalMapPosition.y.
        private const float SyncFactorY = 2.5f;

        // Diagnostics:
        private const bool enableLogging = true;
        private const int LogEveryNthFrame = 60;

        public ITriangleMeshWithColor? StartCoordinates { get; set; }
        public ITriangleMeshWithColor? GuideCoordinates { get; set; }
        public I3dObject ParentObject { get; set; }
        public IPhysics Physics { get; set; } = new Physics.Physics();

        private float Yrotation = BaseYRotation;
        private float Xrotation = BaseXRotation;
        private float Zrotation = BaseZRotation;

        private bool _syncInitialized = false;
        private float _syncY = 0;
        private int _logFrameCounter = 0;

        private static void SafeLog(string message)
        {
            try
            {
                if (enableLogging && Logger.EnableFileLogging) Logger.Log(message, "PowerUp");
            }
            catch { }
        }

        private static string FormatVector(Vector3 v)
        {
            return string.Create(CultureInfo.InvariantCulture, $"x={v.x:0.##};y={v.y:0.##};z={v.z:0.##}");
        }

        public I3dObject MoveObject(I3dObject theObject, IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            ParentObject = theObject;

            // Handle collision: PowerUp is collected by Ship, just mark for removal
            if (theObject.ImpactStatus.HasCrashed == true)
            {
                SafeLog($"[PowerUp] HasCrashed=true, ObjectName='{theObject.ImpactStatus.ObjectName}', setting HasExploded=true. Id={theObject.ObjectId}");
                theObject.ImpactStatus.HasExploded = true;
                theObject.CrashBoxes = new List<List<IVector3>>();
                return theObject;
            }

            if (theObject.Rotation != null) theObject.Rotation.y = Yrotation;
            if (theObject.Rotation != null) theObject.Rotation.x = Xrotation;
            if (theObject.Rotation != null) theObject.Rotation.z = Zrotation;

            // Visual spin around Y
            Yrotation += BaseYRotationIncrementPerFrame;

            // Keep offsets visually in sync with surface scrolling
            SyncMovement(theObject);

            // Push positions back to original in AiObjects
            SyncToOriginal(theObject);

            if (enableLogging && Logger.EnableFileLogging)
            {
                _logFrameCounter++;
                if (_logFrameCounter % LogEveryNthFrame == 0)
                {
                    var offsets = theObject.ObjectOffsets != null ? FormatVector((Vector3)theObject.ObjectOffsets) : "null";
                    var world = theObject.WorldPosition != null ? FormatVector((Vector3)theObject.WorldPosition) : "null";
                    int crashBoxCount = theObject.CrashBoxes?.Count ?? 0;
                    SafeLog($"[PowerUp] Id={theObject.ObjectId} Offsets={offsets} World={world} CrashBoxes={crashBoxCount} HasCrashed={theObject.ImpactStatus?.HasCrashed} HasExploded={theObject.ImpactStatus?.HasExploded}");
                }
            }

            return theObject;
        }

        private void SyncMovement(I3dObject theObject)
        {
            if (!_syncInitialized)
            {
                _syncInitialized = true;
                _syncY = theObject.ObjectOffsets.y;
            }

            theObject.ObjectOffsets = SurfacePositionSyncHelpers.GetSurfaceSyncedObjectOffsets(theObject, _syncY, SyncFactorY);
        }

        private static void SyncToOriginal(I3dObject deepCopy)
        {
            var aiObjects = GameState.SurfaceState?.AiObjects;
            if (aiObjects == null) return;

            for (int i = 0; i < aiObjects.Count; i++)
            {
                if (aiObjects[i].ObjectId == deepCopy.ObjectId)
                {
                    var original = aiObjects[i];
                    if (ReferenceEquals(original, deepCopy)) return;

                    original.WorldPosition = new Vector3
                    {
                        x = deepCopy.WorldPosition.x,
                        y = deepCopy.WorldPosition.y,
                        z = deepCopy.WorldPosition.z
                    };
                    original.ObjectOffsets = new Vector3
                    {
                        x = deepCopy.ObjectOffsets.x,
                        y = deepCopy.ObjectOffsets.y,
                        z = deepCopy.ObjectOffsets.z
                    };
                    return;
                }
            }
        }

        public void ReleaseParticles()
        {
        }

        public void SetParticleGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
        }

        public void SetRearEngineGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
        }

        public void Dispose()
        {
            _syncInitialized = false;
            _syncY = 0;
            StartCoordinates = null;
            GuideCoordinates = null;
        }

        public void SetWeaponGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
        }

        public void ConfigureAudio(IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
        }

        public void ReleaseParticles(I3dObject theObject)
        {
        }
    }
}
