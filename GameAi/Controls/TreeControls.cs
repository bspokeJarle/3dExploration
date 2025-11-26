using Domain;
using Gma.System.MouseKeyHook;
using System;

namespace GameAiAndControls.Controls
{
    public class TreeControls : IObjectMovement
    {
        public ITriangleMeshWithColor? StartCoordinates { get; set; }
        public ITriangleMeshWithColor? GuideCoordinates { get; set; }
        public I3dObject ParentObject { get; set; }
        public IPhysics Physics { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private float Yrotation = 0;
        private float Xrotation = 70;
        private float Zrotation = 0;
        
        public I3dObject MoveObject(I3dObject theObject, IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            //TODO: In time I want the trees to have animations on the branches, now nothing
            //Set parent object
            ParentObject = theObject;
            if (theObject.Rotation!=null) theObject.Rotation.y=Yrotation;
            if (theObject.Rotation!=null) theObject.Rotation.x=Xrotation;
            if (theObject.Rotation!=null) theObject.Rotation.z=Zrotation;

            //For now, just rotate the object at a fixed speed
            //Zrotation += 2;
            //Xrotation += 2;
            return theObject;
        }

        public void ReleaseParticles()
        {
        }

        public void SetParticleGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void SetWeaponGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
            throw new NotImplementedException();
        }

        public void ConfigureAudio(IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            throw new NotImplementedException();
        }
    }
}
