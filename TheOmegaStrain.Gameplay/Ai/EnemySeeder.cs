using TheOmegaStrain.Domain;
using Gma.System.MouseKeyHook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheOmegaStrain.Gameplay.Ai
{
    public class EnemySeeder : IObjectMovement
    {
        public ITriangleMeshWithColorAndTexture? StartCoordinates { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ITriangleMeshWithColorAndTexture? GuideCoordinates { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IPhysics Physics { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void ConfigureAudio(IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public I3dObject MoveObject(I3dObject theObject, IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            throw new NotImplementedException();
        }

        public void ReleaseParticles(I3dObject theObject)
        {
            throw new NotImplementedException();
        }

        public void SetParticleGuideCoordinates(ITriangleMeshWithColorAndTexture StartCoord, ITriangleMeshWithColorAndTexture GuideCoord)
        {
            throw new NotImplementedException();
        }
        public void SetRearEngineGuideCoordinates(ITriangleMeshWithColorAndTexture StartCoord, ITriangleMeshWithColorAndTexture GuideCoord) { }

        public void SetWeaponGuideCoordinates(ITriangleMeshWithColorAndTexture StartCoord, ITriangleMeshWithColorAndTexture GuideCoord)
        {
            throw new NotImplementedException();
        }
    }
}
