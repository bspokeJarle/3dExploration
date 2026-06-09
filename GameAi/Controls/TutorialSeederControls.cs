using CommonUtilities._3DHelpers;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonSetup;
using Domain;
using GameAiAndControls.Controls.SeederControls;

namespace GameAiAndControls.Controls
{
    public class TutorialSeederControls : IObjectMovement
    {
        private const float IdleSpinDegreesPerSecond = 45f;
        private const float SyncFactorY = SurfacePositionSyncHelpers.DefaultEnemySurfaceSyncFactorY;

        private readonly SeederControls.SeederControls _combatControls = new();
        private bool _syncInitialized;
        private float _syncY;
        private float _zRotation;
        private bool _useCombatControls;

        public ITriangleMeshWithColor? StartCoordinates { get; set; }
        public ITriangleMeshWithColor? GuideCoordinates { get; set; }
        public IPhysics Physics { get; set; } = new Physics.Physics();

        public I3dObject MoveObject(I3dObject theObject, IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            if (_useCombatControls)
            {
                return _combatControls.MoveObject(theObject, audioPlayer, soundRegistry);
            }

            if (theObject.ImpactStatus?.HasCrashed == true)
            {
                _combatControls.ConfigureAudio(audioPlayer, soundRegistry);
                _combatControls.HandleCrash(theObject);

                if (theObject.ImpactStatus?.ObjectHealth <= 0 || theObject.ImpactStatus?.HasExploded == true)
                {
                    _useCombatControls = true;
                    return _combatControls.MoveObject(theObject, audioPlayer, soundRegistry);
                }
            }

            ApplyIdlePose(theObject);
            SyncMovement(theObject);
            return theObject;
        }

        public void ConfigureAudio(IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            _combatControls.ConfigureAudio(audioPlayer, soundRegistry);
        }

        public void ReleaseParticles(I3dObject theObject)
        {
            _combatControls.ReleaseParticles(theObject);
        }

        public void SetParticleGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
            StartCoordinates = StartCoord;
            GuideCoordinates = GuideCoord;
            _combatControls.SetParticleGuideCoordinates(StartCoord, GuideCoord);
        }

        public void SetRearEngineGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
        }

        public void SetWeaponGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
        }

        public void Dispose()
        {
            _combatControls.Dispose();
            StartCoordinates = null;
            GuideCoordinates = null;
            _syncInitialized = false;
            _syncY = 0f;
            _zRotation = 0f;
            _useCombatControls = false;
        }

        private void ApplyIdlePose(I3dObject theObject)
        {
            theObject.Rotation ??= new _3dSpecificsImplementations.Vector3();
            float dt = GameState.DeltaTime > 0f ? GameState.DeltaTime : 1f / ScreenSetup.targetFps;
            _zRotation += IdleSpinDegreesPerSecond * dt;

            theObject.Rotation.x = 90f;
            theObject.Rotation.y = 0f;
            theObject.Rotation.z = _zRotation;
        }

        private void SyncMovement(I3dObject theObject)
        {
            if (!_syncInitialized)
            {
                _syncInitialized = true;
                _syncY = theObject.ObjectOffsets?.y ?? 0f;
            }

            theObject.ObjectOffsets = SurfacePositionSyncHelpers.GetSurfaceSyncedObjectOffsets(theObject, _syncY, SyncFactorY);
        }
    }
}
