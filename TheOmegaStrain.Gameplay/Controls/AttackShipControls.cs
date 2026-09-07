using TheOmegaStrain.Common.CommonSetup;
using TheOmegaStrain.Domain;
using System;

namespace TheOmegaStrain.Gameplay.Controls
{
    public sealed class AttackShipControls : IObjectMovement
    {
        // Engines emit a steady stream, alternating frames to keep the particle count sane.
        private const int FramesBetweenReleases = 2;
        private const int ParticleThrust = 3;

        public ITriangleMeshWithColorAndTexture? StartCoordinates { get; set; }
        public ITriangleMeshWithColorAndTexture? GuideCoordinates { get; set; }
        public ITriangleMeshWithColorAndTexture? RearEngineStartCoordinates { get; set; }
        public ITriangleMeshWithColorAndTexture? RearEngineGuideCoordinates { get; set; }
        public I3dObject ParentObject { get; set; }
        public IPhysics Physics { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        // Degrees per frame the ship spins.
        private const float XRotationSpeed = 1f;
        private const float YRotationSpeed = 1f;

        // LiveGameLoop deep-copies every inhabitant each frame and the cloner gives the copy
        // its own Rotation vector, so anything written to theObject.Rotation is discarded when
        // the copy dies. The controller instance is shared between frames, so the accumulated
        // angle has to live here or the ship would never actually turn.
        private float _accumulatedX;
        private float _accumulatedY;

        private int _framesSinceRelease;

        public I3dObject MoveObject(I3dObject theObject, IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            ParentObject = theObject;

            var rotation = theObject.Rotation as Vector3 ?? new Vector3();
            rotation.x += _accumulatedX;
            rotation.y += _accumulatedY;
            theObject.Rotation = rotation;

            _accumulatedX += XRotationSpeed;
            _accumulatedY += YRotationSpeed;

            ReleaseParticles(theObject);
            if (theObject.Particles?.Particles.Count > 0)
                theObject.Particles.MoveParticles();

            return theObject;
        }

        public void Dispose() { }
        public void ConfigureAudio(IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry) { }

        public void ReleaseParticles(I3dObject theObject)
        {
            if (++_framesSinceRelease < FramesBetweenReleases)
                return;

            // Wait until at least one engine has received its rotated guides.
            bool leftReady = StartCoordinates != null && GuideCoordinates != null;
            bool rightReady = RearEngineStartCoordinates != null && RearEngineGuideCoordinates != null;
            if (!leftReady && !rightReady)
                return;

            _framesSinceRelease = 0;

            var worldPosition = new Vector3
            {
                x = theObject.WorldPosition?.x ?? 0f,
                y = theObject.WorldPosition?.y ?? 0f,
                z = theObject.WorldPosition?.z ?? 0f
            };

            if (leftReady)
                theObject.Particles?.ReleaseParticles(
                    GuideCoordinates, StartCoordinates, worldPosition, this, ParticleThrust, null);

            if (rightReady)
                theObject.Particles?.ReleaseParticles(
                    RearEngineGuideCoordinates, RearEngineStartCoordinates, worldPosition, this, ParticleThrust, null);
        }

        public void SetParticleGuideCoordinates(ITriangleMeshWithColorAndTexture StartCoord, ITriangleMeshWithColorAndTexture GuideCoord)
        {
            if (StartCoord != null) StartCoordinates = StartCoord;
            if (GuideCoord != null) GuideCoordinates = GuideCoord;
        }

        public void SetRearEngineGuideCoordinates(ITriangleMeshWithColorAndTexture StartCoord, ITriangleMeshWithColorAndTexture GuideCoord)
        {
            if (StartCoord != null) RearEngineStartCoordinates = StartCoord;
            if (GuideCoord != null) RearEngineGuideCoordinates = GuideCoord;
        }

        public void SetWeaponGuideCoordinates(ITriangleMeshWithColorAndTexture StartCoord, ITriangleMeshWithColorAndTexture GuideCoord) { }
    }
}
