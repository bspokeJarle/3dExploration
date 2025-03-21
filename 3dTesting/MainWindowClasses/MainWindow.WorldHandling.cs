using _3dTesting._3dRotation;
using _3dTesting._3dWorld;
using _3dTesting._Coordinates;
using _3dTesting.Helpers;
using Domain;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting.MainWindowClasses
{
    public class GameWorldManager
    {
        private readonly _3dTo2d From3dTo2d = new();
        private readonly _3dRotate Rotate3d = new();
        private readonly ParticleManager particleManager = new();

        public List<_2dTriangleMesh> UpdateWorld(_3dWorld._3dWorld world)
        {
            var activeWorld = _3dObjectHelpers.DeepCopy3dObjects(world.WorldInhabitants);
            var particleObjectList = new List<_3dObject>();
            var renderedList = new ConcurrentBag<_3dObject>(); // ✅ Use thread-safe collection

            Parallel.ForEach(activeWorld, inhabitant =>
            {
                if (!inhabitant.CheckInhabitantVisibility()) return; // ✅ Skip non-visible objects

                renderedList.Add(inhabitant);

                // ✅ Apply movement updates
                inhabitant.Movement?.MoveObject(inhabitant);

                // ✅ Optimize rotation offset check
                bool hasRotationOffset = inhabitant.RotationOffsetX > 0 ||
                                         inhabitant.RotationOffsetY > 0 ||
                                         inhabitant.RotationOffsetZ > 0;

                if (hasRotationOffset)
                {
                    Parallel.ForEach(inhabitant.ObjectParts, part =>
                    {
                        if (part.Triangles.Count > 500) // ✅ Only parallelize for large meshes
                        {
                            Parallel.For(0, part.Triangles.Count, i =>
                            {
                                var triangles = part.Triangles[i];
                                GameHelpers.ApplyRotationOffset(ref triangles,
                                    inhabitant.RotationOffsetX,
                                    inhabitant.RotationOffsetY,
                                    inhabitant.RotationOffsetZ);
                            });
                        }
                        else
                        {
                            for (int i = 0; i < part.Triangles.Count; i++)
                            {
                                var triangles = part.Triangles[i];
                                GameHelpers.ApplyRotationOffset(ref triangles,
                                    inhabitant.RotationOffsetX,
                                    inhabitant.RotationOffsetY,
                                    inhabitant.RotationOffsetZ);
                            }
                        }
                    });
                }

                // ✅ Rotate all parts efficiently
                Parallel.ForEach(inhabitant.ObjectParts, part =>
                {
                    part.Triangles = RotateMesh(part.Triangles, (Vector3)inhabitant.Rotation);
                    if (inhabitant.ObjectName == "Surface")
                        inhabitant.ParentSurface.RotatedSurfaceTriangles = part.Triangles;

                    SetMovementGuides(inhabitant, part, part.Triangles);
                });

                // ✅ Optimize centroid calculation
                if (hasRotationOffset)
                {
                    Vector3 centroid = GameHelpers.ComputeCentroid(inhabitant);

                    Parallel.ForEach(inhabitant.ObjectParts, part =>
                    {
                        if (part.Triangles.Count > 500) // ✅ Only parallelize large lists
                        {
                            Parallel.For(0, part.Triangles.Count, i =>
                            {
                                var triangles = part.Triangles[i];
                                GameHelpers.RecenterTriangle(ref triangles, centroid);
                            });
                        }
                        else
                        {
                            for (int i = 0; i < part.Triangles.Count; i++)
                            {
                                var triangles = part.Triangles[i];
                                GameHelpers.RecenterTriangle(ref triangles, centroid);
                            }
                        }
                    });
                }

                // ✅ Handle particles
                particleManager.HandleParticles(inhabitant, particleObjectList);
            });

            if (particleObjectList.Count > 0) activeWorld.AddRange(particleObjectList);
            CrashDetection.HandleCrashboxes(renderedList.ToList()); // ✅ Convert to List once
            return From3dTo2d.convertTo2dFromObjects(activeWorld);
        }


        private List<ITriangleMeshWithColor> RotateMesh(List<ITriangleMeshWithColor> mesh, Vector3 rotation)
        {
            var rotatedMesh = Rotate3d.RotateMesh(mesh, rotation.z, 'Z'); // ✅ Rotate around Z-axis first
            rotatedMesh = Rotate3d.RotateMesh(rotatedMesh, rotation.y, 'Y'); // ✅ Rotate around Y-axis
            rotatedMesh = Rotate3d.RotateMesh(rotatedMesh, rotation.x, 'X'); // ✅ Rotate around X-axis
            return rotatedMesh;
        }

        private void SetMovementGuides(_3dObject inhabitant, I3dObjectPart part, List<ITriangleMeshWithColor> rotatedMesh)
        {
            switch (part.PartName)
            {
                case "SeederParticlesStartGuide":
                case "JetMotor":
                    inhabitant.Movement.SetStartGuideCoordinates(rotatedMesh.First() as TriangleMeshWithColor, null);
                    break;
                case "SeederParticlesGuide":
                case "JetMotorDirectionGuide":
                    inhabitant.Movement.SetStartGuideCoordinates(null, rotatedMesh.First() as TriangleMeshWithColor);
                    break;
            }
        }
    }
}
