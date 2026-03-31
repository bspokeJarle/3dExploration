using CommonUtilities._3DHelpers;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonSetup;
using Domain;
using GameAiAndControls.Helpers;
using System;
using System.Collections.Generic;
using static Domain._3dSpecificsImplementations;

namespace GameAiAndControls.Controls.SeederControls
{
    public class SeederControls : IObjectMovement
    {
        // ============================
        // CONTROLS CONFIGURATION
        // ============================
        // Visual rotation:
        // - Base rotations applied each frame to the seeder object.
        private const float BaseYRotation = 0f;
        private const float BaseXRotation = 90f;
        private const float BaseZRotationIncrementPerFrame = 2f;

        // Sync offsets:
        // - SyncFactorY: scales how much the seeder's Y-offset follows the surface's GlobalMapPosition.y.
        // - SyncInitializedStartY: initial additive offset captured from the object when first synced (auto-set).
        private const float SyncFactorY = 2.5f;

        // Audio:
        // - ExplosionForce: force factor passed to physics when exploding.
        // - EnableControlsLogging: local logging toggle for controls-only events (crash/explosion).
        private const float ExplosionForce = 200f;

        public ITriangleMeshWithColor? StartCoordinates { get; set; }
        public ITriangleMeshWithColor? GuideCoordinates { get; set; }
        public I3dObject ParentObject { get; set; }
        public IPhysics Physics { get; set; } = new Physics.Physics();

        // Audio references are initialized lazily from ConfigureAudio.
        private IAudioPlayer? _audio;
        private SoundDefinition? _explosionSound;

        private float Yrotation = BaseYRotation;
        private float Xrotation = BaseXRotation;
        private float Zrotation = 0;

        private bool _syncInitialized = false;
        private float _syncY = 0;
        private bool enableLogging = false;
        private bool isExploding = false;
        private DateTime ExplosionDeltaTime;
        private Vector3? explosionWorldPosition;
        private Vector3? explosionObjectOffsets;
        private Vector3? _trackedWorldPosition;

        public void ConfigureAudio(IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            //If configured already, skip
            if (_audio != null || _explosionSound != null)
                return;

            if (audioPlayer == null || soundRegistry == null)
                return;

            _audio = audioPlayer;
            _explosionSound = soundRegistry.Get("explosion_main");
        }

        public I3dObject MoveObject(I3dObject theObject, IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            // Lazily initialize audio the first time MoveObject runs.
            ConfigureAudio(audioPlayer, soundRegistry);

            // Skip AI movement when a crash is pending or already exploding,
            // so the explosion stays anchored at the position where the hit occurred.
            if (!isExploding && theObject.ImpactStatus.HasCrashed != true)
            {
                var aiPos = SeederAi.MoveWorldPositionAccordingToAi(theObject.IsOnScreen, theObject);
                if (aiPos != null)
                {
                    _trackedWorldPosition = aiPos;
                }
            }

            // Always apply the tracked position so the deep copy's stale
            // WorldPosition (copied from the original) is replaced with the
            // control-class's authoritative value.
            if (_trackedWorldPosition != null)
            {
                theObject.WorldPosition = new Vector3
                {
                    x = _trackedWorldPosition.x,
                    y = _trackedWorldPosition.y,
                    z = _trackedWorldPosition.z
                };
            }
      
            //Set parent object
            ParentObject = theObject;
            if (theObject.Rotation != null) theObject.Rotation.y = Yrotation;
            if (theObject.Rotation != null) theObject.Rotation.x = Xrotation;
            if (theObject.Rotation != null) theObject.Rotation.z = Zrotation;

            //Handle impact status, trigger explosion if health is 0
            if (theObject.ImpactStatus.HasCrashed == true && isExploding == false)
            {
                HandleCrash(theObject);
            }

            if (isExploding)
            {
                // Anchor position with fresh copies each frame so UpdateExplosion
                // cannot drift the saved coordinates via in-place field mutation.
                if (explosionWorldPosition != null)
                {
                    theObject.WorldPosition = new Vector3
                    {
                        x = explosionWorldPosition.x,
                        y = explosionWorldPosition.y,
                        z = explosionWorldPosition.z
                    };
                }

                if (explosionObjectOffsets != null)
                {
                    theObject.ObjectOffsets = new Vector3
                    {
                        x = explosionObjectOffsets.x,
                        y = explosionObjectOffsets.y,
                        z = explosionObjectOffsets.z
                    };
                }

                //Update explosion
                Physics.UpdateExplosion(theObject, ExplosionDeltaTime);
                if (theObject.ImpactStatus.HasExploded == true)
                {
                    theObject.ObjectParts = new List<I3dObjectPart>();
                }
            }

            //If there are particles, move them
            if (ParentObject.Particles?.Particles.Count > 0)
            {
                ParentObject.Particles.MoveParticles();
            }

            // Visual spin
            Zrotation += BaseZRotationIncrementPerFrame;

            // Keep seeder offsets visually in sync with surface scrolling.
            // Must run even during explosion so the explosion stays anchored
            // to the terrain position as the surface scrolls (SyncMovement
            // only adjusts ObjectOffsets.y; x/z stay frozen from the explosion anchor).
            SyncMovement(theObject);

            // Push the deep copy's authoritative positions back to the original
            // object so shadow casting and other systems that read from AiObjects
            // stay in sync with the AI-tracked location.
            SyncToOriginal(theObject);

            return theObject;
        }

        public void HandleCrash(I3dObject theObject)
        {
            theObject.ImpactStatus.ObjectHealth = theObject.ImpactStatus.ObjectHealth - WeaponSetup.GetWeaponDamage("Lazer");
            if (theObject.ImpactStatus.ObjectHealth <= 0)
            {
                if (_audio != null && _explosionSound != null)
                {
                    var audioPosition = ((_3dObject)theObject).GetAudioPosition();
                    //Play the explosion sound
                    _audio.Play(
                        _explosionSound,
                        AudioPlayMode.OneShot,
                        new AudioPlayOptions
                        {
                            WorldPosition = new System.Numerics.Vector3(audioPosition.x, audioPosition.y, audioPosition.z)
                        });
                }

                ExplosionDeltaTime = DateTime.Now;
                isExploding = true;
                // Copy values — not references — so UpdateExplosion cannot drift the anchor.
                var wp = theObject.WorldPosition;
                explosionWorldPosition = new Vector3 { x = wp.x, y = wp.y, z = wp.z };
                var oo = theObject.ObjectOffsets;
                explosionObjectOffsets = new Vector3 { x = oo.x, y = oo.y, z = oo.z };
                // Handle object destruction or other logic here
                var explodedVersion = Physics.ExplodeObject(theObject, ExplosionForce);
                //Remove Crash boxes to avoid further collisions
                theObject.CrashBoxes = new List<List<IVector3>>();
                //Remove AI state to stop movement and other logic
                SeederAi.RemoveAiState(theObject.ObjectId);
                if (enableLogging) Logger.Log($"Seeder has exploded.");
            }
            if (enableLogging) Logger.Log($"Seeder has crashed, current health {theObject.ImpactStatus.ObjectHealth}. CrashedWith:{theObject.ImpactStatus.ObjectName} ObjectId:{theObject.ObjectId}");
        }

        public void SyncMovement(I3dObject theObject)
        {
            if (!_syncInitialized)
            {
                _syncInitialized = true;
                _syncY = theObject.ObjectOffsets.y;
            }

            theObject.ObjectOffsets = CommonUtilities._3DHelpers.SurfacePositionSyncHelpers.GetSurfaceSyncedObjectOffsets(theObject, _syncY, SyncFactorY);
        }

        public void ReleaseParticles(I3dObject theObject)
        {
            if (StartCoordinates == null || GuideCoordinates == null) return;

            // Align emission origin with surface-centered position used by AI
            var obj3d = theObject as _3dObject;
            var alignedWorld = obj3d != null
                ? CommonUtilities._3DHelpers.SurfacePositionSyncHelpers.GetSurfaceAlignedWorldPosition(obj3d)
                : (Vector3)theObject.WorldPosition;

            ParentObject?.Particles?.ReleaseParticles(
                GuideCoordinates,
                StartCoordinates,
                alignedWorld,
                this,
                1,
                null);
        }

        public void SetParticleGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
            if (StartCoord != null) StartCoordinates = StartCoord;
            if (GuideCoord != null) GuideCoordinates = GuideCoord;
        }

        public void Dispose()
        {
            if (ParentObject != null)
            {
                SeederAi.RemoveAiState(ParentObject.ObjectId);
            }

            isExploding = false;
            _syncInitialized = false;
            _syncY = 0;
            explosionWorldPosition = null;
            explosionObjectOffsets = null;
            StartCoordinates = null;
            GuideCoordinates = null;
            _audio = null;
            _explosionSound = null;
        }

        public void SetWeaponGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
            //No implementation needed, Seeder have no weapons
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
    }
}
