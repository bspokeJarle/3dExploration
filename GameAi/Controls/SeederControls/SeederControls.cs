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
        private const bool EnableControlsLogging = false;

        public ITriangleMeshWithColor? StartCoordinates { get; set; }
        public ITriangleMeshWithColor? GuideCoordinates { get; set; }
        public I3dObject ParentObject { get; set; }
        public IPhysics Physics { get; set; } = new Physics.Physics();

        // Audio setup (gjøres lazy via ConfigureAudio)
        private IAudioPlayer? _audio;
        private SoundDefinition? _explosionSound;

        private float Yrotation = BaseYRotation;
        private float Xrotation = BaseXRotation;
        private float Zrotation = 0;

        private bool _syncInitialized = false;
        private float _syncY = 0;
        private bool enableLogging = EnableControlsLogging;
        private bool isExploding = false;
        private DateTime ExplosionDeltaTime;

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
            // Lazy audio-konfig – gjøres første gang MoveObject kalles
            ConfigureAudio(audioPlayer, soundRegistry);

            // Update world position according to AI
            theObject.WorldPosition = SeederAi.MoveWorldPositionAccordingToAi(theObject.IsOnScreen, theObject);
      
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

            // Keep seeder offsets visually in sync with surface scrolling
            SyncMovement(theObject);

            return theObject;
        }

        public void HandleCrash(I3dObject theObject)
        {
            theObject.ImpactStatus.ObjectHealth = theObject.ImpactStatus.ObjectHealth - WeaponSetup.GetWeaponDamage("Lazer");
            if (theObject.ImpactStatus.ObjectHealth <= 0)
            {
                if (_audio != null && _explosionSound != null)
                {
                    //Play the explosion sound
                    _audio.Play(_explosionSound, AudioPlayMode.OneShot);
                }

                ExplosionDeltaTime = DateTime.Now;
                isExploding = true;
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

            theObject.ObjectOffsets = new Vector3()
            {
                x = theObject.ObjectOffsets.x,
                y = GameState.SurfaceState.GlobalMapPosition.y * SyncFactorY + _syncY,
                z = theObject.ObjectOffsets.z
            };
        }

        public void ReleaseParticles(I3dObject theObject)
        {
            if (StartCoordinates == null || GuideCoordinates == null) return;

            // Align emission origin with surface-centered position used by AI
            var obj3d = theObject as _3dObject;
            var alignedWorld = obj3d != null
                ? SeederMovementHelpers.SyncronizeSeederWithSurfacePosition(obj3d)
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
            throw new NotImplementedException();
        }

        public void SetWeaponGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
            //No implementation needed, Seeder have no weapons
        }
    }
}
