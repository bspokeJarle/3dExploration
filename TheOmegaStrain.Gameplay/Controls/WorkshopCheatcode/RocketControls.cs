using TheOmegaStrain.Common.OmegaEngineAdapters;
using TheOmegaStrain.Domain;
using System;
using System.Collections.Generic;

namespace TheOmegaStrain.Gameplay.Controls
{
    /// <summary>
    /// Movement and exhaust control for the Rocket.
    ///
    /// The rocket has a single engine, so it only uses the primary guide pair
    /// (RocketParticlesStartGuide / RocketParticlesDirectionGuide).
    /// </summary>
    public sealed class RocketControls : IObjectMovement
    {
        private const string StartGuidePartName = "RocketParticlesStartGuide";
        private const string DirectionGuidePartName = "RocketParticlesDirectionGuide";

        // A steady stream, emitted every other frame to keep the particle count sane.
        private const int FramesBetweenReleases = 2;
        private const int ParticleThrust = 3;

        // Degrees per frame the rocket spins around its own long axis.
        private const float RollSpeed = 2f;

        public ITriangleMeshWithColorAndTexture? StartCoordinates { get; set; }
        public ITriangleMeshWithColorAndTexture? GuideCoordinates { get; set; }
        public ITriangleMeshWithColorAndTexture? RearEngineStartCoordinates { get; set; }
        public ITriangleMeshWithColorAndTexture? RearEngineGuideCoordinates { get; set; }
        public I3dObject ParentObject { get; set; }
        public IPhysics Physics { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        // LiveGameLoop deep-copies every inhabitant each frame and the cloner gives the copy
        // its own Rotation vector, so anything written to theObject.Rotation is discarded when
        // the copy dies. The controller instance is shared between frames, so the accumulated
        // angle has to live here or the rocket would never actually turn.
        private float _accumulatedX;

        private int _framesSinceRelease;
        private readonly OmegaMeshRotation _rotate = new();

        public I3dObject MoveObject(I3dObject theObject, IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            ParentObject = theObject;

            var rotation = theObject.Rotation as Vector3 ?? new Vector3();
            rotation.x += _accumulatedX;
            theObject.Rotation = rotation;

            _accumulatedX += RollSpeed;

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

            // Rotate the guides for THIS frame. LiveGameLoop only binds guide parts after
            // MoveObject has already run, so relying on the bound coordinates would always
            // emit from the previous frame's orientation.
            var start = GetCurrentFrameRotatedGuide(theObject, StartGuidePartName);
            var guide = GetCurrentFrameRotatedGuide(theObject, DirectionGuidePartName);
            if (start == null || guide == null)
                return;

            _framesSinceRelease = 0;

            var worldPosition = new Vector3
            {
                x = theObject.WorldPosition?.x ?? 0f,
                y = theObject.WorldPosition?.y ?? 0f,
                z = theObject.WorldPosition?.z ?? 0f
            };

            theObject.Particles?.ReleaseParticles(
                guide, start, worldPosition, this, ParticleThrust, null);
        }

        private ITriangleMeshWithColorAndTexture? GetCurrentFrameRotatedGuide(I3dObject theObject, string partName)
        {
            var part = theObject.ObjectParts.Find(p => p.PartName == partName);
            if (part?.Triangles == null || part.Triangles.Count == 0)
                return null;

            var rotation = theObject.Rotation ?? new Vector3();
            var mesh = new List<ITriangleMeshWithColorAndTexture>
            {
                OmegaObjectHelpers.CopyTriangle(part.Triangles[0])
            };

            mesh = _rotate.RotateZMesh(mesh, rotation.z);
            mesh = _rotate.RotateYMesh(mesh, rotation.y);
            mesh = _rotate.RotateXMesh(mesh, rotation.x);

            return mesh[0];
        }

        public void SetParticleGuideCoordinates(ITriangleMeshWithColorAndTexture StartCoord, ITriangleMeshWithColorAndTexture GuideCoord)
        {
            if (StartCoord != null) StartCoordinates = StartCoord;
            if (GuideCoord != null) GuideCoordinates = GuideCoord;
        }

        public void SetRearEngineGuideCoordinates(ITriangleMeshWithColorAndTexture StartCoord, ITriangleMeshWithColorAndTexture GuideCoord) { }

        public void SetWeaponGuideCoordinates(ITriangleMeshWithColorAndTexture StartCoord, ITriangleMeshWithColorAndTexture GuideCoord) { }
    }
}
