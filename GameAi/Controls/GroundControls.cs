using Domain;
using Gma.System.MouseKeyHook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameAiAndControls.Controls
{
    public class GroundControls : IObjectMovement
    {
        public ITriangleMeshWithColor? StartCoordinates { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ITriangleMeshWithColor? GuideCoordinates { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public I3dObject ParentObject { get; set; }

        public float zPosition { get; set; } = 0;
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
            ParentObject = theObject;
            if (theObject != null && theObject.ParentSurface != null)
            {
                //Replace the surfaces from the new viewport - other objects might have moved surface position
                var newViewPort = theObject!.ParentSurface!.GetSurfaceViewPort();                
                theObject.ObjectParts = newViewPort.ObjectParts; 
                theObject.CrashBoxes = newViewPort.CrashBoxes;
            }            
            return theObject!;
        }

        public void SetParticleGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
            throw new NotImplementedException();
        }

        public void SetWeaponGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
            throw new NotImplementedException();
        }
    }
}
