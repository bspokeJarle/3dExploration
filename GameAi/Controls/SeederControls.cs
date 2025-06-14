using Domain;
using Gma.System.MouseKeyHook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameAiAndControls.Controls
{
    public class SeederControls : IObjectMovement
    {
        public ITriangleMeshWithColor? StartCoordinates { get; set; }
        public ITriangleMeshWithColor? GuideCoordinates { get; set; }
        public I3dObject ParentObject { get; set; }
        public IPhysics Physics { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private float Yrotation = 0;
        //private float Xrotation = 0;
        private float Xrotation = 90;
        private float Zrotation = 0;
        
        private DateTime lastRelease = DateTime.Now;
        private readonly long releaseInterval = 10000000 * 10;

        public I3dObject MoveObject(I3dObject theObject)
        {
            if (lastRelease.Ticks + releaseInterval < DateTime.Now.Ticks) ReleaseParticles();
            //Set parent object
            ParentObject = theObject;
            if (theObject.Rotation!=null) theObject.Rotation.y=Yrotation;
            if (theObject.Rotation!=null) theObject.Rotation.x=Xrotation;
            if (theObject.Rotation!=null) theObject.Rotation.z=Zrotation;

            //If there are particles, move them
            if (ParentObject.Particles?.Particles.Count > 0)
            {
                ParentObject.Particles.MoveParticles();
            }
            //For now, just rotate the object at a fixed speed
            Zrotation += 2;
            //Xrotation += 2;
            return theObject;
        }

        public void ReleaseParticles()
        {
            lastRelease = DateTime.Now;
            ParentObject?.Particles?.ReleaseParticles(GuideCoordinates, StartCoordinates, ParentObject.ParentSurface.GlobalMapPosition, this, 3, null);
        }

        public void SetStartGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
            if (StartCoord != null) StartCoordinates = StartCoord;
            if (GuideCoord != null) GuideCoordinates = GuideCoord;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

    }
}
