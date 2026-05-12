using CommonUtilities._3DHelpers;
using Domain;
using System;
using System.Collections.Generic;
using static Domain._3dSpecificsImplementations;

namespace GameAiAndControls.Controls
{
    public class TowerControls : IObjectMovement
    {
        public ITriangleMeshWithColor? StartCoordinates { get; set; }
        public ITriangleMeshWithColor? GuideCoordinates { get; set; }
        public I3dObject ParentObject { get; set; }
        public IPhysics Physics { get; set; } = new Physics.Physics();

        private float Yrotation = 0;
        private float Xrotation = 70;
        private float Zrotation = 0;

        private float TowerRotationSpeedZ = 1.5f;
        private float TowerZRotation = 0;

        private readonly _3dRotationCommon _rotate = new(); // Rotation fra CommonHelpers
        private readonly Dictionary<string, List<ITriangleMeshWithColor>> _originalTopPartMeshes = new();

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
            CacheOriginalTopParts();

            var towerHeadFrame = ParentObject.ObjectParts.Find(obj => obj.PartName == "TowerHeadFrame");
            var towerHeadGlass = ParentObject.ObjectParts.Find(obj => obj.PartName == "TowerHeadGlass");
            var towerRoof = ParentObject.ObjectParts.Find(obj => obj.PartName == "TowerRoof");
            var towerRadar = ParentObject.ObjectParts.Find(obj => obj.PartName == "TowerRadar");
            var snowTowerHeadFrame = ParentObject.ObjectParts.Find(obj => obj.PartName == "SnowTowerHeadFrame");
            var snowTowerGlass = ParentObject.ObjectParts.Find(obj => obj.PartName == "SnowTowerGlass");
            var snowTowerSnowLid = ParentObject.ObjectParts.Find(obj => obj.PartName == "SnowTowerSnowLid");
            var snowTowerAntenna = ParentObject.ObjectParts.Find(obj => obj.PartName == "SnowTowerAntenna");

            if (towerHeadFrame != null && towerHeadGlass != null && towerRoof != null && towerRadar != null)
            {
                ApplyRotatedTriangles(towerHeadFrame);
                ApplyRotatedTriangles(towerHeadGlass);
                ApplyRotatedTriangles(towerRoof);
                ApplyRotatedTriangles(towerRadar);
            }

            if (snowTowerHeadFrame != null && snowTowerGlass != null && snowTowerSnowLid != null && snowTowerAntenna != null)
            {
                ApplyRotatedTriangles(snowTowerHeadFrame);
                ApplyRotatedTriangles(snowTowerGlass);
                ApplyRotatedTriangles(snowTowerSnowLid);
                ApplyRotatedTriangles(snowTowerAntenna);
            }
        }

        private void CacheOriginalTopParts()
        {
            if (ParentObject == null)
                return;

            CacheOriginalPart("TowerHeadFrame");
            CacheOriginalPart("TowerHeadGlass");
            CacheOriginalPart("TowerRoof");
            CacheOriginalPart("TowerRadar");

            CacheOriginalPart("SnowTowerHeadFrame");
            CacheOriginalPart("SnowTowerGlass");
            CacheOriginalPart("SnowTowerSnowLid");
            CacheOriginalPart("SnowTowerAntenna");
        }

        private void CacheOriginalPart(string partName)
        {
            if (_originalTopPartMeshes.ContainsKey(partName))
                return;

            var part = ParentObject.ObjectParts.Find(obj => obj.PartName == partName);
            if (part == null)
                return;

            _originalTopPartMeshes[partName] = CloneTriangles(part.Triangles);
        }

        private void ApplyRotatedTriangles(I3dObjectPart part)
        {
            if (part.PartName == null)
                return;
            if (!_originalTopPartMeshes.TryGetValue(part.PartName, out var baseMesh))
                return;

            var source = CloneTriangles(baseMesh);
            part.Triangles = _rotate.RotateZMesh(source, TowerZRotation);
        }

        private static List<ITriangleMeshWithColor> CloneTriangles(List<ITriangleMeshWithColor> source)
        {
            var clone = new List<ITriangleMeshWithColor>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                var triangle = source[i];
                clone.Add(new TriangleMeshWithColor
                {
                    Color = triangle.Color,
                    noHidden = triangle.noHidden,
                    landBasedPosition = triangle.landBasedPosition,
                    angle = triangle.angle,
                    vert1 = CopyVector(triangle.vert1),
                    vert2 = CopyVector(triangle.vert2),
                    vert3 = CopyVector(triangle.vert3),
                    normal1 = CopyVector(triangle.normal1),
                    normal2 = CopyVector(triangle.normal2),
                    normal3 = CopyVector(triangle.normal3)
                });
            }

            return clone;
        }

        private static Vector3 CopyVector(IVector3 vector)
        {
            return new Vector3 { x = vector.x, y = vector.y, z = vector.z };
        }

        public void ReleaseParticles()
        {
        }

        public void SetParticleGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
        }
        public void SetRearEngineGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord) { }

        public void Dispose()
        {
            _originalTopPartMeshes.Clear();
            ParentObject = null!;
            StartCoordinates = null;
            GuideCoordinates = null;
            TowerZRotation = 0f;
        }

        public void SetWeaponGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
        }

        public void ConfigureAudio(IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
        }

        public void ReleaseParticles(I3dObject theObject)
        {
        }
    }
}
