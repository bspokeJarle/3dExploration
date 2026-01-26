using CommonUtilities._3DHelpers;
using Domain;
using System;

namespace GameAiAndControls.Controls
{
    public class TowerControls : IObjectMovement
    {
        public ITriangleMeshWithColor? StartCoordinates { get; set; }
        public ITriangleMeshWithColor? GuideCoordinates { get; set; }
        public I3dObject ParentObject { get; set; }
        public IPhysics Physics { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private float Yrotation = 0;
        private float Xrotation = 70;
        private float Zrotation = 0;

        private float TowerRotationSpeedZ = 1.5f;
        private float TowerZRotation = 0;

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
            //Xrotation += 2;

            RotateUpperTowerAnimation();

            return theObject;
        }

        public void RotateUpperTowerAnimation()
        {
            TowerZRotation += TowerRotationSpeedZ;

            if (ParentObject == null) return;
            var towerHeadFrame = ParentObject.ObjectParts.Find(obj => obj.PartName == "TowerHeadFrame");
            var towerHeadGlass = ParentObject.ObjectParts.Find(obj => obj.PartName == "TowerHeadGlass");
            var towerRoof = ParentObject.ObjectParts.Find(obj => obj.PartName == "TowerRoof");
            var towerRadar = ParentObject.ObjectParts.Find(obj => obj.PartName == "TowerRadar");

            if (towerHeadFrame != null && towerHeadGlass != null && towerRoof != null && towerRadar != null)
            {
                //Rotate around Z each frame
                towerHeadFrame.Triangles = _rotate.RotateZMesh(towerHeadFrame.Triangles, TowerZRotation);
                towerHeadGlass.Triangles = _rotate.RotateZMesh(towerHeadGlass.Triangles, TowerZRotation);
                towerRoof.Triangles = _rotate.RotateZMesh(towerRoof.Triangles, TowerZRotation);
                towerRadar.Triangles = _rotate.RotateZMesh(towerRadar.Triangles, TowerZRotation);
            }
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
