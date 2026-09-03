using RetroMesh.Engine;
using TheOmegaStrain.Domain;

namespace TheOmegaStrain.Domain
{
    public interface IObjectMovement
    {
        I3dObject MoveObject(I3dObject theObject, IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry);
        void ConfigureAudio(IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry);
        ITriangleMeshWithColorAndTexture? StartCoordinates { get; set; }
        ITriangleMeshWithColorAndTexture? GuideCoordinates { get; set; }
        void ReleaseParticles(I3dObject theObject);
        void SetParticleGuideCoordinates(ITriangleMeshWithColorAndTexture StartCoord, ITriangleMeshWithColorAndTexture GuideCoord);
        void SetRearEngineGuideCoordinates(ITriangleMeshWithColorAndTexture StartCoord, ITriangleMeshWithColorAndTexture GuideCoord);
        void SetWeaponGuideCoordinates(ITriangleMeshWithColorAndTexture StartCoord, ITriangleMeshWithColorAndTexture GuideCoord);
        IPhysics Physics { get; set; }
        void Dispose();
    }
}
