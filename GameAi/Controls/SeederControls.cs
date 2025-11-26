using Domain;
using Gma.System.MouseKeyHook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Domain._3dSpecificsImplementations;

namespace GameAiAndControls.Controls
{
    public class SeederControls : IObjectMovement
    {
        public ITriangleMeshWithColor? StartCoordinates { get; set; }
        public ITriangleMeshWithColor? GuideCoordinates { get; set; }
        public I3dObject ParentObject { get; set; }
        public IPhysics Physics { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private float Yrotation = 0;
        private float Xrotation = 90;
        private float Zrotation = 0;
        
        private DateTime lastRelease = DateTime.Now;
        private readonly long releaseInterval = 10000000 * 10;

        private bool _syncInitialized = false;
        private float _syncY = 0;
        //Factor to stay in sync with surface movement
        private float _syncFactor = 2.5f;
        private bool enableLogging = false;

        public I3dObject MoveObject(I3dObject theObject, IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
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
            //Zrotation += 2;
            //Xrotation += 2;
            SyncMovement(theObject);
            return theObject;
        }

        public void SyncMovement(I3dObject theObject)
        {
            if (!_syncInitialized)
            {               
                _syncInitialized = true;
                _syncY = theObject.ObjectOffsets.y;
            }

            theObject.ObjectOffsets = new Vector3()
            {
                x = theObject.ObjectOffsets.x,
                y = (theObject.ParentSurface.GlobalMapPosition.y * _syncFactor) + _syncY,
                z = theObject.ObjectOffsets.z
            };
            if (enableLogging)
            {
                Logger.Log($"Seeder Y position: {theObject.ObjectOffsets.y} surface at {theObject.ParentSurface.GlobalMapPosition.y}");
            }
        }

        public void ReleaseParticles()
        {
            lastRelease = DateTime.Now;
            ParentObject?.Particles?.ReleaseParticles(GuideCoordinates, StartCoordinates, ParentObject.ParentSurface.GlobalMapPosition, this, 3, null);
        }

        public void SetParticleGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
            if (StartCoord != null) StartCoordinates = StartCoord;
            if (GuideCoord != null) GuideCoordinates = GuideCoord;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void SetWeaponGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
            //No implementation needed
        }

        public void ConfigureAudio(IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            throw new NotImplementedException();
        }
    }
}
