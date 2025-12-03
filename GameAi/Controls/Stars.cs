using Domain;
using System;
using System.Numerics;
using static Domain._3dSpecificsImplementations;

namespace GameAiAndControls.Controls
{
    public class StarsControl : IObjectMovement
    {
        public ITriangleMeshWithColor? StartCoordinates { get; set; }
        public ITriangleMeshWithColor? GuideCoordinates { get; set; }
        public I3dObject ParentObject { get; set; }
        public IPhysics Physics { get; set; }

        // Optional future use (spin, pulse, etc.)
        private float _yRotation = 0f;
        private float _xRotation = 0f;
        private float _zRotation = 0f;

        private bool _syncInitialized = false;

        // Base local offset when the star was spawned
        private _3dSpecificsImplementations.Vector3 _baseOffset;

        // Surface position when the star was spawned
        private IVector3? _initialSurfacePosition;

        // Factors to keep stars roughly in sync with surface movement.
        // Values < 1.0f will give a parallax effect (stars move litt mindre enn bakken).
        private float _syncFactorX = 1.0f;
        private float _syncFactorY = 1.0f;
        private float _syncFactorZ = 1.0f;

        private bool _enableLogging = false;

        public I3dObject MoveObject(I3dObject theObject, IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            ParentObject = theObject;

            // If you later want rotation on stars, you can use these:
            if (theObject.Rotation != null)
            {
                theObject.Rotation.x = _xRotation;
                theObject.Rotation.y = _yRotation;
                theObject.Rotation.z = _zRotation;
            }

            // Main part: keep the star visually in sync with the moving surface.
            SyncMovement(theObject);

            return theObject;
        }

        private void SyncMovement(I3dObject theObject)
        {
            if (theObject.ParentSurface == null)
            return;

            var surfacePos = theObject.ParentSurface.GlobalMapPosition;

            if (!_syncInitialized)
            {
                _syncInitialized = true;

                // Remember where the star started (local offsets)
                _baseOffset = new _3dSpecificsImplementations.Vector3(
                    theObject.ObjectOffsets.x,
                    theObject.ObjectOffsets.y,
                    theObject.ObjectOffsets.z
                );

                // Remember where the surface was when this star was spawned
                _initialSurfacePosition = new _3dSpecificsImplementations.Vector3
                {
                    x = surfacePos.x,
                    y = surfacePos.y,
                    z = surfacePos.z
                };
            }

            if (_initialSurfacePosition == null)
                return;

            // Compute how much the surface has moved since the star was created
            var deltaX = (surfacePos.x - _initialSurfacePosition.x) * _syncFactorX;
            var deltaY = (surfacePos.y - _initialSurfacePosition.y) * _syncFactorY;
            var deltaZ = (surfacePos.z - _initialSurfacePosition.z) * _syncFactorZ;

            // Apply this delta to the star's base offset so it follows the surface
            theObject.ObjectOffsets = new _3dSpecificsImplementations.Vector3
            {
                x = _baseOffset.x + deltaX,
                y = _baseOffset.y + deltaY,
                z = _baseOffset.z + deltaZ
            };

            if (_enableLogging)
            {
                Logger.Log(
                    $"Star sync -> Offsets: ({theObject.ObjectOffsets.x:0.0}, {theObject.ObjectOffsets.y:0.0}, {theObject.ObjectOffsets.z:0.0}) " +
                    $"Surface: ({surfacePos.x:0.0}, {surfacePos.y:0.0}, {surfacePos.z:0.0})"
                );
            }
        }

        public void ReleaseParticles()
        {
            // Stars do not release particles (for now).
        }

        public void SetParticleGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
            // Not used for stars, but we keep the implementation to satisfy the interface.
            if (StartCoord != null) StartCoordinates = StartCoord;
            if (GuideCoord != null) GuideCoordinates = GuideCoord;
        }

        public void SetWeaponGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
            // No weapon logic needed for stars.
        }

        public void ConfigureAudio(IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            // Stars are silent for now.
        }

        public void Dispose()
        {
            // Nothing to dispose yet.
        }
    }
}
