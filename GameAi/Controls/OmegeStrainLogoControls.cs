using CommonUtilities._3DHelpers;
using Domain;
using System;

namespace GameAiAndControls.Controls
{
    public class OmegaStrainLogoControls : IObjectMovement
    {
        public ITriangleMeshWithColor? StartCoordinates { get; set; }
        public ITriangleMeshWithColor? GuideCoordinates { get; set; }
        public I3dObject ParentObject { get; set; }
        public IPhysics Physics { get; set; } = new Physics.Physics();

        private float Yrotation = 0;
        private float Xrotation = 90;
        private float Zrotation = 0;
        private float Xoffset = 1000;
        private float Zoffset = 0;
        private bool zooomOut = false;
        private bool exploded = false;
        private DateTime ExplosionDeltaTime;

        private readonly _3dRotationCommon _rotate = new(); // Rotation fra CommonHelpers

        public I3dObject MoveObject(I3dObject theObject, IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            //TODO: In time I want the trees to have animations on the branches, now nothing
            //Set parent object
            ParentObject = theObject;
            if (theObject.Rotation != null) theObject.Rotation.y = Yrotation;
            if (theObject.Rotation != null) theObject.Rotation.x = Xrotation;
            if (theObject.Rotation != null) theObject.Rotation.z = Zrotation;

            //For now, just rotate the object at a fixed speed
            //Zrotation += 2;
            //Yrotation += 1;
            if (Xoffset>0) Xoffset -= 10;
            if (Xoffset<=0 && Xrotation<=270) Xrotation += 2;
            if (Xoffset<=0 && Xrotation>=270) zooomOut = true;
            if (zooomOut && exploded==false)
            {
                Zrotation += 2;
                Yrotation += 1;
                Zoffset += 10;
                theObject.ObjectOffsets.z = Zoffset;
            }
            if (Zoffset>800)
            {
                if (exploded == false)
                {
                    ExplosionDeltaTime = DateTime.Now;
                    Physics.ExplodeObject(theObject, 400f);
                    exploded = true;
                }
                else 
                {
                    Physics.UpdateExplosion(theObject,ExplosionDeltaTime);
                }
            }
            theObject.ObjectOffsets.x = Xoffset;
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

        public void ReleaseParticles(I3dObject theObject)
        {
            throw new NotImplementedException();
        }
    }
}
