using TheOmegaStrain.Common.CommonSetup;
using TheOmegaStrain.Domain;

namespace TheOmegaStrain.Gameplay.Controls
{
    public class IglooControls : IObjectMovement
    {
        private const float BaseYRotation = 0f;
        private const float BaseXRotation = WorldViewSetup.SurfaceFacingObjectPitchDegrees;
        public ITriangleMeshWithColorAndTexture? StartCoordinates { get; set; }
        public ITriangleMeshWithColorAndTexture? GuideCoordinates { get; set; }
        public IPhysics Physics { get; set; } = new Physics.Physics();

        public I3dObject MoveObject(I3dObject theObject, IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            var rotation = theObject.Rotation as Vector3 ?? new Vector3();
            float existingZ = rotation.z;
            rotation.y = BaseYRotation;
            rotation.x = BaseXRotation;
            rotation.z = existingZ;
            theObject.Rotation = rotation;

            return theObject;
        }

        public void ReleaseParticles()
        {
        }

        public void ReleaseParticles(I3dObject theObject)
        {
        }

        public void SetParticleGuideCoordinates(ITriangleMeshWithColorAndTexture StartCoord, ITriangleMeshWithColorAndTexture GuideCoord)
        {
        }

        public void SetRearEngineGuideCoordinates(ITriangleMeshWithColorAndTexture StartCoord, ITriangleMeshWithColorAndTexture GuideCoord)
        {
        }

        public void SetWeaponGuideCoordinates(ITriangleMeshWithColorAndTexture StartCoord, ITriangleMeshWithColorAndTexture GuideCoord)
        {
        }

        public void ConfigureAudio(IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
        }

        public void Dispose()
        {
            StartCoordinates = null;
            GuideCoordinates = null;
        }
    }
}
