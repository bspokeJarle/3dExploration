using _3dTesting._3dRotation;
using _3dTesting._3dWorld;
using _3dTesting._Coordinates;
using _3dTesting.Helpers;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting.MainWindowClasses
{
    public class GameWorldManager
    {
        private _3dTo2d From3dTo2d = new();
        private _3dRotate Rotate3d = new();
        private ParticleManager particleManager = new();

        public List<_2dTriangleMesh> UpdateWorld(_3dWorld._3dWorld world)
        {
            var activeWorld = _3dObjectHelpers.DeepCopy3dObjects(world.WorldInhabitants);
            var particleObjectList = new List<_3dObject>();

            Parallel.ForEach(activeWorld, inhabitant =>
            {
                // Apply movement updates
                inhabitant.Movement?.MoveObject(inhabitant);

                // Apply rotation offsets
                foreach (var part in inhabitant.ObjectParts)
                {
                    for (int i = 0; i < part.Triangles.Count; i++)
                    {
                        var triangle = part.Triangles[i];
                        GameHelpers.ApplyRotationOffset(ref triangle, inhabitant.RotationOffsetX, inhabitant.RotationOffsetY, inhabitant.RotationOffsetZ);
                        part.Triangles[i] = triangle;
                    }
                }

                Parallel.ForEach(inhabitant.ObjectParts, part =>
                {
                    var rotatedMesh = RotateMesh(part.Triangles, (Vector3)inhabitant.Rotation);

                    // Set movement guides
                    SetMovementGuides(inhabitant, part, rotatedMesh);

                    part.Triangles = rotatedMesh;
                });

                // Handle particles separately
                particleManager.HandleParticles(inhabitant, particleObjectList);
            });

            if (particleObjectList.Count > 0) activeWorld.AddRange(particleObjectList);
            CrashDetection.HandleCrashboxes(activeWorld);

            return From3dTo2d.convertTo2dFromObjects(activeWorld);
        }

        private List<ITriangleMeshWithColor> RotateMesh(List<ITriangleMeshWithColor> mesh, Vector3 rotation)
        {
            var rotatedMesh = Rotate3d.RotateZMesh(mesh, rotation.z);
            rotatedMesh = Rotate3d.RotateYMesh(rotatedMesh, rotation.y);
            rotatedMesh = Rotate3d.RotateXMesh(rotatedMesh, rotation.x);
            return rotatedMesh;
        }

        private void SetMovementGuides(_3dObject inhabitant, I3dObjectPart part, List<ITriangleMeshWithColor> rotatedMesh)
        {
            if (part.PartName == "SeederParticlesStartGuide")
            {
                inhabitant.Movement.SetStartGuideCoordinates(rotatedMesh.First() as TriangleMeshWithColor, null);
            }
            if (part.PartName == "SeederParticlesGuide")
            {
                inhabitant.Movement.SetStartGuideCoordinates(null, rotatedMesh.First() as TriangleMeshWithColor);
            }
            if (part.PartName == "JetMotor")
            {
                inhabitant.Movement.SetStartGuideCoordinates(rotatedMesh.First() as TriangleMeshWithColor, null);
            }
            if (part.PartName == "JetMotorDirectionGuide")
            {
                inhabitant.Movement.SetStartGuideCoordinates(null, rotatedMesh.First() as TriangleMeshWithColor);
            }
        }
    }
}
